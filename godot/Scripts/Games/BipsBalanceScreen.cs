using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using BipIsland.Screens;
using Godot;
using static BipIsland.Drawing.Up;
using Mode = BipCore.BipsBalanceGame.Mode;
using Op = BipCore.BipsBalanceGame.Op;
using Slot = BipCore.BipsBalanceGame.Slot;

namespace BipIsland.Games;

/// <summary>
/// Bip's Balance (Numbers Island): a see-saw with a sum on the left (4 + 3), its answer on the right,
/// and "=" over the middle. One number is a box with a question mark. Tap one of three number cards:
/// the right one levels the see-saw; a wrong one shows in the box and tips the see-saw the way it
/// would really go, then comes back out. Two misses: the right card wiggles and Bip counts on (or
/// back) from the first number, with cubes. Early levels stand cubes on the see-saw; story sums show
/// the animals instead. No timers, no dragging.
/// </summary>
public partial class BipsBalanceScreen : GameScreen
{
    /// <summary>Where the see-saw turns, and how far it tips.</summary>
    private static readonly Vector2 PivotAt = P(0, -60);
    private const float TipAngle = 0.13f;
    private const double LeftCardX = -380;
    private const double RightCardX = 430;
    /// <summary>Card tops sit on the plank; their middle is this far above it.</summary>
    private const double CardMiddle = 92;
    private const double PartGap = 150;
    private const double CubeSize = 34;
    private static readonly Vector2[] ChoiceAt = [P(-350, -330), P(0, -330), P(350, -330)];

    private readonly BipsBalanceGame _game;
    private readonly GameSession _session;
    private BipsBalanceGame.Round? _round;
    private QuestionAttempt _attempt = new();
    private readonly List<Node2D> _choices = [];
    private readonly List<Node2D> _extras = [];
    private readonly Color _colour;
    private readonly Color _other;
    private Node2D? _beam;
    private Node2D? _box;
    private Node2D? _boxText;
    private Node2D? _cubes;
    private bool _greeted;

    public BipsBalanceScreen(BipsBalanceGame game)
    {
        _game = game;
        _session = Coordinator.NewSession(game);
        _colour = BlockDrawing.ChildColour(Coordinator.CurrentChild?.Avatar);
        _other = BlockDrawing.Lighter(_colour);
    }

    protected override Island HomeIsland => Island.Numbers;
    protected override string GameId => BipsBalanceGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions => _choices;

    /// <summary>The question on screen (for the walk-through test), or null between questions.</summary>
    public BipsBalanceGame.Round? Round => _round;

    /// <summary>Which card is right (for the walk-through test), or -1 before the cards show.</summary>
    public int RightIndex => _round == null || _choices.Count == 0 ? -1 : _round.Choices.ToList().FindIndex(c => _game.IsCorrect(c, _round));

    /// <summary>How far the see-saw leans now (0 = level), for the walk-through test.</summary>
    public float Tilt => _beam?.Rotation ?? 0;

    protected override void Build()
    {
        AddGameChrome(P(640, -330), P(-640, -400), 0.7f);
        DrawPark();
        After(StartDelay, AskQuestion);
    }

    private ulong Seed(int i) => (ulong)(4200 + _session.RoundsPlayed * 37 + i);

    private T Add<T>(T node, Vector2 at, int z = 5, Node? parent = null) where T : Node2D
    {
        node.Position = at;
        node.ZIndex = z;
        (parent ?? Stage).AddChild(node);
        return node;
    }

    /// <summary>The park: a grassy bank the see-saw stands on, and the see-saw's stand.</summary>
    private void DrawPark()
    {
        var grass = Pen(Ellipse(P(0, -240), 900, 110), 4100, fill: Palette.Grass.WithAlpha(0.55f), ink: Palette.Leaf, lineWidth: 5);
        grass.ZIndex = -20;
        Stage.AddChild(grass);
        var stand = Pen(Polygon(P(-80, -200), P(80, -200), P(0, -60)), 4101, fill: Palette.Orange, lineWidth: 6);
        stand.ZIndex = 2;
        Stage.AddChild(stand);
    }

    private void Clear()
    {
        StopHint();
        foreach (var node in _choices.Concat(_extras)) node.QueueFree();
        _beam?.QueueFree();
        _choices.Clear();
        _extras.Clear();
        _beam = _box = _boxText = _cubes = null;
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
        DrawBeam(next);
        if (next.Helper is { } helper) DrawHelper(helper);
        if (next.Story != null) DrawStory(next);
        else if (next.ShowsCubes) ShowCubes(next);
        DrawChoices(next);
        Sfx.Play(BipSounds.Effect.Beep);
        InputLocked = false;
        // The very first question of a visit starts with "Make both sides of the see-saw balance!"
        SayPrompt(withIntro: !_greeted);
        _greeted = true;
    }

    // MARK: - The see-saw

    /// <summary>
    /// The plank with its two cards and "=" over the stand, all on one node that tips. The sum goes on
    /// the left card, the answer on the right; one number is the box.
    /// </summary>
    private void DrawBeam(BipsBalanceGame.Round round)
    {
        var beam = Add(new Node2D { Name = "Beam" }, PivotAt, 4);
        _beam = beam;
        beam.AddChild(Pen(RoundRect(R(-660, -14, 1320, 28), 12), Seed(1), fill: Palette.LightBrown, lineWidth: 6));

        var s = round.Sentence;
        var left = Add(new Node2D(), P(LeftCardX, 0), 1, beam);
        left.AddChild(Pen(RoundRect(R(-PartGap * 1.5 - 20, 14, PartGap * 3 + 40, 156), 30), Seed(4), fill: Palette.Card, lineWidth: 6));
        Part(left, -PartGap, s.Left, round.Hidden == Slot.Left);
        Symbol(left, 0, s.Op == Op.Plus ? "+" : "−");
        Part(left, PartGap, s.Right, round.Hidden == Slot.Right);

        var right = Add(new Node2D(), P(RightCardX, 0), 1, beam);
        right.AddChild(Pen(RoundRect(R(-110, 14, 220, 156), 30), Seed(5), fill: Palette.Card, lineWidth: 6));
        Part(right, 0, s.Result, round.Hidden == Slot.Result);

        Symbol(beam, 0, "=");
        beam.Rotation = TiltFor(s.With(round.Hidden, 0));
    }

    /// <summary>A number on a card, or the box with a question mark when it's the one to find.</summary>
    private void Part(Node2D card, double x, int number, bool hidden)
    {
        if (!hidden)
        {
            Symbol(card, x, number.ToString());
            return;
        }
        var box = Add(new Node2D { Name = "Box" }, P(x, CardMiddle), 2, card);
        box.AddChild(Pen(RoundRect(R(-62, -62, 124, 124), 18), Seed(6), fill: Palette.Sun.WithAlpha(0.35f), ink: Palette.Orange, lineWidth: 5, wobble: 2.5));
        _box = box;
        ShowInBox("?", Palette.Orange);
    }

    private void Symbol(Node2D parent, double x, string text)
    {
        var letter = Sketch.Letter(text, 100, shadow: Palette.Orange);
        letter.Position = P(x, CardMiddle);
        parent.AddChild(letter);
    }

    /// <summary>Puts a number (or "?") in the box.</summary>
    private void ShowInBox(string text, Color colour)
    {
        if (_box == null) return;
        _boxText?.QueueFree();
        _boxText = Sketch.Letter(text, 100, colour: colour, shadow: Palette.Sun);
        _box.AddChild(_boxText);
    }

    /// <summary>
    /// Which way the see-saw leans for a sentence: the heavier side goes down, level when they match.
    /// (An empty box counts as nothing, so before an answer the side with the box rides up.)
    /// </summary>
    private static float TiltFor(BipsBalanceGame.Sentence s) =>
        Math.Sign(s.SumSide - s.Result) switch { > 0 => -TipAngle, < 0 => TipAngle, _ => 0 };

    private Tween? _tip;

    private void TipTo(float angle, double seconds = 0.45)
    {
        if (_beam == null) return;
        _tip?.Kill();
        _tip = _beam.CreateTween();
        _tip.TweenProperty(_beam, "rotation", angle, seconds).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    /// <summary>Counts wrong answers, so only the latest one takes its number back out of the box.</summary>
    private int _wrongShown;

    /// <summary>Fact families: the finished sum on a sign above, the one that helps.</summary>
    private void DrawHelper(BipsBalanceGame.Sentence helper)
    {
        var sign = Add(new Node2D { Name = "Helper" }, P(0, 290), 6);
        sign.AddChild(Pen(RoundRect(R(-300, -60, 600, 120), 30), Seed(7), fill: Palette.Card, ink: Palette.Teal, lineWidth: 6));
        sign.AddChild(Sketch.Letter($"{helper.Left} + {helper.Right} = {helper.Result}", 80, shadow: Palette.LightTeal));
        _extras.Add(sign);
    }

    // MARK: - Cubes and stories (on the left card, so they ride the see-saw)

    /// <summary>
    /// Cubes for the sum: two towers to add (the second a lighter shade), or one tower with the
    /// cubes taken away crossed out. Shown from the start on the first levels, and in the hint.
    /// </summary>
    private void ShowCubes(BipsBalanceGame.Round round)
    {
        if (_beam == null || _cubes != null) return;
        var s = round.Sentence;
        var cubes = Add(new Node2D { Name = "Cubes" }, P(LeftCardX, 176), 3, _beam);
        _cubes = cubes;
        if (s.Op == Op.Plus)
        {
            Add(BlockDrawing.Tower(s.Left, _colour, CubeSize, Seed(20)), P(-PartGap, 0), 0, cubes);
            Add(BlockDrawing.Tower(s.Right, _other, CubeSize, Seed(50)), P(PartGap, 0), 0, cubes);
            return;
        }
        var tower = Add(BlockDrawing.Tower(s.Left, _colour, CubeSize, Seed(20)), P(-PartGap / 2, 0), 0, cubes);
        // The top cubes go: faded, with a cross through each.
        for (var i = s.Left - s.Right; i < s.Left; i++)
        {
            var at = BlockDrawing.CubeAt(i, s.Left, CubeSize);
            var cube = tower.GetChild<Node2D>(i);
            cube.Modulate = new Color(1, 1, 1, 0.35f);
            var cross = Pen(Polyline(at + P(-13, -13), at + P(13, 13)), Seed(60 + i), ink: Palette.Red, lineWidth: 5, wobble: 0.8);
            cross.AddChild(Pen(Polyline(at + P(-13, 13), at + P(13, -13)), Seed(80 + i), ink: Palette.Red, lineWidth: 5, wobble: 0.8));
            tower.AddChild(cross);
        }
    }

    /// <summary>Story sums: the animals instead of cubes, in two groups (the ones that go away faded).</summary>
    private void DrawStory(BipsBalanceGame.Round round)
    {
        if (_beam == null || round.Thing is not { } thing) return;
        var s = round.Sentence;
        var group = Add(new Node2D { Name = "Story" }, P(LeftCardX, 200), 3, _beam);
        _cubes = group;
        void Animals(int count, double x, bool faded)
        {
            for (var i = 0; i < count; i++)
            {
                var picture = PictureNode.Make(thing.Picture, thing.Id);
                picture.Scale = Vector2.One * 0.5f;
                if (faded) picture.Modulate = new Color(1, 1, 1, 0.35f);
                Add(picture, P(x + (i % 3 - 1) * 80, 40 + i / 3 * 80), 0, group);
            }
        }
        if (s.Op == Op.Plus)
        {
            Animals(s.Left, -PartGap, false);
            Animals(s.Right, PartGap, false);
        }
        else
        {
            Animals(s.Left - s.Right, -PartGap, false);
            Animals(s.Right, PartGap, true);
        }
    }

    // MARK: - Number cards

    private void DrawChoices(BipsBalanceGame.Round round)
    {
        for (var i = 0; i < round.Choices.Count; i++)
        {
            var button = Buttons.Tappable(new Node2D(), $"choice:{i}");
            button.AddChild(Pen(Ellipse(Vector2.Zero, 95, 95), Seed(30 + i), fill: Palette.Card, lineWidth: 6));
            button.AddChild(Sketch.Letter(round.Choices[i].ToString(), 110, shadow: Palette.Red));
            button.Scale = Vector2.One * 0.01f;
            _choices.Add(Add(button, ChoiceAt[i % ChoiceAt.Length], 10));
            button.CreateTween().TweenProperty(button, "scale", Vector2.One, 0.25);
        }
        Sfx.Play(BipSounds.Effect.Chime);
    }

    // MARK: - Voice

    private void SayPrompt(bool withIntro = false)
    {
        if (_round is not { } round) return;
        Bip.Hop();
        var prompt = _game.Prompt(round);
        Voice.Play(withIntro ? [VoiceLine.Balance, .. prompt] : prompt);
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

    private void Answer(BipsBalanceGame.Round round, int index)
    {
        var choice = round.Choices[index];
        var card = _choices[index];
        var outcome = _attempt.Answer(_game.IsCorrect(choice, round));
        if (outcome.Kind == AnswerOutcomeKind.Correct)
        {
            InputLocked = true;
            StopHint();
            ShowInBox(choice.ToString(), Palette.Ink);
            TipTo(0, 0.6);
            var grow = card.CreateTween();
            grow.TweenProperty(card, "scale", Vector2.One * 1.15f, 0.15);
            grow.TweenProperty(card, "scale", Vector2.One * 1.05f, 0.1);
            Buttons.Sparkle(card.Position, Stage);
            if (_box != null) Buttons.Sparkle(Stage.ToLocal(_box.GlobalPosition), Stage);
            Sfx.Play(BipSounds.Effect.Chime);
            Bip.Celebrate();
            var change = Record(outcome.FirstTry, _game.SkillId(round));
            Voice.Play([Coordinator.RandomPraise(), .. _game.SayWhenRight(round)],
                       completion: () => AfterAnswer(change, _session, AskQuestion));
            return;
        }
        // Wrong: the number goes in the box and the see-saw tips the way it really would, then it comes back out.
        Sfx.Play(BipSounds.Effect.Boop);
        Buttons.Shake(card);
        Bip.Tilt();
        ShowInBox(choice.ToString(), Palette.Red);
        TipTo(TiltFor(round.Sentence.With(round.Hidden, choice)));
        var shown = ++_wrongShown;
        After(1.0, () =>
        {
            if (_round != round || InputLocked || shown != _wrongShown) return;
            ShowInBox("?", Palette.Orange);
            TipTo(TiltFor(round.Sentence.With(round.Hidden, 0)));
        });
        if (outcome.Kind != AnswerOutcomeKind.Hint) return;
        // Two misses: the right card wiggles; Bip counts on or back with the cubes, or says the sum.
        var right = RightIndex is var r && r >= 0 && r < _choices.Count ? _choices[r] : null;
        if (right != null) ShowHint(right);
        // (Not for fact families: their finished sum is the help, and the sign sits where the cubes would.)
        if (round.Hidden == Slot.Result && round.Story == null && round.Mode != Mode.FactFamily) ShowCubes(round);
        var hint = _game.Hint(round);
        After(0.4, () => Voice.Play(round.Hidden == Slot.Result ? hint : [AudioCatalogue.LookHint, .. hint]));
    }
}
