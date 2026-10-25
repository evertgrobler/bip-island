using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>The profile animals (<see cref="Avatars"/>), drawn: each a friendly face that fills its badge.</summary>
public static partial class ArtPictures
{
    private static readonly Dictionary<string, Func<Node2D>> Animals = new()
    {
        ["lion"] = Lion, ["penguin"] = Penguin, ["tortoise"] = Tortoise, ["zebra"] = Zebra,
        ["giraffe"] = Giraffe, ["elephant"] = Elephant, ["crab"] = () => Placed(Crab(), 0, -6, 0.82), ["rhino"] = Rhino,
    };

    /// <summary>A profile animal by its avatar id, or null for an unknown one.</summary>
    public static Node2D? Animal(string id) => Animals.TryGetValue(id, out var draw) ? draw() : null;

    /// <summary>Two friendly eyes and a little smile, shared by the animal faces.</summary>
    private static void Eyes(Pic p, double x, double y, double apart, double r = 9)
    {
        p.Fill(Circle(x - apart, y, r * 1.6), Palette.White, 3).Fill(Circle(x + apart, y, r * 1.6), Palette.White, 3);
        p.Dot(x - apart + 2, y - 1, r).Dot(x + apart + 2, y - 1, r);
        p.Dot(x - apart + 4, y + 3, r * 0.35, Palette.White).Dot(x + apart + 4, y + 3, r * 0.35, Palette.White);
    }

    private static Node2D Lion()
    {
        var p = new Pic(5000);
        p.Fill(Polygon(Enumerable.Range(0, 20).Select(k => (a: k * Math.PI / 10, r: k % 2 == 0 ? 100.0 : 84))
                                  .Select(q => P(q.r * Math.Cos(q.a), q.r * Math.Sin(q.a)))), Palette.Orange.Darkened(0.15f), 5);
        foreach (var side in new[] { -1.0, 1.0 }) p.Fill(Circle(side * 48, 46, 18), Palette.Sun.Darkened(0.05f), 4);
        p.Fill(Circle(0, 0, 66), Palette.Sun.Lightened(0.1f), 5);
        p.Fill(Ellipse(P(0, -26), 32, 24), Palette.Card, 3);
        p.Fill(Polygon(P(-12, -12), P(12, -12), P(0, -24)), Palette.Brown, 3);
        p.Line(Polyline(P(0, -24), P(0, -32), P(-12, -38)), line: 3).Line(Polyline(P(0, -32), P(12, -38)), line: 3);
        Eyes(p, 0, 16, 24, 7);
        return p.Node;
    }

    private static Node2D Penguin()
    {
        var p = new Pic(5020);
        p.Fill(Ellipse(P(0, -10), 76, 92), Palette.Ink.Lightened(0.15f), 5);
        p.Fill(Polygon(Around(0, -10, 56, 76, 0, 2 * Math.PI, 18).Select(v => v + new Vector2(0, 8))), Palette.White, 4);
        p.Fill(Polygon(Around(0, 32, 56, 40, Math.PI * 0.05, Math.PI * 0.95, 10).Concat(new[] { P(-30, 30), P(0, 10), P(30, 30) }.Reverse())), Palette.Ink.Lightened(0.15f), 3);
        Eyes(p, 0, 24, 22, 7);
        p.Fill(Polygon(P(-14, 4), P(14, 4), P(0, -16)), Palette.Orange, 3);
        foreach (var side in new[] { -1.0, 1.0 }) p.Dot(side * 38, -2, 8, Palette.Pink.WithAlpha(0.7));
        return p.Node;
    }

    private static Node2D Tortoise()
    {
        var p = new Pic(5040);
        foreach (var x in new[] { -56.0, 56 }) p.Fill(Ellipse(P(x, -64), 20, 16), Palette.Leaf.Lightened(0.2f), 4);
        p.Fill(Polygon(Around(0, -50, 92, 100, 0, Math.PI, 16)), Palette.Leaf, 5);
        foreach (var (x, y) in new[] { (-44.0, -10.0), (0.0, 10.0), (44.0, -10.0), (-20.0, 30.0), (20.0, 30.0) })
            p.Fill(Polygon(Enumerable.Range(0, 6).Select(k => k * Math.PI / 3).Select(a => P(x + 18 * Math.Cos(a), y + 16 * Math.Sin(a)))), Palette.Grass.Lightened(0.2f), 3);
        p.Fill(Polygon(P(-96, -50), P(96, -50), P(90, -64), P(-90, -64)), Palette.Leaf.Darkened(0.2f), 3);
        p.Fill(Circle(0, -62, 34), Palette.Grass.Lightened(0.25f), 4);
        Eyes(p, 0, -54, 14, 5);
        p.Line(Arc(P(0, -72), 10, 6, Math.PI + 0.4, 2 * Math.PI - 0.4), line: 3);
        return p.Node;
    }

    private static Node2D Zebra()
    {
        var p = new Pic(5060);
        foreach (var side in new[] { -1.0, 1.0 }) p.Fill(Polygon(P(side * 24, 56), P(side * 52, 104), P(side * 56, 50)), Palette.White, 4);
        p.Fill(Polygon(P(-10, 92), P(10, 92), P(16, 50), P(-16, 50)), Palette.Ink, 3);
        p.Fill(Polygon(Around(0, 20, 50, 60, 0, Math.PI, 10).Concat(Around(0, -40, 40, 50, Math.PI, 2 * Math.PI, 10))), Palette.White, 5);
        foreach (var y in new[] { 50.0, 26, 2 })
            foreach (var side in new[] { -1.0, 1.0 })
                p.Fill(Polygon(P(side * 50, y), P(side * 16, y - 4), P(side * 20, y - 14), P(side * 48, y - 10)), Palette.Ink, 2);
        p.Fill(Ellipse(P(0, -60), 38, 30), Palette.Ink.Lightened(0.35f), 4);
        p.Dot(-14, -58, 5).Dot(14, -58, 5);
        Eyes(p, 0, 26, 24, 6);
        return p.Node;
    }

    private static Node2D Giraffe()
    {
        var p = new Pic(5080);
        var coat = Palette.Sun;
        foreach (var side in new[] { -1.0, 1.0 })
        {
            p.Line(Polyline(P(side * 16, 60), P(side * 22, 96)), line: 6);
            p.Fill(Circle(side * 22, 98, 9), Palette.Brown, 3);
            p.Fill(Ellipse(P(side * 64, 44), 26, 12), coat, 4);
        }
        p.Fill(Polygon(Around(0, 20, 52, 50, 0, Math.PI, 10).Concat(Around(0, -40, 42, 56, Math.PI, 2 * Math.PI, 10))), coat, 5);
        foreach (var (x, y, r) in new[] { (-28.0, 40.0, 10.0), (26.0, 46.0, 8.0), (-36.0, 0.0, 8.0), (36.0, 0.0, 9.0) })
            p.Fill(Circle(x, y, r), Palette.Orange.Darkened(0.2f), 2);
        p.Fill(Ellipse(P(0, -66), 36, 26), coat.Lightened(0.3f), 3);
        p.Dot(-12, -62, 5).Dot(12, -62, 5);
        Eyes(p, 0, 20, 22, 6);
        return p.Node;
    }

    private static Node2D Elephant()
    {
        var p = new Pic(5100);
        var skin = new Color("a7adb8");
        foreach (var side in new[] { -1.0, 1.0 })
        {
            p.Fill(Ellipse(P(side * 70, 16), 40, 54), skin, 5);
            p.Fill(Ellipse(P(side * 72, 16), 24, 36), Palette.Pink.WithAlpha(0.8), 3);
        }
        p.Fill(Circle(0, 20, 60), skin, 5);
        p.Line(Polyline(P(0, -10), P(0, -60), P(16, -86), P(30, -80)), Palette.Ink, 32, 0.6).Line(Polyline(P(0, -10), P(0, -60), P(16, -86), P(30, -80)), skin, 22, 0.6);
        Eyes(p, 0, 30, 24, 7);
        return p.Node;
    }

    private static Node2D Rhino()
    {
        var p = new Pic(5120);
        var skin = new Color("9aa2a8");
        foreach (var side in new[] { -1.0, 1.0 }) p.Fill(Polygon(P(side * 30, 50), P(side * 62, 86), P(side * 66, 40)), skin, 4);
        p.Fill(Ellipse(P(0, 0), 66, 70), skin, 5);
        p.Fill(Ellipse(P(0, -44), 46, 30), skin.Lightened(0.15f), 4);
        p.Fill(Polygon(P(-16, -26), P(16, -26), P(4, 30)), Palette.Card, 4);
        p.Dot(-16, -50, 5).Dot(16, -50, 5);
        Eyes(p, 0, 20, 30, 6);
        return p.Node;
    }
}
