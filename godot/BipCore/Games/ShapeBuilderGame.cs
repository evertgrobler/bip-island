namespace BipCore;

/// <summary>
/// Shape Builder (Art Island): flat shapes, from "find the circle" to telling regular shapes from
/// wonky ones. Each level mixes some kinds of question (games.json "modes") over its own shapes:
/// <list type="bullet">
/// <item>find: "Find the triangle" — pick it out of three.</item>
/// <item>fill: a picture made of shapes has a gap — which shape fits?</item>
/// <item>sides: "Which shape has 3 straight sides?" or "Which shape has a curved side?"</item>
/// <item>turn: every shape turns a quarter of the way round — which still looks the same?</item>
/// <item>count: one shape turns all the way round — how many times does it look the same?</item>
/// <item>regular: which shape has all its sides the same length?</item>
/// </list>
/// Choices are shape ids, or numbers ("4") for count rounds.
/// </summary>
public sealed class ShapeBuilderGame(ContentLibrary content) : IMiniGame<ShapeBuilderGame.Round, string>
{
    public const string GameId = "shape_builder";

    public enum Mode { Find, Fill, Sides, Turn, Count, Regular }

    /// <param name="Target">The shape asked for (find), missing (fill) or turning (count).</param>
    /// <param name="Sides">Sides rounds: the straight sides asked for, or 0 for "a curved side".</param>
    /// <param name="Picture">Fill rounds: the picture, and <paramref name="Missing"/> the index of its missing piece.</param>
    /// <param name="Turns">How far each choice is turned for drawing, in degrees (0 when upright).</param>
    public sealed record Round(Mode Mode, IReadOnlyList<string> Choices, ArtShape? Target = null, int Sides = 0,
                               ShapePicture? Picture = null, int Missing = -1, IReadOnlyList<int>? Turns = null) : IGameRound<string>
    {
        public IReadOnlyList<string> UsedItems => Mode == Mode.Fill && Picture != null ? [$"picture:{Picture.Id}"] : [];
    }

    private readonly Dictionary<string, ArtShape> _shapes = content.Shapes.Shapes.ToDictionary(s => s.Id);
    private readonly List<ShapePicture> _pictures = content.Shapes.Pictures;

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } = [new("studio", "Art studio")];

    public ArtShape? Shape(string id) => _shapes.GetValueOrDefault(id);

    public static Mode ParseMode(string mode) => Enum.Parse<Mode>(mode, ignoreCase: true);

    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var level = this.Level(learner);
        var shapes = (level.Shapes ?? []).Select(Shape).OfType<ArtShape>().ToList();
        if (shapes.Count < 3) return null;
        // Try the level's kinds of question in a random order; a kind with nothing fresh is skipped.
        foreach (var mode in rng.Shuffled((level.Modes ?? ["find"]).Select(ParseMode)))
        {
            var round = mode switch
            {
                Mode.Find => Find(shapes, rng),
                Mode.Fill => Fill(shapes, session, rng),
                Mode.Sides => Sides(shapes, rng),
                Mode.Turn => OneOf(shapes.Where(s => s.SameAfterQuarterTurn), shapes.Where(s => !s.SameAfterQuarterTurn), Mode.Turn, rng),
                Mode.Count => Count(shapes, rng),
                _ => OneOf(shapes.Where(s => s.Regular == true), shapes.Where(s => s.Regular == false), Mode.Regular, rng),
            };
            if (round != null) return round;
        }
        return null;
    }

    /// <summary>Turned shapes from stage 2 on (Cambridge 2Gg.01: shapes in any position).</summary>
    private static List<int> TurnsFor(IReadOnlyList<ArtShape> shapes, IEnumerable<string> ids, IRandomSource rng)
    {
        var turned = shapes.Any(s => s.Band >= Band.Stage2);
        return ids.Select(_ => turned ? rng.NextInt(0, 11) * 30 : 0).ToList();
    }

    private Round? Find(List<ArtShape> shapes, IRandomSource rng)
    {
        if (rng.Pick(shapes) is not { } target) return null;
        var others = rng.Shuffled(shapes.Where(s => s.Name != target.Name)).DistinctBy(s => s.Name).Take(2).ToList();
        if (others.Count < 2) return null;
        var choices = rng.Shuffled(others.Append(target)).Select(s => s.Id).ToList();
        return new Round(Mode.Find, choices, target, Turns: TurnsFor(shapes, choices, rng));
    }

    private Round? Fill(List<ArtShape> shapes, GameSession session, IRandomSource rng)
    {
        var ids = shapes.Select(s => s.Id).ToHashSet();
        var usable = _pictures
            .Where(p => !session.UsedItems.Contains($"picture:{p.Id}"))
            .Where(p => p.Pieces.All(piece => piece.Fixed == true || ids.Contains(piece.Shape)))
            .ToList();
        if (rng.Pick(usable) is not { } picture) return null;
        var gaps = Enumerable.Range(0, picture.Pieces.Count).Where(i => picture.Pieces[i].Fixed != true).ToList();
        if (!rng.TryPick(gaps, out var missing) || Shape(picture.Pieces[missing].Shape) is not { } target) return null;
        var others = rng.Shuffled(shapes.Where(s => s.Name != target.Name)).DistinctBy(s => s.Name).Take(2).ToList();
        if (others.Count < 2) return null;
        var choices = rng.Shuffled(others.Append(target)).Select(s => s.Id).ToList();
        return new Round(Mode.Fill, choices, target, Picture: picture, Missing: missing, Turns: choices.Select(_ => 0).ToList());
    }

    /// <summary>"Which has N straight sides?" (one shape has N, the others don't) or "which has a curved side?".</summary>
    private Round? Sides(List<ArtShape> shapes, IRandomSource rng)
    {
        var curved = shapes.Where(s => s.Curved).ToList();
        var straight = shapes.Where(s => !s.Curved).ToList();
        if (curved.Count > 0 && straight.Count >= 2 && rng.NextBool())
        {
            var odd = OneOf(curved, straight.DistinctBy(s => s.StraightSides), Mode.Sides, rng);
            return odd;
        }
        if (rng.Pick(straight) is not { } target) return null;
        var others = rng.Shuffled(shapes.Where(s => s.StraightSides != target.StraightSides)).DistinctBy(s => s.StraightSides).Take(2).ToList();
        if (others.Count < 2) return null;
        var choices = rng.Shuffled(others.Append(target)).Select(s => s.Id).ToList();
        return new Round(Mode.Sides, choices, target, Sides: target.StraightSides, Turns: choices.Select(_ => 0).ToList());
    }

    /// <summary>One shape that has the property and two that don't.</summary>
    private static Round? OneOf(IEnumerable<ArtShape> yes, IEnumerable<ArtShape> no, Mode mode, IRandomSource rng)
    {
        if (rng.Pick(yes.ToList()) is not { } target) return null;
        var others = rng.Shuffled(no.Where(s => s.Name != target.Name)).Take(2).ToList();
        if (others.Count < 2) return null;
        var choices = rng.Shuffled(others.Append(target)).Select(s => s.Id).ToList();
        return new Round(mode, choices, target, Turns: choices.Select(_ => 0).ToList());
    }

    private static Round? Count(List<ArtShape> shapes, IRandomSource rng)
    {
        if (rng.Pick(shapes.Where(s => s.LookSameTurns >= 2).ToList()) is not { } target) return null;
        var answer = target.LookSameTurns;
        var near = rng.Shuffled(new[] { answer - 2, answer - 1, answer + 1, answer + 2 }.Where(n => n >= 1)).Take(2);
        var choices = rng.Shuffled(near.Prepend(answer)).Select(n => n.ToString()).ToList();
        return new Round(Mode.Count, choices, target);
    }

    public bool IsCorrect(string choice, Round round)
    {
        if (round.Mode == Mode.Count) return int.TryParse(choice, out var n) && n == round.Target?.LookSameTurns;
        if (Shape(choice) is not { } shape) return false;
        return round.Mode switch
        {
            Mode.Find or Mode.Fill => round.Target != null && shape.Name == round.Target.Name,
            Mode.Sides => round.Sides == 0 ? shape.Curved : !shape.Curved && shape.StraightSides == round.Sides,
            Mode.Turn => shape.SameAfterQuarterTurn,
            _ => shape.Regular == true,
        };
    }

    public string SkillId(Round round) => round.Mode switch
    {
        Mode.Sides => "shape_sides",
        Mode.Turn or Mode.Count => "shape_turns",
        Mode.Regular => "shape_regular",
        _ => round.Choices.Select(Shape).Any(s => s?.Band >= Band.Stage2) ? "shape_names" : "shapes_2d",
    };
}
