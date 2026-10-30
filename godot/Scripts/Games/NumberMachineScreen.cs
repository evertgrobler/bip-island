using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using BipIsland.Screens;
using Godot;
using static BipIsland.Drawing.Up;
using Mode = BipCore.NumberMachineGame.Mode;

namespace BipIsland.Games;

/// <summary>
/// Number Machine (Numbers Island): Bip's hand-drawn machine with a rule card on the front (× 5, ÷ 4).
/// A number ball rolls into the hopper, the handle turns, and the window shows the groups (six towers of
/// two cubes for 6 × 2) while what comes out waits in the chute as a question mark. Three cards: a
/// number, or for the rule level a rule card. Wrong → soft boop, try again; two misses → the right card
/// wiggles, the groups show (if they weren't already) and Bip counts in steps while each one bobs.
/// No timers, no dragging.
/// </summary>
public partial class NumberMachineScreen : GameScreen
{
    private static readonly Vector2[] ChoiceAt = [P(-350, -330), P(0, -330), P(350, -330)];
    private static readonly Vector2 MachineAt = P(40, 90);
    private static readonly Vector2 InAt = P(-470, 250);
    private static readonly Vector2 OutAt = P(500, -60);
    private const double WindowWidth = 460;
    private const double WindowHeight = 150;

    private readonly NumberMachineGame _game;
    private readonly GameSession _session;
    private readonly Color _colour;
    private NumberMachineGame.Round? _round;
    private QuestionAttempt _attempt = new();
    private readonly List<Node2D> _choices = [];
    private readonly List<Node2D> _scene = [];
    private readonly List<Node2D> _groups = [];
    private Node2D? _machine;
    private Node2D? _handle;
    private Node2D? _window;
    private Node2D? _outText;
    private Node2D? _outBall;
    private Node2D? _inText;
    private Node2D? _inBall;
    private Node2D? _ruleCard;
    private bool _greeted;

    public NumberMachineScreen(NumberMachineGame game)
    {
        _game = game;
        _session = Coordinator.NewSession(game);
        _colour = BlockDrawing.ChildColour(Coordinator.CurrentChild?.Avatar);
    }

    protected override Island HomeIsland => Island.Numbers;
    protected override string GameId => NumberMachineGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions => _choices;

    /// <summary>The question on screen (for the walk-through test), or null between questions.</summary>
    public NumberMachineGame.Round? Round => _round;

    /// <summary>Which card is right (for the walk-through test), or -1 before the cards show.</summary>
    public int RightIndex => _round == null || _choices.Count == 0 ? -1 : _round.Choices.ToList().FindIndex(c => _game.IsCorrect(c, _round));

    /// <summary>Groups in the window (for the walk-through test).</summary>
    public int GroupsShown => _groups.Count;

    protected override void Build()
    {
        AddGameChrome(P(640, -330), P(-640, -400), 0.7f);
        DrawMachine();
        After(StartDelay, AskQuestion);
    }

    private ulong Seed(int i) => (ulong)(6200 + _session.RoundsPlayed * 43 + i);

    private T Add<T>(T node, Vector2 at, int z = 5, Node? parent = null) where T : Node2D
    {
        node.Position = at;
        node.ZIndex = z;
        (parent ?? Stage).AddChild(node);
        return node;
    }

    // MARK: - The machine (drawn once; the rule card, balls and groups change each round)

    private void DrawMachine()
    {
        var machine = Add(new Node2D { Name = "Machine" }, MachineAt, 2);
        _machine = machine;
        // Hopper on top-left, chute out bottom-right, body, legs.
        machine.AddChild(Pen(Polygon(P(-330, 250), P(-170, 250), P(-200, 170), P(-300, 170)), 6101, fill: Palette.Sun, lineWidth: 6));
        machine.AddChild(Pen(Polygon(P(250, -60), P(380, -110), P(380, -170), P(250, -130)), 6102, fill: Palette.Sun, lineWidth: 6));
        foreach (var (i, x) in Indexed(-230.0, 210.0))
            machine.AddChild(Pen(RoundRect(R(x - 18, -260, 36, 90), 10), (ulong)(6103 + i), fill: Palette.Stone, lineWidth: 5));
        machine.AddChild(Pen(RoundRect(R(-280, -180, 560, 360), 40), 6105, fill: Palette.Teal, lineWidth: 7));
        // Lights along the top.
        foreach (var (i, colour) in Indexed(Palette.Red, Palette.Sun, Palette.Leaf, Palette.Purple))
            machine.AddChild(Pen(Ellipse(P(-90 + i * 60, 150), 16, 16), (ulong)(6106 + i), fill: colour, lineWidth: 4));
        // The window that shows the groups.
        _window = Add(new Node2D { Name = "Window" }, P(0, -70), 1, machine);
        _window.AddChild(Pen(RoundRect(R(-WindowWidth / 2 - 10, -WindowHeight / 2 - 10, WindowWidth + 20, WindowHeight + 20), 20), 6110,
                             fill: Palette.Card, lineWidth: 5));
        // The handle on the right side: a disc with an arm and a knob, which turns.
        _handle = Add(new Node2D { Name = "Handle" }, P(300, 60), 3, machine);
        _handle.AddChild(Pen(Ellipse(Vector2.Zero, 34, 34), 6111, fill: Palette.Orange, lineWidth: 5));
        _handle.AddChild(Pen(Polyline(Vector2.Zero, P(70, 60)), 6112, ink: Palette.Ink, lineWidth: 9));
        _handle.AddChild(Pen(Ellipse(P(70, 60), 18, 18), 6113, fill: Palette.Red, lineWidth: 4));
    }

    private void Clear()
    {
        StopHint();
        foreach (var node in _choices.Concat(_scene).Concat(_groups)) node.QueueFree();
        _choices.Clear();
        _scene.Clear();
        _groups.Clear();
        _outText = _outBall = _inText = _inBall = _ruleCard = null;
    }

    private void AskQuestion()
    {
        if (_session.NextRound(_game, Coordinator.LearnerFor(GameId), Coordinator.Rng) is not { } next)
        {
            EndVisit(Ending.RoundDone);
            return;
        }
        Clear();
        _attempt = new QuestionAttempt();
        ResetKeys();
        _round = next;
        DrawRuleCard(next);
        // What goes in, and what comes out: the one to find is a question mark.
        var (inText, outText) = next.Mode switch
        {
            Mode.Out => (next.In.ToString(), "?"),
            Mode.In => ("?", next.Product.ToString()),
            Mode.Divide => (next.Product.ToString(), "?"),
            _ => (next.In.ToString(), next.Product.ToString()),
        };
        var ball = Ball(inText, Seed(1), out var inLabel);
        _inBall = ball;
        _inText = inLabel;
        Add(ball, InAt + P(-260, 120), 6);
        _scene.Add(ball);
        ball.CreateTween().TweenProperty(ball, "position", InAt, 0.6).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
        _outBall = Add(Ball(outText, Seed(2), out var text), OutAt, 6);
        _outText = text;
        _scene.Add(_outBall);
        if (next.Mode == Mode.Rule) DrawExamples(next);
        TurnHandle();
        // The times machine shows its groups as it runs; the others keep them for the hint.
        if (next.Mode == Mode.Out) After(0.7, () => { if (_round == next) ShowGroups(next); });
        DrawChoices(next);
        Sfx.Play(BipSounds.Effect.Whirr);
        InputLocked = false;
        // The first question of a visit starts with "This is Bip's number machine!"
        SayPrompt(withIntro: !_greeted);
        _greeted = true;
    }

    private void DrawRuleCard(NumberMachineGame.Round round)
    {
        if (_machine == null) return;
        var card = Add(new Node2D { Name = "Rule" }, P(0, 80), 2, _machine);
        card.AddChild(Pen(RoundRect(R(-120, -50, 240, 100), 22), Seed(3), fill: Palette.Card, ink: Palette.Purple, lineWidth: 6));
        var sign = round.Mode == Mode.Divide ? "÷" : "×";
        var label = round.Mode == Mode.Rule ? $"{sign} ?" : $"{sign} {round.Rule}";
        card.AddChild(Sketch.Letter(label, 80, shadow: Palette.Purple));
        _ruleCard = card;
        _scene.Add(card);
    }

    private Node2D Ball(string text, ulong seed, out Node2D label)
    {
        var ball = new Node2D();
        ball.AddChild(Pen(Ellipse(Vector2.Zero, 78, 78), seed, fill: text == "?" ? Palette.BalanceBoxFill : Palette.Card, ink: Palette.Orange, lineWidth: 6));
        label = Sketch.Letter(text, text.Length > 2 ? 70 : 90, colour: text == "?" ? Palette.AlertOrange : Palette.Ink, shadow: Palette.Sun);
        ball.AddChild(label);
        return ball;
    }

    /// <summary>The rule level: what went in and came out, twice, on signs left of the machine under the ball ("3 → 12").</summary>
    private void DrawExamples(NumberMachineGame.Round round)
    {
        var inputs = round.Inputs ?? [round.In];
        for (var i = 0; i < inputs.Count; i++)
        {
            var sign = Add(new Node2D { Name = $"Example{i}", Scale = Vector2.One * 0.85f }, P(-500, 80 - i * 125), 6);
            sign.AddChild(Pen(RoundRect(R(-170, -50, 340, 100), 26), Seed(10 + i), fill: Palette.Card, ink: Palette.Teal, lineWidth: 5));
            sign.AddChild(Sketch.Letter($"{inputs[i]} → {inputs[i] * round.Rule}", 70, shadow: Palette.LightTeal));
            _scene.Add(sign);
        }
        // The balls show the first example, so the same numbers are on the machine.
    }

    private void TurnHandle()
    {
        if (_handle == null) return;
        var spin = _handle.CreateTween();
        spin.TweenProperty(_handle, "rotation", _handle.Rotation + Mathf.Tau, 0.7).SetDelay(0.5);
    }

    /// <summary>
    /// The groups in the window: In towers of Rule cubes (6 × 2: six towers of two), or In rows of Rule
    /// cubes when rows give bigger cubes (2 × 10: two rows of ten), so the cubes are always big enough to count.
    /// </summary>
    private void ShowGroups(NumberMachineGame.Round round)
    {
        if (_window == null || _groups.Count > 0) return;
        var towerSize = Math.Min(Math.Min(46, (WindowHeight - 14) / round.Rule), WindowWidth / round.In - 8);
        var rowSize = Math.Min(Math.Min(46, (WindowWidth - 14) / round.Rule), WindowHeight / round.In - 6);
        var rows = rowSize > towerSize;
        var size = rows ? rowSize : towerSize;
        var spacing = rows ? Math.Min(size + 12, WindowHeight / round.In) : Math.Min(size + 18, WindowWidth / round.In);
        for (var i = 0; i < round.In; i++)
        {
            var tower = BlockDrawing.Tower(round.Rule, i % 2 == 0 ? _colour : BlockDrawing.Lighter(_colour), size, Seed(100 + i));
            // A row is a tower turned to lie on its side, growing to the right from its left end.
            if (rows) tower.Rotation = Mathf.Pi / 2;
            Add(tower, rows ? P(-round.Rule * size / 2, ((round.In - 1) / 2.0 - i) * spacing)
                            : P((i - (round.In - 1) / 2.0) * spacing, -WindowHeight / 2 + 4), 1, _window);
            tower.Scale = Vector2.One * 0.01f;
            tower.CreateTween().TweenProperty(tower, "scale", Vector2.One, 0.2).SetDelay(i * 0.06);
            _groups.Add(tower);
        }
    }

    // MARK: - Cards

    private void DrawChoices(NumberMachineGame.Round round)
    {
        for (var i = 0; i < round.Choices.Count; i++)
        {
            var button = Buttons.Tappable(new Node2D(), $"choice:{i}");
            var text = round.Mode == Mode.Rule ? $"× {round.Choices[i]}" : round.Choices[i].ToString();
            if (round.Mode == Mode.Rule)
                button.AddChild(Pen(RoundRect(R(-120, -70, 240, 140), 26), Seed(30 + i), fill: Palette.Card, ink: Palette.Purple, lineWidth: 6));
            else
                button.AddChild(Pen(Ellipse(Vector2.Zero, 95, 95), Seed(30 + i), fill: Palette.Card, lineWidth: 6));
            button.AddChild(Sketch.Letter(text, text.Length > 3 ? 80 : 100, shadow: Palette.Red));
            button.Scale = Vector2.One * 0.01f;
            _choices.Add(Add(button, ChoiceAt[i % ChoiceAt.Length], 10));
            button.CreateTween().TweenProperty(button, "scale", Vector2.One, 0.25);
        }
    }

    // MARK: - Voice

    private void SayPrompt(bool withIntro = false)
    {
        if (_round is not { } round) return;
        Bip.Hop();
        var prompt = _game.Prompt(round);
        Voice.Play(withIntro ? [VoiceLine.NumberMachine, .. prompt] : prompt);
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }

    // MARK: - Answers

    protected override void HandleTap(string name, Node2D node)
    {
        if (name == "replay")
        {
            Buttons.Press(node);
            SayPrompt();
            return;
        }
        if (_round is not { } round || !name.StartsWith("choice:", StringComparison.Ordinal)
            || !int.TryParse(name["choice:".Length..], out var index) || index >= round.Choices.Count || index >= _choices.Count) return;
        Answer(round, index);
    }

    private void Answer(NumberMachineGame.Round round, int index)
    {
        var card = _choices[index];
        var outcome = _attempt.Answer(_game.IsCorrect(round.Choices[index], round));
        if (outcome.Kind == AnswerOutcomeKind.Correct)
        {
            InputLocked = true;
            StopHint();
            // The question mark becomes the answer: on the ball that came out, or on the rule card.
            if (round.Mode == Mode.Rule && _ruleCard != null)
            {
                _ruleCard.GetChild(1).QueueFree();
                _ruleCard.AddChild(Sketch.Letter($"× {round.Rule}", 80, shadow: Palette.Purple));
            }
            else
            {
                // What went in fills the ball that rolled in; anything else, the ball that came out.
                var (ball, label) = round.Mode == Mode.In ? (_inBall, _inText) : (_outBall, _outText);
                label?.QueueFree();
                ball?.AddChild(Sketch.Letter(round.Answer.ToString(), round.Answer >= 100 ? 70 : 90, shadow: Palette.Sun));
            }
            var grow = card.CreateTween();
            grow.TweenProperty(card, "scale", Vector2.One * 1.15f, 0.15);
            grow.TweenProperty(card, "scale", Vector2.One * 1.05f, 0.1);
            Buttons.Sparkle(card.Position, Stage);
            if (_outBall != null) Buttons.Sparkle(_outBall.Position, Stage);
            TurnHandle();
            Sfx.Play(BipSounds.Effect.Chime);
            Bip.Celebrate();
            var change = Record(outcome.FirstTry, _game.SkillId(round));
            Voice.Play([Coordinator.RandomPraise(), .. _game.SayWhenRight(round)],
                       completion: () => AfterAnswer(change, _session, AskQuestion));
            return;
        }
        Sfx.Play(BipSounds.Effect.Boop);
        Buttons.Shake(card);
        Bip.Tilt();
        if (outcome.Kind != AnswerOutcomeKind.Hint) return;
        // Two misses: the right card wiggles, the groups show, and each bobs as Bip counts in steps.
        if (RightIndex is var r && r >= 0 && r < _choices.Count) ShowHint(_choices[r]);
        ShowGroups(round);
        After(0.4, () => Voice.Play(_game.Hint(round)));
        for (var i = 0; i < _groups.Count; i++)
        {
            var group = _groups[i];
            After(1.8 + i * 0.7, () =>
            {
                if (_round != round || !IsInstanceValid(group)) return;
                var bob = group.CreateTween();
                bob.TweenProperty(group, "scale", Vector2.One * 1.15f, 0.15);
                bob.TweenProperty(group, "scale", Vector2.One, 0.2);
            });
        }
    }
}
