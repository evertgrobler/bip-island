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

    /// <summary>
    /// Which screens the Mac's paper covers go on: every screen except the main one (where the lock puts the game)
    /// and the one the game's window overlaps most. Rectangles are (x, y, width, height) in screen pixels. A screen
    /// the window only grazes (a pixel of rounding, a moment of rearranging) still gets its cover. Owner's MacBook
    /// with a second screen, 0.2.256 (9 Nov 2026): the cover landed on the game's own screen.
    /// </summary>
    public static List<int> ScreensToCover((int X, int Y, int W, int H) window,
                                           IReadOnlyList<(int X, int Y, int W, int H)> screens, int main)
    {
        var game = -1;
        long most = 0;
        for (var i = 0; i < screens.Count; i++)
        {
            var area = Overlap(window, screens[i]);
            if (area > most) (most, game) = (area, i);
        }
        var cover = new List<int>();
        for (var i = 0; i < screens.Count; i++)
            if (i != main && i != game && screens[i].W > 0 && screens[i].H > 0) cover.Add(i);
        return cover;
    }

    /// <summary>How many pixels two (x, y, width, height) rectangles share.</summary>
    public static long Overlap((int X, int Y, int W, int H) a, (int X, int Y, int W, int H) b)
    {
        long w = Math.Min(a.X + a.W, b.X + b.W) - Math.Max(a.X, b.X);
        long h = Math.Min(a.Y + a.H, b.Y + b.H) - Math.Max(a.Y, b.Y);
        return w > 0 && h > 0 ? w * h : 0;
    }

    /// <summary>
    /// The Mac's mouse fence: where a mouse at (x, y) goes back to, inside the game window at (left, top) of
    /// width by height (screen pixels), or null when it is already inside and must be left alone.
    /// </summary>
    public static (int X, int Y)? FencePoint(int x, int y, int left, int top, int width, int height)
    {
        if (width <= 0 || height <= 0) return null;
        if (x >= left && x < left + width && y >= top && y < top + height) return null;
        return (Math.Clamp(x, left, left + width - 1), Math.Clamp(y, top, top + height - 1));
    }
}
