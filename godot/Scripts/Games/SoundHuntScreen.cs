using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Games;

/// <summary>
/// Sound Hunt: "Find the picture that starts with… sss" — click the right one of three pictures
/// (four at a higher level). The word to find never repeats in a visit. Wrong → soft boop and the
/// sound again; two misses → spoken hint and the right card wiggles.
/// </summary>
public partial class SoundHuntScreen : GameScreen
{
    private readonly SoundHuntGame _game;
    /// <summary>The sound Bip chose this visit for; a level change on it ends the visit.</summary>
    private readonly PhonicsSound _focus;
    private readonly GameSession _session;
    private SoundHuntGame.Round? _round;
    private QuestionAttempt _attempt = new();
    private readonly List<PictureCard> _cards = [];

    public SoundHuntScreen(SoundHuntGame game, PhonicsSound focus)
    {
        _game = game;
        _focus = focus;
        _session = Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Letters;
    protected override string GameId => SoundHuntGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions => _cards;

    /// <summary>Which card is right (for the walk-through test), or -1 between questions.</summary>
    public int RightIndex => _round == null || _cards.Count == 0 ? -1 : _round.Choices.ToList().FindIndex(c => _game.IsCorrect(c, _round));

    /// <summary>Three pictures 400 apart; four (a higher level) 350 apart, still 270 pt cards.</summary>
    private static Vector2 CardPosition(int index, int count)
    {
        var spacing = count > 3 ? 350 : 400;
        return P((index - (count - 1) / 2.0) * spacing, 60);
    }

    protected override void Build()
    {
        AddGameChrome(P(600, -330), P(-560, -420));
        After(StartDelay, AskQuestion);
    }

    private void AskQuestion()
    {
        // Nothing fresh left to ask (or the visit is over).
        if (_session.NextRound(_game, Coordinator.LearnerFor(GameId, _focus), Coordinator.Rng) is not { } next)
        {
            EndVisit(Ending.RoundDone);
            return;
        }
        StopHint();
        foreach (var card in _cards) card.QueueFree();
        _cards.Clear();
        _attempt = new QuestionAttempt();
        ResetKeys();
        _round = next;
        for (var i = 0; i < next.Choices.Count; i++)
        {
            var choice = next.Choices[i];
            var card = Buttons.Tappable(new PictureCard(choice.Picture, choice.Word, (ulong)(900 + _session.RoundsPlayed * 7 + i)), $"card:{i}");
            card.Position = CardPosition(i, next.Choices.Count);
            card.ZIndex = 10;
            card.Scale = Vector2.One * 0.01f;
            Stage.AddChild(card);
            _cards.Add(card);
            var pop = card.CreateTween();
            pop.TweenInterval(0.12 * i);
            pop.TweenProperty(card, "scale", Vector2.One * 1.08f, 0.2);
            pop.TweenProperty(card, "scale", Vector2.One, 0.1);
        }
        Sfx.Play(BipSounds.Effect.Beep);
        InputLocked = false;
        SayPrompt();
    }

    private void SayPrompt()
    {
        if (_round is not { } round) return;
        Voice.Play([VoiceLine.FindTheSound, round.Target.SoundClip]);
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
        if (_round is not { } round || !name.StartsWith("card:") || !int.TryParse(name["card:".Length..], out var index)
            || index >= round.Choices.Count || index >= _cards.Count) return;
        var card = _cards[index];
        var outcome = _attempt.Answer(_game.IsCorrect(round.Choices[index], round));
        switch (outcome.Kind)
        {
            case AnswerOutcomeKind.Correct:
                InputLocked = true;
                StopHint();
                var grow = card.CreateTween();
                grow.TweenProperty(card, "scale", Vector2.One * 1.15f, 0.15);
                grow.TweenProperty(card, "scale", Vector2.One * 1.05f, 0.1);
                Buttons.Sparkle(card.Position, Stage);
                Sfx.Play(BipSounds.Effect.Chime);
                Bip.Celebrate();
                var change = Record(outcome.FirstTry, _game.SkillId(round), round.Target.Id);
                // Only the sound Bip chose this visit for can end it early.
                var counted = round.Target.Id == _focus.Id ? change : MasteryChange.None;
                Voice.Play([Coordinator.RandomPraise(), round.Answer.Audio], completion: () => AfterAnswer(counted, _session, AskQuestion));
                break;
            case AnswerOutcomeKind.TryAgain:
                Sfx.Play(BipSounds.Effect.Boop);
                Buttons.Shake(card);
                Bip.Tilt();
                After(0.4, () => Voice.Play([round.Target.SoundClip]));
                break;
            default:
                Sfx.Play(BipSounds.Effect.Boop);
                Buttons.Shake(card);
                Bip.Tilt();
                if (RightIndex is var right && right >= 0 && right < _cards.Count) ShowHint(_cards[right]);
                After(0.4, () => Voice.Play([Coordinator.RandomHint(), round.Target.SoundClip]));
                break;
        }
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}
