namespace BipCore;

/// <summary>
/// Paint Pots (Art Island): mixing paint. Mixes follow real paint (red and yellow make orange),
/// from art/paints.json, never screen colours. Two kinds of question (games.json "modes"):
/// <list type="bullet">
/// <item>make: "Make green!" — tap two of the pots on the table. Choices are pairs, "blue+yellow".</item>
/// <item>predict: Bip holds up two pots — what will they make? Choices are colour ids; then it mixes to check.</item>
/// </list>
/// White makes colours lighter (stage 1) and black makes them darker (stage 2).
/// </summary>
public sealed class PaintPotsGame(ContentLibrary content) : IMiniGame<PaintPotsGame.Round, string>
{
    public const string GameId = "paint_pots";

    public enum Mode { Make, Predict }

    /// <param name="Pots">The pots on the table, in screen order.</param>
    /// <param name="Target">The colour to make (make), or the colour the two pots make (predict).</param>
    /// <param name="First">Predict rounds: the two pots Bip holds up.</param>
    public sealed record Round(Mode Mode, IReadOnlyList<PaintColour> Pots, PaintColour Target, IReadOnlyList<string> Choices,
                               PaintColour? First = null, PaintColour? Second = null) : IGameRound<string>
    {
        public IReadOnlyList<string> UsedItems => [];
    }

    private readonly Dictionary<string, PaintColour> _colours = content.Paints.Colours.ToDictionary(c => c.Id);
    private readonly Dictionary<string, PaintPot> _pots = content.Paints.Pots.ToDictionary(p => p.Colour);
    private readonly List<PaintMix> _mixes = content.Paints.Mixes;

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } = [new("studio", "Art studio")];

    public PaintColour? Colour(string id) => _colours.GetValueOrDefault(id);
    public PaintPot? Pot(string colour) => _pots.GetValueOrDefault(colour);

    /// <summary>What two paints make, or null if the mix isn't in the content.</summary>
    public PaintColour? Mix(string a, string b) =>
        _mixes.FirstOrDefault(m => (m.A == a && m.B == b) || (m.A == b && m.B == a)) is { } mix ? Colour(mix.Makes) : null;

    /// <summary>A pair of pots as a choice: the two colour ids in alphabetical order, "blue+yellow".</summary>
    public static string Pair(string a, string b) => string.CompareOrdinal(a, b) <= 0 ? $"{a}+{b}" : $"{b}+{a}";

    public static (string A, string B) SplitPair(string pair)
    {
        var parts = pair.Split('+');
        return parts.Length == 2 ? (parts[0], parts[1]) : ("", "");
    }

    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var level = this.Level(learner);
        var pots = (level.Pots ?? []).Select(Colour).OfType<PaintColour>().ToList();
        if (pots.Count < 3) return null;
        var pairs = new List<(PaintColour A, PaintColour B, PaintColour Makes)>();
        for (var i = 0; i < pots.Count; i++)
            for (var j = i + 1; j < pots.Count; j++)
                if (Mix(pots[i].Id, pots[j].Id) is { } makes) pairs.Add((pots[i], pots[j], makes));
        if (!rng.TryPick(pairs, out var pick)) return null;
        var mode = rng.Pick((level.Modes ?? ["make"]).ToList()) is { } m ? Enum.Parse<Mode>(m, ignoreCase: true) : Mode.Make;

        if (mode == Mode.Make)
        {
            var choices = pairs.Select(p => Pair(p.A.Id, p.B.Id)).ToList();
            return new Round(Mode.Make, pots, pick.Makes, choices);
        }
        var others = rng.Shuffled(pairs.Select(p => p.Makes).Where(c => c.Id != pick.Makes.Id).DistinctBy(c => c.Id)).Take(2);
        var colours = rng.Shuffled(others.Append(pick.Makes)).Select(c => c.Id).ToList();
        var (first, second) = rng.NextBool() ? (pick.A, pick.B) : (pick.B, pick.A);
        return new Round(Mode.Predict, pots, pick.Makes, colours, first, second);
    }

    public bool IsCorrect(string choice, Round round)
    {
        if (round.Mode == Mode.Predict)
            return round.First != null && round.Second != null && Mix(round.First.Id, round.Second.Id)?.Id == choice;
        var (a, b) = SplitPair(choice);
        return a != b && Mix(a, b)?.Id == round.Target.Id;
    }

    /// <summary>Anything with white or black in it is lighter-and-darker; the rest is plain mixing.</summary>
    public string SkillId(Round round)
    {
        static bool Shade(string id) => id is "white" or "black";
        var light = round.Mode == Mode.Predict
            ? Shade(round.First?.Id ?? "") || Shade(round.Second?.Id ?? "")
            : round.Choices.Where(c => IsCorrect(c, round)).Any(c => Shade(SplitPair(c).A) || Shade(SplitPair(c).B));
        return light ? "colour_light_dark" : "colour_mixing";
    }
}
