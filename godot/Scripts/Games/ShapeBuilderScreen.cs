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
/// Shape Builder (Art Island): three shape cards to choose from. Depending on the round, find the
/// named shape, fill the gap in a picture made of shapes, pick by sides, watch the shapes turn, count
/// how often one looks the same in a full turn (three numerals), or find the regular shape.
/// Wrong → soft boop and try again; two misses → the right card wiggles and Bip says it again.
/// </summary>
public partial class ShapeBuilderScreen : GameScreen
{
    private const double CardSize = 270;
    private readonly ShapeBuilderGame _game;
    private readonly GameSession _session;
    private ShapeBuilderGame.Round? _round;
    private QuestionAttempt _attempt = new();
    private readonly List<Node2D> _cards = [];
    private readonly List<Node2D> _turning = [];
    private Node2D? _picture;
    private Node2D? _gap;
    private Node2D? _bigShape;

    public ShapeBuilderScreen(ShapeBuilderGame game)
    {
        _game = game;
        _session = Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Art;
    protected override string GameId => ShapeBuilderGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions => _cards;

    /// <summary>Which card is right (for the walk-through test), or -1 between questions.</summary>
    public int RightIndex => _round == null || _cards.Count == 0 ? -1 : _round.Choices.ToList().FindIndex(c => _game.IsCorrect(c, _round));

    protected override void Build()
    {
        AddGameChrome(P(640, -330), P(-640, -400), 0.7f);
        After(StartDelay, AskQuestion);
    }

    private ulong Seed(int i) => (ulong)(3100 + _session.RoundsPlayed * 23 + i);

    private void Clear()
    {
        StopHint();
        foreach (var node in _cards.Concat(_turning)) node.QueueFree();
        _cards.Clear();
        _turning.Clear();
        _picture?.QueueFree();
        _picture = null;
        _gap = null;
        _bigShape?.QueueFree();
        _bigShape = null;
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
        var lowCards = next.Mode is ShapeBuilderGame.Mode.Fill or ShapeBuilderGame.Mode.Count;
        if (next.Mode == ShapeBuilderGame.Mode.Fill) DrawPicture(next);
        if (next.Mode == ShapeBuilderGame.Mode.Count) DrawTurningShape(next);

        // Shape colours are shuffled, so the colour never gives the answer away.
        var colours = Coordinator.Rng.Shuffled(ArtDrawing.ShapeColours).ToList();
        for (var i = 0; i < next.Choices.Count; i++)
        {
            var card = Buttons.Tappable(new Node2D(), $"card:{i}");
            var size = lowCards ? 230 : CardSize;
            card.AddChild(Pen(RoundRect(R(-size / 2, -size / 2, size, size), 30), Seed(i), fill: Palette.Card, lineWidth: 6));
            if (next.Mode == ShapeBuilderGame.Mode.Count)
            {
                card.AddChild(Sketch.Letter(next.Choices[i], 120, shadow: Palette.Purple));
            }
            else if (_game.Shape(next.Choices[i]) is { } shape)
            {
                // Fill rounds: the right shape is drawn just as the gap needs it; the others keep their own
                // proportions (a square drawn the gap's shape would look like a rectangle).
                var fits = next.Mode == ShapeBuilderGame.Mode.Fill && next.Picture is { } pic && shape.Name == next.Target?.Name;
                var (w, h) = fits ? FitPiece(next.Picture!.Pieces[next.Missing], size * 0.68) : ArtDrawing.CardSize(shape, size * 0.62);
                var turn = fits ? next.Picture!.Pieces[next.Missing].Rotation ?? 0 : next.Mode == ShapeBuilderGame.Mode.Fill ? 0 : next.Turns?[i] ?? 0;
                var drawn = ArtDrawing.Shape(shape, w, h, colours[i % colours.Count], Seed(10 + i), turn);
                card.AddChild(drawn);
                if (next.Mode == ShapeBuilderGame.Mode.Turn) _turning.Add(drawn);
            }
            card.Position = P((i - (next.Choices.Count - 1) / 2.0) * (lowCards ? 330 : 400), lowCards ? -300 : -40);
            card.ZIndex = 10;
            card.Scale = Vector2.One * 0.01f;
            Stage.AddChild(card);
            _cards.Add(card);
            var pop = card.CreateTween();
            pop.TweenInterval(0.12 * i);
            pop.TweenProperty(card, "scale", Vector2.One * 1.06f, 0.2);
            pop.TweenProperty(card, "scale", Vector2.One, 0.1);
        }
        Sfx.Play(BipSounds.Effect.Beep);
        InputLocked = false;
        SayPrompt();
    }

    /// <summary>A piece's own width and height, shrunk to fit a card.</summary>
    private static (double W, double H) FitPiece(ShapePiece piece, double size)
    {
        var scale = Math.Min(1, size / Math.Max(piece.W, piece.H));
        return (piece.W * scale, piece.H * scale);
    }

    /// <summary>The picture above the cards, with one piece missing (a pale "?" gap).</summary>
    private void DrawPicture(ShapeBuilderGame.Round round)
    {
        var picture = new Node2D { Position = P(0, 170), Scale = Vector2.One * 0.85f, ZIndex = 5 };
        picture.AddChild(Pen(RoundRect(R(-330, -235, 660, 470), 40), Seed(30), fill: Palette.EggBlue.WithAlpha(0.5), lineWidth: 6));
        for (var i = 0; i < round.Picture!.Pieces.Count; i++)
        {
            var piece = round.Picture.Pieces[i];
            if (_game.Shape(piece.Shape) is not { } shape) continue;
            Node2D part = i == round.Missing
                ? ArtDrawing.Gap(shape, piece.W, piece.H, Seed(40 + i), piece.Rotation ?? 0)
                : ArtDrawing.Shape(shape, piece.W, piece.H, ArtDrawing.Named(piece.Colour), Seed(40 + i), piece.Rotation ?? 0, 5);
            // Pieces are drawn in the picture's order, so a door still shows on top of a missing wall.
            part.Position = P(piece.X, piece.Y);
            picture.AddChild(part);
            if (i == round.Missing) _gap = part;
        }
        Stage.AddChild(picture);
        _picture = picture;
        if (_gap != null) Buttons.Pulse(_gap);
    }

    /// <summary>Count rounds: the shape turns slowly all the way round, above the numerals.</summary>
    private void DrawTurningShape(ShapeBuilderGame.Round round)
    {
        if (round.Target is not { } shape) return;
        var (w, h) = ArtDrawing.CardSize(shape, 300);
        var holder = new Node2D { Position = P(0, 150), ZIndex = 5 };
        // A dot on one corner, so a full turn can be told from no turn at all.
        holder.AddChild(ArtDrawing.Shape(shape, w, h, Palette.Sun, Seed(50)));
        holder.AddChild(Pen(Ellipse(P(0, -h * 0.25), 16, 16), Seed(51), fill: Palette.Red, lineWidth: 3));
        Stage.AddChild(holder);
        _bigShape = holder;
        SpinFull();
    }

    private void SpinFull()
    {
        if (_bigShape is not { } shape) return;
        shape.Rotation = 0;
        var spin = shape.CreateTween();
        spin.TweenInterval(1.2);
        spin.TweenProperty(shape, "rotation", Mathf.Tau, 6.0);
    }

    /// <summary>Turn rounds: every shape makes a slow quarter turn and back.</summary>
    private void TurnQuarter()
    {
        foreach (var shape in _turning)
        {
            var start = shape.Rotation;
            var turn = shape.CreateTween();
            turn.TweenInterval(1.5);
            turn.TweenProperty(shape, "rotation", start - Mathf.Pi / 2, 1.6);
            turn.TweenInterval(1.4);
            turn.TweenProperty(shape, "rotation", start, 0.6);
        }
    }

    private List<string> Prompt(ShapeBuilderGame.Round round) => round.Mode switch
    {
        ShapeBuilderGame.Mode.Find => [VoiceLine.FindTheShape, round.Target!.Audio],
        ShapeBuilderGame.Mode.Fill => [VoiceLine.WhichShapeFits],
        ShapeBuilderGame.Mode.Sides when round.Sides == 0 => [VoiceLine.ShapeCurved],
        ShapeBuilderGame.Mode.Sides => [VoiceLine.ShapeSides, AudioCatalogue.NumberClip(round.Sides), VoiceLine.StraightSides],
        ShapeBuilderGame.Mode.Turn => [VoiceLine.ShapeTurn],
        ShapeBuilderGame.Mode.Count => [VoiceLine.ShapeCountTurns],
        _ => [VoiceLine.ShapeRegular],
    };

    /// <summary>What Bip says with the praise: the shape's name, or the number of times.</summary>
    private List<string> Answer(ShapeBuilderGame.Round round) => round.Mode switch
    {
        ShapeBuilderGame.Mode.Count => [AudioCatalogue.NumberClip(round.Target!.LookSameTurns)],
        ShapeBuilderGame.Mode.Turn or ShapeBuilderGame.Mode.Regular when RightIndex >= 0 && _game.Shape(round.Choices[RightIndex]) is { } s => [s.Audio],
        ShapeBuilderGame.Mode.Sides when RightIndex >= 0 && _game.Shape(round.Choices[RightIndex]) is { } s => [s.Audio],
        _ => round.Target is { } t ? [t.Audio] : [],
    };

    private void SayPrompt()
    {
        if (_round is not { } round) return;
        Voice.Play(Prompt(round));
        Bip.Hop();
        if (round.Mode == ShapeBuilderGame.Mode.Turn) TurnQuarter();
        if (round.Mode == ShapeBuilderGame.Mode.Count) SpinFull();
    }

    protected override void HandleTap(string name, Node2D node)
    {
        if (name == "replay")
        {
            Buttons.Press(node);
            SayPrompt();
            return;
        }
        if (_round is not { } round || !name.StartsWith("card:", StringComparison.Ordinal)
            || !int.TryParse(name["card:".Length..], out var index) || index >= round.Choices.Count || index >= _cards.Count) return;
        var card = _cards[index];
        var right = RightIndex >= 0 && RightIndex < _cards.Count ? _cards[RightIndex] : null;
        var correct = _game.IsCorrect(round.Choices[index], round);
        if (correct && round.Mode == ShapeBuilderGame.Mode.Fill) FillGap(card, round);
        AnswerChoice(_attempt, correct, card, right, _game.SkillId(round), Answer(round), _session, AskQuestion);
    }

    /// <summary>The right shape flies from its card into the gap and takes the picture's colour.</summary>
    private void FillGap(Node2D card, ShapeBuilderGame.Round round)
    {
        if (_picture is not { } picture || _gap is not { } gap || _game.Shape(round.Picture!.Pieces[round.Missing].Shape) is not { } shape) return;
        var piece = round.Picture.Pieces[round.Missing];
        var filled = ArtDrawing.Shape(shape, piece.W, piece.H, ArtDrawing.Named(piece.Colour), Seed(60), piece.Rotation ?? 0, 5);
        filled.Position = gap.Position;
        filled.Scale = Vector2.One * 0.01f;
        picture.AddChild(filled);
        picture.MoveChild(filled, gap.GetIndex());
        gap.Visible = false;
        var grow = filled.CreateTween();
        grow.TweenInterval(0.2);
        grow.TweenProperty(filled, "scale", Vector2.One * 1.1f, 0.2);
        grow.TweenProperty(filled, "scale", Vector2.One, 0.1);
        card.Modulate = new Color(1, 1, 1, 0.4f);
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}
