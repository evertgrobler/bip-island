namespace BipCore;

/// <summary>
/// One moment of Bip's day: a Morning Order card ("Brush your teeth", "Borsel jou tande") with its
/// words in both languages. Its clip has the same name in each language's folder.
/// </summary>
public sealed record DayCard(string Set, int N, string Picture, string Audio, string HomeText, string LearningText);

/// <summary>
/// Bip's Day (Language Island, "Bip se dag"): Bip says what he is doing in the language the child is
/// learning, and the child finds the picture (3, then 4). The moments are the Morning Order stories,
/// written and drawn once for both languages.
/// </summary>
public sealed class BipsDayGame : IMiniGame<BipsDayGame.Round, DayCard>
{
    public const string GameId = "bips_day";
    public const int ChoiceCount = 3;

    public sealed record Round(DayCard Target, IReadOnlyList<DayCard> Choices, HomeLanguage Learning) : IGameRound<DayCard>
    {
        public IReadOnlyList<string> UsedItems => [$"{Target.Set}_{Target.N}"];
    }

    public string Id => GameId;
    public GameEntry Entry { get; }
    public IReadOnlyList<GameSkin> Skins { get; } = [new("picture_cards", "Picture cards")];
    /// <summary>Every moment, with the band of its story.</summary>
    public IReadOnlyList<(DayCard Card, Band Band)> Cards { get; }
    private readonly HomeLanguage _learning;

    /// <param name="home">The child's content.</param>
    /// <param name="learning">The content of the language they're learning (its story words).</param>
    public BipsDayGame(ContentLibrary home, ContentLibrary learning)
    {
        Entry = home.EntryForGame(GameId);
        _learning = learning.Language;
        var theirs = learning.Sequences.Sets.ToDictionary(s => s.Id);
        Cards = home.Sequences.Sets
            .Where(set => theirs.ContainsKey(set.Id))
            .SelectMany(set => set.Cards.Select(card => (card, other: theirs[set.Id].Cards.FirstOrDefault(c => c.N == card.N), set)))
            .Where(x => x.other is not null && x.other.Picture == x.card.Picture)
            .Select(x => (new DayCard(x.set.Id, x.card.N, x.card.Picture, x.card.Audio, x.card.Text, x.other!.Text), x.set.Band))
            .ToList();
    }

    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var level = this.Level(learner);
        // A level can keep to the youngest stories (games.json "storiesUpTo"); without it, every story.
        var open = Cards.Where(c => level.StoriesUpTo is not { } upTo || c.Band <= upTo).Select(c => c.Card).ToList();
        if (rng.Pick(open.Where(c => !session.UsedItems.Contains($"{c.Set}_{c.N}")).ToList()) is not DayCard target) return null;
        var wanted = Math.Max(level.Choices ?? ChoiceCount, 2);
        // One card from each other story, and none that could pass for another choice or the answer:
        // stories repeat moments ("Brush your teeth" is in the morning and at bedtime).
        var picked = new List<DayCard>();
        foreach (var card in rng.Shuffled(open.Where(c => c.Set != target.Set)))
        {
            if (picked.Count >= wanted - 1) break;
            if (picked.Prepend(target).All(p => CanShareRound(p, card))) picked.Add(card);
        }
        if (picked.Count < wanted - 1) return null;
        return new Round(target, rng.Shuffled(picked.Prepend(target)), _learning);
    }

    /// <summary>Two moments can share a round when they're from different stories and neither their picture nor their words match.</summary>
    public static bool CanShareRound(DayCard a, DayCard b) =>
        a.Set != b.Set && a.Picture != b.Picture
        && !a.LearningText.Equals(b.LearningText, StringComparison.OrdinalIgnoreCase)
        && !a.HomeText.Equals(b.HomeText, StringComparison.OrdinalIgnoreCase);

    public bool IsCorrect(DayCard choice, Round round) => choice.Set == round.Target.Set && choice.N == round.Target.N;

    public string SkillId(Round round) => "lang_day";
}
