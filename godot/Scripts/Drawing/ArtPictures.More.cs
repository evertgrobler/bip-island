using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>The letter-sound pictures of <see cref="ArtPictures"/> (graphemes.json mnemonicPicture).</summary>
public static partial class ArtPictures
{
    private static readonly Dictionary<string, Func<Node2D>> More = new()
    {
        ["pic_map"] = Map,
        ["pic_goat"] = Goat,
        ["pic_octopus"] = Octopus,
        ["pic_rabbit"] = Rabbit,
        ["pic_leg"] = Leg,
        ["pic_kiss"] = Kiss,
        ["pic_jam"] = Jam,
        ["pic_web"] = Web,
        ["pic_yoyo"] = YoYo,
        ["pic_bee"] = () => Bee(false),
        ["pic_buzz"] = () => Bee(true),
        ["pic_queen"] = Queen,
        ["pic_chip"] = Chips,
        ["pic_ship"] = Ship,
        ["pic_thumb"] = Thumb,
        ["pic_this"] = This,
        ["pic_ring"] = Ring,
        ["pic_book"] = Book,
        ["pic_fork"] = Fork,
        ["pic_turn"] = TurnArrow,
        ["pic_cow"] = Cow,
        ["pic_coin"] = () => Coin("R1", 90, Silver, 2700),
        ["pic_ladder"] = Ladder,
        ["pic_cake"] = Cake,
        ["pic_pie"] = Pie,
        ["pic_bird"] = Bird,
        ["pic_saw"] = Saw,
        ["pic_letter"] = Letter,
        ["pic_hammer"] = Hammer,
        ["pic_dinner"] = Dinner,
    };

    private static Node2D? MakeMore(string id) => More.TryGetValue(id, out var draw) ? draw() : (Node2D?)MakeRest(id);

    private static IEnumerable<string> MoreIds => More.Keys.Concat(RestIds);

    /// <summary>Skin for hands and faces: a warm brown.</summary>
    private static readonly Color Skin = new("c68a5e");

    /// <summary>A thick outlined stroke (a tentacle, a leg, a rail): ink underneath, colour on top.</summary>
    private static void Tube(Pic p, double width, Color colour, params Vector2[] points)
    {
        p.Line(Polyline(points), Palette.Ink, width + 9, 0.8).Line(Polyline(points), colour, width, 0.8);
    }

    // A folded treasure map: land, sea, a dotted path and an X.
    private static Node2D Map()
    {
        var p = new Pic(2400);
        var paper = new Color("f4e3b5");
        p.Fill(Polygon(P(-104, -70), P(-36, -84), P(-36, 74), P(-104, 88)), paper, 5);
        p.Fill(Polygon(P(-36, -84), P(34, -70), P(34, 88), P(-36, 74)), paper.Darkened(0.08f), 5);
        p.Fill(Polygon(P(34, -70), P(104, -84), P(104, 74), P(34, 88)), paper, 5);
        p.Fill(Polygon(P(-84, 40), P(-50, 60), P(-10, 44), P(30, 56), P(60, 30), P(50, -10), P(10, -30), P(-40, -20), P(-80, 0)), Palette.Grass, 4);
        p.Fill(Ellipse(P(70, -48), 22, 14), Palette.Sea, 3);
        foreach (var (i, (x, y)) in Indexed((-64.0, 20.0), (-46.0, 8.0), (-28.0, 14.0), (-10.0, 4.0), (6.0, -6.0)))
            p.Dot(x, y, 4, Palette.Red);
        p.Line(Polyline(P(18, 4), P(42, 28)), Palette.Red, 7).Line(Polyline(P(18, 28), P(42, 4)), Palette.Red, 7);
        return p.Node;
    }

    // A white goat side on: horns, a beard and a little tail.
    private static Node2D Goat()
    {
        var p = new Pic(2420);
        var coat = new Color("f2efe8");
        foreach (var x in new[] { -56.0, -30, 30, 54 })
            Tube(p, 12, coat, P(x, -20), P(x, -92));
        p.Fill(Polygon(P(70, 20), P(96, 34), P(86, 10)), coat, 4);
        p.Fill(Ellipse(P(0, 4), 80, 44), coat, 5);
        p.Fill(Polygon(P(-60, 20), P(-70, 62), P(-96, 54), P(-112, 20), P(-96, 8)), coat, 5);
        p.Fill(Polygon(P(-80, 58), P(-64, 98), P(-56, 92), P(-68, 54)), Palette.Stone.Darkened(0.2f), 4);
        p.Fill(Polygon(P(-66, 46), P(-40, 52), P(-62, 36)), coat, 4);
        p.Fill(Polygon(P(-106, 14), P(-98, -16), P(-92, 12)), Palette.Stone.Darkened(0.1f), 3);
        p.Dot(-86, 38, 5);
        foreach (var x in new[] { -56.0, -30, 30, 54 }) p.Fill(Box(x - 8, -98, 16, 10), Palette.Ink, 3);
        return p.Node;
    }

    // A purple octopus waving eight arms.
    private static Node2D Octopus()
    {
        var p = new Pic(2440);
        for (var i = 0; i < 8; i++)
        {
            // Each arm spreads out from under the head and curls up at its tip.
            var x = -56 + i * 16.0;
            var curl = x < 0 ? -1 : 1;
            Tube(p, 13, Palette.Purple, P(x, -10), P(x * 1.25, -46), P(x * 1.5, -72), P(x * 1.5 + curl * 14, -86), P(x * 1.5 + curl * 22, -72));
        }
        p.Fill(Polygon(Around(0, 0, 70, 96, 0, Math.PI, 16)), Palette.Purple, 5);
        p.Fill(Ellipse(P(-24, 24), 14, 18), Palette.White, 3).Fill(Ellipse(P(24, 24), 14, 18), Palette.White, 3);
        p.Dot(-22, 22, 7).Dot(26, 22, 7);
        p.Line(Arc(P(0, 2), 16, 12, Math.PI + 0.4, 2 * Math.PI - 0.4), line: 4);
        p.Dot(-44, 4, 7, Palette.Pink).Dot(44, 4, 7, Palette.Pink);
        return p.Node;
    }

    // A grey rabbit's face: long ears, buck teeth, whiskers.
    private static Node2D Rabbit()
    {
        var p = new Pic(2460);
        var fur = new Color("d9d4cc");
        foreach (var side in new[] { -1.0, 1.0 })
        {
            p.Fill(Ellipse(P(side * 30, 60), 22, 52), fur, 5);
            p.Fill(Ellipse(P(side * 30, 62), 10, 38), Palette.Pink, 3);
        }
        p.Fill(Ellipse(P(0, -30), 66, 58), fur, 5);
        p.Fill(Box(-11, -70, 10, 16), Palette.White, 3).Fill(Box(1, -70, 10, 16), Palette.White, 3);
        p.Fill(Polygon(P(-9, -38), P(9, -38), P(0, -48)), Palette.DeepPink, 3);
        p.Line(Polyline(P(0, -48), P(0, -54)), line: 4);
        foreach (var side in new[] { -1.0, 1.0 })
            foreach (var dy in new[] { 0.0, -12 })
                p.Line(Polyline(P(side * 22, -48 + dy / 2), P(side * 88, -40 + dy)), Palette.Ink, 3, 1);
        p.Dot(-24, -16, 8).Dot(24, -16, 8).Dot(-21, -13, 2.5, Palette.White).Dot(27, -13, 2.5, Palette.White);
        return p.Node;
    }

    // A child's leg in shorts, a sock and a takkie.
    private static Node2D Leg()
    {
        var p = new Pic(2480);
        p.Fill(Polygon(P(-40, 104), P(40, 104), P(44, 40), P(-36, 40)), Palette.Teal, 5);
        p.Fill(Polygon(P(-30, 42), P(20, 42), P(14, -50), P(-24, -50)), Skin, 5);
        p.Fill(Polygon(P(-26, -40), P(16, -40), P(16, -72), P(-26, -72)), Palette.White, 4);
        p.Fill(Polygon(P(-30, -70), P(18, -70), P(70, -82), P(84, -98), P(-32, -98)), Palette.Red, 5);
        p.Line(Polyline(P(-32, -94), P(84, -94)), Palette.White, 6);
        p.Line(Polyline(P(10, -76), P(30, -78)), Palette.White, 3).Line(Polyline(P(16, -84), P(38, -86)), Palette.White, 3);
        return p.Node;
    }

    // Red lips blowing a kiss, with little hearts.
    private static Node2D Kiss()
    {
        var p = new Pic(2500);
        p.Fill(Polygon(P(-80, 0), P(-40, 34), P(-14, 30), P(0, 18), P(14, 30), P(40, 34), P(80, 0), P(30, 4), P(0, 0), P(-30, 4)), Palette.Red, 5);
        p.Fill(Polygon(P(-80, 0), P(-30, 4), P(0, 0), P(30, 4), P(80, 0), P(44, -30), P(0, -40), P(-44, -30)), Palette.Red.Darkened(0.08f), 5);
        p.Shine(-30, -18, 20, 8, 0.1);
        foreach (var (i, (x, y, r)) in Indexed((60.0, 62.0, 16.0), (88.0, 92.0, 10.0)))
            p.Fill(Polygon(P(x, y - r), P(x - r, y + r * 0.2), P(x - r * 0.5, y + r * 0.8), P(x, y + r * 0.4), P(x + r * 0.5, y + r * 0.8), P(x + r, y + r * 0.2)), Palette.Pink, 3);
        return p.Node;
    }

    // A jar of red jam with a checked cloth on the lid and a strawberry label.
    private static Node2D Jam()
    {
        var p = new Pic(2520);
        p.Fill(RoundRect(R(-62, -92, 124, 138), 22), Palette.Red, 5);
        p.Fill(Polygon(P(-74, 46), P(74, 46), P(84, 70), P(60, 96), P(-60, 96), P(-84, 70)), Palette.White, 5);
        foreach (var x in new[] { -50.0, -10, 30 }) p.Fill(Box(x, 56, 18, 16), Palette.Red.Lightened(0.2f), 2);
        p.Line(Polyline(P(-70, 52), P(70, 52)), Palette.Brown, 5);
        p.Fill(RoundRect(R(-44, -58, 88, 70), 10), Palette.Card, 4);
        p.Fill(Polygon(P(-16, 0), P(16, 0), P(0, -36)), Palette.Red, 3);
        p.Fill(Polygon(P(-12, 2), P(0, 12), P(12, 2)), Palette.Leaf, 3);
        p.Shine(-46, -10, 10, 50, 0);
        return p.Node;
    }

    // A spider's web: spokes and rings, with a small spider.
    private static Node2D Web()
    {
        var p = new Pic(2540);
        const int spokes = 8;
        for (var i = 0; i < spokes; i++)
        {
            var a = i * 2 * Math.PI / spokes;
            p.Line(Polyline(P(0, 0), P(104 * Math.Cos(a), 104 * Math.Sin(a))), Grey, 3, 1);
        }
        foreach (var r in new[] { 28.0, 52, 76, 98 })
            p.Line(Polyline(Enumerable.Range(0, spokes + 1).Select(i => i * 2 * Math.PI / spokes)
                                      .Select(a => P(r * Math.Cos(a), r * Math.Sin(a)))), Grey, 3, 1);
        p.Line(Polyline(P(40, 104), P(40, 50)), line: 2);
        p.Fill(Circle(40, 40, 12), Palette.Ink, 2);
        foreach (var side in new[] { -1.0, 1.0 })
            foreach (var dy in new[] { 8.0, 0, -8 })
                p.Line(Polyline(P(40 + side * 8, 40 + dy), P(40 + side * 22, 46 + dy * 1.6), P(40 + side * 28, 36 + dy * 1.6)), line: 3, wobble: 0.5);
        return p.Node;
    }

    // A red yo-yo on its string, hanging from a finger loop.
    private static Node2D YoYo()
    {
        var p = new Pic(2560);
        p.Line(Circle(0, 92, 10), line: 4);
        p.Line(Polyline(P(0, 82), P(0, 0)), line: 3);
        p.Fill(Circle(0, -30, 66), Palette.Red, 5);
        p.Fill(Circle(0, -30, 22), Palette.Sun, 4);
        p.Line(Circle(0, -30, 48), Palette.Red.Darkened(0.2f), 3);
        p.Shine(-30, 0, 24, 12, 0.6);
        return p.Node;
    }

    // A bee with stripes and wings; "buzz" adds the sound lines.
    private static Node2D Bee(bool buzzing)
    {
        var p = new Pic(buzzing ? 2600UL : 2580UL);
        p.Fill(Ellipse(P(-10, 52), 30, 42), Palette.Ice.WithAlpha(0.9), 4);
        p.Fill(Ellipse(P(26, 50), 26, 36), Palette.Ice.WithAlpha(0.9), 4);
        p.Fill(Polygon(P(70, 4), P(98, 0), P(70, -6)), Palette.Ink, 3);
        p.Fill(Ellipse(P(10, 0), 66, 44), Palette.Sun, 5);
        foreach (var x in new[] { -6.0, 24, 50 })
            p.Fill(Polygon(Around(x, 0, 10, 42, Math.PI / 2, -Math.PI / 2, 6).Concat(Around(x - 6, 0, 10, 42, -Math.PI / 2, Math.PI / 2, 6))), Palette.Ink, 2);
        p.Fill(Circle(-64, 6, 30), Palette.Ink, 4);
        p.Dot(-74, 12, 6, Palette.White).Dot(-72, 14, 3);
        p.Line(Polyline(P(-70, 32), P(-84, 58)), line: 3).Line(Polyline(P(-54, 34), P(-56, 62)), line: 3);
        if (buzzing)
            foreach (var (i, y) in Indexed(-40.0, -60.0, -80.0))
                p.Line(Arc(P(-30 - i * 20, y + 40), 80, 40, Math.PI * 1.15, Math.PI * 1.35), Palette.Orange, 5);
        return p.Node;
    }

    // A queen: a smiling face with a golden crown.
    private static Node2D Queen()
    {
        var p = new Pic(2620);
        p.Fill(Polygon(Around(0, -10, 74, 90, 0, Math.PI, 12).Concat(new[] { P(-80, -96), P(80, -96) })), Palette.Brown.Darkened(0.2f), 5);
        p.Fill(Ellipse(P(0, -10), 54, 62), Skin, 5);
        p.Fill(Polygon(P(-56, 50), P(-62, 104), P(-30, 76), P(0, 108), P(30, 76), P(62, 104), P(56, 50)), Palette.Sun, 5);
        foreach (var (i, x) in Indexed(-30.0, 0.0, 30.0)) p.Fill(Circle(x, 62, 7), i == 1 ? Palette.Red : Palette.Teal, 3);
        p.Dot(-20, 0, 6).Dot(20, 0, 6);
        p.Line(Arc(P(0, -26), 22, 14, Math.PI + 0.4, 2 * Math.PI - 0.4), line: 4);
        p.Dot(-34, -20, 8, Palette.Pink.WithAlpha(0.7)).Dot(34, -20, 8, Palette.Pink.WithAlpha(0.7));
        return p.Node;
    }

    // Chips in a red carton.
    private static Node2D Chips()
    {
        var p = new Pic(2640);
        var chip = new Color("f5c95a");
        foreach (var (i, (x, top, lean)) in Indexed((-46.0, 66.0, -8.0), (-24.0, 92.0, -4.0), (0.0, 80.0, 2.0), (22.0, 98.0, 6.0), (44.0, 70.0, 10.0), (-8.0, 104.0, 0.0)))
            p.Fill(Polygon(P(x - 9, -10), P(x + 9, -10), P(x + 9 + lean, top), P(x - 9 + lean, top)), chip, 4);
        p.Fill(Polygon(P(-70, 30), P(70, 30), P(52, -96), P(-52, -96)), Palette.Red, 5);
        p.Fill(Polygon(Around(0, 30, 70, 22, Math.PI, 2 * Math.PI, 10)), Palette.Red.Darkened(0.12f), 4);
        p.Fill(Circle(0, -40, 18), Palette.Sun, 3);
        return p.Node;
    }

    // A ship: a red hull with portholes, a cabin and a funnel, on the waves.
    private static Node2D Ship()
    {
        var p = new Pic(2660);
        p.Fill(Box(30, 30, 30, 50), Palette.Red, 5);
        p.Fill(Box(30, 70, 30, 10), Palette.Ink, 3);
        p.Fill(Box(-60, 0, 120, 40), Palette.White, 5);
        foreach (var x in new[] { -40.0, -10, 20 }) p.Fill(Box(x, 12, 18, 16), Palette.Ice, 3);
        p.Fill(Polygon(P(-110, 4), P(110, 4), P(84, -54), P(-90, -54)), Palette.Teal, 5);
        foreach (var x in new[] { -56.0, -16, 24, 62 }) p.Fill(Circle(x, -24, 9), Palette.Ice, 3);
        p.Line(Polyline(Enumerable.Range(0, 12).Select(i => P(-110 + i * 20, i % 2 == 0 ? -62.0 : -72))), Palette.Sea, 6);
        p.Line(Polyline(P(4, 60), P(14, 76), P(4, 92), P(14, 104)), Palette.Stone, 5);
        return p.Node;
    }

    /// <summary>A closed hand seen from the side (fingers curled), knuckles to the right.</summary>
    private static void Fist(Pic p, double x, double y)
    {
        p.Fill(RoundRect(R(x - 52, y - 46, 104, 92), 30), Skin, 5);
        foreach (var dy in new[] { 22.0, 0, -22 }) p.Line(Arc(P(x + 30, y + dy), 22, 11, -Math.PI / 2, Math.PI / 2), Skin.Darkened(0.3f), 3);
    }

    // A thumbs-up.
    private static Node2D Thumb()
    {
        var p = new Pic(2680);
        p.Fill(RoundRect(R(-34, 0, 40, 104), 20), Skin, 5);
        p.Fill(Ellipse(P(-14, 88), 10, 8), Palette.Card.Darkened(0.05f), 2);
        Fist(p, 0, -40);
        p.Fill(Box(-60, -104, 112, 26), Palette.Teal, 4);
        return p.Node;
    }

    // "This": a hand pointing at a red ball.
    private static Node2D This()
    {
        var p = new Pic(2700);
        p.Fill(RoundRect(R(-20, 6, 104, 34), 17), Skin, 5);
        Fist(p, -40, -4);
        p.Fill(Box(-110, -52, 26, 96), Palette.Teal, 4);
        p.Fill(Circle(70, -66, 30), Palette.Red, 5);
        p.Shine(60, -54, 14, 8, 0.6);
        return p.Node;
    }

    // A gold ring with a sparkling stone.
    private static Node2D Ring()
    {
        var p = new Pic(2720);
        p.Line(Ellipse(P(0, -30), 66, 56), Palette.Ink, 26, 1).Line(Ellipse(P(0, -30), 66, 56), Palette.Sun, 16, 1);
        p.Fill(Polygon(P(-30, 40), P(30, 40), P(42, 60), P(0, 104), P(-42, 60)), Palette.EggBlue, 5);
        p.Line(Polyline(P(-42, 60), P(42, 60)), line: 3).Line(Polyline(P(-14, 60), P(0, 104), P(14, 60)), line: 3);
        foreach (var (i, (x, y)) in Indexed((64.0, 84.0), (-66.0, 92.0)))
            p.Line(Polyline(P(x - 10, y), P(x + 10, y)), Palette.Sun, 4).Line(Polyline(P(x, y - 10), P(x, y + 10)), Palette.Sun, 4);
        return p.Node;
    }

    // An open book.
    private static Node2D Book()
    {
        var p = new Pic(2740);
        p.Fill(Polygon(P(-108, -60), P(0, -72), P(108, -60), P(108, 64), P(0, 52), P(-108, 64)), Palette.Red, 5);
        p.Fill(Polygon(P(-98, -48), P(0, -62), P(0, 60), P(-98, 72)), Palette.Card, 5);
        p.Fill(Polygon(P(0, -62), P(98, -48), P(98, 72), P(0, 60)), Palette.Card, 5);
        foreach (var side in new[] { -1.0, 1.0 })
            foreach (var y in new[] { 36.0, 14, -8, -30 })
                p.Line(Polyline(P(side * 16, y), P(side * 82, y + 8)), Palette.Stone.Darkened(0.2f), 4, 1);
        return p.Node;
    }

    private static Node2D Fork()
    {
        var p = new Pic(2760);
        foreach (var x in new[] { -24.0, -8, 8, 24 }) p.Fill(Box(x - 4, 30, 8, 74), Silver, 3);
        p.Fill(Polygon(P(-30, 34), P(30, 34), P(26, 6), P(8, -6), P(-8, -6), P(-26, 6)), Silver, 4);
        p.Fill(RoundRect(R(-12, -104, 24, 104), 12), Silver, 4);
        return p.Node;
    }

    // A big curved arrow, going round and coming back.
    private static Node2D TurnArrow()
    {
        var p = new Pic(2780);
        var arc = Around(0, 0, 62, 62, -Math.PI / 2, Math.PI * 0.95, 18).ToArray();
        p.Line(Polyline(arc.Prepend(P(-70, -62))), Palette.Ink, 32, 0.6).Line(Polyline(arc.Prepend(P(-70, -62))), Palette.Orange, 22, 0.6);
        p.Fill(Polygon(P(-62, -100), P(-110, -62), P(-62, -24)), Palette.Orange, 5);
        return p.Node;
    }

    // A black-and-white cow side on, with horns and a pink nose.
    private static Node2D Cow()
    {
        var p = new Pic(2800);
        foreach (var x in new[] { -60.0, -36, 40, 62 }) Tube(p, 13, Palette.White, P(x, -10), P(x, -88));
        p.Line(Polyline(P(82, 16), P(100, -20)), line: 4);
        p.Fill(RoundRect(R(-76, -30, 164, 84), 34), Palette.White, 5);
        p.Fill(Polygon(P(-20, 50), P(20, 54), P(30, 20), P(0, 10), P(-30, 24)), Palette.Ink, 2);
        p.Fill(Polygon(P(40, 0), P(70, 6), P(76, -24), P(46, -24)), Palette.Ink, 2);
        p.Fill(Ellipse(P(30, -34), 16, 10), Palette.Pink, 3);
        p.Fill(Polygon(P(-120, 30), P(-104, 70), P(-60, 64), P(-62, 20), P(-84, 4)), Palette.White, 5);
        p.Fill(Ellipse(P(-104, 18), 22, 18), Palette.Pink, 4);
        p.Dot(-110, 18, 3).Dot(-98, 18, 3).Dot(-86, 50, 5);
        p.Fill(Polygon(P(-70, 66), P(-60, 92), P(-52, 64)), Palette.Sand, 3).Fill(Polygon(P(-100, 70), P(-112, 94), P(-90, 70)), Palette.Sand, 3);
        p.Fill(Polygon(P(-64, 56), P(-36, 66), P(-56, 40)), Palette.White, 4);
        foreach (var x in new[] { -60.0, -36, 40, 62 }) p.Fill(Box(x - 7, -94, 14, 10), Palette.Ink, 3);
        return p.Node;
    }

    // A wooden ladder, leaning a little.
    private static Node2D Ladder()
    {
        var p = new Pic(2820);
        foreach (var t in new[] { 0.1, 0.3, 0.5, 0.7, 0.9 })
            Tube(p, 9, Palette.LightBrown, P(-46 + 20 * t, -104 + 208 * t), P(36 + 20 * t, -104 + 208 * t));
        Tube(p, 12, Palette.Brown, P(-50, -104), P(-30, 104));
        Tube(p, 12, Palette.Brown, P(40, -104), P(60, 104));
        return p.Node;
    }

    // A round cake with icing, a candle and a cherry.
    private static Node2D Cake()
    {
        var p = new Pic(2840);
        p.Fill(Ellipse(P(0, -76), 104, 18), Palette.Stone, 4);
        p.Fill(Polygon(Around(0, -60, 84, 22, Math.PI, 2 * Math.PI, 10).Concat(Around(0, 20, 84, 22, 0, Math.PI, 10))), Palette.Pink, 5);
        p.Fill(Polygon(Around(0, 20, 84, 22, 0, Math.PI, 12).Concat(new[] { P(-84, 10), P(-70, -6), P(-56, 8), P(-36, -10), P(-16, 6), P(4, -12), P(26, 4), P(46, -10), P(66, 6), P(84, -4), P(84, 10) })),
               Palette.White, 4);
        p.Fill(Ellipse(P(0, 20), 84, 22), Palette.White, 4);
        p.Fill(Box(-6, 26, 12, 40), Palette.Teal, 3);
        p.Fill(Polygon(Around(0, 74, 8, 8, Math.PI, 2 * Math.PI, 6).Append(P(0, 94))), Palette.Orange, 3);
        p.Fill(Circle(40, 36, 12), Palette.Red, 3);
        return p.Node;
    }

    // A pie in its dish, with a golden crust and steam.
    private static Node2D Pie()
    {
        var p = new Pic(2860);
        p.Fill(Polygon(P(-104, -6), P(104, -6), P(80, -70), P(-80, -70)), Palette.EggBlue, 5);
        p.Fill(Polygon(Around(0, -6, 96, 46, 0, Math.PI, 14)), new Color("e9b765"), 5);
        foreach (var x in new[] { -40.0, 0, 40 }) p.Line(Polyline(P(x - 10, 8), P(x + 10, 20)), Palette.Brown, 4);
        foreach (var (i, x) in Indexed(-30.0, 0.0, 30.0)) p.Line(Polyline(P(x, 52), P(x + 8, 68), P(x - 4, 84), P(x + 6, 100)), Palette.Stone, 5);
        return p.Node;
    }

    // A small blue bird on a branch.
    private static Node2D Bird()
    {
        var p = new Pic(2880);
        p.Line(Polyline(P(-110, -54), P(110, -44)), Palette.Brown, 9);
        p.Fill(Polygon(P(-74, 4), P(-110, 30), P(-104, 0)), Palette.Sea.Darkened(0.2f), 4);
        p.Fill(Ellipse(P(-6, 6), 70, 50), Palette.Sea, 5);
        p.Fill(Ellipse(P(10, -12), 40, 26), Palette.Card, 3);
        p.Fill(Polygon(P(-40, 24), P(10, 28), P(-20, -4)), Palette.Sea.Darkened(0.2f), 4);
        p.Fill(Circle(52, 46, 32), Palette.Sea, 5);
        p.Fill(Polygon(P(80, 50), P(106, 40), P(80, 36)), Palette.Orange, 3);
        p.Dot(60, 54, 6).Dot(62, 56, 2, Palette.White);
        p.Line(Polyline(P(-14, -40), P(-14, -50)), Palette.Orange, 4).Line(Polyline(P(14, -40), P(14, -50)), Palette.Orange, 4);
        return p.Node;
    }

    // A hand saw: a grey toothed blade and a wooden handle.
    private static Node2D Saw()
    {
        var p = new Pic(2900);
        var teeth = Enumerable.Range(0, 15).SelectMany(i => new[] { P(100 - i * 12, -20 - i * 1.6), P(94 - i * 12, -30 - i * 1.6) });
        p.Fill(Polygon(new[] { P(-74, 40), P(104, 20) }.Concat(teeth).Append(P(-76, -46))), Silver, 4);
        p.Fill(Polygon(P(-74, 52), P(-30, 50), P(-26, -56), P(-110, -60), P(-110, 40)), Palette.Brown, 5);
        p.Fill(Ellipse(P(-70, -4), 18, 26), Palette.Card, 4);
        return p.Node;
    }

    // An envelope with a stamp.
    private static Node2D Letter()
    {
        var p = new Pic(2920);
        p.Fill(Box(-104, -66, 208, 136), Palette.White, 5);
        p.Line(Polyline(P(-104, 70), P(0, -6), P(104, 70)), line: 4);
        p.Line(Polyline(P(-104, -66), P(-34, 12)), line: 3).Line(Polyline(P(104, -66), P(34, 12)), line: 3);
        p.Fill(Box(60, 30, 30, 32), Palette.Red, 3);
        p.Fill(Polygon(Star(P(75, 46), 10)), Palette.Sun, 2);
        return p.Node;
    }

    private static Node2D Hammer()
    {
        var p = new Pic(2940);
        p.Fill(Polygon(P(-14, 50), P(14, 50), P(18, -104), P(-18, -104)), Palette.LightBrown, 5);
        p.Fill(Polygon(P(-88, 46), P(60, 46), P(60, 92), P(-70, 92), P(-96, 80), P(-96, 58)), Grey, 5);
        p.Fill(Polygon(P(60, 52), P(104, 40), P(110, 70), P(60, 86)), Grey, 5);
        return p.Node;
    }

    // Dinner on a plate: chicken, peas and potato, with a knife and fork.
    private static Node2D Dinner()
    {
        var p = new Pic(2960);
        p.Fill(Polygon(P(-110, 60), P(-100, 66), P(-94, -60), P(-104, -62)), Silver, 3);
        p.Fill(Polygon(P(100, 66), P(110, 62), P(106, -62), P(96, -60)), Silver, 3);
        p.Fill(Ellipse(P(0, 0), 84, 74), Palette.White, 5);
        p.Line(Ellipse(P(0, 0), 64, 56), Palette.Stone, 3);
        p.Fill(Polygon(P(-40, 30), P(-6, 40), P(14, 16), P(-6, -6), P(-30, 0)), Palette.Brown.Lightened(0.15f), 4);
        p.Fill(Box(10, 6, 30, 10), Palette.Card, 3);
        p.Fill(Ellipse(P(-14, -30), 24, 18), Palette.Sun.Lightened(0.3f), 4);
        foreach (var (x, y) in new[] { (26.0, -20.0), (38.0, -30.0), (24.0, -36.0), (40.0, -14.0), (32.0, -44.0) })
            p.Fill(Circle(x, y, 7), Palette.Leaf, 2);
        return p.Node;
    }
}
