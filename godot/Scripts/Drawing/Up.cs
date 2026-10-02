using System.Collections.Generic;
using System.Linq;
using Godot;

namespace BipIsland.Drawing;

/// <summary>
/// Shapes in the Swift app's y-up coordinates, flipped into Godot's y-down space. Ported drawings
/// keep the Swift numbers and seeds exactly, so they can be checked side by side with the Mac version:
/// every point goes through <see cref="P"/>, every rectangle through <see cref="R"/>, and angles are negated.
/// </summary>
public static class Up
{
    /// <summary>A point given y-up (Swift), as a Godot point.</summary>
    public static Vector2 P(double x, double y) => new((float)x, (float)-y);

    /// <summary>A rectangle given by its bottom-left corner y-up (Swift CGRect), as a Godot rectangle.</summary>
    public static Rect2 R(double x, double y, double w, double h) => new((float)x, (float)-(y + h), (float)w, (float)h);

    /// <summary>Sketch.Node taking the Swift numbers as they are (doubles), with the same parameter names.</summary>
    public static SketchNode Pen(SketchShape shape, ulong seed, Color? fill = null, Color? ink = null,
                                 double lineWidth = 5, double wobble = 2.2) =>
        Sketch.Node(shape, seed, fill, ink, (float)lineWidth, (float)wobble);

    /// <summary>A Swift rotation (counter-clockwise) as a Godot one (clockwise).</summary>
    public static float Turn(double radians) => (float)-radians;

    public static SketchShape Ellipse(Vector2 center, double rx, double ry) => new SketchShape.Ellipse(center, (float)rx, (float)ry);
    public static SketchShape RoundRect(Rect2 rect, double radius) => new SketchShape.RoundedRect(rect, (float)radius);
    public static SketchShape Polygon(params Vector2[] points) => new SketchShape.Polygon(points);
    public static SketchShape Polygon(IEnumerable<Vector2> points) => new SketchShape.Polygon(points.ToArray());
    public static SketchShape Polyline(params Vector2[] points) => new SketchShape.Polyline(points);
    public static SketchShape Polyline(IEnumerable<Vector2> points) => new SketchShape.Polyline(points.ToArray());

    /// <summary>An arc from one Swift angle to another; the angles are mirrored for y-down.</summary>
    public static SketchShape Arc(Vector2 center, double rx, double ry, double from, double to) =>
        new SketchShape.Arc(center, (float)rx, (float)ry, (float)-from, (float)-to);

    /// <summary>A five-pointed star around a Swift-space centre (points up on screen).</summary>
    public static Vector2[] Star(Vector2 center, double radius) => Sketch.StarPoints(center, (float)radius);

    /// <summary>A plain filled oval with no outline (Swift's SKShapeNode(ellipseOf:)), e.g. a shine.</summary>
    public static Node2D Oval(double width, double height, Color colour) => new Oval((float)width / 2, (float)height / 2, colour);

    /// <summary>A filled ink dot at a Swift-space point.</summary>
    public static Node2D Dot(double x, double y, double radius, Color? colour = null) =>
        new Disc((float)radius, colour ?? Palette.Ink) { Position = P(x, y) };

    public static Color WithAlpha(this Color colour, double alpha) => new(colour, (float)alpha);

    /// <summary>NSColor.blended(withFraction:of:): a mix of two colours.</summary>
    public static Color Blend(this Color colour, double fraction, Color other) => colour.Lerp(other, (float)fraction);

    /// <summary>Swift's <c>array.enumerated()</c>: each item with its index.</summary>
    public static IEnumerable<(int Index, T Item)> Indexed<T>(params T[] items) => items.Select((item, i) => (i, item));

    /// <summary>Makes a node from its children, laid on top of each other in order.</summary>
    public static Node2D Group(params Node2D[] parts)
    {
        var n = new Node2D();
        foreach (var part in parts) n.AddChild(part);
        return n;
    }
}

/// <summary>A filled oval with no outline.</summary>
public partial class Oval : Node2D
{
    private readonly float _rx;
    private readonly float _ry;
    private readonly Color _colour;

    public Oval() { }

    public Oval(float rx, float ry, Color colour)
    {
        _rx = rx;
        _ry = ry;
        _colour = colour;
    }

    public override void _Draw()
    {
        var points = new Vector2[48];
        for (var i = 0; i < points.Length; i++)
        {
            var a = (float)i / points.Length * Mathf.Tau;
            points[i] = new Vector2(_rx * Mathf.Cos(a), _ry * Mathf.Sin(a));
        }
        DrawColoredPolygon(points, _colour);
    }
}
