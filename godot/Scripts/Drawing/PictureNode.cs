using System;
using System.Linq;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// Hand-drawn pictures, by picture id from Content/asset_manifest.json (pic_&lt;word&gt;). A port of the
/// Swift app's PictureNode with the same shapes, colours and seeds (numbers are the Swift y-up ones,
/// flipped by <see cref="Up"/>). Each fits in roughly a 220 × 220 box around the origin. Ids without
/// real art yet show a Morning Order step scene, an emoji stand-in, or a marked placeholder, so
/// pre-readers always see a picture, never just a word.
/// </summary>
public static class PictureNode
{
    public static Node2D Make(string id, string word) => id switch
    {
        "pic_sun" => Sun(),
        "pic_ant" => Ant(),
        "pic_ants" => Several(Ant, new[] { P(-50, 50), P(40, 0), P(-30, -60) }, 0.5),
        "pic_tap" => Tap(),
        "pic_pan" => Pan(),
        "pic_ink" => Ink(),
        "pic_net" => Net(),
        "pic_pin" => Pin(),
        "pic_pins" => Several(Pin, new[] { P(-40, 20), P(10, -10), P(50, -40) }, 0.6),
        "pic_tin" => Tin(),
        "pic_apple" => Apple(),
        "pic_tent" => Tent(),
        "pic_pig" => Pig(),
        "pic_igloo" => Igloo(),
        "pic_nest" => Nest(),
        _ => WordPictures.Make(id) ?? StepPictures.Make(id) ?? EmojiPictures.Make(id) ?? Placeholder(word),
    };

    public static Node2D Make(string word) => Make($"pic_{word}", word);

    /// <summary>True when an id has a real picture (hand-drawn, a step scene or an emoji), not the placeholder.</summary>
    public static bool HasArt(string id) =>
        HandDrawnIds.Contains(id) || WordPictures.Ids.Contains(id) || StepPictures.Make(id) is { } step && Free(step) || EmojiPictures.Icons.ContainsKey(id);

    private static bool Free(Node node)
    {
        node.Free();
        return true;
    }

    /// <summary>Every picture id drawn by hand here (the rest come from WordPictures, StepPictures or EmojiPictures).</summary>
    public static readonly string[] HandDrawnIds =
    {
        "pic_sun", "pic_ant", "pic_ants", "pic_tap", "pic_pan", "pic_ink", "pic_net", "pic_pin", "pic_pins",
        "pic_tin", "pic_apple", "pic_tent", "pic_pig", "pic_igloo", "pic_nest",
    };

    private static Node2D Sun()
    {
        var n = new Node2D();
        for (var i = 0; i < 10; i++)
        {
            var a = (float)(i) / 10 * 2 * Mathf.Pi;
            var ray = Polyline(new[] {
                P(72 * Mathf.Cos(a), 72 * Mathf.Sin(a)),
                P(100 * Mathf.Cos(a), 100 * Mathf.Sin(a)),
             });
            n.AddChild(Pen(ray, ink: Palette.Orange, lineWidth: 8, seed: (ulong)(300 + i)));
        }
        n.AddChild(Pen(Ellipse(Vector2.Zero, 60, 60), fill: Palette.Sun, seed: 311));
        n.AddChild(Dot(-20, 12, 6));
        n.AddChild(Dot(20, 12, 6));
        n.AddChild(Pen(Arc(P(0, -2), 26, 20, Mathf.Pi * 1.15, Mathf.Pi * 1.85), lineWidth: 5, seed: 312));
        return n;
    }

    private static Node2D Apple()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polyline(new[] { P(0, 48), P(4, 70), P(10, 88) }), ink: Palette.Brown, lineWidth: 9, seed: 320));
        var leaf = Pen(Ellipse(Vector2.Zero, 28, 12), fill: Palette.Leaf, lineWidth: 4, seed: 321);
        leaf.Position = P(36, 78);
        leaf.Rotation = Turn(0.5);
        n.AddChild(leaf);
        var body = Polygon(new[] {
            P(0, 44), P(30, 60), P(66, 44), P(80, 0),
            P(66, -50), P(34, -76), P(0, -66), P(-34, -76),
            P(-66, -50), P(-80, 0), P(-66, 44), P(-30, 60),
         });
        n.AddChild(Pen(body, fill: Palette.Red, seed: 322));
        var shine = Oval(18, 34, Palette.White.WithAlpha(0.55));
        shine.Position = P(-40, 10);
        shine.Rotation = Turn(-0.3);
        n.AddChild(shine);
        return n;
    }

    private static Node2D Tent()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polyline(new[] { P(-110, -70), P(110, -70) }), ink: Palette.Leaf, lineWidth: 7, seed: 330));
        n.AddChild(Pen(Polyline(new[] { P(0, 80), P(0, 112) }), lineWidth: 5, seed: 331));
        n.AddChild(Pen(Polygon(new[] { P(0, 112), P(34, 102), P(0, 92) }), fill: Palette.Red, lineWidth: 4, seed: 332));
        n.AddChild(Pen(Polygon(new[] { P(-100, -68), P(0, 84), P(100, -68) }), fill: Palette.Orange, seed: 333));
        n.AddChild(Pen(Polygon(new[] { P(-34, -68), P(0, 14), P(34, -68) }), fill: Palette.Brown, lineWidth: 4, seed: 334));
        return n;
    }

    private static Node2D Pig()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polygon(new[] { P(-58, 30), P(-70, 92), P(-18, 58) }), fill: Palette.DeepPink, lineWidth: 4.5, seed: 340));
        n.AddChild(Pen(Polygon(new[] { P(58, 30), P(70, 92), P(18, 58) }), fill: Palette.DeepPink, lineWidth: 4.5, seed: 341));
        n.AddChild(Pen(Ellipse(P(0, -5), 82, 72), fill: Palette.Pink, seed: 342));
        n.AddChild(Pen(Ellipse(P(0, -30), 32, 22), fill: Palette.DeepPink, lineWidth: 4.5, seed: 343));
        n.AddChild(Dot(-11, -30, 5));
        n.AddChild(Dot(11, -30, 5));
        n.AddChild(Dot(-30, 18, 7));
        n.AddChild(Dot(30, 18, 7));
        return n;
    }

    private static Node2D Igloo()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polyline(new[] { P(-115, -62), P(115, -62) }), ink: Palette.Sea, lineWidth: 6, seed: 350));
        var dome = Enumerable.Range(0, 13).Select(i => {
            var a = (float)(i) / 12 * Mathf.Pi;
            return P(100 * Mathf.Cos(a), -60 + 110 * Mathf.Sin(a));
        }).ToList();
        dome.Add(P(-100, -60));
        n.AddChild(Pen(Polygon(dome), fill: Palette.Ice, seed: 351));
        foreach (var (i, height) in Indexed((float)(-20), 18, 52))
        {
            var halfWidth = 100 * Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((height + 60) / 110, 2)));
            n.AddChild(Pen(Polyline(new[] { P(-halfWidth + 6, height), P(halfWidth - 6, height) }), ink: Palette.Sea, lineWidth: 3.5, seed: (ulong)(352 + i)));
        }
        var door = Enumerable.Range(0, 9).Select(i => {
            var a = (float)(i) / 8 * Mathf.Pi;
            return P(30 * Mathf.Cos(a), -60 + 50 * Mathf.Sin(a));
        }).ToList();
        door.Add(P(-30, -60));
        n.AddChild(Pen(Polygon(door), fill: Palette.Ink.WithAlpha(0.8), lineWidth: 4, seed: 356));
        return n;
    }

    private static Node2D Nest()
    {
        var n = new Node2D();
        foreach (var (i, x) in Indexed((float)(-36), 0, 36))
        {
            n.AddChild(Pen(Ellipse(P(x, i == 1 ? 18 : 6), 23, 30), fill: Palette.EggBlue, lineWidth: 4, seed: (ulong)(360 + i)));
        }
        var bowl = Enumerable.Range(0, 15).Select(i => {
            var a = Mathf.Pi + (float)(i) / 14 * Mathf.Pi;
            return P(100 * Mathf.Cos(a), -4 + 62 * Mathf.Sin(a));
        }).ToList();
        bowl.Add(P(-100, -4));
        n.AddChild(Pen(Polygon(bowl), fill: Palette.Brown, seed: 364));
        for (var i = 0; i < 6; i++)
        {
            var y = -14 - (float)(i) * 8;
            var w = 92 - (float)(i) * 11;
            n.AddChild(Pen(Polyline(new[] { P(-w, y), P(-w / 3, y - 6), P(w / 3, y + 4), P(w, y - 3) }),
                                   ink: Palette.LightBrown, lineWidth: 3.5, wobble: 3, seed: (ulong)(370 + i)));
        }
        return n;
    }

    /// A few copies of one picture, for plurals (ants, pins).
    private static Node2D Several(Func<Node2D> draw, Vector2[] offsets, double scale)
    {
        var n = new Node2D();
        foreach (var offset in offsets)
        {
            var copy = draw();
            copy.Scale = Vector2.One * (float)(scale);
            copy.Position = offset;
            n.AddChild(copy);
        }
        return n;
    }

    private static Node2D Ant()
    {
        var n = new Node2D();
        // Six legs, then antennae, then the three body parts on top.
        foreach (var (i, x) in Indexed((float)(-22), 0, 22))
        {
            n.AddChild(Pen(Polyline(new[] { P(x, -10), P(x - 18, -48), P(x - 30, -62) }), lineWidth: 5, seed: (ulong)(380 + i)));
            n.AddChild(Pen(Polyline(new[] { P(x, -10), P(x + 16, -48), P(x + 30, -60) }), lineWidth: 5, seed: (ulong)(383 + i)));
        }
        n.AddChild(Pen(Polyline(new[] { P(74, 20), P(88, 58), P(104, 70) }), lineWidth: 4.5, seed: 386));
        n.AddChild(Pen(Polyline(new[] { P(80, 16), P(110, 44), P(126, 46) }), lineWidth: 4.5, seed: 387));
        n.AddChild(Pen(Ellipse(P(-66, -2), 46, 36), fill: Palette.Red, seed: 388));
        n.AddChild(Pen(Ellipse(P(0, 0), 26, 22), fill: Palette.Red, seed: 389));
        n.AddChild(Pen(Ellipse(P(58, 8), 30, 28), fill: Palette.Red, seed: 390));
        n.AddChild(Dot(66, 14, 6));
        return n;
    }

    internal static Node2D Tap()
    {
        var n = new Node2D();
        // Pipe from the wall, the spout curving down, a handle on top and a falling drop.
        n.AddChild(Pen(RoundRect(R(-110, 10, 24, 70), 6), fill: Palette.Stone, lineWidth: 4.5, seed: 391));
        n.AddChild(Pen(Polygon(new[] { P(-88, 30), P(40, 30), P(62, 18), P(70, -10),
                                         P(42, -10), P(38, 4), P(-88, 4) }),
                               fill: Palette.Ice, seed: 392));
        n.AddChild(Pen(RoundRect(R(-20, 30, 16, 30), 4), fill: Palette.Stone, lineWidth: 4, seed: 393));
        n.AddChild(Pen(RoundRect(R(-52, 58, 80, 18), 8), fill: Palette.Red, lineWidth: 4.5, seed: 394));
        n.AddChild(Pen(Polygon(new[] { P(56, -30), P(70, -58), P(62, -76), P(50, -76), P(42, -58) }),
                               fill: Palette.Sea, lineWidth: 4, seed: 395));
        return n;
    }

    private static Node2D Pan()
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(52, -12, 96, 24), 10), fill: Palette.Brown, seed: 396));
        n.AddChild(Pen(Ellipse(P(-24, 0), 86, 62), fill: Palette.Ink.WithAlpha(0.75), seed: 397));
        n.AddChild(Pen(Ellipse(P(-24, 4), 66, 44), fill: Palette.Stone, lineWidth: 4, seed: 398));
        // A fried egg in the pan.
        n.AddChild(Pen(Ellipse(P(-30, 4), 40, 26), fill: Palette.White, lineWidth: 3.5, seed: 399));
        n.AddChild(Pen(Ellipse(P(-24, 6), 14, 12), fill: Palette.Sun, lineWidth: 3.5, seed: 400));
        return n;
    }

    private static Node2D Ink()
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(-62, -90, 124, 120), 26), fill: Palette.Purple, seed: 401));
        n.AddChild(Pen(RoundRect(R(-26, 28, 52, 26), 6), fill: Palette.Purple, lineWidth: 4.5, seed: 402));
        n.AddChild(Pen(RoundRect(R(-34, 52, 68, 30), 8), fill: Palette.Ink.WithAlpha(0.85), lineWidth: 4.5, seed: 403));
        n.AddChild(Pen(RoundRect(R(-40, -60, 80, 54), 8), fill: Palette.Card, lineWidth: 3.5, seed: 404));
        // A drop of ink on the label.
        n.AddChild(Pen(Polygon(new[] { P(0, -12), P(14, -34), P(8, -48), P(-8, -48), P(-14, -34) }),
                               fill: Palette.Purple, lineWidth: 3.5, seed: 405));
        return n;
    }

    private static Node2D Net()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polyline(new[] { P(-110, -100), P(-20, -6) }), ink: Palette.Brown, lineWidth: 10, seed: 406));
        // Mesh hanging below the hoop.
        var mesh = new Node2D();
        for (var i = 0; i < 5; i++)
        {
            var x = -6 + (float)(i) * 26;
            mesh.AddChild(Pen(Polyline(new[] { P(x, 50), P(x + 10, -40) }), ink: Palette.Ink.WithAlpha(0.7), lineWidth: 3, wobble: 1.5, seed: (ulong)(407 + i)));
        }
        for (var i = 0; i < 3; i++)
        {
            var y = 30 - (float)(i) * 28;
            mesh.AddChild(Pen(Polyline(new[] { P(-10 + (float)(i) * 8, y), P(110 - (float)(i) * 6, y) }), ink: Palette.Ink.WithAlpha(0.7), lineWidth: 3, wobble: 1.5, seed: (ulong)(412 + i)));
        }
        n.AddChild(mesh);
        n.AddChild(Pen(Arc(P(50, 20), 62, 70, Mathf.Pi * 1.05, Mathf.Pi * 1.95), lineWidth: 5, seed: 415));
        n.AddChild(Pen(Ellipse(P(50, 50), 66, 26), ink: Palette.Sun, lineWidth: 8, seed: 416));
        return n;
    }

    private static Node2D Pin()
    {
        var n = new Node2D();
        // A sewing pin lying across the card.
        n.AddChild(Pen(Polyline(new[] { P(-96, -70), P(62, 46) }), ink: Palette.Stone.Blend(0.5, Palette.Ink), lineWidth: 7, wobble: 1, seed: 417));
        n.AddChild(Pen(Polyline(new[] { P(-96, -70), P(62, 46) }), ink: Palette.White, lineWidth: 2, wobble: 1, seed: 418));
        n.AddChild(Pen(Ellipse(P(72, 54), 30, 30), fill: Palette.Red, seed: 419));
        var shine = Oval(14, 14, Palette.White.WithAlpha(0.8));
        shine.Position = P(62, 64);
        n.AddChild(shine);
        return n;
    }

    private static Node2D Tin()
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(-64, -86, 128, 150), 12), fill: Palette.Stone, seed: 420));
        n.AddChild(Pen(RoundRect(R(-64, -50, 128, 76), 4), fill: Palette.Red, lineWidth: 4.5, seed: 421));
        n.AddChild(Pen(Ellipse(P(0, -12), 26, 22), fill: Palette.Sun, lineWidth: 3.5, seed: 422));
        n.AddChild(Pen(Ellipse(P(0, 64), 64, 16), fill: Palette.Ice, lineWidth: 4.5, seed: 423));
        return n;
    }

    /// Not drawn yet: a dashed frame with the word written in, so it's obvious this is a stand-in.

    /// <summary>
    /// Last resort for a picture with neither art nor emoji: the word in a dashed frame, clearly
    /// marked as a stand-in.
    /// </summary>
    private static Node2D Placeholder(string word)
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(-100, -80, 200, 160), 24), 424, fill: Palette.Stone.WithAlpha(0.5),
                               ink: Palette.Ink.WithAlpha(0.5), lineWidth: 3, wobble: 3.5f));
        n.AddChild(Sketch.Letter(word, word.Length > 6 ? 48 : 64, shadow: new Color(0, 0, 0, 0)));
        var note = Sketch.Label("picture coming", 22, Palette.Ink.WithAlpha(0.55));
        note.Position += P(0, -60);
        n.AddChild(note);
        return n;
    }
}

/// <summary>A picture on a hand-drawn card (Meet the Sound, Sound Hunt, Morning Order).</summary>
public partial class PictureCard : Node2D
{
    public const float Size = 270;

    public string Word { get; } = "";

    public PictureCard() { }

    public PictureCard(string pictureId, string word, ulong seed)
    {
        Word = word;
        const float half = Size / 2;
        AddChild(Pen(RoundRect(R(-half, -half, Size, Size), 30), seed, fill: Palette.Card, lineWidth: 6));
        var picture = PictureNode.Make(pictureId, word);
        picture.Scale = Vector2.One * 0.95f;
        AddChild(picture);
    }
}
