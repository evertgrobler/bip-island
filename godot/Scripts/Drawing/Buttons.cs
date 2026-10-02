using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// Big, round, picture-only buttons (no reading needed); every one is at least 120 pt on screen.
/// A port of the Swift app's Buttons with the same shapes and seeds. Each button carries its tap name
/// as metadata (<see cref="TapMeta"/>: "home", "replay", "next", "play"), the Godot stand-in for the
/// Swift node names "tap:home" and so on (Godot node names can't contain a colon).
/// </summary>
public static class Buttons
{
    /// <summary>The metadata key that holds a tappable node's name.</summary>
    public const string TapMeta = "tap";

    /// <summary>Marks a node as tappable under a name (Swift: node.name = "tap:&lt;name&gt;").</summary>
    public static T Tappable<T>(T node, string name) where T : Node
    {
        node.SetMeta(TapMeta, name);
        node.Name = name;
        return node;
    }

    /// <summary>The tap name of a node, or null if it isn't tappable.</summary>
    public static string? TapName(Node node) => node.HasMeta(TapMeta) ? node.GetMeta(TapMeta).AsString() : null;

    /// <summary>Back home: a little house.</summary>
    public static Node2D Home() => Tappable(Group(
        Pen(Ellipse(Vector2.Zero, 72, 72), 500, fill: Palette.Sun),
        Pen(Polygon(P(-34, -30), P(34, -30), P(34, 8), P(-34, 8)), 501, fill: Palette.Card, lineWidth: 4.5),
        Pen(Polygon(P(-46, 6), P(0, 44), P(46, 6)), 502, fill: Palette.Red, lineWidth: 4.5),
        Pen(Polygon(P(-10, -30), P(10, -30), P(10, -4), P(-10, -4)), 503, fill: Palette.Brown, lineWidth: 3.5)), "home");

    /// <summary>Hear it again: a speaker with sound waves.</summary>
    public static Node2D Replay()
    {
        var n = Group(
            Pen(Ellipse(Vector2.Zero, 72, 72), 510, fill: Palette.LightTeal),
            Pen(Polygon(P(-36, -14), P(-18, -14), P(4, -34), P(4, 34), P(-18, 14), P(-36, 14)), 511, fill: Palette.Ink, lineWidth: 4));
        foreach (var (i, r) in Indexed(20.0, 34.0))
            n.AddChild(Pen(Arc(P(6, 0), r, r * 1.1, -0.9, 0.9), (ulong)(512 + i), lineWidth: 5));
        return Tappable(n, "replay");
    }

    /// <summary>Carry on: a green circle with an arrow.</summary>
    public static Node2D Next() => Tappable(Group(
        Pen(Ellipse(Vector2.Zero, 88, 88), 520, fill: Palette.Go),
        Pen(Polygon(P(-40, -14), P(6, -14), P(6, -40), P(48, 0), P(6, 40), P(6, 14), P(-40, 14)), 521, fill: Palette.White, lineWidth: 4.5)), "next");

    /// <summary>Play: a green circle with a triangle.</summary>
    public static Node2D Play() => Tappable(Group(
        Pen(Ellipse(Vector2.Zero, 100, 100), 530, fill: Palette.Go, lineWidth: 6),
        Pen(Polygon(P(-30, -46), P(52, 0), P(-30, 46)), 531, fill: Palette.White, lineWidth: 5)), "play");

    /// <summary>Gentle "press" bounce.</summary>
    public static void Press(Node2D node)
    {
        var tween = node.CreateTween();
        tween.TweenProperty(node, "scale", Vector2.One * 0.9f, 0.06);
        tween.TweenProperty(node, "scale", Vector2.One * 1.05f, 0.1);
        tween.TweenProperty(node, "scale", Vector2.One, 0.08);
    }

    /// <summary>Slow pulse to say "click me", until the returned tween is killed.</summary>
    public static Tween Pulse(Node2D node)
    {
        var tween = node.CreateTween().SetLoops();
        tween.TweenProperty(node, "scale", Vector2.One * 1.07f, 0.7).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(node, "scale", Vector2.One, 0.7).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        return tween;
    }

    /// <summary>Shake for a wrong answer.</summary>
    public static void Shake(Node2D node)
    {
        var tween = node.CreateTween();
        tween.TweenProperty(node, "rotation", Turn(0.08), 0.05);
        tween.TweenProperty(node, "rotation", Turn(-0.08), 0.08);
        tween.TweenProperty(node, "rotation", Turn(0.05), 0.07);
        tween.TweenProperty(node, "rotation", 0f, 0.06);
    }

    /// <summary>Wiggle for a hint ("look for the one that's wiggling!"), until the returned tween is killed.</summary>
    public static Tween HintWiggle(Node2D node)
    {
        var tween = node.CreateTween().SetLoops();
        tween.TweenProperty(node, "rotation", Turn(0.1), 0.18);
        tween.Parallel().TweenProperty(node, "scale", Vector2.One * 1.08f, 0.18);
        tween.TweenProperty(node, "rotation", Turn(-0.1), 0.18);
        tween.Parallel().TweenProperty(node, "scale", Vector2.One, 0.18);
        tween.TweenProperty(node, "rotation", 0f, 0.1);
        tween.TweenInterval(0.4);
        return tween;
    }

    /// <summary>A burst of little paper confetti dots from a point in the parent.</summary>
    public static void Sparkle(Vector2 at, Node2D parent)
    {
        Color[] colours = { Palette.Sun, Palette.Red, Palette.Teal, Palette.Purple, Palette.Orange, Palette.Go };
        for (var i = 0; i < 12; i++)
        {
            var dot = new Disc((float)GD.RandRange(6.0, 11.0), colours[i % colours.Length], Palette.Ink, 2)
            {
                Position = at,
                ZIndex = 50,
            };
            parent.AddChild(dot);
            var angle = (float)(i / 12.0 * Mathf.Tau + GD.RandRange(-0.2, 0.2));
            var distance = (float)GD.RandRange(90.0, 170.0);
            var tween = dot.CreateTween().SetParallel();
            tween.TweenProperty(dot, "position", at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance, 0.5)
                 .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(dot, "modulate:a", 0f, 0.6);
            tween.TweenProperty(dot, "scale", Vector2.One * 0.4f, 0.6);
            tween.Chain().TweenCallback(Callable.From(dot.QueueFree));
        }
    }
}
