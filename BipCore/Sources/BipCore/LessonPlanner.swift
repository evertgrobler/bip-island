/// The three Letters games, by their ids in games.json.
public enum ActivityKind: String, Codable, Sendable, CaseIterable {
    case meetTheSound = "meet_the_sound"
    case soundHunt = "sound_hunt"
    case bubblePop = "bubble_pop"

    public var gameID: String { rawValue }
}

public struct PlannedActivity: Equatable, Sendable {
    public let kind: ActivityKind
    /// The sound Bip wants to practise.
    public let sound: PhonicsSound

    public init(kind: ActivityKind, sound: PhonicsSound) {
        self.kind = kind
        self.sound = sound
    }
}

/// Where one child is on Letters Island, worked out from their progress and the skill map.
public struct LettersProgress: Sendable {
    public let course: PhonicsCourse
    public let progress: ChildProgress
    public let startingBand: Band
    /// Group numbers whose skill (snd_g1 …) is unlocked, in order. Group 1 is always open.
    public let unlockedGroups: [Int]
    /// The group shown on the island: the first open group with sounds still to learn.
    public let currentGroup: PhonicsGroup
    /// Sounds the child has met, plus every sound in a group they already know.
    public let knownSoundIDs: Set<String>

    public init(content: ContentLibrary, course: PhonicsCourse, progress: ChildProgress, startingBand: Band) {
        self.course = course
        self.progress = progress
        self.startingBand = startingBand

        let lookup = { (id: String) in content.skill(id: id) }
        func groupSkill(_ group: PhonicsGroup) -> Skill? { content.skill(id: group.skillID) }
        let open = course.groups.filter { group in
            guard let found = groupSkill(group) else { return false }
            return progress.isUnlocked(found, startingBand: startingBand, lookup: lookup)
        }
        let openGroups = open.isEmpty ? [course.firstGroup] : open
        unlockedGroups = openGroups.map(\.number)

        var known = Set<String>()
        for group in openGroups {
            let groupKnown = groupSkill(group).map { progress.isKnown($0, startingBand: startingBand) } ?? false
            for sound in group.sounds where groupKnown || progress.sounds.stage(of: sound) >= .met {
                known.insert(sound.id)
            }
        }
        knownSoundIDs = known

        let stillToLearn = openGroups.first { group in
            let treatedAsKnown = groupSkill(group).map { progress.isTreatedAsKnown($0, startingBand: startingBand) } ?? false
            return !treatedAsKnown && group.sounds.contains { progress.sounds.stage(of: $0) < .mastered }
        }
        currentGroup = stillToLearn ?? openGroups.last!
    }

    public var highestUnlockedGroup: Int { unlockedGroups.last ?? 1 }

    public func stage(of sound: PhonicsSound) -> SoundStage {
        progress.sounds.stage(of: sound)
    }

    /// What the round generators need, optionally focused on one sound.
    public func learner(focus: PhonicsSound? = nil) -> Learner {
        Learner(band: startingBand, unlockedPhonicsGroup: highestUnlockedGroup, knownSoundIDs: knownSoundIDs, focusSoundID: focus?.id)
    }
}

/// Bip's suggestion of what to play next on Letters Island.
///
/// Rules: a phonics group that is due for review comes first. Otherwise new sounds are met in order,
/// keeping no more than two met-but-not-yet-recognised sounds waiting; waiting sounds are practised
/// with Sound Hunt (met) and then Bubble Pop (recognises). When the group is all learnt, it's review.
public struct LessonPlanner: Sendable {
    /// How many met-but-not-recognised sounds can wait before Bip stops introducing new ones.
    public static let maxSoundsWaiting = 2

    public let letters: LettersProgress
    /// Whether Sound Hunt has pictures for a sound (no word starts with ng or x).
    public let canHunt: @Sendable (PhonicsSound) -> Bool

    public init(letters: LettersProgress, canHunt: @escaping @Sendable (PhonicsSound) -> Bool) {
        self.letters = letters
        self.canHunt = canHunt
    }

    public func nextActivity<G: RandomNumberGenerator>(day: Int, rules: MasteryRules, using rng: inout G) -> PlannedActivity {
        // 1. Reviews first.
        for number in letters.unlockedGroups {
            guard let group = letters.course.group(number),
                  letters.progress.isDueForReview(group.skillID, on: day, rules: rules) else { continue }
            if let review = reviewActivity(in: group, using: &rng) { return review }
        }

        // 2. Meet, then hunt, then pop, through the current group.
        let sounds = letters.currentGroup.sounds
        let notMastered = sounds.filter { letters.stage(of: $0) < .mastered }
        let waiting = notMastered.filter { letters.stage(of: $0) == .met }.count
        if let fresh = notMastered.first(where: { letters.stage(of: $0) == .new }), waiting < Self.maxSoundsWaiting {
            return PlannedActivity(kind: .meetTheSound, sound: fresh)
        }
        if let practise = notMastered.first(where: { letters.stage(of: $0) != .new }) {
            return PlannedActivity(kind: practiceKind(for: practise), sound: practise)
        }
        if let fresh = notMastered.first {
            return PlannedActivity(kind: .meetTheSound, sound: fresh)
        }

        // 3. Everything in the group is learnt: review it.
        return reviewActivity(in: letters.currentGroup, using: &rng)
            ?? PlannedActivity(kind: .meetTheSound, sound: sounds.randomElement(using: &rng) ?? letters.course.allSounds[0])
    }

    /// The sound Bip suggests next (the first in the current group not learnt yet), or nil.
    public func suggestedSound() -> PhonicsSound? {
        letters.currentGroup.sounds.first { letters.stage(of: $0) < .mastered }
    }

    /// Sound Hunt for a met sound, Bubble Pop once it's recognised (or if no picture starts with it).
    func practiceKind(for sound: PhonicsSound) -> ActivityKind {
        letters.stage(of: sound) == .met && canHunt(sound) ? .soundHunt : .bubblePop
    }

    private func reviewActivity<G: RandomNumberGenerator>(in group: PhonicsGroup, using rng: inout G) -> PlannedActivity? {
        let known = group.sounds.filter { letters.knownSoundIDs.contains($0.id) }
        guard let sound = known.randomElement(using: &rng) else { return nil }
        let kind: ActivityKind = canHunt(sound) && Bool.random(using: &rng) ? .soundHunt : .bubblePop
        return PlannedActivity(kind: kind, sound: sound)
    }
}
