namespace BipCore;

/// <summary>
/// How Bip talks to a child of their age (owner, 18 October 2026: "what is appealing to a 5 year old
/// won't be appealing to a 10 year old"). Little (up to 6, or no age set) hears today's sparkling lines;
/// Big (7 and up) hears an upbeat, relaxed version of Bip's own lines: praise, hints, level and round
/// lines, greetings and the charging lines. Everything the child learns from (sounds, words, numbers,
/// sentences, game instructions) is one recording for every age. The band follows the child's age, not
/// their learning level, so an older child catching up is never spoken to like a four-year-old.
/// The age is the number a grown-up set in the parent area's Children tab (<c>SavedChild.Age</c>), not a
/// birthday, so it only moves up when a grown-up changes it; the Children tab asks them to on the birthday.
/// </summary>
public enum VoiceBand { Little, Big }

public static class VoiceBands
{
    /// <summary>The first age that hears the Big lines (Grade 2, Cambridge Stage 2).</summary>
    public const int BigFromAge = 7;
    public const string Suffix = "_big";
    public const int BigPraiseCount = 12;
    /// <summary>Fewer Big praise clips in the build than this and praise comes from the Little pool, so a half-recorded pool never repeats one line.</summary>
    public const int FewestBigPraise = 4;

    public static VoiceBand For(int? age) => age >= BigFromAge ? VoiceBand.Big : VoiceBand.Little;

    /// <summary>
    /// The lines with a Big version, by their Little (base) names; praise has its own Big pool. Only Bip's
    /// own vo_, hint_ and praise_ lines ever have one, so a word clip such as word_big is never taken for one.
    /// </summary>
    public static readonly IReadOnlyList<string> BigVersions =
    [
        .. AudioCatalogue.HintClips,
        VoiceLine.LevelUp, VoiceLine.LetsPractiseAgain, VoiceLine.RoundDone,
        VoiceLine.Welcome, VoiceLine.LettersIsland, VoiceLine.NumbersIsland, VoiceLine.WordsIsland, VoiceLine.CodingIsland,
        VoiceLine.ArtIsland, VoiceLine.LanguageIsland, VoiceLine.IslandSleeping, VoiceLine.WhoIsPlaying,
        VoiceLine.BipCharging, VoiceLine.DayDone,
    ];

    /// <summary><see cref="BigVersions"/> for looking up every clip the voice plays.</summary>
    private static readonly HashSet<string> BigVersionSet = new(BigVersions, StringComparer.Ordinal);

    public static readonly IReadOnlyList<string> BigPraiseClips =
        Enumerable.Range(1, BigPraiseCount).Select(n => $"praise_{n:00}{Suffix}").ToList();

    /// <summary>Every Big clip the voice script lists.</summary>
    public static IReadOnlyList<string> BigClips => [.. BigVersions.Select(BigClip), .. BigPraiseClips];

    public static string BigClip(string clip) => clip + Suffix;

    /// <summary>
    /// The clip to play for a band: a Big child hears the Big version when the build has it, else the
    /// Little one, so a line not recorded yet is never silent.
    /// </summary>
    public static string Pick(string clip, VoiceBand band, Func<string, bool> recorded) =>
        band == VoiceBand.Big && BigVersionSet.Contains(clip) && recorded(BigClip(clip)) ? BigClip(clip) : clip;

    /// <summary>
    /// The clip the voice really plays for <paramref name="clip"/>: a lead-on clip not recorded yet falls
    /// back to its plain one (num_7_mid → num_7, vo_and → word_and), then the band picks its version.
    /// </summary>
    public static string Resolve(string clip, VoiceBand band, Func<string, bool> recorded)
    {
        if (!recorded(clip) && AudioCatalogue.StandIn(clip) is { } standIn && recorded(standIn)) clip = standIn;
        return Pick(clip, band, recorded);
    }

    /// <summary>The praise a band picks from: the recorded Big pool once it has a few lines, else the Little pool.</summary>
    public static IReadOnlyList<string> PraisePool(VoiceBand band, Func<string, bool> recorded)
    {
        if (band != VoiceBand.Big) return AudioCatalogue.PraiseClips;
        var big = BigPraiseClips.Where(recorded).ToList();
        return big.Count >= FewestBigPraise ? big : AudioCatalogue.PraiseClips;
    }
}
