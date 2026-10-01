/// Word Builder: drag letter tiles into slots to make the word in the picture.
/// The round offers three finished tile rows — the word and two misspellings — so there is
/// exactly one right answer to find. Tiles are graphemes, so "chip" builds from ch-i-p.
public struct WordBuilderGame: MiniGame {
    public static let id = "word_builder"
    public static let choiceCount = 3

    public struct Round: GameRound {
        public let word: Word
        /// Candidate tile rows, in screen order. Exactly one spells the word.
        public let choices: [[String]]
        public let answer: [String]
        public var usedItems: [String] { [word.word] }
    }

    public let entry: GameEntry
    public let skins = [
        GameSkin(id: "tiles", name: "Tiles"),
        GameSkin(id: "blocks", name: "Blocks"),
        GameSkin(id: "fridge_magnets", name: "Fridge magnets"),
    ]
    private let words: [Word]

    public init(content: ContentLibrary) throws {
        entry = try content.entry(forGame: Self.id)
        words = content.words.words.filter { $0.picturable && $0.picture != nil }
    }

    /// Words the child can spell: decodable, and 3 sounds for the youngest band.
    func candidates(for learner: Learner) -> [Word] {
        words.filter { word in
            guard word.decodableFromGroup <= learner.unlockedPhonicsGroup else { return false }
            return learner.band == .foundation ? word.soundCount == 3 : word.soundCount >= 3
        }
    }

    /// Misspellings: distinct shuffles of the tiles that don't spell the word.
    static func misspellings<G: RandomNumberGenerator>(of tiles: [String], count: Int, using rng: inout G) -> [[String]] {
        var out: [[String]] = []
        for _ in 0..<30 where out.count < count {
            let shuffled = tiles.shuffled(using: &rng)
            if shuffled != tiles && !out.contains(shuffled) { out.append(shuffled) }
        }
        return out
    }

    public func makeRound<G: RandomNumberGenerator>(for learner: Learner, session: GameSession, using rng: inout G) -> Round? {
        let fresh = candidates(for: learner).filter { !session.usedItems.contains($0.word) }
        let focused = learner.focusSoundID.flatMap { focus in fresh.filter { $0.graphemes.contains(focus) } }
        let pool = (focused?.isEmpty == false ? focused! : fresh).shuffled(using: &rng)
        for word in pool {
            let wrong = Self.misspellings(of: word.graphemes, count: Self.choiceCount - 1, using: &rng)
            guard wrong.count == Self.choiceCount - 1 else { continue }
            return Round(word: word, choices: ([word.graphemes] + wrong).shuffled(using: &rng), answer: word.graphemes)
        }
        return nil
    }

    public func isCorrect(_ choice: [String], in round: Round) -> Bool {
        choice == round.answer
    }

    public func skillID(for round: Round) -> String {
        let singleLetters = round.answer.allSatisfy { $0.count == 1 && !$0.contains("-") }
        if singleLetters && round.word.soundCount == 3 { return "spell_cvc" }
        return singleLetters ? "endings" : "spell_digraph"
    }
}
