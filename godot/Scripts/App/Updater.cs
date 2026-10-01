using System;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Velopack;

namespace BipIsland.App;

/// <summary>
/// Automatic updates (Velopack), the Godot version of the Swift app's Sparkle setup.
/// The feed address comes from res://assets/update.json, written by CI; local and pull-request
/// builds have none and never check. A new version is downloaded quietly in the background and
/// applied the next time the game starts (VelopackApp's auto-apply), so play is never interrupted.
/// The feed has one folder per computer (feed/win/ and feed/osx/), because Velopack names its packages
/// the same on both.
/// </summary>
public static class Updater
{
    public const string ConfigPath = "res://assets/update.json";

    /// <summary>The feed this build checks, or null for builds that don't update.</summary>
    public static string? FeedUrl { get; } = ReadFeedUrl();

    /// <summary>This computer's folder in the feed.</summary>
    public static string? PlatformFeedUrl =>
        FeedUrl == null ? null : FeedUrl.TrimEnd('/') + (OS.GetName() == "Windows" ? "/win/" : "/osx/");

    /// <summary>The installed version, or null when the game isn't installed (the editor, a test run).</summary>
    public static string? InstalledVersion
    {
        get
        {
            try
            {
                var manager = new UpdateManager(PlatformFeedUrl ?? "https://updates.invalid/");
                return manager.IsInstalled ? manager.CurrentVersion?.ToString() : null;
            }
            catch (Exception e)
            {
                GD.Print($"Bip Island: couldn't read the installed version: {e.Message}");
                return null;
            }
        }
    }

    private static string? ReadFeedUrl()
    {
        if (!FileAccess.FileExists(ConfigPath)) return null;
        try
        {
            using var doc = JsonDocument.Parse(FileAccess.GetFileAsString(ConfigPath));
            var feed = doc.RootElement.GetProperty("feed").GetString();
            return string.IsNullOrWhiteSpace(feed) ? null : feed;
        }
        catch (Exception e)
        {
            GD.PushWarning($"Bip Island: {ConfigPath} couldn't be read: {e.Message}");
            return null;
        }
    }

    /// <summary>Checks the feed and downloads a newer version, if there is one. Never throws.</summary>
    public static async Task CheckAndDownloadInBackground()
    {
        if (PlatformFeedUrl is not { } feed) return;
        try
        {
            var manager = new UpdateManager(feed);
            if (!manager.IsInstalled) return;
            var update = await manager.CheckForUpdatesAsync();
            if (update == null) return;
            await manager.DownloadUpdatesAsync(update);
            GD.Print($"Bip Island: version {update.TargetFullRelease.Version} downloaded; it installs next time the game starts.");
        }
        catch (Exception e)
        {
            // No internet, or the feed is down: try again next launch. Children never see this.
            GD.Print($"Bip Island: update check failed: {e.Message}");
        }
    }

    /// <summary>
    /// For the CI install-and-update test: check <paramref name="feed"/>, download the newer version,
    /// then quit, install it and restart with <paramref name="restartArgs"/>. Returns an error message
    /// if it couldn't (on success the process exits and never returns).
    /// </summary>
    public static async Task<string> UpdateNowAndRestart(string feed, string[] restartArgs)
    {
        try
        {
            var manager = new UpdateManager(feed);
            if (!manager.IsInstalled) return "the game isn't installed, so it can't update";
            var update = await manager.CheckForUpdatesAsync();
            if (update == null) return $"no newer version than {manager.CurrentVersion} in {feed}";
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
