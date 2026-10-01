/// Word Builder: drag (or tap) letter tiles into slots to build the word in the picture.
/// Tiles are graphemes, so "chip" builds from ch-i-p, and each one says its sound as it lands.
/// The bank holds the word's tiles plus a few spare ones (more at higher levels). Spares come
/// only from sounds the child has met and never sound or look like a sound in the word, so
/// there is exactly one right spelling.
public struct WordBuilderGame: MiniGame {
    public static let id = "word_builder"
    public static let choiceCount = 3

    /// The most spare tiles a level can ask for.
    public static let maxSpares = 4

    /// One letter tile: the grapheme id, what the child sees and the sound it makes.
    public struct Tile: Hashable, Sendable {
        public let id: String
        public let text: String
        public let soundClip: String
    }

    public struct Round: GameRound {
        public let word: Word
        /// Candidate tile rows, in screen order. Exactly one spells the word.
        public let choices: [[String]]
        public let answer: [String]
        /// The word's tiles in spelling order.
        public var answerTiles: [Tile] = []
        /// Every tile to build from, shuffled: the word's tiles plus the spares.
        public var bank: [Tile] = []
        public var usedItems: [String] { [word.word] }
    }

    public let entry: GameEntry
    public let skins = [
        GameSkin(id: "tiles", name: "Tiles"),
        GameSkin(id: "blocks", name: "Blocks"),
        GameSkin(id: "fridge_magnets", name: "Fridge magnets"),
    ]
    private let words: [Word]
    private let course: PhonicsCourse

    public init(content: ContentLibrary) throws {
        entry = try content.entry(forGame: Self.id)
        let phonics = PhonicsCourse(content)
        course = phonics
        // Split digraphs (a-e in cake) can't sit in one slot, so those words stay out.
        words = content.words.words.filter { word in
            word.picturable && word.picture != nil && word.graphemes.allSatisfy { id in
                phonics.sound(id: id).map { !$0.grapheme.contains("-") } == true
            }
        }
    }

    func tile(_ sound: PhonicsSound) -> Tile {
        Tile(id: sound.id, text: sound.grapheme, soundClip: sound.soundClip)
    }

    /// Spare tiles: sounds the child has met that don't sound or look like any sound in the word.
    func spares<G: RandomNumberGenerator>(for word: Word, learner: Learner, count: Int, using rng: inout G) -> [Tile] {
        let inWord = word.graphemes.compactMap { course.sound(id: $0) }
        let pool = course.sounds(upToGroup: learner.unlockedPhonicsGroup).filter { sound in
            !sound.grapheme.contains("-") && !inWord.contains { $0.id == sound.id || $0.isConfusable(with: sound) }
        }
        return pool.shuffled(using: &rng).prefix(count).map(tile)
    }

    /// Words the child can spell: decodable, with the level's number of sounds. If the level
    /// has nothing yet at the child's phonics group, the band's usual words instead.
    func candidates(for learner: Learner) -> [Word] {
        let decodable = words.filter { $0.decodableFromGroup <= learner.unlockedPhonicsGroup }
        if let counts = level(for: learner).soundCounts {
            let atLevel = decodable.filter { counts.contains($0.soundCount) }
            if !atLevel.isEmpty { return atLevel }
        }
        return decodable.filter { learner.band == .foundation ? $0.soundCount == 3 : $0.soundCount >= 3 }
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
            var round = Round(word: word, choices: ([word.graphemes] + wrong).shuffled(using: &rng), answer: word.graphemes)
            round.answerTiles = word.graphemes.compactMap { course.sound(id: $0) }.map(tile)
            let spareCount = min(max(level(for: learner).spares ?? 1, 0), Self.maxSpares)
            round.bank = (round.answerTiles + spares(for: word, learner: learner, count: spareCount, using: &rng))
                .shuffled(using: &rng)
            return round
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
