/// The mastery rules from the game plan.
public enum MasteryRules {
    /// Correct answers in a row that move a skill up a level.
    public static let correctInARowToLevelUp = 3
    /// Misses in a row that drop a skill back a level.
    public static let missesInARowToDropBack = 2
}

public enum MasteryChange: Equatable, Sendable {
    case none
    case levelledUp(to: Int)
    case droppedBack(to: Int)
}

/// Progress on one skill: a level plus the current streaks.
public struct SkillMastery: Codable, Equatable, Sendable {
    public private(set) var level: Int
    public private(set) var correctStreak: Int
    public private(set) var missStreak: Int

    public init(level: Int = 0, correctStreak: Int = 0, missStreak: Int = 0) {
        self.level = level
        self.correctStreak = correctStreak
        self.missStreak = missStreak
    }

    /// Records one answered question. Three right in a row moves up a level,
    /// two misses in a row drops back one. Streaks reset whenever the level changes.
    @discardableResult
    public mutating func record(correct: Bool, minLevel: Int = 0, maxLevel: Int) -> MasteryChange {
        if correct {
            correctStreak += 1
            missStreak = 0
            if correctStreak >= MasteryRules.correctInARowToLevelUp {
                correctStreak = 0
                if level < maxLevel {
                    level += 1
                    return .levelledUp(to: level)
                }
            }
        } else {
            missStreak += 1
            correctStreak = 0
            if missStreak >= MasteryRules.missesInARowToDropBack {
                missStreak = 0
                if level > minLevel {
                    level -= 1
                    return .droppedBack(to: level)
                }
            }
        }
        return .none
    }

    /// Moves straight to a level (e.g. after "Meet the sound"), clearing the streaks.
    public mutating func raise(to newLevel: Int) {
        guard newLevel > level else { return }
        level = newLevel
        correctStreak = 0
        missStreak = 0
    }
}

/// Where a child is with one sound. Each stage has its own activity.
public enum SoundStage: Int, Codable, Sendable, CaseIterable, Comparable {
    /// Not met yet → "Meet the sound".
    case new = 0
    /// Met → "Sound hunt".
    case met = 1
    /// Recognises it in pictures → "Pop the letter".
    case recognises = 2
    /// Knows it → review only.
    case mastered = 3

    public static func < (lhs: SoundStage, rhs: SoundStage) -> Bool { lhs.rawValue < rhs.rawValue }
}

/// Mastery for every sound a child has worked on, keyed by sound id.
public struct MasteryTracker: Codable, Equatable, Sendable {
    public private(set) var skills: [String: SkillMastery]

    public init(skills: [String: SkillMastery] = [:]) {
        self.skills = skills
    }

    public func mastery(of soundID: String) -> SkillMastery {
        skills[soundID] ?? SkillMastery()
    }

    public func stage(of sound: PhonicsSound) -> SoundStage {
        let level = min(max(mastery(of: sound.id).level, 0), SoundStage.mastered.rawValue)
        return SoundStage(rawValue: level) ?? .new
    }

    /// Records one answered question about a sound. A miss at "met" drops back to "new",
    /// so the child hears the sound introduced again.
    @discardableResult
    public mutating func record(correct: Bool, for sound: PhonicsSound) -> MasteryChange {
        var mastery = mastery(of: sound.id)
        let change = mastery.record(correct: correct, maxLevel: SoundStage.mastered.rawValue)
        skills[sound.id] = mastery
        return change
    }

    /// Called when "Meet the sound" finishes.
    public mutating func markMet(_ sound: PhonicsSound) {
        var mastery = mastery(of: sound.id)
        mastery.raise(to: SoundStage.met.rawValue)
        skills[sound.id] = mastery
    }
}
