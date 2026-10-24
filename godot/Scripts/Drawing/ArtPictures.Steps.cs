using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// Drawn stand-ins for the emoji the Morning Order step scenes (<see cref="StepPictures"/>) are built
/// from, so the steps match the word pictures. <see cref="ForEmoji"/> is keyed by the emoji text the
/// scenes already use; each drawing is in the usual 220 × 220 box and drawn the way the emoji faces
/// (the rocket points up and right, as the scenes turn it).
/// </summary>
public static partial class ArtPictures
{
    private static readonly Dictionary<string, Func<Node2D>> ByEmoji = new()
    {
        // Pictures that already exist as words.
        ["☀️"] = () => PictureNode.Make("pic_sun", ""),
        ["🌳"] = Tree, ["🍎"] = () => PictureNode.Make("pic_apple", ""), ["🍏"] = GreenApple,
        ["👐"] = Hands, ["🤲"] = Hands, ["🙌"] = Hands, ["✋"] = Hand,
        ["👕"] = Shirt, ["🍪"] = Biscuit, ["🐕"] = Dog, ["🚗"] = Car, ["⭐"] = Star_, ["🧼"] = Soap,
        ["🥪"] = Sandwich, ["🧦"] = Sock, ["🥄"] = Spoon, ["🌙"] = Moon, ["🎂"] = Cake, ["🛌"] = Bed,
        ["🌧️"] = Rain, ["🌦️"] = Rain, ["🥣"] = Bowl, ["🥚"] = Egg, ["🦆"] = Duck, ["☁️"] = () => Cloud(0, 0, 1, Palette.White, 2240).Node,
        ["🥛"] = Milk, ["🧃"] = Juice, ["✉️"] = Letter, ["💌"] = Letter, ["🐚"] = Shell, ["🚩"] = Flag,
        ["✏️"] = Pencil, ["🖍️"] = Crayon, ["📖"] = Book, ["🧸"] = Teddy, ["🐔"] = Hen, ["🐛"] = Bug,
        ["🪺"] = () => PictureNode.Make("pic_nest", ""), ["🐣"] = Hatching, ["🧢"] = Cap, ["⚽"] = Ball,
        ["🗑️"] = Bin, ["🍌"] = Banana, ["🧑"] = () => Man(), ["🏫"] = School,
        // New ones for the steps.
        ["🧒"] = Child, ["😁"] = () => FaceOnly(4940, grin: true), ["😋"] = () => FaceOnly(4950, grin: false),
        ["🥳"] = PartyFace, ["👵"] = Gran, ["👶"] = Baby, ["👨‍👩‍👧"] = Family, ["🧑‍🚀"] = Astronaut, ["🚶"] = Walking,
        ["🪣"] = Bucket, ["👟"] = Takkie, ["🛁"] = Bath, ["🌸"] = () => Flower(Palette.Pink, 4300), ["🌼"] = () => Flower(Palette.Sun, 4310),
        ["🌻"] = Sunflower, ["🌱"] = Sprout, ["🌾"] = Wheat, ["🚀"] = Rocket, ["🪥"] = Toothbrush, ["👖"] = Trousers,
        ["🍦"] = IceCream, ["🧺"] = Basket, ["⏰"] = Clock, ["🧻"] = ToiletRoll, ["🧈"] = Butter, ["🎒"] = SchoolBag,
        ["📚"] = Books, ["🦋"] = Butterfly, ["🌈"] = Rainbow, ["🕯️"] = Candle, ["🍕"] = Pizza, ["🧽"] = Sponge,
        ["📄"] = Paper, ["📮"] = PostBox, ["🚜"] = Tractor, ["🔥"] = Fire, ["🌕"] = FullMoon,
        // Little effects.
        ["✨"] = Sparkles, ["💨"] = Puff, ["🌬️"] = Puff, ["💤"] = Zzz, ["🌀"] = Swirl, ["💥"] = Bang,
        ["💦"] = Splash, ["💧"] = Drop, ["🫧"] = Bubbles, ["🎶"] = Notes, ["👇"] = () => Finger(down: true), ["👆"] = () => Finger(down: false),
        ["1️⃣"] = () => Number("1"), ["2️⃣"] = () => Number("2"), ["3️⃣"] = () => Number("3"),
    };

    /// <summary>The drawing for an emoji the step scenes use, or null to keep the emoji.</summary>
    public static Node2D? ForEmoji(string emoji) => ByEmoji.TryGetValue(emoji, out var draw) ? draw() : null;

    private static Node2D GreenApple()
    {
        var p = new Pic(4400);
        p.Line(Polyline(P(0, 50), P(6, 84)), Palette.Brown, 7);
        p.Fill(Polygon(P(8, 74), P(46, 94), P(30, 64)), Palette.Leaf, 3);
        p.Blob(Palette.Grass, 5, Circle(-28, 0, 56), Circle(28, 0, 56), Circle(0, -26, 50));
        p.Shine(-38, 24, 14, 26, 0.4);
        return p.Node;
    }

    // Two open hands side by side.
    private static Node2D Hands()
    {
        var left = Hand();
        left.Position = P(-50, 0);
        left.Scale = new Vector2(-0.62f, 0.62f);
        var right = Hand();
        right.Position = P(50, 0);
        right.Scale = Vector2.One * 0.62f;
        return Group(left, right);
    }

    private static Node2D Crayon()
    {
        var n = new Pic(4410);
        n.Fill(Polygon(P(-18, 70), P(18, 70), P(18, -50), P(-18, -50)), Palette.Purple, 5);
        n.Fill(Box(-18, 20, 36, 30), Palette.Card, 3);
        n.Fill(Polygon(P(-18, -50), P(18, -50), P(8, -90), P(-8, -90)), Palette.Purple, 4);
        var p = new Pic(4419);
        p.Add(n.Node);
        n.Node.Rotation = Turn(-0.6);
        return p.Node;
    }

    // A chick breaking out of its egg.
    private static Node2D Hatching()
    {
        var p = new Pic(4420);
        p.Fill(Circle(0, 30, 44), Palette.Sun, 5);
        p.Fill(Polygon(P(-10, 28), P(10, 28), P(0, 14)), Palette.Orange, 3);
        p.Dot(-14, 42, 5).Dot(14, 42, 5);
        p.Fill(Polygon(Around(0, -30, 66, 70, Math.PI, 2 * Math.PI, 12).Concat(new[] { P(66, -30), P(44, -6), P(22, -26), P(0, -4), P(-22, -26), P(-44, -6) })), new Color("f3e2c4"), 5);
        return p.Node;
    }

    private static Node2D School()
    {
        var p = new Pic(4430);
        p.Fill(Box(-104, -90, 208, 110), Palette.Red.Lightened(0.25f), 5);
        p.Fill(Polygon(P(-40, 20), P(40, 20), P(40, 70), P(0, 96), P(-40, 70)), Palette.Red.Lightened(0.25f), 5);
        p.Fill(Circle(0, 56, 14), Palette.Card, 3);
        p.Line(Polyline(P(0, 56), P(0, 64)), line: 3).Line(Polyline(P(0, 56), P(6, 56)), line: 3);
        p.Fill(Box(-18, -90, 36, 60), Palette.Brown, 4);
        foreach (var x in new[] { -86.0, -54, 34, 66 }) p.Fill(Box(x, -40, 22, 26), Palette.Ice, 3);
        p.Line(Polyline(P(0, 96), P(0, 116)), line: 3).Fill(Polygon(P(0, 116), P(26, 108), P(0, 100)), Palette.Sun, 2);
        return p.Node;
    }

    // A child: head, shirt, shorts and legs.
    private static Node2D Child()
    {
        var p = new Pic(4440);
        foreach (var x in new[] { -16.0, 16 }) Tube(p, 13, Skin, P(x, -40), P(x, -96));
        foreach (var side in new[] { -1.0, 1.0 }) Tube(p, 11, Skin, P(side * 30, 10), P(side * 54, -40));
        p.Fill(Box(-32, -56, 64, 24), Palette.Sea, 4);
        p.Fill(Polygon(P(-34, 20), P(34, 20), P(32, -40), P(-32, -40)), Palette.Sun, 5);
        p.Fill(Polygon(Around(0, 60, 40, 44, -0.2, Math.PI + 0.2, 8)), Palette.Ink.Lightened(0.15f), 4);
        Face(p, 0, 52, 34, 38);
        return p.Node;
    }

    /// <summary>A round face on its own: a big grin, or licking its lips (yummy). Each use passes its own seed block.</summary>
    private static Node2D FaceOnly(ulong seed, bool grin)
    {
        var p = new Pic(seed);
        p.Fill(Circle(0, 0, 90), Palette.Sun, 5);
        p.Dot(-30, 24, 9).Dot(30, 24, 9);
        if (grin)
        {
            p.Fill(Polygon(new[] { P(-50, -10), P(50, -10) }.Concat(Around(0, -10, 50, 44, 0, -Math.PI, 10).Skip(1))), Palette.White, 4);
            p.Line(Polyline(P(-46, -24), P(46, -24)), line: 3);
        }
        else
        {
            p.Line(Arc(P(0, -20), 44, 26, Math.PI + 0.3, 2 * Math.PI - 0.3), line: 5);
            p.Fill(Ellipse(P(26, -44), 16, 12), Palette.Pink, 3);
        }
        p.Dot(-56, -10, 10, Palette.Pink.WithAlpha(0.7)).Dot(56, -10, 10, Palette.Pink.WithAlpha(0.7));
        return p.Node;
    }

    private static Node2D PartyFace()
    {
        var p = new Pic(4460);
        p.Add(FaceOnly(4960, grin: true));
        p.Fill(Polygon(P(-40, 70), P(20, 120), P(30, 64)), Palette.Purple, 4);
        p.Dot(20, 120, 9, Palette.Sun);
        return p.Node;
    }

    // A granny with grey hair in a bun and glasses.
    private static Node2D Gran()
    {
        var p = new Pic(4470);
        p.Fill(Circle(0, 80, 24), Palette.Stone, 4);
        p.Fill(Polygon(Around(0, 0, 66, 76, -0.1, Math.PI + 0.1, 12)), Palette.Stone, 5);
        Face(p, 0, -6, 54, 64);
        p.Line(Circle(-20, 0, 14), line: 3).Line(Circle(20, 0, 14), line: 3).Line(Polyline(P(-6, 0), P(6, 0)), line: 3);
        p.Fill(Polygon(P(-60, -80), P(60, -80), P(80, -110), P(-80, -110)), Palette.Purple, 4);
        return p.Node;
    }

    private static Node2D Baby()
    {
        var p = new Pic(4480);
        p.Fill(Ellipse(P(0, -50), 60, 50), Palette.EggBlue, 5);
        Face(p, 0, 26, 52, 52);
        p.Line(Polyline(P(-4, 76), P(2, 90), P(10, 80)), line: 4);
        p.Fill(Circle(0, -8, 10), Palette.Sea, 3);
        return p.Node;
    }

    // A family: a grown-up, a child and another grown-up.
    private static Node2D Family()
    {
        var p = new Pic(4490);
        p.Add(Man(), -60, 0, 0.7);
        p.Add(Child(), 0, -24, 0.6);
        p.Add(Mum(), 60, 20, 0.5);
        return p.Node;
    }

    private static Node2D Astronaut()
    {
        var p = new Pic(4500);
        foreach (var x in new[] { -20.0, 20 }) Tube(p, 18, Palette.White, P(x, -40), P(x, -96));
        foreach (var side in new[] { -1.0, 1.0 }) Tube(p, 16, Palette.White, P(side * 40, 14), P(side * 64, -40));
        p.Fill(RoundRect(R(-44, -50, 88, 80), 20), Palette.White, 5);
        p.Fill(Box(-20, -30, 40, 24), Palette.Red, 3);
        p.Fill(Circle(0, 62, 48), Palette.White, 5);
        p.Fill(Ellipse(P(0, 62), 34, 30), Palette.Ink.Lightened(0.2f), 4);
        p.Shine(-12, 74, 14, 8, 0.4);
        return p.Node;
    }

    private static Node2D Walking()
    {
        var p = new Pic(4510);
        Tube(p, 13, Palette.Ink.Lightened(0.3f), P(-6, -36), P(-30, -96));
        Tube(p, 13, Palette.Ink.Lightened(0.3f), P(6, -36), P(30, -96));
        Tube(p, 11, Skin, P(-20, 20), P(-44, -30));
        Tube(p, 11, Skin, P(20, 20), P(44, -20));
        p.Fill(Polygon(P(-30, 30), P(30, 30), P(26, -40), P(-26, -40)), Palette.Teal, 5);
        p.Fill(Polygon(Around(0, 66, 32, 36, -0.2, Math.PI + 0.2, 8)), Palette.Ink.Lightened(0.15f), 4);
        Face(p, 0, 60, 28, 32);
        return p.Node;
    }

    private static Node2D Bucket()
    {
        var p = new Pic(4520);
        p.Line(Arc(P(0, 30), 70, 66, 0.1, Math.PI - 0.1), line: 6);
        p.Fill(Polygon(P(-74, 30), P(74, 30), P(56, -90), P(-56, -90)), Palette.Red, 5);
        p.Fill(Ellipse(P(0, 30), 74, 16), Palette.Red.Darkened(0.2f), 4);
        return p.Node;
    }

    // A takkie (running shoe).
    private static Node2D Takkie()
    {
        var p = new Pic(4530);
        p.Fill(Polygon(P(-100, -40), P(-90, 40), P(-30, 50), P(10, 10), P(90, -10), P(104, -40)), Palette.Sea, 5);
        p.Fill(Polygon(P(-104, -40), P(104, -40), P(104, -60), P(-104, -60)), Palette.White, 4);
        foreach (var x in new[] { -30.0, -10, 10 }) p.Line(Polyline(P(x - 10, 34 - (x + 30) * 0.6), P(x + 14, 24 - (x + 30) * 0.6)), Palette.White, 4);
        return p.Node;
    }

    private static Node2D Bath()
    {
        var p = new Pic(4540);
        foreach (var x in new[] { -70.0, 70 }) p.Line(Polyline(P(x, -50), P(x * 1.1, -84)), line: 7);
        p.Line(Polyline(P(-100, 0), P(-100, 70), P(-80, 76)), Silver, 8);
        p.Fill(Polygon(Around(0, 0, 104, 60, Math.PI, 2 * Math.PI, 14)), Palette.White, 5);
        p.Fill(Box(-110, -6, 220, 14), Palette.White, 4);
        foreach (var (x, y, r) in new[] { (-50.0, 18.0, 16.0), (-18.0, 26.0, 20.0), (20.0, 20.0, 16.0), (50.0, 26.0, 14.0) }) p.Fill(Circle(x, y, r), Palette.Bubble, 3);
        return p.Node;
    }

    private static Node2D Flower(Color petals, ulong seed)
    {
        var p = new Pic(seed);
        p.Line(Polyline(P(0, 0), P(0, -104)), Palette.Leaf, 8);
        p.Fill(Polygon(P(0, -60), P(-44, -40), P(-10, -74)), Palette.Leaf, 3);
        for (var i = 0; i < 6; i++)
        {
            var a = i * Math.PI / 3;
            p.Fill(Circle(36 * Math.Cos(a), 36 + 36 * Math.Sin(a), 26), petals, 4);
        }
        p.Fill(Circle(0, 36, 22), Palette.Orange, 4);
        return p.Node;
    }

    private static Node2D Sunflower()
    {
        var p = new Pic(4550);
        p.Line(Polyline(P(0, 0), P(0, -104)), Palette.Leaf, 9);
        for (var i = 0; i < 14; i++)
        {
            var a = i * 2 * Math.PI / 14;
            p.Fill(Polygon(P(40 * Math.Cos(a - 0.2), 40 + 40 * Math.Sin(a - 0.2)), P(78 * Math.Cos(a), 40 + 78 * Math.Sin(a)), P(40 * Math.Cos(a + 0.2), 40 + 40 * Math.Sin(a + 0.2))), Palette.Sun, 3);
        }
        p.Fill(Circle(0, 40, 42), Palette.Brown, 4);
        return p.Node;
    }

    private static Node2D Sprout()
    {
        var p = new Pic(4560);
        p.Fill(Polygon(Around(0, -60, 90, 30, 0, Math.PI, 10)), Palette.Brown, 4);
        p.Line(Polyline(P(0, -40), P(0, 30)), Palette.Leaf, 7);
        p.Fill(Polygon(Around(-30, 40, 30, 16, 0, 2 * Math.PI, 12)), Palette.Grass, 4);
        p.Fill(Polygon(Around(30, 50, 30, 16, 0, 2 * Math.PI, 12)), Palette.Grass, 4);
        return p.Node;
    }

    private static Node2D Wheat()
    {
        var p = new Pic(4570);
        foreach (var (i, lean) in Indexed(-0.25, 0.0, 0.25))
        {
            var top = P(lean * 160, 80);
            p.Line(Polyline(P(lean * 40, -100), top), Palette.Sun.Darkened(0.2f), 5);
            for (var k = 0; k < 4; k++)
                foreach (var side in new[] { -1.0, 1.0 })
                    p.Fill(Ellipse(P(lean * 160 * (0.6 + k * 0.1) + side * 9, 20 + k * 18), 8, 13), Palette.Sun, 2.5);
        }
        return p.Node;
    }

    // A rocket pointing up and to the right, as the scenes turn it.
    private static Node2D Rocket()
    {
        var p = new Pic(4580);
        var n = new Pic(4581);
        foreach (var side in new[] { -1.0, 1.0 }) n.Fill(Polygon(P(side * 26, -30), P(side * 60, -80), P(side * 26, -70)), Palette.Red, 4);
        n.Fill(Polygon(P(-28, -70), P(28, -70), P(30, 40), P(0, 100), P(-30, 40)), Palette.White, 5);
        n.Fill(Polygon(P(-30, 40), P(30, 40), P(0, 100)), Palette.Red, 4);
        n.Fill(Circle(0, 10, 16), Palette.Sea, 4);
        n.Fill(Polygon(P(-16, -70), P(16, -70), P(0, -100)), Palette.Orange, 3);
        p.Add(n.Node);
        n.Node.Rotation = Turn(-Math.PI / 4);
        return p.Node;
    }

    private static Node2D Toothbrush()
    {
        var n = new Pic(4590);
        n.Fill(Polygon(P(-10, 60), P(10, 60), P(8, -100), P(-8, -100)), Palette.Teal, 4);
        n.Fill(RoundRect(R(-14, 56, 28, 50), 6), Palette.Teal, 4);
        n.Fill(Box(14, 60, 18, 42), Palette.White, 3);
        n.Fill(Box(30, 66, 16, 30), Palette.Ice, 2);
        var p = new Pic(4599);
        p.Add(n.Node);
        n.Node.Rotation = Turn(-0.4);
        return p.Node;
    }

    private static Node2D Trousers()
    {
        var p = new Pic(4600);
        p.Fill(Polygon(P(-56, 90), P(56, 90), P(66, -100), P(18, -100), P(0, 20), P(-18, -100), P(-66, -100)), Palette.Sea.Darkened(0.2f), 5);
        p.Fill(Box(-56, 74, 112, 16), Palette.Sea.Darkened(0.35f), 3);
        return p.Node;
    }

    private static Node2D IceCream()
    {
        var p = new Pic(4610);
        p.Fill(Polygon(P(-40, 10), P(40, 10), P(0, -104)), Palette.Sand.Darkened(0.1f), 5);
        p.Line(Polyline(P(-24, -10), P(14, -50)), Palette.Brown, 3).Line(Polyline(P(24, -10), P(-14, -50)), Palette.Brown, 3);
        p.Blob(Palette.Pink, 5, Circle(-24, 30, 30), Circle(24, 30, 30), Circle(0, 60, 34));
        p.Fill(Circle(0, 98, 10), Palette.Red, 3);
        return p.Node;
    }

    private static Node2D Basket()
    {
        var p = new Pic(4620);
        p.Line(Arc(P(0, 0), 70, 80, 0.15, Math.PI - 0.15), Palette.Brown, 9);
        p.Fill(Polygon(P(-96, 6), P(96, 6), P(74, -90), P(-74, -90)), Palette.LightBrown, 5);
        foreach (var y in new[] { -14.0, -40, -66 }) p.Line(Polyline(P(-86, y), P(86, y)), Palette.Brown, 3, 1);
        foreach (var x in new[] { -40.0, 0, 40 }) p.Line(Polyline(P(x, 6), P(x * 0.8, -90)), Palette.Brown, 3, 1);
        return p.Node;
    }

    // An alarm clock.
    private static Node2D Clock()
    {
        var p = new Pic(4630);
        foreach (var side in new[] { -1.0, 1.0 }) p.Fill(Circle(side * 56, 66, 22), Palette.Sun, 4);
        foreach (var side in new[] { -1.0, 1.0 }) p.Line(Polyline(P(side * 50, -70), P(side * 66, -96)), line: 6);
        p.Fill(Circle(0, 0, 76), Palette.Red, 5);
        p.Fill(Circle(0, 0, 58), Palette.Card, 4);
        p.Line(Polyline(P(0, 0), P(0, 40)), line: 6).Line(Polyline(P(0, 0), P(26, -12)), line: 6);
        return p.Node;
    }

    private static Node2D ToiletRoll()
    {
        var p = new Pic(4640);
        p.Fill(Polygon(P(-60, 60), P(60, 60), P(60, -80), P(-60, -80)), Palette.White, 5);
        p.Fill(Polygon(P(40, -60), P(96, -60), P(96, -100), P(40, -100)), Palette.White, 4);
        p.Fill(Ellipse(P(0, 60), 60, 22), Palette.White, 5);
        p.Fill(Ellipse(P(0, 60), 20, 8), Palette.Stone, 3);
        return p.Node;
    }

    private static Node2D Butter()
    {
        var p = new Pic(4650);
        p.Fill(Ellipse(P(0, -40), 104, 30), Palette.EggBlue, 5);
        p.Fill(Polygon(P(-60, -40), P(30, -40), P(60, -20), P(60, 30), P(-30, 30), P(-60, 10)), Palette.Sun.Lightened(0.35f), 5);
        p.Line(Polyline(P(-60, 10), P(30, 10), P(60, 30)), line: 4).Line(Polyline(P(30, 10), P(30, -40)), line: 4);
        return p.Node;
    }

    private static Node2D SchoolBag()
    {
        var p = new Pic(4660);
        p.Line(Arc(P(0, 60), 34, 30, 0.1, Math.PI - 0.1), line: 8);
        p.Fill(RoundRect(R(-70, -96, 140, 160), 34), Palette.Orange, 5);
        p.Fill(RoundRect(R(-46, -76, 92, 64), 16), Palette.Orange.Darkened(0.12f), 4);
        p.Line(Polyline(P(-70, 20), P(70, 20)), line: 4);
        return p.Node;
    }

    private static Node2D Books()
    {
        var p = new Pic(4670);
        foreach (var (i, (y, c, shift)) in Indexed((-70.0, Palette.Red, 0.0), (-26.0, Palette.Teal, 10.0), (18.0, Palette.Sun, -8.0)))
        {
            p.Fill(Box(-80 + shift, y, 160, 40), c, 5);
            p.Fill(Box(60 + shift, y + 6, 16, 28), Palette.Card, 3);
        }
        return p.Node;
    }

    private static Node2D Butterfly()
    {
        var p = new Pic(4680);
        foreach (var side in new[] { -1.0, 1.0 })
        {
            p.Fill(Ellipse(P(side * 50, 34), 48, 40), Palette.Purple, 5);
            p.Fill(Ellipse(P(side * 40, -34), 34, 30), Palette.Pink, 5);
            p.Fill(Circle(side * 50, 34, 14), Palette.Sun, 3);
            p.Line(Polyline(P(side * 4, 60), P(side * 24, 96)), line: 3);
        }
        p.Fill(Ellipse(P(0, 0), 12, 66), Palette.Ink, 3);
        return p.Node;
    }

    private static Node2D Rainbow()
    {
        var p = new Pic(4690);
        foreach (var (i, c) in Indexed(Palette.Red, Palette.Orange, Palette.Sun, Palette.Grass, Palette.Sea, Palette.Purple))
            p.Line(Arc(P(0, -60), 100 - i * 13, 120 - i * 13, 0, Math.PI), c, 14, 0.8);
        Cloud(-80, -60, 0.4, Palette.White, 0, p);
        Cloud(80, -60, 0.4, Palette.White, 0, p);
        return p.Node;
    }

    private static Node2D Candle()
    {
        var p = new Pic(4700);
        p.Fill(Box(-24, -100, 48, 140), Palette.Sea.Lightened(0.3f), 5);
        p.Line(Polyline(P(0, 40), P(0, 54)), line: 4);
        p.Fill(Polygon(Around(0, 66, 16, 16, Math.PI, 2 * Math.PI, 6).Append(P(0, 100))), Palette.Orange, 3);
        p.Fill(Polygon(Around(0, 64, 8, 8, Math.PI, 2 * Math.PI, 4).Append(P(0, 82))), Palette.Sun, 2);
        return p.Node;
    }

    private static Node2D Pizza()
    {
        var p = new Pic(4710);
        p.Fill(Polygon(P(-80, 70), P(80, 70), P(0, -100)), Palette.Sun.Lightened(0.2f), 5);
        p.Fill(Polygon(P(-84, 70), P(84, 70), P(80, 88), P(-80, 88)), new Color("d99a4e"), 5);
        foreach (var (x, y) in new[] { (-30.0, 40.0), (24.0, 30.0), (0.0, -20.0), (-6.0, 50.0) }) p.Fill(Circle(x, y, 13), Palette.Red, 3);
        return p.Node;
    }

    private static Node2D Sponge()
    {
        var p = new Pic(4720);
        p.Fill(RoundRect(R(-80, -50, 160, 100), 16), Palette.Sun, 5);
        p.Fill(Box(-80, -50, 160, 30), Palette.Leaf, 4);
        foreach (var (x, y) in new[] { (-50.0, 20.0), (-10.0, 30.0), (30.0, 10.0), (54.0, 30.0), (10.0, 0.0) }) p.Dot(x, y, 5, Palette.Orange);
        return p.Node;
    }

    private static Node2D Paper()
    {
        var p = new Pic(4730);
        p.Fill(Polygon(P(-70, -96), P(70, -96), P(70, 66), P(40, 96), P(-70, 96)), Palette.White, 5);
        p.Fill(Polygon(P(40, 96), P(40, 66), P(70, 66)), Palette.Stone, 3);
        foreach (var y in new[] { 50.0, 20, -10, -40 }) p.Line(Polyline(P(-48, y), P(48, y)), Palette.Stone.Darkened(0.2f), 4, 1);
        return p.Node;
    }

    // A red post box.
    private static Node2D PostBox()
    {
        var p = new Pic(4740);
        p.Fill(Polygon(Around(0, 40, 54, 50, 0, Math.PI, 12).Concat(new[] { P(-54, -100), P(54, -100) })), Palette.Red, 5);
        p.Fill(Box(-34, 20, 68, 12), Palette.Ink, 3);
        p.Fill(Box(-20, -40, 40, 30), Palette.Card, 3);
        return p.Node;
    }

    private static Node2D Tractor()
    {
        var p = new Pic(4750);
        p.Fill(Box(-20, -30, 90, 60), Palette.Leaf, 5);
        p.Fill(Box(-90, -30, 80, 100), Palette.Leaf.Darkened(0.1f), 5);
        p.Fill(Box(-76, 10, 52, 46), Palette.Ice, 3);
        p.Fill(Box(40, 30, 12, 40), Palette.Ink, 3);
        Wheel(p, -50, -44, 46);
        Wheel(p, 64, -54, 28);
        return p.Node;
    }

    private static Node2D Fire()
    {
        var p = new Pic(4760);
        p.Fill(Polygon(P(-60, -60), P(-70, 10), P(-40, -10), P(-30, 60), P(0, 10), P(20, 90), P(40, 0), P(66, 30), P(60, -60)), Palette.Red, 5);
        p.Fill(Polygon(P(-40, -60), P(-30, -10), P(0, -30), P(14, 40), P(30, -20), P(40, -60)), Palette.Sun, 4);
        return p.Node;
    }

    private static Node2D FullMoon()
    {
        var p = new Pic(4770);
        p.Fill(Circle(0, 0, 96), Palette.Stone.Lightened(0.3f), 5);
        foreach (var (x, y, r) in new[] { (-30.0, 30.0, 18.0), (34.0, -20.0, 24.0), (-20.0, -46.0, 12.0), (40.0, 46.0, 10.0) })
            p.Fill(Circle(x, y, r), Palette.Stone, 3);
        return p.Node;
    }

    // MARK: - Effects

    private static Node2D Sparkles()
    {
        var p = new Pic(4800);
        foreach (var (x, y, r) in new[] { (-30.0, 30.0, 50.0), (50.0, -30.0, 32.0), (40.0, 60.0, 20.0) })
            p.Fill(Polygon(P(x, y + r), P(x + r * 0.22, y + r * 0.22), P(x + r, y), P(x + r * 0.22, y - r * 0.22), P(x, y - r), P(x - r * 0.22, y - r * 0.22), P(x - r, y), P(x - r * 0.22, y + r * 0.22)), Palette.Sun, 3);
        return p.Node;
    }

    private static Node2D Puff()
    {
        var p = new Pic(4810);
        foreach (var (i, y) in Indexed(40.0, 0.0, -40.0))
            p.Line(Polyline(Around(-60 + i * 20, y, 90, 30, Math.PI * 0.9, Math.PI * 0.1, 10)), Palette.Stone.Darkened(0.2f), 9, 1);
        return p.Node;
    }

    private static Node2D Zzz()
    {
        var p = new Pic(4820);
        p.Text("z", 60, -40, -40, Palette.SleepyZ).Text("z", 80, 10, 10, Palette.SleepyZ).Text("Z", 100, 56, 60, Palette.SleepyZ);
        return p.Node;
    }

    private static Node2D Swirl()
    {
        var p = new Pic(4830);
        p.Line(Polyline(Enumerable.Range(0, 40).Select(i => i * 0.4).Select(a => P(a * 5.4 * Math.Cos(a), a * 5.4 * Math.Sin(a)))), Palette.Sea, 10, 0.8);
        return p.Node;
    }

    private static Node2D Bang()
    {
        var p = new Pic(4840);
        p.Fill(Polygon(Enumerable.Range(0, 16).Select(i => (a: i * Math.PI / 8, r: i % 2 == 0 ? 96.0 : 50)).Select(q => P(q.r * Math.Cos(q.a), q.r * Math.Sin(q.a)))), Palette.Orange, 5);
        p.Fill(Polygon(Enumerable.Range(0, 16).Select(i => (a: i * Math.PI / 8, r: i % 2 == 0 ? 56.0 : 28)).Select(q => P(q.r * Math.Cos(q.a), q.r * Math.Sin(q.a)))), Palette.Sun, 3);
        return p.Node;
    }

    private static void DropAt(Pic p, double x, double y, double r)
    {
        p.Fill(Polygon(Around(x, y, r, r, Math.PI, 2 * Math.PI, 8).Append(P(x, y + r * 2.2))), Palette.Sea, 3);
    }

    private static Node2D Drop()
    {
        var p = new Pic(4850);
        DropAt(p, 0, -20, 50);
        p.Shine(-18, -10, 12, 30, 0);
        return p.Node;
    }

    private static Node2D Splash()
    {
        var p = new Pic(4860);
        foreach (var (x, y, r) in new[] { (-40.0, -20.0, 30.0), (30.0, 10.0, 26.0), (-10.0, 50.0, 20.0) }) DropAt(p, x, y, r);
        return p.Node;
    }

    private static Node2D Bubbles()
    {
        var p = new Pic(4870);
        foreach (var (x, y, r) in new[] { (-30.0, -20.0, 46.0), (40.0, 30.0, 34.0), (-10.0, 66.0, 22.0) })
            p.Fill(Circle(x, y, r), Palette.Bubble, 3).Shine(x - r * 0.35, y + r * 0.35, r * 0.4, r * 0.25);
        return p.Node;
    }

    private static Node2D Notes()
    {
        var p = new Pic(4880);
        foreach (var (x, y) in new[] { (-50.0, -40.0), (40.0, -60.0) })
        {
            p.Fill(Ellipse(P(x, y), 24, 18), Palette.Purple, 4);
            p.Line(Polyline(P(x + 20, y + 6), P(x + 20, y + 110)), line: 6);
        }
        p.Fill(Polygon(P(-30, 70), P(60, 50), P(60, 30), P(-30, 50)), Palette.Ink, 3);
        return p.Node;
    }

    private static Node2D Finger(bool down)
    {
        var p = new Pic(down ? 4890UL : 4900UL);
        p.Add(WordPictures.Pointer(down ? P(0, 90) : P(0, -90), down ? P(0, -90) : P(0, 90), down ? 4895UL : 4905UL));
        return p.Node;
    }

    private static Node2D Number(string digit)
    {
        var p = new Pic(4910);
        p.Fill(RoundRect(R(-80, -80, 160, 160), 30), Palette.Sea, 5);
        p.Text(digit, 130, 0, 0, Palette.Ink);
        return p.Node;
    }
}
