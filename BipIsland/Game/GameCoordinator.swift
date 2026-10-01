import BipCore
import SpriteKit

/// Owns the game state and moves between scenes.
final class GameCoordinator: ObservableObject {
    /// Every scene is laid out on this canvas and scaled to fill the screen.
    /// Keep important things within ±780 × ±430 so 16:9 and 16:10 screens both show them.
    static let sceneSize = CGSize(width: 1600, height: 1000)

    let skView: SKView
    let voice = VoicePlayer()
    let sounds = BipSounds()
    let planner = LessonPlanner(sounds: Phonics.firstGroup.sounds)
    private(set) var tracker: MasteryTracker
    private let store = ProgressStore()
    var rng = SystemRandomNumberGenerator()
    private(set) var hasWelcomed = false

    init() {
        skView = SKView(frame: NSRect(x: 0, y: 0, width: 1280, height: 800))
        skView.ignoresSiblingOrder = false
        skView.preferredFramesPerSecond = 60
        tracker = store.loadTracker()
    }

    func start() {
        showMap()
    }

    // MARK: Navigation

    func showMap() {
        present(MapScene(coordinator: self, greet: !hasWelcomed))
        hasWelcomed = true
    }

    func showLettersIsland(greet: Bool = true) {
        present(LettersIslandScene(coordinator: self, greet: greet))
    }

    /// Bip's suggestion: the next activity for the first sound not yet mastered.
    func startNextActivity() {
        start(planner.nextActivity(tracker: tracker, using: &rng))
    }

    func start(_ activity: PlannedActivity) {
        switch activity.kind {
        case .meetTheSound:
            present(MeetSoundScene(coordinator: self, sound: activity.sound))
        case .soundHunt:
            present(SoundHuntScene(coordinator: self, sound: activity.sound))
        case .popTheLetter:
            present(PopLetterScene(coordinator: self, sound: activity.sound))
        }
    }

    private func present(_ scene: BaseScene) {
        voice.stop()
        skView.presentScene(scene, transition: .fade(with: Palette.paper, duration: 0.45))
    }

    // MARK: Progress

    @discardableResult
    func record(correct: Bool, for sound: PhonicsSound) -> MasteryChange {
        let change = tracker.record(correct: correct, for: sound)
        store.save(tracker)
        return change
    }

    func markMet(_ sound: PhonicsSound) {
        tracker.markMet(sound)
        store.save(tracker)
    }

    func stage(of sound: PhonicsSound) -> SoundStage {
        tracker.stage(of: sound)
    }

    // MARK: Audio and input

    func randomPraise() -> String {
        AudioCatalogue.praiseClips.randomElement(using: &rng) ?? "praise_01"
    }

    func randomHint() -> String {
        AudioCatalogue.hintClips.randomElement(using: &rng) ?? "hint_01"
    }

    /// Any key: hear the current sound or instruction again.
    func replayPrompt() {
        (skView.scene as? BaseScene)?.replayPrompt()
    }

    /// Paused while the parent gate is open. The voice is left to finish its (short) line so that
    /// scenes waiting for it to end carry on normally afterwards.
    func setPaused(_ paused: Bool) {
        skView.isPaused = paused
        (skView.scene as? BaseScene)?.didChangePause(paused)
    }
}
