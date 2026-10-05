using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// Word Rocket's drawings: the three craft (a rocket, a hot-air balloon and a submarine) built round a
/// row of letter windows, a window, and a picture of the keyboard whose keys can glow for a hint.
/// Everything is drawn round (0, 0) with y up through <see cref="Up"/>, in the game's wobbly ink.
/// </summary>
public static class RocketDrawing
{
    /// <summary>Where the windows go for <paramref name="count"/> letters, and how big each one is.</summary>
    public static (List<Vector2> Centres, double Radius) WindowLayout(int count)
    {
        var spacing = Math.Min(118, 880.0 / Math.Max(count, 1));
        var radius = Math.Min(50, spacing * 0.42);
        var centres = Enumerable.Range(0, count).Select(i => P((i - (count - 1) / 2.0) * spacing, 0)).ToList();
        return (centres, radius);
    }

    /// <summary>
    /// The craft for a skin ("rocket", "balloon" or "submarine") round windows that reach
    /// <paramref name="halfWidth"/> either side of the middle. The node named "Exhaust" (flame or
    /// bubbles) is hidden until it launches.
    /// </summary>
    public static Node2D Craft(string skin, double halfWidth) => skin switch
    {
        "balloon" => Balloon(halfWidth),
        "submarine" => Submarine(halfWidth),
        _ => Rocket(halfWidth),
    };

    private static Node2D Rocket(double w)
    {
        var craft = new Node2D { Name = "Rocket" };
        var exhaust = new Node2D { Name = "Exhaust", Visible = false };
        exhaust.AddChild(Pen(Polygon(P(-w - 118, 58), P(-w - 270, 0), P(-w - 118, -58)), 1201, fill: Palette.Orange, lineWidth: 4));
        exhaust.AddChild(Pen(Polygon(P(-w - 118, 30), P(-w - 205, 0), P(-w - 118, -30)), 1202, fill: Palette.Sun, lineWidth: 3));
        craft.AddChild(exhaust);
        // Fins and the nose sit behind the body, so the joins are hidden.
        foreach (var (i, side) in Indexed(1.0, -1.0))
            craft.AddChild(Pen(Polygon(P(-w - 10, 78 * side), P(-w - 140, 192 * side), P(-w - 188, 192 * side), P(-w - 150, 78 * side)),
                               (ulong)(1203 + i), fill: Palette.Red, lineWidth: 5));
        craft.AddChild(Pen(Polygon(P(-w - 70, 50), P(-w - 122, 68), P(-w - 122, -68), P(-w - 70, -50)), 1205, fill: Palette.Stone, lineWidth: 5));
        craft.AddChild(Pen(Polygon(P(w + 50, 94), P(w + 150, 72), P(w + 232, 0), P(w + 150, -72), P(w + 50, -94)), 1206,
                           fill: Palette.Red, lineWidth: 5));
        craft.AddChild(Pen(RoundRect(R(-w - 80, -98, 2 * w + 170, 196), 90), 1207, fill: Palette.Card, lineWidth: 6));
        // A stripe along the body, above and below the windows.
        foreach (var (i, y) in Indexed(-70.0, 70.0))
            craft.AddChild(Pen(Polyline(P(-w - 30, y), P(w + 30, y)), (ulong)(1208 + i), ink: Palette.Teal, lineWidth: 6));
        return craft;
    }

    private static Node2D Balloon(double w)
    {
        var craft = new Node2D { Name = "Balloon" };
        var rx = Math.Max(w * 0.8, 210);
        foreach (var (i, x) in Indexed(-1.0, 1.0))
            craft.AddChild(Pen(Polyline(P(x * (w + 10), 70), P(x * rx * 0.3, 128)), (ulong)(1220 + i), lineWidth: 4));
        var exhaust = new Node2D { Name = "Exhaust", Visible = false };
        exhaust.AddChild(Pen(Polygon(P(-24, 76), P(0, 132), P(24, 76)), 1222, fill: Palette.Orange, lineWidth: 4));
        exhaust.AddChild(Pen(Polygon(P(-11, 78), P(0, 108), P(11, 78)), 1223, fill: Palette.Sun, lineWidth: 3));
        craft.AddChild(exhaust);
        // The envelope: round on top, narrowing to the skirt, with sunny and red panels.
        foreach (var (i, (squeeze, colour)) in Indexed((1.0, Palette.Red), (0.6, Palette.Sun), (0.2, Palette.Red)))
            craft.AddChild(Pen(Polygon(Envelope(rx * squeeze, P(0, 250), 110, 100, 0.3 / squeeze)), (ulong)(1224 + i), fill: colour, lineWidth: 6 - i));
        craft.AddChild(Pen(Polygon(P(-rx * 0.32, 156), P(rx * 0.32, 156), P(rx * 0.27, 124), P(-rx * 0.27, 124)), 1227, fill: Palette.Brown, lineWidth: 4));
        // The basket holds the windows; two lines of weave.
        craft.AddChild(Pen(RoundRect(R(-w - 40, -74, 2 * w + 80, 148), 26), 1228, fill: Palette.LightBrown, ink: Palette.Brown, lineWidth: 6));
        foreach (var (i, y) in Indexed(-58.0, 58.0))
            craft.AddChild(Pen(Polyline(P(-w - 26, y), P(w + 26, y)), (ulong)(1229 + i), ink: Palette.Brown, lineWidth: 4));
        return craft;
    }

    /// <summary>
    /// A balloon outline <paramref name="rx"/> wide each side: a half oval <paramref name="top"/> high
    /// above <paramref name="centre"/>, curving in to a skirt <paramref name="neck"/> times as wide,
    /// <paramref name="bottom"/> below.
    /// </summary>
    private static IEnumerable<Vector2> Envelope(double rx, Vector2 centre, double top, double bottom, double neck)
    {
        neck = Math.Min(neck, 1);
        for (var i = 0; i <= 16; i++)
        {
            var a = Math.PI * i / 16;
            yield return centre + P(rx * Math.Cos(a), top * Math.Sin(a));
        }
        for (var i = 1; i < 12; i++)
        {
            var t = i / 12.0;
            var width = rx * (1 - (1 - neck) * t * t);
            yield return centre + P(-width, -bottom * t);
        }
        yield return centre + P(-rx * neck, -bottom);
        yield return centre + P(rx * neck, -bottom);
        for (var i = 11; i >= 1; i--)
        {
            var t = i / 12.0;
            var width = rx * (1 - (1 - neck) * t * t);
            yield return centre + P(width, -bottom * t);
        }
    }

    private static Node2D Submarine(double w)
    {
        var craft = new Node2D { Name = "Submarine" };
        var exhaust = new Node2D { Name = "Exhaust", Visible = false };
        foreach (var (i, (x, y, r)) in Indexed((-w - 170.0, 20.0, 16.0), (-w - 215.0, -14.0, 11.0), (-w - 250.0, 30.0, 8.0), (-w - 200.0, 55.0, 6.0)))
            exhaust.AddChild(Pen(Ellipse(P(x, y), r, r), (ulong)(1240 + i), fill: Palette.Bubble, ink: Palette.Teal, lineWidth: 3));
        craft.AddChild(exhaust);
        // The tower and periscope, then the propeller, behind the body.
        craft.AddChild(Pen(Polyline(P(45, 150), P(45, 222), P(92, 222)), 1245, ink: Palette.Ink, lineWidth: 10));
        craft.AddChild(Pen(RoundRect(R(-95, 70, 190, 104), 22), 1246, fill: Palette.Sun, lineWidth: 6));
        foreach (var (i, y) in Indexed(38.0, -38.0))
            craft.AddChild(Pen(Ellipse(P(-w - 118, y), 16, 36), (ulong)(1247 + i), fill: Palette.Orange, lineWidth: 4));
        craft.AddChild(Pen(Ellipse(P(-w - 112, 0), 18, 18), 1249, fill: Palette.Stone, lineWidth: 4));
        craft.AddChild(Pen(RoundRect(R(-w - 100, -100, 2 * w + 200, 200), 100), 1250, fill: Palette.Sun, lineWidth: 6));
        foreach (var (i, x) in Indexed(-w - 55, w + 55))
            craft.AddChild(Pen(Ellipse(P(x, 0), 12, 12), (ulong)(1251 + i), fill: Palette.Orange, lineWidth: 3));
        return craft;
    }
}

/// <summary>One window of the craft: empty, or holding a typed letter in big Atkinson Hyperlegible.</summary>
public partial class RocketWindow : Node2D
{
    private readonly double _radius;
    private readonly Node2D _ring;
    private Node2D? _letter;

    /// <summary>The letter in the window, or null while it waits.</summary>
    public string? Letter { get; private set; }

    public RocketWindow() : this(50, 0) { }

    public RocketWindow(double radius, ulong seed)
    {
        _radius = radius;
        AddChild(Pen(Ellipse(Vector2.Zero, radius, radius), 1260 + seed, fill: Palette.Ice, lineWidth: 5));
        _ring = Pen(Ellipse(Vector2.Zero, radius + 11, radius + 11), 1280 + seed, ink: Palette.Orange, lineWidth: 6);
        _ring.Visible = false;
        AddChild(_ring);
    }

    /// <summary>Shows a letter (lower case), or empties the window with null.</summary>
    public void Show(string? letter)
    {
        _letter?.QueueFree();
        _letter = null;
        Letter = letter;
        if (letter == null) return;
        _letter = Sketch.Letter(letter, (int)Math.Round(_radius * 1.6), Palette.Ink, Palette.Sun);
        _letter.Position = P(0, _radius * 0.06);
        AddChild(_letter);
        var pop = _letter.CreateTween();
        _letter.Scale = Vector2.One * 0.4f;
        pop.TweenProperty(_letter, "scale", Vector2.One * 1.15f, 0.1);
        pop.TweenProperty(_letter, "scale", Vector2.One, 0.08);
    }

    /// <summary>The orange ring round the window the next letter goes in.</summary>
    public bool IsNext
    {
        get => _ring.Visible;
        set => _ring.Visible = value;
    }
}

/// <summary>
/// A picture of the keyboard (QWERTY, lower case, with Backspace). It is only a picture: the real
/// keyboard types, so the keys aren't click targets. A key can bounce when pressed, and glow for a hint.
/// </summary>
public partial class KeyboardPicture : Node2D
{
    public const string Backspace = "backspace";
    private static readonly string[] Rows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"];
    private const double Step = 96;
    private readonly Dictionary<string, Node2D> _keys = [];
    private readonly Dictionary<string, Node2D> _glows = [];
    private Tween? _glowTween;

    /// <summary>The key glowing for a hint, if any.</summary>
    public string? Glowing { get; private set; }

    public KeyboardPicture()
    {
        double[] starts = [-432, -408, -360];
        for (var row = 0; row < Rows.Length; row++)
        {
            for (var i = 0; i < Rows[row].Length; i++)
            {
                var letter = Rows[row][i].ToString();
                AddKey(letter, P(starts[row] + i * Step, -row * 100), 84, (ulong)(row * 10 + i));
            }
        }
        AddKey(Backspace, P(-360 + 7 * Step + 42, -200), 168, 40);
    }

    private void AddKey(string name, Vector2 at, double width, ulong seed)
    {
        var glow = Pen(RoundRect(R(-width / 2 - 12, -54, width + 24, 108), 26), 1300 + seed, fill: Palette.Sun, ink: Palette.Orange, lineWidth: 5);
        glow.Position = at;
        glow.Visible = false;
        AddChild(glow);
        _glows[name] = glow;
        var key = new Node2D { Position = at };
        key.AddChild(Pen(RoundRect(R(-width / 2, -42, width, 84), 16), 1340 + seed, fill: Palette.Card, lineWidth: 4));
        if (name == Backspace)
            key.AddChild(Pen(Polygon(P(-46, 0), P(-14, 22), P(-14, 9), P(40, 9), P(40, -9), P(-14, -9), P(-14, -22)), 1390,
                             fill: Palette.Ink, lineWidth: 3));
        else
            key.AddChild(Sketch.Label(name, 46, Palette.Ink));
        AddChild(key);
        _keys[name] = key;
    }

    /// <summary>A quick bounce on a key that was pressed.</summary>
    public void Press(string key)
    {
        if (!_keys.TryGetValue(key, out var node)) return;
        var tween = node.CreateTween();
        tween.TweenProperty(node, "scale", Vector2.One * 0.85f, 0.05);
        tween.TweenProperty(node, "scale", Vector2.One, 0.1);
    }

    /// <summary>The hint: one key glows and pulses until <see cref="StopGlow"/>.</summary>
    public void Glow(string key)
    {
        StopGlow();
        if (!_glows.TryGetValue(key, out var glow)) return;
        Glowing = key;
        glow.Visible = true;
        glow.Scale = Vector2.One;
        _glowTween = Buttons.Pulse(glow);
    }

    public void StopGlow()
    {
        _glowTween?.Kill();
        _glowTween = null;
        if (Glowing != null && _glows.TryGetValue(Glowing, out var glow)) glow.Visible = false;
        Glowing = null;
    }
}
