/// Meet the Sound: the letter bounces in, the narrator says its pure sound, then the picture word.
/// One sound per visit. The "answer" is clicking the letter to hear it again.
public struct MeetTheSoundGame: MiniGame {
    public static let id = "meet_the_sound"

    public struct Round: GameRound {
        public let sound: PhonicsSound
        public var choices: [PhonicsSound] { [sound] }
        public var usedItems: [String] { [sound.pictureWord] }
    }

    public let entry: GameEntry
    public let skins = [
        GameSkin(id: "paper_desk", name: "Paper desk"),
        GameSkin(id: "chalkboard", name: "Chalkboard"),
        GameSkin(id: "sand", name: "Sand"),
    ]
    public var roundsPerSession: Int? { 1 }
    private let course: PhonicsCourse

    public init(content: ContentLibrary, course: PhonicsCourse) throws {
        entry = try content.entry(forGame: Self.id)
        self.course = course
    }

    /// The focus sound if there is one; otherwise the first unlocked sound the child hasn't met.
    public func makeRound<G: RandomNumberGenerator>(for learner: Learner, session: GameSession, using rng: inout G) -> Round? {
        let open = course.sounds(upToGroup: learner.unlockedPhonicsGroup)
            .filter { !session.usedItems.contains($0.pictureWord) }
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
        PhonicsCourse.skillID(forGroup: round.sound.group)
    }
}
