/// Quick Look: dots or objects flash briefly — how many, without counting?
/// Same question shape as Count & Tap (one right numeral out of three); only what the child
/// sees changes, from animals to tap to dice, dominoes and ten-frames.
public struct QuickLookGame: MiniGame {
    public static let id = "quick_look"

    public struct Round: GameRound {
        public let count: Int
        public let choices: [Int]
        /// How long the dots show, in seconds (shorter at higher levels).
        public let flashSeconds: Double
        public var usedItems: [String] { [] }
    }

    public let entry: GameEntry
    public let skins = [
        GameSkin(id: "dice", name: "Dice"),
        GameSkin(id: "dominoes", name: "Dominoes"),
        GameSkin(id: "ten_frames", name: "Ten-frames"),
    ]
    private let numbers: NumbersFile

    public init(content: ContentLibrary) throws {
        entry = try content.entry(forGame: Self.id)
        numbers = content.numbers
    }

    /// The standard flash when a level doesn't say.
    public static let defaultFlashSeconds = 2.0

    /// 1 up to the level's number (3, 5, 8, 10), or what the band sees at a glance.
    public func makeRound<G: RandomNumberGenerator>(for learner: Learner, session: GameSession, using rng: inout G) -> Round? {
        let step = level(for: learner)
        let seeTo = min(step.countTo ?? numbers.bands[learner.band.rawValue]?.subitiseTo ?? 5, 10)
        guard seeTo >= 1 else { return nil }
        let count = Int.random(in: 1...seeTo, using: &rng)
        let flash = step.flashTenths.map { Double($0) / 10 } ?? Self.defaultFlashSeconds
        return Round(count: count, choices: NumeralChoices.three(around: count, using: &rng), flashSeconds: flash)
    }

    public func isCorrect(_ choice: Int, in round: Round) -> Bool {
        choice == round.count
    }

    public func skillID(for round: Round) -> String {
        if round.count <= 5 { return "subitise_5" }
        return round.count <= 10 ? "subitise_10" : "estimate"
    }
}
