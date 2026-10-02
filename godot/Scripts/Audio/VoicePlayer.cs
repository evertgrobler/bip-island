using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace BipIsland.Audio;

/// <summary>
/// Plays narrator clips one after another ("Find the picture that starts with…" + "sss").
/// Clips are res://assets/audio/&lt;name&gt;.ogg (the file-name contract in CLAUDE.md, as .ogg).
/// A clip that isn't in the build is replaced by a short silence, so the game never gets stuck.
/// </summary>
public partial class VoicePlayer : Node
{
    public const string AudioFolder = "res://assets/audio/";
    private const double GapBetweenClips = 0.12;
    private const double SilentStubLength = 0.6;

    private readonly AudioStreamPlayer _player = new();
    private int _sequence;

    public override void _Ready()
    {
        // Pauses with the game (the parent gate pauses the tree): the clip and the wait for the next
        // one both stop where they are and carry on afterwards, so a game waiting for the end of a
        // sentence never gets stuck.
        ProcessMode = ProcessModeEnum.Pausable;
        AddChild(_player);
    }

    public static string PathFor(string clip) => AudioFolder + clip + ".ogg";

    public static bool HasClip(string clip) => ResourceLoader.Exists(PathFor(clip));

    /// <summary>How many clips are in this build (for the parent area and the self-test report).</summary>
    public static int CountClips()
    {
        using var dir = DirAccess.Open(AudioFolder);
        if (dir == null) return 0;
        // Exported games list imported files as "name.ogg.import" (or "name.ogg.remap"); count each clip once.
        return dir.GetFiles()
            .Select(f => f.Replace(".import", "").Replace(".remap", ""))
            .Where(f => f.EndsWith(".ogg", StringComparison.Ordinal))
            .Distinct()
            .Count();
    }

    /// <summary>
    /// Plays clips in order. Starting a new sequence stops the current one.
    /// <paramref name="onClipStart"/> fires as each clip begins; <paramref name="completion"/> after the last.
    /// </summary>
    public void Play(IReadOnlyList<string> clips, Action<int, string>? onClipStart = null, Action? completion = null)
    {
        Stop();
        PlayClip(0, clips, _sequence, onClipStart, completion);
    }

    public void Stop()
    {
        _sequence++;
        _player.Stop();
    }

    private void PlayClip(int index, IReadOnlyList<string> clips, int id, Action<int, string>? onClipStart, Action? completion)
    {
        if (id != _sequence) return;
        if (index >= clips.Count)
        {
            completion?.Invoke();
            return;
        }
        var name = clips[index];
        onClipStart?.Invoke(index, name);

        var length = SilentStubLength;
        if (HasClip(name) && GD.Load<AudioStream>(PathFor(name)) is { } stream)
        {
            _player.Stream = stream;
            _player.Play();
            length = stream.GetLength();
        }
        else
        {
            GD.Print($"Bip Island: no clip {name} in this build, playing silence instead");
        }

        GetTree().CreateTimer(length + GapBetweenClips, processAlways: false).Timeout +=
            () => PlayClip(index + 1, clips, id, onClipStart, completion);
    }
}
