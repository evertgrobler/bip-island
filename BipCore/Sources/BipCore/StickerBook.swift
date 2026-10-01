/// The sticker book: every correct answer moves the child up a ladder of stars, stickers,
/// creatures and decorations (docs/GAMES.md). Stickers are earned, never bought, and the book
/// shows what isn't earned yet as grey outlines.
///
/// Stickers are derived from progress, not stored: one per phonics sound (mastered) and one per
/// mastered skill. Stars are the counter towards the next sticker: every 10 stars earn a page
/// visit, and Bip's mystery box adds bonus stars once a day.
public enum StickerBook {
    /// Every sticker that exists: a sound sticker per sound, then a skill sticker per skill.
    public static func allStickers(content: ContentLibrary) -> [String] {
        let sounds = PhonicsCourse(content).allSounds.map { "sound_\($0.id)" }
        let skills = content.skills.skills.map { "skill_\($0.id)" }
        return sounds + skills
    }

    /// Stickers earned so far: mastered sounds and mastered skills.
    public static func earned(progress: ChildProgress, content: ContentLibrary) -> Set<String> {
        var out = Set<String>()
        let course = PhonicsCourse(content)
        for sound in course.allSounds where progress.sounds.stage(of: sound) == .mastered {
            out.insert("sound_\(sound.id)")
        }
        for skill in content.skills.skills where progress.isMastered(skill.id) {
            out.insert("skill_\(skill.id)")
        }
        return out
    }

    /// Stickers still to find, shown as grey outlines.
    public static func missing(progress: ChildProgress, content: ContentLibrary) -> [String] {
        let got = earned(progress: progress, content: content)
        return allStickers(content: content).filter { !got.contains($0) }
    }

    /// Stars towards the next sticker page (10 stars a page).
    public static func starsTowardsNextPage(_ progress: ChildProgress) -> Int {
        progress.stars % ChildProgress.starsPerSticker
    }
}
