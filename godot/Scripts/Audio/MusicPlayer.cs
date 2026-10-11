using System;
using BipCore;
using Godot;

namespace BipIsland.Audio;

/// <summary>
/// The calm background loops on the map and the islands (rules in <see cref="MusicRules"/>). Two players
/// take turns so one loop fades into the next; the volume follows the grown-up's setting and dips while
/// the narrator speaks. Loops are res://assets/music/&lt;track&gt;.ogg; an island without its own loop plays
/// the map's, and with no loops at all it stays quiet.
/// Keeps playing behind the parent gate (the tree pauses there), only lower.
/// </summary>
public partial class MusicPlayer : Node
{
    public const string MusicFolder = "res://assets/music/";
    /// <summary>How fast the volume eases towards its goal (per second): about a second for a fade, quick enough to clear the way for the voice.</summary>
    private const float VolumeSpeed = 3f;
    private const float Silent = 0.0001f;

    private readonly AudioStreamPlayer[] _players = [new(), new()];
    private readonly float[] _volumes = [0, 0];
    private int _current;
    private string? _track;
    /// <summary>The current track was found and is playing (checked once, not every frame).</summary>
    private bool _audible;

    /// <summary>The grown-up's setting.</summary>
    public MusicLevel Level { get; set; } = MusicLevel.Normal;
    /// <summary>Asked every frame: is the narrator talking? The music dips while it is.</summary>
    public Func<bool> NarratorSpeaking { get; set; } = () => false;

    /// <summary>The loop playing now (null when quiet), for the walk-through test.</summary>
    public string? Track => _track;

    public static string PathFor(string track) => MusicFolder + track + ".ogg";

    public static bool HasTrack(string track) => ResourceLoader.Exists(PathFor(track));

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        foreach (var player in _players)
        {
            player.VolumeDb = Mathf.LinearToDb(Silent);
            AddChild(player);
        }
    }

    /// <summary>Fades to <paramref name="track"/> (null for quiet). Asking for the loop already playing changes nothing.</summary>
    public void PlayTrack(string? track)
    {
        // An island without its own loop yet carries on with the map's.
        if (track != null && !HasTrack(track) && HasTrack(MusicRules.MapTrack)) track = MusicRules.MapTrack;
        if (track == _track) return;
        _track = track;
        _audible = false;
        if (track == null || !HasTrack(track) || GD.Load<AudioStream>(PathFor(track)) is not { } stream)
        {
            if (track != null) GD.Print($"Bip Island: no music {track} in this build");
            return;
        }
        if (stream is AudioStreamOggVorbis ogg) ogg.Loop = true;
        _current = 1 - _current;
        var player = _players[_current];
        player.Stream = stream;
        _volumes[_current] = 0;
        player.VolumeDb = Mathf.LinearToDb(Silent);
        player.Play();
        _audible = true;
    }

    public override void _Process(double delta)
    {
        var target = _audible ? MusicRules.Volume(Level, NarratorSpeaking()) : 0f;
        // Each player eases towards its goal: the new loop up to the target, the old one down to nothing.
        var ease = 1f - Mathf.Exp(-(float)delta * VolumeSpeed);
        for (var i = 0; i < _players.Length; i++)
        {
            var goal = i == _current ? target : 0f;
            _volumes[i] = Mathf.Lerp(_volumes[i], goal, ease);
            var player = _players[i];
            player.VolumeDb = Mathf.LinearToDb(Math.Max(_volumes[i], Silent));
            if (i != _current && _volumes[i] < 0.002f && player.Playing) player.Stop();
        }
    }
}
