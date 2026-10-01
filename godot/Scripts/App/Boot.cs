using System.Linq;
using BipIsland.Drawing;
using Godot;
using Velopack;

namespace BipIsland.App;

/// <summary>
/// Runs first (autoload): Velopack's install/update hooks, the bundled fonts, the kid lock's
/// "can't close" rule, the quiet background update check and, in CI, the self-tests.
/// </summary>
public partial class Boot : Node
{
    /// <summary>How long Esc must be held to leave. (Phase 4 adds the grown-up maths question after it.)</summary>
    private const double EscHoldSeconds = 3;

    public static Boot Instance { get; private set; } = null!;
    /// <summary>Arguments after "--" on the command line (the self-tests use these).</summary>
    public static string[] UserArgs { get; private set; } = System.Array.Empty<string>();

    private bool _allowQuit;
    private double _escHeld;

    public override void _EnterTree()
    {
        Instance = this;
        // Must run before anything else: while installing or updating, Velopack starts the game
        // with special arguments and expects it to do its job and exit straight away.
        VelopackApp.Build().Run();

        ProcessMode = ProcessModeEnum.Always;
        UserArgs = OS.GetCmdlineUserArgs();
        Fonts.Load();

        // Kid lock: the window close button, Cmd-Q and Alt-F4 do nothing; only the parent gate quits.
        GetTree().AutoAcceptQuit = false;
        if (UserArgs.Contains("--bip-windowed"))
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
    }

    public override void _Ready()
    {
        if (SelfTest.IsRequested(UserArgs))
            SelfTest.Run(this, UserArgs);
        else
            _ = Updater.CheckAndDownloadInBackground();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && !_allowQuit)
            GD.Print("Bip Island: quitting needs the parent gate (hold Esc).");
    }

    public override void _Process(double delta)
    {
        if (Input.IsKeyPressed(Key.Escape))
        {
            _escHeld += delta;
            if (_escHeld >= EscHoldSeconds) Quit();
        }
        else
        {
            _escHeld = 0;
        }
    }

    /// <summary>The only way out: the parent gate, a self-test finishing, or an update restarting the game.</summary>
    public void Quit(int exitCode = 0)
    {
        _allowQuit = true;
        GetTree().Quit(exitCode);
    }
}
