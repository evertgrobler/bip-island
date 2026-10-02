using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>Within a game: 3 right in a row moves up a level, 2 misses in a row drops back.</summary>
public sealed class MasteryTests
{
    private readonly MasteryRules _rules = TestContent.Rules;

    [Fact]
    public void ThreeRightInARowMovesUp()
    {
        var m = new SkillMastery(level: 1);
        Assert.Equal(MasteryChange.None, m.Record(true, _rules, maxLevel: 3));
        Assert.Equal(MasteryChange.None, m.Record(true, _rules, maxLevel: 3));
        Assert.Equal(MasteryChange.LevelledUp(2), m.Record(true, _rules, maxLevel: 3));
        Assert.Equal(2, m.Level);
        Assert.Equal(0, m.CorrectStreak); // streak restarts at the new level
    }

    [Fact]
    public void AMissBreaksTheCorrectStreak()
    {
        var m = new SkillMastery(level: 1);
        m.Record(true, _rules, maxLevel: 3);
        m.Record(true, _rules, maxLevel: 3);
        m.Record(false, _rules, maxLevel: 3);
        Assert.Equal(MasteryChange.None, m.Record(true, _rules, maxLevel: 3));
        Assert.Equal(1, m.Level);
    }

    [Fact]
    public void TwoMissesInARowDropBack()
    {
        var m = new SkillMastery(level: 2);
        Assert.Equal(MasteryChange.None, m.Record(false, _rules, maxLevel: 3));
        Assert.Equal(MasteryChange.DroppedBack(1), m.Record(false, _rules, maxLevel: 3));
        Assert.Equal(0, m.MissStreak);
    }

    [Fact]
    public void MissesSeparatedByARightAnswerDoNotDropBack()
    {
        var m = new SkillMastery(level: 2);
        m.Record(false, _rules, maxLevel: 3);
        m.Record(true, _rules, maxLevel: 3);
        Assert.Equal(MasteryChange.None, m.Record(false, _rules, maxLevel: 3));
        Assert.Equal(2, m.Level);
    }

    [Fact]
    public void LevelStaysWithinBounds()
    {
        var top = new SkillMastery(level: 3);
        for (var i = 0; i < 6; i++) Assert.Equal(MasteryChange.None, top.Record(true, _rules, maxLevel: 3));
        Assert.Equal(3, top.Level);

        var bottom = new SkillMastery(level: 0);
        for (var i = 0; i < 6; i++) Assert.Equal(MasteryChange.None, bottom.Record(false, _rules, maxLevel: 3));
        Assert.Equal(0, bottom.Level);
    }

    [Fact]
    public void TheNumbersComeFromTheRules()
    {
        var quick = TestContent.Rules with { CorrectInARowToMoveUp = 1, MissesInARowToDropBack = 1, ReviewAfterDays = [2] };
        var m = new SkillMastery(level: 1);
        Assert.Equal(MasteryChange.LevelledUp(2), m.Record(true, quick, maxLevel: 3));
        Assert.Equal(MasteryChange.DroppedBack(1), m.Record(false, quick, maxLevel: 3));
    }

    [Fact]
    public void SoundStages()
    {
        var tracker = new MasteryTracker();
        Assert.Equal(SoundStage.New, tracker.Stage("s"));
        tracker.MarkMet("s");
        Assert.Equal(SoundStage.Met, tracker.Stage("s"));
        for (var i = 0; i < 3; i++) tracker.Record(true, "s", _rules);
        Assert.Equal(SoundStage.Recognises, tracker.Stage("s"));
        for (var i = 0; i < 3; i++) tracker.Record(true, "s", _rules);
        Assert.Equal(SoundStage.Mastered, tracker.Stage("s"));
    }

    [Fact]
    public void TwoMissesAfterMeetingGoesBackToMeetTheSound()
    {
        var tracker = new MasteryTracker();
        tracker.MarkMet("s");
        tracker.Record(false, "s", _rules);
        Assert.Equal(MasteryChange.DroppedBack(0), tracker.Record(false, "s", _rules));
        Assert.Equal(SoundStage.New, tracker.Stage("s"));
    }

    [Fact]
    public void MarkMetNeverLowersALevel()
    {
        var tracker = new MasteryTracker(new Dictionary<string, SkillMastery> { ["s"] = new SkillMastery(level: 3) });
        tracker.MarkMet("s");
        Assert.Equal(SoundStage.Mastered, tracker.Stage("s"));
    }

    [Fact]
    public void TrackerSurvivesEncoding()
    {
        var tracker = new MasteryTracker();
        tracker.MarkMet("s");
        tracker.Record(true, "s", _rules);
        Assert.Equal(tracker, BipJson.Decode<MasteryTracker>(BipJson.Encode(tracker)));
    }
}
