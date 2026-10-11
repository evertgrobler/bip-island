namespace BipCore;

/// <summary>
/// Bip's Balance (Numbers Island): a number sentence on a see-saw. One side holds a sum (4 + 3), the
/// other its answer, and one number is a box with a question mark. The child taps one of three
/// number cards; the right one makes both sides balance. This is where the written + − = come in,
/// after Block Towers has shown the same sums as towers. Kinds of question (games.json "modes"):
/// <list type="bullet">
/// <item>add: a + b = ? within the level's number, cubes shown.</item>
/// <item>takeaway: a − b = ? within the level's number, cubes shown (the ones taken away crossed out).</item>
/// <item>past10: going past 10 within 20 (8 + 5, 14 − 6), cubes only in the hint.</item>
/// <item>missing: the box is one of the first two numbers (7 + ? = 10, ? − 3 = 5): Cambridge's
/// "an object stands for an unknown number" (3Nc.04).</item>
/// <item>factfamily: a finished sum first (3 + 5 = 8), then one it helps with (8 − 5 = ?): adding and
/// taking away undo each other (2Ni.02).</item>
/// </list>
/// About 1 round in 4 of add and take away is a story sum instead ("There are 5 ducks. 3 more come
/// along. How many are there now?"), from numbers.json "storySums". No answer is ever 0 or below.
/// </summary>
public sealed class BipsBalanceGame(ContentLibrary content) : IMiniGame<BipsBalanceGame.Round, int>
{
    public const string GameId = "bips_balance";
    /// <summary>One round in this many of add and take away is a story sum.</summary>
    public const int StoryEvery = 4;
    /// <summary>Stories and cubes stay small enough to see at once.</summary>
    public const int SmallSums = 10;

    public enum Mode { Add, TakeAway, Past10, Missing, FactFamily }
    public enum Op { Plus, Minus }
    /// <summary>Which number in the sentence is the box: Left op Right = Result.</summary>
    public enum Slot { Left, Right, Result }

    /// <summary>A number sentence: <c>Left op Right = Result</c>.</summary>
    public sealed record Sentence(int Left, Op Op, int Right, int Result)
    {
        public int this[Slot slot] => slot switch { Slot.Left => Left, Slot.Right => Right, _ => Result };

        /// <summary>The sentence with <paramref name="value"/> in <paramref name="slot"/>.</summary>
        public Sentence With(Slot slot, int value) => slot switch
        {
            Slot.Left => this with { Left = value },
            Slot.Right => this with { Right = value },
            _ => this with { Result = value },
        };

        /// <summary>What the sum side comes to (the see-saw's left side).</summary>
        public int SumSide => Op == Op.Plus ? Left + Right : Left - Right;

        /// <summary>Both sides weigh the same.</summary>
        public bool Balances => SumSide == Result;

        public string Key => $"{Left}{(Op == Op.Plus ? "+" : "-")}{Right}={Result}";

        public override string ToString() => $"{Left} {(Op == Op.Plus ? "+" : "−")} {Right} = {Result}";
    }

    /// <param name="Sentence">The whole sentence, answer included.</param>
    /// <param name="Hidden">The number the child finds.</param>
    /// <param name="Helper">Fact families: the finished sum shown first.</param>
    /// <param name="Story">A story sum's template, with <paramref name="Thing"/> the animals in it.</param>
    public sealed record Round(Mode Mode, Sentence Sentence, Slot Hidden, IReadOnlyList<int> Choices,
                               Sentence? Helper = null, StorySum? Story = null, CountingObject? Thing = null) : IGameRound<int>
    {
        public int Answer => Sentence[Hidden];
        public IReadOnlyList<string> UsedItems => [Sentence.Key + "@" + Hidden];
        /// <summary>Cubes stand on the see-saw for the first two kinds of sum (they fade after that).</summary>
        public bool ShowsCubes => Mode is Mode.Add or Mode.TakeAway && Story == null;
    }

    private readonly NumbersFile _numbers = content.Numbers;
    private readonly Dictionary<string, CountingObject> _things = content.Numbers.CountingObjects.ToDictionary(o => o.Id);

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } =
        [new("seesaw", "See-saw at the park")];

    public static Mode ParseMode(string mode) => mode switch
    {
        "takeaway" => Mode.TakeAway,
        "past10" => Mode.Past10,
        "factfamily" => Mode.FactFamily,
        _ => Enum.Parse<Mode>(mode, ignoreCase: true),
    };

    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var level = this.Level(learner);
        var upTo = Math.Clamp(level.CountTo ?? SmallSums, 5, 20);
        foreach (var mode in rng.Shuffled((level.Modes ?? ["add"]).Select(ParseMode)))
        {
            if (MakeRound(mode, upTo, session, rng) is Round round) return round;
        }
        return null;
    }

    private Round? MakeRound(Mode mode, int upTo, GameSession session, IRandomSource rng)
    {
        var options = Sentences(mode, upTo);
        // Nothing asked twice in a visit while there's something fresh; small levels may repeat after that.
        var fresh = options.Where(o => !session.UsedItems.Contains(o.Sentence.Key + "@" + o.Hidden)).ToList();
        if (!rng.TryPick(fresh, out var pick) && !rng.TryPick(options, out pick)) return null;
        var (sentence, hidden) = pick;
        var answer = sentence[hidden];
        var choices = Choices(answer, rng);
        if (mode == Mode.FactFamily)
        {
            // The finished sum it comes from: a + b = c helps with c − b, c − a and b + a.
            var helper = sentence.Op == Op.Minus
                ? new Sentence(sentence.Result, Op.Plus, sentence.Right, sentence.Left)
                : new Sentence(sentence.Right, Op.Plus, sentence.Left, sentence.Result);
            return new Round(mode, sentence, hidden, choices, Helper: helper);
        }
        if (mode is Mode.Add or Mode.TakeAway && sentence.Result <= SmallSums && sentence.Left >= 2 && sentence.Right >= 2
            && rng.NextIndex(StoryEvery) == 0 && Story(sentence.Op, rng) is { } story)
            return new Round(mode, sentence, hidden, choices, Story: story.Story, Thing: story.Thing);
        return new Round(mode, sentence, hidden, choices);
    }

    /// <summary>A story for this kind of sum and one of its animals, if numbers.json has one.</summary>
    private (StorySum Story, CountingObject Thing)? Story(Op op, IRandomSource rng)
    {
        var stories = (_numbers.StorySums ?? []).Where(s => s.Op == (op == Op.Plus ? "plus" : "minus")).ToList();
        if (!rng.TryPick(stories, out var story)) return null;
        var things = story.Things.Where(_things.ContainsKey).Select(id => _things[id]).ToList();
        return rng.TryPick(things, out var thing) ? (story, thing) : null;
    }

    /// <summary>Every sentence a kind of question can ask at this level, with the number that's the box.</summary>
    public static List<(Sentence Sentence, Slot Hidden)> Sentences(Mode mode, int upTo)
    {
        lock (SentenceCache)
        {
            if (!SentenceCache.TryGetValue((mode, upTo), out var all)) SentenceCache[(mode, upTo)] = all = AllSentences(mode, upTo);
            return all;
        }
    }

    private static readonly Dictionary<(Mode, int), List<(Sentence, Slot)>> SentenceCache = [];

    private static List<(Sentence Sentence, Slot Hidden)> AllSentences(Mode mode, int upTo)
    {
        var adds = (from a in Enumerable.Range(1, upTo) from b in Enumerable.Range(1, upTo)
                    where a + b <= upTo select new Sentence(a, Op.Plus, b, a + b)).ToList();
        var takes = (from a in Enumerable.Range(2, Math.Max(0, upTo - 1)) from b in Enumerable.Range(1, a - 1)
                     select new Sentence(a, Op.Minus, b, a - b)).ToList();
        return mode switch
        {
            Mode.Add => adds.Select(s => (s, Slot.Result)).ToList(),
            Mode.TakeAway => takes.Select(s => (s, Slot.Result)).ToList(),
            // Single-digit numbers that cross 10: 8 + 5 = 13, 14 − 6 = 8.
            Mode.Past10 => adds.Where(s => s.Left < 10 && s.Right < 10 && s.Result > 10)
                .Concat(takes.Where(s => s.Left > 10 && s.Right < 10 && s.Result < 10))
                .Select(s => (s, Slot.Result)).ToList(),
            // The missing number is at most 10, so the hint's count stays short.
            Mode.Missing => adds.SelectMany(s => new[] { (s, Slot.Left), (s, Slot.Right) })
                .Concat(takes.SelectMany(s => new[] { (s, Slot.Left), (s, Slot.Right) }))
                .Where(o => o.Item1[o.Item2] <= MostMissing).ToList(),
            // Ask c − b, c − a or b + a after a + b = c (b + a only when it's a different sum). No 1s:
            // "1 + 18 = 19, so 18 + 1" teaches little.
            _ => adds.Where(s => s.Left >= 2 && s.Right >= 2).SelectMany(s => new[] { new Sentence(s.Result, Op.Minus, s.Right, s.Left), new Sentence(s.Result, Op.Minus, s.Left, s.Right) }
                    .Concat(s.Left != s.Right ? [new Sentence(s.Right, Op.Plus, s.Left, s.Result)] : []))
                .Distinct().Select(s => (s, Slot.Result)).ToList(),
        };
    }

    /// <summary>Three cards: the answer and two near it, none below 1.</summary>
    private static List<int> Choices(int answer, IRandomSource rng)
    {
        var near = new[] { answer - 2, answer - 1, answer + 1, answer + 2 }.Where(n => n >= 1);
        return rng.Shuffled(rng.Shuffled(near).Take(2).Prepend(answer));
    }

    /// <summary>Right when the number in the box makes both sides balance.</summary>
    public bool IsCorrect(int choice, Round round) => round.Sentence.With(round.Hidden, choice).Balances;

    public string SkillId(Round round) => round.Mode switch
    {
        Mode.Missing => "missing_number",
        // Fact families stay within 20: adding and taking away to 20 (they also show 2Ni.02).
        _ => "add_sub_20",
    };

    // What Bip says. Plain lists of clips, so the tests can check each one has a recording.

    /// <summary>The sentence out loud, with "what number" for the box (or the answer, once found).</summary>
    public static List<string> Say(Sentence sentence, Slot? hidden = null)
    {
        string Part(Slot slot) => slot == hidden ? VoiceLine.WhatNumber : AudioCatalogue.NumberClip(sentence[slot]);
        return [Part(Slot.Left), sentence.Op == Op.Plus ? VoiceLine.Plus : VoiceLine.TakeAway, Part(Slot.Right),
                VoiceLine.EqualsWord, Part(Slot.Result)];
    }

    /// <summary>The question: a story, or the sentence; a fact family says its finished sum first.</summary>
    public List<string> Prompt(Round round)
    {
        if (round.Story is { } story && round.Thing is { } thing)
            return story.Clips.Select(clip => clip switch
            {
                "{a}" => AudioCatalogue.NumberClip(round.Sentence.Left),
                "{b}" => AudioCatalogue.NumberClip(round.Sentence.Right),
                "{things}" => thing.AudioPlural,
                _ => clip,
            }).ToList();
        var ask = Say(round.Sentence, round.Hidden).Append(VoiceLine.WhichNumber);
        return round.Helper is { } helper ? [.. Say(helper), VoiceLine.BalanceHelps, .. ask] : ask.ToList();
    }

    /// <summary>After a right answer: the whole sentence.</summary>
    public static List<string> SayWhenRight(Round round) => Say(round.Sentence);

    /// <summary>
    /// The spoken hint after two misses: Bip counts, one number at a time, along the short way, and
    /// never says the missing number on its own. Adding counts on from the bigger number (2 + 18:
    /// "18… 19, 20"); taking away counts back, or counts on from the number taken away when that is
    /// shorter (20 − 18: "18… 19, 20"); a missing number counts between the two numbers shown
    /// (7 + ? = 10: "7… 8, 9, 10"). A fact family points back to the finished sum on the sign.
    /// </summary>
    public static List<string> Hint(Round round)
    {
        var s = round.Sentence;
        if (round.Helper is { } helper) return [VoiceLine.BalanceHelps, .. Say(helper)];
        return round.Hidden switch
        {
            // a + b = ?: on from the bigger.  a − b = ?: back by b, or on from b up to a when that's shorter.
            Slot.Result when s.Op == Op.Plus => Count(Math.Max(s.Left, s.Right), s.Result),
            Slot.Result => s.Right <= s.Result ? Count(s.Left, s.Result) : Count(s.Right, s.Left),
            // a + ? = c and ? + b = c: on from the number shown to c.  a − ? = c: back from a to c.
            // ? − b = c: on from c by b.
            _ when s.Op == Op.Plus => Count(round.Hidden == Slot.Right ? s.Left : s.Right, s.Result),
            Slot.Right => Count(s.Left, s.Result),
            _ => Count(s.Result, s.Left),
        };
    }

    /// <summary>"Let's count on (or back) together", then every number from <paramref name="from"/> to <paramref name="to"/>.</summary>
    private static List<string> Count(int from, int to)
    {
        var step = to >= from ? 1 : -1;
        var numbers = Enumerable.Range(0, Math.Abs(to - from) + 1).Select(i => AudioCatalogue.NumberClip(from + step * i));
        return [step > 0 ? VoiceLine.CountOnTogether : VoiceLine.CountBackTogether, .. numbers];
    }

    /// <summary>The longest count a hint can make (a missing number is at most this).</summary>
    public const int MostMissing = 10;
}
