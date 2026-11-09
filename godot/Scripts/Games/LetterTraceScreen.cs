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
/// child clicks again (a trackpad, where holding the button down is hard for small hands). The
/// checkpoints sit on the letter's own strokes (<see cref="TraceCheckpoints"/>, from
/// Content/phonics/trace_shapes.json): any order, as many lifts as needed (the dot on i and j), back over
/// a stroke, and most of them is enough. Tracing is savoured, never gated. One letter per visit.
/// </summary>
public partial class LetterTraceScreen : GameScreen
{
    /// <summary>How near the pen must come to a checkpoint: about a stroke's width, so drawing beside the letter still counts.</summary>
    private const float TouchRadius = 60;
    /// <summary>The letter's font size (and so one em of trace_shapes.json).</summary>
    private const int LetterSize = 380;

    private readonly LetterTraceGame _game;
    private readonly PhonicsSound _sound;
    private Node2D _letter = null!;
    private Node2D _startDot = null!;
    private Tween? _pulse;
    private TraceCheckpoints _checkpoints = null!;
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
    public IEnumerable<Vector2> CheckpointsOnScreen => _checkpoints.Remaining.Select(p => Stage.ToGlobal(new Vector2(p.X, p.Y)));
    /// <summary>The start dot's centre in canvas coordinates (for the walk-through test).</summary>
    public Vector2 StartDotOnScreen => Stage.ToGlobal(StartDotAt);
    public bool Started => _started;
    public TracePenMode PenMode => _pen.Mode;
    /// <summary>True while the start dot wiggles for a hint.</summary>
    public bool StartDotHinted => HintedNode == _startDot;
    public int TouchedCount => _checkpoints.Touched;
    public bool StartDotShown => _startDot.Visible;

    protected override void Build()
    {
        AddHomeButton();
        AddBip(P(-560, -400), 0.95f);
        _letter = Buttons.Tappable(Sketch.Letter(_sound.Grapheme, LetterSize, shadow: Palette.Sun), "letter");
        _letter.Position = P(0, 60);
        _letter.ZIndex = 10;
        Stage.AddChild(_letter);
        _checkpoints = new TraceCheckpoints(Shape(), Numeric(_letter.Position), LetterSize, TouchRadius);

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
                        Trace(at, _pen.StrokeFrom is { } from ? new Vector2(from.X, from.Y) : null);
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
                    var where = Stage.ToLocal(release.GlobalPosition);
                    // A click on a stroke traces it (dotting the i or the j with one click): never a miss.
                    var dotted = _pressedOn is null or "letter" && _pen.Mode == TracePenMode.Idle && _pen.IsClick(Numeric(where))
                                 && _checkpoints.Touch(Numeric(where)) > 0;
                    var spot = _pressedOn switch
                    {
                        _ when dotted => TraceSpot.Button,
                        null => TraceSpot.Paper,
                        "start" => TraceSpot.StartDot,
                        _ => TraceSpot.Button,
                    };
                    var result = _pen.Release(Numeric(where), spot);
                    _pressedOn = null;
                    SyncStartDot();
                    if (dotted) Traced(1);
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
                Trace(StartDotAt, null);
                break;
            case TraceTapResult.Missed or TraceTapResult.Hint:
                Sfx.Play(BipSounds.Effect.Boop);
                Bip.Tilt();
                if (result != TraceTapResult.Hint || StartDotHinted) break;
                // Two misses: the start dot wiggles and Bip says "Look for the one that's wiggling!",
                // once (later misses just boop while it wiggles).
                _pulse?.Kill();
                _pulse = null;
                ShowHint(_startDot);
                After(0.6, SayHintWhenQuiet);
                break;
        }
    }

    /// <summary>
    /// The hint, but never over the narrator: it waits for the opening "Trace the letter…" (or the
    /// letter's sound) to finish, and is dropped if the dot stopped wiggling meanwhile.
    /// </summary>
    private void SayHintWhenQuiet()
    {
        if (InputLocked || !StartDotHinted) return;
        if (Voice.Speaking)
        {
            After(0.3, SayHintWhenQuiet);
            return;
        }
        Voice.Play([Coordinator.RandomHint(followedBySound: false)]);
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

    private void Trace(Vector2 point, Vector2? from)
    {
        if (!CanTrace) return;
        Color[] colours = [Palette.Sun, Palette.Pink, Palette.Teal, Palette.Purple];
        var dot = new Disc(8 + Coordinator.Rng.NextIndex(7), colours[Coordinator.Rng.NextIndex(colours.Length)]) { Position = point, ZIndex = 30 };
        Stage.AddChild(dot);
        var fade = dot.CreateTween();
        fade.TweenProperty(dot, "modulate:a", 0f, 0.7);
        fade.TweenCallback(Callable.From(dot.QueueFree));

        Traced(from is { } start ? _checkpoints.Stroke(Numeric(start), Numeric(point)) : _checkpoints.Touch(Numeric(point)));
    }

    /// <summary>Checkpoints were reached: a tick, and the end once the letter is traced.</summary>
    private void Traced(int fresh)
    {
        if (fresh > 0) Sfx.Play(BipSounds.Effect.Tick);
        if (_checkpoints.Done) FinishTrace();
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

    /// <summary>
    /// This grapheme's checkpoints from trace_shapes.json. A grapheme the file doesn't have yet (new content before
    /// scripts/trace_shapes.py ran; the validator catches it) gets one checkpoint in the letter's middle: never gated.
    /// </summary>
    private List<List<float>> Shape()
    {
        if (Coordinator.Content?.TraceShapes.Shapes.TryGetValue(_sound.Grapheme, out var shape) == true && shape.Count > 0)
            return shape;
        return [[0f, 0f]];
    }
}
