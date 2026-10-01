/// A food card for Feed the Monster.
public struct MonsterFood: Hashable, Sendable {
    public let word: String
    public let picture: String
    public let audio: String
    /// Grapheme id of the first sound ("m" for milk).
    public let firstSoundID: String
    /// The first sound itself, so corn and kite both start with /k/.
    public let firstSoundIPA: String
    /// The phonics group from which the food may be shown.
    public let availableFromGroup: Int
}

/// Feed the Monster: drag only the foods that start with the sound into a hungry monster.
/// One right food hides among two wrong ones. Foods come from the word bank's tagged foods,
/// so the game opens up as groups unlock (the first food, nut, arrives in group 3).
public struct FeedMonsterGame: MiniGame {
    public static let id = "feed_the_monster"
    public static let choiceCount = 3

    public struct Round: GameRound {
        public let target: PhonicsSound
        public let choices: [MonsterFood]
        public let answer: MonsterFood
        public var usedItems: [String] { [answer.word] }
    }

    public let entry: GameEntry
    public let skins = [
        GameSkin(id: "monster_blue", name: "Blue"),
        GameSkin(id: "monster_green", name: "Green"),
        GameSkin(id: "monster_purple", name: "Purple"),
    ]
    /// Every food the monster can eat, in word-bank order.
    public let foods: [MonsterFood]
    private let course: PhonicsCourse

    public init(content: ContentLibrary, course: PhonicsCourse) throws {
        entry = try content.entry(forGame: Self.id)
        self.course = course
        foods = content.words.words
            .filter { $0.tags?.contains("food") == true && $0.picturable && $0.picture != nil }
            .compactMap { word -> MonsterFood? in
                guard let first = content.grapheme(id: word.firstSound) else { return nil }
                return MonsterFood(word: word.word, picture: word.picture!, audio: word.audio,
                                   firstSoundID: first.id, firstSoundIPA: first.ipa,
                                   availableFromGroup: word.decodableFromGroup)
            }
    }

    /// Foods the child may see at this phonics group.
    public func foods(upToGroup group: Int) -> [MonsterFood] {
        foods.filter { $0.availableFromGroup <= group }
    }

    /// Alternates between the focus sound and the other sounds the child knows, so a session isn't
    /// cut short when the focus sound has only one food.
    public func makeRound<G: RandomNumberGenerator>(for learner: Learner, session: GameSession, using rng: inout G) -> Round? {
        let available = foods(upToGroup: learner.unlockedPhonicsGroup)
        func freshAnswers(for sound: PhonicsSound) -> [MonsterFood] {
            available.filter { $0.firstSoundIPA == sound.ipa && !session.usedItems.contains($0.word) }
        }
        func others(for sound: PhonicsSound) -> [MonsterFood] {
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
        let distractors = SoundHuntGame.pickDistractors(
            from: others(for: target).map { HuntPicture(word: $0.word, picture: $0.picture, audio: $0.audio,
                                                        firstSoundID: $0.firstSoundID, firstSoundIPA: $0.firstSoundIPA,
                                                        availableFromGroup: $0.availableFromGroup) },
            count: Self.choiceCount - 1, preferFreshOver: session.usedItems, using: &rng)
        guard distractors.count == Self.choiceCount - 1 else { return nil }
        let byWord = Dictionary(uniqueKeysWithValues: available.map { ($0.word, $0) })
        let choices = ([answer] + distractors.compactMap { byWord[$0.word] }).shuffled(using: &rng)
        guard choices.count == Self.choiceCount else { return nil }
        return Round(target: target, choices: choices, answer: answer)
    }

    public func isCorrect(_ choice: MonsterFood, in round: Round) -> Bool {
        choice.firstSoundIPA == round.target.ipa
    }

    public func skillID(for round: Round) -> String {
        PhonicsCourse.skillID(forGroup: round.target.group)
    }
}
