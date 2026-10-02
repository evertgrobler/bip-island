using System.Collections.Generic;
using BipCore;
using BipIsland.Drawing;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Games;

/// <summary>
/// Quick Look: dots flash briefly (2.5 seconds at first, a little less at higher levels) — how many,
/// without counting? Then tap the numeral, like Count &amp; Tap. The flash is display time, not a
/// countdown: wrong answers still get retries and hints.
/// </summary>
public partial class QuickLookScreen : NumeralGameScreen
{
    /// <summary>Dice-like spots (Mac app coordinates), so small numbers read as patterns.</summary>
    private static readonly (double X, double Y)[] Spots =
    [
        (0, 0), (-90, 90), (90, 90), (-90, -90), (90, -90),
        (-180, 90), (180, 90), (-180, -90), (180, -90), (0, 180),
    ];

    private readonly QuickLookGame _game;
    private QuickLookGame.Round? _round;
    private readonly List<Node2D> _dots = [];

    public QuickLookScreen(QuickLookGame game) : base(game) => _game = game;

    protected override string GameId => QuickLookGame.GameId;
    protected override int Count => _round?.Count ?? 0;
    protected override IReadOnlyList<int> Choices => _round?.Choices ?? [];
    protected override bool IsCorrect(int numeral) => _round != null && _game.IsCorrect(numeral, _round);
    protected override string SkillId => _round != null ? _game.SkillId(_round) : "subitise_5";
    protected override string PromptClip => VoiceLine.QuickLook;
    protected override ulong NumeralSeed => 940;

    protected override void AskQuestion()
    {
        if (Session.NextRound(_game, Coordinator.LearnerFor(GameId), Coordinator.Rng) is not { } next)
        {
            EndVisit(Ending.RoundDone);
            return;
        }
        foreach (var dot in _dots) dot.QueueFree();
        _dots.Clear();
        ClearNumerals();
        Attempt = new QuestionAttempt();
        ResetKeys();
        _round = next;

        for (var i = 0; i < next.Count; i++)
        {
            var (x, y) = Spots[i % Spots.Length];
            var dot = Pen(Ellipse(P(x, y + 120), 42, 42), (ulong)(930 + i), fill: Palette.Red, lineWidth: 4);
            dot.ZIndex = 10;
            Stage.AddChild(dot);
            _dots.Add(dot);
        }
        InputLocked = true;
        SayPrompt();
        After(next.FlashSeconds, HideDots);
    }

    private void HideDots()
    {
        foreach (var dot in _dots)
        {
            // Shrink each dot towards its own centre (the sketch is drawn around the spot, not its origin).
            var holder = dot;
            var shrink = holder.CreateTween();
            shrink.TweenProperty(holder, "modulate:a", 0f, 0.2);
            shrink.TweenCallback(Callable.From(holder.QueueFree));
        }
        _dots.Clear();
        ShowNumerals();
        InputLocked = false;
    }
}
