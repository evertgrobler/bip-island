using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Games;

/// <summary>
/// Letter Trace: follow the letter with the pointer while sparkles trail behind. Two ways to draw,
/// both always on (<see cref="TracePen"/>): drag with the mouse button held, or click the green
/// start dot and the pen follows the pointer with the button up until the letter is done or the
/// child clicks again (a trackpad, where holding the button down is hard for small hands). Wide
/// checkpoints, not a tight path; tracing is savoured, never gated. One letter per visit.
/// </summary>
public partial class LetterTraceScreen : GameScreen
{
    private const float TouchRadius = 120;

    private readonly LetterTraceGame _game;
    private readonly PhonicsSound _sound;
    private Node2D _letter = null!;
    private Node2D _startDot = null!;
    private Tween? _pulse;
    /// <summary>A loose ring around the letter plus its middle (Mac app coordinates turned into Godot ones).</summary>
    private readonly List<Vector2> _checkpoints =
        [.. Enumerable.Range(0, 8).Select(i => P(170 * Math.Cos(i / 8.0 * Math.Tau), 60 + 170 * Math.Sin(i / 8.0 * Math.Tau))), P(0, 60)];
    private readonly HashSet<int> _touched = [];
    /// <summary>Just above the letter, where most letters start.</summary>
    private static readonly Vector2 StartDotAt = P(0, 290);
    private readonly TracePen _pen = new();
    private string? _pressedOn;
    private bool _started;

    public LetterTraceScreen(LetterTraceGame game, PhonicsSound sound)
    {
        _game = game;
        _sound = sound;
    }

    protected override Island HomeIsland => Island.Letters;
    protected override string GameId => LetterTraceGame.GameId;

    /// <summary>Checkpoints still to reach (for the walk-through test), in canvas coordinates.</summary>
    public IEnumerable<Vector2> CheckpointsOnScreen => _checkpoints.Select(Stage.ToGlobal);
    /// <summary>The start dot's centre in canvas coordinates (for the walk-through test).</summary>
    public Vector2 StartDotOnScreen => Stage.ToGlobal(StartDotAt);
    public bool Started => _started;
    public TracePenMode PenMode => _pen.Mode;
    /// <summary>True while the start dot wiggles for a hint.</summary>
    public bool StartDotHinted => HintedNode == _startDot;
    public int TouchedCount => _touched.Count;
    public bool StartDotShown => _startDot.Visible;

    protected override void Build()
    {
        AddHomeButton();
        AddBip(P(-560, -400), 0.95f);
        _letter = Buttons.Tappable(Sketch.Letter(_sound.Grapheme, 380, shadow: Palette.Sun), "letter");
        _letter.Position = P(0, 60);
        _letter.ZIndex = 10;
        Stage.AddChild(_letter);

        // 120 pt across: the smallest click target allowed.
        _startDot = Buttons.Tappable(Group(
            Pen(Ellipse(Vector2.Zero, TracePen.StartRadius, TracePen.StartRadius), 610, fill: Palette.Go, lineWidth: 5),
            Pen(Polygon(Star(Vector2.Zero, 26)), 611, fill: Palette.White, lineWidth: 3, wobble: 1)), "start");
        _startDot.Position = StartDotAt;
        _startDot.ZIndex = 15;
        Stage.AddChild(_startDot);
        _pulse = Buttons.Pulse(_startDot);

        InputLocked = true;
        After(StartDelay, () =>
        {
            Voice.Play([VoiceLine.TraceLetter, _sound.SoundClip]);
            Bip.Hop();
            InputLocked = false;
            _started = true;
        });
    }

    private bool CanTrace => _started && !InputLocked;

    public override void _UnhandledInput(InputEvent @event)
    {
        // A press on home has already closed this screen by the time its release comes.
        if (!IsInsideTree()) return;
        if (CanTrace)
        {
            switch (@event)
            {
                // With the button held (a drag, once it moves past a click's wobble), or with it up
                // after a click on the start dot. A drag whose release we didn't see (outside the
                // window, behind the parent gate) stops on the first move without the button.
                case InputEventMouseMotion motion:
                    var at = Stage.ToLocal(motion.GlobalPosition);
                    var draws = _pen.Move(Numeric(at), motion.ButtonMask.HasFlag(MouseButtonMask.Left));
                    SyncStartDot();
                    if (draws)
                    {
                        GetViewport().SetInputAsHandled();
                        Trace(at);
                    }
                    return;
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
                {
                    GetViewport().SetInputAsHandled();
                    // One hit test for the pen and the tap alike.
                    var target = TappableAt(press.GlobalPosition);
                    _pressedOn = target is null ? null : Buttons.TapName(target);
                    var stopped = _pen.Press(Numeric(Stage.ToLocal(press.GlobalPosition))) == TraceTapResult.Stopped;
                    if (stopped) Sfx.Play(BipSounds.Effect.Tick);
                    SyncStartDot();
                    // The click that lifts the pen does nothing else, except on Bip and home: they always work.
                    if (!stopped || IsAlwaysOn(_pressedOn)) TapOn(target);
                    return;
                }
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release:
                    GetViewport().SetInputAsHandled();
                    var spot = _pressedOn switch
                    {
                        null => TraceSpot.Paper,
                        "start" => TraceSpot.StartDot,
                        _ => TraceSpot.Button,
                    };
                    var result = _pen.Release(Numeric(Stage.ToLocal(release.GlobalPosition)), spot);
                    _pressedOn = null;
                    SyncStartDot();
                    Tapped(result);
                    return;
            }
        }
        base._UnhandledInput(@event);
    }

    private void Tapped(TraceTapResult result)
    {
        switch (result)
        {
            case TraceTapResult.Started:
                StopHint();
                Sfx.Play(BipSounds.Effect.Pop);
                Trace(StartDotAt);
                break;
            case TraceTapResult.Missed or TraceTapResult.Hint:
                Sfx.Play(BipSounds.Effect.Boop);
                Bip.Tilt();
                if (result != TraceTapResult.Hint) break;
                // Two misses: the start dot wiggles and Bip says "Look for the one that's wiggling!"
                if (!StartDotHinted)
                {
                    _pulse?.Kill();
                    _pulse = null;
                    ShowHint(_startDot);
                }
                After(0.6, () => { if (!InputLocked && StartDotHinted) Voice.Play([Coordinator.RandomHint(followedBySound: false)]); });
                break;
        }
    }

    /// <summary>
    /// Shows the start dot (pulsing) whenever the pen isn't following, however it stopped: a click
    /// lifts the pen, or a drag takes over. What's traced so far stays traced.
    /// </summary>
    private void SyncStartDot()
    {
        var show = _pen.ShowsStartDot;
        if (show == _startDot.Visible) return;
        _startDot.Visible = show;
        if (show)
        {
            if (!StartDotHinted) _pulse ??= Buttons.Pulse(_startDot);
            return;
        }
        StopHint();
        _pulse?.Kill();
        _pulse = null;
        _startDot.Scale = Vector2.One;
    }

    private void Trace(Vector2 point)
    {
        if (!CanTrace) return;
        Color[] colours = [Palette.Sun, Palette.Pink, Palette.Teal, Palette.Purple];
        var dot = new Disc(8 + Coordinator.Rng.NextIndex(7), colours[Coordinator.Rng.NextIndex(colours.Length)]) { Position = point, ZIndex = 30 };
        Stage.AddChild(dot);
        var fade = dot.CreateTween();
        fade.TweenProperty(dot, "modulate:a", 0f, 0.7);
        fade.TweenCallback(Callable.From(dot.QueueFree));

        for (var i = 0; i < _checkpoints.Count; i++)
        {
            if (_touched.Contains(i) || point.DistanceTo(_checkpoints[i]) >= TouchRadius) continue;
            _touched.Add(i);
            Sfx.Play(BipSounds.Effect.Tick);
        }
        if (_touched.Count == _checkpoints.Count) FinishTrace();
    }

    private void FinishTrace()
    {
        InputLocked = true;
        _pen.Finish();
        SyncStartDot();
        var grow = _letter.CreateTween();
        grow.TweenProperty(_letter, "scale", Vector2.One * 1.12f, 0.15);
        grow.TweenProperty(_letter, "scale", Vector2.One, 0.15);
        Buttons.Sparkle(P(0, 60), Stage);
        Sfx.Play(BipSounds.Effect.Chime);
        Bip.Celebrate();
        Record(true, _game.SkillId(new LetterTraceGame.Round(_sound)), _sound.Id);
        // One trace is the whole game: the end card shows the star and any new picture stickers.
        Voice.Play([Coordinator.RandomPraise(), _sound.SoundClip],
                   completion: () => FinishVisit(() => Coordinator.ShowIsland(Island.Letters, greet: false)));
    }

    protected override void HandleTap(string name, Node2D node)
    {
        if (name == "letter") Voice.Play([_sound.SoundClip]);
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) Voice.Play([VoiceLine.TraceLetter, _sound.SoundClip]);
    }

    private static System.Numerics.Vector2 Numeric(Vector2 point) => new(point.X, point.Y);
}
