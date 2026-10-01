import Foundation

/// The adult maths question behind the parent gate. Hard enough that a 4–8-year-old won't guess it.
public struct ParentChallenge: Equatable, Sendable {
    public let left: Int
    public let right: Int

    public init(left: Int, right: Int) {
        self.left = left
        self.right = right
    }

    public var answer: Int { left * right }
    public var question: String { "\(left) × \(right)" }

    public static func random<G: RandomNumberGenerator>(using rng: inout G) -> ParentChallenge {
        ParentChallenge(left: Int.random(in: 12...19, using: &rng), right: Int.random(in: 3...9, using: &rng))
    }

    public func isCorrect(_ input: String) -> Bool {
        let digits = input.filter { !$0.isWhitespace }
        return Int(digits) == answer
    }
}

/// Detects a key held down for a set time (hold Esc for 3 seconds to open the parent gate).
/// Times are in seconds from any monotonic clock.
public struct HoldDetector: Equatable, Sendable {
    public let duration: TimeInterval
    public private(set) var pressedAt: TimeInterval?

    public init(duration: TimeInterval = 3) {
        self.duration = duration
    }

    public var isHeld: Bool { pressedAt != nil }

    /// Key down. Auto-repeat presses while already held don't restart the clock.
    public mutating func press(at time: TimeInterval) {
        if pressedAt == nil { pressedAt = time }
    }

    public mutating func release() {
        pressedAt = nil
    }

    /// 0…1 of the way to opening.
    public func progress(at time: TimeInterval) -> Double {
        guard let pressedAt, duration > 0 else { return 0 }
        return min(max((time - pressedAt) / duration, 0), 1)
    }

    public func isComplete(at time: TimeInterval) -> Bool {
        progress(at: time) >= 1
    }
}
