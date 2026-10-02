namespace BipCore;

/// <summary>
/// Morning Order: put picture cards in order — wake up, brush teeth, eat breakfast.
/// The round offers three finished orders — the routine and two muddles — so there is exactly
/// one right answer. Routines come from the content, so new ones arrive with no new code.
/// </summary>
public sealed class MorningOrderGame(ContentLibrary content) : IMiniGame<MorningOrderGame.Round, IReadOnlyList<SequenceCard>>
{
    public const string GameId = "morning_order";
    public const int ChoiceCount = 3;
    /// <summary>The fewest cards a story can be trimmed to: three cards still give two different muddles.</summary>
    public const int MinimumCards = 3;

    /// <param name="Choices">Candidate orders, in screen order. Exactly one runs first-to-last.</param>
    public sealed record Round(SequenceSet Set, IReadOnlyList<IReadOnlyList<SequenceCard>> Choices, IReadOnlyList<SequenceCard> Answer)
        : IGameRound<IReadOnlyList<SequenceCard>>
    {
        public IReadOnlyList<string> UsedItems => [Set.Id];
    }

    private readonly List<SequenceSet> _sets = content.Sequences.Sets;

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } =
        [new("picture_cards", "Picture cards"), new("recipe_book", "Recipe book"), new("garden_bed", "Garden bed")];

    /// <summary>The right order: cards sorted by their number.</summary>
    public static List<SequenceCard> CorrectOrder(SequenceSet set) => set.Cards.OrderBy(c => c.N).ToList();

    /// <summary>
    /// A story cut to its first <paramref name="limit"/> steps (the whole story if it's already short enough).
    /// Early levels ask for three steps, later ones for the whole story.
    /// </summary>
    public static SequenceSet Trimmed(SequenceSet set, int? limit)
    {
        var cards = CorrectOrder(set);
        if (limit is not int most || cards.Count <= most) return set;
        return set with { Cards = cards.Take(Math.Max(most, MinimumCards)).ToList() };
    }

    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var limit = this.Level(learner).Cards;
        var fresh = rng.Shuffled(_sets.Where(s => s.Band <= learner.Band && !session.UsedItems.Contains(s.Id)));
        foreach (var whole in fresh)
        {
            var set = Trimmed(whole, limit);
            var answer = CorrectOrder(set);
            var wrong = new List<IReadOnlyList<SequenceCard>>();
            for (var attempt = 0; attempt < 30 && wrong.Count < ChoiceCount - 1; attempt++)
            {
                var shuffled = rng.Shuffled(set.Cards);
                if (!shuffled.SequenceEqual(answer) && !wrong.Any(w => w.SequenceEqual(shuffled))) wrong.Add(shuffled);
            }
            if (wrong.Count != ChoiceCount - 1) continue;
            return new Round(set, rng.Shuffled(wrong.Prepend(answer)), answer);
        }
        return null;
    }

    public bool IsCorrect(IReadOnlyList<SequenceCard> choice, Round round) => choice.SequenceEqual(round.Answer);

    public string SkillId(Round round) => "sequencing";
}
