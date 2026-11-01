namespace BipCore;

/// <summary>
/// Number Machine (Numbers Island): Bip's machine with a rule card on the front (× 2, × 5, ÷ 4). A number
/// rolls in, the handle turns, the machine shows the groups (6 into the × 2 machine: six groups of two)
/// and the child picks what comes out from three cards. Thinking like a machine (in, rule, out) links
/// to Coding Island. Kinds of question (games.json "modes"), with the machine rules in each level's
/// "steps":
/// <list type="bullet">
/// <item>out: a number goes in, what comes out? (the tables: 2Ni.07, 3Ni.05, 3Ni.06)</item>
/// <item>in: what came out is shown; what went in? (grouping, 2Ni.06)</item>
/// <item>divide: the ÷ machine gives back what went into the × machine (3Ni.03, 3Ni.07)</item>
/// <item>rule: two numbers went in and came out; which rule card is the machine's? (3Ni.03, 3Ni.05)</item>
/// </list>
/// What goes in (or comes out of the ÷ machine) is 2 to 10, so the most that comes out is 10 × 10.
/// </summary>
public sealed class NumberMachineGame(ContentLibrary content) : IMiniGame<NumberMachineGame.Round, int>
{
    public const string GameId = "number_machine";
    public const int LeastIn = 2;
    public const int MostIn = 10;
    /// <summary>The tables a level uses when games.json names none (Cambridge stage 2).</summary>
    public static readonly IReadOnlyList<int> StageTwoTables = [2, 5, 10];
    /// <summary>Numbers that go in on the rule level's cards: two examples.</summary>
    public const int Examples = 2;

    public enum Mode { Out, In, Divide, Rule }

    /// <param name="In">What goes into the × machine (for the ÷ machine, what comes out).</param>
    /// <param name="Rule">The number on the rule card (× Rule, or ÷ Rule).</param>
    /// <param name="Inputs">The rule level: the numbers that went in (In is the first).</param>
    public sealed record Round(Mode Mode, int In, int Rule, IReadOnlyList<int> Choices, IReadOnlyList<int>? Inputs = null) : IGameRound<int>
    {
        /// <summary>What the × machine makes: In × Rule (what goes into the ÷ machine).</summary>
        public int Product => In * Rule;

        public int Answer => Mode switch
        {
            Mode.Out => Product,
            Mode.Rule => Rule,
            _ => In,
        };

        public IReadOnlyList<string> UsedItems => Mode == Mode.Rule
            ? [$"Rule:{Rule}:{string.Join(",", Inputs ?? [])}"]
            : [$"{Mode}:{In}x{Rule}"];
    }

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } = [new("workshop", "Bip's workshop")];

    public static Mode ParseMode(string mode) => Enum.Parse<Mode>(mode, ignoreCase: true);

    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var level = this.Level(learner);
        var rules = level.Steps is { Count: > 0 } steps ? steps : StageTwoTables;
        foreach (var mode in rng.Shuffled((level.Modes ?? ["out"]).Select(ParseMode)))
        {
            var options = (from rule in rules from n in Enumerable.Range(LeastIn, MostIn - LeastIn + 1) select (n, rule)).ToList();
            if (mode == Mode.Rule)
            {
                // Three rule cards need three different rules.
                if (rules.Distinct().Count() < 3) continue;
                var fresh = options.Where(o => !session.UsedItems.Any(u => u.StartsWith($"Rule:{o.rule}:", StringComparison.Ordinal))).ToList();
                if (!rng.TryPick(fresh, out var pickRule) && !rng.TryPick(options, out pickRule)) continue;
                var inputs = rng.Shuffled(Enumerable.Range(LeastIn, MostIn - LeastIn + 1)).Take(Examples).ToList();
                var cards = rng.Shuffled(rng.Shuffled(rules.Distinct().Where(r => r != pickRule.rule)).Take(2).Prepend(pickRule.rule));
                return new Round(mode, inputs[0], pickRule.rule, cards, inputs);
            }
            var unused = options.Where(o => !session.UsedItems.Contains($"{mode}:{o.n}x{o.rule}")).ToList();
            if (!rng.TryPick(unused, out var pick) && !rng.TryPick(options, out pick)) continue;
            var round = new Round(mode, pick.n, pick.rule, []);
            return round with { Choices = NearChoices.Make(round.Answer, mode == Mode.Out ? pick.rule : 1, rng) };
        }
        return null;
    }

    public bool IsCorrect(int choice, Round round) => choice == round.Answer;

    public string SkillId(Round round) => round.Mode switch
    {
        Mode.Out => StageTwoTables.Contains(round.Rule) ? "count_steps" : "tables_3_4_8",
        Mode.In => "share_group",
        _ => "times_divide",
    };

    // What Bip says. Plain lists of clips, so the tests can check each one has a recording.

    /// <summary>
    /// The question, with every number on the screen said, so a child who can't read them yet can play:
    /// "Six times two. What comes out of the machine?"; "It's a times ten machine. Seventy came out. What
    /// went into the machine?"; "Three went in, twelve came out. Four went in, sixteen came out. What does
    /// the machine do? Find its card. Times two, times four, or times eight?" (the cards in their order).
    /// </summary>
    public List<string> Prompt(Round round) => round.Mode switch
    {
        Mode.Out => [content.MidNumberClip(round.In), VoiceLine.Times, AudioCatalogue.NumberClip(round.Rule), VoiceLine.MachineOut],
        Mode.In => [VoiceLine.ItsA, VoiceLine.Times, content.MidNumberClip(round.Rule), VoiceLine.MachineEnd,
                    content.MidNumberClip(round.Product), VoiceLine.CameOut, VoiceLine.MachineIn],
        Mode.Divide => [content.MidNumberClip(round.Product), VoiceLine.DividedBy, AudioCatalogue.NumberClip(round.Rule), VoiceLine.MachineOut],
        _ => [.. (round.Inputs ?? [round.In]).SelectMany(n => new[]
                 { content.MidNumberClip(n), VoiceLine.WentIn, content.MidNumberClip(n * round.Rule), VoiceLine.CameOut }),
              VoiceLine.MachineRule, .. RuleCards(round.Choices)],
    };

    /// <summary>The rule cards read out in order: "times two, times four, or times eight".</summary>
    private List<string> RuleCards(IReadOnlyList<int> cards) =>
    [
        .. cards.SkipLast(1).SelectMany(c => new[] { VoiceLine.Times, content.MidNumberClip(c) }),
        VoiceLine.Or, VoiceLine.Times, AudioCatalogue.NumberClip(cards[^1]),
    ];

    /// <summary>After a right answer, the whole sentence: "six times two equals twelve", "twenty divided by four equals five", "times four".</summary>
    public List<string> SayWhenRight(Round round) => round.Mode switch
    {
        Mode.Divide => [content.MidNumberClip(round.Product), VoiceLine.DividedBy, content.MidNumberClip(round.Rule), VoiceLine.EqualsWord,
                        AudioCatalogue.NumberClip(round.In)],
        Mode.Rule => [VoiceLine.Times, AudioCatalogue.NumberClip(round.Rule)],
        _ => [content.MidNumberClip(round.In), VoiceLine.Times, content.MidNumberClip(round.Rule), VoiceLine.EqualsWord,
              AudioCatalogue.NumberClip(round.Product)],
    };

    /// <summary>
    /// The spoken hint after two misses: count in steps of the rule, as the machine does, up to what
    /// the × machine makes ("Let's count in steps of 5: 5, 10, 15, 20, 25, 30"). For what went in, the
    /// ÷ machine and the rule, the child counts the steps (the groups the machine shows) to find it.
    /// </summary>
    public List<string> Hint(Round round)
    {
        var steps = Enumerable.Range(1, round.In)
            .Select(i => i < round.In ? content.MidNumberClip(i * round.Rule) : AudioCatalogue.NumberClip(i * round.Rule));
        return [VoiceLine.CountInSteps, AudioCatalogue.NumberClip(round.Rule), .. steps];
    }
}
