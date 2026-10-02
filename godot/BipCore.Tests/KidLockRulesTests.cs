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
}
