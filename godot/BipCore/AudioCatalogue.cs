using System.Text.RegularExpressions;

namespace BipCore;

/// <summary>Spoken instruction clips (vo_&lt;key&gt;). The exact words are in audio/script.csv.</summary>
public static class VoiceLine
{
    public const string Welcome = "vo_welcome";
    public const string IslandSleeping = "vo_island_sleeping";
    public const string LettersIsland = "vo_letters_island";
    public const string MeetNewSound = "vo_meet_new_sound";
    public const string SayItWithMe = "vo_say_it_with_me";
    public const string ClickToHearAgain = "vo_click_to_hear_again";
    public const string FindTheSound = "vo_find_the_sound";
    public const string PopTheLetter = "vo_pop_the_letter";
    public const string LevelUp = "vo_level_up";
    public const string LetsPractiseAgain = "vo_lets_practise_again";
    public const string RoundDone = "vo_round_done";
    public const string TraceLetter = "vo_trace_letter";
    public const string FeedMonster = "vo_feed_monster";
    public const string CountTap = "vo_count_tap";
    public const string QuickLook = "vo_quick_look";
    public const string SoundButtons = "vo_sound_buttons";
    public const string WordBuilder = "vo_build_word";
    public const string WordRocket = "vo_word_rocket";
    public const string MorningOrder = "vo_morning_order";
    public const string BipsPath = "vo_bips_path";
    public const string NumbersIsland = "vo_numbers_island";
    public const string WordsIsland = "vo_words_island";
    public const string CodingIsland = "vo_coding_island";
    public const string WhoIsPlaying = "vo_who_is_playing";
    public const string BipCharging = "vo_bip_charging";
    public const string DayDone = "vo_day_done";
    // Art Island
    public const string ArtIsland = "vo_art_island";
    public const string FindTheShape = "vo_find_the_shape";
    public const string WhichShapeFits = "vo_which_shape_fits";
    public const string ShapeSides = "vo_shape_sides";
    public const string StraightSides = "vo_straight_sides";
    public const string ShapeCurved = "vo_shape_curved";
    public const string ShapeTurn = "vo_shape_turn";
    public const string ShapeCountTurns = "vo_shape_count_turns";
    public const string ShapeRegular = "vo_shape_regular";
    public const string PaintMake = "vo_paint_make";
    public const string PaintPredict = "vo_paint_predict";
    public const string Make = "vo_make";
    public const string MirrorSame = "vo_mirror_same";
    public const string MirrorFinish = "vo_mirror_finish";
    public const string MirrorPegs = "vo_mirror_pegs";
    public const string MirrorLine = "vo_mirror_line";

    /// <summary>Every instruction clip, in the order above.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        Welcome, IslandSleeping, LettersIsland, MeetNewSound, SayItWithMe, ClickToHearAgain, FindTheSound,
        PopTheLetter, LevelUp, LetsPractiseAgain, RoundDone, TraceLetter, FeedMonster, CountTap, QuickLook,
        SoundButtons, WordBuilder, WordRocket, MorningOrder, BipsPath, NumbersIsland, WordsIsland, CodingIsland, WhoIsPlaying,
        BipCharging, DayDone,
        ArtIsland, FindTheShape, WhichShapeFits, ShapeSides, StraightSides, ShapeCurved, ShapeTurn, ShapeCountTurns,
        ShapeRegular, PaintMake, PaintPredict, Make, MirrorSame, MirrorFinish, MirrorPegs, MirrorLine,
    ];
}

/// <summary>
/// Audio clip names, following the file-name contract in CLAUDE.md.
///
/// Two lists feed the voice script: game instructions, praise and hints live in audio/script.csv;
/// every sound and word clip the content needs is in Content/asset_manifest.json (with its text).
/// </summary>
public static partial class AudioCatalogue
{
    public const int PraiseCount = 10;
    public const int HintCount = 4;

    public static string WordClip(string word) => $"word_{word}";
    public static string NumberClip(int n) => $"num_{n}";

    public static readonly IReadOnlyList<string> PraiseClips = Enumerable.Range(1, PraiseCount).Select(n => $"praise_{n:00}").ToList();
    /// <summary>The one hint that stands alone ("Look for the one that's wiggling!"); the others lead into a sound.</summary>
    public const string LookHint = "hint_02";

    public static readonly IReadOnlyList<string> HintClips = Enumerable.Range(1, HintCount).Select(n => $"hint_{n:00}").ToList();

    /// <summary>Clips written in audio/script.csv rather than generated from the content.</summary>
    public static IReadOnlyList<string> ScriptedClips => [.. VoiceLine.All, .. PraiseClips, .. HintClips];

    /// <summary>The naming contract: snd_, word_, num_, vo_, name_, money_ with lower-case keys; praise_NN and hint_NN.</summary>
    public static bool FollowsNamingContract(string name) => NamingContract().IsMatch(name);

    [GeneratedRegex(@"^((snd|word|num|vo|name|money)_[a-z0-9_]+|(praise|hint)_[0-9]{2})$")]
    private static partial Regex NamingContract();
}
