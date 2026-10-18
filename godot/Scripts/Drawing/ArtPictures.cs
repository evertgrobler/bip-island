using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// Hand-drawn pictures that replace the emoji stand-ins (owner, 18 October 2026: "start on the art").
/// Same conventions as <see cref="PictureNode"/> and <see cref="WordPictures"/>: y-up numbers, roughly a
/// 220 × 220 box around the origin, wobbly ink outlines and the game palette. Each picture gets its own
/// block of seeds (from <see cref="Pic"/>), so its wobble never changes when another picture is added.
/// When a picture lands here, its row in <see cref="EmojiPictures"/> goes.
/// </summary>
public static partial class ArtPictures
{
    private static readonly Dictionary<string, Func<Node2D>> Pictures = new()
    {
        // Rand money: the real colours and sizes, so a child learns the coins they'll hold.
        ["pic_money_c10"] = () => Coin("10c", 66, Bronze, 2000),
        ["pic_money_c20"] = () => Coin("20c", 74, Bronze, 2010),
        ["pic_money_c50"] = () => Coin("50c", 82, Bronze, 2020),
        ["pic_money_r1"] = () => Coin("R1", 90, Silver, 2030),
        ["pic_money_r2"] = () => Coin("R2", 80, Silver, 2040),
        ["pic_money_r5"] = () => Coin("R5", 92, Silver, 2050, centre: Bronze),
        ["pic_money_r10"] = () => Note("R10", new Color("8cc27a"), 2060),
        ["pic_money_r20"] = () => Note("R20", new Color("d39a5f"), 2070),
        ["pic_money_r50"] = () => Note("R50", new Color("e2735f"), 2080),
        ["pic_dog"] = Dog,
        ["pic_cat"] = Cat,
        ["pic_duck"] = Duck,
        ["pic_fish"] = Fish,
        ["pic_egg"] = Egg,
        ["pic_hat"] = Hat,
        ["pic_bus"] = Bus,
        ["pic_van"] = Van,
        ["pic_car"] = Car,
        ["pic_box"] = Box,
        ["pic_bell"] = Bell,
        ["pic_kite"] = Kite,
        ["pic_umbrella"] = Umbrella,
        ["pic_moon"] = Moon,
        ["pic_cloud"] = () => Cloud(0, 0, 1, Palette.White, 2240).Node,
        ["pic_rain"] = Rain,
        ["pic_snow"] = Snow,
        ["pic_leaf"] = Leaf,
        ["pic_bone"] = Bone,
    };

    public static Node2D? Make(string id)
    {
        if (Pictures.TryGetValue(id, out var draw)) return draw();
        return MakeMore(id) ?? MakeSentence(id);
    }

    public static IEnumerable<string> Ids => Pictures.Keys.Concat(MoreIds).Concat(SentenceIds);

    private static readonly Color Bronze = new("d8a25e");
    private static readonly Color Silver = new("cfd3d8");
    private static readonly Color Grey = new("9a9aa3");

    /// <summary>A picture under construction: each shape takes the next seed, so pictures stay stable.</summary>
    private sealed class Pic(ulong seed)
    {
        public Node2D Node { get; } = new();
        private ulong _seed = seed;

        public Pic Fill(SketchShape shape, Color fill, double line = 5, double wobble = 2.2)
        {
            Node.AddChild(Pen(Clean(shape), _seed++, fill: fill, lineWidth: line, wobble: wobble));
            return this;
        }

        public Pic Line(SketchShape shape, Color? ink = null, double line = 5, double wobble = 2.2)
        {
            Node.AddChild(Pen(shape, _seed++, ink: ink, lineWidth: line, wobble: wobble));
            return this;
        }

        /// <summary>
        /// Overlapping shapes as one outlined blob (a cloud, a bone): all the outlines first, then the
        /// same shapes filled with no outline on top, hiding the lines inside.
        /// </summary>
        public Pic Blob(Color fill, double line, params SketchShape[] shapes)
        {
            foreach (var shape in shapes) Node.AddChild(Pen(shape, _seed++, fill: fill, lineWidth: line));
            foreach (var shape in shapes) Node.AddChild(Pen(shape, _seed++, fill: fill, lineWidth: 0));
            return this;
        }

        public Pic Dot(double x, double y, double r, Color? colour = null)
        {
            Node.AddChild(Up.Dot(x, y, r, colour));
            return this;
        }

        public Pic Shine(double x, double y, double w, double h, double turn = 0.5)
        {
            var shine = Oval(w, h, Palette.White.WithAlpha(0.7));
            shine.Position = P(x, y);
            shine.Rotation = Turn(turn);
            Node.AddChild(shine);
            return this;
        }

        public Pic Text(string text, int size, double x, double y, Color? colour = null)
        {
            var label = Sketch.Letter(text, size, colour ?? Palette.Ink, withShadow: false);
            label.Position = P(x, y);
            Node.AddChild(label);
            return this;
        }

        public Pic Add(Node2D node, double x = 0, double y = 0, double scale = 1)
        {
            node.Position = P(x, y);
            node.Scale = Vector2.One * (float)scale;
            Node.AddChild(node);
            return this;
        }
    }

    /// <summary>
    /// A polygon without repeated points (where two curves meet, or the last point is the first again):
    /// a repeated point stops the fill, leaving the shape white.
    /// </summary>
    private static SketchShape Clean(SketchShape shape)
    {
        if (shape is not SketchShape.Polygon polygon) return shape;
        var kept = new List<Vector2>();
        foreach (var point in polygon.Points)
            if (kept.Count == 0 || kept[^1].DistanceTo(point) > 0.5f) kept.Add(point);
        while (kept.Count > 3 && kept[^1].DistanceTo(kept[0]) <= 0.5f) kept.RemoveAt(kept.Count - 1);
        return new SketchShape.Polygon([.. kept]);
    }

    /// <summary>Points around an ellipse (y-up), from one angle to another, for shapes with curved sides.</summary>
    private static IEnumerable<Vector2> Around(double cx, double cy, double rx, double ry, double from, double to, int steps = 16)
    {
        for (var i = 0; i <= steps; i++)
        {
            var a = from + (to - from) * i / steps;
            yield return P(cx + rx * Math.Cos(a), cy + ry * Math.Sin(a));
        }
    }

    private static SketchShape Box(double x, double y, double w, double h) =>
        Polygon(P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h));

    private static SketchShape Circle(double x, double y, double r) => Ellipse(P(x, y), r, r);

    /// <summary>A wheel: tyre and hub.</summary>
    private static void Wheel(Pic p, double x, double y, double r)
    {
        p.Fill(Circle(x, y, r), Palette.Ink, 4).Fill(Circle(x, y, r * 0.45), Silver, 3);
    }

    // MARK: - Money

    private static Node2D Coin(string value, double r, Color metal, ulong seed, Color? centre = null)
    {
        var p = new Pic(seed);
        p.Fill(Circle(0, 0, r), metal, 5);
        if (centre is { } middle) p.Fill(Circle(0, 0, r * 0.66), middle, 4);
        else p.Line(Circle(0, 0, r * 0.82), metal.Darkened(0.25f), 3);
        p.Text(value, (int)(r * (value.Length > 2 ? 0.62 : 0.72)), 0, 0, Palette.Ink.WithAlpha(0.8));
        p.Shine(-r * 0.5, r * 0.5, r * 0.3, r * 0.16);
        return p.Node;
    }

    private static Node2D Note(string value, Color colour, ulong seed)
    {
        var p = new Pic(seed);
        p.Fill(RoundRect(R(-108, -56, 216, 112), 10), colour, 5);
        p.Line(RoundRect(R(-96, -44, 192, 88), 6), colour.Darkened(0.3f), 3);
        // The watermark circle and the big number, as on the real notes.
        p.Fill(Circle(-50, 0, 30), colour.Lightened(0.35f), 3);
        p.Text(value, 50, 42, 2, Palette.Ink.WithAlpha(0.85));
        return p.Node;
    }

    // MARK: - Animals

    // A puppy's face: floppy brown ears, a pale muzzle, a black nose and a pink tongue.
    private static Node2D Dog()
    {
        var p = new Pic(2100);
        foreach (var side in new[] { -1.0, 1.0 })
            p.Fill(Polygon(P(side * 40, 60), P(side * 92, 40), P(side * 96, -40), P(side * 70, -50), P(side * 56, 20)), Palette.Brown, 5);
        p.Fill(Ellipse(P(0, 4), 70, 66), Palette.LightBrown, 5);
        p.Fill(Ellipse(P(0, -30), 38, 30), Palette.Card, 4);
        p.Fill(Ellipse(P(0, -60), 11, 13), Palette.Pink, 3);
        p.Fill(Ellipse(P(0, -16), 14, 10), Palette.Ink, 3);
        p.Line(Polyline(P(0, -26), P(0, -38), P(-14, -46)), line: 4).Line(Polyline(P(0, -38), P(14, -46)), line: 4);
        p.Dot(-26, 20, 8).Dot(26, 20, 8).Dot(-23, 23, 2.5, Palette.White).Dot(29, 23, 2.5, Palette.White);
        return p.Node;
    }

    // A ginger cat's face: pointed ears, whiskers, a pink nose.
    private static Node2D Cat()
    {
        var p = new Pic(2120);
        foreach (var side in new[] { -1.0, 1.0 })
        {
            p.Fill(Polygon(P(side * 24, 46), P(side * 74, 92), P(side * 70, 20)), Palette.Orange, 5);
            p.Fill(Polygon(P(side * 36, 46), P(side * 66, 74), P(side * 62, 34)), Palette.Pink, 3);
        }
        p.Fill(Ellipse(P(0, 0), 78, 64), Palette.Orange, 5);
        foreach (var x in new[] { -30.0, 0, 30 }) p.Line(Polyline(P(x - 6, 62), P(x, 46), P(x + 6, 62)), Palette.Brown, 4);
        p.Fill(Polygon(P(-10, -6), P(10, -6), P(0, -18)), Palette.DeepPink, 3);
        p.Line(Polyline(P(0, -18), P(0, -28), P(-12, -36)), line: 4).Line(Polyline(P(0, -28), P(12, -36)), line: 4);
        foreach (var side in new[] { -1.0, 1.0 })
            foreach (var (i, dy) in Indexed(-4.0, -16.0))
                p.Line(Polyline(P(side * 30, -14 + dy / 2), P(side * 100, -4 + dy * 1.5)), Palette.Ink, 3, 1);
        p.Fill(Ellipse(P(-28, 14), 10, 13), Palette.Leaf, 3).Fill(Ellipse(P(28, 14), 10, 13), Palette.Leaf, 3);
        p.Fill(Ellipse(P(-28, 14), 3.5, 10), Palette.Ink, 1).Fill(Ellipse(P(28, 14), 3.5, 10), Palette.Ink, 1);
        return p.Node;
    }

    // A yellow duck on the water.
    private static Node2D Duck()
    {
        var p = new Pic(2140);
        p.Fill(Ellipse(P(0, -64), 110, 24), Palette.Sea, 4);
        p.Fill(Polygon(P(-70, -20), P(60, -20), P(96, 10), P(80, 30), P(20, 10), P(-60, 20)), Palette.Sun, 5);
        p.Fill(Ellipse(P(10, -16), 80, 44), Palette.Sun, 5);
        p.Fill(Polygon(P(-10, -4), P(50, -10), P(30, 20)), Palette.Sun.Darkened(0.1f), 4);
        p.Fill(Circle(-46, 46, 38), Palette.Sun, 5);
        p.Fill(Polygon(P(-80, 50), P(-112, 38), P(-80, 30)), Palette.Orange, 4);
        p.Dot(-54, 56, 6).Dot(-52, 58, 2, Palette.White);
        p.Line(Polyline(P(-90, -60), P(-70, -56), P(-50, -60)), Palette.White, 4).Line(Polyline(P(70, -64), P(90, -60)), Palette.White, 4);
        return p.Node;
    }

    // An orange fish with a tail, a fin and bubbles.
    private static Node2D Fish()
    {
        var p = new Pic(2160);
        p.Fill(Polygon(P(54, 0), P(104, 40), P(96, 0), P(104, -40)), Palette.Orange.Darkened(0.1f), 5);
        p.Fill(Ellipse(P(-14, 0), 76, 48), Palette.Orange, 5);
        p.Fill(Polygon(P(-20, 40), P(10, 66), P(26, 40)), Palette.Orange.Darkened(0.1f), 4);
        p.Line(Arc(P(-50, 0), 30, 40, -1.1, 1.1), line: 4);
        foreach (var (i, x) in Indexed(-10.0, 12.0, 34.0)) p.Line(Arc(P(x, 0), 14, 18, -1.0, 1.0), Palette.Sun, 4);
        p.Dot(-62, 12, 7).Dot(-60, 14, 2, Palette.White);
        p.Line(Circle(-104, 40, 9), Palette.Sea, 3).Line(Circle(-92, 70, 6), Palette.Sea, 3);
        return p.Node;
    }

    // MARK: - Things

    private static Node2D Egg()
    {
        var p = new Pic(2180);
        // Narrower at the top than the bottom.
        p.Fill(Polygon(Enumerable.Range(0, 32).Select(i => i * 2 * Math.PI / 32)
                                 .Select(a => P(62 * Math.Cos(a), -14 + (Math.Sin(a) > 0 ? 98 : 72) * Math.Sin(a)))),
               new Color("f3e2c4"), 5);
        p.Shine(-24, 40, 18, 34, 0.3);
        return p.Node;
    }

    // A sun hat: a wide brim and a band.
    private static Node2D Hat() => Hat(Palette.Teal);

    private static Node2D Hat(Color colour)
    {
        var p = new Pic(2190);
        p.Fill(Ellipse(P(0, -36), 108, 32), colour, 5);
        p.Fill(Polygon(Around(0, 20, 54, 56, Math.PI, 0, 14).Prepend(P(-62, -30)).Append(P(62, -30))
                         .Concat(Around(0, -30, 62, 16, 0, -Math.PI, 10))), colour, 5);
        p.Fill(Polygon(P(-60, -22), P(60, -22), P(58, 4), P(-58, 4)), colour == Palette.Red ? Palette.Sun : Palette.Red, 4);
        p.Fill(Ellipse(P(30, -8), 14, 12), Palette.Sun, 3);
        return p.Node;
    }

    private static Node2D Bus()
    {
        var p = new Pic(2200);
        p.Fill(RoundRect(R(-108, -46, 216, 116), 18), Palette.Sun, 5);
        foreach (var x in new[] { -96.0, -52, -8 }) p.Fill(RoundRect(R(x, 14, 36, 36), 6), Palette.Ice, 4);
        p.Fill(RoundRect(R(36, -30, 30, 80), 6), Palette.Ice, 4);
        p.Fill(RoundRect(R(76, 14, 22, 36), 6), Palette.Ice, 4);
        p.Line(Polyline(P(-108, -4), P(36, -4)), Palette.Red, 7);
        Wheel(p, -64, -48, 22);
        Wheel(p, 70, -48, 22);
        return p.Node;
    }

    // A minibus van: a sloping nose and a sliding door.
    private static Node2D Van()
    {
        var p = new Pic(2210);
        p.Fill(Polygon(P(-108, -40), P(106, -40), P(106, 8), P(70, 64), P(-108, 64)), Palette.White, 5);
        p.Fill(Polygon(P(60, 10), P(96, 10), P(66, 52), P(60, 52)), Palette.Ice, 4);
        foreach (var x in new[] { -96.0, -46 }) p.Fill(RoundRect(R(x, 14, 40, 38), 6), Palette.Ice, 4);
        p.Line(Polyline(P(-2, -32), P(-2, 54)), line: 3).Line(Polyline(P(52, -32), P(52, 54)), line: 3);
        p.Line(Polyline(P(-108, -10), P(106, -10)), Palette.Teal, 8);
        Wheel(p, -64, -44, 22);
        Wheel(p, 66, -44, 22);
        return p.Node;
    }

    private static Node2D Car()
    {
        var p = new Pic(2220);
        p.Fill(Polygon(P(-62, 10), P(-36, 56), P(40, 56), P(70, 10)), Palette.Red, 5);
        p.Fill(Polygon(P(-48, 12), P(-30, 44), P(-4, 44), P(-4, 12)), Palette.Ice, 4);
        p.Fill(Polygon(P(8, 12), P(8, 44), P(34, 44), P(56, 12)), Palette.Ice, 4);
        p.Fill(RoundRect(R(-108, -36, 216, 54), 20), Palette.Red, 5);
        p.Fill(Ellipse(P(96, -6), 8, 8), Palette.Sun, 3);
        Wheel(p, -58, -38, 24);
        Wheel(p, 58, -38, 24);
        return p.Node;
    }

    // A cardboard box, a little open-flapped, with tape.
    private static Node2D Box()
    {
        var p = new Pic(2230);
        var front = Palette.LightBrown;
        p.Fill(Polygon(P(-86, -76), P(40, -76), P(40, 40), P(-86, 40)), front, 5);
        p.Fill(Polygon(P(40, -76), P(92, -48), P(92, 66), P(40, 40)), front.Darkened(0.15f), 5);
        p.Fill(Polygon(P(-86, 40), P(40, 40), P(92, 66), P(-34, 66)), front.Lightened(0.2f), 5);
        p.Line(Polyline(P(-24, 40), P(28, 66)), Palette.Sand, 10, 1);
        p.Line(Polyline(P(-24, 40), P(-24, 0)), Palette.Sand, 10, 1);
        return p.Node;
    }

    private static Node2D Bell()
    {
        var p = new Pic(2250);
        p.Line(Circle(0, 84, 14), line: 6);
        p.Fill(Circle(0, -70, 16), Palette.Orange, 4);
        p.Fill(Polygon(Around(0, 30, 44, 44, 0, Math.PI, 12)
                         .Concat(new[] { P(-48, -10), P(-76, -46), P(-92, -60), P(92, -60), P(76, -46), P(48, -10) })), Palette.Sun, 5);
        p.Shine(-22, 30, 12, 40, 0.2);
        return p.Node;
    }

    // A diamond kite in two colours with a bowed tail.
    private static Node2D Kite()
    {
        var p = new Pic(2260);
        p.Line(Polyline(P(10, -50), P(-6, -70), P(18, -92), P(-4, -108)), line: 3);
        foreach (var (i, (x, y)) in Indexed((-6.0, -72.0), (16.0, -94.0)))
            p.Fill(Polygon(P(x - 14, y + 8), P(x + 14, y - 8), P(x + 14, y + 8), P(x - 14, y - 8)), i == 0 ? Palette.Red : Palette.Teal, 3);
        p.Fill(Polygon(P(10, 104), P(-60, 30), P(0, 30)), Palette.Red, 5);
        p.Fill(Polygon(P(10, 104), P(0, 30), P(76, 30)), Palette.Sun, 5);
        p.Fill(Polygon(P(-60, 30), P(10, -50), P(0, 30)), Palette.Sun, 5);
        p.Fill(Polygon(P(0, 30), P(10, -50), P(76, 30)), Palette.Red, 5);
        return p.Node;
    }

    // A rainbow-striped umbrella with a hooked handle.
    private static Node2D Umbrella()
    {
        var p = new Pic(2270);
        p.Line(Polyline(P(0, 50), P(0, -76)), line: 7).Line(Arc(P(-18, -76), 18, 18, 0, -Math.PI), line: 7);
        Color[] stripes = [Palette.Red, Palette.Sun, Palette.Teal, Palette.Purple];
        for (var i = 0; i < 4; i++)
        {
            // One wedge per colour: the top curve, down a rib, and back along the scalloped hem.
            var (left, right) = (-104 + i * 52.0, -52 + i * 52.0);
            var top = Around(0, 0, 104, 92, Math.Acos(left / 104), Math.Acos(right / 104), 6);
            var hem = Around(left + 26, 0, 26, 16, 0, Math.PI, 6);
            p.Fill(Polygon(top.Append(P(right, 0)).Concat(hem.Skip(1))), stripes[i], 5);
        }
        p.Line(Polyline(P(0, 92), P(0, 104)), line: 6);
        return p.Node;
    }

    // A crescent moon with a sleepy face.
    private static Node2D Moon()
    {
        var p = new Pic(2280);
        var (cx, cy, r) = (44.0, 26.0, 74.0);
        var phi = Math.Atan2(cy, cx);
        // Both curves start inside the bite and sweep round, so each kept run is one piece, horn to horn.
        var outer = Enumerable.Range(0, 60).Select(i => phi + i * (2 * Math.PI) / 60)
            .Select(a => (x: 90 * Math.Cos(a), y: 90 * Math.Sin(a)))
            .Where(q => Math.Pow(q.x - cx, 2) + Math.Pow(q.y - cy, 2) > r * r);
        var inner = Enumerable.Range(0, 60).Select(i => phi - i * (2 * Math.PI) / 60)
            .Select(a => (x: cx + r * Math.Cos(a), y: cy + r * Math.Sin(a)))
            .Where(q => q.x * q.x + q.y * q.y < 90 * 90);
        p.Fill(Polygon(outer.Select(q => P(q.x, q.y)).Concat(inner.Select(q => P(q.x, q.y)))), Palette.Sun, 5);
        p.Line(Arc(P(-46, 6), 12, 8, Math.PI + 0.3, 2 * Math.PI - 0.3), line: 4);
        p.Line(Arc(P(-40, -30), 16, 10, Math.PI + 0.4, 2 * Math.PI - 0.4), line: 4);
        return p.Node;
    }

    /// <summary>A fluffy cloud of overlapping puffs; shared by rain and the cloud picture.</summary>
    private static Pic Cloud(double x, double y, double scale, Color colour, ulong seed, Pic? into = null)
    {
        var p = into ?? new Pic(seed);
        double S(double v) => v * scale;
        p.Blob(colour, 5,
               Ellipse(P(x + S(-60), y + S(-10)), S(46), S(38)), Ellipse(P(x + S(-10), y + S(26)), S(56), S(52)),
               Ellipse(P(x + S(52), y + S(6)), S(50), S(44)), Ellipse(P(x, y + S(-22)), S(90), S(32)));
        return p;
    }

    private static Node2D Rain()
    {
        var p = new Pic(2300);
        Cloud(0, 36, 0.9, Palette.Stone, 0, p);
        foreach (var (i, (x, y)) in Indexed((-60.0, -40.0), (-20.0, -70.0), (24.0, -44.0), (64.0, -76.0), (-44.0, -96.0), (44.0, -100.0)))
            p.Fill(Polygon(Around(x, y, 9, 9, Math.PI, 2 * Math.PI, 6).Append(P(x, y + 22))), Palette.Sea, 3);
        return p.Node;
    }

    // Snow on the mountain tops (the Drakensberg in winter), with flakes falling.
    private static Node2D Snow()
    {
        var p = new Pic(2320);
        p.Fill(Polygon(P(-110, -80), P(-30, 50), P(20, -10), P(60, 70), P(110, -80)), Grey, 5);
        p.Fill(Polygon(P(-62, -2), P(-30, 50), P(-6, 20), P(-22, 14), P(-36, 22), P(-48, 4)), Palette.White, 4);
        p.Fill(Polygon(P(30, 22), P(60, 70), P(88, 22), P(72, 30), P(58, 18), P(44, 30)), Palette.White, 4);
        p.Fill(Polygon(P(-110, -80), P(110, -80), P(110, -60), P(-110, -64)), Palette.White, 4);
        foreach (var (i, (x, y)) in Indexed((-80.0, 70.0), (-6.0, 92.0), (96.0, 92.0), (10.0, 52.0)))
            foreach (var a in new[] { 0.0, Math.PI / 3, 2 * Math.PI / 3 })
                p.Line(Polyline(P(x - 9 * Math.Cos(a), y - 9 * Math.Sin(a)), P(x + 9 * Math.Cos(a), y + 9 * Math.Sin(a))), Palette.Sea, 3, 0.5);
        return p.Node;
    }

    private static Node2D Leaf()
    {
        var p = new Pic(2340);
        p.Line(Polyline(P(-70, -80), P(-40, -50)), Palette.Brown, 7);
        p.Fill(Polygon(Around(10, 10, 74, 46, Math.PI * 0.75, Math.PI * 1.75, 10).Concat(Around(10, 10, 74, 46, -Math.PI * 0.25, Math.PI * 0.75, 10).Skip(1))),
               Palette.Leaf, 5);
        p.Line(Polyline(P(-40, -50), P(64, 66)), Palette.Leaf.Darkened(0.3f), 4);
        foreach (var t in new[] { 0.25, 0.45, 0.65 })
        {
            var (mx, my) = (-40 + 104 * t, -50 + 116 * t);
            p.Line(Polyline(P(mx, my), P(mx - 30, my + 18)), Palette.Leaf.Darkened(0.3f), 3, 1);
            p.Line(Polyline(P(mx, my), P(mx + 28, my - 16)), Palette.Leaf.Darkened(0.3f), 3, 1);
        }
        return p.Node;
    }

    private static Node2D Bone()
    {
        var p = new Pic(2360);
        var knobs = new[] { (-82.0, 40.0), (-56.0, 66.0), (56.0, -66.0), (82.0, -40.0) }.Select(k => Circle(k.Item1, k.Item2, 26));
        var shaft = Polygon(P(-58, 30), P(-46, 42), P(46, -30), P(58, -42), P(46, -54), P(-34, 18));
        p.Blob(new Color("f6efdc"), 5, knobs.Append(shaft).ToArray());
        return p.Node;
    }
}
