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
/// Bip's Path: snap arrow blocks into the strip, then press Go. Bip walks the program one step at a
/// time with a highlight on the current block, leaving footprints, so a mistake shows itself. On
/// turning puzzles an arrow shows which way he faces. The take-back button removes the last block
/// and the bin clears the strip. Any working program passes; fewer blocks earns extra sparkles.
/// Wrong → soft boop and keep editing; two misses → the next right block wiggles.
/// </summary>
public partial class BipsPathScreen : GameScreen
{
    /// <summary>Square size. A little smaller than the Mac app's 100, and the grid starts further in,
    /// so the biggest grid (6 × 6) clears the home button and the block palette.</summary>
    private const double Cell = 92;
    /// <summary>The longest strip: two rows of six (the longest content program needs ten).</summary>
    private const int MaxBlocks = 12;
    /// <summary>Little Bip stands on his feet, so he sits a bit below a square's centre to look centred.</summary>
    private static readonly Vector2 TokenOffset = new(0, 44);

    private readonly BipsPathGame _game;
    private readonly GameSession _session;
    private BipsPathGame.Round? _round;
    private QuestionAttempt _attempt = new();
    private Node2D? _grid;
    private Bip? _token;
    private Node2D? _facingArrow;
    private readonly List<string> _strip = [];
    private readonly List<Node2D> _stripNodes = [];
    private readonly List<Node2D> _palette = [];
    private List<string> _paletteBlocks = [];
    private readonly List<Node2D> _trail = [];
    private readonly List<Tween> _wiggles = [];

    public BipsPathScreen(BipsPathGame game)
    {
        _game = game;
        _session = Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Coding;
    protected override string GameId => BipsPathGame.GameId;

    /// <summary>A working program for this puzzle (for the walk-through test).</summary>
    public IReadOnlyList<string> Answer => _round?.Answer ?? [];
    public IReadOnlyList<string> PaletteBlocks => _paletteBlocks;
    public IReadOnlyList<string> Strip => _strip;
    public int FootprintCount => _trail.Count;

    protected override void Build()
    {
        // Bip and the replay button sit bottom right, clear of the biggest grid and the strip.
        AddGameChrome(P(700, -390), P(440, -400), 0.6f);
        var go = Buttons.Tappable(Buttons.Play(), "go");
        go.Position = P(640, -160);
        go.ZIndex = 10;
        Stage.AddChild(go);
        var undo = UndoButton();
        undo.Position = P(260, -160);
        undo.ZIndex = 10;
        Stage.AddChild(undo);
        var clear = ClearButton();
        clear.Position = P(430, -160);
        clear.ZIndex = 10;
        Stage.AddChild(clear);
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
        _grid?.QueueFree();
        foreach (var node in _palette.Concat(_stripNodes)) node.QueueFree();
        _grid = null;
        _token = null;
        _facingArrow = null;
        _trail.Clear();
        _palette.Clear();
        _strip.Clear();
        _stripNodes.Clear();
        _attempt = new QuestionAttempt();
        _round = next;
        DrawLevel(next.Level);
        DrawPalette(next.Level);
        InputLocked = false;
        SayPrompt();
    }

    private static Vector2 CellPoint(GridPosition at) => CellPoint(at.Row, at.Column);

    private static Vector2 CellPoint(int row, int column) =>
        P(-620 + column * Cell + Cell / 2, 290 - row * Cell - Cell / 2);

    private void DrawLevel(GridLevel level)
    {
        var board = new Node2D { ZIndex = 5 };
        for (var row = 0; row < level.Grid.Rows; row++)
        {
            for (var col = 0; col < level.Grid.Cols; col++)
            {
                var square = Pen(RoundRect(R(-(Cell - 8) / 2, -(Cell - 8) / 2, Cell - 8, Cell - 8), 16),
                                 (ulong)(990 + row * 7 + col), fill: Palette.Card, lineWidth: 4);
                square.Position = CellPoint(row, col);
                board.AddChild(square);
            }
        }
        foreach (var rock in level.Rocks)
            board.AddChild(Pen(Ellipse(CellPoint(rock), 34, 30), 991, fill: Palette.Stone, lineWidth: 4));
        var goal = Pen(RoundRect(R(-34, -34, 68, 68), 14), 992, fill: Palette.Go, lineWidth: 4);
        goal.Position = CellPoint(level.Goal);
        board.AddChild(goal);
        var bolt = Pen(Polygon(P(6, 26), P(-12, 2), P(-2, 2), P(-6, -26), P(12, -2), P(2, -2)), 993, fill: Palette.Sun, lineWidth: 3);
        bolt.Position = goal.Position;
        board.AddChild(bolt);
        var token = new Bip(41) { Position = CellPoint(level.Start) + TokenOffset, Scale = Vector2.One * 0.32f, ZIndex = 3 };
        board.AddChild(token);
        if (level.Blocks.Contains("forward"))
        {
            // Turning puzzles: an arrow in front of Bip shows which way "forward" goes.
            var arrow = Pen(Polygon(P(-12, -10), P(12, -10), P(0, 12)), 1003, fill: Palette.Orange, lineWidth: 3);
            arrow.ZIndex = 4;
            board.AddChild(arrow);
            _facingArrow = arrow;
        }
        Stage.AddChild(board);
        _grid = board;
        _token = token;
        PointArrow(GridWalker.ParseFacing(level.StartFacing), CellPoint(level.Start), animated: false);
    }

    /// <summary>Puts the facing arrow just in front of Bip, pointing the way he faces.</summary>
    private void PointArrow(Facing facing, Vector2 cell, bool animated)
    {
        if (_facingArrow is not { } arrow) return;
        var (angle, offset) = facing switch
        {
            Facing.Right => (-Math.PI / 2, P(38, 0)),
            Facing.Down => (Math.PI, P(0, -38)),
            Facing.Left => (Math.PI / 2, P(-38, 0)),
            _ => (0.0, P(0, 38)),
        };
        var target = cell + offset;
        var rotation = Turn(angle);
        if (!animated)
        {
            arrow.Position = target;
            arrow.Rotation = rotation;
            return;
        }
        // Shortest way round, so a quarter turn never spins three quarters.
        var turn = Mathf.Wrap(rotation - arrow.Rotation, -Mathf.Pi, Mathf.Pi);
        var swing = arrow.CreateTween().SetParallel();
        swing.TweenProperty(arrow, "position", target, 0.3);
        swing.TweenProperty(arrow, "rotation", arrow.Rotation + turn, 0.3);
    }

    /// <summary>Take back the last block: a curly arrow pointing back.</summary>
    private static Node2D UndoButton() => Buttons.Tappable(Group(
        Pen(Ellipse(Vector2.Zero, 64, 64), 1004, fill: Palette.Sun, lineWidth: 5),
        Pen(Arc(P(4, -4), 26, 24, -2.4, 1.6), 1005, lineWidth: 7),
        Pen(Polygon(P(-34, 4), P(-10, 4), P(-22, -18)), 1006, fill: Palette.Ink, lineWidth: 3)), "undo");

    /// <summary>Clear the strip: a little bin.</summary>
    private static Node2D ClearButton() => Buttons.Tappable(Group(
        Pen(Ellipse(Vector2.Zero, 64, 64), 1007, fill: Palette.Pink, lineWidth: 5),
        Pen(Polygon(P(-20, 16), P(20, 16), P(14, -28), P(-14, -28)), 1008, fill: Palette.Card, lineWidth: 4),
        Pen(Polyline(P(-28, 24), P(28, 24)), 1009, lineWidth: 5),
        Pen(Polyline(P(-8, 30), P(8, 30)), 1010, lineWidth: 5)), "clear");

    private void DrawPalette(GridLevel level)
    {
        _paletteBlocks = [.. level.Blocks];
        for (var i = 0; i < level.Blocks.Count; i++)
        {
            var button = Buttons.Tappable(new Node2D(), $"block:{i}");
            button.Position = P((i - (level.Blocks.Count - 1) / 2.0) * 130, -340);
            button.ZIndex = 10;
            button.AddChild(Pen(Ellipse(Vector2.Zero, 56, 56), (ulong)(994 + i), fill: Palette.Sun, lineWidth: 5));
            button.AddChild(BlockIcon(level.Blocks[i]));
            Stage.AddChild(button);
            _palette.Add(button);
        }
    }

    /// <summary>Picture-only blocks: arrows for steps, a curved arrow for turns. No reading needed.</summary>
    private static Node2D BlockIcon(string block)
    {
        var arrow = Pen(Polygon(P(-22, -10), P(6, -10), P(6, -24), P(30, 0), P(6, 24), P(6, 10), P(-22, 10)), 995, fill: Palette.Ink, lineWidth: 3);
        switch (block)
        {
            case "up":
                arrow.Rotation = Turn(Math.PI / 2);
                return arrow;
            case "down":
                arrow.Rotation = Turn(-Math.PI / 2);
                return arrow;
            case "left":
                arrow.Rotation = Turn(Math.PI);
                return arrow;
            case "forward":
                return Group(arrow, Pen(Ellipse(P(-34, 0), 8, 8), 996, fill: Palette.Ink, lineWidth: 2));
            case "turnLeft" or "turnRight":
                var head = Pen(Polygon(P(-10, 22), P(10, 26), P(2, 8)), 998, fill: Palette.Ink, lineWidth: 3);
                if (block == "turnRight") head.Scale = new Vector2(-1, 1);
                return Group(Pen(Arc(Vector2.Zero, 24, 24, -0.6, 2.4), 997, lineWidth: 7), head);
            default:
                return arrow;
        }
    }

    private void DrawStrip()
    {
        foreach (var node in _stripNodes) node.QueueFree();
        _stripNodes.Clear();
        for (var i = 0; i < _strip.Count; i++)
        {
            var node = Buttons.Tappable(new Node2D(), $"strip:{i}");
            // Two rows of six along the top right, clear of the level stars in the middle.
            node.Position = P(180 + i % 6 * 95, 392 - i / 6 * 85);
            node.ZIndex = 10;
            node.AddChild(Pen(RoundRect(R(-40, -40, 80, 80), 16), (ulong)(999 + i), fill: Palette.LightTeal, lineWidth: 4));
            var icon = BlockIcon(_strip[i]);
            icon.Scale *= 0.8f;
            node.AddChild(icon);
            Stage.AddChild(node);
            _stripNodes.Add(node);
        }
    }

    private void AddBlock(string block)
    {
        if (!_paletteBlocks.Contains(block) || _strip.Count >= MaxBlocks)
        {
            Sfx.Play(BipSounds.Effect.Boop);
            return;
        }
        Sfx.Play(BipSounds.Effect.Tick);
        _strip.Add(block);
        DrawStrip();
    }

    private void TakeBack(bool all)
    {
        if (_strip.Count == 0)
        {
            Sfx.Play(BipSounds.Effect.Boop);
            return;
        }
        Sfx.Play(BipSounds.Effect.Tick);
        if (all) _strip.Clear();
        else _strip.RemoveAt(_strip.Count - 1);
        DrawStrip();
    }

    /// <summary>
    /// The whole game from the keyboard: arrows snap blocks in (1-7 picks from the palette too),
    /// Enter or Space presses Go, Backspace takes the last block back. Other keys replay.
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key || key.Keycode == Key.Escape)
        {
            base._UnhandledInput(@event);
            return;
        }
        GetViewport().SetInputAsHandled();
        if (InputLocked || _round is not { } round) return;
        switch (key.Keycode)
        {
            case Key.Up: AddBlock("up"); break;
            case Key.Down: AddBlock("down"); break;
            case Key.Left: AddBlock("left"); break;
            case Key.Right: AddBlock("right"); break;
            case Key.Enter or Key.KpEnter or Key.Space: PressGo(round); break;
            case Key.Backspace: if (_strip.Count > 0) TakeBack(all: false); break;
            case >= Key.Key1 and <= Key.Key7:
                var index = (int)(key.Keycode - Key.Key1);
                if (index < _paletteBlocks.Count) AddBlock(_paletteBlocks[index]);
                break;
            default: ReplayPrompt(); break;
        }
    }

    private void SayPrompt()
    {
        Voice.Play([VoiceLine.BipsPath]);
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
        if (name.StartsWith("block:") && int.TryParse(name["block:".Length..], out var block) && block < _paletteBlocks.Count)
        {
            AddBlock(_paletteBlocks[block]);
            return;
        }
        if (name.StartsWith("strip:") && int.TryParse(name["strip:".Length..], out var placed) && placed < _strip.Count)
        {
            Sfx.Play(BipSounds.Effect.Tick);
            _strip.RemoveAt(placed);
            DrawStrip();
            return;
        }
        if (name is "undo" or "clear")
        {
            Buttons.Press(node);
            TakeBack(all: name == "clear");
            return;
        }
        if (name == "go" && _round is { } round)
        {
            Buttons.Press(node);
            PressGo(round);
        }
    }

    private void PressGo(BipsPathGame.Round round)
    {
        if (_strip.Count == 0)
        {
            Sfx.Play(BipSounds.Effect.Boop);
            return;
        }
        InputLocked = true;
        StopWiggles();
        ClearTrail();
        var (positions, crashed) = GridWalker.Path(_strip, round.Level);
        var facings = GridWalker.Facings(_strip, round.Level);
        var won = !crashed && positions.Count > 0 && positions[^1] == round.Level.Goal;
        Walk(positions, facings, 1, () =>
        {
            if (won) Win(round);
            else Miss(round);
        });
    }

    /// <summary>One step of the walk: the block lights up, Bip moves (or turns) and leaves a footprint.</summary>
    private void Walk(List<GridPosition> positions, List<Facing> facings, int step, Action done)
    {
        if (_token is not { } token || step >= positions.Count)
        {
            done();
            return;
        }
        if (step - 1 < _stripNodes.Count)
        {
            var lit = _stripNodes[step - 1];
            var glow = lit.CreateTween();
            glow.TweenProperty(lit, "scale", Vector2.One * 1.2f, 0.15);
            glow.TweenProperty(lit, "scale", Vector2.One, 0.15);
        }
        Sfx.Play(BipSounds.Effect.Tick);
        var from = positions[step - 1];
        var to = positions[step];
        if (step < facings.Count) PointArrow(facings[step], CellPoint(to), animated: true);
        var move = token.CreateTween();
        if (from == to)
        {
            // A turn: Bip stays put and the arrow swings round.
            move.TweenProperty(token, "rotation", Turn(0.15), 0.1);
            move.TweenProperty(token, "rotation", 0f, 0.15);
            move.TweenInterval(0.1);
        }
        else
        {
            DropFootprint(CellPoint(from));
            move.TweenProperty(token, "position", CellPoint(to) + TokenOffset, 0.35);
        }
        move.TweenCallback(Callable.From(() => Walk(positions, facings, step + 1, done)));
    }

    /// <summary>A little footprint on every square Bip walks off, so the route he took stays visible.</summary>
    private void DropFootprint(Vector2 at)
    {
        if (_grid is not { } grid) return;
        var dot = Pen(Ellipse(Vector2.Zero, 11, 11), (ulong)(1011 + _trail.Count), fill: Palette.Teal, lineWidth: 2.5);
        dot.Position = at;
        dot.ZIndex = 1;
        dot.Scale = Vector2.One * 0.1f;
        grid.AddChild(dot);
        dot.CreateTween().TweenProperty(dot, "scale", Vector2.One, 0.15);
        _trail.Add(dot);
    }

    private void ClearTrail()
    {
        foreach (var dot in _trail) dot.QueueFree();
        _trail.Clear();
    }

    private Vector2 TokenSpot => _token is { } token ? token.Position - TokenOffset : Vector2.Zero;

    private void Win(BipsPathGame.Round round)
    {
        Buttons.Sparkle(TokenSpot, Stage);
        // Fewer blocks earns extra sparkles, but any working program passes.
        if (_strip.Count <= round.Answer.Count) Buttons.Sparkle(TokenSpot, Stage);
        Sfx.Play(BipSounds.Effect.Chime);
        Bip.Celebrate();
        var change = Record(_attempt.Misses == 0, _game.SkillId(round));
        Voice.Play([Coordinator.RandomPraise()], completion: () => AfterAnswer(change, _session, AskQuestion));
    }

    private void Miss(BipsPathGame.Round round)
    {
        _attempt.Answer(false);
        Sfx.Play(BipSounds.Effect.Boop);
        Bip.Tilt();
        if (_token is { } token) Buttons.Shake(token);
        if (_attempt.NeedsHint)
        {
            WiggleNextRightBlock(round);
            After(0.4, () => Voice.Play([Coordinator.RandomHint()]));
        }
        After(0.6, () =>
        {
            ResetToken(round);
            InputLocked = false;
        });
    }

    /// <summary>Wiggles the palette block that comes next in the working program.</summary>
    private void WiggleNextRightBlock(BipsPathGame.Round round)
    {
        var index = _strip.Count;
        for (var i = 0; i < _strip.Count && i < round.Answer.Count; i++)
        {
            if (_strip[i] == round.Answer[i]) continue;
            index = i;
            break;
        }
        var wanted = index < round.Answer.Count ? round.Answer[index] : round.Answer.LastOrDefault() ?? "";
        for (var i = 0; i < _paletteBlocks.Count && i < _palette.Count; i++)
            if (_paletteBlocks[i] == wanted) _wiggles.Add(Buttons.HintWiggle(_palette[i]));
    }

    private void StopWiggles()
    {
        foreach (var wiggle in _wiggles) wiggle.Kill();
        _wiggles.Clear();
        foreach (var block in _palette)
        {
            block.Rotation = 0;
            block.Scale = Vector2.One;
        }
    }

    /// <summary>Bip goes back to the start; the footprints stay until the next Go, to show where it went wrong.</summary>
    private void ResetToken(BipsPathGame.Round round)
    {
        if (_token is not { } token) return;
        token.Position = CellPoint(round.Level.Start) + TokenOffset;
        token.Rotation = 0;
        PointArrow(GridWalker.ParseFacing(round.Level.StartFacing), CellPoint(round.Level.Start), animated: false);
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}
