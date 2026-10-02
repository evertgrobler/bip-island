namespace BipCore;

/// <summary>
/// Bip's Path: snap arrow blocks together to walk Bip to his battery, then press Go.
/// The round offers three finished strips — the working program and two broken ones that
/// crash or miss, checked by the same interpreter the scene steps through — so there is
/// exactly one right answer. Levels come from the content, so new puzzles need no new code.
/// </summary>
public sealed class BipsPathGame(ContentLibrary content) : IMiniGame<BipsPathGame.Round, IReadOnlyList<string>>
{
    public const string GameId = "bips_path";
    public const int ChoiceCount = 3;

    /// <param name="Choices">Candidate programs, in screen order. Exactly one reaches the battery.</param>
    public sealed record Round(GridLevel Level, IReadOnlyList<IReadOnlyList<string>> Choices, IReadOnlyList<string> Answer)
        : IGameRound<IReadOnlyList<string>>
    {
        public IReadOnlyList<string> UsedItems => [Level.Id];
    }

    private readonly List<GridLevel> _levels = content.Levels.Levels.Where(l => l.Game == GameId).ToList();

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } = [new("island", "Island"), new("snow", "Snow"), new("space", "Space")];

    /// <summary>
    /// Broken programs: the working program with one swapped block, that no longer reaches
    /// the battery. Verified by the interpreter, not by trust.
    /// </summary>
    internal static List<IReadOnlyList<string>> BrokenPrograms(GridLevel level, int count, IRandomSource rng)
    {
        var broken = new List<IReadOnlyList<string>>();
        if (level.OptimalProgram.Count == 0) return broken;
        for (var attempt = 0; attempt < 60 && broken.Count < count; attempt++)
        {
            var program = level.OptimalProgram.ToList();
            var at = rng.NextIndex(program.Count);
            var alternatives = level.Blocks.Where(b => b != program[at]).ToList();
            if (alternatives.Count == 0) continue;
            program[at] = alternatives[rng.NextIndex(alternatives.Count)];
            if (!program.SequenceEqual(level.OptimalProgram) && !broken.Any(b => b.SequenceEqual(program))
                && !GridWalker.ReachesGoal(program, level))
            {
                broken.Add(program);
            }
        }
        return broken;
    }

    /// <summary>Puzzles from the level's grid band (small grids first), then any the child's band allows.</summary>
    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var open = _levels.Where(l => !session.UsedItems.Contains(l.Id)).ToList();
        var gridBand = this.Level(learner).GridBand;
        var atLevel = gridBand is Band band ? open.Where(l => l.Band == band).ToList() : [];
        var ceiling = gridBand is Band g && g > learner.Band ? g : learner.Band;
        var fallback = open.Where(l => l.Band <= ceiling && !atLevel.Contains(l)).ToList();
        foreach (var level in rng.Shuffled(atLevel).Concat(rng.Shuffled(fallback)))
        {
            if (!GridWalker.ReachesGoal(level.OptimalProgram, level)) continue;
            var wrong = BrokenPrograms(level, ChoiceCount - 1, rng);
            if (wrong.Count != ChoiceCount - 1) continue;
            return new Round(level, rng.Shuffled(wrong.Prepend(level.OptimalProgram)), level.OptimalProgram);
        }
        return null;
    }

    public bool IsCorrect(IReadOnlyList<string> choice, Round round) => choice.SequenceEqual(round.Answer);

    public string SkillId(Round round)
    {
        if (round.Level.Tier.Contains("turn")) return "turns";
        return round.Level.OptimalLength <= 4 ? "directions" : "programs";
    }
}
