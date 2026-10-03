namespace BipCore;

/// <summary>
/// The kid lock's rules, without any operating-system calls, so they can be tested anywhere:
/// which keys a child can't use to leave the game on Windows, and which Mac presentation options
/// the full-screen lock asks for (the same set as the Swift app's KidLock).
/// </summary>
public static class KidLockRules
{
    // Windows virtual-key codes.
    public const int VkTab = 0x09;
    public const int VkEscape = 0x1B;
    public const int VkLeftWindows = 0x5B;
    public const int VkRightWindows = 0x5C;
    public const int VkF4 = 0x73;

    /// <summary>
    /// True for the Windows keys that switch away from or close the game: the Windows key (Start
    /// menu and every Windows-key shortcut), Alt-Tab, Alt-Esc, Ctrl-Esc (Start) and Alt-F4.
    /// Ctrl-Alt-Del can't be blocked by any app, which is right: a grown-up can always get out.
    /// </summary>
    public static bool BlocksWindowsKey(int virtualKey, bool altDown, bool ctrlDown) => virtualKey switch
    {
        VkLeftWindows or VkRightWindows => true,
        VkTab => altDown,
        VkEscape => altDown || ctrlDown,
        VkF4 => altDown,
        _ => false,
    };

    // NSApplication.PresentationOptions bits (AppKit).
    public const ulong MacHideDock = 1 << 1;
    public const ulong MacHideMenuBar = 1 << 3;
    public const ulong MacDisableAppleMenu = 1 << 4;
    public const ulong MacDisableProcessSwitching = 1 << 5;
    public const ulong MacDisableForceQuit = 1 << 6;
    public const ulong MacDisableHideApplication = 1 << 8;

    /// <summary>
    /// The Mac lock: no Dock, no menu bar, no Cmd-Tab, no Force Quit, no Cmd-H, no Apple menu.
    /// AppKit only accepts the "disable" options together with a hidden Dock and menu bar, which
    /// this set always includes (an invalid set would throw inside AppKit and crash the game).
    /// </summary>
    public const ulong MacLockOptions = MacHideDock | MacHideMenuBar | MacDisableAppleMenu
                                        | MacDisableProcessSwitching | MacDisableForceQuit | MacDisableHideApplication;

    /// <summary>True when every lock option is set (for the self-test report).</summary>
    public static bool MacLockIsOn(ulong presentationOptions) => (presentationOptions & MacLockOptions) == MacLockOptions;

    /// <summary>NSEvent.ModifierFlags option key: held at launch, the game opens unlocked (parent mode).</summary>
    public const ulong MacOptionKeyFlag = 1 << 19;
}
