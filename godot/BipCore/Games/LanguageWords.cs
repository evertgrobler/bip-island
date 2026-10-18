namespace BipCore;

/// <summary>
/// A Language Island word round (Show Me and What Is It?): one word to find among others, all in the
/// language the child is learning.
/// </summary>
/// <param name="Learning">The language the words are said in (the one that isn't the child's home language).</param>
public sealed record LanguageWordRound(LanguageWord Target, IReadOnlyList<LanguageWord> Choices, HomeLanguage Learning)
    : IGameRound<LanguageWord>
{
    public IReadOnlyList<string> UsedItems => [Target.Id];
}

/// <summary>The words the Language Island's word games deal from (Content/language/words.json).</summary>
public static class LanguageWords
{
    /// <summary>Words whose theme is open at this game level.</summary>
    public static List<LanguageWord> OpenAt(ContentLibrary content, int level)
    {
        var open = content.LanguageWords.Themes.Where(t => t.FromLevel <= level).Select(t => t.Id).ToHashSet();
        return content.LanguageWords.Words.Where(w => open.Contains(w.Theme)).ToList();
    }

    /// <summary>
    /// Two words can share a round when nothing about them could be mistaken: different pictures and
    /// different words in both languages ("hand" is "hand" in both, so it never meets itself).
    /// </summary>
    public static bool CanShareRound(LanguageWord a, LanguageWord b) =>
        a.Picture != b.Picture && !a.En.Equals(b.En, StringComparison.OrdinalIgnoreCase)
                               && !a.Af.Equals(b.Af, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A fresh word and <paramref name="choices"/> - 1 others. From the second level the others come
    /// from the same theme first (red among colours), which is harder than red among animals.
    /// </summary>
    public static LanguageWordRound? MakeRound(ContentLibrary content, Learner learner, int choices, GameSession session, IRandomSource rng)
    {
        var open = OpenAt(content, learner.GameLevel);
        if (rng.Pick(open.Where(w => !session.UsedItems.Contains(w.Id)).ToList()) is not LanguageWord target) return null;
        var others = rng.Shuffled(open.Where(w => CanShareRound(w, target)));
        var ordered = learner.GameLevel > 0 ? others.Where(w => w.Theme == target.Theme).Concat(others.Where(w => w.Theme != target.Theme)) : others;
        var picked = new List<LanguageWord>();
        foreach (var word in ordered)
        {
            if (picked.Count >= choices - 1) break;
            if (picked.All(p => CanShareRound(p, word))) picked.Add(word);
        }
        if (picked.Count < choices - 1) return null;
        return new LanguageWordRound(target, rng.Shuffled(picked.Prepend(target)), Languages.Learning(content.Language));
    }
}
