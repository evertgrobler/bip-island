using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BipIsland.Audio;
using BipIsland.Drawing;
using Godot;

namespace BipIsland.App;

/// <summary>
/// Checks CI runs on the real exported game, because nobody can playtest every build by hand.
/// Arguments go after "--":
///   --bip-report &lt;file&gt;               write a JSON report (version, fonts, clips, content, save) and quit
///   --bip-screenshot &lt;file&gt;           save a PNG of the first screen and quit
///   --bip-update-test &lt;feed&gt; &lt;file&gt;  save something, update from the feed, restart, then write the report
///   --bip-scene &lt;res://path.tscn&gt;     open that scene first (Boot), e.g. to screenshot a dev page
///   --bip-walk &lt;file&gt;                 click round every screen and check each step (Dev/WalkTest.cs)
///   --bip-save-dir &lt;folder&gt;           keep saves there instead of the player's folder
///   --bip-kidlock-check &lt;file&gt;        (not headless) write whether the kid lock really took hold, then quit
/// </summary>
public static class SelfTest
{
    public const string MarkerPath = "user://selftest_marker.json";

    public static bool IsRequested(string[] args) =>
        args.Any(a => a is "--bip-report" or "--bip-screenshot" or "--bip-update-test" or "--bip-walk" or "--bip-kidlock-check");

    public static async void Run(Boot boot, string[] args)
    {
        var tree = boot.GetTree();
        // Let the main screen build and draw.
        for (var i = 0; i < 3; i++) await boot.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        try
        {
            if (Value(args, "--bip-update-test", 2) is { } update)
            {
                var (feed, report) = (update[0], update[1]);
                WriteMarker();
                // The new version restarts headless and writes the report, proving the update and the save.
                var error = await Updater.UpdateNowAndRestart(feed, new[] { "--headless", "--", "--bip-report", report });
                WriteReport(report, error);
                boot.Quit(1);
                return;
            }
            if (Value(args, "--bip-kidlock-check", 1) is { } lockCheck)
            {
                // Give the window and the operating system a moment to settle, then ask it.
                for (var i = 0; i < 60; i++) await boot.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                var state = KidLock.Check();
                var locked = state is "mac lock on" or "windows hook on";
                System.IO.File.WriteAllText(lockCheck[0], JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["os"] = OS.GetName(),
                    ["kidLock"] = state,
                    ["locked"] = locked,
                }));
                GD.Print($"Bip Island: child lock check: {state}");
                boot.Quit(locked ? 0 : 1);
                return;
            }
            if (Value(args, "--bip-walk", 1) is { } walk)
            {
                var passed = await BipIsland.Dev.WalkTest.Run(boot, walk[0]);
                boot.Quit(passed ? 0 : 1);
                return;
            }
            if (Value(args, "--bip-screenshot", 1) is { } shot)
            {
                // Give the animations a moment to settle into a typical frame.
                for (var i = 0; i < 45; i++) await boot.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                await boot.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var image = boot.GetViewport().GetTexture().GetImage();
                var saved = image.SavePng(shot[0]);
                GD.Print($"Bip Island: screenshot {shot[0]}: {saved}");
                if (saved != Error.Ok) { boot.Quit(1); return; }
            }
            if (Value(args, "--bip-report", 1) is { } path)
            {
                var ok = WriteReport(path[0], null);
                boot.Quit(ok ? 0 : 1);
                return;
            }
            boot.Quit(0);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Bip Island: self-test crashed: {e}");
            boot.Quit(1);
        }
    }

    private static string[]? Value(string[] args, string flag, int count)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + count < args.Length ? args[(i + 1)..(i + 1 + count)] : null;
    }

    private static void WriteMarker()
    {
        using var file = FileAccess.Open(MarkerPath, FileAccess.ModeFlags.Write);
        file.StoreString(JsonSerializer.Serialize(new Dictionary<string, string?>
        {
            ["writtenBy"] = Updater.InstalledVersion,
            ["writtenAt"] = DateTime.UtcNow.ToString("O"),
        }));
    }

    /// <summary>Writes the report; returns true when everything the game needs is in the build.</summary>
    private static bool WriteReport(string path, string? error)
    {
        var contentFiles = CountFiles("res://assets/content", ".json");
        var clipCount = VoicePlayer.CountClips();
        var sampleClip = VoicePlayer.HasClip("snd_s") && GD.Load<AudioStream>(VoicePlayer.PathFor("snd_s"))?.GetLength() > 0;
        var graphemes = ReadsAsJson("res://assets/content/phonics/graphemes.json");
        var game = BipIsland.Game.GameCoordinator.Instance;
        var islandsOpen = new[] { game.LettersOpen, game.NumbersOpen, game.WordsOpen, game.CodingOpen }.Count(open => open);
        var ok = error == null && game.Content != null && islandsOpen == 4 && Fonts.IsInstalled && Fonts.EmojiInstalled && contentFiles > 0 && clipCount > 0
                 && sampleClip && graphemes;

        var report = new Dictionary<string, object?>
        {
            ["ok"] = ok,
            ["error"] = error,
            ["os"] = OS.GetName(),
            ["appVersion"] = ProjectSettings.GetSetting("application/config/version").AsString(),
            ["installedVersion"] = Updater.InstalledVersion,
            ["updateFeed"] = Updater.PlatformFeedUrl,
            ["fontsInstalled"] = Fonts.IsInstalled,
            ["emojiInstalled"] = Fonts.EmojiInstalled,
            ["contentFiles"] = contentFiles,
            ["contentLoaded"] = game.Content != null,
            ["islandsOpen"] = islandsOpen,
            ["graphemesReadable"] = graphemes,
            ["clipCount"] = clipCount,
            ["sampleClipPlays"] = sampleClip,
            ["saveMarker"] = FileAccess.FileExists(MarkerPath) ? FileAccess.GetFileAsString(MarkerPath) : null,
            ["userDataDir"] = OS.GetUserDataDir(),
            ["kidLock"] = KidLock.Check(),
        };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(path, json);
        GD.Print(json);
        return ok;
    }

    private static bool ReadsAsJson(string resPath)
    {
        if (!FileAccess.FileExists(resPath)) return false;
        try
        {
            using var _ = JsonDocument.Parse(FileAccess.GetFileAsString(resPath));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static int CountFiles(string folder, string extension)
    {
        using var dir = DirAccess.Open(folder);
        if (dir == null) return 0;
        var count = dir.GetFiles().Count(f => f.EndsWith(extension, StringComparison.Ordinal));
        foreach (var sub in dir.GetDirectories()) count += CountFiles($"{folder}/{sub}", extension);
        return count;
    }
}
