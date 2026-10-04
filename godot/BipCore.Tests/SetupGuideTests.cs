using BipCore;
using Xunit;

namespace BipCore.Tests;

public sealed class SetupGuideTests
{
    [Fact]
    public void StepsGoInOrderAndFinishAfterAllSet()
    {
        var guide = new SetupGuide();
        Assert.Equal(SetupGuide.Step.Welcome, guide.Current);
        Assert.True(guide.Next());
        Assert.Equal(SetupGuide.Step.GrownUpArea, guide.Current);
        guide.GateOpened();
        Assert.True(guide.Next());
        Assert.Equal(SetupGuide.Step.Passcode, guide.Current);
        guide.Next();
        Assert.Equal(SetupGuide.Step.Children, guide.Current);
        guide.Next();
        Assert.Equal(SetupGuide.Step.PlayTime, guide.Current);
        guide.Next();
        Assert.Equal(SetupGuide.Step.Tour, guide.Current);
        for (var i = 0; i < SetupGuide.TourCards; i++) guide.Next();
        Assert.Equal(SetupGuide.Step.AllSet, guide.Current);
        Assert.False(guide.Done);
        Assert.True(guide.Next());
        Assert.True(guide.Done);
        Assert.False(guide.Next());
    }

    [Fact]
    public void OnAFirstRunThePractiseStepWaitsForTheGate()
    {
        // Nobody has passed the gate yet: the parent controls after this step stay behind it.
        var fresh = new SetupGuide();
        fresh.GateOpened(); // on Welcome: doesn't count
        fresh.Next();
        Assert.False(fresh.CanGoOn);
        Assert.False(fresh.Next());
        Assert.False(fresh.CanSkipStep);
        Assert.False(fresh.SkipStep());
        Assert.Equal(SetupGuide.Step.GrownUpArea, fresh.Current);
        fresh.GateOpened();
        Assert.True(fresh.Next());
        Assert.Equal(SetupGuide.Step.Passcode, fresh.Current);
    }

    [Fact]
    public void FromSettingsThePractiseStepCanBeSkipped()
    {
        var again = new SetupGuide(gatedAlready: true);
        again.Next();
        Assert.False(again.CanGoOn);
        Assert.True(again.CanSkipStep);
        Assert.True(again.SkipStep());
        Assert.Equal(SetupGuide.Step.Passcode, again.Current);
    }

    [Fact]
    public void BackWalksTheTourCardsAndStopsAtWelcome()
    {
        var guide = new SetupGuide();
        Assert.False(guide.Back());
        guide.Next();
        guide.GateOpened();
        for (var i = 0; i < 4; i++) guide.Next(); // to the tour
        guide.Next(); // tour card 2
        Assert.Equal(1, guide.TourCard);
        guide.Back();
        Assert.Equal(0, guide.TourCard);
        guide.Back();
        Assert.Equal(SetupGuide.Step.PlayTime, guide.Current);
        guide.Next();
        guide.Next();
        guide.Next();
        guide.Next();
        Assert.Equal(SetupGuide.Step.AllSet, guide.Current);
        guide.Back();
        Assert.Equal(SetupGuide.Step.Tour, guide.Current);
        Assert.Equal(SetupGuide.TourCards - 1, guide.TourCard);
    }

    [Fact]
    public void OnlyThePractiseThePasscodeAndTheTourCanBeSkipped()
    {
        var guide = new SetupGuide(gatedAlready: true);
        Assert.False(guide.SkipStep()); // Welcome
        guide.Next();
        Assert.True(guide.SkipStep()); // GrownUpArea → Passcode
        Assert.True(guide.SkipStep()); // Passcode → Children
        Assert.False(guide.SkipStep()); // Children must be seen
    }

    [Fact]
    public void SetupDoneIsSavedAndOldSavesHaveNotSeenTheGuide()
    {
        var folder = Path.Combine(Path.GetTempPath(), "bip-setup-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            // A save written before the guide existed has no SetupDone: it reads as false.
            File.WriteAllText(Path.Combine(folder, SaveStore.FileName), "{\"Version\":1,\"Children\":[]}");
            var store = new SaveStore(folder);
            Assert.False(store.SetupDone);
            store.MarkSetupDone();
            Assert.True(new SaveStore(folder).SetupDone);
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    [Fact]
    public void AFirstRunCutShortStillHasTheGuideToCome()
    {
        var folder = Path.Combine(Path.GetTempPath(), "bip-setup-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            Assert.False(new SaveStore(folder).SetupDone);
            // The first run wrote "Player 1", then the computer restarted before the guide was done.
            Assert.False(new SaveStore(folder).SetupDone);
        }
        finally { Directory.Delete(folder, recursive: true); }
    }
}
