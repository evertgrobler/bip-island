import Foundation
import BipCore

/// Hold Esc for 3 seconds → the parent passcode (if one is set) or an adult maths question →
/// parent area. After `ParentPasscode.triesBeforeMaths` wrong codes it asks maths instead.
final class ParentGateModel: ObservableObject {
    enum Phase: Equatable {
        case closed
        case question
        case unlocked
    }

    @Published private(set) var phase: Phase = .closed
    /// 0…1 while Esc is held.
    @Published private(set) var holdProgress: Double = 0
    @Published private(set) var challenge: ParentChallenge
    @Published var answer = ""
    @Published private(set) var lastAnswerWasWrong = false
    /// True while the gate is asking for the passcode rather than maths.
    @Published private(set) var usingPasscode = false
    /// Whether a parent passcode is set on this Mac.
    @Published private(set) var hasPasscode = false

    let versionText: String
    let voiceText: String
    let updatesConfigured: Bool

    var onOpen: (() -> Void)?
    var onClose: (() -> Void)?
    var onQuit: (() -> Void)?
    var onCheckForUpdates: (() -> Void)?
    var onEndBreakEarly: (() -> Void)?
    /// Set by the app: play length, break length and daily maximum.
    var playSettings: PlayTimeSettings?

    private static let passcodeKey = "bip.parentPasscode"
    private var passcode: ParentPasscode? {
        didSet { hasPasscode = passcode != nil }
    }
    private var wrongPasscodeTries = 0
    private var hold = HoldDetector(duration: 3)
    private var timer: Timer?
    private var rng = SystemRandomNumberGenerator()

    init(versionText: String, voiceText: String, updatesConfigured: Bool) {
        self.versionText = versionText
        self.voiceText = voiceText
        self.updatesConfigured = updatesConfigured
        challenge = ParentChallenge.random(using: &rng)
        if let data = UserDefaults.standard.data(forKey: Self.passcodeKey) {
            passcode = try? JSONDecoder().decode(ParentPasscode.self, from: data)
        }
        hasPasscode = passcode != nil
    }

    private var now: TimeInterval { ProcessInfo.processInfo.systemUptime }

    func escapePressed() {
        guard phase == .closed, !hold.isHeld else { return }
        hold.press(at: now)
        timer?.invalidate()
        timer = Timer.scheduledTimer(withTimeInterval: 1.0 / 30.0, repeats: true) { [weak self] _ in
            self?.tick()
        }
    }

    func escapeReleased() {
        hold.release()
        timer?.invalidate()
        timer = nil
        holdProgress = 0
    }

    private func tick() {
        holdProgress = hold.progress(at: now)
        if hold.isComplete(at: now) {
            open()
        }
    }

    func open() {
        escapeReleased()
        challenge = ParentChallenge.random(using: &rng)
        answer = ""
        lastAnswerWasWrong = false
        wrongPasscodeTries = 0
        usingPasscode = passcode != nil
        phase = .question
        onOpen?()
    }

    func submitAnswer() {
        if usingPasscode, let passcode {
            if passcode.matches(answer) {
                phase = .unlocked
            } else {
                wrongPasscodeTries += 1
                lastAnswerWasWrong = true
                if wrongPasscodeTries >= ParentPasscode.triesBeforeMaths { useMathsInstead() }
            }
            answer = ""
            return
        }
        if challenge.isCorrect(answer) {
            phase = .unlocked
        } else {
            lastAnswerWasWrong = true
            challenge = ParentChallenge.random(using: &rng)
            answer = ""
        }
    }

    /// "Forgot the passcode?": answer a maths question instead.
    func useMathsInstead() {
        usingPasscode = false
        challenge = ParentChallenge.random(using: &rng)
        answer = ""
    }

    /// Sets or changes the passcode. False when it isn't 4–8 digits.
    @discardableResult
    func setPasscode(_ code: String) -> Bool {
        guard let new = ParentPasscode(code: code), let data = try? JSONEncoder().encode(new) else { return false }
        UserDefaults.standard.set(data, forKey: Self.passcodeKey)
        passcode = new
        return true
    }

    func removePasscode() {
        UserDefaults.standard.removeObject(forKey: Self.passcodeKey)
        passcode = nil
    }

    func close() {
        guard phase != .closed else { return }
        phase = .closed
        answer = ""
        onClose?()
    }

    func quit() {
        onQuit?()
    }

    func checkForUpdates() {
        onCheckForUpdates?()
    }

    func endBreakEarly() {
        onEndBreakEarly?()
    }
}
