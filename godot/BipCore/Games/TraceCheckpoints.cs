using System.Numerics;

namespace BipCore;

/// <summary>
/// Letter Trace's checkpoints on one letter, and when the letter counts as traced. The checkpoints sit along the
/// letter's own strokes (Content/phonics/trace_shapes.json), so the child follows the letter itself: in any order,
/// lifting the pen as often as they like (the dot of i and j, the second stroke of k), going back over a stroke
/// (a, d) and starting again with a fresh drag. Wide circles, and most of them is enough: tracing is savoured,
/// never gated (owner, 9 Nov 2026: i, j and a couldn't be finished with the old ring of checkpoints).
/// </summary>
public sealed class TraceCheckpoints
{
    /// <summary>The share of checkpoints that finishes the letter: a stroke's very tip may be missed.</summary>
    public const float Enough = 0.9f;

    /// <summary>A checkpoint with no other within this many ems is a stroke on its own (the dot of i and j).</summary>
    public const float AloneEms = 0.16f;

    private readonly Vector2[] _points;
    private readonly bool[] _touched;
    private readonly bool[] _alone;
    private readonly float _radius;

    /// <param name="shape">[x, y] in ems from the letter's centre (trace_shapes.json).</param>
    /// <param name="centre">Where the letter's centre is drawn.</param>
    /// <param name="size">The letter's font size: one em in drawing units.</param>
    /// <param name="radius">How near the pointer must come to a checkpoint.</param>
    public TraceCheckpoints(IReadOnlyList<IReadOnlyList<float>> shape, Vector2 centre, float size, float radius)
    {
        _points = shape.Where(p => p.Count >= 2).Select(p => centre + new Vector2(p[0], p[1]) * size).ToArray();
        _touched = new bool[_points.Length];
        _radius = radius;
        // A stroke of one checkpoint can't be "nearly" traced: it must be touched (the dot of i and j).
        var near = AloneEms * size;
        _alone = _points.Select((p, i) => _points.Where((q, j) => j != i).All(q => Vector2.Distance(p, q) > near)).ToArray();
    }

    public IReadOnlyList<Vector2> Points => _points;
    public int Count => _points.Length;
    public int Touched => _touched.Count(t => t);

    /// <summary>Checkpoints still to reach.</summary>
    public IEnumerable<Vector2> Remaining => _points.Where((_, i) => !_touched[i]);

    /// <summary>
    /// True once enough checkpoints are touched and every stroke of its own (a dot) is: all of them for a letter with
    /// very few.
    /// </summary>
    public bool Done => Count > 0 && Touched >= Needed(Count) && _alone.Select((a, i) => !a || _touched[i]).All(t => t);

    /// <summary>
    /// How many checkpoints a letter with <paramref name="count"/> of them needs: all but a tenth, and always at least
    /// one may be missed from four up (a serif or a stroke's tip), all of them for three or fewer.
    /// </summary>
    public static int Needed(int count) => count <= 3 ? count : count - Math.Max(1, (int)(count * (1 - Enough)));

    /// <summary>Whether <paramref name="at"/> is on the letter's strokes: near any checkpoint, touched or not.</summary>
    public bool Near(Vector2 at) => _points.Any(p => Vector2.Distance(at, p) <= _radius);

    /// <summary>The pen is at <paramref name="at"/>: returns how many checkpoints it reached for the first time.</summary>
    public int Touch(Vector2 at)
    {
        var fresh = 0;
        for (var i = 0; i < _points.Length; i++)
        {
            if (_touched[i] || Vector2.Distance(at, _points[i]) > _radius) continue;
            _touched[i] = true;
            fresh++;
        }
        return fresh;
    }

    /// <summary>
    /// The pen moved from <paramref name="from"/> to <paramref name="to"/> in one step (a fast mouse skips pixels):
    /// every point along the way touches, so a quick stroke counts as much as a slow one.
    /// </summary>
    public int Stroke(Vector2 from, Vector2 to)
    {
        var steps = Math.Max(1, (int)Math.Ceiling(Vector2.Distance(from, to) / (_radius / 2)));
        var fresh = 0;
        for (var s = 1; s <= steps; s++) fresh += Touch(Vector2.Lerp(from, to, s / (float)steps));
        return fresh;
    }
}
