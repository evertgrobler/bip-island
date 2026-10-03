using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Games;

/// <summary>
/// Sound Buttons: push each letter button to hear its sound, then tap the picture of the word they
/// make together. Buttons are free play; only the picture counts towards mastery. Wrong → soft boop
/// and try again; two misses → the word is said and the picture wiggles.
/// </summary>
public partial class SoundButtonsScreen : GameScreen
{
    /// <summary>A little higher and closer together than the Mac app's (±400, -140), so Bip doesn't cover the left card.</summary>
    private static readonly Vector2[] CardPositions = [P(-350, -80), P(0, -80), P(350, -80)];

    private readonly SoundButtonsGame _game;
    private readonly PhonicsCourse _course;
    private readonly GameSession _session;
    private SoundButtonsGame.Round? _round;
    private QuestionAttempt _attempt = new();
    private readonly List<Node2D> _tiles = [];
    private readonly List<PictureCard> _cards = [];
    private List<string> _clips = [];

    public SoundButtonsScreen(SoundButtonsGame game, PhonicsCourse course)
    {
        _game = game;
        _course = course;
        _session = Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Words;
    protected override string GameId => SoundButtonsGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions => _cards;

    /// <summary>Which picture is right (for the walk-through test), or -1 between questions.</summary>
    public int RightIndex => _round == null ? -1 : _round.Choices.ToList().FindIndex(c => _game.IsCorrect(c, _round));

    protected override void Build()
    {
        AddGameChrome(P(600, -330));
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
        foreach (var node in _tiles.Concat<Node2D>(_cards)) node.QueueFree();
        _tiles.Clear();
        _cards.Clear();
        _attempt = new QuestionAttempt();
        ResetKeys();
        _round = next;
        _clips = next.Word.Graphemes.Select(id => _course.Sound(id)?.SoundClip).OfType<string>().ToList();

        var tileCount = System.Math.Max(_clips.Count, 1);
        for (var i = 0; i < next.Word.Graphemes.Count; i++)
        {
            var tile = Buttons.Tappable(new Node2D(), $"tile:{i}");
            tile.Position = P((i - (tileCount - 1) / 2.0) * 170, 220);
            tile.ZIndex = 10;
            tile.AddChild(Pen(Ellipse(Vector2.Zero, 72, 72), (ulong)(950 + i), fill: Palette.Sun, lineWidth: 6));
            tile.AddChild(Sketch.Letter(next.Word.Graphemes[i], 88, shadow: Palette.Orange));
            tile.Scale = Vector2.One * 0.01f;
            Stage.AddChild(tile);
            _tiles.Add(tile);
            var grow = tile.CreateTween();
            grow.TweenInterval(0.1 * i);
            grow.TweenProperty(tile, "scale", Vector2.One, 0.2);
        }
        for (var i = 0; i < next.Choices.Count; i++)
        {
            var choice = next.Choices[i];
            var card = Buttons.Tappable(new PictureCard(choice.Picture, choice.Word, (ulong)(960 + _session.RoundsPlayed * 7 + i)), $"card:{i}");
            card.Position = CardPositions[i % CardPositions.Length];
            card.ZIndex = 10;
            card.Scale = Vector2.One * 0.01f;
            Stage.AddChild(card);
            _cards.Add(card);
            card.CreateTween().TweenProperty(card, "scale", Vector2.One, 0.25);
        }
        Sfx.Play(BipSounds.Effect.Beep);
        InputLocked = false;
        SayPrompt();
    }

    private void SayPrompt()
    {
        Voice.Play([VoiceLine.SoundButtons]);
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
        if (name.StartsWith("tile:") && int.TryParse(name["tile:".Length..], out var tile) && tile < _clips.Count)
        {
            var bounce = node.CreateTween();
            bounce.TweenProperty(node, "scale", Vector2.One * 1.12f, 0.1);
            bounce.TweenProperty(node, "scale", Vector2.One, 0.12);
            Voice.Play([_clips[tile]]);
            return;
        }
        if (_round is not { } round || !name.StartsWith("card:") || !int.TryParse(name["card:".Length..], out var index)
            || index >= round.Choices.Count || index >= _cards.Count) return;
        var right = RightIndex is var r && r >= 0 && r < _cards.Count ? _cards[r] : null;
        AnswerChoice(_attempt, _game.IsCorrect(round.Choices[index], round), _cards[index], right, _game.SkillId(round),
                     [round.Answer.Audio], _session, AskQuestion);
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }
}
