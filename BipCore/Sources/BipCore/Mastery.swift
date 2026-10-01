public enum MasteryChange: Equatable, Sendable {
    case none
    case levelledUp(to: Int)
    case droppedBack(to: Int)
}

/// A level plus the current streaks: right answers in a row move up, misses in a row drop back.
/// The numbers come from `MasteryRules` (Content/curriculum/skills.json).
public struct SkillMastery: Codable, Equatable, Sendable {
    public private(set) var level: Int
    public private(set) var correctStreak: Int
    public private(set) var missStreak: Int

    public init(level: Int = 0, correctStreak: Int = 0, missStreak: Int = 0) {
        self.level = level
        self.correctStreak = correctStreak
        self.missStreak = missStreak
    }

    /// Records one answered question. Streaks reset whenever the level changes.
    @discardableResult
    public mutating func record(correct: Bool, rules: MasteryRules, minLevel: Int = 0, maxLevel: Int) -> MasteryChange {
        if correct {
            correctStreak += 1
            missStreak = 0
            if correctStreak >= rules.correctInARowToMoveUp {
                correctStreak = 0
                if level < maxLevel {
                    level += 1
                    return .levelledUp(to: level)
                }
            }
        } else {
            missStreak += 1
            correctStreak = 0
            if missStreak >= rules.missesInARowToDropBack {
                missStreak = 0
                if level > minLevel {
                    level -= 1
                    return .droppedBack(to: level)
                }
            }
        }
        return .none
    }

    /// Moves straight to a level (e.g. after "Meet the Sound"), clearing the streaks.
    public mutating func raise(to newLevel: Int) {
        guard newLevel > level else { return }
        level = newLevel
        correctStreak = 0
        missStreak = 0
    }
}

/// Where a child is with one sound. Each stage has its own Letters game.
public enum SoundStage: Int, Codable, Sendable, CaseIterable, Comparable {
    /// Not met yet → Meet the Sound.
    case new = 0
    /// Met → Sound Hunt.
    case met = 1
    /// Recognises it in pictures → Bubble Pop.
    case recognises = 2
    /// Knows it → review only.
    case mastered = 3

    public static func < (lhs: SoundStage, rhs: SoundStage) -> Bool { lhs.rawValue < rhs.rawValue }
}

/// The level of every sound a child has worked on, keyed by sound id.
public struct MasteryTracker: Codable, Equatable, Sendable {
    public private(set) var skills: [String: SkillMastery]

    public init(skills: [String: SkillMastery] = [:]) {
        self.skills = skills
    }

    public func mastery(of soundID: String) -> SkillMastery {
        skills[soundID] ?? SkillMastery()
    }

    public func stage(of soundID: String) -> SoundStage {
        let level = min(max(mastery(of: soundID).level, 0), SoundStage.mastered.rawValue)
        return SoundStage(rawValue: level) ?? .new
    }

    public func stage(of sound: PhonicsSound) -> SoundStage {
        stage(of: sound.id)
    }

    /// Records one answered question about a sound. Two misses at "met" drop back to "new",
    /// so the child hears the sound introduced again.
    @discardableResult
    public mutating func record(correct: Bool, for soundID: String, rules: MasteryRules) -> MasteryChange {
        var mastery = mastery(of: soundID)
        let change = mastery.record(correct: correct, rules: rules, maxLevel: SoundStage.mastered.rawValue)
        skills[soundID] = mastery
        return change
    }

    /// Called when Meet the Sound finishes.
    public mutating func markMet(_ soundID: String) {
        var mastery = mastery(of: soundID)
        mastery.raise(to: SoundStage.met.rawValue)
        skills[soundID] = mastery
    }
}
