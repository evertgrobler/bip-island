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
    public static Boot Instance { get; private set; } = null!;
    /// <summary>Arguments after "--" on the command line (the self-tests use these).</summary>
    public static string[] UserArgs { get; private set; } = System.Array.Empty<string>();

    private bool _allowQuit;

    public override void _EnterTree()
    {
        Instance = this;
        // Must run before anything else: while installing or updating, Velopack starts the game
        // with special arguments and expects it to do its job and exit straight away.
        // Updates need a grown-up (owner rule): never install a downloaded update on launch by itself.
        VelopackApp.Build().SetAutoApplyOnStartup(false).Run();

        ProcessMode = ProcessModeEnum.Always;
        UserArgs = OS.GetCmdlineUserArgs();
        Fonts.Load();

        // Kid lock: the window close button, Cmd-Q and Alt-F4 do nothing; only the parent area's Quit button quits.
        GetTree().AutoAcceptQuit = false;
    }

    public override void _Ready()
    {
        // Full screen and locked (or a normal window in parent mode, tests and --bip-windowed).
        KidLock.Install(windowedRequested: UserArgs.Contains("--bip-windowed"));
        // "--bip-scene res://…tscn" opens that scene instead of the main screen (dev pages, screenshots).
        var scene = System.Array.IndexOf(UserArgs, "--bip-scene");
        if (scene >= 0 && scene + 1 < UserArgs.Length)
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, UserArgs[scene + 1]);
        if (SelfTest.IsRequested(UserArgs))
            SelfTest.Run(this, UserArgs);
        else
            _ = Updater.CheckAndDownloadInBackground();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            if (_allowQuit) GetTree().Quit();
            else GD.Print("Bip Island: quitting needs the parent area (hold Esc).");
        }
        else if (what == NotificationApplicationFocusIn)
        {
            KidLock.Reapply();
        }
    }

    /// <summary>Lets the window close (an update about to restart the game).</summary>
    public void AllowQuit()
    {
        _allowQuit = true;
        KidLock.Release();
    }

    /// <summary>The only way out: the parent gate, a self-test finishing, or an update restarting the game.</summary>
    public void Quit(int exitCode = 0)
    {
        AllowQuit();
        GetTree().Quit(exitCode);
    }
}
