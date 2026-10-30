namespace BipCore;

/// <summary>
/// Picnic Plates (Numbers Island): times and sharing with food on Bip's picnic blanket, before Number
/// Machine shows the same in numbers. Every round is equal groups of food, and every share comes out
/// even. Some kinds of question have the child do it first (tap the basket to deal the food round the
/// plates, or to pack it), then all of them pick the answer from three number cards.
/// Kinds of question (games.json "modes"), each with its tables in "steps" (2, 5 and 10 at stage 2):
/// <list type="bullet">
/// <item>groups: plates with the same on each. How many altogether? (2Ni.05)</item>
/// <item>make: "Put 5 on each plate": each tap of the basket puts one on every plate, until they hold 5.
/// Then: how many altogether? (2Ni.05)</item>
/// <item>arrays: a tray in rows. How many on the tray? (2Ni.05)</item>
/// <item>sharing: food in the basket, shared round the plates one each, round and round, until it's
/// gone. How many on each plate? (2Ni.06)</item>
/// <item>grouping: loose food packed into punnets of the same size, a punnet a tap. How many punnets? (2Ni.06)</item>
/// </list>
/// In groups, make and arrays the number on each plate (or in each row, or punnet) is one of the
/// level's tables, so the hint counts in its steps; in sharing either the plates or the share is.
/// </summary>
public sealed class PicnicPlatesGame(ContentLibrary content) : IMiniGame<PicnicPlatesGame.Round, int>
{
    public const string GameId = "picnic_plates";
    /// <summary>The tables a level uses when games.json names none (Cambridge stage 2).</summary>
    public static readonly IReadOnlyList<int> StageTwoTables = [2, 5, 10];

    public enum Mode { Groups, Make, Arrays, Sharing, Grouping }

    /// <param name="Groups">Plates, rows on the tray, or punnets.</param>
    /// <param name="Each">The food on each plate, in each row, or in each punnet.</param>
    public sealed record Round(Mode Mode, int Groups, int Each, PicnicFood Food, IReadOnlyList<int> Choices) : IGameRound<int>
    {
        public int Total => Groups * Each;

        public int Answer => Mode switch
        {
            Mode.Sharing => Each,
            Mode.Grouping => Groups,
            _ => Total,
        };

        /// <summary>Taps of the basket before the question: a round of dealing, or a punnet packed, each.</summary>
        public int Deals => Mode switch
        {
            Mode.Make or Mode.Sharing => Each,
            Mode.Grouping => Groups,
            _ => 0,
        };

        public IReadOnlyList<string> UsedItems => [$"{Mode}:{Groups}x{Each}"];
    }

    private readonly Picnic _picnic = content.Numbers.Picnic
        ?? throw new InvalidOperationException("numbers.json has no picnic section for Picnic Plates");

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } = [new("blanket", "Picnic blanket")];

    public static Mode ParseMode(string mode) => Enum.Parse<Mode>(mode, ignoreCase: true);

    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var level = this.Level(learner);
        var tables = level.Steps is { Count: > 0 } steps ? steps : StageTwoTables;
        foreach (var mode in rng.Shuffled((level.Modes ?? ["groups"]).Select(ParseMode)))
        {
            var options = Shapes(mode, tables, _picnic);
            // Nothing asked twice in a visit while there's something fresh.
            var fresh = options.Where(o => !session.UsedItems.Contains($"{mode}:{o.Groups}x{o.Each}")).ToList();
            if (!rng.TryPick(fresh, out var pick) && !rng.TryPick(options, out pick)) continue;
            if (!rng.TryPick(_picnic.Food, out var food)) return null;
            var answer = mode switch { Mode.Sharing => pick.Each, Mode.Grouping => pick.Groups, _ => pick.Groups * pick.Each };
            var step = mode is Mode.Groups or Mode.Make or Mode.Arrays ? pick.Each : 1;
            return new Round(mode, pick.Groups, pick.Each, food, NearChoices.Make(answer, step, rng));
        }
        return null;
    }

    /// <summary>Every (groups, each) a kind of question can use with these tables, within what the blanket holds.</summary>
    public static List<(int Groups, int Each)> Shapes(Mode mode, IReadOnlyList<int> tables, Picnic picnic)
    {
        var mostGroups = mode == Mode.Grouping ? picnic.MostPlates + 1 : picnic.MostPlates;
        return (from groups in Enumerable.Range(2, Math.Max(0, mostGroups - 1))
                from each in Enumerable.Range(2, Math.Max(0, picnic.MostEach - 1))
                where groups * each <= picnic.MostFood
                where mode == Mode.Sharing ? tables.Contains(groups) || tables.Contains(each) : tables.Contains(each)
                select (groups, each)).ToList();
    }

    public bool IsCorrect(int choice, Round round) => round.Mode switch
    {
        Mode.Sharing => choice * round.Groups == round.Total,
        Mode.Grouping => choice * round.Each == round.Total,
        _ => choice == round.Groups * round.Each,
    };

    public string SkillId(Round round) => round.Mode is Mode.Sharing or Mode.Grouping ? "share_group" : "equal_groups";

    // What Bip says. Plain lists of clips, so the tests can check each one has a recording.

    /// <summary>
    /// What to do first: deal or pack with the basket ("Share twelve between two plates. Click the
    /// basket."). Nothing for a kind of question with no dealing.
    /// </summary>
    public List<string> Instruction(Round round) => round.Mode switch
    {
        Mode.Make => [VoiceLine.Put, content.MidNumberClip(round.Each), VoiceLine.OnEachPlate, VoiceLine.ClickTheBasket],
        Mode.Sharing => [VoiceLine.Share, content.MidNumberClip(round.Total), VoiceLine.Between, content.MidNumberClip(round.Groups),
                         VoiceLine.Plates, VoiceLine.ClickTheBasket],
        Mode.Grouping => [VoiceLine.GroupsOf, AudioCatalogue.NumberClip(round.Each), VoiceLine.ClickTheBasket],
        _ => [],
    };

    /// <summary>The question, asked once the food is dealt (or straight away).</summary>
    public List<string> Question(Round round) => round.Mode switch
    {
        Mode.Arrays => [VoiceLine.HowManyOnTray],
        Mode.Sharing => [VoiceLine.HowManyEach],
        Mode.Grouping => [VoiceLine.HowManyGroups],
        _ => [VoiceLine.HowManyAltogether],
    };

    /// <summary>After a right answer: the answer.</summary>
    public List<string> SayWhenRight(Round round) => [AudioCatalogue.NumberClip(round.Answer)];

    /// <summary>
    /// The spoken hint after two misses. Times: count in steps of the number on each plate (or row),
    /// "Let's count in steps of 5: 5, 10, 15". Sharing: "Let's give one to each plate, round and round:
    /// 1, 2, 3, 4", a number a round, so the last number is how many each plate got. Grouping: "Let's
    /// count the groups: 1, 2, 3". The last number is always the answer, said as the count ends.
    /// </summary>
    public List<string> Hint(Round round)
    {
        List<string> Count(int count, int step)
        {
            var numbers = Enumerable.Range(1, count)
                .Select(i => i < count ? content.MidNumberClip(i * step) : AudioCatalogue.NumberClip(i * step));
            return [.. numbers];
        }
        return round.Mode switch
        {
            Mode.Sharing => [VoiceLine.OneEach, .. Count(round.Each, 1)],
            Mode.Grouping => [VoiceLine.CountGroups, .. Count(round.Groups, 1)],
            _ => [VoiceLine.CountInSteps, AudioCatalogue.NumberClip(round.Each), .. Count(round.Groups, round.Each)],
        };
    }
}
