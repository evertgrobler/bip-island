using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// Bip's Stories scenes, built from the content (Content/stories/scenes.json): a simple hand-drawn
/// background (sky, ground, sea, a room, a kitchen…) in a 960 × 440 frame, with the game's pictures
/// placed on it. Scenes are data, so a new story needs no new code. Backgrounds carry no faces (the
/// sun here is a plain circle), so the oldest children's scenes keep faces to people and animals.
/// </summary>
public static class StoryScenes
{
    public const float Width = 960;
    public const float Height = 440;
    private const double HalfW = Width / 2 - 6;
    private const double HalfH = Height / 2 - 6;
    private const double Corner = 22;

    /// <summary>Every background this file draws (the validator keeps scenes.json to these).</summary>
    public static readonly IReadOnlyList<string> Backgrounds =
        ["park", "beach", "sea", "room", "kitchen", "classroom", "rain", "field", "market", "garden", "road", "hill", "stage"];

    private static readonly Color Sky = Palette.Ice.Blend(0.35, Palette.Sea);
    private static readonly Color GreySky = Palette.Stone.Blend(0.35, Palette.Sea.Blend(0.5, Palette.Ink));
    private static readonly Color Wall = Color.FromHtml("#F7DEC0");
    private static readonly Color Floor = Palette.LightBrown;

    /// <summary>A whole scene: its background, then its pictures in order (later ones on top), then the frame.</summary>
    public static Node2D Scene(StoryScene scene, ulong seed)
    {
        var n = Background(scene.Background, seed);
        foreach (var (i, placed) in scene.Pictures.Select((p, i) => (i, p)))
        {
            var picture = PictureNode.Make(placed.Picture, placed.Picture.Replace("pic_", ""));
            picture.Position = P(placed.X, placed.Y);
            picture.Scale = new Vector2((float)(placed.Scale * (placed.Flip == true ? -1 : 1)), (float)placed.Scale);
            n.AddChild(picture);
        }
        n.AddChild(Pen(RoundRect(R(-Width / 2, -Height / 2, Width, Height), 28), seed + 99, lineWidth: 7));
        return n;
    }

    /// <summary>A background by name; an unknown one is plain sky over grass.</summary>
    public static Node2D Background(string id, ulong seed)
    {
        var n = new Node2D();
        var s = seed;
        void Add(SketchShape shape, Color? fill = null, Color? ink = null, double width = 4, double wobble = 2.2) =>
            n.AddChild(Pen(shape, s++, fill: fill, ink: ink, lineWidth: width, wobble: wobble));
        void Fill(double top, double bottom, Color colour) => Add(Polygon(Band(top, bottom)), colour, colour.Blend(0.35, Palette.Ink), 3);

        switch (id)
        {
            case "beach":
                Fill(HalfH, -HalfH, Sky);
                PlainSun(Add, 360, 140);
                Fill(-20, -80, Palette.Sea);
                Waves(Add, -40, 3);
                Fill(-70, -HalfH, Palette.Sand);
                break;
            case "sea":
                Fill(HalfH, -HalfH, Sky);
                PlainSun(Add, -360, 140);
                Fill(10, -HalfH, Palette.Sea);
                Waves(Add, -10, 5);
                Waves(Add, -110, 6);
                break;
            case "room":
            case "kitchen":
            case "classroom":
                Fill(HalfH, -HalfH, id == "classroom" ? Palette.Sun.Blend(0.75, Palette.White) : Wall);
                Fill(-90, -HalfH, id == "kitchen" ? Palette.Stone : Floor);
                for (var x = -360.0; x <= 360; x += 120)
                    Add(Polyline(P(x, -92), P(x - 30, -HalfH + 2)), ink: Palette.Brown.WithAlpha(0.5), width: 2.5, wobble: 1);
                if (id == "classroom")
                {
                    Add(RoundRect(R(-200, 30, 520, 160), 10), Palette.Leaf.Blend(0.45, Palette.Ink), Palette.Brown, 7);
                    Add(Polyline(P(-160, 150), P(-40, 140), P(40, 152)), ink: Palette.White, width: 3, wobble: 1.5);
                    Add(Polyline(P(-160, 100), P(0, 96)), ink: Palette.White, width: 3, wobble: 1.5);
                }
                else
                {
                    Window(Add, -320, 70);
                    if (id == "kitchen")
                    {
                        for (var x = -120.0; x <= 460; x += 60)
                            Add(Polyline(P(x, 20), P(x, -88)), ink: Palette.Sea.WithAlpha(0.6), width: 2, wobble: 0.8);
                        Add(Polyline(P(-130, -30), P(470, -30)), ink: Palette.Sea.WithAlpha(0.6), width: 2, wobble: 0.8);
                        Add(Polygon(P(-HalfW, -88), P(HalfW, -88), P(HalfW, -112), P(-HalfW, -112)), Palette.Brown, width: 4);
                    }
                    else
                    {
                        Add(RoundRect(R(120, 60, 150, 110), 6), Palette.Card, Palette.Brown, 6);
                        Add(Polygon(P(140, 80), P(190, 140), P(250, 80)), Palette.Grass, width: 3);
                    }
                }
                break;
            case "rain":
                Fill(HalfH, -HalfH, GreySky);
                Fill(-60, -HalfH, Palette.Grass.Blend(0.25, Palette.Ink));
                var rng = new SeededRandom(seed);
                for (var i = 0; i < 26; i++)
                {
                    var x = rng.Range(-440, 440);
                    var y = rng.Range(-150, 180);
                    Add(Polyline(P(x, y), P(x - 8, y - 26)), ink: Palette.Sea.Blend(0.3, Palette.Ink), width: 3, wobble: 0.5);
                }
                break;
            case "field":
                Fill(HalfH, -HalfH, Sky);
                PlainSun(Add, -380, 150);
                Fill(-10, -HalfH, Palette.Grass);
                Add(Polyline(P(0, -12), P(0, -HalfH + 2)), ink: Palette.White, width: 5, wobble: 1);
                Add(Polyline(P(380, -30), P(380, 60), P(470, 70)), ink: Palette.White, width: 8, wobble: 1);
                Add(Polyline(P(380, 60), P(470, 40)), ink: Palette.White.WithAlpha(0.6), width: 3, wobble: 1);
                break;
            case "market":
                Fill(HalfH, -HalfH, Sky);
                Fill(-40, -HalfH, Palette.Sand);
                foreach (var (i, x) in Indexed(-40.0, 260.0))
                {
                    Add(Polyline(P(x - 120, -80), P(x - 120, 110)), ink: Palette.Brown, width: 6, wobble: 1);
                    Add(Polyline(P(x + 120, -80), P(x + 120, 110)), ink: Palette.Brown, width: 6, wobble: 1);
                    for (var k = 0; k < 4; k++)
                        Add(Polygon(P(x - 130 + k * 65, 110), P(x - 65 + k * 65, 110), P(x - 65 + k * 65, 150), P(x - 130 + k * 65, 150)),
                            k % 2 == 0 ? Palette.Red : Palette.White, width: 3.5);
                    Add(RoundRect(R(x - 130, -100, 260, 30), 6), Palette.LightBrown, width: 4);
                }
                break;
            case "garden":
                Fill(HalfH, -HalfH, Sky);
                PlainSun(Add, 380, 150);
                for (var x = -440.0; x <= 440; x += 55)
                    Add(RoundRect(R(x - 8, -40, 16, 70), 5), Palette.Card, width: 3, wobble: 1);
                Add(Polyline(P(-HalfW, 10), P(HalfW, 10)), ink: Palette.Brown, width: 4, wobble: 1);
                Fill(-30, -HalfH, Palette.Grass);
                Add(Polygon(P(-440, -170), P(440, -170), P(420, -205), P(-420, -205)), Palette.Brown, width: 3.5);
                break;
            case "road":
                Fill(HalfH, -HalfH, Sky);
                PlainSun(Add, 380, 150);
                Fill(-50, -HalfH, Palette.Grass);
                Add(Polygon(Band(-110, -HalfH)), Palette.Stone.Blend(0.35, Palette.Ink), width: 4);
                for (var x = -420.0; x < 440; x += 120)
                    Add(Polyline(P(x, -165), P(x + 60, -165)), ink: Palette.White, width: 6, wobble: 0.8);
                break;
            case "hill":
                Fill(HalfH, -HalfH, Sky);
                PlainSun(Add, -380, 150);
                Add(Polygon(P(-HalfW, -60), P(-200, 60), P(60, 120), P(300, 40), P(HalfW, -20), P(HalfW, -HalfH + Corner), P(HalfW - Corner, -HalfH),
                            P(-HalfW + Corner, -HalfH), P(-HalfW, -HalfH + Corner)), Palette.Grass, width: 4);
                Add(Polyline(P(-420, -200), P(-200, -120), P(0, -100), P(200, -40), P(360, 20)), ink: Palette.Sand.Blend(0.2, Palette.Brown), width: 14, wobble: 2);
                break;
            case "stage":
                Fill(HalfH, -HalfH, Palette.Purple.Blend(0.55, Palette.Ink));
                Fill(-100, -HalfH, Palette.Brown);
                foreach (var side in new[] { -1.0, 1.0 })
                    Add(Polygon(P(side * HalfW, HalfH), P(side * 330, HalfH), P(side * 380, 0), P(side * 420, -100), P(side * HalfW, -100)), Palette.Red, width: 4);
                Add(Polygon(Band(HalfH, 170)), Palette.Red, width: 4);
                break;
            default: // park
                Fill(HalfH, -HalfH, Sky);
                PlainSun(Add, 380, 150);
                Cloud(Add, -250, 140);
                Fill(-40, -HalfH, Palette.Grass);
                Add(Ellipse(P(-400, -40), 70, 40), Palette.Leaf, width: 3.5);
                break;
        }
        if (id is "park" or "beach" or "field" or "road" or "garden" or "hill") Cloud(Add, id == "park" ? 100 : -120, 160);
        return n;
    }

    /// <summary>A strip across the frame between two heights, its corners rounded off where it meets the frame's.</summary>
    private static IEnumerable<Vector2> Band(double top, double bottom)
    {
        var t = Math.Min(top, HalfH);
        var b = Math.Max(bottom, -HalfH);
        var points = new List<Vector2>();
        points.Add(P(-HalfW, t >= HalfH ? t - Corner : t));
        if (t >= HalfH) { points.Add(P(-HalfW + Corner, t)); points.Add(P(HalfW - Corner, t)); }
        points.Add(P(HalfW, t >= HalfH ? t - Corner : t));
        points.Add(P(HalfW, b <= -HalfH ? b + Corner : b));
        if (b <= -HalfH) { points.Add(P(HalfW - Corner, b)); points.Add(P(-HalfW + Corner, b)); }
        points.Add(P(-HalfW, b <= -HalfH ? b + Corner : b));
        return points;
    }

    private static void PlainSun(Action<SketchShape, Color?, Color?, double, double> add, double x, double y) =>
        add(Ellipse(P(x, y), 40, 40), Palette.Sun, Palette.Orange, 4, 2.2);

    private static void Cloud(Action<SketchShape, Color?, Color?, double, double> add, double x, double y)
    {
        add(Ellipse(P(x, y), 70, 26), Palette.White, Palette.Sea.Blend(0.4, Palette.Ink), 3, 1.5);
        add(Ellipse(P(x - 20, y + 18), 34, 24), Palette.White, Palette.Sea.Blend(0.4, Palette.Ink), 3, 1.5);
    }

    private static void Waves(Action<SketchShape, Color?, Color?, double, double> add, double y, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var x = -400 + i * (800.0 / Math.Max(1, count - 1));
            add(Arc(P(x, y), 26, 10, 0.2, Math.PI - 0.2), null, Palette.White, 3, 0.8);
        }
    }

    private static void Window(Action<SketchShape, Color?, Color?, double, double> add, double x, double y)
    {
        add(RoundRect(R(x - 90, y - 70, 180, 140), 8), Sky, Palette.Brown, 6, 1.5);
        add(Polyline(P(x, y - 70), P(x, y + 70)), null, Palette.Brown, 5, 1);
        add(Polyline(P(x - 90, y), P(x + 90, y)), null, Palette.Brown, 5, 1);
    }
}
