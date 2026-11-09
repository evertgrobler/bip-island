using System;
using System.Collections.Generic;
using BipIsland.Drawing;
using System.Runtime.InteropServices;
using BipCore;
using Godot;

namespace BipIsland.App;

/// <summary>
/// The kid lock (the Swift app's KidLock), for the Godot version:
/// - Mac: a borderless window covering the main screen, then the same presentation options as the
///   Swift app (no Dock, no menu bar, no Cmd-Tab, no Force Quit, no Cmd-H, no Apple menu). Logging
///   out, restarting and shutting down are never blocked: the game quits when the Mac says it's
///   about to power off.
/// - Windows: exclusive full screen, plus a keyboard hook that swallows the Windows key, Alt-Tab,
///   Alt-Esc and Ctrl-Esc while the game is in front. Ctrl-Alt-Del always works.
/// - Both: while the game is in front, the mouse stays inside its window, so on a computer with a second
///   screen a child can't move it over there and click another program (which would also take the game
///   out of the front, where the Windows keyboard hook stops blocking). If another program takes the
///   front by itself (a system dialog, an updater), the limit lifts until the game is back in front.
///   On a Mac every other screen is also covered by a plain paper window of the game's own, so even a
///   flick that gets past the mouse fence for a moment lands on the game, never on another program.
/// - Hold Option (Mac) or Alt (Windows) while the game opens for parent mode: a normal window, no lock.
/// Cmd-Q, Alt-F4 and the close button are handled in <see cref="Boot"/> (only the parent area quits).
/// Every operating-system call is guarded: if one fails, the game logs it and plays unlocked.
/// </summary>
public static class KidLock
{
    /// <summary>"mac", "windows", "parent mode", "off" (other systems, tests) or "failed: …".</summary>
    public static string Status { get; private set; } = "off";
    public static bool IsOn => Status is "mac" or "windows";

    private static bool IsMac => OS.GetName() == "macOS";
    private static bool IsWindows => OS.GetName() == "Windows";

    /// <summary>Locks the game in, or opens a normal window in parent mode. Call once, from Boot._Ready.</summary>
    public static void Install(bool windowedRequested)
    {
        if (DisplayServer.GetName() == "headless" || windowedRequested || !(IsMac || IsWindows))
        {
            Status = "off";
            return;
        }
        if (ParentModeKeyHeld())
        {
            Status = "parent mode";
            OpenParentWindow();
            return;
        }
        try
        {
            if (IsMac) Mac.Lock();
            else Windows.Lock();
            Status = IsMac ? "mac" : "windows";
            KeepMouseIn();
        }
        catch (Exception error)
        {
            Status = "failed: " + error.Message;
            GD.PrintErr($"Bip Island: the child lock couldn't start, so the game plays unlocked: {error.Message}");
            // Still fill the screen, as a child expects, and keep the mouse on it: that needs neither the
            // keyboard hook nor the Mac options.
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            KeepMouseIn();
        }
    }

    /// <summary>
    /// Another program took the front by itself (a system dialog, an updater): the covers step aside until the
    /// game is back (<see cref="Reapply"/>), so that program's windows on another screen can be seen.
    /// </summary>
    public static void Lifted()
    {
        foreach (var cover in Covers)
            if (GodotObject.IsInstanceValid(cover)) cover.Hide();
    }

    /// <summary>The game came back to the front: put the Mac options and the mouse limit back (the system can drop them).</summary>
    public static void Reapply()
    {
        if (!IsOn && !Status.StartsWith("failed", StringComparison.Ordinal)) return;
        KeepMouseIn();
        if (Status != "mac") return;
        // A click on a cover (which can't take the focus) can bring the game back: give the game's window the
        // keys again, so the fence and the Esc hold work straight away.
        try { DisplayServer.WindowMoveToForeground(); }
        catch (Exception error) { GD.PrintErr($"Bip Island: child lock: {error.Message}"); }
        try { Mac.ApplyOptions(); }
        catch (Exception error) { GD.PrintErr($"Bip Island: child lock: {error.Message}"); }
    }

    /// <summary>
    /// Whether the mouse is kept inside the game's window (for the self-test report). On Windows this asks
    /// the system for the cursor's clip rectangle and compares it with the game window; on a Mac it reports
    /// whether the fence (<see cref="FenceMouse"/>) is up and every other screen has its cover.
    /// </summary>
    public static bool MouseKeptIn
    {
        get
        {
            if (IsMac) return _fence && OtherScreensCovered();
            if (Input.MouseMode != Input.MouseModeEnum.Confined) return false;
            try { return Status != "windows" || Windows.CursorClippedToGame(); }
            catch (Exception) { return false; }
        }
    }

    /// <summary>On a Mac, whether <see cref="FenceMouse"/> keeps the mouse in (see <see cref="KeepMouseIn"/>).</summary>
    private static bool _fence;

    /// <summary>
    /// Keeps the mouse inside the game's window, so it can't reach a second screen. On Windows Godot's
    /// Confined mode asks the system to clip the cursor, and the mouse moves as normal. On a Mac Confined
    /// takes over the pointer and moves it without the Mac's acceleration, so it felt slow and held back
    /// (owner, 9 Nov 2026 on 0.2.242): there the mouse stays the Mac's own and <see cref="FenceMouse"/> only
    /// puts it back when it leaves the window. Godot skips a mode it thinks is already set, so the mode goes
    /// to Visible first: the system really is asked again.
    /// </summary>
    private static void KeepMouseIn()
    {
        try
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            if (IsMac)
            {
                _fence = true;
                CoverOtherScreens();
            }
            else Input.MouseMode = Input.MouseModeEnum.Confined;
        }
        catch (Exception error) { GD.PrintErr($"Bip Island: child lock (mouse): {error.Message}"); }
    }

    /// <summary>The paper windows over the Mac's other screens (see <see cref="CoverOtherScreens"/>).</summary>
    private static readonly List<Window> Covers = [];

    /// <summary>
    /// Covers every screen but the game's with a borderless paper window of the game's own, always on top and
    /// never taking the focus (the fence below only pulls the mouse back once a frame, so a quick flick and
    /// click could otherwise reach another program on a second screen; review of #74). A click there lands on
    /// the cover and the game stays in front. Done again when the game comes back to the front, in case a
    /// screen was plugged in or taken away.
    /// </summary>
    private static void CoverOtherScreens()
    {
        if (OtherScreensCovered())
        {
            foreach (var cover in Covers) cover.Show();
            return;
        }
        RemoveCovers();
        foreach (var screen in OtherScreens())
        {
            var cover = new Window
            {
                Title = "Bip Island",
                ForceNative = true,
                Borderless = true,
                AlwaysOnTop = true,
                Unfocusable = true,
                Transient = false,
                Position = DisplayServer.ScreenGetPosition(screen),
                Size = DisplayServer.ScreenGetSize(screen),
            };
            var paper = new ColorRect { Color = Palette.Paper, MouseFilter = Control.MouseFilterEnum.Stop };
            paper.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            cover.AddChild(paper);
            Covers.Add(cover);
            // Install runs from Boot._Ready: add the windows once the tree is free to take them.
            Boot.Instance.CallDeferred(Node.MethodName.AddChild, cover);
        }
    }

    /// <summary>Every screen but the one the game's window is on.</summary>
    private static List<int> OtherScreens()
    {
        var game = DisplayServer.WindowGetCurrentScreen();
        var screens = new List<int>();
        for (var screen = 0; screen < DisplayServer.GetScreenCount(); screen++)
            if (screen != game) screens.Add(screen);
        return screens;
    }

    /// <summary>Whether each other screen has a cover exactly over it (screens can be plugged in, swapped or moved).</summary>
    private static bool OtherScreensCovered()
    {
        Covers.RemoveAll(cover => !GodotObject.IsInstanceValid(cover));
        var screens = OtherScreens();
        if (Covers.Count != screens.Count) return false;
        for (var i = 0; i < screens.Count; i++)
        {
            if (Covers[i].Position != DisplayServer.ScreenGetPosition(screens[i])
                || Covers[i].Size != DisplayServer.ScreenGetSize(screens[i])) return false;
        }
        return true;
    }

    private static ulong _coversCheckedAt;

    private static void RemoveCovers()
    {
        foreach (var cover in Covers)
            if (GodotObject.IsInstanceValid(cover)) cover.QueueFree();
        Covers.Clear();
    }

    /// <summary>
    /// The Mac's mouse fence, every frame while the game is in front: a mouse that has left the game's window
    /// (towards a second screen) goes back to the nearest point inside it. Inside the window it does nothing,
    /// so the mouse moves exactly as the Mac moves it.
    /// </summary>
    public static void FenceMouse()
    {
        if (!_fence || !DisplayServer.WindowIsFocused()) return;
        try
        {
            // Once a second, also check the covers still match the screens (one plugged in while the game is
            // in front gets its cover now, not only the next time the game comes back to the front).
            var now = Time.GetTicksMsec();
            if (now - _coversCheckedAt >= 1000)
            {
                _coversCheckedAt = now;
                CoverOtherScreens();
            }
            var mouse = DisplayServer.MouseGetPosition();
            var origin = DisplayServer.WindowGetPosition();
            var size = DisplayServer.WindowGetSize();
            var back = KidLockRules.FencePoint(mouse.X, mouse.Y, origin.X, origin.Y, size.X, size.Y);
            // Godot reads (0,0) on a Mac when the cursor is in no screen's frame (an edge row, a gap between
            // screens): only trust a (0,0) that is a real place inside the window, never jump the mouse for it.
            if (back is { } point && mouse != Vector2I.Zero) DisplayServer.WarpMouse(new Vector2I(point.X, point.Y) - origin);
        }
        catch (Exception error)
        {
            _fence = false;
            GD.PrintErr($"Bip Island: child lock (mouse fence): {error.Message}");
        }
    }

    /// <summary>Lifts the lock before the game quits or restarts for an update.</summary>
    public static void Release()
    {
        _fence = false;
        try { RemoveCovers(); }
        catch (Exception error) { GD.PrintErr($"Bip Island: child lock release (covers): {error.Message}"); }
        try { Input.MouseMode = Input.MouseModeEnum.Visible; }
        catch (Exception error) { GD.PrintErr($"Bip Island: child lock release (mouse): {error.Message}"); }
        if (!IsOn) return;
        try
        {
            if (Status == "mac") Mac.Unlock();
            else Windows.Unlock();
        }
        catch (Exception error)
        {
            GD.PrintErr($"Bip Island: child lock release: {error.Message}");
        }
        Status = "off";
    }

    /// <summary>What the operating system says now, for the self-test report.</summary>
    public static string Check()
    {
        try
        {
            if (Status == "mac") return KidLockRules.MacLockIsOn(Mac.CurrentOptions()) ? "mac lock on" : "mac lock missing";
            if (Status == "windows") return Windows.HookInstalled ? "windows hook on" : "windows hook missing";
        }
        catch (Exception error)
        {
            return "check failed: " + error.Message;
        }
        return Status;
    }

    private static bool ParentModeKeyHeld()
    {
        try
        {
            return IsMac ? Mac.OptionKeyHeld() : Windows.AltKeyHeld();
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void OpenParentWindow()
    {
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
        DisplayServer.WindowSetTitle("Bip Island (parent mode)");
        var screen = DisplayServer.WindowGetCurrentScreen();
        var size = new Vector2I(1280, 800);
        DisplayServer.WindowSetSize(size);
        DisplayServer.WindowSetPosition(DisplayServer.ScreenGetPosition(screen) + (DisplayServer.ScreenGetSize(screen) - size) / 2);
    }

    /// <summary>The main screen, covered by a borderless window (as the Swift app's KidWindow).</summary>
    private static void CoverMainScreen()
    {
        var screen = DisplayServer.GetPrimaryScreen();
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
        DisplayServer.WindowSetPosition(DisplayServer.ScreenGetPosition(screen));
        DisplayServer.WindowSetSize(DisplayServer.ScreenGetSize(screen));
        DisplayServer.WindowMoveToForeground();
    }

    // Mac: AppKit through the Objective-C runtime.

    private static class Mac
    {
        private const string ObjC = "/usr/lib/libobjc.A.dylib";

        [DllImport(ObjC)] private static extern IntPtr objc_getClass(string name);
        [DllImport(ObjC)] private static extern IntPtr sel_registerName(string name);
        [DllImport(ObjC)] private static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, IntPtr extraBytes);
        [DllImport(ObjC)] private static extern void objc_registerClassPair(IntPtr cls);
        [DllImport(ObjC)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool class_addMethod(IntPtr cls, IntPtr selector, IntPtr implementation, string types);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr Send(IntPtr receiver, IntPtr selector);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern ulong SendULong(IntPtr receiver, IntPtr selector);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern void SendULongArg(IntPtr receiver, IntPtr selector, ulong value);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr SendString(IntPtr receiver, IntPtr selector, string utf8);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")]
        private static extern void SendObserver(IntPtr receiver, IntPtr selector, IntPtr observer, IntPtr action, IntPtr name, IntPtr sender);

        private delegate void NotificationHandler(IntPtr self, IntPtr command, IntPtr notification);
        // Kept alive for as long as the game runs: AppKit calls it through a raw pointer.
        private static NotificationHandler? _powerOffHandler;
        private static IntPtr _observer;

        private static IntPtr App => Send(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));

        public static void Lock()
        {
            WatchForPowerOff();
            CoverMainScreen();
            ApplyOptions();
        }

        public static void ApplyOptions() =>
            SendULongArg(App, sel_registerName("setPresentationOptions:"), KidLockRules.MacLockOptions);

        public static ulong CurrentOptions() => SendULong(App, sel_registerName("presentationOptions"));

        public static void Unlock() => SendULongArg(App, sel_registerName("setPresentationOptions:"), 0);

        public static bool OptionKeyHeld() =>
            (SendULong(objc_getClass("NSEvent"), sel_registerName("modifierFlags")) & KidLockRules.MacOptionKeyFlag) != 0;

        /// <summary>
        /// Never block logging out, restarting or shutting down: when the Mac announces it's about to
        /// power off (NSWorkspaceWillPowerOffNotification), the lock lifts and the game quits.
        /// </summary>
        private static void WatchForPowerOff()
        {
            if (_observer != IntPtr.Zero) return;
            var nsObject = objc_getClass("NSObject");
            var observerClass = objc_allocateClassPair(nsObject, "BipPowerOffObserver", IntPtr.Zero);
            var selector = sel_registerName("bipWillPowerOff:");
            if (observerClass != IntPtr.Zero)
            {
                _powerOffHandler = (_, _, _) =>
                {
                    // AppKit posts this on the main thread. Allow quitting straight away, so the quit
                    // request that follows a logout is accepted even if it arrives before the
                    // deferred Quit runs (otherwise macOS says "Bip Island interrupted log out").
                    Boot.Instance.AllowQuit();
                    Callable.From(() => Boot.Instance.Quit()).CallDeferred();
                };
                class_addMethod(observerClass, selector, Marshal.GetFunctionPointerForDelegate(_powerOffHandler), "v@:@");
                objc_registerClassPair(observerClass);
            }
            else
            {
                observerClass = objc_getClass("BipPowerOffObserver");
            }
            _observer = Send(Send(observerClass, sel_registerName("alloc")), sel_registerName("init"));
            var workspace = Send(objc_getClass("NSWorkspace"), sel_registerName("sharedWorkspace"));
            var centre = Send(workspace, sel_registerName("notificationCenter"));
            var name = SendString(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), "NSWorkspaceWillPowerOffNotification");
            SendObserver(centre, sel_registerName("addObserver:selector:name:object:"), _observer, selector, name, IntPtr.Zero);
        }
    }

    // Windows: exclusive full screen and a low-level keyboard hook.

    private static class Windows
    {
        private const int WhKeyboardLowLevel = 13;
        private const int HcAction = 0;
        private const uint AltDownFlag = 0x20;
        private const int VkControl = 0x11;
        private const int VkMenu = 0x12;

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardHookData
        {
            public uint VirtualKey;
            public uint ScanCode;
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

        private delegate IntPtr HookHandler(int code, IntPtr message, IntPtr data);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookExW(int hookId, HookHandler handler, IntPtr module, uint threadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool GetClipCursor(out Rect rect);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, out Rect rect);
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr window, ref Point point);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point { public int X, Y; }

        /// <summary>Whether Windows clips the cursor to the game window's inside (what Confined asks for).</summary>
        public static bool CursorClippedToGame()
        {
            if (!GetClipCursor(out var clip) || !GetClientRect(_gameWindow, out var inside)) return false;
            var topLeft = new Point { X = inside.Left, Y = inside.Top };
            var bottomRight = new Point { X = inside.Right, Y = inside.Bottom };
            if (!ClientToScreen(_gameWindow, ref topLeft) || !ClientToScreen(_gameWindow, ref bottomRight)) return false;
            // Inside the window, with a pixel's give for rounding.
            return clip.Left >= topLeft.X - 1 && clip.Top >= topLeft.Y - 1
                && clip.Right <= bottomRight.X + 1 && clip.Bottom <= bottomRight.Y + 1;
        }
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int virtualKey);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandleW(IntPtr name);

        [DllImport("user32.dll")] private static extern int GetMessageW(out Message message, IntPtr window, uint first, uint last);
        [DllImport("user32.dll")] private static extern bool PostThreadMessageW(uint threadId, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();

        [StructLayout(LayoutKind.Sequential)]
        private struct Message
        {
            public IntPtr Window;
            public uint Id;
            public IntPtr WParam;
            public IntPtr LParam;
            public uint Time;
            public int X;
            public int Y;
        }

        private const uint WmQuit = 0x0012;

        // Kept alive for as long as the hook is installed: Windows calls it through a raw pointer.
        private static HookHandler? _handler;
        private static IntPtr _hook;
        private static IntPtr _gameWindow;
        private static uint _hookThreadId;

        public static bool HookInstalled => _hook != IntPtr.Zero;

        public static bool AltKeyHeld() => GetAsyncKeyState(VkMenu) < 0;

        /// <summary>
        /// The hook lives on its own thread with its own message loop. On the game's main thread a
        /// long frame would hold up every key on the computer, and Windows quietly removes hooks that
        /// keep timing out.
        /// </summary>
        public static void Lock()
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
            _gameWindow = new IntPtr(DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle));
            _handler = OnKey;
            var ready = new System.Threading.ManualResetEventSlim();
            var error = 0;
            var thread = new System.Threading.Thread(() =>
            {
                _hookThreadId = GetCurrentThreadId();
                _hook = SetWindowsHookExW(WhKeyboardLowLevel, _handler, GetModuleHandleW(IntPtr.Zero), 0);
                if (_hook == IntPtr.Zero) error = Marshal.GetLastWin32Error();
                ready.Set();
                if (_hook == IntPtr.Zero) return;
                // Keys are delivered to the hook while this loop waits; WM_QUIT (from Unlock) ends it.
                while (GetMessageW(out _, IntPtr.Zero, 0, 0) > 0) { }
                UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            })
            { IsBackground = true, Name = "Bip Island child lock" };
            thread.Start();
            if (!ready.Wait(TimeSpan.FromSeconds(2))) throw new InvalidOperationException("keyboard hook thread didn't start");
            if (_hook == IntPtr.Zero) throw new InvalidOperationException($"keyboard hook refused (error {error})");
        }

        public static void Unlock()
        {
            if (_hookThreadId != 0) PostThreadMessageW(_hookThreadId, WmQuit, IntPtr.Zero, IntPtr.Zero);
            _hookThreadId = 0;
        }

        /// <summary>Runs for every key press on the computer, so it does as little as possible.</summary>
        private static IntPtr OnKey(int code, IntPtr message, IntPtr data)
        {
            // Presses and releases both: the Start menu opens when the Windows key is let go.
            if (code == HcAction && GetForegroundWindow() == _gameWindow)
            {
                var key = Marshal.PtrToStructure<KeyboardHookData>(data);
                var alt = (key.Flags & AltDownFlag) != 0;
                var ctrl = GetAsyncKeyState(VkControl) < 0;
                if (KidLockRules.BlocksWindowsKey((int)key.VirtualKey, alt, ctrl)) return new IntPtr(1);
            }
            return CallNextHookEx(_hook, code, message, data);
        }
    }
}
