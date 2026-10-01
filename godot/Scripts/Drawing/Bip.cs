using System.Collections.Generic;
using Godot;

namespace BipIsland.Drawing;

/// <summary>
/// Bip: a small, friendly hand-drawn robot. The origin is between his wheels; he stands upwards
/// (negative y). A port of the Swift app's BipNode with the same shapes, colours and seeds.
/// </summary>
public partial class Bip : Node2D
{
    private readonly Node2D _head = new();
    private readonly Node2D _antenna = new();
    private readonly Node2D _leftArm = new();
    private readonly Node2D _rightArm = new();
    private readonly List<Node2D> _eyes = new();
    private readonly Node2D _chestLight = new();
    private ulong _seed;
    private Tween? _hop;

    public Bip() : this(40) { }

    public Bip(ulong seed)
    {
        Name = "Bip";
        _seed = seed;
        Build();
    }

    public override void _Ready() => StartIdle();

    /// <summary>The Swift drawing is y-up; this flips a point into Godot's y-down space.</summary>
    private static Vector2 Up(double x, double y) => new((float)x, (float)-y);

    private static Rect2 UpRect(double x, double y, double w, double h) =>
        new((float)x, (float)-(y + h), (float)w, (float)h);

    private ulong Next() => _seed += 13;

    private void Build()
    {
        // Wheels
        foreach (var x in new[] { -42.0, 42.0 })
        {
            AddChild(Sketch.Node(new SketchShape.Ellipse(Up(x, 18), 28, 20), Next(), fill: new Color(Palette.Ink, 0.85f)));
            AddChild(Sketch.Node(new SketchShape.Ellipse(Up(x, 18), 9, 7), Next(), fill: Palette.Stone, lineWidth: 3));
        }

        // Arms (behind the body)
        foreach (var (arm, side) in new[] { (_leftArm, -1.0), (_rightArm, 1.0) })
        {
            arm.Position = Up(62 * side, 100);
            arm.AddChild(Sketch.Node(new SketchShape.Polyline(new[] { Vector2.Zero, Up(32 * side, -24), Up(40 * side, -50) }), Next(), lineWidth: 7));
            arm.AddChild(Sketch.Node(new SketchShape.Ellipse(Up(40 * side, -56), 12, 12), Next(), fill: Palette.Orange, lineWidth: 4));
            AddChild(arm);
        }

        // Body
        AddChild(Sketch.Node(new SketchShape.RoundedRect(UpRect(-66, 28, 132, 112), 26), Next(), fill: Palette.Teal));
        _chestLight.Position = Up(0, 92);
        _chestLight.AddChild(new Disc(13, Palette.Sun, Palette.Ink, 3));
        AddChild(_chestLight);
        Color[] buttons = { Palette.Red, Palette.Sun, Palette.Go };
        for (var i = 0; i < 3; i++)
            AddChild(Sketch.Node(new SketchShape.Ellipse(Up(-24.0 + i * 24, 54), 6, 6), Next(), fill: buttons[i], lineWidth: 2.5f));

        // Neck and head
        AddChild(Sketch.Node(new SketchShape.RoundedRect(UpRect(-14, 136, 28, 18), 4), Next(), fill: Palette.Stone, lineWidth: 4));
        _head.Position = Up(0, 150);
        AddChild(_head);

        _antenna.Position = Up(0, 104);
        _antenna.AddChild(Sketch.Node(new SketchShape.Polyline(new[] { Vector2.Zero, Up(2, 44) }), Next(), lineWidth: 5));
        _antenna.AddChild(Sketch.Node(new SketchShape.Ellipse(Up(2, 54), 13, 13), Next(), fill: Palette.Red, lineWidth: 4));
        _head.AddChild(_antenna);

        _head.AddChild(Sketch.Node(new SketchShape.RoundedRect(UpRect(-82, 0, 164, 108), 30), Next(), fill: Palette.LightTeal));

        foreach (var x in new[] { -34.0, 34.0 })
        {
            var eye = new Node2D { Position = Up(x, 60) };
            eye.AddChild(Sketch.Node(new SketchShape.Ellipse(Vector2.Zero, 22, 25), Next(), fill: Palette.White, lineWidth: 4));
            eye.AddChild(new Disc(10, Palette.Ink) { Position = Up(3, -3) });
            eye.AddChild(new Disc(3.5f, Palette.White) { Position = Up(7, 2) });
            _head.AddChild(eye);
            _eyes.Add(eye);
        }
        foreach (var x in new[] { -58.0, 58.0 })
            _head.AddChild(new Disc(10, new Color(Palette.Pink, 0.8f)) { Position = Up(x, 30), Scale = new Vector2(1, 0.65f) });

        // Smile: the bottom of an ellipse (Swift: 1.15π to 1.85π, y-up; angles negate when y flips).
        _head.AddChild(Sketch.Node(new SketchShape.Arc(Up(0, 32), 22, 14, -Mathf.Pi * 1.15f, -Mathf.Pi * 1.85f), Next(), lineWidth: 4.5f));
    }

    private void StartIdle()
    {
        foreach (var eye in _eyes)
        {
            var blink = CreateTween().SetLoops();
            blink.TweenInterval(GD.RandRange(1.5, 3.5));
            blink.TweenProperty(eye, "scale:y", 0.1f, 0.07);
            blink.TweenProperty(eye, "scale:y", 1f, 0.09);
        }

        var headStart = _head.Position;
        var bob = CreateTween().SetLoops().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        bob.TweenProperty(_head, "position", headStart + new Vector2(0, -6), 0.9);
        bob.TweenProperty(_head, "position", headStart, 0.9);

        var wiggle = CreateTween().SetLoops();
        wiggle.TweenProperty(_antenna, "rotation", -0.12f, 0.7);
        wiggle.TweenProperty(_antenna, "rotation", 0.12f, 0.7);

        var glow = CreateTween().SetLoops();
        glow.TweenProperty(_chestLight, "modulate:a", 0.5f, 0.8);
        glow.TweenProperty(_chestLight, "modulate:a", 1f, 0.8);
    }

    /// <summary>A happy little hop (when clicked or beeping).</summary>
    public void Hop()
    {
        if (_hop != null && _hop.IsRunning()) return;
        var start = Position;
        _hop = CreateTween();
        _hop.TweenProperty(this, "position", start + new Vector2(0, -30), 0.14).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _hop.TweenProperty(this, "position", start, 0.16).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        var antenna = CreateTween();
        antenna.TweenProperty(_antenna, "rotation", -0.4f, 0.08);
        antenna.TweenProperty(_antenna, "rotation", 0.3f, 0.1);
        antenna.TweenProperty(_antenna, "rotation", 0f, 0.1);
    }

    /// <summary>Arms up, spin and hop: for right answers.</summary>
    public void Celebrate()
    {
        Hop();
        foreach (var (arm, side) in new[] { (_leftArm, 1f), (_rightArm, -1f) })
        {
            var wave = CreateTween();
            for (var i = 0; i < 2; i++)
            {
                wave.TweenProperty(arm, "rotation", -0.9f * side, 0.15);
                wave.TweenProperty(arm, "rotation", 0f, 0.25);
            }
        }
        var light = CreateTween();
        light.TweenProperty(_chestLight, "scale", new Vector2(1.6f, 1.6f), 0.15);
        light.TweenProperty(_chestLight, "scale", Vector2.One, 0.2);
    }

    /// <summary>A small sympathetic head tilt: for wrong answers. Never sad.</summary>
    public void Tilt()
    {
        var tilt = CreateTween();
        tilt.TweenProperty(_head, "rotation", -0.15f, 0.15);
        tilt.TweenInterval(0.3);
        tilt.TweenProperty(_head, "rotation", 0f, 0.2);
    }

    /// <summary>True if a point (in Bip's own space) is on him; used for clicks.</summary>
    public bool Contains(Vector2 local) => new Rect2(-100, -280, 200, 290).HasPoint(local);
}

/// <summary>A plain filled circle with an optional outline (pupils, cheeks, the chest light).</summary>
public partial class Disc : Node2D
{
    private readonly float _radius;
    private readonly Color _fill;
    private readonly Color? _outline;
    private readonly float _lineWidth;

    public Disc() { }

    public Disc(float radius, Color fill, Color? outline = null, float lineWidth = 0)
    {
        _radius = radius;
        _fill = fill;
        _outline = outline;
        _lineWidth = lineWidth;
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, _radius, _fill, antialiased: true);
        if (_outline is { } ink && _lineWidth > 0)
            DrawArc(Vector2.Zero, _radius, 0, Mathf.Tau, 48, ink, _lineWidth, antialiased: true);
    }
}
