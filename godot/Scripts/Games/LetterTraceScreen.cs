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
/// Letter Trace: follow the letter with the mouse (button held) while sparkles trail behind. Wide
/// checkpoints, not a tight path, so a mouse works for small hands; tracing is savoured, never
/// gated. One letter per visit.
/// </summary>
public partial class LetterTraceScreen : GameScreen
{
    private const float TouchRadius = 120;

    private readonly LetterTraceGame _game;
    private readonly PhonicsSound _sound;
    private Node2D _letter = null!;
    /// <summary>A loose ring around the letter plus its middle (Mac app coordinates turned into Godot ones).</summary>
    private readonly List<Vector2> _checkpoints =
        [.. Enumerable.Range(0, 8).Select(i => P(170 * Math.Cos(i / 8.0 * Math.Tau), 60 + 170 * Math.Sin(i / 8.0 * Math.Tau))), P(0, 60)];
    private readonly HashSet<int> _touched = [];
    private bool _started;
    private bool _drawing;

    public LetterTraceScreen(LetterTraceGame game, PhonicsSound sound)
    {
        _game = game;
        _sound = sound;
    }

    protected override Island HomeIsland => Island.Letters;
    protected override string GameId => LetterTraceGame.GameId;

    /// <summary>Checkpoints still to reach (for the walk-through test), in canvas coordinates.</summary>
    public IEnumerable<Vector2> CheckpointsOnScreen => _checkpoints.Select(Stage.ToGlobal);
    public bool Started => _started;

    protected override void Build()
    {
        AddHomeButton();
        AddBip(P(-560, -400), 0.95f);
        _letter = Buttons.Tappable(Sketch.Letter(_sound.Grapheme, 380, shadow: Palette.Sun), "letter");
        _letter.Position = P(0, 60);
        _letter.ZIndex = 10;
        Stage.AddChild(_letter);

        InputLocked = true;
        After(StartDelay, () =>
        {
            Voice.Play([VoiceLine.TraceLetter, _sound.SoundClip]);
            Bip.Hop();
            InputLocked = false;
            _started = true;
        });
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                _drawing = button.Pressed;
                break;
            case InputEventMouseMotion motion when _drawing || motion.ButtonMask.HasFlag(MouseButtonMask.Left):
                GetViewport().SetInputAsHandled();
                Trace(Stage.ToLocal(motion.GlobalPosition));
                return;
        }
        base._UnhandledInput(@event);
    }

    private void Trace(Vector2 point)
    {
        if (!_started || InputLocked) return;
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
        var grow = _letter.CreateTween();
        grow.TweenProperty(_letter, "scale", Vector2.One * 1.12f, 0.15);
        grow.TweenProperty(_letter, "scale", Vector2.One, 0.15);
        Buttons.Sparkle(P(0, 60), Stage);
        Sfx.Play(BipSounds.Effect.Chime);
        Bip.Celebrate();
        Record(true, _game.SkillId(new LetterTraceGame.Round(_sound)), _sound.Id);
        Voice.Play([Coordinator.RandomPraise(), _sound.SoundClip], completion: () => Coordinator.ShowIsland(Island.Letters, greet: false));
    }

    protected override void HandleTap(string name, Node2D node)
    {
        if (name == "letter") Voice.Play([_sound.SoundClip]);
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) Voice.Play([VoiceLine.TraceLetter, _sound.SoundClip]);
    }
}
