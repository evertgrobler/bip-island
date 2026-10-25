using System;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Velopack;
using Velopack.Sources;

namespace BipIsland.App;

/// <summary>
/// Automatic updates (Velopack). Each version is a GitHub Release in a public downloads repository
/// (evertgrobler/bip-island-downloads), read with Velopack's GithubSource. The repository address comes
/// from res://assets/update.json, written by CI on main; local and pull-request builds have none and
/// never check. A new version is downloaded quietly in the background, but updates need a grown-up
/// (owner rule): it is only installed after the parent gate, from the "Update ready" button or the
/// parent area's Settings tab. Each release holds both computers' files (releases.win.json and
/// releases.osx.json), and the installed game reads the one for its own computer.
/// </summary>
public static class Updater
{
    public const string ConfigPath = "res://assets/update.json";

    private static UpdateManager? _manager;
    private static UpdateInfo? _downloaded;

    /// <summary>The version downloaded and waiting for a grown-up, or null.</summary>
    public static string? ReadyVersion => _downloaded?.TargetFullRelease.Version.ToString();

    /// <summary>A new version finished downloading (its version number). Raised on the main thread.</summary>
    public static event Action<string>? UpdateReady;

    /// <summary>The GitHub repository this build updates from, or null for builds that don't update.</summary>
    public static string? RepoUrl { get; } = ReadRepoUrl();

    /// <summary>Releases in <paramref name="repoUrl"/>, read without a token (the repository is public).</summary>
    private static UpdateManager Manager(string repoUrl) => new(new ReleaseSource(repoUrl));

    /// <summary>
    /// Velopack's GithubSource. A non-GitHub address such as http://127.0.0.1:8765/test/bip-island is
    /// read through its /api/v3/ path (how the CI install test serves a local copy); Velopack drops the
    /// port there, so this keeps it.
    /// </summary>
    private sealed class ReleaseSource(string repoUrl) : GithubSource(repoUrl, null, false)
    {
        protected override Uri GetApiBaseUrl(Uri repoUrl) =>
            repoUrl.Host.EndsWith("github.com", StringComparison.OrdinalIgnoreCase)
                ? base.GetApiBaseUrl(repoUrl)
                : new Uri(repoUrl.GetLeftPart(UriPartial.Authority) + "/api/v3/");
    }

    /// <summary>The installed version, or null when the game isn't installed (the editor, a test run).</summary>
    public static string? InstalledVersion
    {
        get
        {
            try
            {
                var manager = Manager(RepoUrl ?? "https://github.com/invalid/invalid");
                return manager.IsInstalled ? manager.CurrentVersion?.ToString() : null;
            }
            catch (Exception e)
            {
                GD.Print($"Bip Island: couldn't read the installed version: {e.Message}");
                return null;
            }
        }
    }

    private static string? ReadRepoUrl()
    {
        if (!FileAccess.FileExists(ConfigPath)) return null;
        try
        {
            using var doc = JsonDocument.Parse(FileAccess.GetFileAsString(ConfigPath));
            var repo = doc.RootElement.GetProperty("github").GetString();
            return string.IsNullOrWhiteSpace(repo) ? null : repo;
        }
        catch (Exception e)
        {
            GD.PushWarning($"Bip Island: {ConfigPath} couldn't be read: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Checks the releases and downloads a newer version, if there is one. Never throws; returns a short
    /// sentence for the parent area's Settings tab.
    /// </summary>
    public static async Task<string> CheckAndDownloadInBackground()
    {
        if (RepoUrl is not { } repo) return "Automatic updates aren't switched on in this build.";
        try
        {
            var manager = Manager(repo);
            if (!manager.IsInstalled) return "This copy isn't installed, so it can't update itself.";
            if (ReadyVersion is { } waiting) return $"Version {waiting} is ready to install.";
            var update = await manager.CheckForUpdatesAsync();
            if (update == null) return "This is the newest version.";
            await manager.DownloadUpdatesAsync(update);
            _manager = manager;
            _downloaded = update;
            var version = update.TargetFullRelease.Version.ToString();
            GD.Print($"Bip Island: version {version} downloaded; it installs once a grown-up says yes.");
            Callable.From(() => UpdateReady?.Invoke(version)).CallDeferred();
            return $"Version {version} is ready to install.";
        }
        catch (Exception e)
        {
            // No internet, or GitHub is down: try again next launch. Children never see this.
            GD.Print($"Bip Island: update check failed: {e.Message}");
            return "Couldn't check for updates (no internet?). It tries again next time the game opens.";
        }
    }

    /// <summary>
    /// Installs the downloaded version and restarts the game. Only call this after the parent gate.
    /// Returns an error message if it couldn't (on success the game quits and never returns).
    /// </summary>
    public static string InstallAndRestart()
    {
        if (_manager == null || _downloaded == null) return "no update has been downloaded";
        try
        {
            Boot.Instance.AllowQuit();
            _manager.ApplyUpdatesAndRestart(_downloaded.TargetFullRelease);
            return "ApplyUpdatesAndRestart returned without restarting";
        }
        catch (Exception e)
        {
            GD.PrintErr($"Bip Island: installing the update failed: {e.Message}");
            return $"installing the update failed: {e.Message}";
        }
    }

    /// <summary>
    /// For the CI install-and-update test: check the releases in <paramref name="repoUrl"/> (a local copy
    /// served by scripts/godot/fake_github.py), download the newer version,
    /// then quit, install it and restart with <paramref name="restartArgs"/>. Returns an error message
    /// if it couldn't (on success the process exits and never returns).
    /// </summary>
    public static async Task<string> UpdateNowAndRestart(string repoUrl, string[] restartArgs)
    {
        try
        {
            var manager = Manager(repoUrl);
            if (!manager.IsInstalled) return "the game isn't installed, so it can't update";
            var update = await manager.CheckForUpdatesAsync();
            if (update == null) return $"no newer version than {manager.CurrentVersion} in {repoUrl}";
            await manager.DownloadUpdatesAsync(update);
            manager.ApplyUpdatesAndRestart(update.TargetFullRelease, restartArgs);
            return "ApplyUpdatesAndRestart returned without restarting";
        }
        catch (Exception e)
        {
            return $"update failed: {e}";
        }
    }
}
