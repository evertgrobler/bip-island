using System;
using System.Collections.Generic;
using Godot;

namespace BipIsland.Drawing;

/// <summary>
/// A shape drawn with a slightly wobbly hand. Godot coordinates: y grows downwards, angles turn clockwise.
/// (The Swift app is y-up; when porting a drawing, negate its y values and angles.)
/// </summary>
public abstract record SketchShape
{
    public sealed record Ellipse(Vector2 Center, float Rx, float Ry) : SketchShape;
    public sealed record RoundedRect(Rect2 Rect, float Radius) : SketchShape;
    public sealed record Polygon(Vector2[] Points) : SketchShape;
    public sealed record Polyline(Vector2[] Points) : SketchShape;
    /// <summary>An open arc of an ellipse from one angle to another (radians).</summary>
    public sealed record Arc(Vector2 Center, float Rx, float Ry, float From, float To) : SketchShape;

    public bool IsClosed => this is not (Polyline or Arc);

    public List<Vector2> Outline(float spacing = 18)
    {
        switch (this)
        {
            case Ellipse e:
            {
                var count = Math.Max(14, (int)(2 * Mathf.Pi * Math.Max(e.Rx, e.Ry) / spacing));
                var result = new List<Vector2>(count);
                for (var i = 0; i < count; i++)
                {
                    var a = (float)i / count * 2 * Mathf.Pi;
                    result.Add(new Vector2(e.Center.X + e.Rx * Mathf.Cos(a), e.Center.Y + e.Ry * Mathf.Sin(a)));
                }
                return result;
            }
            case Arc arc:
            {
                var count = Math.Max(6, (int)(Math.Abs(arc.To - arc.From) * Math.Max(arc.Rx, arc.Ry) / spacing));
                var result = new List<Vector2>(count + 1);
                for (var i = 0; i <= count; i++)
                {
                    var a = arc.From + (arc.To - arc.From) * i / count;
                    result.Add(new Vector2(arc.Center.X + arc.Rx * Mathf.Cos(a), arc.Center.Y + arc.Ry * Mathf.Sin(a)));
                }
                return result;
            }
            case RoundedRect rr:
            {
                var rect = rr.Rect;
                var r = Math.Min(rr.Radius, Math.Min(rect.Size.X / 2, rect.Size.Y / 2));
                var min = rect.Position;
                var max = rect.End;
                var corners = new List<Vector2>();
                (Vector2 centre, float start)[] centres =
                {
                    (new Vector2(max.X - r, min.Y + r), -Mathf.Pi / 2),
                    (new Vector2(max.X - r, max.Y - r), 0),
                    (new Vector2(min.X + r, max.Y - r), Mathf.Pi / 2),
                    (new Vector2(min.X + r, min.Y + r), Mathf.Pi),
                };
                foreach (var (centre, start) in centres)
                    for (var step = 0; step <= 4; step++)
                    {
                        var a = start + step / 4f * (Mathf.Pi / 2);
                        corners.Add(new Vector2(centre.X + r * Mathf.Cos(a), centre.Y + r * Mathf.Sin(a)));
                    }
                return Sketch.Subdivide(corners, spacing, closed: true);
            }
            case Polygon p:
                return Sketch.Subdivide(p.Points, spacing, closed: true);
            case Polyline l:
                return Sketch.Subdivide(l.Points, spacing, closed: false);
            default:
                throw new InvalidOperationException($"Unknown shape {this}");
        }
    }
}

public static class Sketch
{
    public static List<Vector2> Subdivide(IReadOnlyList<Vector2> points, float spacing, bool closed)
    {
        if (points.Count <= 1) return new List<Vector2>(points);
        var result = new List<Vector2>();
        var edgeCount = closed ? points.Count : points.Count - 1;
        for (var i = 0; i < edgeCount; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            var steps = Math.Max(1, (int)(a.DistanceTo(b) / spacing));
            for (var k = 0; k < steps; k++) result.Add(a.Lerp(b, (float)k / steps));
        }
        if (!closed) result.Add(points[^1]);
        return result;
    }

    public static List<Vector2> Jitter(List<Vector2> points, float amount, ref SeededRandom rng)
    {
        if (amount <= 0) return points;
        var result = new List<Vector2>(points.Count);
        foreach (var p in points)
        {
            var x = p.X + (float)rng.Range(-amount, amount);
            var y = p.Y + (float)rng.Range(-amount, amount);
            result.Add(new Vector2(x, y));
        }
        return result;
    }

    /// <summary>A smooth curve through the points (Catmull–Rom), sampled into short straight pieces.</summary>
    public static Vector2[] Smooth(List<Vector2> p, bool closed, int samplesPerSegment = 6)
    {
        var n = p.Count;
        if (n <= 2)
        {
            var simple = new List<Vector2>(p);
            if (closed && n > 0) simple.Add(p[0]);
            return simple.ToArray();
        }
        var result = new List<Vector2> { p[0] };
        var segments = closed ? n : n - 1;
        for (var i = 0; i < segments; i++)
        {
            var p0 = closed ? p[(i - 1 + n) % n] : p[Math.Max(i - 1, 0)];
            var p1 = p[i];
            var p2 = p[(i + 1) % n];
            var p3 = closed ? p[(i + 2) % n] : p[Math.Min(i + 2, n - 1)];
            var c1 = p1 + (p2 - p0) / 6;
            var c2 = p2 - (p3 - p1) / 6;
            for (var s = 1; s <= samplesPerSegment; s++)
                result.Add(p1.BezierInterpolate(c1, c2, p2, (float)s / samplesPerSegment));
        }
        return result.ToArray();
    }

    public static Vector2[] Path(SketchShape shape, float wobble, ulong seed)
    {
        var rng = new SeededRandom(seed);
        return Smooth(Jitter(shape.Outline(), wobble, ref rng), shape.IsClosed);
    }

    /// <summary>
    /// A hand-drawn shape: a colour fill slightly off the line (like colouring in), a wobbly ink outline,
    /// and a fainter second pass of the pen.
    /// </summary>
    public static SketchNode Node(SketchShape shape, ulong seed, Color? fill = null, Color? ink = null,
                                  float lineWidth = 5, float wobble = 2.2f) =>
        new(shape, seed, fill, ink ?? Palette.Ink, lineWidth, wobble);

    /// <summary>A five-pointed star outline (ten points, tips and dips), pointing up on screen.</summary>
    public static Vector2[] StarPoints(Vector2 center, float radius)
    {
        var points = new Vector2[10];
        for (var i = 0; i < 10; i++)
        {
            var r = i % 2 == 0 ? radius : radius * 0.45f;
            // The Swift app's angles, mirrored for y-down: start at the top and go round.
            var a = -(Mathf.Pi / 2 + i * Mathf.Pi / 5);
            points[i] = new Vector2(center.X + r * Mathf.Cos(a), center.Y + r * Mathf.Sin(a));
        }
        return points;
    }

    /// <summary>A label in the game font, centred on its position.</summary>
    public static Label Label(string text, int size, Color colour, Font? font = null)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        var face = font ?? Fonts.Letters;
        label.AddThemeFontOverride("font", face);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", colour);
        // Size the label to its text now (it isn't in the tree yet), then centre it on (0, 0).
        var textSize = face.GetMultilineStringSize(text, HorizontalAlignment.Center, -1, size);
        label.Size = textSize;
        label.Position = -textSize / 2;
        return label;
    }

    /// <summary>A letter in chalky handwriting with a coloured shadow behind it.</summary>
    public static Node2D Letter(string text, int size, Color? colour = null, Color? shadow = null)
    {
        var container = new Node2D();
        var shadowColour = shadow ?? Palette.Sun;
        var back = Label(text, size, shadowColour);
        back.Position += new Vector2(size * 0.035f, size * 0.035f);
        container.AddChild(back);
        container.AddChild(Label(text, size, colour ?? Palette.Ink));
        return container;
    }
}

/// <summary>Draws one hand-drawn shape. The paths are worked out once, when it is made.</summary>
public partial class SketchNode : Node2D
{
    private readonly Vector2[]? _fill;
    private readonly Color _fillColour;
    private readonly Vector2[]? _line;
    private readonly Vector2[]? _secondPass;
    private readonly Color _ink;
    private readonly float _lineWidth;

    public SketchNode() { }

    public SketchNode(SketchShape shape, ulong seed, Color? fill, Color ink, float lineWidth, float wobble)
    {
        _ink = ink;
        _lineWidth = lineWidth;
        if (fill is { } colour && shape.IsClosed)
        {
            var points = Sketch.Path(shape, wobble * 0.6f, unchecked(seed + 101));
            // A self-crossing outline can't be filled; skip the fill rather than draw garbage.
            if (Geometry2D.TriangulatePolygon(points).Length > 0)
            {
                _fill = points;
                _fillColour = colour;
            }
        }
        if (lineWidth > 0)
        {
            _line = Sketch.Path(shape, wobble, seed);
            _secondPass = Sketch.Path(shape, wobble * 1.4f, unchecked(seed + 7));
        }
    }

    public override void _Draw()
    {
        if (_fill != null)
        {
            DrawSetTransform(new Vector2(2, 2));
            DrawColoredPolygon(_fill, _fillColour);
            DrawSetTransform(Vector2.Zero);
        }
        if (_line != null)
        {
            DrawPolyline(_line, _ink, _lineWidth, antialiased: true);
            // Round the ends, like a felt-tip pen.
            DrawCircle(_line[0], _lineWidth / 2, _ink, antialiased: true);
            DrawCircle(_line[^1], _lineWidth / 2, _ink, antialiased: true);
        }
        if (_secondPass != null)
            DrawPolyline(_secondPass, new Color(_ink, 0.35f), Math.Max(1.5f, _lineWidth * 0.4f), antialiased: true);
    }
}
