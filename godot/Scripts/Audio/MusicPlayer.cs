using System;
using BipCore;
using Godot;

namespace BipIsland.Audio;

/// <summary>
/// The calm background loops on the map and the islands (rules in <see cref="MusicRules"/>). Two players
/// take turns so one loop fades into the next; the volume follows the grown-up's setting. The music drops
/// at once when the narrator starts speaking (a phonics sound can be a third of a second long) and eases
/// back up after she finishes. While the game is paused (the parent gate, Settings, the setup guide) it
/// fades out and holds its place. Loops are res://assets/music/&lt;track&gt;.ogg; an island without its own
/// loop plays the map's, and with no loops at all it stays quiet.
/// </summary>
public partial class MusicPlayer : Node
{
    public const string MusicFolder = "res://assets/music/";
    /// <summary>How fast the volume eases up or fades out (per second): about a second for a fade.</summary>
    private const float VolumeSpeed = 3f;
    private const float Silent = 0.0001f;
    /// <summary>Below this a player is silent enough to stop (or pause) without a click.</summary>
    private const float Inaudible = 0.002f;

    private readonly AudioStreamPlayer[] _players = [new(), new()];
    private readonly float[] _volumes = [0, 0];
    private readonly string?[] _loaded = [null, null];
    private int _current;
    private string? _track;
    /// <summary>The current track was found (checked once, not every frame).</summary>
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
        // Runs while the tree is paused too, so it can fade out under the parent gate and come back after.
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
        if (track == null) return;

        // Going back to the loop that is still fading out: keep it running and bring it back up.
        var other = 1 - _current;
        if (_loaded[other] == track && _players[other].Playing)
        {
            _current = other;
            _audible = true;
            return;
        }
        if (!HasTrack(track) || GD.Load<AudioStream>(PathFor(track)) is not { } stream)
        {
            GD.Print($"Bip Island: no music {track} in this build");
            return;
        }
        if (stream is AudioStreamOggVorbis ogg) ogg.Loop = true;
        _current = other;
        var player = _players[_current];
        player.Stream = stream;
        _loaded[_current] = track;
        _volumes[_current] = 0;
        player.VolumeDb = Mathf.LinearToDb(Silent);
        player.StreamPaused = false;
        player.Play();
        _audible = true;
    }

    public override void _Process(double delta)
    {
        var paused = GetTree().Paused;
        var target = _audible && !paused ? MusicRules.Volume(Level, NarratorSpeaking()) : 0f;
        var ease = 1f - Mathf.Exp(-(float)delta * VolumeSpeed);
        for (var i = 0; i < _players.Length; i++)
        {
            var player = _players[i];
            var goal = i == _current ? target : 0f;
            var current = i == _current;

            // Lowering for the narrator is instant; everything else eases.
            if (current && goal < _volumes[i] && NarratorSpeaking() && !paused) _volumes[i] = goal;
            else _volumes[i] = Mathf.Lerp(_volumes[i], goal, ease);

            if (current && goal > 0)
            {
                // Coming back after a pause, a game or the setting being off.
                if (player.StreamPaused) player.StreamPaused = false;
                else if (!player.Playing && player.Stream != null) player.Play();
            }
            else if (_volumes[i] < Inaudible)
            {
                // Silent: hold the place while paused, otherwise stop decoding.
                if (current && paused && _audible) player.StreamPaused = player.Playing || player.StreamPaused;
                else if (player.Playing) player.Stop();
            }
            player.VolumeDb = Mathf.LinearToDb(Math.Max(_volumes[i], Silent));
        }
    }
}
