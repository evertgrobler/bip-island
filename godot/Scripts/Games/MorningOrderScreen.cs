using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Games;

/// <summary>
/// Morning Order: put the picture cards in order — what comes first? The shuffled cards wait in a
/// row at the bottom; tapping one sends it up to the next numbered space (1, 2, 3…) and says what
/// it shows. Tapping a placed card sends it back down. When every space is full the order is
/// checked: wrong → soft boop and the cards from the first mistake slide back down; two misses →
/// the card that belongs next wiggles and the routine is said in order.
/// Tapping instead of dragging: small hands miss drop targets, and a click always lands.
/// </summary>
public partial class MorningOrderScreen : GameScreen
{
    private const double SlotY = 150;
    private const double TrayY = -190;

    private readonly MorningOrderGame _game;
    private readonly GameSession _session;
    private MorningOrderGame.Round? _round;
    private QuestionAttempt _attempt = new();
    /// <summary>One holder per card (tap name card:&lt;i&gt;), in the round's card order.</summary>
    private readonly List<Node2D> _holders = [];
    private readonly List<Tween?> _moves = [];
    private List<Vector2> _trayPositions = [];
    private List<Vector2> _slotPositions = [];
    /// <summary>Which card (index into the holders) sits in each numbered space.</summary>
    private List<int?> _slots = [];
    private readonly List<Node2D> _slotMarks = [];

    public MorningOrderScreen(MorningOrderGame game)
    {
        _game = game;
        _session = Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Coding;
    protected override string GameId => MorningOrderGame.GameId;

    /// <summary>Arrows move between the cards still waiting, then the placed ones; Enter taps.</summary>
    protected override IReadOnlyList<Node2D> KeyOptions
    {
        get
        {
            var waiting = Enumerable.Range(0, _holders.Count).Where(i => !_slots.Contains(i)).OrderBy(i => _trayPositions[i].X);
            return [.. waiting.Concat(_slots.OfType<int>()).Select(i => _holders[i])];
        }
    }

    /// <summary>Tap names of the cards in the right order (for the walk-through test).</summary>
    public List<string> AnswerCardNames() =>
        _round == null ? [] : [.. _round.Answer.Select(card => $"card:{_round.Set.Cards.IndexOf(card)}")];

    /// <summary>How many numbered spaces hold a card (for the walk-through test).</summary>
    public int PlacedCount => _slots.Count(s => s != null);

    protected override void Build()
    {
        AddGameChrome(P(700, -400), P(-700, -400), 0.7f);
        After(StartDelay, AskQuestion);
    }

    private void AskQuestion()
    {
        if (_session.NextRound(_game, Coordinator.LearnerFor(GameId), Coordinator.Rng) is not { } next)
        {
            EndVisit(Ending.RoundDone);
            return;
        }
        StopHint();
        foreach (var node in _holders.Concat(_slotMarks)) node.QueueFree();
        _holders.Clear();
        _moves.Clear();
        _slotMarks.Clear();
        _attempt = new QuestionAttempt();
        ResetKeys();
        _round = next;

        var cards = next.Set.Cards;
        var count = cards.Count;
        // Six cards still fit across the screen with room between them.
        var scale = count <= 4 ? 1f : count == 5 ? 0.86f : 0.72f;
        var spacing = PictureCard.Size * scale + 40;
        var xs = Enumerable.Range(0, count).Select(i => (i - (count - 1) / 2.0) * spacing).ToList();
        _slotPositions = [.. xs.Select(x => P(x, SlotY))];
        _slots = [.. Enumerable.Repeat<int?>(null, count)];

        // Empty numbered spaces along the top.
        var half = PictureCard.Size * scale / 2;
        for (var i = 0; i < count; i++)
        {
            var mark = new Node2D { Position = _slotPositions[i], ZIndex = 2 };
            mark.AddChild(Pen(RoundRect(R(-half, -half, half * 2, half * 2), 26 * scale), (ulong)(960 + i),
                              fill: Palette.Stone.WithAlpha(0.35), ink: Palette.Ink.WithAlpha(0.3), lineWidth: 4, wobble: 3));
            var badge = new Node2D { Position = P(0, half + 44) };
            badge.AddChild(Pen(Ellipse(Vector2.Zero, 34, 34), (ulong)(970 + i), fill: Palette.Sun, lineWidth: 4.5));
            badge.AddChild(Sketch.Letter($"{i + 1}", 46, withShadow: false));
            mark.AddChild(badge);
            Stage.AddChild(mark);
            _slotMarks.Add(mark);
        }

        // Shuffled along the bottom, never already in the right order.
        var identity = Enumerable.Range(0, count).ToList();
        var trayOrder = identity;
        while (count > 1 && trayOrder.SequenceEqual(identity)) trayOrder = Coordinator.Rng.Shuffled(identity);
        _trayPositions = [.. Enumerable.Repeat(Vector2.Zero, count)];
        for (var spot = 0; spot < count; spot++) _trayPositions[trayOrder[spot]] = P(xs[spot], TrayY);

        for (var i = 0; i < count; i++)
        {
            var holder = Buttons.Tappable(new Node2D(), $"card:{i}");
            var picture = new PictureCard(cards[i].Picture, cards[i].Text, (ulong)(980 + _session.RoundsPlayed * 11 + i))
            {
                Scale = Vector2.One * scale,
            };
            holder.AddChild(picture);
            holder.Position = _trayPositions[i];
            holder.ZIndex = 10;
            holder.Modulate = new Color(1, 1, 1, 0);
            var fade = holder.CreateTween();
            fade.TweenInterval(0.08 * i);
            fade.TweenProperty(holder, "modulate:a", 1f, 0.25);
            Stage.AddChild(holder);
            _holders.Add(holder);
            _moves.Add(null);
        }
        InputLocked = false;
        SayPrompt();
    }

    private void SayPrompt()
    {
        Voice.Play([VoiceLine.MorningOrder]);
        Bip.Hop();
    }

    private void Slide(int index, Vector2 to, double seconds, bool shakeFirst = false)
    {
        _moves[index]?.Kill();
        var holder = _holders[index];
        var move = holder.CreateTween();
        if (shakeFirst)
        {
            move.TweenProperty(holder, "rotation", Turn(0.08), 0.05);
            move.TweenProperty(holder, "rotation", Turn(-0.08), 0.08);
            move.TweenProperty(holder, "rotation", 0f, 0.06);
        }
        move.TweenProperty(holder, "position", to, seconds).SetEase(Tween.EaseType.Out);
        _moves[index] = move;
    }

    protected override void HandleTap(string name, Node2D node)
    {
        if (name == "replay")
        {
            Buttons.Press(node);
            SayPrompt();
            return;
        }
        if (_round is not { } round || !name.StartsWith("card:") || !int.TryParse(name["card:".Length..], out var index)
            || index >= _holders.Count) return;
        var holder = _holders[index];
        if (holder == HintedNode) StopHint();

        if (_slots.IndexOf(index) is var placed && placed >= 0)
        {
            // Placed already: back down to the bottom row.
            _slots[placed] = null;
            Sfx.Play(BipSounds.Effect.Pop);
            Slide(index, _trayPositions[index], 0.22);
            return;
        }
        var empty = _slots.IndexOf(null);
        if (empty < 0) return;
        _slots[empty] = index;
        Sfx.Play(BipSounds.Effect.Tick);
        holder.ZIndex = 12;
        Slide(index, _slotPositions[empty], 0.25);
        _moves[index]!.TweenCallback(Callable.From(() => holder.ZIndex = 10));

        var card = round.Set.Cards[index];
        if (_slots.Contains(null))
        {
            Voice.Play([card.Audio]);
            return;
        }
        // Last space filled: say the card, then see if the story is in order.
        InputLocked = true;
        Voice.Play([card.Audio], completion: () => Check(round));
    }

    private List<SequenceCard> CurrentOrder(MorningOrderGame.Round round) =>
        [.. _slots.OfType<int>().Select(i => round.Set.Cards[i])];

    private void Check(MorningOrderGame.Round round)
    {
        var order = CurrentOrder(round);
        var outcome = _attempt.Answer(_game.IsCorrect(order, round));
        if (outcome.Kind == AnswerOutcomeKind.Correct)
        {
            Sfx.Play(BipSounds.Effect.Chime);
            Bip.Celebrate();
            foreach (var holder in _holders) Buttons.Press(holder);
            var change = Record(outcome.FirstTry, _game.SkillId(round));
            Voice.Play([Coordinator.RandomPraise()], completion: () => AfterAnswer(change, _session, AskQuestion));
            return;
        }
        Sfx.Play(BipSounds.Effect.Boop);
        Bip.Tilt();
        var mistake = Enumerable.Range(0, order.Count).FirstOrDefault(i => order[i] != round.Answer[i]);
        SendBack(mistake);
        if (outcome.Kind == AnswerOutcomeKind.TryAgain)
        {
            After(0.5, () => InputLocked = false);
            return;
        }
        // The card that belongs in the first wrong space wiggles, and the routine is said in order.
        var wanted = round.Set.Cards.IndexOf(round.Answer[mistake]);
        After(0.4, () =>
        {
            if (wanted >= 0 && wanted < _holders.Count) ShowHint(_holders[wanted]);
            Voice.Play([Coordinator.RandomHint(), .. round.Answer.Select(c => c.Audio)], completion: () => InputLocked = false);
        });
    }

    /// <summary>Everything from the first mistake on goes back to the bottom row; the right start stays.</summary>
    private void SendBack(int first)
    {
        for (var slot = first; slot < _slots.Count; slot++)
        {
            if (_slots[slot] is not { } index) continue;
            _slots[slot] = null;
            Slide(index, _trayPositions[index], 0.25, shakeFirst: true);
        }
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}
