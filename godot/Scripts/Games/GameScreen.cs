using System;
using System.Collections.Generic;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using BipIsland.Screens;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Games;

/// <summary>
/// What every mini-game screen shares (the game half of the Swift app's BaseScene): the level badge,
/// recording answers, the three ways a visit ends and the star-counting celebration, then back to
/// the game's island.
/// </summary>
public abstract partial class GameScreen : BaseScreen
{
    /// <summary>The island the game lives on: home and the end of a visit go back there.</summary>
    protected abstract Island HomeIsland { get; }
    protected abstract string GameId { get; }

    /// <summary>How long after opening the first question comes (0 for screenshots).</summary>
    public static double StartDelay { get; set; } = 0.5;

    private Node2D? _levelBadge;

    public override void _Ready()
    {
        base._Ready();
        var count = Coordinator.LevelCount(GameId);
        if (count > 1) ShowLevelBadge(Coordinator.CurrentVisit().LevelBefore, count);
    }

    protected override void GoHome() => Coordinator.ShowIsland(HomeIsland);

    /// <summary>Records an answer (first try counts) against its skill and sound, and the game's level.</summary>
    protected MasteryChange Record(bool correct, string skillId, string? soundId = null) =>
        Coordinator.Record(correct, skillId, soundId);

    /// <summary>Top centre: which level of this game the child is on, as filled stars.</summary>
    protected void ShowLevelBadge(int level, int count)
    {
        _levelBadge?.QueueFree();
        var badge = new Node2D { Name = "LevelBadge", Position = P(0, 420), ZIndex = 45 };
        var width = count * 46 + 40;
        badge.AddChild(Pen(RoundRect(R(-width / 2.0, -30, width, 60), 28), 560, fill: Palette.Card, lineWidth: 4));
        for (var i = 0; i < count; i++)
        {
            var x = (i - (count - 1) / 2.0) * 46;
            badge.AddChild(Pen(Polygon(Star(P(x, 0), 18)), (ulong)(561 + i),
                               fill: i <= level ? Palette.Sun : Palette.Stone.WithAlpha(0.5), lineWidth: 3, wobble: 1));
        }
        Stage.AddChild(badge);
        _levelBadge = badge;
    }

    /// <summary>Home, Bip bottom left and the replay button: what every game screen shows.</summary>
    protected void AddGameChrome(Vector2 replayAt, Vector2? bipAt = null, float bipScale = 0.8f)
    {
        AddHomeButton();
        AddBip(bipAt ?? P(-560, -400), bipScale);
        var replay = Buttons.Replay();
        replay.Position = replayAt;
        replay.ZIndex = 10;
        Stage.AddChild(replay);
    }

    // Hints

    private Tween? _hint;
    private Node2D? _hinted;

    /// <summary>The node wiggling for a hint, if any.</summary>
    protected Node2D? HintedNode => _hinted;

    /// <summary>Wiggles one node ("look for the one that's wiggling!") until <see cref="StopHint"/>.</summary>
    protected void ShowHint(Node2D node)
    {
        StopHint();
        _hinted = node;
        _hint = Buttons.HintWiggle(node);
    }

    protected void StopHint()
    {
        _hint?.Kill();
        _hint = null;
        if (_hinted != null && IsInstanceValid(_hinted))
        {
            _hinted.Rotation = 0;
            _hinted.Scale = Vector2.One;
        }
        _hinted = null;
    }

    /// <summary>
    /// A pick-one answer (a numeral, a picture). Right: sparkle, record, praise and
    /// <paramref name="sayWhenRight"/>, then the next question or the end. Wrong: soft boop and try
    /// again; after two misses <paramref name="right"/> wiggles and Bip says a hint and <paramref name="sayWhenRight"/>.
    /// </summary>
    protected void AnswerChoice(QuestionAttempt attempt, bool correct, Node2D node, Node2D? right, string skillId,
                                IReadOnlyList<string> sayWhenRight, GameSession session, Action next, string? soundId = null)
    {
        var outcome = attempt.Answer(correct);
        if (outcome.Kind == AnswerOutcomeKind.Correct)
        {
            InputLocked = true;
            StopHint();
            var grow = node.CreateTween();
            grow.TweenProperty(node, "scale", Vector2.One * 1.15f, 0.15);
            grow.TweenProperty(node, "scale", Vector2.One * 1.05f, 0.1);
            Buttons.Sparkle(node.Position, Stage);
            Sfx.Play(BipSounds.Effect.Chime);
            Bip.Celebrate();
            var change = Record(outcome.FirstTry, skillId, soundId);
            Voice.Play([Coordinator.RandomPraise(), .. sayWhenRight], completion: () => AfterAnswer(change, session, next));
            return;
        }
        Sfx.Play(BipSounds.Effect.Boop);
        Buttons.Shake(node);
        Bip.Tilt();
        if (outcome.Kind != AnswerOutcomeKind.Hint) return;
        if (right != null) ShowHint(right);
        After(0.4, () => Voice.Play([Coordinator.RandomHint(), .. sayWhenRight]));
    }

    protected enum Ending { LevelUp, PractiseAgain, RoundDone }

    /// <summary>After a right answer: a sound level change ends the visit, else the next question (or the end).</summary>
    protected void AfterAnswer(MasteryChange change, GameSession session, Action next)
    {
        switch (change.Kind)
        {
            case MasteryChangeKind.LevelledUp:
                EndVisit(Ending.LevelUp);
                break;
            case MasteryChangeKind.DroppedBack:
                EndVisit(Ending.PractiseAgain);
                break;
            default:
                if (session.IsFinished) EndVisit(Ending.RoundDone);
                else After(0.3, next);
                break;
        }
    }

    protected void EndVisit(Ending ending)
    {
        InputLocked = true;
        if (ending == Ending.LevelUp)
        {
            Sfx.Play(BipSounds.Effect.Whirr);
            Bip.Celebrate();
        }
        var line = ending switch
        {
            Ending.LevelUp => VoiceLine.LevelUp,
            Ending.PractiseAgain => VoiceLine.LetsPractiseAgain,
            _ => VoiceLine.RoundDone,
        };
        Voice.Play([line], completion: () => FinishVisit(() => Coordinator.ShowIsland(HomeIsland, greet: false)));
    }

    /// <summary>
    /// The end of a visit: a star pops in for every star earned, and a level-up gets a bigger burst
    /// and a fuller badge. Then <paramref name="next"/> runs (usually back to the island).
    /// </summary>
    protected void FinishVisit(Action next)
    {
        if (!IsInsideTree()) return;
        var summary = Coordinator.CurrentVisit();
        InputLocked = true;
        var card = new Node2D { Name = "VisitCard", ZIndex = 80, Scale = Vector2.One * 0.01f };
        card.AddChild(Pen(RoundRect(R(-420, -170, 840, 340), 40), 570, fill: Palette.Card, lineWidth: 6));
        Stage.AddChild(card);
        var pop = card.CreateTween();
        pop.TweenProperty(card, "scale", Vector2.One * 1.05f, 0.2);
        pop.TweenProperty(card, "scale", Vector2.One, 0.1);
        Sfx.Play(BipSounds.Effect.Chime);
        Bip.Celebrate();

        var shown = Math.Min(summary.StarsEarned, 10);
        for (var i = 0; i < shown; i++)
        {
            var star = Pen(Polygon(Star(Vector2.Zero, 30)), (ulong)(571 + i), fill: Palette.Sun, lineWidth: 4, wobble: 1);
            star.Position = P((i - (shown - 1) / 2.0) * 70, 40);
            star.Scale = Vector2.One * 0.01f;
            card.AddChild(star);
            var grow = star.CreateTween();
            grow.TweenInterval(0.3 + 0.15 * i);
            grow.TweenCallback(Callable.From(() => Sfx.Play(BipSounds.Effect.Tick)));
            grow.TweenProperty(star, "scale", Vector2.One * 1.2f, 0.12);
            grow.TweenProperty(star, "scale", Vector2.One, 0.08);
        }
        var total = Sketch.Label(summary.StarsEarned > 0 ? $"+{summary.StarsEarned} {(summary.StarsEarned == 1 ? "star" : "stars")}" : "Well played!", 46, Palette.Ink);
        total.Position += P(0, -60);
        card.AddChild(total);

        var wait = 0.6 + 0.15 * shown;
        if (summary.LevelledUp)
        {
            var label = Sketch.Label($"Level {summary.LevelNow + 1}!", 58, Palette.Ink);
            label.Position += P(0, -125);
            label.Modulate = new Color(1, 1, 1, 0);
            card.AddChild(label);
            var fade = label.CreateTween();
            fade.TweenInterval(wait);
            fade.TweenProperty(label, "modulate:a", 1f, 0.2);
            After(wait, () =>
            {
                Sfx.Play(BipSounds.Effect.Whirr);
                Buttons.Sparkle(P(-200, 0), Stage);
                Buttons.Sparkle(P(200, 0), Stage);
                ShowLevelBadge(summary.LevelNow, summary.LevelCount);
                if (_levelBadge is { } badge)
                {
                    var bounce = badge.CreateTween();
                    bounce.TweenProperty(badge, "scale", Vector2.One * 1.3f, 0.2);
                    bounce.TweenProperty(badge, "scale", Vector2.One, 0.2);
                }
            });
            wait += 1.2;
        }
        After(wait + 1.2, next);
    }
}
