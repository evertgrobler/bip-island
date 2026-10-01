import Foundation
import BipCore

/// Hold Esc for 3 seconds → adult maths question → parent area (quit, updates).
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

    let versionText: String
    let voiceText: String
    let updatesConfigured: Bool

    var onOpen: (() -> Void)?
    var onClose: (() -> Void)?
    var onQuit: (() -> Void)?
    var onCheckForUpdates: (() -> Void)?

    private var hold = HoldDetector(duration: 3)
    private var timer: Timer?
    private var rng = SystemRandomNumberGenerator()

    init(versionText: String, voiceText: String, updatesConfigured: Bool) {
        self.versionText = versionText
        self.voiceText = voiceText
        self.updatesConfigured = updatesConfigured
        challenge = ParentChallenge.random(using: &rng)
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
        phase = .question
        onOpen?()
    }

    func submitAnswer() {
        if challenge.isCorrect(answer) {
            phase = .unlocked
        } else {
            lastAnswerWasWrong = true
            challenge = ParentChallenge.random(using: &rng)
            answer = ""
        }
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
}
