using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using BipIsland.Screens;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Games;

/// <summary>
/// Feed the Monster: drag only the foods that start with the sound into a hungry monster. The food
/// never repeats in a visit. Wrong → soft boop, the food goes back and the sound is said again; two
/// misses → spoken hint and the right food wiggles. From the keyboard, Enter feeds the glowing food.
/// </summary>
public partial class FeedMonsterScreen : GameScreen
{
    private static readonly Vector2 MonsterMouth = P(520, 60);
    /// <summary>A food dropped this close to the mouth is eaten.</summary>
    private const float EatDistance = 150;

    private readonly FeedMonsterGame _game;
    /// <summary>The sound Bip chose this visit for; a level change on it ends the visit.</summary>
    private readonly PhonicsSound _focus;
    private readonly GameSession _session;
    private FeedMonsterGame.Round? _round;
    private QuestionAttempt _attempt = new();
    private readonly List<PictureCard> _cards = [];
    private readonly List<Vector2> _homes = [];
    private PictureCard? _dragged;
    private Vector2 _dragOffset;

    public FeedMonsterScreen(FeedMonsterGame game, PhonicsSound focus)
    {
        _game = game;
        _focus = focus;
        _session = Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Letters;
    protected override string GameId => FeedMonsterGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions => _cards;

    /// <summary>Which food is right (for the walk-through test), or -1 between questions.</summary>
    public int RightIndex => _round == null || _cards.Count == 0 ? -1 : _round.Choices.ToList().FindIndex(c => _game.IsCorrect(c, _round));

    /// <summary>Three foods in a row; four (a higher level) in a 2 × 2 grid, slightly smaller, left of the monster.</summary>
    private static Vector2 FoodPosition(int index, int count)
    {
        Vector2[] spots = count <= 3
            ? [P(-500, 60), P(-150, 60), P(200, 60)]
            : [P(-420, 190), P(-100, 190), P(-420, -110), P(-100, -110)];
        return spots[index % spots.Length];
    }

    protected override void Build()
    {
        AddGameChrome(P(600, -330));
        var body = Pen(Ellipse(MonsterMouth, 150, 170), 900, fill: Palette.Purple, lineWidth: 6);
        body.ZIndex = 5;
        Stage.AddChild(body);
        var mouth = Pen(Ellipse(MonsterMouth, 80, 62), 901, fill: Palette.Ink, lineWidth: 4);
        mouth.ZIndex = 6;
        Stage.AddChild(mouth);
        foreach (var (i, eye) in Indexed(-50, 50))
        {
            var white = Pen(Ellipse(P(520 + eye, 190), 30, 36), (ulong)(902 + i), fill: Palette.White, lineWidth: 4);
            white.ZIndex = 6;
            Stage.AddChild(white);
        }
        After(StartDelay, AskQuestion);
    }

    private void AskQuestion()
    {
        if (_session.NextRound(_game, Coordinator.LearnerFor(GameId, _focus), Coordinator.Rng) is not { } next)
        {
            EndVisit(Ending.RoundDone);
            return;
        }
        StopHint();
        foreach (var card in _cards) card.QueueFree();
        _cards.Clear();
        _homes.Clear();
        _dragged = null;
        _attempt = new QuestionAttempt();
        ResetKeys();
        _round = next;
        for (var i = 0; i < next.Choices.Count; i++)
        {
            var choice = next.Choices[i];
            var card = Buttons.Tappable(new PictureCard(choice.Picture, choice.Word, (ulong)(910 + _session.RoundsPlayed * 7 + i)), $"food:{i}");
            card.Position = FoodPosition(i, next.Choices.Count);
            if (next.Choices.Count > 3) card.Scale = Vector2.One * 0.85f;
            card.ZIndex = 10;
            Stage.AddChild(card);
            _cards.Add(card);
            _homes.Add(card.Position);
        }
        Sfx.Play(BipSounds.Effect.Beep);
        InputLocked = false;
        SayPrompt();
    }

    private void SayPrompt()
    {
        if (_round is not { } round) return;
        Voice.Play([VoiceLine.FeedMonster, round.Target.SoundClip]);
        Bip.Hop();
    }

    // Dragging

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press
                when !InputLocked && _dragged == null && FoodAt(press.GlobalPosition) is { } card:
                GetViewport().SetInputAsHandled();
                _dragged = card;
                _dragOffset = card.Position - Stage.ToLocal(press.GlobalPosition);
                card.ZIndex = 20;
                if (card == HintedNode) StopHint();
                return;
            case InputEventMouseMotion motion when _dragged != null:
                GetViewport().SetInputAsHandled();
                if (!InputLocked) _dragged.Position = Stage.ToLocal(motion.GlobalPosition) + _dragOffset;
                return;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } when _dragged is { } dropped:
                GetViewport().SetInputAsHandled();
                _dragged = null;
                if (!InputLocked && dropped.Position.DistanceTo(MonsterMouth) < EatDistance) Feed(_cards.IndexOf(dropped));
                else SendHome(dropped);
                return;
        }
        base._UnhandledInput(@event);
    }

    private PictureCard? FoodAt(Vector2 global) =>
        _cards.LastOrDefault(c => Hit.IsShown(c) && Hit.GlobalArea(c) is { } area && area.HasPoint(global));

    /// <summary>Drags a food to the monster through the real mouse handling (for the walk-through test).</summary>
    public void DragToMonsterForTest(int index)
    {
        if (index < 0 || index >= _cards.Count) return;
        var from = Stage.ToGlobal(_cards[index].Position);
        var to = Stage.ToGlobal(MonsterMouth);
        _UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, GlobalPosition = from, Position = from });
        _UnhandledInput(new InputEventMouseMotion { GlobalPosition = to, Position = to });
        _UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, GlobalPosition = to, Position = to });
    }

    private void SendHome(PictureCard card)
    {
        card.ZIndex = 10;
        var index = _cards.IndexOf(card);
        if (index >= 0 && index < _homes.Count) card.CreateTween().TweenProperty(card, "position", _homes[index], 0.25);
    }

    private void Feed(int index)
    {
        if (_round is not { } round || index < 0 || index >= round.Choices.Count) return;
        var card = _cards[index];
        card.ZIndex = 10;
        var outcome = _attempt.Answer(_game.IsCorrect(round.Choices[index], round));
        if (outcome.Kind == AnswerOutcomeKind.Correct)
        {
            InputLocked = true;
            StopHint();
            card.CreateTween().TweenProperty(card, "position", MonsterMouth, 0.15);
            var gulp = card.CreateTween();
            gulp.TweenProperty(card, "scale", Vector2.One * 0.4f, 0.2);
            gulp.TweenProperty(card, "modulate:a", 0f, 0.05);
            Sfx.Play(BipSounds.Effect.Pop);
            After(0.15, () => Sfx.Play(BipSounds.Effect.Chime));
            Bip.Celebrate();
            var change = Record(outcome.FirstTry, _game.SkillId(round), round.Target.Id);
            var counted = round.Target.Id == _focus.Id ? change : MasteryChange.None;
            Voice.Play([Coordinator.RandomPraise(), round.Answer.Audio], completion: () => AfterAnswer(counted, _session, AskQuestion));
            return;
        }
        Sfx.Play(BipSounds.Effect.Boop);
        Buttons.Shake(card);
        Bip.Tilt();
        SendHome(card);
        if (outcome.Kind == AnswerOutcomeKind.Hint)
        {
            if (RightIndex is var right && right >= 0 && right < _cards.Count) ShowHint(_cards[right]);
            After(0.4, () => Voice.Play([Coordinator.RandomHint(), round.Target.SoundClip]));
        }
        else
        {
            After(0.4, () => Voice.Play([round.Target.SoundClip]));
        }
    }

    protected override void HandleTap(string name, Node2D node)
    {
        if (name == "replay")
        {
            Buttons.Press(node);
            SayPrompt();
            return;
        }
        // Mouse presses on food belong to dragging; this is the keyboard's Enter on the glowing food.
        if (name.StartsWith("food:") && int.TryParse(name["food:".Length..], out var index)) Feed(index);
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}
