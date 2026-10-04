using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BipCore;

/// <summary>
/// A calendar day as a whole number: days since 1 January 2001 on this computer's calendar
/// (the same numbering as the Swift app, so saved progress carries over). Mastery needs answers on
/// different days, and reviews come back after a number of days.
/// </summary>
public static class DayNumber
{
    private static readonly DateTimeOffset Reference = new(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static DateOnly LocalDate(DateTimeOffset moment, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(moment, zone).DateTime);

    /// <summary>The day a moment falls on, in a time zone (the computer's own by default).</summary>
    public static int Of(DateTimeOffset moment, TimeZoneInfo? zone = null)
    {
        zone ??= TimeZoneInfo.Local;
        return LocalDate(moment, zone).DayNumber - LocalDate(Reference, zone).DayNumber;
    }

    /// <summary>The start of a numbered day (the reverse of <see cref="Of"/>).</summary>
    public static DateTimeOffset Date(int day, TimeZoneInfo? zone = null)
    {
        zone ??= TimeZoneInfo.Local;
        var local = LocalDate(Reference, zone).AddDays(day).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }
}

/// <summary>One answered question, kept for the mastery check.</summary>
public sealed record Attempt(bool Correct, int Day);

/// <summary>A child's record for one skill from skills.json: recent answers, when it was mastered, and reviews.</summary>
public sealed class SkillRecord
{
    public const int AttemptsKept = 50;

    private int? _totalAttempts;

    /// <summary>Most recent last. Only the newest few are kept.</summary>
    [JsonInclude] public List<Attempt> Recent { get; private set; } = [];

    /// <summary>Every answer ever given. Older saves without it count the recent ones.</summary>
    [JsonInclude]
    public int TotalAttempts
    {
        get => _totalAttempts ?? Recent.Count;
        private set => _totalAttempts = value;
    }

    [JsonInclude] public int? MasteredOnDay { get; private set; }
    [JsonInclude] public int ReviewsDone { get; private set; }
    [JsonInclude] public int? LastReviewDay { get; private set; }

    [JsonIgnore] public bool IsMastered => MasteredOnDay is not null;

    /// <summary>
    /// Mastered = at least masteredPercent% right over the last masteredWindow attempts,
    /// spread over at least masteredDistinctDays different days, so one lucky session doesn't count.
    /// </summary>
    public bool MeetsMasteryRule(MasteryRules rules)
    {
        if (rules.MasteredWindow <= 0 || Recent.Count < rules.MasteredWindow) return false;
        var window = Recent.TakeLast(rules.MasteredWindow).ToList();
        var right = window.Count(a => a.Correct);
        var days = window.Select(a => a.Day).Distinct().Count();
        return right * 100 >= rules.MasteredPercent * rules.MasteredWindow && days >= rules.MasteredDistinctDays;
    }

    /// <summary>
    /// A mastered skill comes back after 2, then 5, then 14 days (from skills.json), counted from
    /// mastery or from the last review.
    /// </summary>
    public bool IsDueForReview(int day, MasteryRules rules)
    {
        if (MasteredOnDay is not int mastered || ReviewsDone >= rules.ReviewAfterDays.Count) return false;
        var since = LastReviewDay ?? mastered;
        return day >= since + rules.ReviewAfterDays[ReviewsDone];
    }

    /// <summary>Records one answer. Returns true when this answer made the skill mastered.</summary>
    public bool Record(bool correct, int day, MasteryRules rules)
    {
        if (IsDueForReview(day, rules))
        {
            ReviewsDone += 1;
            LastReviewDay = day;
        }
        var total = TotalAttempts;
        Recent.Add(new Attempt(correct, day));
        if (Recent.Count > AttemptsKept) Recent.RemoveRange(0, Recent.Count - AttemptsKept);
        TotalAttempts = total + 1;
        if (MasteredOnDay is null && MeetsMasteryRule(rules))
        {
            MasteredOnDay = day;
            return true;
        }
        return false;
    }

    public SkillRecord Copy() => new()
    {
        Recent = [.. Recent],
        _totalAttempts = _totalAttempts,
        MasteredOnDay = MasteredOnDay,
        ReviewsDone = ReviewsDone,
        LastReviewDay = LastReviewDay,
    };
}

/// <summary>
/// Everything one child has learnt: a level for each sound, and a record for each skill.
/// Saved as JSON (the same shape the Swift app writes), so new fields can be added without losing
/// progress: anything missing loads as empty.
/// </summary>
public sealed class ChildProgress : IEquatable<ChildProgress>
{
    public const int CurrentVersion = 2;
    public const int RecentGamesKept = 20;
    /// <summary>Stars in Bip's jar: one per right answer. Every 10 earn a sticker.</summary>
    public const int StarsPerSticker = 10;
    /// <summary>Bonus stars from Bip's mystery box, once a day.</summary>
    public const int MysteryBonusStars = 5;
    public const int PlayDaysKept = 60;

    [JsonInclude] public int Version { get; private set; } = CurrentVersion;
    /// <summary>Sound stages (new → met → recognises → mastered), keyed by sound id.</summary>
    [JsonInclude] public MasteryTracker Sounds { get; private set; } = new();
    /// <summary>Skill records, keyed by skill id from skills.json.</summary>
    [JsonInclude] public Dictionary<string, SkillRecord> Skills { get; private set; } = [];
    /// <summary>Game ids, most recent last, for Bip's suggestions.</summary>
    [JsonInclude] public List<string> RecentGames { get; private set; } = [];
    /// <summary>Stars in Bip's jar.</summary>
    [JsonInclude] public int Stars { get; private set; }
    /// <summary>The day Bip's mystery box was last opened (null if never).</summary>
    [JsonInclude] public int? LastMysteryDay { get; private set; }
    /// <summary>
    /// Where the play-time break stood in the oldest saves. The break now lives on the child's save
    /// row (<see cref="SavedChild.Break"/>); this is only read to carry it over.
    /// </summary>
    [JsonInclude] public BreakState? Breaks { get; private set; }
    /// <summary>Seconds played, by day number, for the parent progress view. Only recent days are kept.</summary>
    [JsonInclude] public Dictionary<int, int> PlaySecondsByDay { get; private set; } = [];
    /// <summary>The child's level in each game (games.json levels), keyed by game id.</summary>
    [JsonInclude] public Dictionary<string, SkillMastery> GameLevels { get; private set; } = [];

    public ChildProgress() { }

    public ChildProgress(MasteryTracker sounds) => Sounds = sounds;

    /// <summary>Reads saved progress; anything missing loads as empty.</summary>
    public static ChildProgress Decode(string json) => BipJson.Decode<ChildProgress>(json);

    public string Encode() => BipJson.Encode(this);

    /// <summary>A copy of a skill's record (a fresh one if it hasn't been practised).</summary>
    public SkillRecord Skill(string id) => Skills.TryGetValue(id, out var record) ? record.Copy() : new SkillRecord();

    public bool IsMastered(string skillId) => Skills.TryGetValue(skillId, out var record) && record.IsMastered;

    public bool IsDueForReview(string skillId, int day, MasteryRules rules) =>
        Skills.TryGetValue(skillId, out var record) && record.IsDueForReview(day, rules);

    /// <summary>
    /// Skills below the child's starting band count as known: a 6-year-old starting at stage 1
    /// isn't made to work through every foundation sound first.
    /// </summary>
    public bool IsTreatedAsKnown(Skill skill, Band startingBand) => skill.Band < startingBand;

    /// <summary>Counts as known for unlocking other skills: mastered, or below the starting band.</summary>
    public bool IsKnown(Skill skill, Band startingBand) => IsMastered(skill.Id) || IsTreatedAsKnown(skill, startingBand);

    /// <summary>A skill unlocks once every prerequisite is known. Unknown prerequisite ids never unlock.</summary>
    public bool IsUnlocked(Skill skill, Band startingBand, Func<string, Skill?> lookup) =>
        skill.Prerequisites.All(id => lookup(id) is Skill prerequisite && IsKnown(prerequisite, startingBand));

    /// <summary>
    /// Records one answered question: against the skill (for mastery and review) and, if given,
    /// against the sound (for its stage). A right answer also drops a star into Bip's jar.
    /// Returns the sound's level change.
    /// </summary>
    public MasteryChange RecordAnswer(bool correct, string skillId, string? soundId, int day, MasteryRules rules)
    {
        var record = Skill(skillId);
        record.Record(correct, day, rules);
        Skills[skillId] = record;
        if (correct) Stars += 1;
        return soundId is null ? MasteryChange.None : Sounds.Record(correct, soundId, rules);
    }

    public void MarkMet(string soundId) => Sounds.MarkMet(soundId);

    public void NotePlayed(string gameId)
    {
        RecentGames.Add(gameId);
        if (RecentGames.Count > RecentGamesKept) RecentGames.RemoveRange(0, RecentGames.Count - RecentGamesKept);
    }

    /// <summary>Bip's mystery box: once a day, bonus stars. Returns true when the box was full.</summary>
    public bool ClaimMysteryBox(int day)
    {
        if (LastMysteryDay == day) return false;
        LastMysteryDay = day;
        Stars += MysteryBonusStars;
        return true;
    }

    /// <summary>Records play-time break progress. See <see cref="PlayBreaks"/>.</summary>
    public void SetBreaks(BreakState state) => Breaks = state;

    /// <summary>The child's level in a game, or <paramref name="start"/> if they haven't played it yet.</summary>
    public int GameLevel(string gameId, int start) =>
        GameLevels.TryGetValue(gameId, out var mastery) ? mastery.Level : start;

    /// <summary>
    /// One answer in a game: right answers in a row move the game up a level, misses drop it back
    /// (the same rules as everything else). Returns the change.
    /// </summary>
    public MasteryChange RecordGameAnswer(bool correct, string gameId, int start, int levelCount, MasteryRules rules)
    {
        if (levelCount <= 1) return MasteryChange.None;
        var mastery = GameLevels.TryGetValue(gameId, out var found)
            ? found with { }
            : new SkillMastery(Math.Clamp(start, 0, levelCount - 1));
        var change = mastery.Record(correct, rules, maxLevel: levelCount - 1);
        GameLevels[gameId] = mastery;
        return change;
    }

    /// <summary>
    /// Adds play time to a day, for the parent progress view. Days older than
    /// <see cref="PlayDaysKept"/> before this one are dropped.
    /// </summary>
    public void NotePlayTime(int seconds, int day)
    {
        if (seconds <= 0) return;
        PlaySecondsByDay[day] = PlaySecondsByDay.GetValueOrDefault(day) + seconds;
        foreach (var old in PlaySecondsByDay.Keys.Where(d => d <= day - PlayDaysKept).ToList()) PlaySecondsByDay.Remove(old);
    }

    /// <summary>Seconds played over the <paramref name="days"/> days ending on <paramref name="day"/> (today counts as one).</summary>
    public int SecondsPlayed(int days, int day) =>
        PlaySecondsByDay.Where(p => p.Key > day - days && p.Key <= day).Sum(p => p.Value);

    /// <summary>Different days with any play in the <paramref name="days"/> days ending on <paramref name="day"/>.</summary>
    public int DaysPlayed(int days, int day) =>
        PlaySecondsByDay.Count(p => p.Key > day - days && p.Key <= day && p.Value > 0);

    /// <summary>Two progress records are equal when they save to the same JSON (key order aside).</summary>
    public bool Equals(ChildProgress? other) =>
        other is not null && JsonNode.DeepEquals(JsonNode.Parse(Encode()), JsonNode.Parse(other.Encode()));

    public override bool Equals(object? obj) => Equals(obj as ChildProgress);
    public override int GetHashCode() => Stars;
}
