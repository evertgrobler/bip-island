public enum ActivityKind: String, Codable, Sendable, CaseIterable {
    case meetTheSound
    case soundHunt
    case popTheLetter
}

public struct PlannedActivity: Equatable, Sendable {
    public let kind: ActivityKind
    public let sound: PhonicsSound

    public init(kind: ActivityKind, sound: PhonicsSound) {
        self.kind = kind
        self.sound = sound
    }
}

/// A "find the picture that starts with…" question.
public struct HuntQuestion: Equatable, Sendable {
    public let target: PhonicsSound
    /// Shuffled; always contains the target exactly once.
    public let choices: [PhonicsSound]
}

/// Bip's suggestion of what to play next, plus question building for each activity.
public struct LessonPlanner: Sendable {
    /// Sounds in teaching order.
    public let sounds: [PhonicsSound]
    /// Questions in one round of "Sound hunt" or "Pop the letter" (a round also ends on a level change).
    public static let questionsPerRound = 6

    public init(sounds: [PhonicsSound]) {
        precondition(!sounds.isEmpty, "A lesson needs at least one sound")
        self.sounds = sounds
    }

    public static func activity(for stage: SoundStage) -> ActivityKind? {
        switch stage {
        case .new: return .meetTheSound
        case .met: return .soundHunt
        case .recognises: return .popTheLetter
        case .mastered: return nil
        }
    }

    /// The first sound in order that isn't mastered yet, at the activity for its stage.
    /// Once everything is mastered, a random review of hunt or pop.
    public func nextActivity<G: RandomNumberGenerator>(tracker: MasteryTracker, using rng: inout G) -> PlannedActivity {
        for sound in sounds {
            if let kind = Self.activity(for: tracker.stage(of: sound)) {
                return PlannedActivity(kind: kind, sound: sound)
            }
        }
        let sound = sounds.randomElement(using: &rng)!
        let kind: ActivityKind = Bool.random(using: &rng) ? .soundHunt : .popTheLetter
        return PlannedActivity(kind: kind, sound: sound)
    }

    /// The sound Bip suggests next (the first one not mastered), or nil when all are mastered.
    public func suggestedSound(tracker: MasteryTracker) -> PhonicsSound? {
        sounds.first { tracker.stage(of: $0) < .mastered }
    }

    /// Three pictures: the target and two others, in random order.
    public func makeHuntQuestion<G: RandomNumberGenerator>(target: PhonicsSound, choiceCount: Int = 3, using rng: inout G) -> HuntQuestion {
        let others = sounds.filter { $0.id != target.id && $0.pictureWord != target.pictureWord }
        let distractors = Array(others.shuffled(using: &rng).prefix(max(choiceCount - 1, 0)))
        return HuntQuestion(target: target, choices: ([target] + distractors).shuffled(using: &rng))
    }

    /// Letters for the starting bubbles. The target always appears at least once.
    public func makeBubbleLetters<G: RandomNumberGenerator>(target: PhonicsSound, count: Int, using rng: inout G) -> [PhonicsSound] {
        guard count > 0 else { return [] }
        var letters = [target]
        while letters.count < count {
            letters.append(nextBubbleLetter(target: target, onScreen: letters, using: &rng))
        }
        return letters.shuffled(using: &rng)
    }

    /// The letter for a bubble that is (re)appearing. If no other bubble shows the target, this one must,
    /// so the right answer is always there to find. Otherwise it is a random letter that isn't the target,
    /// keeping the target to roughly one bubble at a time.
    public func nextBubbleLetter<G: RandomNumberGenerator>(target: PhonicsSound, onScreen: [PhonicsSound], using rng: inout G) -> PhonicsSound {
        if !onScreen.contains(target) { return target }
        let others = sounds.filter { $0 != target }
        return others.randomElement(using: &rng) ?? target
    }
}
