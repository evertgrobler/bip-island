/// What a parent sees about one child: time played, answers this week, each skill's status by
/// island, every phonics sound's stage, recent games, and what needs a bit more practice.
/// Built from the child's progress and the content, so it is never stored and never stale.
public struct ProgressReport: Sendable {
    public enum SkillStatus: String, Sendable, CaseIterable {
        /// Its prerequisites aren't known yet.
        case locked
        /// Below the child's starting band: counted as known, no need to practise it.
        case knownByAge
        /// Open but not tried yet.
        case ready
        /// Being practised.
        case learning
        /// Mastered (80% of the last 10 on 2 different days).
        case mastered
        /// Mastered, and due for a spaced review today.
        case reviewDue
    }

    public struct SkillLine: Sendable, Identifiable {
        public let id: String
        public let name: String
        public let island: Island
        public let band: Band
        public let status: SkillStatus
        /// Right answers among the most recent ones (up to the mastery window).
        public let recentRight: Int
        public let recentTotal: Int
        public let totalAttempts: Int
        public let masteredOnDay: Int?

        /// 0…100, or nil with no answers yet.
        public var recentPercent: Int? {
            recentTotal > 0 ? Int((Double(recentRight) * 100 / Double(recentTotal)).rounded()) : nil
        }
    }

    public struct IslandSummary: Sendable, Identifiable {
        public let island: Island
        public let skills: [SkillLine]
        public var id: Island { island }
        /// Mastered or known by age, out of all skills on the island.
        public var doneCount: Int { skills.filter { [SkillStatus.mastered, .reviewDue, .knownByAge].contains($0.status) }.count }
    }

    public struct SoundLine: Sendable, Identifiable {
        public let id: String
        public let grapheme: String
        public let stage: SoundStage
    }

    public struct PhonicsGroupLine: Sendable, Identifiable {
        public let number: Int
        public let sounds: [SoundLine]
        public let status: SkillStatus
        public var id: Int { number }
    }

    public let stars: Int
    public let stickersEarned: Int
    public let stickersTotal: Int
    public let minutesToday: Int
    public let minutesThisWeek: Int
    public let daysPlayedThisWeek: Int
    public let answersThisWeek: Int
    public let rightThisWeek: Int
    public let islands: [IslandSummary]
    public let phonics: [PhonicsGroupLine]
    /// Game names, most recent first, no repeats.
    public let recentGames: [String]
    /// Skills being practised where recent answers are mostly wrong.
    public let needsPractice: [SkillLine]
    public let dueForReview: [SkillLine]

    /// The days the week covers, today included.
    public static let weekDays = 7
    /// A skill needs practice below this recent percentage…
    public static let needsPracticeBelowPercent = 60
    /// …once it has at least this many recent answers.
    public static let needsPracticeAfterAnswers = 5
    static let recentGamesShown = 6

    public init(content: ContentLibrary, progress: ChildProgress, startingBand: Band, today: Int) {
        let rules = content.masteryRules
        let lookup = { (id: String) in content.skill(id: id) }

        func status(of skill: Skill) -> SkillStatus {
            let record = progress.skill(skill.id)
            if record.isMastered {
                return record.isDueForReview(on: today, rules: rules) ? .reviewDue : .mastered
            }
            if progress.isTreatedAsKnown(skill, startingBand: startingBand) && record.totalAttempts == 0 { return .knownByAge }
            if !progress.isUnlocked(skill, startingBand: startingBand, lookup: lookup) { return .locked }
            return record.totalAttempts == 0 ? .ready : .learning
        }

        let lines = content.skills.skills.map { skill -> SkillLine in
            let record = progress.skill(skill.id)
            let window = record.recent.suffix(max(1, rules.masteredWindow))
            return SkillLine(id: skill.id, name: skill.name, island: skill.island, band: skill.band, status: status(of: skill),
                             recentRight: window.filter(\.correct).count, recentTotal: window.count,
                             totalAttempts: record.totalAttempts, masteredOnDay: record.masteredOnDay)
        }
        islands = Island.allCases.map { island in IslandSummary(island: island, skills: lines.filter { $0.island == island }) }

        let byID = Dictionary(lines.map { ($0.id, $0) }, uniquingKeysWith: { first, _ in first })
        phonics = PhonicsCourse(content).groups.map { group in
            PhonicsGroupLine(number: group.number,
                             sounds: group.sounds.map { SoundLine(id: $0.id, grapheme: $0.grapheme, stage: progress.sounds.stage(of: $0)) },
                             status: byID[group.skillID]?.status ?? .locked)
        }

        stars = progress.stars
        stickersEarned = StickerBook.earned(progress: progress, content: content).count
        stickersTotal = StickerBook.allStickers(content: content).count
        minutesToday = progress.secondsPlayed(lastDays: 1, endingOn: today) / 60
        minutesThisWeek = progress.secondsPlayed(lastDays: Self.weekDays, endingOn: today) / 60
        daysPlayedThisWeek = progress.daysPlayed(lastDays: Self.weekDays, endingOn: today)

        let weekAnswers = progress.skills.values.flatMap(\.recent).filter { $0.day > today - Self.weekDays && $0.day <= today }
        answersThisWeek = weekAnswers.count
        rightThisWeek = weekAnswers.filter(\.correct).count

        var seen = Set<String>()
        recentGames = progress.recentGames.reversed().compactMap { id -> String? in
            guard !seen.contains(id) else { return nil }
            seen.insert(id)
            return content.game(id: id)?.name
        }.prefix(Self.recentGamesShown).map { $0 }

        needsPractice = lines.filter { line in
            line.status == .learning && line.recentTotal >= Self.needsPracticeAfterAnswers
                && (line.recentPercent ?? 100) < Self.needsPracticeBelowPercent
        }
        dueForReview = lines.filter { $0.status == .reviewDue }
    }
}
