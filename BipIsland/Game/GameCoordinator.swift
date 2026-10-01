import BipCore
import SpriteKit

/// The Letters games, built once from the content.
struct LettersGames {
    let course: PhonicsCourse
    let meet: MeetTheSoundGame
    let hunt: SoundHuntGame
    let pop: BubblePopGame

    init(content: ContentLibrary) throws {
        course = PhonicsCourse(content)
        meet = try MeetTheSoundGame(content: content, course: course)
        hunt = try SoundHuntGame(content: content, course: course)
        pop = try BubblePopGame(content: content, course: course)
    }
}

/// Owns the game state and moves between scenes.
final class GameCoordinator: ObservableObject {
    /// Every scene is laid out on this canvas and scaled to fill the screen.
    /// Keep important things within ±780 × ±430 so 16:9 and 16:10 screens both show them.
    static let sceneSize = CGSize(width: 1600, height: 1000)

    /// Skins the scenes can draw today: the first look of each game. The other skins in each game
    /// type get their artwork in phase 2b (docs/GAMES.md), and are picked at random once listed here.
    static let drawnSkins: Set<String> = ["paper_desk", "treasure_chests", "bubbles"]

    let skView: SKView
    let voice = VoicePlayer()
    let sounds = BipSounds()
    /// Nil only if the bundled content couldn't be read (CI checks it, so this shouldn't happen).
    let content: ContentLibrary?
    let letters: LettersGames?
    private(set) var progress: ChildProgress
    private let store = ProgressStore()
    private let childID: UUID?
    private let childAge: Int?
    var rng = SystemRandomNumberGenerator()
    private(set) var hasWelcomed = false

    init() {
        skView = SKView(frame: NSRect(x: 0, y: 0, width: 1280, height: 800))
        skView.ignoresSiblingOrder = false
        skView.preferredFramesPerSecond = 60

        var loaded: (ContentLibrary, LettersGames)?
        do {
            let library = try ContentLibrary.bundled()
            loaded = (library, try LettersGames(content: library))
        } catch {
            NSLog("Bip Island: game content didn't load, so Letters Island stays asleep: %@", String(describing: error))
        }
        content = loaded?.0
        letters = loaded?.1

        let child = store.currentChild()
        childID = child.id
        childAge = child.age
        progress = child.progress
    }

    func start() {
        showMap()
    }

    // MARK: Where the child is

    var today: Int { DayNumber.of(Date()) }

    var startingBand: Band {
        guard let content, let age = childAge else { return .foundation }
        return content.startingBand(forAge: age)
    }

    var lettersProgress: LettersProgress? {
        guard let content, let letters else { return nil }
        return LettersProgress(content: content, course: letters.course, progress: progress, startingBand: startingBand)
    }

    var planner: LessonPlanner? {
        guard let state = lettersProgress, let hunt = letters?.hunt else { return nil }
        let group = state.highestUnlockedGroup
        return LessonPlanner(letters: state, canHunt: { hunt.canHunt($0, upToGroup: group) })
    }

    // MARK: Navigation

    func showMap() {
        present(MapScene(coordinator: self, greet: !hasWelcomed))
        hasWelcomed = true
    }

    func showLettersIsland(greet: Bool = true) {
        guard lettersProgress != nil else { return showMap() }
        present(LettersIslandScene(coordinator: self, greet: greet))
    }

    /// Bip's suggestion: the next activity for the first sound not yet learnt.
    func startNextActivity() {
        guard let planner, let content else { return }
        start(planner.nextActivity(day: today, rules: content.masteryRules, using: &rng))
    }

    func start(_ activity: PlannedActivity) {
        guard let content, let letters, let state = lettersProgress else { return }
        let learner = state.learner(focus: activity.sound)
        switch activity.kind {
        case .meetTheSound:
            var session = GameSession(game: letters.meet, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
            guard let round = session.nextRound(of: letters.meet, for: learner, using: &rng) else { return }
            present(MeetSoundScene(coordinator: self, sound: round.sound))
        case .soundHunt:
            let session = GameSession(game: letters.hunt, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
            present(SoundHuntScene(coordinator: self, game: letters.hunt, session: session, learner: learner, focus: activity.sound))
        case .bubblePop:
            let session = GameSession(game: letters.pop, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
            present(PopLetterScene(coordinator: self, game: letters.pop, session: session, learner: learner, focus: activity.sound))
        }
        progress.notePlayed(gameID: activity.kind.gameID)
        store.save(progress, for: childID)
    }

    private func present(_ scene: BaseScene) {
        voice.stop()
        skView.presentScene(scene, transition: .fade(with: Palette.paper, duration: 0.45))
    }

    // MARK: Progress

    /// Records one answered question against its skill and sound. Returns the sound's level change.
    @discardableResult
    func record(correct: Bool, skillID: String, soundID: String?) -> MasteryChange {
        guard let content else { return .none }
        let change = progress.recordAnswer(correct: correct, skillID: skillID, soundID: soundID, day: today, rules: content.masteryRules)
        store.save(progress, for: childID)
        return change
    }

    func markMet(_ sound: PhonicsSound) {
        progress.markMet(soundID: sound.id)
        store.save(progress, for: childID)
    }

    func stage(of sound: PhonicsSound) -> SoundStage {
        progress.sounds.stage(of: sound)
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
