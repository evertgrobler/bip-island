namespace BipCore;

/// <summary>
/// The sticker book: every correct answer moves the child up a ladder of stars, stickers,
/// creatures and decorations (docs/GAMES.md). Stickers are earned, never bought, and the book
/// shows what isn't earned yet as grey outlines.
///
/// Two kinds of sticker:
/// - Picture stickers (the first pages): won, not learnt. One for every 10 stars and one for every
///   finished game, so a child sees a reward on their very first day. Each is a picture from
///   words.json, matching the sound just practised when it can ("a sun for s").
/// - Learning stickers: one per phonics sound and one per skill, earned by mastering it (derived
///   from progress, not stored).
/// Bip's mystery box adds bonus stars once a day.
/// </summary>
public static class StickerBook
{
    /// <summary>Every sticker that exists: a sound sticker per sound, then a skill sticker per skill.</summary>
    public static List<string> AllStickers(ContentLibrary content) =>
        new PhonicsCourse(content).AllSounds.Select(s => $"sound_{s.Id}")
            .Concat(content.Skills.Skills.Select(s => $"skill_{s.Id}"))
            .ToList();

    /// <summary>Stickers earned so far: mastered sounds and mastered skills.</summary>
    public static HashSet<string> Earned(ChildProgress progress, ContentLibrary content)
    {
        var earned = new HashSet<string>();
        foreach (var sound in new PhonicsCourse(content).AllSounds)
        {
            if (progress.Sounds.Stage(sound) == SoundStage.Mastered) earned.Add($"sound_{sound.Id}");
        }
        foreach (var skill in content.Skills.Skills)
        {
            if (progress.IsMastered(skill.Id)) earned.Add($"skill_{skill.Id}");
        }
        return earned;
    }

    /// <summary>Stickers still to find, shown as grey outlines.</summary>
    public static List<string> Missing(ChildProgress progress, ContentLibrary content)
    {
        var got = Earned(progress, content);
        return AllStickers(content).Where(s => !got.Contains(s)).ToList();
    }

    /// <summary>Stars towards the next picture sticker (10 stars a sticker).</summary>
    public static int StarsTowardsNextPage(ChildProgress progress) => progress.Stars % ChildProgress.StarsPerSticker;

    /// <summary>Every picture sticker there is: each picture word in words.json, in list order.</summary>
    public static List<string> AllPictureStickers(ContentLibrary content) =>
        PictureWords(content).Select(w => w.Picture!).ToList();

    /// <summary>The first word in words.json for each picture: the one set both the book and the awards use.</summary>
    private static IEnumerable<Word> PictureWords(ContentLibrary content) =>
        content.Words.Words.Where(w => w.Picturable && w.Picture != null).DistinctBy(w => w.Picture);

    /// <summary>
    /// The next picture sticker to win: one the child hasn't got yet, starting with the sound they
    /// just practised ("a sun for s"), else any at random. Null once the whole set is collected.
    /// </summary>
    public static string? NextPictureSticker(ChildProgress progress, ContentLibrary content, string? soundId, IRandomSource rng)
    {
        var have = progress.PictureStickers.ToHashSet();
        var left = PictureWords(content).Where(w => !have.Contains(w.Picture!)).ToList();
        if (left.Count == 0) return null;
        var matching = soundId == null ? [] : left.Where(w => w.FirstSound == soundId).ToList();
        return rng.Pick(matching.Count > 0 ? matching : left)!.Picture;
    }

    /// <summary>
    /// Turns every 10 stars into a picture sticker (and, with <paramref name="finishedGame"/>, adds one
    /// for the game just finished). Returns the stickers won, in order.
    /// </summary>
    public static List<string> AwardStickers(ChildProgress progress, ContentLibrary content, string? soundId, bool finishedGame, IRandomSource rng)
    {
        var won = new List<string>();
        for (var owed = progress.StarStickersOwed; owed > 0; owed--)
        {
            if (NextPictureSticker(progress, content, soundId, rng) is not { } sticker) break;
            progress.AddPictureSticker(sticker, forStars: true);
            won.Add(sticker);
        }
        if (finishedGame && NextPictureSticker(progress, content, soundId, rng) is { } prize)
        {
            progress.AddPictureSticker(prize, forStars: false);
            won.Add(prize);
        }
        return won;
    }
}
