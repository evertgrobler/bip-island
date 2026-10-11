using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// Block Towers' drawing kit: Bip's hand-drawn building cubes, towers, tens (ten cubes snapped
/// together), the cube pile and the tick. No faces or characters: the cubes are plain blocks.
/// Towers stand on their base: a node's (0, 0) is the middle of the bottom edge.
/// </summary>
public static class BlockDrawing
{
    /// <summary>A tower taller than this stands as a column of ten with the rest beside it (14 = ten and four).</summary>
    public const int Column = 10;

    /// <summary>A second shade of a colour, for the other tower when two join.</summary>
    public static Color Lighter(Color colour) => colour.Lerp(Palette.White, 0.45f);

    /// <summary>
    /// One cube, centred on (0, 0): a flat fill under a hand-drawn outline. (A wobbly outline this
    /// small can cross itself at the corners, and the sketch pen then leaves the fill out.)
    /// </summary>
    public static Node2D Cube(Color colour, double size, ulong seed)
    {
        var cube = new Node2D();
        var half = size / 2 - 2;
        cube.AddChild(new BlockPatch(RoundedBox(-half, -half, half * 2, half * 2, size * 0.16), colour));
        // A little shine in the top corner, so it reads as a block rather than a flat square.
        cube.AddChild(new BlockPatch(RoundedBox(-half * 0.6, -half * 0.6, half * 0.5, half * 0.3, size * 0.06), Palette.White.WithAlpha(0.55f)));
        cube.AddChild(Pen(RoundRect(R(-half, -half, half * 2, half * 2), size * 0.16), seed, lineWidth: Math.Max(3, size * 0.08), wobble: 1.2));
        return cube;
    }

    /// <summary>A rounded box's outline in Godot coordinates (x, y the top-left corner).</summary>
    public static Vector2[] RoundedBox(double x, double y, double width, double height, double radius)
    {
        var r = (float)Math.Min(radius, Math.Min(width, height) / 2);
        var (left, top, right, bottom) = ((float)x, (float)y, (float)(x + width), (float)(y + height));
        var points = new List<Vector2>();
        foreach (var (cx, cy, start) in new[] { (right - r, top + r, -Mathf.Pi / 2), (right - r, bottom - r, 0f),
                                                (left + r, bottom - r, Mathf.Pi / 2), (left + r, top + r, Mathf.Pi) })
            for (var step = 0; step <= 3; step++)
            {
                var a = start + step / 3f * (Mathf.Pi / 2);
                points.Add(new Vector2(cx + r * Mathf.Cos(a), cy + r * Mathf.Sin(a)));
            }
        return points.ToArray();
    }


    /// <summary>How many columns a tower of <paramref name="count"/> cubes stands in.</summary>
    public static int Columns(int count) => Math.Max(1, (count + Column - 1) / Column);

    /// <summary>Where cube <paramref name="index"/> of a tower of <paramref name="count"/> sits (from the bottom up, then the next column).</summary>
    public static Vector2 CubeAt(int index, int count, double size)
    {
        var columns = Columns(count);
        var column = index / Column;
        var row = index % Column;
        return P((column - (columns - 1) / 2.0) * size, row * size + size / 2);
    }

    /// <summary>A tower with one colour per cube, bottom first.</summary>
    public static Node2D Tower(IReadOnlyList<Color> cubes, double size, ulong seed)
    {
        var tower = new Node2D();
        for (var i = 0; i < cubes.Count; i++)
        {
            var cube = Cube(cubes[i], size, seed + (ulong)(i * 3));
            cube.Position = CubeAt(i, cubes.Count, size);
            tower.AddChild(cube);
        }
        return tower;
    }

    public static Node2D Tower(int count, Color colour, double size, ulong seed) =>
        Tower(Enumerable.Repeat(colour, count).ToList(), size, seed);

    /// <summary>The box a tower of <paramref name="count"/> takes up, in Godot coordinates (y down), at least <paramref name="least"/> each way.</summary>
    public static Rect2 TowerArea(int count, double size, double least = 130)
    {
        var width = Math.Max(least, Columns(count) * size + 20);
        var height = Math.Max(least, Math.Min(Math.Max(count, 1), Column) * size + 20);
        return new Rect2(-(float)width / 2, -(float)height, (float)width, (float)height);
    }

    /// <summary>
    /// A number as pairs: two cubes side by side on each row, so an odd number has one cube left
    /// on its own at the top (drawn a little apart, with an orange outline).
    /// </summary>
    public static Node2D Pairs(int count, Color colour, double size, ulong seed)
    {
        var node = new Node2D();
        for (var i = 0; i < count; i++)
        {
            var row = i / 2;
            var lone = i == count - 1 && count % 2 == 1;
            var cube = Cube(colour, size, seed + (ulong)(i * 3));
            cube.Position = P(lone ? -size * 0.7 : (i % 2 == 0 ? -size / 2 : size / 2), row * size + size / 2 + (lone ? size * 0.25 : 0));
            node.AddChild(cube);
            if (lone)
            {
                var ring = Pen(RoundRect(R(-size * 0.62, -size * 0.62, size * 1.24, size * 1.24), size * 0.2), seed + 99,
                               ink: Palette.Orange, lineWidth: Math.Max(4, size * 0.1), wobble: 1.5);
                ring.Position = cube.Position;
                node.AddChild(ring);
            }
        }
        return node;
    }

    /// <summary>A ten: ten cubes snapped into one stick, standing on its base.</summary>
    public static Node2D TenStick(Color colour, double size, ulong seed)
    {
        var stick = new Node2D();
        var half = size / 2 - 2;
        stick.AddChild(new BlockPatch(RoundedBox(-half, -(size * Column - 2), half * 2, size * Column - 4, size * 0.16), colour));
        stick.AddChild(Pen(RoundRect(R(-half, 2, half * 2, size * Column - 4), size * 0.16), seed, lineWidth: Math.Max(3, size * 0.08), wobble: 1.2));
        for (var i = 1; i < Column; i++)
            stick.AddChild(Pen(Polyline(P(-half, i * size), P(half, i * size)), seed + (ulong)i, ink: Palette.Ink.WithAlpha(0.45f), lineWidth: 2, wobble: 0.8));
        return stick;
    }

    /// <summary>A tray heaped with cubes: tap it to take one.</summary>
    public static Node2D Pile(Color colour, ulong seed, string? label = null)
    {
        var pile = new Node2D();
        pile.AddChild(Pen(RoundRect(R(-110, -70, 220, 70), 26), seed, fill: Palette.LightBrown, lineWidth: 6));
        // A heap of cubes in the tray: three, then two, then one on top.
        var i = 0;
        foreach (var (row, count) in Indexed(3, 2, 1))
            for (var c = 0; c < count; c++)
            {
                var cube = Cube(colour, 44, seed + 10 + (ulong)(i++ * 3));
                cube.Position = P((c - (count - 1) / 2.0) * 46, -14 + row * 44);
                pile.AddChild(cube);
            }
        if (label != null)
        {
            var text = Sketch.Label(label, 30, Palette.Ink);
            var holder = new Node2D { Position = P(0, -100) };
            holder.AddChild(text);
            pile.AddChild(holder);
        }
        return pile;
    }

    /// <summary>A bundle of tens for the tens pile.</summary>
    public static Node2D TensPile(Color colour, ulong seed)
    {
        var pile = new Node2D();
        pile.AddChild(Pen(RoundRect(R(-110, -70, 220, 70), 26), seed, fill: Palette.LightBrown, lineWidth: 6));
        // Three tens lying in the tray, one on top of the other.
        for (var i = 0; i < 3; i++)
        {
            var stick = TenStick(colour, 19, seed + 20 + (ulong)(i * 13));
            stick.Rotation = Turn(-Math.PI / 2);
            stick.Position = P(-95, -50 + i * 21);
            pile.AddChild(stick);
        }
        return pile;
    }

    /// <summary>The big green tick: "I've finished".</summary>
    public static Node2D Tick(ulong seed)
    {
        var tick = new Node2D();
        tick.AddChild(Pen(Ellipse(Vector2.Zero, 78, 78), seed, fill: Palette.Go, lineWidth: 6));
        tick.AddChild(Pen(Polyline(P(-36, 2), P(-10, -26), P(38, 28)), seed + 1, ink: Palette.White, lineWidth: 16, wobble: 1));
        return tick;
    }

    /// <summary>Dotted outlines for the empty spaces of a tower (rows <paramref name="from"/> up to <paramref name="to"/>).</summary>
    public static Node2D Ghost(int from, int to, double size, ulong seed)
    {
        var ghost = new Node2D();
        for (var i = from; i < to; i++)
        {
            var half = size / 2 - 4;
            var outline = Pen(RoundRect(R(-half, -half, half * 2, half * 2), size * 0.16), seed + (ulong)i,
                              fill: Palette.Card.WithAlpha(0.6f), ink: Palette.Ink.WithAlpha(0.3f), lineWidth: 3, wobble: 2.5);
            outline.Position = CubeAt(i, to, size);
            ghost.AddChild(outline);
        }
        return ghost;
    }

    /// <summary>A wooden board for towers to stand on; its top edge is at (0, 0).</summary>
    public static Node2D Board(double width, ulong seed)
    {
        var board = new Node2D();
        board.AddChild(new BlockPatch(RoundedBox(-width / 2, 0, width, 26, 10), Palette.LightBrown));
        board.AddChild(Pen(RoundRect(R(-width / 2, -26, width, 26), 10), seed, lineWidth: 5, wobble: 1.5));
        return board;
    }

    /// <summary>A card with a big numeral, so the child sees the number they hear.</summary>
    public static Node2D NumberCard(int number, ulong seed)
    {
        var card = new Node2D();
        var width = number >= 10 ? 220 : 170;
        card.AddChild(Pen(RoundRect(R(-width / 2.0, -85, width, 170), 30), seed, fill: Palette.Card, lineWidth: 6));
        card.AddChild(Sketch.Letter(number.ToString(), 120, shadow: Palette.Orange));
        return card;
    }
}

/// <summary>A plain filled shape with no outline.</summary>
public partial class BlockPatch : Node2D
{
    private readonly Vector2[] _points = [];
    private readonly Color _colour;

    public BlockPatch() { }

    public BlockPatch(Vector2[] points, Color colour)
    {
        _points = points;
        _colour = colour;
    }

    public override void _Draw()
    {
        if (_points.Length >= 3) DrawColoredPolygon(_points, _colour);
    }
}
