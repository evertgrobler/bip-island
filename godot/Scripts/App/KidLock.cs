using System;
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
        }
        catch (Exception error)
        {
            Status = "failed: " + error.Message;
            GD.PrintErr($"Bip Island: the child lock couldn't start, so the game plays unlocked: {error.Message}");
            // Still fill the screen, as a child expects.
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        }
    }

    /// <summary>The game came back to the front: put the Mac options back (macOS can drop them).</summary>
    public static void Reapply()
    {
        if (Status != "mac") return;
        try { Mac.ApplyOptions(); }
        catch (Exception error) { GD.PrintErr($"Bip Island: child lock: {error.Message}"); }
    }

    /// <summary>Lifts the lock before the game quits or restarts for an update.</summary>
    public static void Release()
    {
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
