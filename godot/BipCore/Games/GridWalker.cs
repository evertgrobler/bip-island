namespace BipCore;

/// <summary>Which way Bip faces on a grid.</summary>
public enum Facing { Up, Right, Down, Left }

/// <summary>
/// Runs a Bip's Path program on its grid: one step at a time, exactly as the scene plays it.
/// Absolute blocks (up, down, left, right) move regardless of facing; forward steps the way Bip
/// faces; turnLeft and turnRight only turn him. Walking into a rock or off the grid fails, and
/// Bip must finish on the battery: passing over it doesn't count.
/// </summary>
public static class GridWalker
{
    public static Facing ParseFacing(string text) => text switch
    {
        "right" => Facing.Right,
        "down" => Facing.Down,
        "left" => Facing.Left,
        _ => Facing.Up,
    };

    /// <summary>The facing as the content writes it ("up", "right").</summary>
    public static string Key(this Facing facing) => facing switch
    {
        Facing.Right => "right",
        Facing.Down => "down",
        Facing.Left => "left",
        _ => "up",
    };

    private static Facing Turned(Facing facing, bool left) => (Facing)(((int)facing + (left ? 3 : 1)) % 4);

    private static (int Rows, int Cols) Step(Facing facing) => facing switch
    {
        Facing.Up => (-1, 0),
        Facing.Down => (1, 0),
        Facing.Left => (0, -1),
        _ => (0, 1),
    };

    /// <summary>Whether the program lands Bip on the goal without crashing.</summary>
    public static bool ReachesGoal(IReadOnlyList<string> program, GridLevel level)
    {
        var (positions, crashed) = Path(program, level);
        return !crashed && positions[^1] == level.Goal;
    }

    /// <summary>
    /// Every square Bip stands on, starting square first. Crashed is true when a block is
    /// unknown or Bip walks into a rock or off the grid; the positions stop where he crashed.
    /// The scene steps through these one at a time with a highlight on the current block.
    /// </summary>
    public static (List<GridPosition> Positions, bool Crashed) Path(IReadOnlyList<string> program, GridLevel level)
    {
        var at = level.Start;
        var facing = ParseFacing(level.StartFacing);
        var positions = new List<GridPosition> { at };
        foreach (var block in program)
        {
            switch (block)
            {
                case "up": at = at with { Row = at.Row - 1 }; break;
                case "down": at = at with { Row = at.Row + 1 }; break;
                case "left": at = at with { Column = at.Column - 1 }; break;
                case "right": at = at with { Column = at.Column + 1 }; break;
                case "forward":
                    var (rows, cols) = Step(facing);
                    at = new GridPosition(at.Row + rows, at.Column + cols);
                    break;
                case "turnLeft": facing = Turned(facing, left: true); break;
                case "turnRight": facing = Turned(facing, left: false); break;
                default: return (positions, true);
            }
            if (block is "turnLeft" or "turnRight")
            {
                positions.Add(at);
                continue;
            }
            if (at.Row < 0 || at.Column < 0 || at.Row >= level.Grid.Rows || at.Column >= level.Grid.Cols) return (positions, true);
            if (level.Rocks.Contains(at)) return (positions, true);
            positions.Add(at);
        }
        return (positions, false);
    }

    /// <summary>
    /// Which way Bip faces before the first block and after each block that ran, matching
    /// <see cref="Path"/>'s positions one for one (so the scene can turn him as he walks).
    /// </summary>
    public static List<Facing> Facings(IReadOnlyList<string> program, GridLevel level)
    {
        var walked = Path(program, level).Positions.Count;
        var facing = ParseFacing(level.StartFacing);
        var facings = new List<Facing> { facing };
        foreach (var block in program)
        {
            if (facings.Count >= walked) break;
            if (block == "turnLeft") facing = Turned(facing, left: true);
            if (block == "turnRight") facing = Turned(facing, left: false);
            facings.Add(facing);
        }
        return facings;
    }
}
