/// Bubble Pop: letters float up; pop the one that makes the sound you hear. No timer: bubbles drift
/// round and round, and exactly one bubble always shows the right letter.
///
/// The other letters never sound the same as the target (c and k) or look the same (ow in cow
/// and ow in snow), so there is only ever one right answer.
public struct BubblePopGame: MiniGame {
    public static let id = "bubble_pop"
    public static let bubbleCount = 5

    public struct Round: GameRound {
        public let target: PhonicsSound
        /// The starting bubbles. Exactly one is the target.
        public let choices: [PhonicsSound]
        /// Letters a bubble may show when it floats back in. Never confusable with the target.
        public let others: [PhonicsSound]
        /// How fast the bubbles drift (100 = normal).
        public var speedPercent: Int = 100
        public var usedItems: [String] { [] }
    }

    public let entry: GameEntry
    public let skins = [
        GameSkin(id: "bubbles", name: "Bubbles"),
        GameSkin(id: "balloons", name: "Balloons at the fair"),
        GameSkin(id: "fireflies", name: "Fireflies at night"),
    ]
    private let course: PhonicsCourse

    public init(content: ContentLibrary, course: PhonicsCourse) throws {
        entry = try content.entry(forGame: Self.id)
        self.course = course
    }

    /// The focus sound, or a random sound the child knows. Other letters come from every unlocked group.
    public func makeRound<G: RandomNumberGenerator>(for learner: Learner, session: GameSession, using rng: inout G) -> Round? {
        let unlocked = course.sounds(upToGroup: learner.unlockedPhonicsGroup)
        let known = unlocked.filter { learner.knownSoundIDs.contains($0.id) }
        guard let target = known.first(where: { $0.id == learner.focusSoundID }) ?? known.randomElement(using: &rng) else {
            return nil
        }
        let others = unlocked.filter { $0.id != target.id && !$0.isConfusable(with: target) }
        guard !others.isEmpty else { return nil }

        // More bubbles at higher levels. Different letters where there are enough, repeating only
        // when the pool is small.
        let step = level(for: learner)
        let count = min(max(step.bubbles ?? Self.bubbleCount, 3), 8)
        var distractors: [PhonicsSound] = []
        while distractors.count < count - 1 {
            distractors += others.shuffled(using: &rng).prefix(count - 1 - distractors.count)
        }
        var round = Round(target: target, choices: ([target] + distractors).shuffled(using: &rng), others: others)
        round.speedPercent = step.speedPercent ?? 100
        return round
    }

    /// The letter for a bubble floating back in. If no other bubble shows the target, this one must,
    /// so the right answer is always there to find; otherwise it shows one of the other letters,
    /// so there is never more than one right bubble.
    public func nextBubble<G: RandomNumberGenerator>(in round: Round, onScreen: [PhonicsSound], using rng: inout G) -> PhonicsSound {
        if !onScreen.contains(where: { $0.id == round.target.id }) { return round.target }
        return round.others.randomElement(using: &rng) ?? round.target
    }

    public func isCorrect(_ choice: PhonicsSound, in round: Round) -> Bool {
        choice.id == round.target.id
    }

    public func skillID(for round: Round) -> String {
        PhonicsCourse.skillID(forGroup: round.target.group)
    }
}
