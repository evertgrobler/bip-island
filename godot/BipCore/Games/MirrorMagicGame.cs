namespace BipCore;

/// <summary>
/// Mirror Magic (Art Island): symmetry on a peg board. Kinds of question (games.json "modes"):
/// <list type="bullet">
/// <item>same: which of three pictures is the same on both sides? Choices "0" to "2" (the candidates).</item>
/// <item>finish: half a picture is shown — which piece finishes it? The wrong pieces are the half
///   copied without flipping (the classic mistake) and a half of another picture.</item>
/// <item>pegs: some pegs sit on one side of the mirror; put pegs on the other side. Choices are the
///   holes on the empty side ("row,col"); every peg in the reflection is right, one tap each.</item>
/// <item>line: where does the mirror go? "vertical", "horizontal" or "diagonal".</item>
/// </list>
/// Grids are rows of paint letters, top row first, "." for an empty hole (art/mirror.json).
/// </summary>
public sealed class MirrorMagicGame(ContentLibrary content) : IMiniGame<MirrorMagicGame.Round, string>
{
    public const string GameId = "mirror_magic";
    public const string Vertical = "vertical";
    public const string Horizontal = "horizontal";
    public const string Diagonal = "diagonal";
    public const char Empty = '.';

    public enum Mode { Same, Finish, Pegs, Line }

    /// <param name="Picture">The picture (same, finish, line) or the board with its pegs (pegs).</param>
    /// <param name="Axis">The mirror line: "vertical" (down the middle) or "horizontal" (across).</param>
    /// <param name="Candidates">Same: whole pictures. Finish: the half-pictures to choose from.</param>
    public sealed record Round(Mode Mode, string Axis, IReadOnlyList<string> Picture, IReadOnlyList<string> Choices,
                               IReadOnlyList<IReadOnlyList<string>> Candidates, string PictureId = "") : IGameRound<string>
    {
        public IReadOnlyList<string> UsedItems => PictureId == "" ? [] : [$"picture:{PictureId}"];

        /// <summary>The finish rounds' shown half (the left or top half of <see cref="Picture"/>).</summary>
        public IReadOnlyList<string> ShownHalf => Half(Picture, Axis, first: true);

        /// <summary>Pegs rounds: every hole that must get a peg.</summary>
        public IReadOnlyList<string> PegsToPlace => Mode == Mode.Pegs ? Choices.Where(c => IsPegTarget(this, c)).ToList() : [];
    }

    private readonly MirrorFile _file = content.Mirror;

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } = [new("pegs", "Peg board")];
    public MirrorFile File => _file;

    // MARK: Grids

    /// <summary>Folds onto itself along this line (a square grid; "diagonal" runs top left to bottom right).</summary>
    public static bool Symmetric(IReadOnlyList<string> rows, string line)
    {
        var n = rows.Count;
        if (n == 0) return false;
        return line switch
        {
            Vertical => rows.All(r => r.SequenceEqual(r.Reverse())),
            Horizontal => Enumerable.Range(0, n).All(i => rows[i] == rows[n - 1 - i]),
            Diagonal => rows.All(r => r.Length == n) && Enumerable.Range(0, n).All(i => Enumerable.Range(0, n).All(j => rows[i][j] == rows[j][i])),
            _ => false,
        };
    }

    /// <summary>The left (or top) half when <paramref name="first"/>, else the right (or bottom) half.</summary>
    public static IReadOnlyList<string> Half(IReadOnlyList<string> rows, string axis, bool first)
    {
        if (rows.Count == 0) return [];
        if (axis == Vertical)
        {
            var w = rows[0].Length / 2;
            return rows.Select(r => first ? r[..w] : r[(r.Length - w)..]).ToList();
        }
        var h = rows.Count / 2;
        return first ? rows.Take(h).ToList() : rows.Skip(rows.Count - h).ToList();
    }

    /// <summary>A half flipped in the mirror: what the other side must look like.</summary>
    public static IReadOnlyList<string> Reflect(IReadOnlyList<string> half, string axis) =>
        axis == Vertical ? half.Select(r => new string(r.Reverse().ToArray())).ToList() : half.Reverse().ToList();

    private static bool SameGrid(IReadOnlyList<string> a, IReadOnlyList<string> b) => a.SequenceEqual(b);

    // MARK: Rounds

    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var level = this.Level(learner);
        var axis = level.Mirror ?? Vertical;
        foreach (var mode in rng.Shuffled((level.Modes ?? ["same"]).Select(m => Enum.Parse<Mode>(m, ignoreCase: true))))
        {
            var round = mode switch
            {
                Mode.Same => Same(session, rng),
                Mode.Finish => Finish(axis, session, rng),
                Mode.Pegs => Pegs(axis, rng),
                _ => Line(session, rng),
            };
            if (round != null) return round;
        }
        return null;
    }

    private List<MirrorPicture> Fresh(GameSession session, Func<MirrorPicture, bool> keep) =>
        _file.Pictures.Where(p => keep(p) && !session.UsedItems.Contains($"picture:{p.Id}")).ToList();

    /// <summary>The picture, and two copies with a 2 x 2 patch on one side painted differently.</summary>
    private Round? Same(GameSession session, IRandomSource rng)
    {
        if (rng.Pick(Fresh(session, p => p.Mirror == Vertical)) is not { } picture) return null;
        var rows = picture.Rows;
        var n = rows.Count;
        var paints = _file.Paints.Keys.Select(k => k[0]).Append(Empty).ToList();
        var candidates = new List<IReadOnlyList<string>> { rows };
        for (var tries = 0; candidates.Count < 3 && tries < 100; tries++)
        {
            var grid = rows.Select(r => r.ToCharArray()).ToArray();
            var top = rng.NextInt(0, n - 2);
            var left = rng.NextInt(0, n / 2 - 2);
            var paint = paints[rng.NextIndex(paints.Count)];
            for (var r = top; r < top + 2; r++)
                for (var c = left; c < left + 2; c++)
                    grid[r][c] = paint;
            var broken = grid.Select(g => new string(g)).ToList();
            if (!Symmetric(broken, Vertical) && !candidates.Any(c => SameGrid(c, broken))) candidates.Add(broken);
        }
        if (candidates.Count < 3) return null;
        var order = rng.Shuffled(candidates);
        return new Round(Mode.Same, Vertical, rows, ["0", "1", "2"], order, picture.Id);
    }

    /// <summary>The flipped half, the unflipped half (when that differs) and another picture's half.</summary>
    private Round? Finish(string axis, GameSession session, IRandomSource rng)
    {
        if (rng.Pick(Fresh(session, p => p.Mirror == axis)) is not { } picture) return null;
        var shown = Half(picture.Rows, axis, first: true);
        var right = Reflect(shown, axis);
        var candidates = new List<IReadOnlyList<string>> { right };
        void Offer(IReadOnlyList<string> half)
        {
            if (candidates.Count < 3 && !candidates.Any(c => SameGrid(c, half))) candidates.Add(half);
        }
        Offer(shown); // copied straight across without flipping
        foreach (var other in rng.Shuffled(_file.Pictures.Where(p => p.Id != picture.Id && p.Mirror == axis)))
            Offer(Half(other.Rows, axis, first: false));
        if (candidates.Count < 3) return null;
        return new Round(Mode.Finish, axis, picture.Rows, ["0", "1", "2"], rng.Shuffled(candidates), picture.Id);
    }

    /// <summary>A few pegs on the left (or top) of an empty board; the choices are the holes on the other side.</summary>
    private Round? Pegs(string axis, IRandomSource rng)
    {
        var board = _file.Pegs;
        var n = board.Size;
        var peg = _file.Paints.FirstOrDefault(p => p.Value == board.Colour).Key?[0] ?? 'P';
        var half = n / 2;
        var holes = new List<(int R, int C)>();
        for (var r = 0; r < n; r++)
            for (var c = 0; c < n; c++)
                if (axis == Vertical ? c < half : r < half) holes.Add((r, c));
        var count = rng.NextInt(board.Fewest, board.Most);
        var placed = rng.Shuffled(holes).Take(count).ToHashSet();
        var rows = Enumerable.Range(0, n)
            .Select(r => new string(Enumerable.Range(0, n).Select(c => placed.Contains((r, c)) ? peg : Empty).ToArray()))
            .ToList();
        var choices = new List<string>();
        for (var r = 0; r < n; r++)
            for (var c = 0; c < n; c++)
                if (axis == Vertical ? c >= half : r >= half) choices.Add($"{r},{c}");
        return new Round(Mode.Pegs, axis, rows, choices, []);
    }

    private Round? Line(GameSession session, IRandomSource rng)
    {
        if (rng.Pick(Fresh(session, _ => true)) is not { } picture) return null;
        return new Round(Mode.Line, picture.Mirror, picture.Rows, rng.Shuffled(new[] { Vertical, Horizontal, Diagonal }), [], picture.Id);
    }

    // MARK: Answers

    private static bool IsPegTarget(Round round, string hole)
    {
        var parts = hole.Split(',');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var r) || !int.TryParse(parts[1], out var c)) return false;
        var n = round.Picture.Count;
        if (r < 0 || c < 0 || r >= n || c >= n) return false;
        var (mr, mc) = round.Axis == Vertical ? (r, n - 1 - c) : (n - 1 - r, c);
        return round.Picture[r][c] == Empty && round.Picture[mr][mc] != Empty;
    }

    public bool IsCorrect(string choice, Round round)
    {
        switch (round.Mode)
        {
            case Mode.Same:
                return int.TryParse(choice, out var i) && i >= 0 && i < round.Candidates.Count && Symmetric(round.Candidates[i], Vertical);
            case Mode.Finish:
                return int.TryParse(choice, out var j) && j >= 0 && j < round.Candidates.Count
                       && SameGrid(round.Candidates[j], Reflect(round.ShownHalf, round.Axis));
            case Mode.Pegs:
                return IsPegTarget(round, choice);
            default:
                return Symmetric(round.Picture, choice);
        }
    }

    public string SkillId(Round round) => round.Mode switch
    {
        Mode.Same => "symmetry_halves",
        Mode.Finish => "symmetry_finish",
        _ => "symmetry_lines",
    };
}
