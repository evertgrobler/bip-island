/// The plug-in template every mini-game follows (docs/GAMES.md, "Making it robust"):
/// it says which skills and ages it teaches (from Content/curriculum/games.json), builds a round at
/// random from the content lists at the child's level, checks an answer, and lists its skins.
/// A new game is one new file with one type that conforms to `MiniGame`.
public protocol MiniGame: Sendable {
    associatedtype Round: GameRound

    /// Matches the game's id in games.json.
    static var id: String { get }

    /// The game's registry entry: island, ages, skills, build phase.
    var entry: GameEntry { get }

    /// Same rules, different setting. The first skin is the one drawn when no other is ready.
    var skins: [GameSkin] { get }

    /// Rounds in one visit, if the game wants something other than games.json's `roundsPerSession`.
    var roundsPerSession: Int? { get }

    /// A new random round for this child, or nil when there is nothing fresh left to ask.
    /// Must never reuse an item in `session.usedItems`.
    func makeRound<G: RandomNumberGenerator>(for learner: Learner, session: GameSession, using rng: inout G) -> Round?

    /// Whether a choice is right. Written from the rules, not from how the round was built, so the
    /// tests can check that every round has exactly one right answer.
    func isCorrect(_ choice: Round.Choice, in round: Round) -> Bool

    /// The skill in skills.json that a round practises.
    func skillID(for round: Round) -> String
}

public extension MiniGame {
    var roundsPerSession: Int? { nil }

    /// The difficulty step this child plays at.
    func level(for learner: Learner) -> GameLevel {
        entry.level(learner.gameLevel)
    }

    /// The right choices in a round. A fair round has exactly one.
    func correctChoices(in round: Round) -> [Round.Choice] {
        round.choices.filter { isCorrect($0, in: round) }
    }
}

/// One question.
public protocol GameRound: Sendable {
    associatedtype Choice: Hashable & Sendable
    /// Everything the child can pick, in screen order.
    var choices: [Choice] { get }
    /// Content items this round uses up (words), so they aren't asked again in the same session.
    var usedItems: [String] { get }
}

/// A look for a game: Bubble Pop can be bubbles, balloons or fireflies.
public struct GameSkin: Hashable, Sendable, Identifiable {
    public let id: String
    public let name: String

    public init(id: String, name: String) {
        self.id = id
        self.name = name
    }
}

/// What the round generators need to know about the child.
public struct Learner: Sendable {
    public let band: Band
    /// The highest phonics group unlocked. Words are only offered once their group is unlocked.
    public let unlockedPhonicsGroup: Int
    /// Sounds the child has met (or that sit in a group they already know).
    public let knownSoundIDs: Set<String>
    /// The sound Bip wants to practise, if any.
    public let focusSoundID: String?
    /// The child's level in the game being played (an index into the game's `levels`).
    public let gameLevel: Int

    public init(band: Band, unlockedPhonicsGroup: Int, knownSoundIDs: Set<String>, focusSoundID: String? = nil, gameLevel: Int = 0) {
        self.band = band
        self.unlockedPhonicsGroup = unlockedPhonicsGroup
        self.knownSoundIDs = knownSoundIDs
        self.focusSoundID = focusSoundID
        self.gameLevel = gameLevel
    }

    public func focusing(on soundID: String?) -> Learner {
        Learner(band: band, unlockedPhonicsGroup: unlockedPhonicsGroup, knownSoundIDs: knownSoundIDs, focusSoundID: soundID, gameLevel: gameLevel)
    }

    public func at(level: Int) -> Learner {
        Learner(band: band, unlockedPhonicsGroup: unlockedPhonicsGroup, knownSoundIDs: knownSoundIDs, focusSoundID: focusSoundID, gameLevel: level)
    }
}

/// One visit to a game: a skin and a run of rounds, with nothing asked twice.
public struct GameSession: Sendable {
    public let gameID: String
    public let skin: GameSkin
    public let maxRounds: Int
    public private(set) var roundsPlayed = 0
    public private(set) var usedItems: Set<String> = []

    public init(gameID: String, skin: GameSkin, maxRounds: Int) {
        self.gameID = gameID
        self.skin = skin
        self.maxRounds = max(1, maxRounds)
    }

    /// Starts a session with a random skin from those the app can draw (the first skin if none match).
    public init<Game: MiniGame, G: RandomNumberGenerator>(game: Game, settings: SessionSettings, drawableSkins: Set<String>, using rng: inout G) {
        let ready = game.skins.filter { drawableSkins.contains($0.id) }
        let skin = ready.randomElement(using: &rng) ?? game.skins.first ?? GameSkin(id: "default", name: "Default")
        self.init(gameID: Game.id, skin: skin, maxRounds: game.roundsPerSession ?? settings.roundsPerSession)
    }

    public var isFinished: Bool { roundsPlayed >= maxRounds }

    /// The next round, or nil when the session is over (all rounds played, or nothing fresh left).
    public mutating func nextRound<Game: MiniGame, G: RandomNumberGenerator>(of game: Game, for learner: Learner, using rng: inout G) -> Game.Round? {
        guard !isFinished, let round = game.makeRound(for: learner, session: self, using: &rng) else { return nil }
        roundsPlayed += 1
        usedItems.formUnion(round.usedItems)
        return round
    }
}

/// Raised when a game type's id isn't in games.json.
public struct UnknownGameError: Error, CustomStringConvertible {
    public let id: String
    public var description: String { "Game '\(id)' is not in Content/curriculum/games.json" }
}

extension ContentLibrary {
    /// The registry entry for a game type, or an error if games.json doesn't list it.
    public func entry(forGame id: String) throws -> GameEntry {
        guard let entry = game(id: id) else { throw UnknownGameError(id: id) }
        return entry
    }
}
