import BipCore
import SpriteKit

/// The Letters games, built once from the content.
struct LettersGames {
    let course: PhonicsCourse
    let meet: MeetTheSoundGame
    let hunt: SoundHuntGame
    let pop: BubblePopGame
    let trace: LetterTraceGame
    let monster: FeedMonsterGame

    init(content: ContentLibrary) throws {
        course = PhonicsCourse(content)
        meet = try MeetTheSoundGame(content: content, course: course)
        hunt = try SoundHuntGame(content: content, course: course)
        pop = try BubblePopGame(content: content, course: course)
        trace = try LetterTraceGame(content: content, course: course)
        monster = try FeedMonsterGame(content: content, course: course)
    }
}

/// The Numbers games.
struct NumbersGames {
    let count: CountTapGame
    let quick: QuickLookGame

    init(content: ContentLibrary) throws {
        count = try CountTapGame(content: content)
        quick = try QuickLookGame(content: content)
    }
}

/// The Words games.
struct WordsGames {
    let course: PhonicsCourse
    let buttons: SoundButtonsGame
    let builder: WordBuilderGame

    init(content: ContentLibrary) throws {
        course = PhonicsCourse(content)
        buttons = try SoundButtonsGame(content: content, course: course)
        builder = try WordBuilderGame(content: content)
    }
}

/// The Coding games.
struct CodingGames {
    let order: MorningOrderGame
    let path: BipsPathGame

    init(content: ContentLibrary) throws {
        order = try MorningOrderGame(content: content)
        path = try BipsPathGame(content: content)
    }
}

/// Owns the game state and moves between scenes.
final class GameCoordinator: ObservableObject {
    /// Every scene is laid out on this canvas and scaled to fill the screen.
    /// Keep important things within ±780 × ±430 so 16:9 and 16:10 screens both show them.
    static let sceneSize = CGSize(width: 1600, height: 1000)

    /// Skins the scenes can draw today: the first look of each game. The other skins in each game
    /// type get their artwork in phase 2b (docs/GAMES.md), and are picked at random once listed here.
    static let drawnSkins: Set<String> = [
        "paper_desk", "treasure_chests", "bubbles", "sparkles", "monster_blue",
        "ducks", "dice", "buttons", "tiles", "picture_cards", "island",
    ]

    let skView: SKView
    let voice = VoicePlayer()
    let sounds = BipSounds()
    /// Nil only if the bundled content couldn't be read (CI checks it, so this shouldn't happen).
    let content: ContentLibrary?
    /// Each island loads on its own, so one bad game file can't close the whole map.
    let letters: LettersGames?
    let numbers: NumbersGames?
    let words: WordsGames?
    let coding: CodingGames?
    /// The child who is playing now.
    private(set) var progress: ChildProgress
    private let store: ProgressStore
    @Published private(set) var children: [ChildSummary]
    @Published private(set) var childID: UUID?
    /// One break for the whole Mac, so switching profiles can't skip it.
    private var breakState: BreakState
    /// The most play one gap between checks can add to a child's minutes (an idle app left
    /// open overnight shouldn't count as hours of play).
    private static let maxPlayCreditSeconds = 30 * 60
    /// Play length, break length and daily maximum, set by parents behind the parent gate.
    let playSettings = PlayTimeSettings()
    private var lastBreakCheck = Date()
    var rng = SystemRandomNumberGenerator()
    private(set) var hasWelcomed = false

    init() {
        skView = GameSKView(frame: NSRect(x: 0, y: 0, width: 1280, height: 800))
        skView.ignoresSiblingOrder = false
        skView.preferredFramesPerSecond = 60

        var library: ContentLibrary?
        do {
            library = try ContentLibrary.bundled()
        } catch {
            NSLog("Bip Island: game content didn't load, so the islands stay asleep: %@", String(describing: error))
        }
        content = library
        if let library {
            letters = try? LettersGames(content: library)
            numbers = try? NumbersGames(content: library)
            words = try? WordsGames(content: library)
            coding = try? CodingGames(content: library)
            if letters == nil || numbers == nil || words == nil || coding == nil {
                NSLog("Bip Island: some island games didn't load and stay asleep")
            }
        } else {
            letters = nil
            numbers = nil
            words = nil
            coding = nil
        }

        // Locals first: Swift doesn't allow reading self's properties until all are set.
        let profileStore = ProgressStore()
        let all = profileStore.children()
        let id = profileStore.lastChildID() ?? all.first?.id
        var loaded = ChildProgress()
        if let id { loaded = profileStore.progress(for: id) }
        store = profileStore
        children = all
        childID = id
        progress = loaded
        breakState = profileStore.loadBreak(carryingOver: loaded.breaks) ?? BreakState(dayStamp: DayNumber.of(Date()))
    }

    /// With more than one child, the game opens on "Who's playing?".
    func start() {
        if children.count > 1 {
            showProfiles()
        } else {
            showMap()
        }
    }

    // MARK: Children

    var currentChild: ChildSummary? {
        children.first { $0.id == childID }
    }

    func showProfiles() {
        present(ProfilesScene(coordinator: self))
    }

    /// The child picks their picture: their progress, stars and stickers load, and the map opens.
    func choose(childID id: UUID) {
        guard children.contains(where: { $0.id == id }) else { return }
        currentBreakPhase() // Bank play time to the child who was playing.
        childID = id
        progress = store.progress(for: id)
        store.setLastChild(id)
        hasWelcomed = false
        showMap()
    }

    /// A parent adds a child (at most four). Returns false when they couldn't be added.
    @discardableResult
    func addChild(name: String, age: Int?, avatar: String?) -> Bool {
        let added = store.addChild(name: name, age: age, avatar: avatar) != nil
        children = store.children()
        return added
    }

    func updateChild(_ child: ChildSummary) {
        store.update(child)
        children = store.children()
    }

    /// Removes a child and their progress. If they were playing, the first child takes over.
    func deleteChild(_ id: UUID) {
        store.delete(id)
        children = store.children()
        if id == childID, let first = children.first {
            childID = first.id
            progress = store.progress(for: first.id)
            store.setLastChild(first.id)
        }
    }

    /// Stars in a child's jar, for their card on "Who's playing?".
    func starCount(for id: UUID) -> Int {
        id == childID ? progress.stars : store.progress(for: id).stars
    }

    /// What a parent sees for one child.
    func report(for id: UUID) -> ProgressReport? {
        guard let content else { return nil }
        let childProgress = id == childID ? progress : store.progress(for: id)
        let age = children.first { $0.id == id }?.age
        let band = age.map { content.startingBand(forAge: $0) } ?? .foundation
        return ProgressReport(content: content, progress: childProgress, startingBand: band, today: today)
    }

    // MARK: Where the child is

    var today: Int { DayNumber.of(Date()) }

    var startingBand: Band {
        guard let content, let age = currentChild?.age else { return .foundation }
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

    // MARK: Levels inside games

    /// The game being played now (nil on the map and islands).
    private(set) var currentGameID: String?
    private var visitStartStars = 0
    private var visitStartLevel = 0

    /// The child's level in a game (games.json `levels`), starting where their band does.
    func gameLevel(_ gameID: String) -> Int {
        guard let entry = content?.game(id: gameID) else { return 0 }
        return progress.gameLevel(for: gameID, startingAt: entry.startingLevel(for: startingBand))
    }

    func levelCount(_ gameID: String) -> Int {
        content?.game(id: gameID)?.levelSteps.count ?? 1
    }

    /// A learner for one game, at the child's level in it.
    func learnerFor(_ gameID: String, focus: PhonicsSound? = nil) -> Learner? {
        plainLearner(focus: focus)?.at(level: gameLevel(gameID))
    }

    /// What happened this visit, for the celebration at the end.
    struct VisitSummary {
        let starsEarned: Int
        let levelBefore: Int
        let levelNow: Int
        let levelCount: Int
        var levelledUp: Bool { levelNow > levelBefore }
    }

    func visitSummary() -> VisitSummary {
        let id = currentGameID ?? ""
        return VisitSummary(starsEarned: max(0, progress.stars - visitStartStars), levelBefore: visitStartLevel,
                            levelNow: currentGameID == nil ? visitStartLevel : gameLevel(id), levelCount: levelCount(id))
    }

    /// What round generators need: band, unlocked phonics group and known sounds.
    func plainLearner(focus: PhonicsSound? = nil) -> Learner? {
        guard let state = lettersProgress else { return nil }
        return state.learner(focus: focus)
    }

    /// The sound to practise outside the planner: Bip's suggestion, else the group's first sound.
    func practiceSound() -> PhonicsSound? {
        if let suggested = planner?.suggestedSound() { return suggested }
        return lettersProgress?.currentGroup.sounds.first
    }

    // MARK: Navigation

    /// Banks the minutes since the last check and reports where the break stands.
    @discardableResult
    func currentBreakPhase() -> BreakPhase {
        let now = Date()
        let elapsed = Int(now.timeIntervalSince(lastBreakCheck))
        lastBreakCheck = now
        let wasPlaying = breakState.breakEndsAt == nil
        let phase = PlayBreaks.advance(state: &breakState, elapsed: elapsed, now: now, day: today,
                                       settings: playSettings.breakSettings)
        store.saveBreak(breakState)
        if wasPlaying {
            progress.notePlayTime(seconds: min(elapsed, Self.maxPlayCreditSeconds), on: today)
            store.save(progress, for: childID)
        }
        return phase
    }

    /// A parent ends the break early from settings.
    func endBreakEarly() {
        PlayBreaks.endBreakEarly(state: &breakState)
        store.saveBreak(breakState)
        lastBreakCheck = Date()
    }

    /// False while Bip is charging or the day is done: games stay closed.
    func playAllowed() -> Bool {
        currentBreakPhase() == .playing
    }

    func showMap() {
        guard playAllowed() else { return showCharging() }
        present(MapScene(coordinator: self, greet: !hasWelcomed))
        hasWelcomed = true
    }

    func showCharging() {
        present(ChargingScene(coordinator: self))
    }

    func showStickers() {
        present(StickerScene(coordinator: self))
    }

    /// Bip's mystery box: once a day, bonus stars. Returns true when the box was full.
    @discardableResult
    func claimMystery() -> Bool {
        guard progress.claimMysteryBox(on: today) else { return false }
        store.save(progress, for: childID)
        return true
    }

    var mysteryAvailable: Bool {
        progress.lastMysteryDay != today
    }

    func showLettersIsland(greet: Bool = true) {
        guard playAllowed() else { return showCharging() }
        guard lettersProgress != nil else { return showMap() }
        present(LettersIslandScene(coordinator: self, greet: greet))
    }

    func showNumbersIsland(greet: Bool = true) {
        guard playAllowed() else { return showCharging() }
        guard numbers != nil else { return showMap() }
        present(NumbersIslandScene(coordinator: self, greet: greet))
    }

    func showWordsIsland(greet: Bool = true) {
        guard playAllowed() else { return showCharging() }
        guard words != nil else { return showMap() }
        present(WordsIslandScene(coordinator: self, greet: greet))
    }

    func showCodingIsland(greet: Bool = true) {
        guard playAllowed() else { return showCharging() }
        guard coding != nil else { return showMap() }
        present(CodingIslandScene(coordinator: self, greet: greet))
    }

    /// Bip's suggestion of what to play next, across all four islands. The child picks freely;
    /// one island glows, and Bip nudges towards another island when one dominates recent play.
    var islandSuggestion: IslandSuggestion? {
        guard let content else { return nil }
        return PlayRecommender(content: content).suggest(progress: progress, startingBand: startingBand,
                                                         day: today, rules: content.masteryRules, using: &rng)
    }

    /// Bip's suggestion: the next activity for the first sound not yet learnt.
    func startNextActivity() {
        guard let planner, let content else { return }
        start(planner.nextActivity(day: today, rules: content.masteryRules, using: &rng))
    }

    func start(_ activity: PlannedActivity) {
        guard let content, let letters, let state = lettersProgress else { return }
        let learner = state.learner(focus: activity.sound).at(level: gameLevel(activity.kind.gameID))
        switch activity.kind {
        case .meetTheSound:
            var session = GameSession(game: letters.meet, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
            guard let round = session.nextRound(of: letters.meet, for: learner, using: &rng) else { return }
            present(MeetSoundScene(coordinator: self, sound: round.sound), gameID: MeetTheSoundGame.id)
        case .soundHunt:
            let session = GameSession(game: letters.hunt, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
            present(SoundHuntScene(coordinator: self, game: letters.hunt, session: session, learner: learner, focus: activity.sound), gameID: SoundHuntGame.id)
        case .bubblePop:
            let session = GameSession(game: letters.pop, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
            present(PopLetterScene(coordinator: self, game: letters.pop, session: session, learner: learner, focus: activity.sound), gameID: BubblePopGame.id)
        }
        progress.notePlayed(gameID: activity.kind.gameID)
        store.save(progress, for: childID)
    }

    // MARK: Starting games from the islands

    func startTrace() {
        guard let content, let letters, let sound = practiceSound(), let learner = learnerFor(LetterTraceGame.id, focus: sound) else { return }
        var session = GameSession(game: letters.trace, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
        guard session.nextRound(of: letters.trace, for: learner, using: &rng) != nil else { return }
        progress.notePlayed(gameID: LetterTraceGame.id)
        store.save(progress, for: childID)
        present(TraceLetterScene(coordinator: self, game: letters.trace, sound: sound), gameID: LetterTraceGame.id)
    }

    func startMonster() {
        guard let content, let letters, let sound = practiceSound(), let learner = learnerFor(FeedMonsterGame.id, focus: sound) else { return }
        let session = GameSession(game: letters.monster, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
        progress.notePlayed(gameID: FeedMonsterGame.id)
        store.save(progress, for: childID)
        present(FeedMonsterScene(coordinator: self, game: letters.monster, session: session, learner: learner, focus: sound), gameID: FeedMonsterGame.id)
    }

    func startCount() {
        guard let content, let numbers, let learner = learnerFor(CountTapGame.id) else { return }
        let session = GameSession(game: numbers.count, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
        progress.notePlayed(gameID: CountTapGame.id)
        store.save(progress, for: childID)
        present(CountTapScene(coordinator: self, game: numbers.count, session: session, learner: learner), gameID: CountTapGame.id)
    }

    func startQuick() {
        guard let content, let numbers, let learner = learnerFor(QuickLookGame.id) else { return }
        let session = GameSession(game: numbers.quick, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
        progress.notePlayed(gameID: QuickLookGame.id)
        store.save(progress, for: childID)
        present(QuickLookScene(coordinator: self, game: numbers.quick, session: session, learner: learner), gameID: QuickLookGame.id)
    }

    func startButtons() {
        guard let content, let words, let learner = learnerFor(SoundButtonsGame.id) else { return }
        let session = GameSession(game: words.buttons, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
        progress.notePlayed(gameID: SoundButtonsGame.id)
        store.save(progress, for: childID)
        present(SoundButtonsScene(coordinator: self, game: words.buttons, session: session, learner: learner, course: words.course), gameID: SoundButtonsGame.id)
    }

    func startBuilder() {
        guard let content, let words, let learner = learnerFor(WordBuilderGame.id) else { return }
        let session = GameSession(game: words.builder, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
        progress.notePlayed(gameID: WordBuilderGame.id)
        store.save(progress, for: childID)
        present(WordBuilderScene(coordinator: self, game: words.builder, session: session, learner: learner), gameID: WordBuilderGame.id)
    }

    func startOrder() {
        guard let content, let coding, let learner = learnerFor(MorningOrderGame.id) else { return }
        let session = GameSession(game: coding.order, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
        progress.notePlayed(gameID: MorningOrderGame.id)
        store.save(progress, for: childID)
        present(MorningOrderScene(coordinator: self, game: coding.order, session: session, learner: learner), gameID: MorningOrderGame.id)
    }

    func startPath() {
        guard let content, let coding, let learner = learnerFor(BipsPathGame.id) else { return }
        let session = GameSession(game: coding.path, settings: content.games.session, drawableSkins: Self.drawnSkins, using: &rng)
        progress.notePlayed(gameID: BipsPathGame.id)
        store.save(progress, for: childID)
        present(BipsPathScene(coordinator: self, game: coding.path, session: session, learner: learner), gameID: BipsPathGame.id)
    }

    private func present(_ scene: BaseScene, gameID: String? = nil) {
        currentGameID = gameID
        if let gameID {
            visitStartStars = progress.stars
            visitStartLevel = gameLevel(gameID)
            if levelCount(gameID) > 1 {
                scene.showLevelBadge(level: visitStartLevel, of: levelCount(gameID))
            }
        }
        voice.stop()
        // While Bip charges (or the day is done) every game and island redirects here.
        if !(scene is ChargingScene) && !(scene is StickerScene) && !playAllowed() {
            skView.presentScene(ChargingScene(coordinator: self), transition: .fade(with: Palette.paper, duration: 0.45))
            return
        }
        skView.presentScene(scene, transition: .fade(with: Palette.paper, duration: 0.45))
    }

    // MARK: Progress

    /// Records one answered question against its skill and sound. Returns the sound's level change.
    @discardableResult
    func record(correct: Bool, skillID: String, soundID: String?) -> MasteryChange {
        guard let content else { return .none }
        let change = progress.recordAnswer(correct: correct, skillID: skillID, soundID: soundID, day: today, rules: content.masteryRules)
        // The game's own level moves too (3 right in a row up, 2 misses back); it shows at the end of the visit.
        if let gameID = currentGameID, let entry = content.game(id: gameID) {
            progress.recordGameAnswer(correct: correct, gameID: gameID, startingAt: entry.startingLevel(for: startingBand),
                                      levelCount: entry.levelSteps.count, rules: content.masteryRules)
        }
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

    /// Any key: the scene plays with it (arrows choose, Enter confirms), or the
    /// current sound or instruction plays again when the key does nothing in play.
    func handleKey(_ event: NSEvent) {
        if let scene = skView.scene as? BaseScene, scene.handleKey(event) { return }
        replayPrompt()
    }

    /// Any key pressed: say the current sound or instruction again.
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
