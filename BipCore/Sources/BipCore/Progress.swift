import Foundation

/// A calendar day as a whole number (days since 1 January 2001 on this Mac's calendar).
/// Mastery needs answers on different days, and reviews come back after a number of days.
public enum DayNumber {
    public static func of(_ date: Date, calendar: Calendar = .current) -> Int {
        let start = calendar.startOfDay(for: Date(timeIntervalSinceReferenceDate: 0))
        return calendar.dateComponents([.day], from: start, to: calendar.startOfDay(for: date)).day ?? 0
    }
}

/// One answered question, kept for the mastery check.
public struct Attempt: Codable, Equatable, Sendable {
    public let correct: Bool
    public let day: Int

    public init(correct: Bool, day: Int) {
        self.correct = correct
        self.day = day
    }
}

/// A child's record for one skill from skills.json: recent answers, when it was mastered, and reviews.
public struct SkillRecord: Codable, Equatable, Sendable {
    /// Most recent last. Only the newest few are kept.
    public private(set) var recent: [Attempt]
    public private(set) var totalAttempts: Int
    public private(set) var masteredOnDay: Int?
    public private(set) var reviewsDone: Int
    public private(set) var lastReviewDay: Int?

    static let attemptsKept = 50

    public init() {
        recent = []
        totalAttempts = 0
        masteredOnDay = nil
        reviewsDone = 0
        lastReviewDay = nil
    }

    public var isMastered: Bool { masteredOnDay != nil }

    /// Mastered = at least `masteredPercent`% right over the last `masteredWindow` attempts,
    /// spread over at least `masteredDistinctDays` different days, so one lucky session doesn't count.
    public func meetsMasteryRule(_ rules: MasteryRules) -> Bool {
        guard rules.masteredWindow > 0, recent.count >= rules.masteredWindow else { return false }
        let window = recent.suffix(rules.masteredWindow)
        let right = window.filter(\.correct).count
        let days = Set(window.map(\.day)).count
        return right * 100 >= rules.masteredPercent * rules.masteredWindow && days >= rules.masteredDistinctDays
    }

    /// A mastered skill comes back after 2, then 5, then 14 days (from skills.json), counted from
    /// mastery or from the last review.
    public func isDueForReview(on day: Int, rules: MasteryRules) -> Bool {
        guard let masteredOnDay, reviewsDone < rules.reviewAfterDays.count else { return false }
        let since = lastReviewDay ?? masteredOnDay
        return day >= since + rules.reviewAfterDays[reviewsDone]
    }

    /// Records one answer. Returns true when this answer made the skill mastered.
    @discardableResult
    public mutating func record(correct: Bool, on day: Int, rules: MasteryRules) -> Bool {
        if isDueForReview(on: day, rules: rules) {
            reviewsDone += 1
            lastReviewDay = day
        }
        recent.append(Attempt(correct: correct, day: day))
        if recent.count > Self.attemptsKept {
            recent.removeFirst(recent.count - Self.attemptsKept)
        }
        totalAttempts += 1
        if masteredOnDay == nil && meetsMasteryRule(rules) {
            masteredOnDay = day
            return true
        }
        return false
    }

    // Missing keys decode as empty, so progress saved by an older version always loads.
    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        recent = try c.decodeIfPresent([Attempt].self, forKey: .recent) ?? []
        totalAttempts = try c.decodeIfPresent(Int.self, forKey: .totalAttempts) ?? recent.count
        masteredOnDay = try c.decodeIfPresent(Int.self, forKey: .masteredOnDay)
        reviewsDone = try c.decodeIfPresent(Int.self, forKey: .reviewsDone) ?? 0
        lastReviewDay = try c.decodeIfPresent(Int.self, forKey: .lastReviewDay)
    }
}

/// Everything one child has learnt: a level for each sound, and a record for each skill.
/// Saved as JSON inside the child's profile, so new fields can be added without losing progress.
public struct ChildProgress: Codable, Equatable, Sendable {
    public static let currentVersion = 1
    static let recentGamesKept = 20

    public private(set) var version: Int
    /// Sound stages (new → met → recognises → mastered), keyed by sound id.
    public private(set) var sounds: MasteryTracker
    /// Skill records, keyed by skill id from skills.json.
    public private(set) var skills: [String: SkillRecord]
    /// Game ids, most recent last, for Bip's suggestions.
    public private(set) var recentGames: [String]

    public init(sounds: MasteryTracker = MasteryTracker()) {
        version = Self.currentVersion
        self.sounds = sounds
        skills = [:]
        recentGames = []
    }

    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        version = try c.decodeIfPresent(Int.self, forKey: .version) ?? Self.currentVersion
        sounds = try c.decodeIfPresent(MasteryTracker.self, forKey: .sounds) ?? MasteryTracker()
        skills = try c.decodeIfPresent([String: SkillRecord].self, forKey: .skills) ?? [:]
        recentGames = try c.decodeIfPresent([String].self, forKey: .recentGames) ?? []
    }

    public func skill(_ id: String) -> SkillRecord {
        skills[id] ?? SkillRecord()
    }

    public func isMastered(_ skillID: String) -> Bool {
        skill(skillID).isMastered
    }

    public func isDueForReview(_ skillID: String, on day: Int, rules: MasteryRules) -> Bool {
        skill(skillID).isDueForReview(on: day, rules: rules)
    }

    /// Skills below the child's starting band count as known: a 6-year-old starting at stage 1
    /// isn't made to work through every foundation sound first.
    public func isTreatedAsKnown(_ skill: Skill, startingBand: Band) -> Bool {
        skill.band < startingBand
    }

    /// Counts as known for unlocking other skills: mastered, or below the starting band.
    public func isKnown(_ skill: Skill, startingBand: Band) -> Bool {
        isMastered(skill.id) || isTreatedAsKnown(skill, startingBand: startingBand)
    }

    /// A skill unlocks once every prerequisite is known. Unknown prerequisite ids never unlock.
    public func isUnlocked(_ skill: Skill, startingBand: Band, lookup: (String) -> Skill?) -> Bool {
        skill.prerequisites.allSatisfy { id in
            guard let prerequisite = lookup(id) else { return false }
            return isKnown(prerequisite, startingBand: startingBand)
        }
    }

    /// Records one answered question: against the skill (for mastery and review) and, if given,
    /// against the sound (for its stage). Returns the sound's level change.
    @discardableResult
    public mutating func recordAnswer(correct: Bool, skillID: String, soundID: String?, day: Int, rules: MasteryRules) -> MasteryChange {
        var record = skill(skillID)
        record.record(correct: correct, on: day, rules: rules)
        skills[skillID] = record
        guard let soundID else { return .none }
        return sounds.record(correct: correct, for: soundID, rules: rules)
    }

    public mutating func markMet(soundID: String) {
        sounds.markMet(soundID)
    }

    public mutating func notePlayed(gameID: String) {
        recentGames.append(gameID)
        if recentGames.count > Self.recentGamesKept {
            recentGames.removeFirst(recentGames.count - Self.recentGamesKept)
        }
    }
}
