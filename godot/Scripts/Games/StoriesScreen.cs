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
    private Node2D? _hintScene;
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
    /// <summary>True when the green arrow glows to say the page is read (for the walk-through).</summary>
    public bool ArrowGlows => _nextPulse != null && _nextPulse.IsValid() && _nextPulse.IsRunning();
    /// <summary>Questions asked so far this visit (for the walk-through).</summary>
    public int RoundsPlayed => _session.RoundsPlayed;
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
        _hintScene = null;
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
    private Node2D SentenceLine(string text, ulong seed, int level) =>
        Buttons.Tappable(TextCard(text, 1040, 120, 24, 4, 50, level <= 2 ? 50 : text.Length > 60 ? 34 : 40, seed), $"line:{_lines.Count}");

    private IReadOnlyList<string> PageClips(int page) => _round is { } round ? StoriesGame.SentenceClips(round.Story)[page] : [];

    /// <summary>
    /// Reads the page, lighting each sentence as it is said; then the arrow glows. The intro comes first
    /// until it has been heard to the end (a click on a sentence can cut it short; then the next page says it).
    /// </summary>
    private void ReadPage()
    {
        var clips = PageClips(Page);
        var withIntro = !_introduced;
        var offset = withIntro ? 1 : 0;
        _nextPulse?.Kill();
        Voice.Play(withIntro ? [VoiceLine.BipsStories, .. clips] : clips, onClipStart: (index, _) =>
                   {
                       if (withIntro && index >= 1) _introduced = true;
                       Light(index - offset);
                   },
                   completion: () =>
                   {
                       _introduced = true;
                       PageDone();
                   });
        Bip.Hop();
    }

    /// <summary>The page has been read (or a clicked sentence has): no sentence lit, and the arrow glows to turn the page.</summary>
    private void PageDone()
    {
        Light(-1);
        if (CurrentPhase != Phase.Reading || _next == null || !IsInstanceValid(_next)) return;
        _nextPulse?.Kill();
        _nextPulse = Buttons.Pulse(_next);
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
        var holder = new Node2D { Position = P(0, 330) };
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

    private static Node2D WordCard(string text, ulong seed) => TextCard(text, 380, 220, 30, 6, 40, text.Length > 24 ? 36 : 44, seed);

    /// <summary>
    /// Words on a light hand-drawn card, wrapped and centred, with the whole card as its click area: a story
    /// sentence and a word answer are both made this way.
    /// </summary>
    private static Node2D TextCard(string text, float width, float height, float radius, float lineWidth, float margin, int fontSize, ulong seed)
    {
        var card = new Node2D();
        card.AddChild(Pen(RoundRect(R(-width / 2, -height / 2, width, height), radius), seed, fill: Palette.Card, lineWidth: lineWidth));
        var inner = new Vector2(width - margin, height - 20);
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = inner,
            Position = -inner / 2,
        };
        label.AddThemeFontOverride("font", Fonts.Letters);
        label.AddThemeFontSizeOverride("font_size", fontSize);
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

    /// <summary>
    /// The hint: the scene that holds the answer comes back, small, between the question and the cards, and is
    /// read again. A further miss reads the same scene again rather than adding another.
    /// </summary>
    private void ShowHintScene()
    {
        if (_round is not { } round) return;
        var page = Math.Clamp(round.Question.Scene - 1, 0, round.Scenes.Scenes.Count - 1);
        if (_hintScene == null || !IsInstanceValid(_hintScene))
        {
            _hintScene = StoryScenes.Scene(round.Scenes.Scenes[page], (ulong)(7000 + page * 120));
            _hintScene.Position = P(0, 175);
            _hintScene.Scale = Vector2.One * 0.4f;
            Stage.AddChild(_hintScene);
            _shown.Add(_hintScene);
        }
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
            // Reading one sentence again stops the page read, so the arrow still glows when it ends.
            Voice.Play([PageClips(Page)[line]], onClipStart: (_, _) => Light(line), completion: PageDone);
            return;
        }
        if (!name.StartsWith("answer:", StringComparison.Ordinal) || !int.TryParse(name["answer:".Length..], out var index)
            || index >= _cards.Count || _round is not { } asked) return;
        var choice = asked.Choices[index];
        // The shared answer flow: a retry or the hint never cuts off the praise of a quick right answer.
        // The scene that holds the answer is the hint; the right card doesn't wiggle, so the child finds it.
        AnswerChoice(_attempt, _game.IsCorrect(choice, asked), _cards[index], right: null, _game.SkillId(asked), [AnswerClip(choice)],
                     _session, NextRound, onRetry: () => Voice.Play([StoriesGame.QuestionClip(asked.Story, asked.Number)]),
                     onHint: ShowHintScene);
    }

    public override void ReplayPrompt()
    {
        if (InputLocked) return;
        if (CurrentPhase == Phase.Reading) ReadPage();
        else SayQuestion();
    }
}
