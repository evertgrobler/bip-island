namespace BipCore;

public enum SkillStatus
{
    /// <summary>Its prerequisites aren't known yet.</summary>
    Locked,
    /// <summary>Below the child's starting band: counted as known, no need to practise it.</summary>
    KnownByAge,
    /// <summary>Open but not tried yet.</summary>
    Ready,
    /// <summary>Being practised.</summary>
    Learning,
    /// <summary>Mastered (80% of the last 10 on 2 different days).</summary>
    Mastered,
    /// <summary>Mastered, and due for a spaced review today.</summary>
    ReviewDue,
}

/// <summary>
/// What a parent sees about one child: time played, answers this week, each skill's status by
/// island, every phonics sound's stage, recent games, and what needs a bit more practice.
/// Built from the child's progress and the content, so it is never stored and never stale.
/// </summary>
public sealed class ProgressReport
{
    public sealed record SkillLine(string Id, string Name, Island Island, Band Band, SkillStatus Status,
                                   int RecentRight, int RecentTotal, int TotalAttempts, int? MasteredOnDay)
    {
        /// <summary>0…100, or null with no answers yet.</summary>
        public int? RecentPercent =>
            RecentTotal > 0 ? (int)Math.Round(RecentRight * 100.0 / RecentTotal, MidpointRounding.AwayFromZero) : null;
    }

    public sealed record IslandSummary(Island Island, IReadOnlyList<SkillLine> Skills)
    {
        /// <summary>Mastered or known by age, out of all skills on the island.</summary>
        public int DoneCount => Skills.Count(s => s.Status is SkillStatus.Mastered or SkillStatus.ReviewDue or SkillStatus.KnownByAge);
    }

    public sealed record SoundLine(string Id, string Grapheme, SoundStage Stage);

    public sealed record PhonicsGroupLine(int Number, IReadOnlyList<SoundLine> Sounds, SkillStatus Status);

    public int Stars { get; }
    public int StickersEarned { get; }
    public int StickersTotal { get; }
    public int MinutesToday { get; }
    public int MinutesThisWeek { get; }
    public int DaysPlayedThisWeek { get; }
    public int AnswersThisWeek { get; }
    public int RightThisWeek { get; }
    public IReadOnlyList<IslandSummary> Islands { get; }
    public IReadOnlyList<PhonicsGroupLine> Phonics { get; }
    /// <summary>Game names, most recent first, no repeats.</summary>
    public IReadOnlyList<string> RecentGames { get; }
    /// <summary>Skills being practised where recent answers are mostly wrong.</summary>
    public IReadOnlyList<SkillLine> NeedsPractice { get; }
    public IReadOnlyList<SkillLine> DueForReview { get; }

    /// <summary>The days the week covers, today included.</summary>
    public const int WeekDays = 7;
    /// <summary>A skill needs practice below this recent percentage…</summary>
    public const int NeedsPracticeBelowPercent = 60;
    /// <summary>…once it has at least this many recent answers.</summary>
    public const int NeedsPracticeAfterAnswers = 5;
    public const int RecentGamesShown = 6;

    public ProgressReport(ContentLibrary content, ChildProgress progress, Band startingBand, int today)
    {
        var rules = content.MasteryRules;

        SkillStatus StatusOf(Skill skill)
        {
            var record = progress.Skill(skill.Id);
            if (record.IsMastered) return record.IsDueForReview(today, rules) ? SkillStatus.ReviewDue : SkillStatus.Mastered;
            if (progress.IsTreatedAsKnown(skill, startingBand) && record.TotalAttempts == 0) return SkillStatus.KnownByAge;
            if (!progress.IsUnlocked(skill, startingBand, content.Skill)) return SkillStatus.Locked;
            return record.TotalAttempts == 0 ? SkillStatus.Ready : SkillStatus.Learning;
        }

        var lines = content.Skills.Skills.Select(skill =>
        {
            var record = progress.Skill(skill.Id);
            var window = record.Recent.TakeLast(Math.Max(1, rules.MasteredWindow)).ToList();
            return new SkillLine(skill.Id, skill.Name, skill.Island, skill.Band, StatusOf(skill),
                                 window.Count(a => a.Correct), window.Count, record.TotalAttempts, record.MasteredOnDay);
        }).ToList();
        Islands = IslandExtensions.All.Select(island => new IslandSummary(island, lines.Where(l => l.Island == island).ToList())).ToList();

        var byId = new Dictionary<string, SkillLine>();
        foreach (var line in lines) byId.TryAdd(line.Id, line);
        Phonics = new PhonicsCourse(content).Groups.Select(group => new PhonicsGroupLine(
            group.Number,
            group.Sounds.Select(s => new SoundLine(s.Id, s.Grapheme, progress.Sounds.Stage(s))).ToList(),
            byId.TryGetValue(group.SkillId, out var found) ? found.Status : SkillStatus.Locked)).ToList();

        Stars = progress.Stars;
        StickersEarned = StickerBook.Earned(progress, content).Count;
        StickersTotal = StickerBook.AllStickers(content).Count;
        MinutesToday = progress.SecondsPlayed(1, today) / 60;
        MinutesThisWeek = progress.SecondsPlayed(WeekDays, today) / 60;
        DaysPlayedThisWeek = progress.DaysPlayed(WeekDays, today);

        var weekAnswers = progress.Skills.Values.SelectMany(r => r.Recent)
            .Where(a => a.Day > today - WeekDays && a.Day <= today).ToList();
        AnswersThisWeek = weekAnswers.Count;
        RightThisWeek = weekAnswers.Count(a => a.Correct);

        var seen = new HashSet<string>();
        var recent = new List<string>();
        foreach (var id in Enumerable.Reverse(progress.RecentGames))
        {
            if (!seen.Add(id)) continue;
            if (content.Game(id) is GameEntry game) recent.Add(game.Name);
        }
        RecentGames = recent.Take(RecentGamesShown).ToList();

        NeedsPractice = lines.Where(line =>
            line.Status == SkillStatus.Learning && line.RecentTotal >= NeedsPracticeAfterAnswers
            && (line.RecentPercent ?? 100) < NeedsPracticeBelowPercent).ToList();
        DueForReview = lines.Where(l => l.Status == SkillStatus.ReviewDue).ToList();
    }
}
