/// Quick Look: dots or objects flash briefly — how many, without counting?
/// Same question shape as Count & Tap (one right numeral out of three); only what the child
/// sees changes, from animals to tap to dice, dominoes and ten-frames.
public struct QuickLookGame: MiniGame {
    public static let id = "quick_look"

    public struct Round: GameRound {
        public let count: Int
        public let choices: [Int]
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

    /// 1 up to what the band sees at a glance (5, then 10).
    public func makeRound<G: RandomNumberGenerator>(for learner: Learner, session: GameSession, using rng: inout G) -> Round? {
        let seeTo = min(numbers.bands[learner.band.rawValue]?.subitiseTo ?? 5, 10)
        guard seeTo >= 1 else { return nil }
        let count = Int.random(in: 1...seeTo, using: &rng)
        return Round(count: count, choices: NumeralChoices.three(around: count, using: &rng))
    }

    public func isCorrect(_ choice: Int, in round: Round) -> Bool {
        choice == round.count
    }

    public func skillID(for round: Round) -> String {
        if round.count <= 5 { return "subitise_5" }
        return round.count <= 10 ? "subitise_10" : "estimate"
    }
}
