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
/// Paint Pots (Art Island). Make rounds: a splodge shows the colour to make; tap two pots and their
/// paint swirls together in the bowl. Predict rounds: Bip holds up two pots — tap the splodge they
/// will make, then they pour into the bowl to check. Mixes are real paint, from art/paints.json.
/// Wrong → the bowl shows what was made, a soft boop, and the bowl empties for another go; two
/// misses → the right pot (or splodge) wiggles and Bip says the colour again.
/// </summary>
public partial class PaintPotsScreen : GameScreen
{
    private static readonly Vector2 BowlAt = P(-150, 150);
    private static readonly Vector2 TargetAt = P(330, 160);

    private readonly PaintPotsGame _game;
    private readonly GameSession _session;
    private PaintPotsGame.Round? _round;
    private QuestionAttempt _attempt = new();
    private readonly List<Node2D> _pots = [];
    private readonly List<Node2D> _splodges = [];
    private readonly List<Node2D> _extras = [];
    private Node2D? _bowl;
    private Node2D? _bowlPaint;
    private int _firstPot = -1;

    public PaintPotsScreen(PaintPotsGame game)
    {
        _game = game;
        _session = Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Art;
    protected override string GameId => PaintPotsGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions => _round?.Mode == PaintPotsGame.Mode.Predict ? _splodges : _pots;

    protected override void Build()
    {
        AddGameChrome(P(640, -330), P(-640, -400), 0.7f);
        _bowl = ArtDrawing.Bowl(3200);
        _bowl.Position = BowlAt;
        _bowl.ZIndex = 4;
        Stage.AddChild(_bowl);
        After(StartDelay, AskQuestion);
    }

    private ulong Seed(int i) => (ulong)(3210 + _session.RoundsPlayed * 31 + i);

    private void Clear()
    {
        StopHint();
        foreach (var node in _pots.Concat(_splodges).Concat(_extras)) node.QueueFree();
        _pots.Clear();
        _splodges.Clear();
        _extras.Clear();
        EmptyBowl();
        _firstPot = -1;
    }

    private void EmptyBowl()
    {
        _bowlPaint?.QueueFree();
        _bowlPaint = null;
    }

    private void FillBowl(Color colour)
    {
        EmptyBowl();
        if (_bowl is not { } bowl) return;
        var paint = ArtDrawing.BowlPaint(colour, Seed(90));
        paint.ZIndex = 1;
        paint.Scale = new Vector2(0.2f, 1);
        bowl.AddChild(paint);
        _bowlPaint = paint;
        var swirl = paint.CreateTween();
        swirl.TweenProperty(paint, "scale", Vector2.One, 0.35);
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
        if (next.Mode == PaintPotsGame.Mode.Make) BuildMake(next);
        else BuildPredict(next);
        Sfx.Play(BipSounds.Effect.Beep);
        InputLocked = false;
        SayPrompt();
    }

    private void BuildMake(PaintPotsGame.Round round)
    {
        // The colour to make, as a big splodge.
        var target = ArtDrawing.Splodge(ArtDrawing.Paint(round.Target), 120, Seed(1));
        target.Position = TargetAt;
        target.ZIndex = 5;
        Stage.AddChild(target);
        _extras.Add(target);
        var spacing = round.Pots.Count > 4 ? 250 : 290;
        for (var i = 0; i < round.Pots.Count; i++)
        {
            var colour = round.Pots[i];
            var pot = Buttons.Tappable(ArtDrawing.Pot(colour, _game.Pot(colour.Id)?.Label ?? "", Seed(10 + i * 5)), $"pot:{i}");
            pot.Position = P((i - (round.Pots.Count - 1) / 2.0) * spacing, -270);
            pot.ZIndex = 10;
            Stage.AddChild(pot);
            _pots.Add(pot);
        }
    }

    private void BuildPredict(PaintPotsGame.Round round)
    {
        // Bip's two pots, held up beside the bowl.
        foreach (var (i, colour) in Indexed(round.First!, round.Second!))
        {
            var pot = ArtDrawing.Pot(colour, _game.Pot(colour.Id)?.Label ?? "", Seed(40 + i * 5));
            pot.Position = P(i == 0 ? 210 : 470, 170);
            pot.Scale = Vector2.One * 0.85f;
            pot.ZIndex = 5;
            Stage.AddChild(pot);
            _extras.Add(pot);
        }
        var plus = Sketch.Label("+", 90, Palette.Ink);
        var holder = new Node2D { Position = P(340, 170), ZIndex = 6 };
        holder.AddChild(plus);
        Stage.AddChild(holder);
        _extras.Add(holder);
        for (var i = 0; i < round.Choices.Count; i++)
        {
            if (_game.Colour(round.Choices[i]) is not { } colour) continue;
            var splodge = Buttons.Tappable(new Node2D(), $"splodge:{i}");
            splodge.AddChild(Pen(RoundRect(R(-140, -125, 280, 250), 34), Seed(60 + i), fill: Palette.Card, lineWidth: 6));
            splodge.AddChild(ArtDrawing.Splodge(ArtDrawing.Paint(colour), 92, Seed(65 + i * 2)));
            splodge.Position = P((i - 1) * 380, -280);
            splodge.ZIndex = 10;
            Stage.AddChild(splodge);
            _splodges.Add(splodge);
        }
    }

    private List<string> Prompt(PaintPotsGame.Round round) => round.Mode == PaintPotsGame.Mode.Make
        ? [VoiceLine.PaintMake, round.Target.Audio]
        : [VoiceLine.PaintPredict, round.First!.Audio, AudioCatalogue.WordClip("and"), round.Second!.Audio];

    /// <summary>"Red and yellow make orange."</summary>
    private static List<string> Sentence(PaintColour a, PaintColour b, PaintColour makes) =>
        [a.Audio, AudioCatalogue.WordClip("and"), b.Audio, VoiceLine.Make, makes.Audio];

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
        if (name.StartsWith("pot:", StringComparison.Ordinal) && int.TryParse(name["pot:".Length..], out var pot) && pot < _pots.Count)
            TapPot(round, pot);
        else if (name.StartsWith("splodge:", StringComparison.Ordinal) && int.TryParse(name["splodge:".Length..], out var index)
                 && index < _splodges.Count)
            TapSplodge(round, index);
    }

    /// <summary>The first pot pours in; the second mixes and is checked. Tapping the first again takes it back.</summary>
    private void TapPot(PaintPotsGame.Round round, int index)
    {
        var node = _pots[index];
        Buttons.Press(node);
        var colour = round.Pots[index];
        if (_firstPot == index)
        {
            _firstPot = -1;
            EmptyBowl();
            Sfx.Play(BipSounds.Effect.Tick);
            return;
        }
        if (_firstPot < 0)
        {
            _firstPot = index;
            Sfx.Play(BipSounds.Effect.Tick);
            FillBowl(ArtDrawing.Paint(colour));
            Voice.Play([colour.Audio]);
            return;
        }
        var first = round.Pots[_firstPot];
        _firstPot = -1;
        var pair = PaintPotsGame.Pair(first.Id, colour.Id);
        var made = _game.Mix(first.Id, colour.Id);
        if (made != null) FillBowl(ArtDrawing.Paint(made));
        var correct = _game.IsCorrect(pair, round);
        var right = RightPot(round);
        if (_bowl is { } bowl)
        {
            var sayRight = correct && made != null ? Sentence(first, colour, made) : [round.Target.Audio];
            AnswerChoice(_attempt, correct, bowl, right, _game.SkillId(round), sayRight, _session, AskQuestion);
        }
        if (!correct)
        {
            // Show what this pair made for a moment, then empty the bowl for another go.
            if (made != null) Voice.Play([made.Audio]);
            After(1.4, () => { if (_round == round) EmptyBowl(); });
        }
    }

    /// <summary>One pot of the right pair (the hint wiggles it).</summary>
    private Node2D? RightPot(PaintPotsGame.Round round)
    {
        var pair = round.Choices.FirstOrDefault(c => _game.IsCorrect(c, round));
        if (pair == null) return null;
        var (a, _) = PaintPotsGame.SplitPair(pair);
        var index = round.Pots.ToList().FindIndex(p => p.Id == a);
        return index >= 0 && index < _pots.Count ? _pots[index] : null;
    }

    private void TapSplodge(PaintPotsGame.Round round, int index)
    {
        var node = _splodges[index];
        var correct = _game.IsCorrect(round.Choices[index], round);
        var rightIndex = round.Choices.ToList().FindIndex(c => _game.IsCorrect(c, round));
        var right = rightIndex >= 0 && rightIndex < _splodges.Count ? _splodges[rightIndex] : null;
        if (correct) FillBowl(ArtDrawing.Paint(round.Target)); // the two pots pour in: it checks out
        AnswerChoice(_attempt, correct, node, right, _game.SkillId(round),
                     correct ? Sentence(round.First!, round.Second!, round.Target) : [round.Target.Audio], _session, AskQuestion);
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}
