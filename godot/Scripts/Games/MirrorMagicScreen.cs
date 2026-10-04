using System;
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
/// Mirror Magic (Art Island), on peg boards. Same: which of three pictures is the same on both
/// sides? Finish: half a picture and an empty half — which piece finishes it? Pegs: tap the holes
/// on the empty side to mirror the pegs (one peg per tap; a wrong hole boops, two misses in a row
/// wiggle the next right hole). Line: which of three pictures has its line drawn where the mirror goes?
/// </summary>
public partial class MirrorMagicScreen : GameScreen
{
    private readonly MirrorMagicGame _game;
    private readonly GameSession _session;
    private MirrorMagicGame.Round? _round;
    private QuestionAttempt _attempt = new();
    private readonly List<Node2D> _cards = [];
    private readonly List<Node2D> _extras = [];
    private readonly Dictionary<string, Node2D> _holes = new();
    private readonly HashSet<string> _placed = [];
    private Node2D? _board;
    private int _pegMisses;
    private bool _pegMistake;

    public MirrorMagicScreen(MirrorMagicGame game)
    {
        _game = game;
        _session = Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Art;
    protected override string GameId => MirrorMagicGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions =>
        _round?.Mode == MirrorMagicGame.Mode.Pegs ? _holes.Where(h => !_placed.Contains(h.Key)).Select(h => h.Value).ToList() : _cards;

    /// <summary>Which card is right (for the walk-through test), or -1 between questions and in peg rounds.</summary>
    public int RightIndex => _round == null || _cards.Count == 0 ? -1 : _round.Choices.ToList().FindIndex(c => _game.IsCorrect(c, _round));

    protected override void Build()
    {
        AddGameChrome(P(640, -330), P(-640, -400), 0.7f);
        After(StartDelay, AskQuestion);
    }

    private ulong Seed(int i) => (ulong)(3400 + _session.RoundsPlayed * 37 + i);

    private PaintColour? Colour(string id) => Coordinator.Content?.Paints.Colours.FirstOrDefault(c => c.Id == id);

    private Node2D Grid(IReadOnlyList<string> rows, double cell, int seed, bool board = true) =>
        ArtDrawing.PegGrid(rows, cell, _game.File.Paints, Colour, Seed(seed), board);

    private void Clear()
    {
        StopHint();
        foreach (var node in _cards.Concat(_extras)) node.QueueFree();
        _cards.Clear();
        _extras.Clear();
        _holes.Clear();
        _placed.Clear();
        _board = null;
        _pegMisses = 0;
        _pegMistake = false;
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
        switch (next.Mode)
        {
            case MirrorMagicGame.Mode.Same: BuildSame(next); break;
            case MirrorMagicGame.Mode.Finish: BuildFinish(next); break;
            case MirrorMagicGame.Mode.Pegs: BuildPegs(next); break;
            default: BuildLine(next); break;
        }
        Sfx.Play(BipSounds.Effect.Beep);
        InputLocked = false;
        SayPrompt();
    }

    private Node2D AddCard(int index, Vector2 at, double width, double height)
    {
        var card = Buttons.Tappable(new Node2D(), $"card:{index}");
        card.AddChild(Pen(RoundRect(R(-width / 2, -height / 2, width, height), 30), Seed(index), fill: Palette.Card, lineWidth: 6));
        card.Position = at;
        card.ZIndex = 10;
        Stage.AddChild(card);
        _cards.Add(card);
        return card;
    }

    private void BuildSame(MirrorMagicGame.Round round)
    {
        for (var i = 0; i < round.Candidates.Count; i++)
        {
            var card = AddCard(i, P((i - 1) * 440, 20), 400, 400);
            card.AddChild(Grid(round.Candidates[i], 40, 10 + i));
        }
    }

    private void BuildFinish(MirrorMagicGame.Round round)
    {
        var across = round.Axis == MirrorMagicGame.Horizontal;
        const double cell = 38;
        var n = round.Picture.Count;
        // The board: the shown half, the mirror, and empty holes where the other half goes.
        var board = new Node2D { Position = P(0, 175), ZIndex = 5 };
        var blank = round.Picture.Select(r => new string(MirrorMagicGame.Empty, r.Length)).ToList();
        var shown = round.Picture.Select((row, r) => new string(row.Select((ch, c) =>
            (across ? r < n / 2 : c < row.Length / 2) ? ch : MirrorMagicGame.Empty).ToArray())).ToList();
        board.AddChild(Grid(blank, cell, 20));
        board.AddChild(Grid(shown, cell, 21, board: false));
        board.AddChild(ArtDrawing.MirrorLine(round.Axis, n * cell + 30, Seed(22)));
        Stage.AddChild(board);
        _extras.Add(board);
        _board = board;
        for (var i = 0; i < round.Candidates.Count; i++)
        {
            var card = AddCard(i, P((i - 1) * 380, -290), across ? 330 : 200, across ? 200 : 330);
            card.AddChild(Grid(round.Candidates[i], across ? 36 : 36, 30 + i));
        }
    }

    private void BuildPegs(MirrorMagicGame.Round round)
    {
        const double cell = 112;
        var n = round.Picture.Count;
        var board = new Node2D { Position = P(60, -20), ZIndex = 5 };
        board.AddChild(Grid(round.Picture, cell, 40));
        board.AddChild(ArtDrawing.MirrorLine(round.Axis, n * cell + 40, Seed(41)));
        Stage.AddChild(board);
        _extras.Add(board);
        _board = board;
        foreach (var hole in round.Choices)
        {
            var rc = hole.Split(',').Select(int.Parse).ToArray();
            var spot = Buttons.Tappable(new Node2D(), $"hole:{hole}");
            spot.Position = ArtDrawing.PegCentre(rc[0], rc[1], n, n, cell);
            Hit.SetArea(spot, new Rect2(-(float)cell / 2, -(float)cell / 2, (float)cell, (float)cell));
            spot.ZIndex = 6;
            board.AddChild(spot);
            _holes[hole] = spot;
        }
    }

    private void BuildLine(MirrorMagicGame.Round round)
    {
        var picture = Grid(round.Picture, 36, 50);
        picture.Position = P(0, 190);
        picture.ZIndex = 5;
        Stage.AddChild(picture);
        _extras.Add(picture);
        var n = round.Picture.Count;
        for (var i = 0; i < round.Choices.Count; i++)
        {
            var card = AddCard(i, P((i - 1) * 380, -270), 300, 300);
            card.AddChild(Grid(round.Picture, 28, 60 + i));
            card.AddChild(ArtDrawing.MirrorLine(round.Choices[i], n * 28 + 20, Seed(70 + i)));
        }
    }

    private List<string> Prompt(MirrorMagicGame.Round round) => round.Mode switch
    {
        MirrorMagicGame.Mode.Same => [VoiceLine.MirrorSame],
        MirrorMagicGame.Mode.Finish => [VoiceLine.MirrorFinish],
        MirrorMagicGame.Mode.Pegs => [VoiceLine.MirrorPegs],
        _ => [VoiceLine.MirrorLine],
    };

    private void SayPrompt()
    {
        if (_round is not { } round) return;
        Voice.Play(Prompt(round));
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
        if (_round is not { } round) return;
        if (name.StartsWith("hole:", StringComparison.Ordinal))
        {
            TapHole(round, name["hole:".Length..], node);
            return;
        }
        if (!name.StartsWith("card:", StringComparison.Ordinal) || !int.TryParse(name["card:".Length..], out var index)
            || index >= round.Choices.Count || index >= _cards.Count) return;
        var card = _cards[index];
        var right = RightIndex >= 0 && RightIndex < _cards.Count ? _cards[RightIndex] : null;
        var correct = _game.IsCorrect(round.Choices[index], round);
        if (correct && round.Mode == MirrorMagicGame.Mode.Finish) CompleteBoard(round);
        AnswerChoice(_attempt, correct, card, right, _game.SkillId(round), [], _session, AskQuestion);
    }

    /// <summary>Finish rounds: the right half drops into the empty side of the board.</summary>
    private void CompleteBoard(MirrorMagicGame.Round round)
    {
        if (_board is not { } board) return;
        var whole = Grid(round.Picture, 38, 80, board: false);
        whole.Modulate = new Color(1, 1, 1, 0);
        board.AddChild(whole);
        var fade = whole.CreateTween();
        fade.TweenProperty(whole, "modulate:a", 1f, 0.4);
    }

    /// <summary>Pegs rounds: one peg per right tap; the round is answered when every peg is placed.</summary>
    private void TapHole(MirrorMagicGame.Round round, string hole, Node2D node)
    {
        if (_placed.Contains(hole)) return;
        if (!_game.IsCorrect(hole, round))
        {
            Sfx.Play(BipSounds.Effect.Boop);
            Buttons.Shake(node);
            Bip.Tilt();
            _pegMistake = true;
            _pegMisses += 1;
            if (_pegMisses >= 2)
            {
                if (_holes.TryGetValue(round.PegsToPlace.First(p => !_placed.Contains(p)), out var next)) ShowHint(next);
                After(0.4, () => Voice.Play([Coordinator.RandomHint(followedBySound: false)]));
            }
            return;
        }
        _pegMisses = 0;
        StopHint();
        _placed.Add(hole);
        var colour = Colour(_game.File.Pegs.Colour) is { } paint ? ArtDrawing.Paint(paint) : Palette.Purple;
        var peg = ArtDrawing.Peg(colour, 112, Seed(90 + _placed.Count));
        peg.Scale = Vector2.One * 0.01f;
        node.AddChild(peg);
        var pop = peg.CreateTween();
        pop.TweenProperty(peg, "scale", Vector2.One * 1.15f, 0.12);
        pop.TweenProperty(peg, "scale", Vector2.One, 0.08);
        Sfx.Play(BipSounds.Effect.Tick);
        if (_placed.Count < round.PegsToPlace.Count) return;

        // Every peg is in: the pattern is finished.
        InputLocked = true;
        if (_board is { } board) Buttons.Sparkle(board.Position, Stage);
        Sfx.Play(BipSounds.Effect.Chime);
        Bip.Celebrate();
        var change = Record(!_pegMistake, _game.SkillId(round));
        Voice.Play([Coordinator.RandomPraise()], completion: () => AfterAnswer(change, _session, AskQuestion));
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}
