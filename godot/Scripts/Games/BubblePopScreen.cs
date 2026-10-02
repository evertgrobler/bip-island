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
/// Bubble Pop: letter bubbles float slowly up; pop the one that says the sound you hear. No timer:
/// bubbles drift round and round, and the game keeps exactly one right letter on screen at all times.
/// </summary>
public partial class BubblePopScreen : GameScreen
{
    private sealed class Bubble(Node2D node, double lane, double phase, double speed, PhonicsSound sound, Node2D label)
    {
        public Node2D Node { get; } = node;
        public double Lane { get; } = lane;
        public double Phase { get; } = phase;
        public double Speed { get; } = speed;
        public PhonicsSound Sound { get; set; } = sound;
        public Node2D Label { get; set; } = label;
        /// <summary>Height in Mac app coordinates (y up), so the drift reads like the original.</summary>
        public double Y { get; set; }
        public Tween? Wiggle { get; set; }
    }

    private const double Radius = 95;
    private const double Top = 600;
    private const double Bottom = -600;

    private readonly BubblePopGame _game;
    /// <summary>The sound Bip chose this visit for; a level change on it ends the visit.</summary>
    private readonly PhonicsSound _focus;
    private readonly GameSession _session;
    private BubblePopGame.Round? _round;
    private readonly List<Bubble> _bubbles = [];
    private QuestionAttempt _attempt = new();
    private double _elapsed;
    private double _slowDown = 1;

    public BubblePopScreen(BubblePopGame game, PhonicsSound focus)
    {
        _game = game;
        _focus = focus;
        _session = Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Letters;
    protected override string GameId => BubblePopGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions => [.. _bubbles.Select(b => b.Node)];

    /// <summary>Tap name of a bubble showing the right letter (for the walk-through test), or null.</summary>
    public string? RightBubble =>
        _round is { } round ? _bubbles.FirstOrDefault(b => _game.IsCorrect(b.Sound, round)) is { } b ? Buttons.TapName(b.Node) : null : null;

    public string? WrongBubble =>
        _round is { } round ? _bubbles.FirstOrDefault(b => !_game.IsCorrect(b.Sound, round)) is { } b ? Buttons.TapName(b.Node) : null : null;

    protected override void Build()
    {
        // Big enough for any window shape. A plain four-cornered shape: a zero-radius rounded
        // rectangle repeats its corners and can't be filled.
        var water = Pen(Polygon(P(-1400, -900), P(1400, -900), P(1400, 900), P(-1400, 900)), 1000, fill: Palette.Sea.WithAlpha(0.25), lineWidth: 0);
        water.ZIndex = -60;
        Stage.AddChild(water);
        AddGameChrome(P(660, -340), P(-640, -420), 0.75f);

        if (_session.NextRound(_game, Coordinator.LearnerFor(GameId, _focus), Coordinator.Rng) is not { } first)
        {
            After(0.3, () => Coordinator.ShowIsland(Island.Letters, greet: false));
            return;
        }
        _round = first;
        // One lane per bubble (more bubbles at higher levels), spread across the screen.
        var count = first.Choices.Count;
        var spacing = Math.Min(220, 1300.0 / Math.Max(count - 1, 1));
        var starts = Coordinator.Rng.Shuffled(Enumerable.Range(0, count).Select(i => -420 + i * 680.0 / Math.Max(count - 1, 1)));
        var pace = first.SpeedPercent / 100.0;
        for (var i = 0; i < count; i++)
        {
            var node = Buttons.Tappable(new Node2D(), $"bubble:{i}");
            node.ZIndex = 10;
            node.AddChild(Pen(Ellipse(Vector2.Zero, Radius, Radius), (ulong)(1010 + i), fill: Palette.Bubble.WithAlpha(0.85), ink: Palette.Sea, lineWidth: 5));
            var shine = Oval(26, 44, Palette.White.WithAlpha(0.8));
            shine.Position = P(-50, 40);
            shine.Rotation = Turn(0.6);
            node.AddChild(shine);
            var label = Sketch.Letter(first.Choices[i].Grapheme, 120, withShadow: false);
            node.AddChild(label);
            Stage.AddChild(node);
            var speed = pace * (45 + Coordinator.Rng.NextIndex(21));
            var bubble = new Bubble(node, (i - (count - 1) / 2.0) * spacing, i * 1.3, speed, first.Choices[i], label) { Y = starts[i] };
            node.Position = P(bubble.Lane, bubble.Y);
            _bubbles.Add(bubble);
        }
        After(StartDelay == 0 ? 0 : 0.6, () => AskQuestion(isFirst: true));
    }

    public override void _Process(double delta)
    {
        var dt = Math.Min(delta, 0.1);
        _elapsed += dt;
        foreach (var bubble in _bubbles)
        {
            bubble.Y += bubble.Speed * _slowDown * dt;
            if (bubble.Y > Top)
            {
                bubble.Y = Bottom;
                Respawn(bubble);
            }
            bubble.Node.Position = P(bubble.Lane + 30 * Math.Sin(_elapsed * 0.7 + bubble.Phase), bubble.Y);
        }
    }

    /// <summary>A bubble that floated off the top comes back at the bottom with a new letter.</summary>
    private void Respawn(Bubble bubble)
    {
        if (_round is not { } round) return;
        var others = _bubbles.Where(b => b != bubble).Select(b => b.Sound).ToList();
        SetLetter(_game.NextBubble(round, others, Coordinator.Rng), bubble);
    }

    private void SetLetter(PhonicsSound letter, Bubble bubble)
    {
        bubble.Sound = letter;
        bubble.Label.QueueFree();
        bubble.Label = Sketch.Letter(letter.Grapheme, 120, withShadow: false);
        bubble.Node.AddChild(bubble.Label);
        StopWiggle(bubble);
        bubble.Node.Modulate = Colors.White;
        if (_attempt.NeedsHint && _round is { } round && _game.IsCorrect(letter, round)) bubble.Wiggle = Buttons.HintWiggle(bubble.Node);
    }

    private static void StopWiggle(Bubble bubble)
    {
        bubble.Wiggle?.Kill();
        bubble.Wiggle = null;
        bubble.Node.Scale = Vector2.One;
        bubble.Node.Rotation = 0;
    }

    /// <summary>The first question uses the bubbles already floating; later ones get a new round of letters.</summary>
    private void AskQuestion(bool isFirst = false)
    {
        if (!isFirst)
        {
            if (_session.NextRound(_game, Coordinator.LearnerFor(GameId, _focus), Coordinator.Rng) is not { } next)
            {
                EndVisit(Ending.RoundDone);
                return;
            }
            _round = next;
            _attempt = new QuestionAttempt();
            foreach (var (bubble, letter) in _bubbles.Zip(next.Choices))
            {
                SetLetter(letter, bubble);
                bubble.Label.Scale = Vector2.One * 0.4f;
                bubble.Label.CreateTween().TweenProperty(bubble.Label, "scale", Vector2.One, 0.25);
            }
        }
        _attempt = new QuestionAttempt();
        ResetKeys();
        _slowDown = 1;
        foreach (var bubble in _bubbles) StopWiggle(bubble);
        InputLocked = false;
        SayPrompt();
    }

    private void SayPrompt()
    {
        if (_round is not { } round) return;
        Voice.Play([VoiceLine.PopTheLetter, round.Target.SoundClip]);
        Bip.Hop();
    }

    protected override void HandleTap(string name, Node2D node)
    {
        if (name == "replay")
        {
            Buttons.Press(node);
            SayPrompt();
            return;
        }
        if (_round is not { } round || !name.StartsWith("bubble:") || !int.TryParse(name["bubble:".Length..], out var index)
            || index >= _bubbles.Count) return;
        var bubble = _bubbles[index];
        var outcome = _attempt.Answer(_game.IsCorrect(bubble.Sound, round));
        switch (outcome.Kind)
        {
            case AnswerOutcomeKind.Correct:
                InputLocked = true;
                Pop(bubble);
                Bip.Celebrate();
                var change = Record(outcome.FirstTry, _game.SkillId(round), round.Target.Id);
                var counted = round.Target.Id == _focus.Id ? change : MasteryChange.None;
                Voice.Play([Coordinator.RandomPraise()], completion: () => AfterAnswer(counted, _session, () => AskQuestion()));
                break;
            case AnswerOutcomeKind.TryAgain:
                Sfx.Play(BipSounds.Effect.Boop);
                Buttons.Shake(bubble.Node);
                Bip.Tilt();
                After(0.4, () => Voice.Play([round.Target.SoundClip]));
                break;
            default:
                Sfx.Play(BipSounds.Effect.Boop);
                Buttons.Shake(bubble.Node);
                Bip.Tilt();
                _slowDown = 0.35;
                foreach (var right in _bubbles.Where(b => _game.IsCorrect(b.Sound, round)))
                {
                    StopWiggle(right);
                    right.Wiggle = Buttons.HintWiggle(right.Node);
                }
                After(0.4, () => Voice.Play([Coordinator.RandomHint(), round.Target.SoundClip]));
                break;
        }
    }

    private void Pop(Bubble bubble)
    {
        Sfx.Play(BipSounds.Effect.Pop);
        After(0.08, () => Sfx.Play(BipSounds.Effect.Chime));
        Buttons.Sparkle(bubble.Node.Position, Stage);
        StopWiggle(bubble);
        var burst = bubble.Node.CreateTween();
        burst.SetParallel();
        burst.TweenProperty(bubble.Node, "scale", Vector2.One * 1.4f, 0.12);
        burst.TweenProperty(bubble.Node, "modulate:a", 0f, 0.12);
        burst.Chain().TweenCallback(Callable.From(() =>
        {
            bubble.Y = Bottom;
            bubble.Node.Scale = Vector2.One;
            Respawn(bubble);
        }));
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}
