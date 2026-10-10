namespace BipCore;

/// <summary>
/// Block Towers (Numbers Island): numbers as towers of cubes the child builds, joins, splits and
/// compares. Bip's own hand-drawn building blocks, with no faces or characters. Kinds of question
/// (games.json "modes"):
/// <list type="bullet">
/// <item>build: "Build a tower of 4." Tap the pile to add cubes, then the tick. Choices are tower heights.</item>
/// <item>which: find the tower of 7, or the tallest or shortest of three. Choices are the three heights.</item>
/// <item>join: a tower of 3 jumps onto a tower of 2: "3 and 2 make…?" Choices are numerals.</item>
/// <item>takeaway: "6 take away 2": the child taps 2 cubes off, then how many are left? Numerals.</item>
/// <item>maketen: a tower of 6 and room up to 10: add cubes to make 10. Choices are cubes added.</item>
/// <item>doubles: two towers of 4 side by side: "Double 4 makes…?" Numerals.</item>
/// <item>oddeven: the tower splits into pairs. Odd or even? Choices "odd" and "even".</item>
/// <item>tensones: build 34 from tens (ten cubes snapped together) and ones. Choices are values 0 to 99.</item>
/// <item>pattern: towers of 2, 4, 6: which comes next? Choices are three heights.</item>
/// </list>
/// Towers taller than 10 stand as a column of ten with the rest beside it, so 14 is "ten and four".
/// </summary>
public sealed class BlockTowersGame(ContentLibrary content) : IMiniGame<BlockTowersGame.Round, string>
{
    public const string GameId = "block_towers";
    public const string Odd = "odd";
    public const string Even = "even";
    /// <summary>A ten: ten cubes snapped together.</summary>
    public const int Ten = 10;

    public enum Mode { Build, Which, Join, TakeAway, MakeTen, Doubles, OddEven, TensOnes, Pattern }

    /// <summary>Which rounds: what the child is looking for.</summary>
    public enum Ask { Number, Tallest, Shortest }

    /// <param name="Target">The answer: the height to build, the total, what's left, the cubes to add, the value.</param>
    /// <param name="A">Join, doubles: the first tower. Take away, make ten: the tower to start with. Odd or even: the tower.</param>
    /// <param name="B">Join: the second tower. Take away: how many come off.</param>
    /// <param name="Towers">Which: the three towers in screen order. Pattern: the three towers so far.</param>
    /// <param name="MostCubes">Building rounds: the tallest the tower can get (the pile runs out there).</param>
    public sealed record Round(Mode Mode, int Target, IReadOnlyList<string> Choices, int A = 0, int B = 0,
                               IReadOnlyList<int>? Towers = null, Ask Ask = Ask.Number, int MostCubes = 0,
                               IReadOnlyList<string>? Used = null) : IGameRound<string>
    {
        public IReadOnlyList<string> UsedItems => Used ?? [];
    }

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } = [new("blocks", "Building blocks")];

    public static Mode ParseMode(string mode) => mode switch
    {
        "takeaway" => Mode.TakeAway,
        "maketen" => Mode.MakeTen,
        "oddeven" => Mode.OddEven,
        "tensones" => Mode.TensOnes,
        _ => Enum.Parse<Mode>(mode, ignoreCase: true),
    };

    /// <summary>Building rounds (the child makes a tower or number, then taps the tick).</summary>
    public static bool IsBuilding(Mode mode) => mode is Mode.Build or Mode.MakeTen or Mode.TensOnes;

    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var level = this.Level(learner);
        var upTo = Math.Clamp(level.CountTo ?? 10, 3, 99);
        foreach (var mode in rng.Shuffled((level.Modes ?? ["build"]).Select(ParseMode)))
        {
            var round = mode switch
            {
                Mode.Build => Build(Math.Min(upTo, 20), session, rng),
                Mode.Which => Which(Math.Min(upTo, 20), session, rng),
                Mode.Join => Join(Math.Min(upTo, 20), session, rng),
                Mode.TakeAway => TakeAway(Math.Min(upTo, 20), session, rng),
                Mode.MakeTen => MakeTen(session, rng),
                Mode.Doubles => Doubles(Math.Min(upTo, 20), session, rng),
                Mode.OddEven => OddEven(Math.Min(upTo, 20), session, rng),
                Mode.TensOnes => TensOnes(upTo, session, rng),
                _ => Pattern(Math.Min(upTo, 20), session, rng),
            };
            if (round != null) return round;
        }
        return null;
    }

    /// <summary>
    /// A fresh question when there is one: <paramref name="options"/> not yet asked this visit (by key).
    /// Small levels run out (towers up to 5), so after that any question may come again.
    /// </summary>
    private static bool PickFresh<T>(IReadOnlyList<T> options, Func<T, string> key, GameSession session, IRandomSource rng,
                                     out T pick, out IReadOnlyList<string> used)
    {
        var fresh = options.Where(o => !session.UsedItems.Contains(key(o))).ToList();
        if (rng.TryPick(fresh, out pick))
        {
            used = [key(pick)];
            return true;
        }
        used = [];
        return rng.TryPick(options, out pick);
    }

    private static List<string> Numerals(IEnumerable<int> numbers) => numbers.Select(n => n.ToString()).ToList();

    /// <summary>Every height from none up to <paramref name="most"/>, for rounds where the child builds.</summary>
    private static List<string> Heights(int most) => Numerals(Enumerable.Range(0, most + 1));

    private Round? Build(int upTo, GameSession session, IRandomSource rng)
    {
        if (!PickFresh(Enumerable.Range(1, upTo).ToList(), n => $"build:{n}", session, rng, out var n, out var used)) return null;
        var most = upTo <= Ten ? Ten : 20;
        return new Round(Mode.Build, n, Heights(most), MostCubes: most, Used: used);
    }

    private Round? Which(int upTo, GameSession session, IRandomSource rng)
    {
        var ask = rng.NextIndex(3) switch { 0 => Ask.Tallest, 1 => Ask.Shortest, _ => Ask.Number };
        var heights = rng.Shuffled(Enumerable.Range(1, upTo)).Take(3).ToList();
        if (heights.Count < 3) return null;
        var target = ask switch { Ask.Tallest => heights.Max(), Ask.Shortest => heights.Min(), _ => heights[0] };
        var towers = rng.Shuffled(heights);
        return new Round(Mode.Which, target, Numerals(towers), Towers: towers, Ask: ask);
    }

    private Round? Join(int upTo, GameSession session, IRandomSource rng)
    {
        var pairs = (from a in Enumerable.Range(1, upTo) from b in Enumerable.Range(1, upTo) where a + b <= upTo select (a, b)).ToList();
        if (!PickFresh(pairs, p => $"join:{p.a}+{p.b}", session, rng, out var pair, out var used)) return null;
        var total = pair.a + pair.b;
        return new Round(Mode.Join, total, Numerals(NumeralChoices.Three(total, rng)), pair.a, pair.b, Used: used);
    }

    private Round? TakeAway(int upTo, GameSession session, IRandomSource rng)
    {
        var pairs = (from n in Enumerable.Range(2, Math.Max(0, upTo - 1)) from k in Enumerable.Range(1, n - 1) select (n, k)).ToList();
        if (!PickFresh(pairs, p => $"take:{p.n}-{p.k}", session, rng, out var pair, out var used)) return null;
        var left = pair.n - pair.k;
        return new Round(Mode.TakeAway, left, Numerals(NumeralChoices.Three(left, rng)), pair.n, pair.k, Used: used);
    }

    /// <summary>Pairs that make 10 (Cambridge 1Ni.04): a tower of 1 to 9, with room up to 10.</summary>
    private Round? MakeTen(GameSession session, IRandomSource rng)
    {
        if (!PickFresh(Enumerable.Range(1, 9).ToList(), a => $"ten:{a}", session, rng, out var a, out var used)) return null;
        return new Round(Mode.MakeTen, Ten - a, Heights(Ten), a, MostCubes: Ten, Used: used);
    }

    /// <summary>Doubles up to double 10 (1Ni.06).</summary>
    private Round? Doubles(int upTo, GameSession session, IRandomSource rng)
    {
        var most = Math.Min(Ten, upTo / 2);
        if (!PickFresh(Enumerable.Range(1, most).ToList(), n => $"double:{n}", session, rng, out var n, out var used)) return null;
        return new Round(Mode.Doubles, 2 * n, Numerals(NumeralChoices.Three(2 * n, rng)), n, n, Used: used);
    }

    private Round? OddEven(int upTo, GameSession session, IRandomSource rng)
    {
        if (!PickFresh(Enumerable.Range(1, upTo).ToList(), n => $"oddeven:{n}", session, rng, out var n, out var used)) return null;
        return new Round(Mode.OddEven, n, [Odd, Even], n, Used: used);
    }

    /// <summary>Two-digit numbers from tens and ones (2Np.01, 2Np.02). Ten ones snap into a ten.</summary>
    private Round? TensOnes(int upTo, GameSession session, IRandomSource rng)
    {
        var most = Math.Min(upTo, 99);
        if (most < 11 || !PickFresh(Enumerable.Range(11, most - 10).ToList(), n => $"tens:{n}", session, rng, out var n, out var used))
            return null;
        return new Round(Mode.TensOnes, n, Heights(99), MostCubes: 99, Used: used);
    }

    /// <summary>Towers growing by the same step (2Nc.06, 3Nc.05): which comes next?</summary>
    private Round? Pattern(int upTo, GameSession session, IRandomSource rng)
    {
        var starts = (from a in Enumerable.Range(1, 4) from d in Enumerable.Range(1, 3) where a + 3 * d <= upTo select (a, d)).ToList();
        if (!PickFresh(starts, s => $"pattern:{s.a}+{s.d}", session, rng, out var s, out var used)) return null;
        var next = s.a + 3 * s.d;
        var wrong = rng.Shuffled(new[] { next - 2, next - 1, next + 1, next + 2 }.Where(h => h >= 1 && h <= 20)).Take(2);
        var choices = rng.Shuffled(wrong.Append(next)).ToList();
        return new Round(Mode.Pattern, next, Numerals(choices), s.a, s.d, Towers: [s.a, s.a + s.d, s.a + 2 * s.d], Used: used);
    }

    public bool IsCorrect(string choice, Round round) => round.Mode switch
    {
        Mode.OddEven => choice == (round.A % 2 == 0 ? Even : Odd),
        Mode.Which when round.Ask == Ask.Tallest => int.TryParse(choice, out var h) && h == round.Towers!.Max(),
        Mode.Which when round.Ask == Ask.Shortest => int.TryParse(choice, out var h) && h == round.Towers!.Min(),
        _ => int.TryParse(choice, out var n) && n == round.Target,
    };

    public string SkillId(Round round) => round.Mode switch
    {
        Mode.Build => round.Target <= Ten ? "count_10" : "count_20",
        Mode.Which => round.Ask == Ask.Number ? (round.Target <= Ten ? "count_10" : "count_20") : "compare",
        Mode.Join or Mode.TakeAway => "add_sub_20",
        Mode.MakeTen => "bonds_10",
        Mode.Doubles => "doubles",
        Mode.OddEven => "even_odd",
        Mode.TensOnes => "count_100",
        _ => "sequences",
    };
}
