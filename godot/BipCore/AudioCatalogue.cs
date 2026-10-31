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
    // Block Towers (Numbers Island)
    public const string BuildTower = "vo_build_tower";
    public const string FindTower = "vo_find_tower";
    public const string TallestTower = "vo_tallest_tower";
    public const string ShortestTower = "vo_shortest_tower";
    public const string HowManyAltogether = "vo_how_many_altogether";
    public const string TakeAway = "vo_take_away";
    public const string HowManyLeft = "vo_how_many_left";
    public const string MakeTen = "vo_make_ten";
    public const string Double = "vo_double";
    public const string HowMany = "vo_how_many";
    public const string OddOrEven = "vo_odd_or_even";
    public const string Odd = "vo_odd";
    public const string BuildNumber = "vo_build_number";
    public const string WhatComesNext = "vo_what_comes_next";
    // Bip's Balance (Numbers Island)
    public const string Balance = "vo_balance";
    public const string Plus = "vo_plus";
    public const string EqualsWord = "vo_equals";
    public const string WhatNumber = "vo_what_number";
    public const string WhichNumber = "vo_which_number";
    public const string BalanceHelps = "vo_balance_helps";
    public const string CountOnTogether = "vo_count_on_together";
    public const string CountBackTogether = "vo_count_back_together";
    public const string StoryThereAre = "vo_story_there_are";
    public const string StoryComeAlong = "vo_story_come_along";
    public const string StoryGoAway = "vo_story_go_away";
    public const string StoryHowManyNow = "vo_story_how_many_now";
    /// <summary>"and" in the middle of a sum ("three and two"), not the reading word word_and.</summary>
    public const string And = "vo_and";
    // Picnic Plates (Numbers Island)
    public const string Picnic = "vo_picnic";
    public const string Put = "vo_put";
    public const string OnEachPlate = "vo_on_each_plate";
    /// <summary>"Share… twelve… between… two… plates."</summary>
    public const string Share = "vo_share";
    public const string Between = "vo_between";
    public const string Plates = "vo_plates";
    public const string HowManyEach = "vo_how_many_each";
    public const string GroupsOf = "vo_groups_of";
    public const string HowManyGroups = "vo_how_many_groups";
    public const string HowManyOnTray = "vo_how_many_on_tray";
    public const string OneEach = "vo_one_each";
    public const string CountInSteps = "vo_count_in_steps";
    public const string ClickTheBasket = "vo_click_the_basket";
    public const string CountGroups = "vo_count_groups";
    // Number Machine (Numbers Island)
    public const string NumberMachine = "vo_number_machine";
    public const string Times = "vo_times";
    public const string DividedBy = "vo_divided_by";
    public const string MachineOut = "vo_machine_out";
    public const string MachineIn = "vo_machine_in";
    public const string MachineRule = "vo_machine_rule";
    /// <summary>"It's a… times… ten… machine.": the rule, said for a child who can't read the card yet.</summary>
    public const string ItsA = "vo_its_a";
    public const string MachineEnd = "vo_machine_end";
    /// <summary>"Three… went in, twelve… came out."</summary>
    public const string WentIn = "vo_went_in";
    public const string CameOut = "vo_came_out";
    /// <summary>"…times four, or… times eight?": before the last of the cards read out.</summary>
    public const string Or = "vo_or";
    // Language Island
    public const string LanguageIsland = "vo_language_island";
    public const string ShowMe = "vo_show_me";
    public const string WhatIsIt = "vo_what_is_it";
    public const string BipsDay = "vo_bips_day";
    /// <summary>"In English, it's…" / "In Afrikaans is dit…": the hint before the word in the child's own language.</summary>
    public const string InOurLanguage = "vo_in_our_language";
    // Bip's Stories (Words Island)
    public const string BipsStories = "vo_bips_stories";
    public const string StoryQuestions = "vo_story_questions";
    public const string StoryLookAgain = "vo_story_look_again";

    /// <summary>Every instruction clip, in the order above.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        Welcome, IslandSleeping, LettersIsland, MeetNewSound, SayItWithMe, ClickToHearAgain, FindTheSound,
        PopTheLetter, LevelUp, LetsPractiseAgain, RoundDone, TraceLetter, FeedMonster, CountTap, QuickLook,
        SoundButtons, WordBuilder, WordRocket, MorningOrder, BipsPath, NumbersIsland, WordsIsland, CodingIsland, WhoIsPlaying,
        BipCharging, DayDone,
        ArtIsland, FindTheShape, WhichShapeFits, ShapeSides, StraightSides, ShapeCurved, ShapeTurn, ShapeCountTurns,
        ShapeRegular, PaintMake, PaintPredict, Make, MirrorSame, MirrorFinish, MirrorPegs, MirrorLine,
        BuildTower, FindTower, TallestTower, ShortestTower, HowManyAltogether, TakeAway, HowManyLeft, MakeTen,
        Double, HowMany, OddOrEven, Odd, BuildNumber, WhatComesNext,
        Balance, Plus, EqualsWord, WhatNumber, WhichNumber, BalanceHelps, CountOnTogether, CountBackTogether,
        StoryThereAre, StoryComeAlong, StoryGoAway, StoryHowManyNow, And,
        Picnic, Put, OnEachPlate, Share, Between, Plates, HowManyEach, GroupsOf, HowManyGroups, HowManyOnTray, OneEach, CountInSteps,
        ClickTheBasket, CountGroups,
        NumberMachine, Times, DividedBy, MachineOut, MachineIn, MachineRule, ItsA, MachineEnd, WentIn, CameOut, Or,
        LanguageIsland, ShowMe, WhatIsIt, BipsDay, InOurLanguage,
        BipsStories, StoryQuestions, StoryLookAgain,
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

    /// <summary>
    /// A word's clip: yo-yo → word_yoyo (file names have no hyphens), vlieër → word_vlieer (file names
    /// are plain ASCII, so a Mac writing é or ë decomposed can never miss the file).
    /// </summary>
    public static string WordClip(string word) => $"word_{PlainLetters(word.Replace("-", ""))}";

    /// <summary>The text without accents or diereses: ë → e, ê → e, ô → o.</summary>
    public static string PlainLetters(string text) =>
        string.Concat(text.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark))
            .Normalize(System.Text.NormalizationForm.FormC);
    public static string NumberClip(int n) => $"num_{n}";

    /// <summary>
    /// A number in the middle of a sentence ("seven… plus three"), said so it carries on rather than
    /// finishing; numbers above <paramref name="joinTo"/> (numbers.json spokenNumbers.joinTo, see
    /// <see cref="ContentLibrary.MidNumberClip"/>) use the plain clip.
    /// </summary>
    public static string MidNumberClip(int n, int joinTo) => n >= 0 && n <= joinTo ? $"num_{n}_mid" : NumberClip(n);

    /// <summary>
    /// A whole sentence with numbers in it, recorded as one clip (numbers.json spokenNumbers):
    /// SentenceClip("join", 3, 2) → vo_say_join_3_2. Games play it when it's in the build and join
    /// smaller clips otherwise.
    /// </summary>
    public static string SentenceClip(string id, int a, int? b = null) => b is { } second ? $"vo_say_{id}_{a}_{second}" : $"vo_say_{id}_{a}";

    /// <summary>The plain clip that stands in for a lead-on clip not recorded yet (num_7_mid → num_7, vo_and → word_and), or null.</summary>
    public static string? StandIn(string clip) =>
        clip.EndsWith("_mid", StringComparison.Ordinal) ? clip[..^"_mid".Length]
        : clip == VoiceLine.And ? WordClip("and") : null;

    /// <summary>Clips that lead straight into the next one, so the player leaves almost no gap after them.</summary>
    public static bool LeadsOn(string clip) =>
        clip.EndsWith("_mid", StringComparison.Ordinal)
        || clip is VoiceLine.And or VoiceLine.Plus or VoiceLine.TakeAway or VoiceLine.EqualsWord or VoiceLine.Make or VoiceLine.InOurLanguage;

    public static readonly IReadOnlyList<string> PraiseClips = Enumerable.Range(1, PraiseCount).Select(n => $"praise_{n:00}").ToList();
    /// <summary>The one hint that stands alone ("Look for the one that's wiggling!"); the others lead into a sound.</summary>
    public const string LookHint = "hint_02";

    public static readonly IReadOnlyList<string> HintClips = Enumerable.Range(1, HintCount).Select(n => $"hint_{n:00}").ToList();

    /// <summary>Clips written in audio/script.csv rather than generated from the content.</summary>
    public static IReadOnlyList<string> ScriptedClips => [.. VoiceLine.All, .. PraiseClips, .. HintClips, .. VoiceBands.BigClips];

    /// <summary>The naming contract: snd_, word_, num_, vo_, name_, money_, learn_ with lower-case keys; praise_NN and hint_NN, with _big for the older children's version.</summary>
    public static bool FollowsNamingContract(string name) => NamingContract().IsMatch(name);

    [GeneratedRegex(@"^((snd|word|num|vo|name|money|learn)_[a-z0-9_]+|(praise|hint)_[0-9]{2}(_big)?)$")]
    private static partial Regex NamingContract();
}
