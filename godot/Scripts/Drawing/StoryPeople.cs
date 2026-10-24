using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// The people in Bip's Stories (Content/stories/scenes.json "newPictures"): eight children and three
/// grown-ups of different South African looks, in the game's one hand-drawn style (wobbly ink, warm
/// colours, dot eyes and a smile). Same conventions as <see cref="PictureNode"/>: y-up numbers, a
/// 220 × 220 box, fixed seeds. The validator checks every id here is listed in the content.
/// </summary>
public static class StoryPeople
{
    private enum Hair { Short, Puffs, Plait, Curly, Ponytail, Scarf, Cap, Headwrap, Bun }

    /// <param name="Dress">A dress instead of a top and shorts.</param>
    /// <param name="Long">Long trousers or jeans (the bottom colour down to the shoes).</param>
    /// <param name="Seed">The person's own first seed, so their wobble never depends on the order of the list.</param>
    private sealed record Look(ulong Seed, Color Skin, Color HairColour, Hair Hair, Color Top, Color Bottom, bool Grown = false,
                               bool Dress = false, bool Long = false, Color? Extra = null, bool Glasses = false,
                               bool Freckles = false, bool Beard = false);

    private static readonly Color Dark = Color.FromHtml("#6B4428");
    private static readonly Color Brown = Color.FromHtml("#9A6A45");
    private static readonly Color LightBrown = Color.FromHtml("#C79670");
    private static readonly Color Fair = Color.FromHtml("#F2CDB0");
    private static readonly Color Black = Color.FromHtml("#2B2A33");
    private static readonly Color Blonde = Color.FromHtml("#F2CE6B");
    private static readonly Color Ginger = Color.FromHtml("#D9733A");
    private static readonly Color Grey = Color.FromHtml("#C9C5C0");

    private static readonly Dictionary<string, Look> People = new()
    {
        ["pic_child_1"] = new(6000, Dark, Black, Hair.Puffs, Palette.Sun, Palette.Sun, Dress: true),
        ["pic_child_2"] = new(6040, Dark, Black, Hair.Short, Palette.Red, Palette.Sea.Blend(0.4, Palette.Ink)),
        ["pic_child_3"] = new(6080, LightBrown, Black, Hair.Plait, Palette.Teal, Palette.Purple),
        ["pic_child_4"] = new(6120, LightBrown, Color.FromHtml("#4A3222"), Hair.Curly, Palette.Leaf, Palette.Brown),
        ["pic_child_5"] = new(6160, Fair, Blonde, Hair.Ponytail, Palette.Pink, Color.FromHtml("#4F79B8"), Long: true),
        ["pic_child_6"] = new(6200, Fair, Ginger, Hair.Short, Palette.Sea, Palette.Stone.Blend(0.3, Palette.Ink), Freckles: true),
        ["pic_child_7"] = new(6240, Brown, Black, Hair.Scarf, Palette.Orange, Palette.Orange, Dress: true, Extra: Palette.Purple),
        ["pic_child_8"] = new(6280, Dark, Black, Hair.Cap, Palette.Orange, Color.FromHtml("#4F79B8"), Extra: Palette.Sun),
        ["pic_grownup_1"] = new(6320, Dark, Black, Hair.Headwrap, Palette.Leaf, Palette.Leaf, Grown: true, Dress: true, Extra: Palette.Go),
        ["pic_grownup_2"] = new(6360, Brown, Black, Hair.Short, Color.FromHtml("#4F79B8"), Palette.Brown, Grown: true, Long: true, Beard: true),
        ["pic_grownup_3"] = new(6400, Brown, Grey, Hair.Bun, Palette.Purple, Color.FromHtml("#6D5A8A"), Grown: true, Dress: true, Glasses: true),
    };

    public static IEnumerable<string> Ids => People.Keys;

    public static Node2D? Make(string id) => People.TryGetValue(id, out var look) ? Draw(look, look.Seed) : null;

    private static Node2D Draw(Look look, ulong seed)
    {
        var n = new Node2D();
        var s = seed;
        ulong Next() => s++;
        // Proportions: a child has a bigger head and shorter legs than a grown-up.
        var (hipY, shoulderY, headY, r) = look.Grown ? (-24.0, 60.0, 84.0, 27.0) : (-40.0, 38.0, 70.0, 33.0);
        const double footY = -102;
        const double halfBody = 30;

        // Behind the head: a long plait, a ponytail or a headscarf's back.
        if (look.Hair == Hair.Scarf)
            n.AddChild(Filled(Polygon(P(0, headY + r + 12), P(r + 14, headY + 6), P(r + 20, headY - r - 16), P(-r - 20, headY - r - 16), P(-r - 14, headY + 6)),
                           Next(), fill: look.Extra ?? Palette.Purple, lineWidth: 4.5));
        if (look.Hair == Hair.Ponytail)
            n.AddChild(Rotated(Pen(Ellipse(Vector2.Zero, 13, 28), Next(), fill: look.HairColour, lineWidth: 4), P(r + 10, headY - 4), -0.4));

        // Legs and shoes.
        foreach (var x in new[] { -14.0, 14.0 })
        {
            var legColour = look.Long ? look.Bottom : look.Skin;
            n.AddChild(Filled(RoundRect(R(x - 9, footY + 2, 18, hipY - footY + 6), 7), Next(), fill: legColour, lineWidth: 4));
            n.AddChild(Filled(Ellipse(P(x + (x < 0 ? -5 : 5), footY), 15, 8), Next(), fill: Palette.Ink.Blend(0.2, Palette.Brown), lineWidth: 3.5));
        }
        // Arms (drawn before the body so the sleeves cover the shoulders).
        foreach (var side in new[] { -1.0, 1.0 })
        {
            var top = shoulderY - 6;
            n.AddChild(Filled(Polygon(P(side * (halfBody - 4), top), P(side * (halfBody + 8), top), P(side * (halfBody + 20), hipY + 8), P(side * (halfBody + 8), hipY + 4)),
                           Next(), fill: look.Skin, lineWidth: 4));
            n.AddChild(Filled(Ellipse(P(side * (halfBody + 14), hipY + 2), 9, 9), Next(), fill: look.Skin, lineWidth: 3.5, wobble: 1));
        }
        // Clothes.
        if (look.Dress)
        {
            n.AddChild(Filled(Polygon(P(-halfBody + 4, shoulderY + 2), P(halfBody - 4, shoulderY + 2), P(halfBody + 16, hipY - 18), P(-halfBody - 16, hipY - 18)),
                           Next(), fill: look.Top, lineWidth: 5));
        }
        else
        {
            if (!look.Long)
                n.AddChild(Filled(RoundRect(R(-halfBody + 2, hipY - 16, halfBody * 2 - 4, 30), 6), Next(), fill: look.Bottom, lineWidth: 4.5));
            else
                n.AddChild(Filled(RoundRect(R(-halfBody + 2, hipY - 8, halfBody * 2 - 4, 22), 6), Next(), fill: look.Bottom, lineWidth: 4.5));
            n.AddChild(Filled(RoundRect(R(-halfBody, hipY + 4, halfBody * 2, shoulderY - hipY + 2), 14), Next(), fill: look.Top, lineWidth: 5));
        }
        // Sleeves.
        foreach (var side in new[] { -1.0, 1.0 })
            n.AddChild(Filled(Polygon(P(side * (halfBody - 6), shoulderY + 2), P(side * (halfBody + 6), shoulderY), P(side * (halfBody + 12), shoulderY - 22), P(side * (halfBody - 2), shoulderY - 24)),
                           Next(), fill: look.Top, lineWidth: 4));

        // Hair that sits behind the face.
        switch (look.Hair)
        {
            case Hair.Puffs:
                foreach (var side in new[] { -1.0, 1.0 })
                    n.AddChild(Filled(Ellipse(P(side * (r + 4), headY + r - 4), 19, 19), Next(), fill: look.HairColour, lineWidth: 4));
                break;
            case Hair.Plait:
                for (var i = 0; i < 4; i++)
                    n.AddChild(Filled(Ellipse(P(r - 2 + i * 2, headY - 8 - i * 20), 10, 12), Next(), fill: look.HairColour, lineWidth: 3.5, wobble: 1));
                break;
            case Hair.Bun:
                n.AddChild(Filled(Ellipse(P(0, headY + r + 6), 15, 13), Next(), fill: look.HairColour, lineWidth: 4));
                break;
            case Hair.Curly:
                for (var i = 0; i < 7; i++)
                {
                    var a = Math.PI * (0.05 + 0.9 * i / 6);
                    n.AddChild(Filled(Ellipse(P((r + 2) * Math.Cos(a), headY + (r + 4) * Math.Sin(a)), 13, 13), Next(), fill: look.HairColour, lineWidth: 3.5, wobble: 1));
                }
                break;
        }

        // The face.
        var faceR = look.Hair == Hair.Scarf ? r - 6 : r;
        n.AddChild(Filled(Ellipse(P(0, headY), faceR, faceR + 2), Next(), fill: look.Skin, lineWidth: 5));
        if (look.Beard)
            n.AddChild(Filled(Polygon(P(-r + 4, headY - 4), P(-r + 10, headY - r + 4), P(0, headY - r - 4), P(r - 10, headY - r + 4), P(r - 4, headY - 4), P(0, headY - 14)),
                           Next(), fill: look.HairColour, lineWidth: 3.5, wobble: 1.2));
        n.AddChild(Dot(-r * 0.36, headY + 4, 4));
        n.AddChild(Dot(r * 0.36, headY + 4, 4));
        if (look.Glasses)
        {
            foreach (var side in new[] { -1.0, 1.0 })
                n.AddChild(Filled(Ellipse(P(side * r * 0.38, headY + 4), 9, 9), Next(), lineWidth: 3, wobble: 0.6));
            n.AddChild(Filled(Polyline(P(-r * 0.38 + 9, headY + 5), P(r * 0.38 - 9, headY + 5)), Next(), lineWidth: 3, wobble: 0.4));
        }
        if (look.Freckles)
            foreach (var (x, y) in new[] { (-14.0, -6.0), (-9.0, -9.0), (9.0, -9.0), (14.0, -6.0) })
                n.AddChild(Dot(x, headY + y, 2, Palette.Orange.Blend(0.4, Palette.Brown)));
        n.AddChild(Filled(Arc(P(0, headY - 6), r * 0.34, r * 0.24, 3.5, 5.9), Next(), ink: look.Beard ? Palette.White : null, lineWidth: 4, wobble: 0.8));

        // Hair and hats on top of the head.
        switch (look.Hair)
        {
            case Hair.Short:
            case Hair.Puffs:
            case Hair.Plait:
            case Hair.Ponytail:
            case Hair.Bun:
            case Hair.Curly:
                n.AddChild(Filled(Polygon(Cap(headY, r + 3, r + 6, 0.02, 0.98, fringe: 0.45)), Next(), fill: look.HairColour, lineWidth: 4));
                break;
            case Hair.Scarf:
                n.AddChild(Filled(Polygon(Cap(headY, r + 4, r + 8, 0.0, 1.0, fringe: 0.6)), Next(), fill: look.Extra ?? Palette.Purple, lineWidth: 4.5));
                break;
            case Hair.Cap:
                n.AddChild(Filled(Polygon(Cap(headY, r + 3, r + 6, 0.02, 0.98, fringe: 0.4)), Next(), fill: look.HairColour, lineWidth: 4));
                n.AddChild(Filled(Polygon(Cap(headY + 6, r + 4, r + 4, 0.0, 1.0, fringe: 0.1)), Next(), fill: look.Extra ?? Palette.Sun, lineWidth: 4.5));
                n.AddChild(Filled(Polygon(P(r - 4, headY + 10), P(r + 30, headY + 4), P(r + 26, headY - 2), P(r - 6, headY + 2)), Next(), fill: look.Extra ?? Palette.Sun, lineWidth: 4));
                break;
            case Hair.Headwrap:
                n.AddChild(Filled(Polygon(P(-r - 4, headY + 10), P(-r - 8, headY + r + 10), P(-r + 4, headY + r + 28), P(r - 4, headY + r + 28),
                                       P(r + 8, headY + r + 10), P(r + 4, headY + 10), P(0, headY + 18)), Next(), fill: look.Extra ?? Palette.Go, lineWidth: 4.5));
                n.AddChild(Filled(Ellipse(P(r - 2, headY + r + 18), 12, 10), Next(), fill: look.Extra ?? Palette.Go, lineWidth: 3.5, wobble: 1));
                break;
        }
        return n;
    }

    /// <summary>The top of a head: an arc of an oval from one side to the other, with a softly curved fringe.</summary>
    private static IEnumerable<Vector2> Cap(double cy, double rx, double ry, double from, double to, double fringe)
    {
        var points = new List<Vector2>();
        for (var i = 0; i <= 12; i++)
        {
            var a = Math.PI * (from + (to - from) * i / 12.0);
            points.Add(P(rx * Math.Cos(a), cy + ry * Math.Sin(a) * 0.95));
        }
        // Back along the hairline, a little above the eyes.
        var line = cy + ry * fringe;
        points.Add(P(-rx * 0.55, line - 4));
        points.Add(P(0, line + 2));
        points.Add(P(rx * 0.55, line - 4));
        return points;
    }

    /// <summary>
    /// A sketched shape whose colour never drops out: the pen leaves out its fill when a thin wobbly outline
    /// crosses itself (a leg, a sleeve), so a plain patch of the same colour goes underneath.
    /// </summary>
    private static Node2D Filled(SketchShape shape, ulong seed, Color? fill = null, Color? ink = null, double lineWidth = 5, double wobble = 2.2)
    {
        var pen = Pen(shape, seed, fill: fill, ink: ink, lineWidth: lineWidth, wobble: wobble);
        if (fill is not { } colour || !shape.IsClosed) return pen;
        var g = new Node2D();
        g.AddChild(new BlockPatch(shape.Outline(6).ToArray(), colour));
        g.AddChild(pen);
        return g;
    }

    private static Node2D Rotated(Node2D node, Vector2 at, double turn)
    {
        node.Position = at;
        node.Rotation = Turn(turn);
        return node;
    }
}
