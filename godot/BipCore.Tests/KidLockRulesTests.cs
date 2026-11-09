using BipCore;
using Xunit;

namespace BipCore.Tests;

public sealed class KidLockRulesTests
{
    [Theory]
    [InlineData(KidLockRules.VkLeftWindows, false, false)]
    [InlineData(KidLockRules.VkRightWindows, false, false)]
    [InlineData(KidLockRules.VkTab, true, false)]      // Alt-Tab
    [InlineData(KidLockRules.VkEscape, true, false)]   // Alt-Esc
    [InlineData(KidLockRules.VkEscape, false, true)]   // Ctrl-Esc (Start)
    [InlineData(KidLockRules.VkF4, true, false)]       // Alt-F4
    public void KeysThatLeaveTheGameAreBlocked(int key, bool alt, bool ctrl) =>
        Assert.True(KidLockRules.BlocksWindowsKey(key, alt, ctrl));

    [Theory]
    [InlineData(KidLockRules.VkEscape, false, false)]  // Esc alone: the parent gate needs it
    [InlineData(KidLockRules.VkTab, false, false)]
    [InlineData(0x41, false, false)]                   // A: replays the sound
    [InlineData(0x0D, false, false)]                   // Enter: chooses
    [InlineData(0x25, false, false)]                   // Left arrow
    [InlineData(0x31, false, false)]                   // 1
    [InlineData(KidLockRules.VkF4, false, false)]
    public void PlayKeysStillWork(int key, bool alt, bool ctrl) =>
        Assert.False(KidLockRules.BlocksWindowsKey(key, alt, ctrl));

    [Fact]
    public void TheMacLockMatchesTheSwiftApp()
    {
        // NSApplication.PresentationOptions: hideDock 2, hideMenuBar 8, disableAppleMenu 16,
        // disableProcessSwitching 32, disableForceQuit 64, disableHideApplication 256.
        Assert.Equal(2UL + 8 + 16 + 32 + 64 + 256, KidLockRules.MacLockOptions);
        Assert.True(KidLockRules.MacLockIsOn(KidLockRules.MacLockOptions | 1UL << 10));
        Assert.False(KidLockRules.MacLockIsOn(KidLockRules.MacHideDock | KidLockRules.MacHideMenuBar));
        Assert.Equal(0x80000UL, KidLockRules.MacOptionKeyFlag);
    }

    [Fact]
    public void TheMacFenceLeavesAMouseInsideAlone()
    {
        Assert.Null(KidLockRules.FencePoint(0, 0, 0, 0, 1440, 900));
        Assert.Null(KidLockRules.FencePoint(1439, 899, 0, 0, 1440, 900));
        Assert.Null(KidLockRules.FencePoint(700, 400, 0, 0, 1440, 900));
    }

    [Theory]
    [InlineData(1440, 300, 1439, 300)]   // onto a second screen on the right
    [InlineData(-5, 300, 0, 300)]        // or on the left
    [InlineData(2000, -40, 1439, 0)]     // up and over, a corner
    [InlineData(500, 1200, 500, 899)]    // a screen below
    public void TheMacFenceBringsTheMouseBackToTheNearestPointInside(int x, int y, int backX, int backY) =>
        Assert.Equal((backX, backY), KidLockRules.FencePoint(x, y, 0, 0, 1440, 900));

    [Fact]
    public void TheMacFenceWorksForAWindowAwayFromTheOrigin() =>
        Assert.Equal((100, 249), KidLockRules.FencePoint(50, 400, 100, 50, 800, 200));

    private static readonly (int, int, int, int) MacBook = (0, 0, 1728, 1117);
    private static readonly (int, int, int, int) Monitor = (1728, 0, 1920, 1080);

    [Fact]
    public void TheCoverGoesOnTheOtherScreenNeverTheGames()
    {
        // The owner's setup: the MacBook (main, the game on it) and a monitor to its right.
        Assert.Equal([1], KidLockRules.ScreensToCover(MacBook, [MacBook, Monitor], main: 0));
    }

    [Fact]
    public void TheMainScreenIsNeverCoveredEvenIfTheWindowLooksElsewhere()
    {
        // A window reading that hasn't caught up yet (still on the monitor): neither screen gets a cover.
        Assert.Empty(KidLockRules.ScreensToCover(Monitor, [MacBook, Monitor], main: 0));
    }

    [Fact]
    public void AScreenTheWindowOnlyGrazesStillGetsItsCover()
    {
        var window = (0, 0, 1729, 1117); // one pixel over the edge
        Assert.Equal([1], KidLockRules.ScreensToCover(window, [MacBook, Monitor], main: 0));
    }

    [Fact]
    public void ScreensAboveAndToTheLeftAreCoveredToo()
    {
        var left = (-1920, -200, 1920, 1080);
        var above = (0, -1080, 1920, 1080);
        Assert.Equal([1, 2], KidLockRules.ScreensToCover(MacBook, [MacBook, left, above], main: 0));
    }

    [Fact]
    public void OneScreenNeedsNoCoverAndAnEmptyScreenIsSkipped()
    {
        Assert.Empty(KidLockRules.ScreensToCover(MacBook, [MacBook], main: 0));
        Assert.Empty(KidLockRules.ScreensToCover(MacBook, [MacBook, (1728, 0, 0, 0)], main: 0));
    }

    [Fact]
    public void OverlapCountsSharedPixels()
    {
        Assert.Equal(0, KidLockRules.Overlap(MacBook, Monitor));
        Assert.Equal(1080, KidLockRules.Overlap((0, 0, 1729, 1117), Monitor)); // one column of the monitor's 1080 rows
    }

    [Fact]
    public void ACoverHidesTheGameOnlyWhenItCoversARealShareOfIt()
    {
        Assert.False(KidLockRules.CoverHidesGame(Monitor, (0, 0, 1729, 1117)));     // grazes one column
        Assert.True(KidLockRules.CoverHidesGame(MacBook, MacBook));                  // the 0.2.256 bug
        Assert.False(KidLockRules.CoverHidesGame(Monitor, MacBook));                 // side by side
    }

    [Fact]
    public void TheGameScreenIsTheOneTheWindowOverlapsMost()
    {
        Assert.Equal(0, KidLockRules.GameScreen(MacBook, [MacBook, Monitor]));
        Assert.Equal(1, KidLockRules.GameScreen(Monitor, [MacBook, Monitor]));
        Assert.Equal(-1, KidLockRules.GameScreen((5000, 5000, 10, 10), [MacBook, Monitor]));
    }
}
