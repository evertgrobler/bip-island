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
/// Word Builder: build the word in the picture from letter tiles, then tap the green arrow.
/// Tap a tile to send it to the next empty space (tap it again to take it back), or drag it to any
/// space. Each tile says its sound as it lands. Spare tiles wait in the bank (more at higher
/// levels). Wrong → soft boop and try again; two misses → the word is said and the wrong spaces
/// wiggle. Right → the tiles light up one by one as the word is sounded out.
/// </summary>
public partial class WordBuilderScreen : GameScreen
{
    /// <summary>Further than this (in canvas points) between press and release is a drag, not a tap.</summary>
    private const float DragDistance = 12;
    /// <summary>A dropped tile lands in the nearest space within this distance, else goes back.</summary>
    private const float DropDistance = 130;

    private readonly WordBuilderGame _game;
    private readonly GameSession _session;
    private WordBuilderGame.Round? _round;
    private QuestionAttempt _attempt = new();
    private PictureCard? _picture;
    private readonly List<Node2D> _slots = [];
    private readonly List<LetterTile?> _slotTiles = [];
    private readonly List<LetterTile> _bankTiles = [];
    private readonly List<Vector2> _bankHomes = [];
    private readonly List<Tween> _wiggles = [];
    private Node2D _nextButton = null!;
    private LetterTile? _dragged;
    private Vector2 _dragOffset;
    private Vector2 _pressPoint;
    private int? _draggedFromSlot;

    public WordBuilderScreen(WordBuilderGame game)
    {
        _game = game;
        _session = Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Words;
    protected override string GameId => WordBuilderGame.GameId;

    /// <summary>Arrows move between the tiles and the green arrow; Enter picks one, like a tap.</summary>
    protected override IReadOnlyList<Node2D> KeyOptions => [.. _bankTiles, _nextButton];

    /// <summary>Tap names of bank tiles that spell the word, in order (for the walk-through test).</summary>
    public List<string> AnswerTileNames()
    {
        var used = new HashSet<LetterTile>();
        var names = new List<string>();
        foreach (var id in _round?.Answer ?? [])
        {
            if (_bankTiles.FirstOrDefault(t => t.Tile.Id == id && !used.Contains(t)) is not { } tile) return [];
            used.Add(tile);
            names.Add(Buttons.TapName(tile)!);
        }
        return names;
    }

    /// <summary>A bank tile that isn't part of the word, if this round has spares (for the walk-through test).</summary>
    public string? SpareTileName()
    {
        var answer = AnswerTileNames();
        return _bankTiles.Select(t => Buttons.TapName(t)!).FirstOrDefault(n => !answer.Contains(n));
    }

    /// <summary>Drags a tile to a space through the real mouse handling (for the walk-through test).</summary>
    public void DragForTest(string tileName, int slot)
    {
        if (FindTappable(tileName) is not LetterTile tile || slot >= _slots.Count) return;
        var from = Stage.ToGlobal(tile.Position);
        var to = Stage.ToGlobal(_slots[slot].Position);
        _UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, GlobalPosition = from, Position = from });
        _UnhandledInput(new InputEventMouseMotion { GlobalPosition = (from + to) / 2, Position = (from + to) / 2, ButtonMask = MouseButtonMask.Left });
        _UnhandledInput(new InputEventMouseMotion { GlobalPosition = to, Position = to, ButtonMask = MouseButtonMask.Left });
        _UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, GlobalPosition = to, Position = to });
    }

    /// <summary>True while a tile follows the pointer (for the walk-through test).</summary>
    public bool Dragging => _dragged != null;

    /// <summary>Presses on a tile, then moves the pointer with the button already let go (for the walk-through test).</summary>
    public void LostReleaseForTest(string tileName)
    {
        if (FindTappable(tileName) is not LetterTile tile) return;
        var from = Stage.ToGlobal(tile.Position);
        _UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, GlobalPosition = from, Position = from });
        _UnhandledInput(new InputEventMouseMotion { GlobalPosition = from + new Vector2(300, 0), Position = from + new Vector2(300, 0) });
    }

    /// <summary>What the spaces hold now (tile ids, "" for empty), for the walk-through test.</summary>
    public IReadOnlyList<string> Spelling => CurrentSpelling();

    protected override void Build()
    {
        AddGameChrome(P(600, -400));
        _nextButton = Buttons.Next();
        _nextButton.Position = P(600, -180);
        _nextButton.ZIndex = 10;
        Stage.AddChild(_nextButton);
        After(StartDelay, AskQuestion);
    }

    private void AskQuestion()
    {
        if (_session.NextRound(_game, Coordinator.LearnerFor(GameId), Coordinator.Rng) is not { } next)
        {
            EndVisit(Ending.RoundDone);
            return;
        }
        StopWiggles();
        _picture?.QueueFree();
        foreach (var node in _slots.Concat(_bankTiles)) node.QueueFree();
        _picture = null;
        _slots.Clear();
        _slotTiles.Clear();
        _bankTiles.Clear();
        _bankHomes.Clear();
        _dragged = null;
        _attempt = new QuestionAttempt();
        ResetKeys();
        _round = next;

        var card = Buttons.Tappable(new PictureCard(next.Word.Picture ?? $"pic_{next.Word.Text}", next.Word.Text,
                                                    (ulong)(970 + _session.RoundsPlayed)), "picture");
        card.Position = P(-420, 120);
        card.ZIndex = 10;
        Stage.AddChild(card);
        _picture = card;

        var count = next.Answer.Count;
        for (var i = 0; i < count; i++)
        {
            var slot = Pen(RoundRect(R(-58, -58, 116, 116), 20), (ulong)(971 + i), fill: Palette.Card, lineWidth: 5);
            slot.Position = P((i - (count - 1) / 2.0) * 140 - 40, 180);
            slot.ZIndex = 5;
            Stage.AddChild(slot);
            _slots.Add(slot);
            _slotTiles.Add(null);
        }

        var spacing = next.Bank.Count > 6 ? 128 : 140;
        for (var i = 0; i < next.Bank.Count; i++)
        {
            var tile = Buttons.Tappable(new LetterTile(next.Bank[i]), $"tile:{i}");
            tile.Position = P((i - (next.Bank.Count - 1) / 2.0) * spacing - 40, -180);
            tile.ZIndex = 10;
            Stage.AddChild(tile);
            _bankTiles.Add(tile);
            _bankHomes.Add(tile.Position);
        }
        InputLocked = false;
        SayPrompt();
    }

    private void SayPrompt()
    {
        if (_round is not { } round) return;
        Voice.Play([VoiceLine.WordBuilder, AudioCatalogue.WordClip(round.Word.Text)]);
        Bip.Hop();
    }

    // Tapping and dragging

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press
                when !InputLocked && _dragged == null && TileAt(press.GlobalPosition) is { } tile:
                GetViewport().SetInputAsHandled();
                StartDrag(tile, Stage.ToLocal(press.GlobalPosition));
                return;
            // The button was let go where we couldn't see it (outside the window, or behind the
            // parent gate): drop the tile where it is now.
            case InputEventMouseMotion motion when _dragged != null && !motion.ButtonMask.HasFlag(MouseButtonMask.Left):
                GetViewport().SetInputAsHandled();
                EndDrag(Stage.ToLocal(motion.GlobalPosition));
                return;
            case InputEventMouseMotion motion when _dragged != null:
                GetViewport().SetInputAsHandled();
                if (!InputLocked) _dragged.Position = Stage.ToLocal(motion.GlobalPosition) + _dragOffset;
                return;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release when _dragged != null:
                GetViewport().SetInputAsHandled();
                EndDrag(Stage.ToLocal(release.GlobalPosition));
                return;
        }
        base._UnhandledInput(@event);
    }

    /// <summary>The topmost bank tile under a canvas point.</summary>
    private LetterTile? TileAt(Vector2 global) =>
        _bankTiles.LastOrDefault(t => Hit.IsShown(t) && Hit.GlobalArea(t) is { } area && area.HasPoint(global));

    private void StartDrag(LetterTile tile, Vector2 point)
    {
        _dragged = tile;
        _pressPoint = point;
        _dragOffset = tile.Position - point;
        tile.ZIndex = 20;
        tile.Move?.Kill();
        _draggedFromSlot = _slotTiles.IndexOf(tile) is var slot && slot >= 0 ? slot : null;
        if (_draggedFromSlot is { } from) _slotTiles[from] = null;
    }

    private void EndDrag(Vector2 point)
    {
        if (_dragged is not { } tile) return;
        _dragged = null;
        tile.ZIndex = 10;
        if (InputLocked)
        {
            SendHome(tile);
            return;
        }
        if (point.DistanceTo(_pressPoint) < DragDistance)
        {
            // A tap: a tile in a space goes back; a tile in the bank goes to the next space.
            if (_draggedFromSlot != null)
            {
                SendHome(tile);
                Sfx.Play(BipSounds.Effect.Tick);
            }
            else
            {
                PlaceInNextSpace(tile);
            }
            return;
        }
        var best = -1;
        var bestDistance = DropDistance;
        for (var i = 0; i < _slots.Count; i++)
        {
            var distance = tile.Position.DistanceTo(_slots[i].Position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }
        if (best >= 0) Place(tile, best);
        else SendHome(tile);
    }

    /// <summary>Puts a tile in a space and says its sound. A tile already there goes back to the bank.</summary>
    private void Place(LetterTile tile, int index)
    {
        if (_slotTiles[index] is { } old && old != tile) SendHome(old);
        if (_slotTiles.IndexOf(tile) is var was && was >= 0) _slotTiles[was] = null;
        _slotTiles[index] = tile;
        tile.ZIndex = 10;
        tile.MoveTo(_slots[index].Position, 0.15);
        Sfx.Play(BipSounds.Effect.Tick);
        Voice.Play([tile.Tile.SoundClip]);
        if (_slotTiles.All(t => t != null))
        {
            var bounce = _nextButton.CreateTween();
            bounce.TweenProperty(_nextButton, "scale", Vector2.One * 1.15f, 0.15);
            bounce.TweenProperty(_nextButton, "scale", Vector2.One, 0.15);
        }
    }

    private void PlaceInNextSpace(LetterTile tile)
    {
        var empty = _slotTiles.IndexOf(null);
        if (empty < 0)
        {
            SendHome(tile);
            Sfx.Play(BipSounds.Effect.Boop);
            return;
        }
        Place(tile, empty);
    }

    private void SendHome(LetterTile tile)
    {
        if (_slotTiles.IndexOf(tile) is var slot && slot >= 0) _slotTiles[slot] = null;
        var index = _bankTiles.IndexOf(tile);
        if (index >= 0 && index < _bankHomes.Count) tile.MoveTo(_bankHomes[index], 0.2);
    }

    private List<string> CurrentSpelling() => _slotTiles.Select(t => t?.Tile.Id ?? "").ToList();

    protected override void HandleTap(string name, Node2D node)
    {
        if (name == "replay")
        {
            Buttons.Press(node);
            SayPrompt();
            return;
        }
        if (name == "picture" && _round is { } shown)
        {
            Voice.Play([AudioCatalogue.WordClip(shown.Word.Text)]);
            return;
        }
        // Keyboard (and the walk-through): Enter on a tile works like tapping it.
        if (name.StartsWith("tile:") && node is LetterTile tile && _dragged == null)
        {
            if (_slotTiles.Contains(tile))
            {
                SendHome(tile);
                Sfx.Play(BipSounds.Effect.Tick);
            }
            else
            {
                PlaceInNextSpace(tile);
            }
            return;
        }
        if (name != "next" || _round is not { } round) return;
        Buttons.Press(node);
        Check(round);
    }

    private void Check(WordBuilderGame.Round round)
    {
        var spelling = CurrentSpelling();
        if (spelling.Any(s => s.Length == 0))
        {
            Sfx.Play(BipSounds.Effect.Boop);
            return;
        }
        var outcome = _attempt.Answer(_game.IsCorrect(spelling, round));
        if (outcome.Kind == AnswerOutcomeKind.Correct)
        {
            InputLocked = true;
            StopWiggles();
            Buttons.Sparkle(P(-40, 180), Stage);
            Sfx.Play(BipSounds.Effect.Chime);
            Bip.Celebrate();
            var change = Record(outcome.FirstTry, _game.SkillId(round));
            SoundOut(round, () => Voice.Play([Coordinator.RandomPraise()], completion: () => AfterAnswer(change, _session, AskQuestion)));
            return;
        }
        Sfx.Play(BipSounds.Effect.Boop);
        Bip.Tilt();
        StopWiggles();
        for (var i = 0; i < spelling.Count && i < _slots.Count; i++)
        {
            if (spelling[i] == round.Answer[i]) continue;
            if (outcome.Kind == AnswerOutcomeKind.Hint) _wiggles.Add(Buttons.HintWiggle(_slots[i]));
            else Buttons.Shake(_slots[i]);
        }
        if (outcome.Kind == AnswerOutcomeKind.Hint)
            After(0.4, () => Voice.Play([Coordinator.RandomHint(), AudioCatalogue.WordClip(round.Word.Text)]));
    }

    private void StopWiggles()
    {
        foreach (var wiggle in _wiggles) wiggle.Kill();
        _wiggles.Clear();
        foreach (var slot in _slots)
        {
            slot.Rotation = 0;
            slot.Scale = Vector2.One;
        }
    }

    /// <summary>Each tile lights up as its sound is said, then the whole word: s-u-n, sun.</summary>
    private void SoundOut(WordBuilderGame.Round round, System.Action done)
    {
        var placed = _slotTiles.OfType<LetterTile>().ToList();
        for (var i = 0; i < placed.Count; i++)
        {
            var glow = placed[i].CreateTween();
            glow.TweenInterval(0.55 * i);
            glow.TweenProperty(placed[i], "scale", Vector2.One * 1.18f, 0.15);
            glow.TweenProperty(placed[i], "scale", Vector2.One, 0.2);
        }
        Voice.Play([.. round.AnswerTiles.Select(t => t.SoundClip), AudioCatalogue.WordClip(round.Word.Text)], completion: done);
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}

/// <summary>A teal letter tile for Word Builder.</summary>
public partial class LetterTile : Node2D
{
    public WordBuilderGame.Tile Tile { get; }
    /// <summary>The tile's current slide into place, if any.</summary>
    public Tween? Move { get; private set; }

    public LetterTile() => Tile = new WordBuilderGame.Tile("", "", "");

    public LetterTile(WordBuilderGame.Tile tile)
    {
        Tile = tile;
        AddChild(Pen(RoundRect(R(-56, -56, 112, 112), 20), 972, fill: Palette.Teal, lineWidth: 5));
        var size = tile.Text.Length >= 3 ? 50 : tile.Text.Length == 2 ? 62 : 76;
        AddChild(Sketch.Letter(tile.Text, size, Palette.White, withShadow: false));
        Hit.SetArea(this, new Rect2(-60, -60, 120, 120));
    }

    public void MoveTo(Vector2 to, double seconds)
    {
        Move?.Kill();
        Move = CreateTween();
        Move.TweenProperty(this, "position", to, seconds);
    }
}
