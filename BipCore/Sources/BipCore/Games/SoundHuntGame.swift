/// A picture card for Sound Hunt.
public struct HuntPicture: Hashable, Sendable {
    public let word: String
    public let picture: String
    public let audio: String
    /// Grapheme id of the first sound ("c" for cat).
    public let firstSoundID: String
    /// The first sound itself, so cat and kite both start with /k/.
    public let firstSoundIPA: String
    /// The phonics group from which the picture may be shown.
    public let availableFromGroup: Int
}

/// Sound Hunt: "Find the picture that starts with… sss". Pick the right picture out of three.
///
/// Pictures come from the decodable word bank (picturable words, only once their group is unlocked)
/// plus each unlocked sound's own picture from Meet the Sound. The word to find never repeats in a
/// session, and the other two pictures never start with the same sound.
public struct SoundHuntGame: MiniGame {
    public static let id = "sound_hunt"
    public static let choiceCount = 3

    public struct Round: GameRound {
        public let target: PhonicsSound
        public let choices: [HuntPicture]
        public let answer: HuntPicture
        public var usedItems: [String] { [answer.word] }
    }

    public let entry: GameEntry
    public let skins = [
        GameSkin(id: "treasure_chests", name: "Treasure chests"),
        GameSkin(id: "market_stalls", name: "Market stalls"),
        GameSkin(id: "picnic_basket", name: "Picnic basket"),
    ]
    /// Every picture the game can show, in word-bank order.
    public let pictures: [HuntPicture]
    private let course: PhonicsCourse

    public init(content: ContentLibrary, course: PhonicsCourse) throws {
        entry = try content.entry(forGame: Self.id)
        self.course = course
        pictures = Self.pictures(from: content)
    }

    static func pictures(from content: ContentLibrary) -> [HuntPicture] {
        var byWord: [String: HuntPicture] = [:]
        var order: [String] = []
        func add(_ picture: HuntPicture) {
            if let existing = byWord[picture.word] {
                if picture.availableFromGroup < existing.availableFromGroup { byWord[picture.word] = picture }
            } else {
                byWord[picture.word] = picture
                order.append(picture.word)
            }
        }
        for word in content.words.words where word.picturable {
            guard let picture = word.picture, let first = content.grapheme(id: word.firstSound) else { continue }
            add(HuntPicture(word: word.word, picture: picture, audio: word.audio, firstSoundID: first.id,
                            firstSoundIPA: first.ipa, availableFromGroup: word.decodableFromGroup))
        }
        // A sound's own picture (s → sun) is met in Meet the Sound, so it can be shown from that sound's group.
        for grapheme in content.phonics.graphemes {
            let mnemonic = grapheme.mnemonicWord
            let first: Grapheme?
            if let banked = content.word(mnemonic) {
                // "this" (for th) is in the word bank but can't be drawn.
                first = banked.picturable ? content.grapheme(id: banked.firstSound) : nil
            } else {
                // Not in the word bank: only usable when the word plainly starts with this sound (ink, egg, queen).
                first = mnemonic.hasPrefix(grapheme.grapheme) && !grapheme.grapheme.contains("-") ? grapheme : nil
            }
            guard let first else { continue }
            let group = min(grapheme.group, content.word(mnemonic)?.decodableFromGroup ?? grapheme.group)
            add(HuntPicture(word: mnemonic, picture: grapheme.mnemonicPicture, audio: AudioCatalogue.wordClip(for: mnemonic),
                            firstSoundID: first.id, firstSoundIPA: first.ipa, availableFromGroup: group))
        }
        return order.compactMap { byWord[$0] }
    }

    /// Pictures the child may see at this phonics group.
    public func pictures(upToGroup group: Int) -> [HuntPicture] {
        pictures.filter { $0.availableFromGroup <= group }
    }

    /// Whether any picture at all starts with this sound (no word starts with ng or x).
    public func canHunt(_ sound: PhonicsSound, upToGroup group: Int) -> Bool {
        pictures(upToGroup: group).contains { $0.firstSoundIPA == sound.ipa }
    }

    /// Alternates between the focus sound and the other sounds the child knows, so a session isn't
    /// cut short when the focus sound has only one or two pictures.
    public func makeRound<G: RandomNumberGenerator>(for learner: Learner, session: GameSession, using rng: inout G) -> Round? {
        let available = pictures(upToGroup: learner.unlockedPhonicsGroup)
        func freshAnswers(for sound: PhonicsSound) -> [HuntPicture] {
            available.filter { $0.firstSoundIPA == sound.ipa && !session.usedItems.contains($0.word) }
        }
        func others(for sound: PhonicsSound) -> [HuntPicture] {
            available.filter { $0.firstSoundIPA != sound.ipa }
        }
        func isPlayable(_ sound: PhonicsSound) -> Bool {
            !freshAnswers(for: sound).isEmpty && Set(others(for: sound).map(\.word)).count >= Self.choiceCount - 1
        }

        let targets = course.sounds(upToGroup: learner.unlockedPhonicsGroup)
            .filter { learner.knownSoundIDs.contains($0.id) && isPlayable($0) }
        let focus = targets.first { $0.id == learner.focusSoundID }
        let rest = targets.filter { $0.id != learner.focusSoundID }
        let target: PhonicsSound
        if let focus, session.roundsPlayed.isMultiple(of: 2) || rest.isEmpty {
            target = focus
        } else if let other = rest.randomElement(using: &rng) {
            target = other
        } else {
            return nil
        }

        guard let answer = freshAnswers(for: target).randomElement(using: &rng) else { return nil }
        let distractors = Self.pickDistractors(from: others(for: target), count: Self.choiceCount - 1,
                                               preferFreshOver: session.usedItems, using: &rng)
        guard distractors.count == Self.choiceCount - 1 else { return nil }
        return Round(target: target, choices: ([answer] + distractors).shuffled(using: &rng), answer: answer)
    }

    /// Different words, preferring ones not seen yet this session and different first sounds from each other.
    static func pickDistractors<G: RandomNumberGenerator>(from pool: [HuntPicture], count: Int, preferFreshOver used: Set<String>,
                                                          using rng: inout G) -> [HuntPicture] {
        let shuffled = pool.shuffled(using: &rng)
        let ordered = shuffled.filter { !used.contains($0.word) } + shuffled.filter { used.contains($0.word) }
        var picked: [HuntPicture] = []
        for candidate in ordered where picked.count < count {
            if !picked.contains(where: { $0.word == candidate.word || $0.firstSoundIPA == candidate.firstSoundIPA }) {
                picked.append(candidate)
            }
        }
        for candidate in ordered where picked.count < count {
            if !picked.contains(where: { $0.word == candidate.word }) {
                picked.append(candidate)
            }
        }
        return picked
    }

    public func isCorrect(_ choice: HuntPicture, in round: Round) -> Bool {
        choice.firstSoundIPA == round.target.ipa
    }

    public func skillID(for round: Round) -> String {
        PhonicsCourse.skillID(forGroup: round.target.group)
    }
}
