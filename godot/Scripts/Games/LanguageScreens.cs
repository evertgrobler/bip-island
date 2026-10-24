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
/// The Language Island's picture games (Show Me and Bip's Day): Bip says something in the language the
/// child is learning and the child clicks its picture. Wrong → soft boop and Bip says it again; two
/// misses → the right card wiggles and Bip says it in the child's own language, then again in the new one.
/// </summary>
public abstract partial class LanguagePictureScreen : GameScreen
{
    protected readonly GameSession Session;
    private QuestionAttempt _attempt = new();
    private readonly List<Node2D> _cards = [];
    private bool _introduced;

    protected LanguagePictureScreen(IMiniGame game) => Session = Coordinator.NewSession(game);

    protected override Island HomeIsland => Island.Language;
    protected override IReadOnlyList<Node2D> KeyOptions => _cards;

    /// <summary>The next round's pictures (id, word for a placeholder), or null when the visit is over.</summary>
    protected abstract IReadOnlyList<(string Picture, string Word)>? NextRound();
    protected abstract bool IsCorrect(int index);
    protected abstract string SkillId { get; }
    /// <summary>What Bip says first in each visit ("Listen to the Afrikaans word…").</summary>
    protected abstract string Intro { get; }
    /// <summary>The answer's clip name, the same in both languages' folders.</summary>
    protected abstract string AnswerClip { get; }
    protected abstract HomeLanguage Learning { get; }

    /// <summary>Which card is right (for the walk-through test), or -1 between questions.</summary>
    public int RightIndex => Enumerable.Range(0, _cards.Count).FirstOrDefault(IsCorrect, -1);

    private string Said => VoicePlayer.InLanguage(AnswerClip, Learning);

    /// <summary>Three or four cards in a row; five a little closer (the cards are 270 pt).</summary>
    private static Vector2 CardPosition(int index, int count)
    {
        var spacing = count switch { > 4 => 300, > 3 => 350, _ => 400 };
        return P((index - (count - 1) / 2.0) * spacing, 60);
    }

    protected override void Build()
    {
        AddGameChrome(P(600, -330), P(-560, -420));
        After(StartDelay, AskQuestion);
    }

    private void AskQuestion()
    {
        if (NextRound() is not { } pictures)
        {
            EndVisit(Ending.RoundDone);
            return;
        }
        StopHint();
        foreach (var card in _cards) card.QueueFree();
        _cards.Clear();
        _attempt = new QuestionAttempt();
        ResetKeys();
        for (var i = 0; i < pictures.Count; i++)
        {
            var card = Buttons.Tappable(new PictureCard(pictures[i].Picture, pictures[i].Word, (ulong)(980 + Session.RoundsPlayed * 7 + i)), $"card:{i}");
            card.Position = CardPosition(i, pictures.Count);
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
        if (_cards.Count == 0) return;
        Voice.Play(_introduced ? [Said] : [Intro, Said]);
        _introduced = true;
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
        if (!name.StartsWith("card:", StringComparison.Ordinal) || !int.TryParse(name["card:".Length..], out var index) || index >= _cards.Count) return;
        var said = Said;
        var right = RightIndex is var r && r >= 0 ? _cards[r] : null;
        // Wrong: Bip says it again. Two misses: "In English, it's… cat. Kat!"
        AnswerChoice(_attempt, IsCorrect(index), _cards[index], right, SkillId, [said], Session, AskQuestion,
                     onRetry: () => Voice.Play([said]), hint: [VoiceLine.InOurLanguage, AnswerClip, said]);
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}

/// <summary>Show Me ("Wys my"): hear a word in the new language, click its picture.</summary>
public partial class ShowMeScreen : LanguagePictureScreen
{
    private readonly ShowMeGame _game;
    private LanguageWordRound? _round;

    public ShowMeScreen(ShowMeGame game) : base(game) => _game = game;

    protected override string GameId => ShowMeGame.GameId;
    protected override string SkillId => "lang_listen";
    protected override string Intro => VoiceLine.ShowMe;
    protected override string AnswerClip => _round?.Target.Audio ?? "";
    protected override HomeLanguage Learning => _round?.Learning ?? Languages.Learning(Coordinator.Language);

    protected override IReadOnlyList<(string Picture, string Word)>? NextRound()
    {
        _round = Session.NextRound(_game, Coordinator.LearnerFor(GameId), Coordinator.Rng);
        return _round?.Choices.Select(c => (c.Picture, c.In(Coordinator.Language))).ToList();
    }

    protected override bool IsCorrect(int index) => _round is { } round && index < round.Choices.Count && _game.IsCorrect(round.Choices[index], round);
}

/// <summary>Bip's Day ("Bip se dag"): hear what Bip is doing in the new language, click the picture.</summary>
public partial class BipsDayScreen : LanguagePictureScreen
{
    private readonly BipsDayGame _game;
    private BipsDayGame.Round? _round;

    public BipsDayScreen(BipsDayGame game) : base(game) => _game = game;

    protected override string GameId => BipsDayGame.GameId;
    protected override string SkillId => "lang_day";
    protected override string Intro => VoiceLine.BipsDay;
    protected override string AnswerClip => _round?.Target.Audio ?? "";
    protected override HomeLanguage Learning => _round?.Learning ?? Languages.Learning(Coordinator.Language);

    protected override IReadOnlyList<(string Picture, string Word)>? NextRound()
    {
        _round = Session.NextRound(_game, Coordinator.LearnerFor(GameId), Coordinator.Rng);
        return _round?.Choices.Select(c => (c.Picture, c.HomeText)).ToList();
    }

    protected override bool IsCorrect(int index) => _round is { } round && index < round.Choices.Count && _game.IsCorrect(round.Choices[index], round);
}

/// <summary>
/// What Is It? ("Wat is dit?"): a picture, and two or three speech bubbles. Bip says each bubble's word
/// in the new language as it lights up; the child clicks the one that names the picture. Two misses →
/// the right bubble wiggles and Bip says the word in the child's own language, then in the new one.
/// </summary>
public partial class WhatIsItScreen : GameScreen
{
    private readonly WhatIsItGame _game;
    private readonly GameSession _session;
    private LanguageWordRound? _round;
    private QuestionAttempt _attempt = new();
    private readonly List<Node2D> _bubbles = [];
    private Node2D? _picture;
    private bool _introduced;

    public WhatIsItScreen(WhatIsItGame game)
    {
        _game = game;
        _session = Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Language;
    protected override string GameId => WhatIsItGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions => _bubbles;

    /// <summary>Which bubble is right (for the walk-through test), or -1 between questions.</summary>
    public int RightIndex => _round == null || _bubbles.Count == 0 ? -1 : _round.Choices.ToList().FindIndex(c => _game.IsCorrect(c, _round));

    protected override void Build()
    {
        AddGameChrome(P(600, -330), P(-560, -420));
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
        foreach (var bubble in _bubbles) bubble.QueueFree();
        _bubbles.Clear();
        _picture?.QueueFree();
        _attempt = new QuestionAttempt();
        ResetKeys();
        _round = next;

        _picture = new PictureCard(next.Target.Picture, next.Target.In(Coordinator.Language), (ulong)(990 + _session.RoundsPlayed)) { Scale = Vector2.One * 1.3f };
        _picture.Position = P(-300, 60);
        Stage.AddChild(_picture);

        var count = next.Choices.Count;
        for (var i = 0; i < count; i++)
        {
            var word = next.Choices[i].In(next.Learning);
            var bubble = Buttons.Tappable(new Node2D(), $"bubble:{i}");
            bubble.AddChild(Pen(Polygon(P(-150, -20), P(-200, -50), P(-130, -50)), (ulong)(995 + i * 3), fill: Palette.Card, lineWidth: 5));
            bubble.AddChild(Pen(Ellipse(Vector2.Zero, 190, 85), (ulong)(996 + i * 3), fill: Palette.Card, lineWidth: 6));
            // The bubble is 380 wide; long words ("liewenheersbesie") shrink to fit inside it.
            var text = Sketch.FittedLabel(word, 56, 300, Palette.Ink, Fonts.Bold);
            bubble.AddChild(text);
            bubble.Position = P(330, 60 + ((count - 1) / 2.0 - i) * (count > 2 ? 205 : 240));
            bubble.ZIndex = 10;
            Stage.AddChild(bubble);
            _bubbles.Add(bubble);
        }
        Sfx.Play(BipSounds.Effect.Beep);
        InputLocked = false;
        SayPrompt();
    }

    /// <summary>The words, each as its bubble lights up (the instruction first, once a visit).</summary>
    private void SayPrompt()
    {
        if (_round is not { } round) return;
        var words = round.Choices.Select(c => VoicePlayer.InLanguage(c.Audio, round.Learning)).ToList();
        var offset = _introduced ? 0 : 1;
        Voice.Play(_introduced ? words : [VoiceLine.WhatIsIt, .. words], onClipStart: (index, _) =>
        {
            var i = index - offset;
            if (i < 0 || i >= _bubbles.Count) return;
            var lit = _bubbles[i].CreateTween();
            lit.TweenProperty(_bubbles[i], "scale", Vector2.One * 1.1f, 0.15);
            lit.TweenProperty(_bubbles[i], "scale", Vector2.One, 0.25);
        });
        _introduced = true;
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
        if (_round is not { } round || !name.StartsWith("bubble:", StringComparison.Ordinal)
            || !int.TryParse(name["bubble:".Length..], out var index) || index >= _bubbles.Count) return;
        var said = VoicePlayer.InLanguage(round.Target.Audio, round.Learning);
        var right = RightIndex is var r && r >= 0 ? _bubbles[r] : null;
        AnswerChoice(_attempt, _game.IsCorrect(round.Choices[index], round), _bubbles[index], right, _game.SkillId(round),
                     [said], _session, AskQuestion, onRetry: SayPrompt, hint: [VoiceLine.InOurLanguage, round.Target.Audio, said]);
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}
