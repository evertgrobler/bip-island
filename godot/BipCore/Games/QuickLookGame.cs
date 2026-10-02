namespace BipCore;

/// <summary>
/// Quick Look: dots or objects flash briefly — how many, without counting?
/// Same question shape as Count &amp; Tap (one right numeral out of three); only what the child
/// sees changes, from animals to tap to dice, dominoes and ten-frames.
/// </summary>
public sealed class QuickLookGame(ContentLibrary content) : IMiniGame<QuickLookGame.Round, int>
{
    public const string GameId = "quick_look";
    /// <summary>The standard flash when a level doesn't say.</summary>
    public const double DefaultFlashSeconds = 2.0;

    /// <param name="FlashSeconds">How long the dots show, in seconds (shorter at higher levels).</param>
    public sealed record Round(int Count, IReadOnlyList<int> Choices, double FlashSeconds) : IGameRound<int>
    {
        public IReadOnlyList<string> UsedItems => [];
    }

    private readonly NumbersFile _numbers = content.Numbers;

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } = [new("dice", "Dice"), new("dominoes", "Dominoes"), new("ten_frames", "Ten-frames")];

    /// <summary>1 up to the level's number (3, 5, 8, 10), or what the band sees at a glance.</summary>
    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var step = this.Level(learner);
        var seeTo = Math.Min(step.CountTo ?? _numbers.Bands.GetValueOrDefault(learner.Band.Key())?.SubitiseTo ?? 5, 10);
        if (seeTo < 1) return null;
        var count = rng.NextInt(1, seeTo);
        var flash = step.FlashTenths is int tenths ? tenths / 10.0 : DefaultFlashSeconds;
        return new Round(count, NumeralChoices.Three(count, rng), flash);
    }

    public bool IsCorrect(int choice, Round round) => choice == round.Count;

    public string SkillId(Round round)
    {
        if (round.Count <= 5) return "subitise_5";
        return round.Count <= 10 ? "subitise_10" : "estimate";
    }
}
