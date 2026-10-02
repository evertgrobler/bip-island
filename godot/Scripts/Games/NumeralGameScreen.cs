using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Games;

/// <summary>
/// The Numbers games' shared ending to a question: three big numerals, tap the one that says how
/// many. Wrong → soft boop and try again; two misses → the right number wiggles and is said again.
/// </summary>
public abstract partial class NumeralGameScreen : GameScreen
{
    private static readonly Vector2[] NumeralPositions = [P(-350, -260), P(0, -260), P(350, -260)];

    protected override Island HomeIsland => Island.Numbers;
    protected GameSession Session { get; }
    protected QuestionAttempt Attempt { get; set; } = new();
    protected List<Node2D> Numerals { get; } = [];

    /// <summary>How many there were in this question.</summary>
    protected abstract int Count { get; }
    protected abstract IReadOnlyList<int> Choices { get; }
    protected abstract bool IsCorrect(int numeral);
    protected abstract string SkillId { get; }
    protected abstract string PromptClip { get; }
    /// <summary>Seeds for the numeral circles' wobble (Count &amp; Tap and Quick Look differ, as in the Mac app).</summary>
    protected abstract ulong NumeralSeed { get; }

    protected NumeralGameScreen(IMiniGame game) => Session = Coordinator.NewSession(game);

    protected override IReadOnlyList<Node2D> KeyOptions => Numerals;

    protected override void Build()
    {
        AddHomeButton();
        AddBip(P(-560, -400), 0.8f);
        var replay = Buttons.Replay();
        replay.Position = P(600, -330);
        replay.ZIndex = 10;
        Stage.AddChild(replay);
        After(StartDelay, AskQuestion);
    }

    /// <summary>Draws the next question, or ends the visit when the session is over.</summary>
    protected abstract void AskQuestion();

    protected void ClearNumerals()
    {
        foreach (var numeral in Numerals) numeral.QueueFree();
        Numerals.Clear();
    }

    protected void SayPrompt()
    {
        Voice.Play([PromptClip]);
        Bip.Hop();
    }

    protected void ShowNumerals()
    {
        for (var i = 0; i < Choices.Count; i++)
        {
            var button = Buttons.Tappable(new Node2D(), $"num:{i}");
            button.Position = NumeralPositions[i % NumeralPositions.Length];
            button.ZIndex = 10;
            button.AddChild(Pen(Ellipse(Vector2.Zero, 95, 95), NumeralSeed + (ulong)i, fill: Palette.Card, lineWidth: 6));
            button.AddChild(Sketch.Letter($"{Choices[i]}", 120, shadow: Palette.Red));
            button.Scale = Vector2.One * 0.01f;
            Stage.AddChild(button);
            Numerals.Add(button);
            button.CreateTween().TweenProperty(button, "scale", Vector2.One, 0.25);
        }
        Sfx.Play(BipSounds.Effect.Chime);
    }

    protected override void HandleTap(string name, Node2D node)
    {
        if (name == "replay")
        {
            Buttons.Press(node);
            SayPrompt();
            return;
        }
        if (name.StartsWith("num:") && int.TryParse(name["num:".Length..], out var index)
            && index < Choices.Count && index < Numerals.Count)
            Answer(Choices[index], Numerals[index]);
    }

    private void Answer(int numeral, Node2D node)
    {
        var outcome = Attempt.Answer(IsCorrect(numeral));
        switch (outcome.Kind)
        {
            case AnswerOutcomeKind.Correct:
                InputLocked = true;
                var grow = node.CreateTween();
                grow.TweenProperty(node, "scale", Vector2.One * 1.15f, 0.15);
                grow.TweenProperty(node, "scale", Vector2.One * 1.05f, 0.1);
                Buttons.Sparkle(node.Position, Stage);
                Sfx.Play(BipSounds.Effect.Chime);
                Bip.Celebrate();
                var change = Record(outcome.FirstTry, SkillId);
                Voice.Play([Coordinator.RandomPraise(), AudioCatalogue.NumberClip(Count)],
                           completion: () => AfterAnswer(change, Session, AskQuestion));
                break;
            default:
                Sfx.Play(BipSounds.Effect.Boop);
                Buttons.Shake(node);
                Bip.Tilt();
                if (outcome.Kind != AnswerOutcomeKind.Hint) break;
                if (RightIndex is var right && right >= 0 && right < Numerals.Count) Buttons.HintWiggle(Numerals[right]);
                After(0.4, () => Voice.Play([Coordinator.RandomHint(), AudioCatalogue.NumberClip(Count)]));
                break;
        }
    }

    /// <summary>Which numeral is right (for the walk-through test), or -1 before the numerals show.</summary>
    public int RightIndex => Numerals.Count == 0 ? -1 : Enumerable.Range(0, Choices.Count).FirstOrDefault(i => IsCorrect(Choices[i]), -1);

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}
