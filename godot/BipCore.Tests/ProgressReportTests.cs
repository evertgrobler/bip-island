using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>The parent progress view and the profile rules.</summary>
public sealed class ProgressReportTests
{
    private readonly MasteryRules _rules = TestContent.Rules;
    private const int Today = 1_000;

    private static ProgressReport Report(ChildProgress progress, Band band = Band.Foundation) =>
        new(TestContent.Library(), progress, band, Today);

    private static ProgressReport.SkillLine Line(string id, ProgressReport report) =>
        report.Islands.SelectMany(i => i.Skills).FirstOrDefault(s => s.Id == id) ?? throw new InvalidOperationException(id);

    [Fact]
    public void AFreshChild()
    {
        var r = Report(new ChildProgress());
        Assert.Equal(0, r.Stars);
        Assert.Equal(0, r.AnswersThisWeek);
        Assert.Equal(0, r.MinutesThisWeek);
        Assert.Equal(IslandExtensions.All, r.Islands.Select(i => i.Island));
        Assert.Equal(SkillStatus.Ready, Line("snd_g1", r).Status);
        Assert.Equal(SkillStatus.Locked, Line("snd_g2", r).Status);
        Assert.Equal(9, r.Phonics.Count);
        Assert.True(r.Phonics[0].Sounds.All(s => s.Stage == SoundStage.New));
        Assert.True(r.StickersTotal > 0);
        Assert.Empty(r.NeedsPractice);
    }

    [Fact]
    public void OlderChildrenSeeEarlierSkillsAsKnown()
    {
        var r = Report(new ChildProgress(), Band.Stage1);
        Assert.Equal(SkillStatus.KnownByAge, Line("snd_g1", r).Status);
        Assert.Equal(SkillStatus.Ready, Line("snd_g5", r).Status);
    }

    [Fact]
    public void LearningMasteredAndReview()
    {
        var progress = new ChildProgress();
        progress.MarkMet("s");
        for (var i = 0; i < 10; i++) progress.RecordAnswer(true, "snd_g1", "s", Today - 3 + i / 5, _rules);
        // Mastered two days ago: the first review (after 2 days) is due today.
        var mastered = Line("snd_g1", Report(progress));
        Assert.Equal(SkillStatus.ReviewDue, mastered.Status);
        Assert.Equal(Today - 2, mastered.MasteredOnDay);
        Assert.Equal(100, mastered.RecentPercent);
        Assert.Equal(["snd_g1"], Report(progress).DueForReview.Select(l => l.Id));
        Assert.Equal(SkillStatus.Ready, Line("snd_g2", Report(progress)).Status); // mastering group 1 opens group 2

        progress.RecordAnswer(false, "snd_g2", null, Today, _rules);
        var learning = Line("snd_g2", Report(progress));
        Assert.Equal(SkillStatus.Learning, learning.Status);
        Assert.Equal(0, learning.RecentPercent);
    }

    [Fact]
    public void SkillsThatNeedMorePractice()
    {
        var progress = new ChildProgress();
        for (var i = 0; i < 6; i++) progress.RecordAnswer(i == 0, "snd_g1", null, Today, _rules);
        var r = Report(progress);
        Assert.Equal(["snd_g1"], r.NeedsPractice.Select(l => l.Id));
        Assert.Equal(6, r.AnswersThisWeek);
        Assert.Equal(1, r.RightThisWeek);
        Assert.Equal(1, r.Stars);
    }

    [Fact]
    public void OnlyThisWeeksAnswersCount()
    {
        var progress = new ChildProgress();
        progress.RecordAnswer(true, "snd_g1", null, Today - 7, _rules);
        progress.RecordAnswer(true, "snd_g1", null, Today - 6, _rules);
        Assert.Equal(1, Report(progress).AnswersThisWeek);
    }

    [Fact]
    public void SoundStagesShow()
    {
        var progress = new ChildProgress();
        progress.MarkMet("a");
        var sounds = Report(progress).Phonics[0].Sounds;
        Assert.Equal(["s", "a", "t", "p", "i", "n"], sounds.Select(s => s.Grapheme));
        Assert.Equal(SoundStage.Met, sounds[1].Stage);
    }

    [Fact]
    public void RecentGamesAreNamedNewestFirstWithoutRepeats()
    {
        var progress = new ChildProgress();
        foreach (var id in new[] { "meet_the_sound", "sound_hunt", "meet_the_sound", "not_a_game" }) progress.NotePlayed(id);
        Assert.Equal(["Meet the Sound", "Sound Hunt"], Report(progress).RecentGames);
    }

    // MARK: Play time

    [Fact]
    public void PlayTimeByDay()
    {
        var progress = new ChildProgress();
        progress.NotePlayTime(600, Today);
        progress.NotePlayTime(300, Today);
        progress.NotePlayTime(1_200, Today - 3);
        progress.NotePlayTime(999, Today - 7);
        progress.NotePlayTime(-5, Today);
        Assert.Equal(900, progress.SecondsPlayed(1, Today));
        Assert.Equal(2_100, progress.SecondsPlayed(7, Today));
        Assert.Equal(2, progress.DaysPlayed(7, Today));
        var r = Report(progress);
        Assert.Equal(15, r.MinutesToday);
        Assert.Equal(35, r.MinutesThisWeek);
        Assert.Equal(2, r.DaysPlayedThisWeek);
    }

    [Fact]
    public void OldPlayTimeIsDropped()
    {
        var progress = new ChildProgress();
        progress.NotePlayTime(60, 10);
        progress.NotePlayTime(60, 500);
        Assert.Equal([500], progress.PlaySecondsByDay.Keys.Order());
    }

    [Fact]
    public void PlayTimeSurvivesSavingAndOldSavesLoad()
    {
        var progress = new ChildProgress();
        progress.NotePlayTime(120, Today);
        Assert.Equal(progress, ChildProgress.Decode(progress.Encode()));
        var old = ChildProgress.Decode("""{"stars": 3}""");
        Assert.Empty(old.PlaySecondsByDay);
        Assert.Equal(3, old.Stars);
    }

    [Fact]
    public void DayNumbersTurnBackIntoDates()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");
        var day = DayNumber.Of(new DateTimeOffset(2026, 10, 1, 15, 0, 0, TimeSpan.FromHours(2)), zone);
        var start = DayNumber.Date(day, zone);
        Assert.Equal(day, DayNumber.Of(start, zone));
        Assert.Equal(1, TimeZoneInfo.ConvertTime(start, zone).Day);
    }

    // MARK: Profiles

    [Fact]
    public void NamesAreTidied()
    {
        Assert.Equal("Lerato Mokoena", ProfileRules.CleanName("  Lerato   Mokoena "));
        Assert.Null(ProfileRules.CleanName("   "));
        Assert.Equal(ProfileRules.MaxNameLength, ProfileRules.CleanName(new string('a', 40))?.Length);
    }

    [Fact]
    public void AvatarsAndTheFourChildLimit()
    {
        Assert.Equal("lion", ProfileRules.FreeAvatar([]));
        Assert.Equal("tortoise", ProfileRules.FreeAvatar(["lion", "penguin"]));
        Assert.Equal("lion", ProfileRules.FreeAvatar(ProfileRules.Avatars));
        Assert.Equal("lion", ProfileRules.ValidAvatar("dragon"));
        Assert.Equal("lion", ProfileRules.ValidAvatar(null));
        Assert.Equal("crab", ProfileRules.ValidAvatar("crab"));
        Assert.True(ProfileRules.CanAdd(3));
        Assert.False(ProfileRules.CanAdd(4));
        Assert.True(ProfileRules.Avatars.Count >= ProfileRules.MaxChildren);
    }
}
