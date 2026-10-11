namespace BipCore;

/// <summary>How loud the background music plays, set by a grown-up in Settings.</summary>
public enum MusicLevel { Off, Quiet, Normal }

/// <summary>
/// Which music plays where, and how loud. Calm loops play on the map and the island screens only;
/// games, breaks and the other screens are quiet, so nothing competes with the letter sounds. The
/// music dips while the narrator speaks. Tracks are Resources/Music/&lt;track&gt;.m4a (music_map,
/// music_letters, …); a track not in the build simply doesn't play.
/// </summary>
public static class MusicRules
{
    public const string MapTrack = "music_map";

    /// <summary>The island screen's loop: music_letters, music_numbers, music_words, music_coding, music_art.</summary>
    public static string IslandTrack(Island island) => "music_" + island.ToString().ToLowerInvariant();

    /// <summary>Every track the game can play, for the self-test and the validator.</summary>
    public static IReadOnlyList<string> AllTracks { get; } =
        [MapTrack, .. Enum.GetValues<Island>().Select(IslandTrack)];

    /// <summary>The share of the music's volume left while the narrator speaks.</summary>
    public const float DuckedShare = 0.3f;

    /// <summary>The music's linear volume (0 to 1) for the grown-up's setting, lower while the narrator speaks.</summary>
    public static float Volume(MusicLevel level, bool narratorSpeaking)
    {
        var full = level switch
        {
            MusicLevel.Normal => 0.45f,
            MusicLevel.Quiet => 0.22f,
            _ => 0f,
        };
        return narratorSpeaking ? full * DuckedShare : full;
    }

    /// <summary>The next setting when a grown-up taps the music button: Normal → Quiet → Off → Normal.</summary>
    public static MusicLevel Next(MusicLevel level) => level switch
    {
        MusicLevel.Normal => MusicLevel.Quiet,
        MusicLevel.Quiet => MusicLevel.Off,
        _ => MusicLevel.Normal,
    };

    public static string Label(MusicLevel level) => level switch
    {
        MusicLevel.Normal => "On",
        MusicLevel.Quiet => "Quiet",
        _ => "Off",
    };
}
