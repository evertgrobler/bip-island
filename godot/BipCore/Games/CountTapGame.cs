namespace BipCore;

/// <summary>
/// Count &amp; Tap: tap each animal as Bip counts aloud, then tap the numeral that says how many.
/// Real objects first, symbols after: the scene shows the animals, the round checks the numeral.
/// </summary>
public sealed class CountTapGame(ContentLibrary content) : IMiniGame<CountTapGame.Round, int>
{
    public const string GameId = "count_and_tap";

    public sealed record Round(int Count, CountingObject Object, IReadOnlyList<int> Choices) : IGameRound<int>
    {
        public IReadOnlyList<string> UsedItems => [];
    }

    private readonly NumbersFile _numbers = content.Numbers;

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } = [new("ducks", "Ducks"), new("sheep", "Sheep"), new("ladybirds", "Ladybirds")];

    /// <summary>
    /// 1 up to the level's number (5, 10, 15, 20), or what the band counts to. At most 20 here;
    /// bigger numbers live in the later Numbers games.
    /// </summary>
    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var countTo = Math.Min(this.Level(learner).CountTo ?? _numbers.Bands.GetValueOrDefault(learner.Band.Key())?.CountTo ?? 10, 20);
        if (countTo < 1 || rng.Pick(_numbers.CountingObjects) is not CountingObject countingObject) return null;
        var count = rng.NextInt(1, countTo);
        return new Round(count, countingObject, NumeralChoices.Three(count, rng));
    }

    public bool IsCorrect(int choice, Round round) => choice == round.Count;

    public string SkillId(Round round) => round.Count <= 10 ? "count_10" : "count_20";
}
