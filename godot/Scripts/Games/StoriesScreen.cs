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
/// Bip's Stories (Words Island): a story one scene at a time, each a picture with its sentences under
/// it. The narrator reads each sentence as it lights up; clicking a sentence reads it again, and the
/// green arrow turns the page. Then the questions, one at a time: Bip asks, says each answer as its
/// card lights up, and the child clicks one. Wrong → soft boop, try again; two misses → Bip says a hint
/// and the scene that holds the answer comes back, read again, before the question is asked again.
/// No typing, no microphone, no timers.
/// </summary>
public partial class StoriesScreen : GameScreen
{
    public enum Phase { Reading, Asking }

    /// <summary>Previews: open on the questions instead of the first page.</summary>
    public static bool PreviewQuestions { get; set; }

    private readonly StoriesGame _game;
    private readonly GameSession _session;
    private StoriesGame.Round? _round;
    private QuestionAttempt _attempt = new();
    private readonly List<Node2D> _shown = [];
    private readonly List<Node2D> _lines = [];
    private readonly List<Node2D> _cards = [];
    private Node2D? _next;
    private Tween? _nextPulse;
    private bool _introduced;
    private bool _questionsIntroduced;

    public StoriesScreen(StoriesGame game)
    {
        _game = game;
        _session = Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Words;
    protected override string GameId => StoriesGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions =>
        CurrentPhase == Phase.Reading ? [.. _lines, .. _next is { } next ? [next] : Array.Empty<Node2D>()] : _cards;

    public Phase CurrentPhase { get; private set; }
    /// <summary>The page being read (from 0), for the walk-through.</summary>
    public int Page { get; private set; }
    public int PageCount => _round?.Scenes.Scenes.Count ?? 0;
    public int LineCount => _lines.Count;
    public Story? Story => _round?.Story;
    /// <summary>Which answer card is right (for the walk-through), or -1 when no question is showing.</summary>
    public int RightIndex => _round is { } round && CurrentPhase == Phase.Asking && _cards.Count > 0
        ? round.Choices.ToList().FindIndex(c => _game.IsCorrect(c, round)) : -1;
    /// <summary>True while the hint shows the answer's scene again.</summary>
    public bool ShowingHintScene { get; private set; }

    protected override void Build()
    {
        AddGameChrome(P(640, 130), P(-650, -400), 0.6f);
        After(StartDelay, NextRound);
    }

    private void NextRound()
    {
        if (_session.NextRound(_game, Coordinator.LearnerFor(GameId), Coordinator.Rng) is not { } round)
        {
            EndVisit(Ending.RoundDone);
            return;
        }
        _round = round;
        if (round.NewStory && !PreviewQuestions) ShowPage(0);
        else Ask();
    }

    // Reading

    private void Clear()
    {
        StopHint();
        _nextPulse?.Kill();
        foreach (var node in _shown.Concat(_lines).Concat(_cards)) node.QueueFree();
        _shown.Clear();
        _lines.Clear();
        _cards.Clear();
        _next?.QueueFree();
        _next = null;
        ShowingHintScene = false;
        ResetKeys();
    }

    private void ShowPage(int page)
    {
        if (_round is not { } round) return;
        Clear();
        CurrentPhase = Phase.Reading;
        Page = page;
        var picture = StoryScenes.Scene(round.Scenes.Scenes[page], (ulong)(7000 + page * 120));
        picture.Position = P(0, 150);
        Stage.AddChild(picture);
        _shown.Add(picture);
        var sentences = round.Story.Text[page];
        for (var i = 0; i < sentences.Count; i++)
        {
            var line = SentenceLine(sentences[i], (ulong)(7400 + page * 10 + i), round.Story.Level);
            line.Position = P(-20, -150 - i * 132);
            line.ZIndex = 5;
            Stage.AddChild(line);
            _lines.Add(line);
        }
        _next = Buttons.Next();
        _next.Position = P(640, -300);
        _next.ZIndex = 10;
        Stage.AddChild(_next);
        InputLocked = false;
        ReadPage();
    }

    /// <summary>A sentence on a light card, at least 120 points tall: click it to hear it again.</summary>
    private Node2D SentenceLine(string text, ulong seed, int level)
    {
        var line = Buttons.Tappable(new Node2D(), $"line:{_lines.Count}");
        const float width = 1040, height = 120;
        line.AddChild(Pen(RoundRect(R(-width / 2, -height / 2, width, height), 24), seed, fill: Palette.Card, lineWidth: 4));
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new Vector2(width - 50, height - 10),
            Position = new Vector2(-(width - 50) / 2, -(height - 10) / 2),
        };
        label.AddThemeFontOverride("font", Fonts.Letters);
        label.AddThemeFontSizeOverride("font_size", level <= 2 ? 50 : text.Length > 60 ? 34 : 40);
        label.AddThemeColorOverride("font_color", Palette.Ink);
        line.AddChild(label);
        Hit.SetArea(line, new Rect2(-width / 2, -height / 2, width, height));
        return line;
    }

    private IReadOnlyList<string> PageClips(int page) => _round is { } round ? StoriesGame.SentenceClips(round.Story)[page] : [];

    /// <summary>Reads the page, lighting each sentence as it is said; then the arrow glows.</summary>
    private void ReadPage()
    {
        var clips = PageClips(Page);
        var offset = _introduced ? 0 : 1;
        _nextPulse?.Kill();
        Voice.Play(_introduced ? clips : [VoiceLine.BipsStories, .. clips], onClipStart: (index, _) => Light(index - offset),
                   completion: () =>
                   {
                       Light(-1);
                       if (_next != null && IsInstanceValid(_next)) _nextPulse = Buttons.Pulse(_next);
                   });
        _introduced = true;
        Bip.Hop();
    }

    /// <summary>Highlights one sentence (the one being read), or none.</summary>
    private void Light(int index)
    {
        for (var i = 0; i < _lines.Count; i++)
            _lines[i].Modulate = index < 0 || i == index ? Colors.White : new Color(1, 1, 1, 0.55f);
    }

    // Questions

    private void Ask()
    {
        if (_round is not { } round) return;
        Clear();
        CurrentPhase = Phase.Asking;
        _attempt = new QuestionAttempt();
        var question = Sketch.Label(round.Question.Text, round.Question.Text.Length > 44 ? 40 : 48, Palette.Ink);
        var holder = new Node2D { Position = P(0, 300) };
        holder.AddChild(question);
        Stage.AddChild(holder);
        _shown.Add(holder);

        var count = round.Choices.Count;
        var spacing = count > 2 ? 420.0 : 480.0;
        for (var i = 0; i < count; i++)
        {
            var choice = round.Choices[i];
            Node2D card = choice.Picture is { } picture
                ? new PictureCard(picture, choice.Text, (ulong)(7600 + _session.RoundsPlayed * 7 + i))
                : WordCard(choice.Text, (ulong)(7650 + _session.RoundsPlayed * 7 + i));
            Buttons.Tappable(card, $"answer:{i}");
            card.Position = P((i - (count - 1) / 2.0) * spacing, -80);
            card.ZIndex = 10;
            card.Scale = Vector2.One * 0.01f;
            Stage.AddChild(card);
            _cards.Add(card);
            var pop = card.CreateTween();
            pop.TweenInterval(0.12 * i);
            pop.TweenProperty(card, "scale", Vector2.One * 1.05f, 0.2);
            pop.TweenProperty(card, "scale", Vector2.One, 0.1);
        }
        Sfx.Play(BipSounds.Effect.Beep);
        InputLocked = false;
        SayQuestion();
    }

    private static Node2D WordCard(string text, ulong seed)
    {
        var card = new Node2D();
        const float width = 380, height = 220;
        card.AddChild(Pen(RoundRect(R(-width / 2, -height / 2, width, height), 30), seed, fill: Palette.Card, lineWidth: 6));
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new Vector2(width - 40, height - 30),
            Position = new Vector2(-(width - 40) / 2, -(height - 30) / 2),
        };
        label.AddThemeFontOverride("font", Fonts.Letters);
        label.AddThemeFontSizeOverride("font_size", text.Length > 24 ? 36 : 44);
        label.AddThemeColorOverride("font_color", Palette.Ink);
        card.AddChild(label);
        Hit.SetArea(card, new Rect2(-width / 2, -height / 2, width, height));
        return card;
    }

    private string AnswerClip(StoryAnswer choice) =>
        _round is { } round ? StoriesGame.AnswerClip(round.Story, choice, Coordinator.Content!.AudioIds) : "";

    /// <summary>The question, then each answer as its card lights up (the instruction first, once a visit).</summary>
    private void SayQuestion()
    {
        if (_round is not { } round) return;
        var lead = new List<string>();
        if (!_questionsIntroduced) lead.Add(VoiceLine.StoryQuestions);
        _questionsIntroduced = true;
        lead.Add(StoriesGame.QuestionClip(round.Story, round.Number));
        var answers = round.Choices.Select(AnswerClip).ToList();
        Voice.Play([.. lead, .. answers], onClipStart: (index, _) =>
        {
            var i = index - lead.Count;
            if (i < 0 || i >= _cards.Count) return;
            var lit = _cards[i].CreateTween();
            lit.TweenProperty(_cards[i], "scale", Vector2.One * 1.1f, 0.15);
            lit.TweenProperty(_cards[i], "scale", Vector2.One, 0.25);
        });
        Bip.Hop();
    }

    /// <summary>The hint: the scene that holds the answer comes back, small, above the cards, and is read again.</summary>
    private void ShowHintScene()
    {
        if (_round is not { } round) return;
        var page = Math.Clamp(round.Question.Scene - 1, 0, round.Scenes.Scenes.Count - 1);
        foreach (var node in _shown) node.Visible = false;
        var picture = StoryScenes.Scene(round.Scenes.Scenes[page], (ulong)(7000 + page * 120));
        picture.Position = P(0, 260);
        picture.Scale = Vector2.One * 0.44f;
        Stage.AddChild(picture);
        _shown.Add(picture);
        ShowingHintScene = true;
        Voice.Play([VoiceLine.StoryLookAgain, .. PageClips(page),
                    StoriesGame.QuestionClip(round.Story, round.Number)]);
    }

    protected override void HandleTap(string name, Node2D node)
    {
        if (name == "replay")
        {
            Buttons.Press(node);
            ReplayPrompt();
            return;
        }
        if (name == "next" && CurrentPhase == Phase.Reading)
        {
            Buttons.Press(node);
            Sfx.Play(BipSounds.Effect.Tick);
            Voice.Stop();
            if (_round is { } round && Page + 1 < round.Scenes.Scenes.Count) ShowPage(Page + 1);
            else Ask();
            return;
        }
        if (name.StartsWith("line:", StringComparison.Ordinal) && int.TryParse(name["line:".Length..], out var line)
            && CurrentPhase == Phase.Reading && line < PageClips(Page).Count)
        {
            Buttons.Press(node);
            Voice.Play([PageClips(Page)[line]], onClipStart: (_, _) => Light(line), completion: () => Light(-1));
            return;
        }
        if (!name.StartsWith("answer:", StringComparison.Ordinal) || !int.TryParse(name["answer:".Length..], out var index)
            || index >= _cards.Count || _round is not { } asked) return;
        var card = _cards[index];
        var choice = asked.Choices[index];
        var outcome = _attempt.Answer(_game.IsCorrect(choice, asked));
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
                var change = Record(outcome.FirstTry, _game.SkillId(asked));
                Voice.Play([Coordinator.RandomPraise(), AnswerClip(choice)], completion: () => AfterAnswer(change, _session, NextRound));
                break;
            case AnswerOutcomeKind.TryAgain:
                Sfx.Play(BipSounds.Effect.Boop);
                Buttons.Shake(card);
                Bip.Tilt();
                After(0.4, () => Voice.Play([StoriesGame.QuestionClip(asked.Story, asked.Number)]));
                break;
            default:
                Sfx.Play(BipSounds.Effect.Boop);
                Buttons.Shake(card);
                Bip.Tilt();
                // The scene that holds the answer is the hint; the right card doesn't wiggle, so the child finds it.
                After(0.4, ShowHintScene);
                break;
        }
    }

    public override void ReplayPrompt()
    {
        if (InputLocked) return;
        if (CurrentPhase == Phase.Reading) ReadPage();
        else SayQuestion();
    }
}
