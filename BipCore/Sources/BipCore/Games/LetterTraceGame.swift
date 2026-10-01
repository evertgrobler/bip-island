/// Letter Trace: follow the letter's path with the mouse while sparkles trail behind.
/// One sound per visit, like Meet the Sound. The "answer" is finishing the trace.
public struct LetterTraceGame: MiniGame {
    public static let id = "letter_trace"

    public struct Round: GameRound {
        public let sound: PhonicsSound
        public var choices: [PhonicsSound] { [sound] }
        public var usedItems: [String] { [sound.id] }

        public init(sound: PhonicsSound) {
            self.sound = sound
        }
    }

    public let entry: GameEntry
    public let skins = [
        GameSkin(id: "sparkles", name: "Sparkles"),
        GameSkin(id: "snail_trail", name: "Snail trail"),
        GameSkin(id: "paint", name: "Paint"),
    ]
    public var roundsPerSession: Int? { 1 }
    private let course: PhonicsCourse

    public init(content: ContentLibrary, course: PhonicsCourse) throws {
        entry = try content.entry(forGame: Self.id)
        self.course = course
    }

    /// The focus sound if there is one; otherwise the first unlocked sound the child hasn't met,
    /// so tracing follows meeting in teaching order.
    public func makeRound<G: RandomNumberGenerator>(for learner: Learner, session: GameSession, using rng: inout G) -> Round? {
        let open = course.sounds(upToGroup: learner.unlockedPhonicsGroup)
            .filter { !session.usedItems.contains($0.id) }
        if let focus = learner.focusSoundID, let sound = open.first(where: { $0.id == focus }) {
            return Round(sound: sound)
        }
        if let fresh = open.first(where: { !learner.knownSoundIDs.contains($0.id) }) {
            return Round(sound: fresh)
        }
        return open.randomElement(using: &rng).map(Round.init)
    }

    public func isCorrect(_ choice: PhonicsSound, in round: Round) -> Bool {
        choice.id == round.sound.id
    }

    public func skillID(for round: Round) -> String {
        "letter_form"
    }
}
