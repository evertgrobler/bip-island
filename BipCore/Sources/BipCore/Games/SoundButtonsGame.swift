/// Sound Buttons: click each letter in c-a-t to hear its sound, then pick the picture
/// of the word they make together. Blending with a safety net: the answer is one of three
/// pictures, so a child who can't blend yet can still play by listening.
public struct SoundButtonsGame: MiniGame {
    public static let id = "sound_buttons"
    public static let choiceCount = 3

    public struct Round: GameRound {
        public let word: Word
        public let choices: [HuntPicture]
        public let answer: HuntPicture
        public var usedItems: [String] { [answer.word] }
    }

    public let entry: GameEntry
    public let skins = [
        GameSkin(id: "buttons", name: "Buttons"),
        GameSkin(id: "stepping_stones", name: "Stepping stones"),
        GameSkin(id: "piano_keys", name: "Piano keys"),
    ]
    private let course: PhonicsCourse
    private let words: [Word]

    public init(content: ContentLibrary, course: PhonicsCourse) throws {
        entry = try content.entry(forGame: Self.id)
        self.course = course
        words = content.words.words.filter { $0.picturable && $0.picture != nil }
    }

    /// Words the child can sound out: decodable, with the level's number of sounds. If the level
    /// has nothing yet at the child's phonics group, the band's usual words instead.
    func candidates(for learner: Learner) -> [Word] {
        let decodable = words.filter { $0.decodableFromGroup <= learner.unlockedPhonicsGroup }
        if let counts = level(for: learner).soundCounts {
            let atLevel = decodable.filter { counts.contains($0.soundCount) }
            if atLevel.count >= Self.choiceCount { return atLevel }
        }
        return decodable.filter { learner.band == .foundation ? $0.soundCount == 3 : $0.soundCount >= 3 }
    }

    public func makeRound<G: RandomNumberGenerator>(for learner: Learner, session: GameSession, using rng: inout G) -> Round? {
        let fresh = candidates(for: learner).filter { !session.usedItems.contains($0.word) }
        guard !fresh.isEmpty else { return nil }
        let focused = learner.focusSoundID.flatMap { focus in fresh.filter { $0.graphemes.contains(focus) } }
        guard let answer = (focused?.isEmpty == false ? focused! : fresh).randomElement(using: &rng),
              let answerFirst = answer.firstSoundIPA(in: course) else { return nil }
        let pool = fresh
            .filter { $0.word != answer.word && $0.firstSoundIPA(in: course) != answerFirst }
            .compactMap { HuntPicture(word: $0, course: course) }
        let distractors = SoundHuntGame.pickDistractors(from: pool, count: Self.choiceCount - 1,
                                                        preferFreshOver: session.usedItems, using: &rng)
        guard distractors.count == Self.choiceCount - 1,
              let mine = HuntPicture(word: answer, course: course) else { return nil }
        return Round(word: answer, choices: ([mine] + distractors).shuffled(using: &rng), answer: mine)
    }

    public func isCorrect(_ choice: HuntPicture, in round: Round) -> Bool {
        choice.word == round.answer.word
    }

    public func skillID(for round: Round) -> String {
        if round.word.soundCount <= 3 { return "blend_cvc" }
        return round.word.soundCount == 4 ? "blend_ccvc" : "read_longer"
    }
}

private extension Word {
    func firstSoundIPA(in course: PhonicsCourse) -> String? {
        course.sound(id: firstSound)?.ipa
    }
}

private extension HuntPicture {
    init?(word: Word, course: PhonicsCourse) {
        guard let picture = word.picture, let first = course.sound(id: word.firstSound) else { return nil }
        self.init(word: word.word, picture: picture, audio: word.audio, firstSoundID: first.id,
                  firstSoundIPA: first.ipa, availableFromGroup: word.decodableFromGroup)
    }
}
