import Foundation
#if canImport(CryptoKit)
import CryptoKit
#endif

/// An optional parent passcode for the parent gate, so grown-ups don't have to solve a maths
/// question every time. Only a salted hash is stored, never the digits themselves.
public struct ParentPasscode: Codable, Equatable, Sendable {
    public static let lengths = 4...8
    /// Wrong tries before the gate falls back to a maths question, so a child can't keep guessing.
    public static let triesBeforeMaths = 3

    public let salt: String
    public let hash: String

    /// Nil when the code isn't 4–8 digits.
    public init?(code: String, salt: String = UUID().uuidString) {
        guard let digits = Self.normalised(code) else { return nil }
        self.salt = salt
        hash = Self.digest(salt + digits)
    }

    public func matches(_ code: String) -> Bool {
        guard let digits = Self.normalised(code) else { return false }
        return Self.digest(salt + digits) == hash
    }

    /// Digits only (spaces ignored), and the right length.
    public static func normalised(_ code: String) -> String? {
        let digits = code.filter { !$0.isWhitespace }
        guard lengths.contains(digits.count), digits.allSatisfy({ $0.isASCII && $0.isNumber }) else { return nil }
        return digits
    }

    static func digest(_ text: String) -> String {
        #if canImport(CryptoKit)
        return SHA256.hash(data: Data(text.utf8)).map { String(format: "%02x", $0) }.joined()
        #else
        // FNV-1a fallback for platforms without CryptoKit (tests only; the app always has it).
        var value: UInt64 = 0xcbf2_9ce4_8422_2325
        for byte in text.utf8 { value = (value ^ UInt64(byte)) &* 0x0000_0100_0000_01B3 }
        return String(value, radix: 16)
        #endif
    }
}
