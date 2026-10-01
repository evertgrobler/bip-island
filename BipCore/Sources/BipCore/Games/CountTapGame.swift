/// Three numeral answers around the right one, e.g. 4 → [3, 4, 5]. Shared by Count & Tap
/// and Quick Look: the games differ in what the child sees, not in what counts as right.
enum NumeralChoices {
    static func three<G: RandomNumberGenerator>(around count: Int, using rng: inout G) -> [Int] {
        let near = [count - 2, count - 1, count + 1, count + 2].filter { $0 >= 0 }
        let picks = Array(near.shuffled(using: &rng).prefix(2))
        return ([count] + picks).shuffled(using: &rng)
    }
}

/// Count & Tap: tap each animal as Bip counts aloud, then tap the numeral that says how many.
/// Real objects first, symbols after: the scene shows the animals, the round checks the numeral.
public struct CountTapGame: MiniGame {
    public static let id = "count_and_tap"

    public struct Round: GameRound {
        public let count: Int
        public let object: CountingObject
        public let choices: [Int]
        public var usedItems: [String] { [] }
    }

    public let entry: GameEntry
    public let skins = [
        GameSkin(id: "ducks", name: "Ducks"),
        GameSkin(id: "sheep", name: "Sheep"),
        GameSkin(id: "ladybirds", name: "Ladybirds"),
    ]
    private let numbers: NumbersFile

    public init(content: ContentLibrary) throws {
        entry = try content.entry(forGame: Self.id)
        numbers = content.numbers
    }

    /// 1 up to the level's number (5, 10, 15, 20), or what the band counts to. At most 20 here;
    /// bigger numbers live in the later Numbers games.
    public func makeRound<G: RandomNumberGenerator>(for learner: Learner, session: GameSession, using rng: inout G) -> Round? {
        let countTo = min(level(for: learner).countTo ?? numbers.bands[learner.band.rawValue]?.countTo ?? 10, 20)
        guard countTo >= 1, let object = numbers.countingObjects.randomElement(using: &rng) else { return nil }
        let count = Int.random(in: 1...countTo, using: &rng)
        return Round(count: count, object: object, choices: NumeralChoices.three(around: count, using: &rng))
    }

    public func isCorrect(_ choice: Int, in round: Round) -> Bool {
        choice == round.count
    }

    public func skillID(for round: Round) -> String {
        round.count <= 10 ? "count_10" : "count_20"
    }
}
