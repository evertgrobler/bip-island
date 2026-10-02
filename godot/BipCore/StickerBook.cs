namespace BipCore;

/// <summary>
/// The sticker book: every correct answer moves the child up a ladder of stars, stickers,
/// creatures and decorations (docs/GAMES.md). Stickers are earned, never bought, and the book
/// shows what isn't earned yet as grey outlines.
///
/// Stickers are derived from progress, not stored: one per phonics sound (mastered) and one per
/// mastered skill. Stars are the counter towards the next sticker: every 10 stars earn a page
/// visit, and Bip's mystery box adds bonus stars once a day.
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

    /// <summary>Stars towards the next sticker page (10 stars a page).</summary>
    public static int StarsTowardsNextPage(ChildProgress progress) => progress.Stars % ChildProgress.StarsPerSticker;
}
