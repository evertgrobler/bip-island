namespace BipCore;

/// <summary>
/// Bip's suggestion of what to play next, across every island. The child always picks
/// freely — one island glows, and Bip nudges towards another island when one island
/// dominates recent play. Never forced.
/// </summary>
/// <param name="IsNudge">True when Bip is steering away from an over-played island ("How about some numbers?").</param>
public sealed record IslandSuggestion(string GameId, Island Island, bool IsNudge);

/// <summary>
/// Picks the glowing island. Rules, in order: a skill due for review comes first; otherwise
/// the island played least lately; and when the last three visits were all one island, Bip
/// suggests another one instead.
/// </summary>
public sealed class PlayRecommender(ContentLibrary content)
{
    /// <summary>How many same-island visits in a row trigger Bip's balancing nudge.</summary>
    public const int SameIslandVisitsBeforeNudge = 3;

    /// <summary>Games the child can open: at least one listed skill is unlocked.</summary>
    public List<GameEntry> UnlockedGames(ChildProgress progress, Band startingBand) =>
        content.Games.Games.Where(entry => entry.Skills.Any(id =>
            content.Skill(id) is Skill skill && progress.IsUnlocked(skill, startingBand, content.Skill))).ToList();

    public IslandSuggestion? Suggest(ChildProgress progress, Band startingBand, int day, MasteryRules rules, IRandomSource rng)
    {
        var open = UnlockedGames(progress, startingBand);
        if (open.Count == 0) return null;

        // 1. Reviews first: a mastered skill that is due, through one of its games.
        var due = open.Where(entry => entry.Skills.Any(id => progress.IsDueForReview(id, day, rules))).ToList();
        if (rng.Pick(due) is GameEntry review) return new IslandSuggestion(review.Id, review.Island, false);

        // 2. The island played least lately (never played counts as longest ago). When the last
        // three visits were all one island, suggesting another one is Bip's balancing nudge.
        int LastPlayed(Island island) => progress.RecentGames.FindLastIndex(id => content.Game(id)?.Island == island);
        var pick = IslandExtensions.All.OrderBy(LastPlayed).First();
        if (rng.Pick(open.Where(g => g.Island == pick).ToList()) is not GameEntry game) return null;

        var recent = progress.RecentGames.TakeLast(SameIslandVisitsBeforeNudge).ToList();
        Island? dominated = recent.Count == SameIslandVisitsBeforeNudge
            ? recent.Select(id => content.Game(id)?.Island).FirstOrDefault(island => island is not null)
            : null;
        var dominatedAll = dominated is Island dom && recent.All(id => content.Game(id)?.Island == dom);
        var isNudge = dominatedAll && pick != dominated;
        return new IslandSuggestion(game.Id, pick, isNudge);
    }
}
