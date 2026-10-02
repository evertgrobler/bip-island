using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>
/// Per-child, per-skill progress: mastery (80% of the last 10, on 2 different days), reviews after
/// 2, 5 and 14 days, unlocking by prerequisites, and saved progress that always loads.
/// </summary>
public sealed class ProgressTests
{
    private readonly MasteryRules _rules = TestContent.Rules;

    // MARK: Mastery

    [Fact]
    public void TenRightOnOneDayIsNotMastery()
    {
        var record = new SkillRecord();
        for (var i = 0; i < 10; i++) record.Record(true, 100, _rules);
        Assert.False(record.IsMastered); // one lucky session doesn't count
    }

    [Fact]
    public void TenRightOverTwoDaysIsMastery()
    {
        var record = new SkillRecord();
        for (var i = 0; i < 5; i++) record.Record(true, 100, _rules);
        for (var i = 0; i < 5; i++)
        {
            var newlyMastered = record.Record(true, 101, _rules);
            Assert.Equal(i == 4, newlyMastered); // mastered on the tenth answer
        }
        Assert.True(record.IsMastered);
        Assert.Equal(101, record.MasteredOnDay);
    }

    [Fact]
    public void EightOutOfTenIsEnoughButSevenIsNot()
    {
        var eight = new SkillRecord();
        for (var i = 0; i < 10; i++) eight.Record(i >= 2, 100 + i / 5, _rules);
        Assert.True(eight.IsMastered);

        var seven = new SkillRecord();
        for (var i = 0; i < 10; i++) seven.Record(i >= 3, 100 + i / 5, _rules);
        Assert.False(seven.IsMastered);
    }

    [Fact]
    public void FewerThanTenAnswersIsNeverMastery()
    {
        var record = new SkillRecord();
        for (var i = 0; i < 9; i++) record.Record(true, 100 + i, _rules);
        Assert.False(record.IsMastered);
    }

    [Fact]
    public void OnlyTheLastTenAnswersCount()
    {
        var record = new SkillRecord();
        for (var i = 0; i < 10; i++) record.Record(false, 100, _rules);
        Assert.False(record.IsMastered);
        for (var i = 0; i < 10; i++) record.Record(true, 101 + i / 5, _rules);
        Assert.True(record.IsMastered, "early mistakes are forgotten once there are ten newer answers");
        Assert.Equal(20, record.TotalAttempts);
    }

    [Fact]
    public void MasteryStaysOnceEarned()
    {
        var record = MasteredRecord(100);
        for (var i = 0; i < 10; i++) record.Record(false, 101, _rules);
        Assert.True(record.IsMastered);
    }

    // MARK: Review

    [Fact]
    public void MasteredSkillsComeBackAfterTwoFiveAndFourteenDays()
    {
        var record = MasteredRecord(100);
        Assert.False(record.IsDueForReview(101, _rules));
        Assert.True(record.IsDueForReview(102, _rules), "2 days after mastery");

        record.Record(true, 102, _rules);
        Assert.Equal(1, record.ReviewsDone);
        Assert.False(record.IsDueForReview(106, _rules));
        Assert.True(record.IsDueForReview(107, _rules), "5 days after the first review");

        record.Record(true, 107, _rules);
        Assert.False(record.IsDueForReview(120, _rules));
        Assert.True(record.IsDueForReview(121, _rules), "14 days after the second review");

        record.Record(true, 121, _rules);
        Assert.Equal(3, record.ReviewsDone);
        Assert.False(record.IsDueForReview(500, _rules), "no more reviews after the last one");
    }

    [Fact]
    public void UnmasteredSkillsAreNeverDueForReview()
    {
        var record = new SkillRecord();
        record.Record(true, 100, _rules);
        Assert.False(record.IsDueForReview(200, _rules));
    }

    // MARK: Unlocking by the skill map

    [Fact]
    public void PhonicsGroupsUnlockInOrder()
    {
        var content = TestContent.Library();
        var g1 = content.Skill("snd_g1")!;
        var g2 = content.Skill("snd_g2")!;
        var progress = new ChildProgress();
        Assert.True(progress.IsUnlocked(g1, Band.Foundation, content.Skill));
        Assert.False(progress.IsUnlocked(g2, Band.Foundation, content.Skill));

        Master("snd_g1", progress);
        Assert.True(progress.IsUnlocked(g2, Band.Foundation, content.Skill));
    }

    [Fact]
    public void OlderChildrenStartAtTheirBand()
    {
        var content = TestContent.Library();
        var progress = new ChildProgress();
        var g4 = content.Skill("snd_g4")!;
        var g5 = content.Skill("snd_g5")!;
        var g6 = content.Skill("snd_g6")!;
        Assert.True(progress.IsTreatedAsKnown(g4, Band.Stage1), "foundation sounds count as known for a 6-year-old");
        Assert.True(progress.IsUnlocked(g5, Band.Stage1, content.Skill));
        Assert.False(progress.IsUnlocked(g6, Band.Stage1, content.Skill), "but stage 1 still goes in order");
        Assert.False(progress.IsUnlocked(g5, Band.Foundation, content.Skill));
    }

    [Fact]
    public void AnUnknownPrerequisiteNeverUnlocks()
    {
        var skill = BipJson.Decode<Skill>(
            """{"id": "x", "island": "letters", "band": "foundation", "name": "X", "objectives": [], "prerequisites": ["nope"]}""");
        Assert.False(new ChildProgress().IsUnlocked(skill, Band.Stage3, _ => null));
    }

    // MARK: Recording answers

    [Fact]
    public void AnAnswerCountsForTheSkillAndTheSound()
    {
        var progress = new ChildProgress();
        progress.MarkMet("s");
        var change = MasteryChange.None;
        for (var i = 0; i < 3; i++) change = progress.RecordAnswer(true, "snd_g1", "s", 100, _rules);
        Assert.Equal(MasteryChange.LevelledUp((int)SoundStage.Recognises), change);
        Assert.Equal(SoundStage.Recognises, progress.Sounds.Stage("s"));
        Assert.Equal(3, progress.Skill("snd_g1").TotalAttempts);
        Assert.Equal(0, progress.Skill("snd_g2").TotalAttempts);
    }

    [Fact]
    public void RecentGamesAreKeptShort()
    {
        var progress = new ChildProgress();
        for (var i = 0; i < 30; i++) progress.NotePlayed($"game_{i}");
        Assert.Equal(20, progress.RecentGames.Count);
        Assert.Equal("game_29", progress.RecentGames[^1]);
    }

    // MARK: Saved progress

    [Fact]
    public void ProgressSurvivesSaving()
    {
        var progress = new ChildProgress();
        progress.MarkMet("s");
        Master("snd_g1", progress);
        progress.NotePlayed("sound_hunt");
        Assert.Equal(progress, ChildProgress.Decode(progress.Encode()));
    }

    [Fact]
    public void EmptyOrPartialSavesStillLoad()
    {
        Assert.Equal(new ChildProgress(), ChildProgress.Decode("{}"));

        // An older save with only sound levels, and a newer save with a field this version doesn't know.
        var old = ChildProgress.Decode("""
            {"sounds": {"skills": {"s": {"level": 2, "correctStreak": 1, "missStreak": 0}}}, "fromTheFuture": true,
             "skills": {"snd_g1": {"recent": [{"correct": true, "day": 5}]}}}
            """);
        Assert.Equal(SoundStage.Recognises, old.Sounds.Stage("s"));
        Assert.Equal(1, old.Skill("snd_g1").TotalAttempts);
        Assert.Equal(ChildProgress.CurrentVersion, old.Version);
    }

    // MARK: Days

    [Fact]
    public void DayNumbersCountCalendarDays()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");
        var morning = new DateTimeOffset(2026, 10, 1, 7, 0, 0, TimeSpan.FromHours(2));
        var evening = new DateTimeOffset(2026, 10, 1, 23, 59, 0, TimeSpan.FromHours(2));
        var tomorrow = new DateTimeOffset(2026, 10, 2, 0, 1, 0, TimeSpan.FromHours(2));
        Assert.Equal(DayNumber.Of(morning, zone), DayNumber.Of(evening, zone));
        Assert.Equal(DayNumber.Of(morning, zone) + 1, DayNumber.Of(tomorrow, zone));
    }

    /// <summary>The same day numbers as the Swift app (days since 1 January 2001), so saved days line up.</summary>
    [Fact]
    public void DayNumbersMatchTheSwiftApp()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");
        Assert.Equal(0, DayNumber.Of(new DateTimeOffset(2001, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), zone));
        // 1 October 2026 is 9,404 days after 1 January 2001.
        Assert.Equal(9404, DayNumber.Of(new DateTimeOffset(2026, 10, 1, 15, 0, 0, TimeSpan.FromHours(2)), zone));
    }

    // MARK: Helpers

    private SkillRecord MasteredRecord(int day)
    {
        var record = new SkillRecord();
        for (var i = 0; i < 5; i++) record.Record(true, day - 1, _rules);
        for (var i = 0; i < 5; i++) record.Record(true, day, _rules);
        Assert.Equal(day, record.MasteredOnDay);
        return record;
    }

    private void Master(string skillId, ChildProgress progress)
    {
        for (var i = 0; i < 10; i++) progress.RecordAnswer(true, skillId, null, 100 + i / 5, _rules);
        Assert.True(progress.IsMastered(skillId));
    }
}
