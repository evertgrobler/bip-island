using System.Collections.Generic;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using BipIsland.Screens;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Games;

/// <summary>
/// Count &amp; Tap: tap each animal as Bip counts aloud, then tap the numeral that says how many.
/// Tapping animals is free play; only the numeral counts towards mastery.
/// </summary>
public partial class CountTapScreen : NumeralGameScreen
{
    private readonly CountTapGame _game;
    private CountTapGame.Round? _round;
    private readonly List<Node2D> _beasts = [];
    private readonly HashSet<int> _tapped = [];

    public CountTapScreen(CountTapGame game) : base(game) => _game = game;

    protected override string GameId => CountTapGame.GameId;
    protected override int Count => _round?.Count ?? 0;
    protected override IReadOnlyList<int> Choices => _round?.Choices ?? [];
    protected override bool IsCorrect(int numeral) => _round != null && _game.IsCorrect(numeral, _round);
    protected override string SkillId => _round != null ? _game.SkillId(_round) : "count_10";
    protected override string PromptClip => VoiceLine.CountTap;
    protected override ulong NumeralSeed => 920;

    protected override void AskQuestion()
    {
        if (Session.NextRound(_game, Coordinator.LearnerFor(GameId), Coordinator.Rng) is not { } next)
        {
            EndVisit(Ending.RoundDone);
            return;
        }
        foreach (var beast in _beasts) beast.QueueFree();
        _beasts.Clear();
        ClearNumerals();
        _tapped.Clear();
        Attempt = new QuestionAttempt();
        ResetKeys();
        _round = next;

        // Five to a row up to 10, seven to a row (a little smaller) above that, so up to 20 still
        // leaves room for the numerals underneath. Pictures are bigger than the Mac app's (about
        // 160 pt rather than 80) so they are easy to see and tap.
        var big = next.Count <= 10;
        var perRow = big ? 5 : 7;
        var (spacing, rowGap, scale) = big ? (200, 190, 1f) : (170, 160, 0.8f);
        for (var i = 0; i < next.Count; i++)
        {
            var picture = PictureNode.Make(next.Object.Picture, next.Object.Id);
            picture.Scale = Vector2.One * scale;
            var holder = Buttons.Tappable(new Node2D(), $"beast:{i}");
            holder.Position = P((i % perRow - (perRow - 1) / 2.0) * spacing, 270 - i / perRow * rowGap);
            holder.ZIndex = 10;
            holder.AddChild(picture);
            Hit.SetArea(holder, new Rect2(-spacing / 2f + 5, -rowGap / 2f + 5, spacing - 10, rowGap - 10));
            Stage.AddChild(holder);
            _beasts.Add(holder);
        }
        InputLocked = false;
        SayPrompt();
    }

    protected override void HandleTap(string name, Node2D node)
    {
        if (name.StartsWith("beast:") && int.TryParse(name["beast:".Length..], out var index) && index < _beasts.Count)
        {
            TapBeast(index);
            return;
        }
        base.HandleTap(name, node);
    }

    private void TapBeast(int index)
    {
        if (_round == null || _tapped.Count >= _round.Count || !_tapped.Add(index)) return;
        var beast = _beasts[index];
        var squash = beast.CreateTween();
        squash.TweenProperty(beast, "scale", Vector2.One * 1.15f, 0.12);
        squash.TweenProperty(beast, "scale", Vector2.One * 0.85f, 0.12);
        Sfx.Play(BipSounds.Effect.Tick);
        Voice.Play([AudioCatalogue.NumberClip(_tapped.Count)]);
        if (_tapped.Count == _round.Count) After(0.9, ShowNumerals);
    }
}
