using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// Art Island's drawing kit: flat shapes from art/shapes.json, paint pots and splodges, and peg
/// boards for Mirror Magic. Everything keeps the wobbly hand-drawn ink of the rest of the game.
/// </summary>
public static class ArtDrawing
{
    /// <summary>The palette colours shape pictures name ("sun", "teal").</summary>
    public static Color Named(string colour) => colour switch
    {
        "sun" => Palette.Sun,
        "red" => Palette.Red,
        "orange" => Palette.Orange,
        "brown" => Palette.Brown,
        "sky" => Palette.EggBlue,
        "ink" => Palette.Ink,
        "white" => Palette.White,
        "teal" => Palette.Teal,
        "leaf" => Palette.Leaf,
        "sand" => Palette.Sand,
        "pink" => Palette.Pink,
        "purple" => Palette.Purple,
        _ => Palette.Stone,
    };

    /// <summary>A paint colour from art/paints.json.</summary>
    public static Color Paint(PaintColour colour) => new(colour.Hex);

    /// <summary>Bright colours for shapes on their own; the colour never gives the answer away.</summary>
    public static readonly Color[] ShapeColours =
        [Palette.Sun, Palette.Red, Palette.Teal, Palette.Purple, Palette.Orange, Palette.Pink, Palette.Leaf, Palette.EggBlue];

    /// <summary>The outline of a shape <paramref name="width"/> by <paramref name="height"/>, centred on (0, 0).</summary>
    public static SketchShape Outline(ArtShape shape, double width, double height)
    {
        double rx = width / 2, ry = height / 2;
        switch (shape.Outline.Kind)
        {
            case "ellipse":
                return Ellipse(Vector2.Zero, rx, ry);
            case "semicircle":
                // Flat side down, the curve filling the box (as scripts/validate_content.py measures it).
                var arc = Enumerable.Range(0, 25).Select(i => Math.PI * i / 24)
                    .Select(a => P(rx * Math.Cos(a), ry * (2 * Math.Sin(a) - 1)));
                return Polygon(arc);
            default:
                return Polygon((shape.Outline.Points ?? []).Select(p => P(p[0] * rx, p[1] * ry)));
        }
    }

    /// <summary>A filled shape. <paramref name="degrees"/> turns it anticlockwise.</summary>
    public static Node2D Shape(ArtShape shape, double width, double height, Color fill, ulong seed,
                               double degrees = 0, double lineWidth = 6)
    {
        var holder = new Node2D { Rotation = Turn(degrees * Math.PI / 180) };
        holder.AddChild(Pen(Outline(shape, width, height), seed, fill: fill, lineWidth: lineWidth));
        return holder;
    }

    /// <summary>Width and height for a shape shown on its own, fitting inside <paramref name="size"/>.</summary>
    public static (double W, double H) CardSize(ArtShape shape, double size)
    {
        double w = shape.Card[0], h = shape.Card[1];
        var scale = size / Math.Max(w, h);
        return (w * scale, h * scale);
    }

    /// <summary>A dashed-looking gap where a shape belongs: pale, with a soft outline.</summary>
    public static Node2D Gap(ArtShape shape, double width, double height, ulong seed, double degrees = 0)
    {
        var holder = new Node2D { Rotation = Turn(degrees * Math.PI / 180) };
        holder.AddChild(Pen(Outline(shape, width, height), seed, fill: Palette.Card.WithAlpha(0.85),
                            ink: Palette.Ink.WithAlpha(0.35), lineWidth: 5, wobble: 3.5));
        var mark = Sketch.Label("?", (int)Math.Clamp(Math.Min(width, height) * 0.45, 30, 90), Palette.Ink.WithAlpha(0.45));
        holder.AddChild(mark);
        return holder;
    }

    /// <summary>A paint pot: a jar of paint with a label picture on the front.</summary>
    public static Node2D Pot(PaintColour colour, string label, ulong seed)
    {
        var pot = new Node2D();
        var paint = Paint(colour);
        pot.AddChild(Pen(RoundRect(R(-80, -95, 160, 170), 30), seed, fill: Palette.Card, lineWidth: 6));
        pot.AddChild(Pen(RoundRect(R(-72, -88, 144, 120), 24), seed + 1, fill: paint, lineWidth: 4));
        // A drip of paint over the rim.
        pot.AddChild(Pen(Ellipse(P(-30, 40), 22, 14), seed + 2, fill: paint, lineWidth: 3));
        pot.AddChild(Pen(RoundRect(R(-92, 60, 184, 30), 12), seed + 3, fill: Palette.Stone, lineWidth: 5));
        var picture = PictureNode.Make(label, colour.Name);
        picture.Scale = Vector2.One * 0.32f;
        picture.Position = P(0, -30);
        var plate = Pen(Ellipse(P(0, -30), 46, 46), seed + 4, fill: Palette.Card, lineWidth: 3);
        pot.AddChild(plate);
        pot.AddChild(picture);
        return pot;
    }

    /// <summary>A splodge of paint (a wobbly blob).</summary>
    public static Node2D Splodge(Color colour, double radius, ulong seed)
    {
        var holder = new Node2D();
        holder.AddChild(Pen(Ellipse(Vector2.Zero, radius, radius * 0.86), seed, fill: colour, lineWidth: 5, wobble: radius * 0.08));
        holder.AddChild(Pen(Ellipse(P(radius * 0.72, -radius * 0.55), radius * 0.2, radius * 0.18), seed + 1, fill: colour, lineWidth: 4));
        return holder;
    }

    /// <summary>The mixing bowl, seen from the front, with room for paint inside.</summary>
    public static Node2D Bowl(ulong seed)
    {
        var bowl = new Node2D();
        bowl.AddChild(Pen(Ellipse(P(0, 0), 190, 120), seed, fill: Palette.White, lineWidth: 7));
        bowl.AddChild(Pen(Ellipse(P(0, 30), 160, 70), seed + 1, fill: Palette.Stone.WithAlpha(0.5), lineWidth: 4));
        return bowl;
    }

    /// <summary>Paint in the bowl (drawn inside it), or nothing for an empty bowl.</summary>
    public static Node2D BowlPaint(Color colour, ulong seed)
    {
        var holder = new Node2D();
        holder.AddChild(Pen(Ellipse(P(0, 30), 150, 60), seed, fill: colour, lineWidth: 4, wobble: 4));
        return holder;
    }

    /// <summary>
    /// A peg board drawing <paramref name="rows"/> (top row first, '.' an empty hole) with holes
    /// <paramref name="cell"/> apart, centred on (0, 0). Paint letters come from art/mirror.json.
    /// </summary>
    public static Node2D PegGrid(IReadOnlyList<string> rows, double cell, IReadOnlyDictionary<string, string> paints,
                                 Func<string, PaintColour?> colour, ulong seed, bool board = true)
    {
        var grid = new Node2D();
        var height = rows.Count;
        var width = height == 0 ? 0 : rows[0].Length;
        if (board)
            grid.AddChild(Pen(RoundRect(R(-width * cell / 2 - cell * 0.3, -height * cell / 2 - cell * 0.3,
                                          width * cell + cell * 0.6, height * cell + cell * 0.6), cell * 0.4),
                              seed, fill: Palette.LightBrown.Blend(0.5, Palette.Card), lineWidth: 6));
        for (var r = 0; r < height; r++)
        {
            for (var c = 0; c < width; c++)
            {
                var at = PegCentre(r, c, width, height, cell);
                var letter = rows[r][c].ToString();
                var paint = letter != "." && paints.TryGetValue(letter, out var id) ? colour(id) : null;
                grid.AddChild(paint == null
                    ? Dot(at.X, -at.Y, cell * 0.14, Palette.Ink.WithAlpha(0.3))
                    : Pen(Ellipse(at, cell * 0.42, cell * 0.42), seed + 1 + (ulong)(r * width + c), fill: Paint(paint),
                          lineWidth: Math.Max(2, cell * 0.07), wobble: 1));
            }
        }
        return grid;
    }

    /// <summary>Where a hole's centre is (in Godot coordinates) on a board centred on (0, 0).</summary>
    public static Vector2 PegCentre(int row, int col, int width, int height, double cell) =>
        P((col - (width - 1) / 2.0) * cell, ((height - 1) / 2.0 - row) * cell);

    /// <summary>A peg on its own (for the pegs the child places).</summary>
    public static Node2D Peg(Color colour, double cell, ulong seed) =>
        Pen(Ellipse(Vector2.Zero, cell * 0.42, cell * 0.42), seed, fill: colour, lineWidth: Math.Max(2, cell * 0.07), wobble: 1);

    /// <summary>The mirror: a silvery line across a board of <paramref name="length"/>.</summary>
    public static Node2D MirrorLine(string axis, double length, ulong seed)
    {
        var half = length / 2;
        var holder = new Node2D();
        var (from, to) = axis switch
        {
            MirrorMagicGame.Horizontal => (P(-half, 0), P(half, 0)),
            MirrorMagicGame.Diagonal => (P(-half, half), P(half, -half)),
            _ => (P(0, half), P(0, -half)),
        };
        holder.AddChild(Pen(Polyline(from, to), seed, ink: Palette.White, lineWidth: 16, wobble: 1));
        holder.AddChild(Pen(Polyline(from, to), seed + 1, ink: Palette.Teal, lineWidth: 7, wobble: 1));
        return holder;
    }
}
