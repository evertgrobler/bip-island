using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using BipIsland.Screens;
using Godot;
using static BipIsland.Drawing.Up;
using Mode = BipCore.BlockTowersGame.Mode;

namespace BipIsland.Games;

/// <summary>
/// Block Towers (Numbers Island): numbers as towers of cubes, in the colour of the child's animal
/// badge. Building rounds (build, make ten, tens and ones): tap the pile to add a cube, tap the
/// tower to take the top one off, tap the tick when it's done. Other rounds: watch the towers join,
/// take cubes off, see them pair up, then tap a numeral, a tower, or odd/even. Everything is a tap:
/// no dragging and no timers. Wrong → soft boop and try again; two misses → the right answer
/// wiggles and Bip says it.
/// </summary>
public partial class BlockTowersScreen : GameScreen
{
    private const double Size = 52;
    private static readonly Vector2 PileAt = P(-380, -300);
    private static readonly Vector2 TickAt = P(380, -300);
    private static readonly Vector2 CardAt = P(-450, 170);

    private readonly BlockTowersGame _game;
    private readonly GameSession _session;
    private BlockTowersGame.Round? _round;
    private QuestionAttempt _attempt = new();
    private readonly List<Node2D> _choices = [];
    private readonly List<Node2D> _extras = [];
    private readonly List<Node2D> _keys = [];
    private Node2D? _tower;
    private Node2D? _pile;
    private Node2D? _tensPile;
    private Node2D? _tensGroup;
    private Node2D? _onesGroup;
    private Node2D? _tick;
    private readonly Color _colour;
    private readonly Color _other;

    /// <summary>Building rounds: cubes the child has put on (make ten: on top of the tower they were given).</summary>
    private int _built;
    private int _tens;
    private int _ones;
    /// <summary>Take-away rounds: cubes taken off so far.</summary>
    private int _taken;

    public BlockTowersScreen(BlockTowersGame game)
    {
        _game = game;
        _session = Coordinator.NewSession(game);
        _colour = Coordinator.CurrentChild?.Avatar is { } avatar && Avatars.Colours.TryGetValue(avatar, out var c) ? c : Palette.Teal;
        // Pale badges (sun, light teal) still need cubes that stand out on the paper.
        if (_colour.V > 0.85f && _colour.S < 0.6f) _colour = _colour.Darkened(0.15f);
        _other = BlockDrawing.Lighter(_colour);
    }

    protected override Island HomeIsland => Island.Numbers;
    protected override string GameId => BlockTowersGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions => _keys;

    /// <summary>The question on screen (for the walk-through test), or null between questions.</summary>
    public BlockTowersGame.Round? Round => _round;

    /// <summary>Which choice is right (for the walk-through test), or -1 when the choices aren't showing.</summary>
    public int RightIndex => _round == null || _choices.Count == 0 ? -1 : _round.Choices.ToList().FindIndex(c => _game.IsCorrect(c, _round));

    protected override void Build()
    {
        AddGameChrome(P(640, -330), P(-640, -400), 0.7f);
        After(StartDelay, AskQuestion);
    }

    private ulong Seed(int i) => (ulong)(3600 + _session.RoundsPlayed * 41 + i);

    private void Clear()
    {
        StopHint();
        foreach (var node in _choices.Concat(_extras)) node.QueueFree();
        foreach (var node in new[] { _tower, _pile, _tensPile, _tensGroup, _onesGroup, _tick }) node?.QueueFree();
        _choices.Clear();
        _extras.Clear();
        _keys.Clear();
        _tower = _pile = _tensPile = _tensGroup = _onesGroup = _tick = null;
        _built = _tens = _ones = _taken = 0;
        _joining = null;
    }

    /// <summary>A board for towers to stand on, with its top at <paramref name="at"/>.</summary>
    private void AddBoard(Vector2 at, double width) => _extras.Add(Add(BlockDrawing.Board(width, Seed(90)), at, 3));

    private T Add<T>(T node, Vector2 at, int z = 5) where T : Node2D
    {
        node.Position = at;
        node.ZIndex = z;
        Stage.AddChild(node);
        return node;
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
            case Mode.Build or Mode.MakeTen: BuildTowerRound(next); break;
            case Mode.TensOnes: BuildTensOnes(next); break;
            case Mode.Which: BuildWhich(next); break;
            case Mode.Join: BuildJoin(next); break;
            case Mode.TakeAway: BuildTakeAway(next); break;
            case Mode.Doubles: BuildDoubles(next); break;
            case Mode.OddEven: BuildOddEven(next); break;
            default: BuildPattern(next); break;
        }
        Sfx.Play(BipSounds.Effect.Beep);
        InputLocked = false;
        SayPrompt();
        // Join rounds: the towers jump together once Bip has said the numbers. A timer, not the end of
        // the sentence: a replay restarts the voice, and the towers must still join.
        if (next.Mode == Mode.Join) After(3.2, () => JoinTowers(next));
    }

    // MARK: - Building rounds

    private void BuildTowerRound(BlockTowersGame.Round round)
    {
        _extras.Add(Add(BlockDrawing.NumberCard(round.Mode == Mode.MakeTen ? BlockTowersGame.Ten : round.Target, Seed(1)), CardAt));
        if (round.Mode == Mode.MakeTen)
            _extras.Add(Add(BlockDrawing.Ghost(round.A, BlockTowersGame.Ten, Size, Seed(2)), P(0, -230), 4));
        _pile = Add(Buttons.Tappable(BlockDrawing.Pile(_colour, Seed(3)), "pile"), PileAt, 10);
        _tick = Add(Buttons.Tappable(BlockDrawing.Tick(Seed(4)), "tick"), TickAt, 10);
        AddBoard(P(0, -230), 260);
        RedrawTower();
        _keys.AddRange([_pile, _tower!, _tick]);
    }

    /// <summary>The tower being built: make ten's starting cubes in the second shade, the child's cubes on top.</summary>
    private void RedrawTower(bool popTop = false)
    {
        if (_round is not { } round) return;
        var keyIndex = _tower != null ? _keys.IndexOf(_tower) : -1;
        _tower?.QueueFree();
        var given = round.Mode == Mode.MakeTen ? round.A : 0;
        var cubes = Enumerable.Repeat(_other, given).Concat(Enumerable.Repeat(_colour, _built)).ToList();
        var tower = Buttons.Tappable(BlockDrawing.Tower(cubes, Size, Seed(10)), "tower");
        Hit.SetArea(tower, BlockDrawing.TowerArea(cubes.Count, Size, 160));
        _tower = Add(tower, P(0, -230), 6);
        if (keyIndex >= 0) _keys[keyIndex] = tower;
        if (popTop && tower.GetChildCount() > 0 && tower.GetChild(tower.GetChildCount() - 1) is Node2D top)
        {
            top.Scale = Vector2.One * 0.3f;
            top.CreateTween().TweenProperty(top, "scale", Vector2.One, 0.15);
        }
    }

    private int MostToAdd(BlockTowersGame.Round round) =>
        round.Mode == Mode.MakeTen ? BlockTowersGame.Ten - round.A : round.MostCubes;

    private void AddCube(BlockTowersGame.Round round)
    {
        if (_built >= MostToAdd(round))
        {
            Sfx.Play(BipSounds.Effect.Boop); // the pile won't make the tower any taller
            return;
        }
        _built += 1;
        Sfx.Play(BipSounds.Effect.Tick);
        RedrawTower(popTop: true);
        // Counting aloud as the cubes go on (make ten counts on from the tower it was given).
        Voice.Play([AudioCatalogue.NumberClip(round.Mode == Mode.MakeTen ? round.A + _built : _built)]);
    }

    private void TakeCubeOff()
    {
        if (_built == 0) return;
        _built -= 1;
        Sfx.Play(BipSounds.Effect.Tick);
        RedrawTower();
    }

    private void BuildTensOnes(BlockTowersGame.Round round)
    {
        _extras.Add(Add(BlockDrawing.NumberCard(round.Target, Seed(1)), P(500, 250)));
        _tensPile = Add(Buttons.Tappable(BlockDrawing.TensPile(_colour, Seed(3)), "tens"), P(-260, -330), 10);
        _pile = Add(Buttons.Tappable(BlockDrawing.Pile(_other, Seed(5)), "ones"), P(130, -330), 10);
        _tick = Add(Buttons.Tappable(BlockDrawing.Tick(Seed(4)), "tick"), P(450, -330), 10);
        AddBoard(P(-240, -170), 520);
        AddBoard(P(260, -170), 160);
        RedrawTensOnes();
        _keys.AddRange([_tensPile, _pile, _tensGroup!, _onesGroup!, _tick]);
    }

    private const double TenSize = 40;

    /// <summary>Tens stand side by side on the left, ones in a column on the right.</summary>
    private void RedrawTensOnes(bool popTen = false, bool popOne = false)
    {
        var tensKey = _tensGroup != null ? _keys.IndexOf(_tensGroup) : -1;
        var onesKey = _onesGroup != null ? _keys.IndexOf(_onesGroup) : -1;
        _tensGroup?.QueueFree();
        _onesGroup?.QueueFree();
        var tens = Buttons.Tappable(new Node2D(), "tensgroup");
        for (var i = 0; i < _tens; i++)
        {
            var stick = BlockDrawing.TenStick(_colour, TenSize, Seed(20 + i));
            stick.Position = P((i - 4) * 50, 0);
            tens.AddChild(stick);
            if (popTen && i == _tens - 1)
            {
                stick.Scale = new Vector2(1, 0.2f);
                stick.CreateTween().TweenProperty(stick, "scale", Vector2.One, 0.2);
            }
        }
        Hit.SetArea(tens, new Rect2(-240, -(float)(TenSize * BlockDrawing.Column + 20), 480, (float)(TenSize * BlockDrawing.Column + 30)));
        _tensGroup = Add(tens, P(-240, -170), 6);
        var ones = Buttons.Tappable(BlockDrawing.Tower(_ones, _other, TenSize, Seed(40)), "onesgroup");
        Hit.SetArea(ones, BlockDrawing.TowerArea(_ones, TenSize, 140));
        if (popOne && ones.GetChildCount() > 0 && ones.GetChild(ones.GetChildCount() - 1) is Node2D top)
        {
            top.Scale = Vector2.One * 0.3f;
            top.CreateTween().TweenProperty(top, "scale", Vector2.One, 0.15);
        }
        _onesGroup = Add(ones, P(260, -170), 6);
        if (tensKey >= 0) _keys[tensKey] = tens;
        if (onesKey >= 0) _keys[onesKey] = ones;
    }

    private void AddTen()
    {
        if (_tens >= 9)
        {
            Sfx.Play(BipSounds.Effect.Boop);
            return;
        }
        _tens += 1;
        Sfx.Play(BipSounds.Effect.Tick);
        RedrawTensOnes(popTen: true);
    }

    /// <summary>A one goes on; the tenth one snaps the ones together into a ten.</summary>
    private void AddOne()
    {
        if (_ones == 9 && _tens >= 9)
        {
            Sfx.Play(BipSounds.Effect.Boop);
            return;
        }
        _ones += 1;
        Sfx.Play(BipSounds.Effect.Tick);
        if (_ones < BlockDrawing.Column)
        {
            RedrawTensOnes(popOne: true);
            return;
        }
        RedrawTensOnes();
        InputLocked = true;
        After(0.35, () =>
        {
            _ones = 0;
            _tens += 1;
            Sfx.Play(BipSounds.Effect.Whirr);
            RedrawTensOnes(popTen: true);
            InputLocked = false;
        });
    }

    /// <summary>What the child has built, as the round's choice (cubes, cubes added, or tens and ones).</summary>
    private int BuiltValue(BlockTowersGame.Round round) => round.Mode == Mode.TensOnes ? _tens * 10 + _ones : _built;

    private void Finish(BlockTowersGame.Round round, Node2D tick)
    {
        Buttons.Press(tick);
        var value = BuiltValue(round);
        if (value == 0 && round.Mode != Mode.MakeTen) return; // nothing built yet: nothing to check
        var correct = _game.IsCorrect(value.ToString(), round);
        AnswerChoice(_attempt, correct, tick, _pile, _game.SkillId(round), SayWhenRight(round), _session, AskQuestion);
    }

    // MARK: - Rounds with choices

    /// <summary>Three numerals along the bottom, as in the other Numbers games.</summary>
    private void ShowNumerals(BlockTowersGame.Round round)
    {
        for (var i = 0; i < round.Choices.Count; i++)
        {
            var button = Buttons.Tappable(new Node2D(), $"choice:{i}");
            button.AddChild(Pen(Ellipse(Vector2.Zero, 95, 95), Seed(60 + i), fill: Palette.Card, lineWidth: 6));
            button.AddChild(Sketch.Letter(round.Choices[i], 120, shadow: Palette.Red));
            button.Scale = Vector2.One * 0.01f;
            _choices.Add(Add(button, P((i - 1) * 350, -330), 10));
            button.CreateTween().TweenProperty(button, "scale", Vector2.One, 0.25);
        }
        _keys.Clear();
        _keys.AddRange(_choices);
        Sfx.Play(BipSounds.Effect.Chime);
    }

    private void BuildWhich(BlockTowersGame.Round round)
    {
        const double size = 44;
        // The number to find sits to the right, clear of the three towers.
        if (round.Ask == BlockTowersGame.Ask.Number) _extras.Add(Add(BlockDrawing.NumberCard(round.Target, Seed(1)), P(640, 170)));
        var towers = round.Towers!;
        var height = Math.Min(towers.Max(), BlockDrawing.Column) * size + 70;
        for (var i = 0; i < towers.Count; i++)
        {
            var choice = Buttons.Tappable(new Node2D(), $"choice:{i}");
            choice.AddChild(Pen(RoundRect(R(-130, -25, 260, height), 30), Seed(30 + i), fill: Palette.Card.WithAlpha(0.7f), lineWidth: 5));
            choice.AddChild(BlockDrawing.Tower(towers[i], _colour, size, Seed(40 + i * 31)));
            _choices.Add(Add(choice, P((i - 1) * 360, -240), 10));
        }
        _keys.AddRange(_choices);
    }

    private void BuildJoin(BlockTowersGame.Round round)
    {
        AddBoard(P(0, -170), 760);
        var a = Add(BlockDrawing.Tower(round.A, _colour, Size, Seed(10)), P(-260, -170));
        var b = Add(BlockDrawing.Tower(round.B, _other, Size, Seed(50)), P(260, -170));
        _extras.AddRange([a, b]);
        _joining = (a, b);
    }

    private (Node2D A, Node2D B)? _joining;

    /// <summary>Join rounds: the second tower jumps onto the first, then the numerals come.</summary>
    private void JoinTowers(BlockTowersGame.Round round)
    {
        if (_round != round || _choices.Count > 0 || _joining is not var (a, b)) return;
        _joining = null;
        var jump = b.CreateTween();
        jump.TweenProperty(b, "position", a.Position + P(0, Math.Min(round.A, BlockDrawing.Column) * Size + 40), 0.5)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        jump.TweenCallback(Callable.From(() =>
        {
            if (_round != round) return;
            a.QueueFree();
            b.QueueFree();
            _extras.Remove(a);
            _extras.Remove(b);
            var cubes = Enumerable.Repeat(_colour, round.A).Concat(Enumerable.Repeat(_other, round.B)).ToList();
            _extras.Add(Add(BlockDrawing.Tower(cubes, Size, Seed(80)), P(0, -170)));
            Sfx.Play(BipSounds.Effect.Whirr);
            ShowNumerals(round);
        }));
    }

    private void BuildTakeAway(BlockTowersGame.Round round)
    {
        _extras.Add(Add(BlockDrawing.NumberCard(round.A, Seed(1)), CardAt));
        AddBoard(P(0, -230), 300);
        var tower = Buttons.Tappable(BlockDrawing.Tower(round.A, _colour, Size, Seed(10)), "tower");
        Hit.SetArea(tower, BlockDrawing.TowerArea(round.A, Size, 160));
        _tower = Add(tower, P(0, -230), 6);
        _keys.Add(tower);
    }

    /// <summary>Take-away rounds: the top cube flies off to the side; once enough are off, how many are left?</summary>
    private void TakeAwayCube(BlockTowersGame.Round round)
    {
        if (_tower is not { } tower || _taken >= round.B) return;
        _taken += 1;
        var top = tower.GetChild(round.A - _taken) as Node2D;
        if (top != null)
        {
            var fly = top.CreateTween();
            fly.TweenProperty(top, "position", top.Position + P(320 + 30 * _taken, -60), 0.35);
            fly.Parallel().TweenProperty(top, "modulate:a", 0.25f, 0.35);
        }
        Sfx.Play(BipSounds.Effect.Tick);
        if (_taken < round.B)
        {
            Voice.Play([AudioCatalogue.NumberClip(_taken)]);
            return;
        }
        _keys.Clear();
        Voice.Play([AudioCatalogue.NumberClip(_taken), VoiceLine.HowManyLeft]);
        After(1.0, () =>
        {
            if (_round == round && _choices.Count == 0) ShowNumerals(round);
        });
    }

    private void BuildDoubles(BlockTowersGame.Round round)
    {
        AddBoard(P(0, -170), 420);
        _extras.Add(Add(BlockDrawing.Tower(round.A, _colour, Size, Seed(10)), P(-110, -170)));
        _extras.Add(Add(BlockDrawing.Tower(round.A, _other, Size, Seed(50)), P(110, -170)));
        ShowNumerals(round);
    }

    private void BuildOddEven(BlockTowersGame.Round round)
    {
        const double size = 44;
        _extras.Add(Add(BlockDrawing.NumberCard(round.A, Seed(1)), CardAt));
        AddBoard(P(0, -170), 260);
        _extras.Add(Add(BlockDrawing.Pairs(round.A, _colour, size, Seed(10)), P(0, -170)));
        foreach (var (i, choice) in Indexed(BlockTowersGame.Odd, BlockTowersGame.Even))
        {
            var button = Buttons.Tappable(new Node2D(), $"choice:{i}");
            button.AddChild(Pen(RoundRect(R(-150, -90, 300, 180), 34), Seed(30 + i), fill: Palette.Card, lineWidth: 6));
            // A little picture: every cube with a partner, or one left on its own.
            var picture = BlockDrawing.Pairs(choice == BlockTowersGame.Odd ? 5 : 4, _other, 30, Seed(35 + i * 9));
            picture.Position = P(-70, -60);
            button.AddChild(picture);
            var label = Sketch.Label(choice, 54, Palette.Ink);
            var holder = new Node2D { Position = P(50, 0) };
            holder.AddChild(label);
            button.AddChild(holder);
            _choices.Add(Add(button, P(i == 0 ? -330 : 330, -330), 10));
        }
        _keys.AddRange(_choices);
    }

    private void BuildPattern(BlockTowersGame.Round round)
    {
        const double size = 34;
        AddBoard(P(-335, -150), 600);
        for (var i = 0; i < round.Towers!.Count; i++)
            _extras.Add(Add(BlockDrawing.Tower(round.Towers[i], _colour, size, Seed(10 + i * 23)), P(-560 + i * 150, -150)));
        // The empty space where the next tower goes.
        var gap = new Node2D();
        gap.AddChild(Pen(RoundRect(R(-55, 0, 110, 130), 20), Seed(5), fill: Palette.Card.WithAlpha(0.6f), ink: Palette.Ink.WithAlpha(0.35f), lineWidth: 4, wobble: 3));
        var mark = Sketch.Label("?", 80, Palette.Ink.WithAlpha(0.5f));
        var holder = new Node2D { Position = P(0, 65) };
        holder.AddChild(mark);
        gap.AddChild(holder);
        _extras.Add(Add(gap, P(-110, -150)));
        var heights = round.Choices.Select(int.Parse).ToList();
        var cardHeight = Math.Min(heights.Max(), BlockDrawing.Column) * size + 60;
        for (var i = 0; i < heights.Count; i++)
        {
            var height = heights[i];
            var choice = Buttons.Tappable(new Node2D(), $"choice:{i}");
            choice.AddChild(Pen(RoundRect(R(-90, -25, 180, cardHeight), 26), Seed(30 + i), fill: Palette.Card, lineWidth: 5));
            choice.AddChild(BlockDrawing.Tower(height, _other, size, Seed(40 + i * 29)));
            _choices.Add(Add(choice, P(190 + i * 210, -150), 10));
        }
        _keys.AddRange(_choices);
    }

    // MARK: - What Bip says

    private List<string> Prompt(BlockTowersGame.Round round)
    {
        var n = AudioCatalogue.NumberClip;
        return round.Mode switch
        {
            Mode.Build => [VoiceLine.BuildTower, n(round.Target)],
            Mode.Which => round.Ask switch
            {
                BlockTowersGame.Ask.Tallest => [VoiceLine.TallestTower],
                BlockTowersGame.Ask.Shortest => [VoiceLine.ShortestTower],
                _ => [VoiceLine.FindTower, n(round.Target)],
            },
            Mode.Join => [n(round.A), AudioCatalogue.WordClip("and"), n(round.B), VoiceLine.HowManyAltogether],
            Mode.TakeAway => _taken >= round.B
                ? [n(round.A), VoiceLine.TakeAway, n(round.B), VoiceLine.HowManyLeft]
                : [n(round.A), VoiceLine.TakeAway, n(round.B)],
            Mode.MakeTen => [n(round.A), VoiceLine.MakeTen],
            Mode.Doubles => [VoiceLine.Double, n(round.A), VoiceLine.HowMany],
            Mode.OddEven => [n(round.A), VoiceLine.OddOrEven],
            Mode.TensOnes => [VoiceLine.BuildNumber, n(round.Target)],
            _ => [VoiceLine.WhatComesNext],
        };
    }

    private static List<string> SayWhenRight(BlockTowersGame.Round round)
    {
        var n = AudioCatalogue.NumberClip;
        return round.Mode switch
        {
            Mode.Join => [n(round.A), AudioCatalogue.WordClip("and"), n(round.B), VoiceLine.Make, n(round.Target)],
            Mode.MakeTen => [n(round.A), AudioCatalogue.WordClip("and"), n(round.Target), VoiceLine.Make, n(BlockTowersGame.Ten)],
            Mode.OddEven => [n(round.A), round.A % 2 == 0 ? AudioCatalogue.WordClip("even") : VoiceLine.Odd],
            _ => [n(round.Target)],
        };
    }

    private void SayPrompt()
    {
        if (_round is not { } round) return;
        Bip.Hop();
        Voice.Play(Prompt(round));
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
        switch (name)
        {
            case "pile" when round.Mode is Mode.Build or Mode.MakeTen:
                Buttons.Press(node);
                AddCube(round);
                return;
            case "tower" when round.Mode is Mode.Build or Mode.MakeTen:
                TakeCubeOff();
                return;
            case "tower" when round.Mode == Mode.TakeAway:
                TakeAwayCube(round);
                return;
            case "tens":
                Buttons.Press(node);
                AddTen();
                return;
            case "ones":
                Buttons.Press(node);
                AddOne();
                return;
            case "tensgroup" when _tens > 0:
                _tens -= 1;
                Sfx.Play(BipSounds.Effect.Tick);
                RedrawTensOnes();
                return;
            case "onesgroup" when _ones > 0:
                _ones -= 1;
                Sfx.Play(BipSounds.Effect.Tick);
                RedrawTensOnes();
                return;
            case "tick":
                Finish(round, node);
                return;
        }
        if (!name.StartsWith("choice:", StringComparison.Ordinal) || !int.TryParse(name["choice:".Length..], out var index)
            || index >= round.Choices.Count || index >= _choices.Count) return;
        var right = RightIndex >= 0 && RightIndex < _choices.Count ? _choices[RightIndex] : null;
        AnswerChoice(_attempt, _game.IsCorrect(round.Choices[index], round), _choices[index], right, _game.SkillId(round),
                     SayWhenRight(round), _session, AskQuestion);
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}
