using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>Bip's suggestions on Letters Island, worked out from the content and a child's progress.</summary>
public sealed class LessonPlannerTests
{
    private readonly MasteryRules _rules = TestContent.Rules;
    private const int Today = 1_000;

    private static LettersProgress Letters(ChildProgress progress, Band band = Band.Foundation)
    {
        var content = TestContent.Library();
        return new LettersProgress(content, new PhonicsCourse(content), progress, band);
    }

    private static LessonPlanner Planner(ChildProgress progress, Band band = Band.Foundation, Func<PhonicsSound, bool>? canHunt = null) =>
        new(Letters(progress, band), canHunt ?? (_ => true));

    private (ActivityKind Kind, string Sound) Next(ChildProgress progress, Func<PhonicsSound, bool>? canHunt = null)
    {
        var activity = Planner(progress, canHunt: canHunt).NextActivity(Today, _rules, new SeededGenerator(1));
        return (activity.Kind, activity.Sound.Id);
    }

    // Progress helpers.
    private static void Met(string[] ids, ChildProgress progress)
    {
        foreach (var id in ids) progress.MarkMet(id);
    }

    private void Raise(string id, SoundStage stage, ChildProgress progress)
    {
        progress.MarkMet(id);
        while (progress.Sounds.Stage(id) < stage) progress.RecordAnswer(true, "practice", id, Today, _rules);
    }

    private void MasterSkill(string id, ChildProgress progress, int finishingOn)
    {
        for (var i = 0; i < 10; i++) progress.RecordAnswer(true, id, null, i < 5 ? finishingOn - 1 : finishingOn, _rules);
    }

    private static readonly string[] GroupOne = ["s", "a", "t", "p", "i", "n"];

    [Fact]
    public void AFreshChildMeetsTheFirstSound()
    {
        var state = Letters(new ChildProgress());
        Assert.Equal([1], state.UnlockedGroups);
        Assert.Equal(1, state.CurrentGroup.Number);
        Assert.Empty(state.KnownSoundIds);
        var (kind, sound) = Next(new ChildProgress());
        Assert.Equal(ActivityKind.MeetTheSound, kind);
        Assert.Equal("s", sound);
    }

    [Fact]
    public void TwoSoundsAreMetBeforePractice()
    {
        var progress = new ChildProgress();
        Met(["s"], progress);
        Assert.Equal(ActivityKind.MeetTheSound, Next(progress).Kind);
        Assert.Equal("a", Next(progress).Sound);

        Met(["a"], progress);
        var (kind, sound) = Next(progress);
        Assert.Equal(ActivityKind.SoundHunt, kind); // two sounds are waiting: practise before meeting more
        Assert.Equal("s", sound);
    }

    [Fact]
    public void RecognisedSoundsMoveOnToBubblePop()
    {
        var progress = new ChildProgress();
        Raise("s", SoundStage.Recognises, progress);
        Met(["a", "t"], progress);
        var (kind, sound) = Next(progress);
        Assert.Equal(ActivityKind.BubblePop, kind);
        Assert.Equal("s", sound);
    }

    [Fact]
    public void ASoundNoPictureStartsWithGoesStraightToBubblePop()
    {
        var progress = new ChildProgress();
        Met(["s", "a"], progress);
        Assert.Equal(ActivityKind.BubblePop, Next(progress, canHunt: s => s.Id != "s").Kind);
    }

    [Fact]
    public void AMasteredSoundMovesOnToTheNextInOrder()
    {
        var progress = new ChildProgress();
        Raise("s", SoundStage.Mastered, progress);
        Assert.Equal("a", Next(progress).Sound);
        Assert.Equal("a", Planner(progress).SuggestedSound()?.Id);
    }

    [Fact]
    public void AWholeGroupLearntButNotYetMasteredMeansReview()
    {
        var progress = new ChildProgress();
        foreach (var id in GroupOne) Raise(id, SoundStage.Mastered, progress);
        var state = Letters(progress);
        Assert.Equal([1], state.UnlockedGroups); // group 2 waits until snd_g1 is mastered over two days
        Assert.Equal(1, state.CurrentGroup.Number);
        Assert.Null(Planner(progress).SuggestedSound());

        var kinds = new HashSet<ActivityKind>();
        var rng = new SeededGenerator(42);
        var planner = Planner(progress);
        for (var i = 0; i < 50; i++) kinds.Add(planner.NextActivity(Today, _rules, rng).Kind);
        Assert.Equal(new HashSet<ActivityKind> { ActivityKind.SoundHunt, ActivityKind.BubblePop }, kinds);
    }

    [Fact]
    public void MasteringAGroupOpensTheNext()
    {
        var progress = new ChildProgress();
        foreach (var id in GroupOne) Raise(id, SoundStage.Mastered, progress);
        MasterSkill("snd_g1", progress, Today);
        var state = Letters(progress);
        Assert.Equal([1, 2], state.UnlockedGroups);
        Assert.Equal(2, state.CurrentGroup.Number);
        Assert.Equal(2, state.Learner().UnlockedPhonicsGroup);
        Assert.Equal("m", Next(progress).Sound);
    }

    [Fact]
    public void AGroupDueForReviewComesFirst()
    {
        var progress = new ChildProgress();
        foreach (var id in GroupOne) Raise(id, SoundStage.Mastered, progress);
        MasterSkill("snd_g1", progress, Today - 2);
        Assert.True(progress.IsDueForReview("snd_g1", Today, _rules));
        var (kind, sound) = Next(progress);
        Assert.NotEqual(ActivityKind.MeetTheSound, kind); // a review, not the next new sound (m)
        Assert.Contains(sound, GroupOne);
    }

    [Fact]
    public void ASixYearOldStartsAtGroupFive()
    {
        var state = Letters(new ChildProgress(), Band.Stage1);
        Assert.Equal([1, 2, 3, 4, 5], state.UnlockedGroups);
        Assert.Equal(5, state.CurrentGroup.Number);
        Assert.True(state.KnownSoundIds.IsSupersetOf(["s", "m", "ck", "h"]), "earlier groups count as known");
        Assert.DoesNotContain("j", state.KnownSoundIds);
        var activity = Planner(new ChildProgress(), Band.Stage1).NextActivity(Today, _rules, new SeededGenerator(1));
        Assert.Equal(new PlannedActivity(ActivityKind.MeetTheSound, state.Course.Sound("j")!), activity);
    }

    [Fact]
    public void TheLearnerCarriesTheFocus()
    {
        var progress = new ChildProgress();
        Met(["s"], progress);
        var state = Letters(progress);
        var learner = state.Learner(state.Course.Sound("s"));
        Assert.Equal("s", learner.FocusSoundId);
        Assert.Equal(new HashSet<string> { "s" }, learner.KnownSoundIds);
        Assert.Equal(1, learner.UnlockedPhonicsGroup);
    }

    [Fact]
    public void ActivitiesMatchTheGameRegistry()
    {
        var content = TestContent.Library();
        foreach (var kind in ActivityKindExtensions.All) Assert.True(content.Game(kind.GameId()) is not null, kind.GameId());
        Assert.Equal(MeetTheSoundGame.GameId, ActivityKind.MeetTheSound.GameId());
        Assert.Equal(SoundHuntGame.GameId, ActivityKind.SoundHunt.GameId());
        Assert.Equal(BubblePopGame.GameId, ActivityKind.BubblePop.GameId());
    }
}
