/// Morning Order: put picture cards in order — wake up, brush teeth, eat breakfast.
/// The round offers three finished orders — the routine and two muddles — so there is exactly
/// one right answer. Routines come from the content, so new ones arrive with no new code.
public struct MorningOrderGame: MiniGame {
    public static let id = "morning_order"
    public static let choiceCount = 3

    public struct Round: GameRound {
        public let set: SequenceSet
        /// Candidate orders, in screen order. Exactly one runs first-to-last.
        public let choices: [[SequenceCard]]
        public let answer: [SequenceCard]
        public var usedItems: [String] { [set.id] }
    }

    public let entry: GameEntry
    public let skins = [
        GameSkin(id: "picture_cards", name: "Picture cards"),
        GameSkin(id: "recipe_book", name: "Recipe book"),
        GameSkin(id: "garden_bed", name: "Garden bed"),
    ]
    private let sets: [SequenceSet]

    public init(content: ContentLibrary) throws {
        entry = try content.entry(forGame: Self.id)
        sets = content.sequences.sets
    }

    /// The right order: cards sorted by their number.
    public static func correctOrder(of set: SequenceSet) -> [SequenceCard] {
        set.cards.sorted { $0.n < $1.n }
    }

    public func makeRound<G: RandomNumberGenerator>(for learner: Learner, session: GameSession, using rng: inout G) -> Round? {
        let fresh = sets.filter { $0.band <= learner.band && !session.usedItems.contains($0.id) }
            .shuffled(using: &rng)
        for set in fresh {
            let answer = Self.correctOrder(of: set)
            var wrong: [[SequenceCard]] = []
            for _ in 0..<30 where wrong.count < Self.choiceCount - 1 {
                let shuffled = set.cards.shuffled(using: &rng)
                if shuffled != answer && !wrong.contains(shuffled) { wrong.append(shuffled) }
            }
            guard wrong.count == Self.choiceCount - 1 else { continue }
            return Round(set: set, choices: ([answer] + wrong).shuffled(using: &rng), answer: answer)
        }
        return nil
    }

    public func isCorrect(_ choice: [SequenceCard], in round: Round) -> Bool {
        choice == round.answer
    }

    public func skillID(for round: Round) -> String {
        "sequencing"
    }
}
