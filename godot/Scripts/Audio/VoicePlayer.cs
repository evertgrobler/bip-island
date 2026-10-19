using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace BipIsland.Audio;

/// <summary>
/// Plays narrator clips one after another ("Find the picture that starts with…" + "sss").
/// Clips are res://assets/audio/&lt;name&gt;.ogg (the file-name contract in CLAUDE.md, as .ogg), and an
/// Afrikaans child's are res://assets/audio/af/&lt;name&gt;.ogg (same names, see <see cref="Language"/>).
/// A clip that isn't in the build is replaced by a short silence, so the game never gets stuck.
/// </summary>
public partial class VoicePlayer : Node
{
    public const string AudioFolder = "res://assets/audio/";
    private const double GapBetweenClips = 0.12;
    /// <summary>After a clip that leads on ("seven…", "plus") the next follows almost at once.</summary>
    private const double GapAfterLeadOn = 0.03;
    private const double SilentStubLength = 0.6;

    private readonly AudioStreamPlayer _player = new();
    private int _sequence;
    /// <summary>A sequence is under way, including the short gaps between its clips.</summary>
    private bool _inSequence;

    public override void _Ready()
    {
        // Pauses with the game (the parent gate pauses the tree): the clip and the wait for the next
        // one both stop where they are and carry on afterwards, so a game waiting for the end of a
        // sentence never gets stuck.
        ProcessMode = ProcessModeEnum.Pausable;
        AddChild(_player);
    }

    /// <summary>
    /// The narrator is talking right now (the background music dips while she is). True for the whole
    /// sequence, gaps included, so the music doesn't swell just before a phonics sound.
    /// </summary>
    public bool Speaking => _inSequence || _player.Playing;

    /// <summary>
    /// Where the language comes from: the coordinator points this at its
    /// <see cref="Game.GameCoordinator.SpokenLanguage"/> once at start-up, so the voice never keeps a copy
    /// of its own. English until then (previews and tests without the coordinator).
    /// </summary>
    public static Func<BipCore.HomeLanguage> LanguageSource { private get; set; } = () => BipCore.HomeLanguage.English;

    /// <summary>The language being spoken: which folder clips come from. An Afrikaans clip that isn't
    /// recorded yet plays as a short silence like any missing clip, never the English one.</summary>
    public static BipCore.HomeLanguage Language => LanguageSource();

    public static string PathFor(string clip) => PathFor(clip, Language);

    /// <summary>A clip's path in a given language (the self-test always checks the English one).</summary>
    public static string PathFor(string clip, BipCore.HomeLanguage language) => FolderOf(language) + clip + ".ogg";

    public static bool HasClip(string clip) => HasClip(clip, Language);

    public static bool HasClip(string clip, BipCore.HomeLanguage language) => ResourceLoader.Exists(PathFor(clip, language));

    private static string FolderOf(BipCore.HomeLanguage language)
    {
        var folder = BipCore.Languages.Folder(language);
        return AudioFolder + (folder.Length > 0 ? folder + "/" : "");
    }

    private static string? StandIn(string clip) =>
        clip.EndsWith("_mid", StringComparison.Ordinal) ? clip[..^"_mid".Length]
        : clip == BipCore.VoiceLine.And ? BipCore.AudioCatalogue.WordClip("and") : null;

    /// <summary>The whole recorded sentence when this build has it, otherwise the clips that make it up.</summary>
    public static List<string> Prefer(string sentence, IEnumerable<string> pieces) =>
        HasClip(sentence) ? [sentence] : pieces.ToList();

    /// <summary>How many clips this build has in a language (for the parent area and the self-test report).</summary>
    public static int CountClips(BipCore.HomeLanguage language)
    {
        using var dir = DirAccess.Open(FolderOf(language));
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
        _inSequence = clips.Count > 0;
        PlayClip(0, clips, _sequence, onClipStart, completion);
    }

    public void Stop()
    {
        _sequence++;
        _inSequence = false;
        _player.Stop();
    }

    private void PlayClip(int index, IReadOnlyList<string> clips, int id, Action<int, string>? onClipStart, Action? completion)
    {
        if (id != _sequence) return;
        if (index >= clips.Count)
        {
            _inSequence = false;
            completion?.Invoke();
            return;
        }
        var name = clips[index];
        onClipStart?.Invoke(index, name);
        // A lead-on clip not recorded yet falls back to the plain one (num_7_mid → num_7, vo_and → word_and).
        if (!HasClip(name) && StandIn(name) is { } standIn && HasClip(standIn)) name = standIn;

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

        var gap = BipCore.AudioCatalogue.LeadsOn(name) ? GapAfterLeadOn : GapBetweenClips;
        GetTree().CreateTimer(length + gap, processAlways: false).Timeout +=
            () => PlayClip(index + 1, clips, id, onClipStart, completion);
    }
}
