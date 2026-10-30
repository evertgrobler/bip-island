using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using BipIsland.Screens;
using Godot;
using static BipIsland.Drawing.Up;
using Mode = BipCore.PicnicPlatesGame.Mode;

namespace BipIsland.Games;

/// <summary>
/// Picnic Plates (Numbers Island): Bip's picnic blanket with plates of food, a tray in rows, or punnets.
/// For making groups and sharing, each click of the basket deals one piece of food to every plate,
/// round and round; for grouping, each click packs one punnet. Then three number cards ask how many
/// altogether, on each plate, or how many punnets. Wrong → soft boop, try again; two misses → the right
/// card wiggles and Bip counts the plates (in steps of the table), the rounds, or the punnets, while
/// each one bobs in turn. No timers, no dragging.
/// </summary>
public partial class PicnicPlatesScreen : GameScreen
{
    private static readonly Vector2[] ChoiceAt = [P(-350, -330), P(0, -330), P(350, -330)];
    private static readonly Vector2 BasketAt = P(-555, 110);
    private const float FoodOnPlate = 0.26f;
    private const double PlateY = 120;

    private readonly PicnicPlatesGame _game;
    private readonly GameSession _session;
    private PicnicPlatesGame.Round? _round;
    private QuestionAttempt _attempt = new();
    private readonly List<Node2D> _choices = [];
    private readonly List<Node2D> _scene = [];
    /// <summary>The plates, tray rows or punnets, in order: the hint bobs them as Bip counts.</summary>
    private readonly List<Node2D> _groups = [];
    /// <summary>Grouping and sharing: the loose food on the blanket, packed or shared from the end.</summary>
    private readonly List<Node2D> _loose = [];
    private Node2D? _basket;
    private Tween? _pulse;
    private int _dealt;
    private bool _dealing;
    private bool _greeted;

    public PicnicPlatesScreen(PicnicPlatesGame game)
    {
        _game = game;
        _session = Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Numbers;
    protected override string GameId => PicnicPlatesGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions =>
        _choices.Count > 0 ? _choices : _basket != null && DealsLeft > 0 ? [_basket] : [];

    /// <summary>The question on screen (for the walk-through test), or null between questions.</summary>
    public PicnicPlatesGame.Round? Round => _round;

    /// <summary>Which card is right (for the walk-through test), or -1 before the cards show.</summary>
    public int RightIndex => _round == null || _choices.Count == 0 ? -1 : _round.Choices.ToList().FindIndex(c => _game.IsCorrect(c, _round));

    /// <summary>Clicks of the basket still to come before the question (for the walk-through test).</summary>
    public int DealsLeft => _round == null ? 0 : _round.Deals - _dealt;

    protected override void Build()
    {
        AddGameChrome(P(640, -330), P(-640, -400), 0.7f);
        DrawBlanket();
        After(StartDelay, AskQuestion);
    }

    private ulong Seed(int i) => (ulong)(5200 + _session.RoundsPlayed * 41 + i);

    private T Add<T>(T node, Vector2 at, int z = 5, Node? parent = null) where T : Node2D
    {
        node.Position = at;
        node.ZIndex = z;
        (parent ?? Stage).AddChild(node);
        return node;
    }

    /// <summary>A red-and-white checked picnic blanket on the grass.</summary>
    private void DrawBlanket()
    {
        var grass = Pen(Ellipse(P(0, 60), 900, 420), 5100, fill: Palette.Grass.WithAlpha(0.45f), ink: Palette.Leaf, lineWidth: 5);
        grass.ZIndex = -21;
        Stage.AddChild(grass);
        var blanket = Pen(RoundRect(R(-680, -210, 1360, 600), 36), 5101, fill: Palette.Card, ink: Palette.Red, lineWidth: 7);
        blanket.ZIndex = -20;
        Stage.AddChild(blanket);
        for (var i = 0; i < 8; i++)
        {
            var stripe = Pen(RoundRect(R(-660 + i * 170, -190, 80, 560), 10), (ulong)(5102 + i), fill: Palette.Red.WithAlpha(0.14f), ink: Palette.Red.WithAlpha(0f), lineWidth: 1);
            stripe.ZIndex = -19;
            Stage.AddChild(stripe);
        }
        for (var i = 0; i < 4; i++)
        {
            var stripe = Pen(RoundRect(R(-660, -190 + i * 150, 1320, 70), 10), (ulong)(5110 + i), fill: Palette.Red.WithAlpha(0.14f), ink: Palette.Red.WithAlpha(0f), lineWidth: 1);
            stripe.ZIndex = -19;
            Stage.AddChild(stripe);
        }
    }

    private void Clear()
    {
        StopHint();
        foreach (var node in _choices.Concat(_scene)) node.QueueFree();
        _choices.Clear();
        _scene.Clear();
        _groups.Clear();
        _loose.Clear();
        _pulse?.Kill();
        _pulse = null;
        _basket = null;
        _dealt = 0;
        _dealing = false;
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
        switch (next.Mode)
        {
            case Mode.Groups:
                DrawPlates(next, filled: true, withBasket: false);
                break;
            case Mode.Make:
                DrawPlates(next, filled: false, withBasket: true);
                break;
            case Mode.Sharing:
                DrawPlates(next, filled: false, withBasket: true);
                DrawSharePile(next);
                break;
            case Mode.Arrays:
                DrawTray(next);
                break;
            case Mode.Grouping:
                DrawLooseFood(next);
                break;
        }
        Sfx.Play(BipSounds.Effect.Beep);
        InputLocked = false;
        if (next.Deals == 0) DrawChoices(next);
        else _pulse = Buttons.Pulse(_basket!);
        // The first question of a visit starts with "Bip is having a picnic!"
        SayPrompt(withIntro: !_greeted);
        _greeted = true;
    }

    // MARK: - The picnic

    private Node2D Food(PicnicFood food, float scale)
    {
        var picture = PictureNode.Make(food.Picture, food.Id);
        picture.Scale = Vector2.One * scale;
        return picture;
    }

    /// <summary>Where the plates go: across the blanket, or right of the basket when there is one.</summary>
    private static List<Vector2> PlateSpots(int count, bool withBasket)
    {
        var (left, right) = withBasket ? (-330.0, 560.0) : (-520.0, 520.0);
        var spacing = Math.Min(260, (right - left) / Math.Max(1, count - 1));
        var middle = (left + right) / 2;
        return Enumerable.Range(0, count).Select(i => P(middle + (i - (count - 1) / 2.0) * spacing, PlateY)).ToList();
    }

    /// <summary>How big the plates are drawn: full size when they fit, smaller when five share the blanket with the basket.</summary>
    private static float PlateScale(int count, bool withBasket)
    {
        var (left, right) = withBasket ? (-330.0, 560.0) : (-520.0, 520.0);
        var spacing = count > 1 ? (right - left) / (count - 1) : 260;
        return (float)Math.Min(1.0, spacing / 236);
    }

    /// <summary>
    /// Where the n-th of <paramref name="count"/> pieces of food sits on a plate (or in a punnet): one row
    /// up to five, centred, then two rows of five.
    /// </summary>
    private static Vector2 OnPlate(int n, int count, double spread = 40)
    {
        var row = n / 5;
        var rowCount = count <= 5 ? count : row == 0 ? 5 : count - 5;
        var x = (n % 5 - (rowCount - 1) / 2.0) * spread;
        return P(x, count <= 5 ? 0 : row == 0 ? 24 : -24);
    }

    private void DrawPlates(PicnicPlatesGame.Round round, bool filled, bool withBasket)
    {
        var spots = PlateSpots(round.Groups, withBasket);
        for (var i = 0; i < round.Groups; i++)
        {
            var plate = Add(new Node2D { Name = $"Plate{i}", Scale = Vector2.One * PlateScale(round.Groups, withBasket) }, spots[i], 2);
            plate.AddChild(Pen(Ellipse(Vector2.Zero, 108, 84), Seed(i), fill: Palette.White, ink: Palette.Teal, lineWidth: 6));
            plate.AddChild(Pen(Ellipse(Vector2.Zero, 80, 60), Seed(10 + i), ink: Palette.LightTeal, lineWidth: 3));
            if (filled)
                for (var n = 0; n < round.Each; n++) Add(Food(round.Food, FoodOnPlate), OnPlate(n, round.Each), 1, plate);
            _scene.Add(plate);
            _groups.Add(plate);
        }
        if (withBasket) DrawBasket(round);
    }

    /// <summary>The basket of food. Clicking it deals a round (or packs a punnet).</summary>
    private void DrawBasket(PicnicPlatesGame.Round round)
    {
        var basket = Buttons.Tappable(new Node2D { Name = "Basket" }, "basket");
        basket.AddChild(Pen(Polyline(Enumerable.Range(0, 13).Select(i => P(-90 * Math.Cos(Math.PI * i / 12), 30 + 110 * Math.Sin(Math.PI * i / 12)))),
                            Seed(20), ink: Palette.Brown, lineWidth: 9));
        // Sharing deals out the food on the blanket, so its basket is empty.
        if (round.Mode != Mode.Sharing)
            foreach (var (i, x) in Indexed(-46.0, 0.0, 46.0))
                Add(Food(round.Food, 0.3f), P(x, 40 + (i == 1 ? 18 : 0)), 0, basket);
        basket.AddChild(Pen(Polygon(P(-120, 40), P(120, 40), P(96, -80), P(-96, -80)), Seed(21), fill: Palette.LightBrown, ink: Palette.Brown, lineWidth: 6));
        foreach (var y in new[] { 0.0, -40.0 })
            basket.AddChild(Pen(Polyline(P(-108, y), P(108, y)), Seed(22 + (int)y), ink: Palette.Brown, lineWidth: 3));
        Hit.SetArea(basket, new Rect2(-130, -130, 260, 260));
        _basket = Add(basket, BasketAt, 6);
        _scene.Add(basket);
    }

    /// <summary>A baking tray of food in rows, each row a group the hint can bob.</summary>
    private void DrawTray(PicnicPlatesGame.Round round)
    {
        const double gap = 72;
        var width = round.Each * gap + 40;
        var height = round.Groups * gap + 40;
        var tray = Add(new Node2D { Name = "Tray" }, P(0, 110), 2);
        tray.AddChild(Pen(RoundRect(R(-width / 2, -height / 2, width, height), 22), Seed(30), fill: Palette.Stone, ink: Palette.Brown, lineWidth: 6));
        _scene.Add(tray);
        for (var r = 0; r < round.Groups; r++)
        {
            var row = Add(new Node2D { Name = $"Row{r}" }, P(0, (round.Groups - 1) / 2.0 * gap - r * gap), 1, tray);
            for (var c = 0; c < round.Each; c++)
                Add(Food(round.Food, 0.28f), P((c - (round.Each - 1) / 2.0) * gap, 0), 0, row);
            _groups.Add(row);
        }
    }

    /// <summary>
    /// Sharing: all the food to share, in rows of ten on the blanket below the plates ("Share twelve
    /// between two plates"), so the child sees the whole amount go, one to each plate a click.
    /// </summary>
    private void DrawSharePile(PicnicPlatesGame.Round round)
    {
        const double gap = 54;
        for (var n = 0; n < round.Total; n++)
        {
            var inRow = Math.Min(10, round.Total - n / 10 * 10);
            var food = Add(Food(round.Food, 0.24f), P(115 + (n % 10 - (inRow - 1) / 2.0) * gap, -55 - n / 10 * gap), 3);
            _scene.Add(food);
            _loose.Add(food);
        }
    }

    /// <summary>Grouping: the food loose on the blanket, the empty punnets in a row, and the basket that packs them.</summary>
    private void DrawLooseFood(PicnicPlatesGame.Round round)
    {
        const double gap = 64;
        for (var n = 0; n < round.Total; n++)
        {
            var food = Add(Food(round.Food, 0.26f), P(-260 + n % 10 * gap, 300 - n / 10 * gap), 3);
            _scene.Add(food);
            _loose.Add(food);
        }
        var spacing = Math.Min(200, 860.0 / Math.Max(1, round.Groups - 1));
        for (var i = 0; i < round.Groups; i++)
        {
            var punnet = Add(new Node2D { Name = $"Punnet{i}" }, P(30 + (i - (round.Groups - 1) / 2.0) * spacing, -110), 2);
            punnet.AddChild(Pen(Polygon(P(-86, 40), P(86, 40), P(70, -40), P(-70, -40)), Seed(40 + i), fill: Palette.Sun.WithAlpha(0.35f), ink: Palette.Orange, lineWidth: 5));
            punnet.Modulate = new Color(1, 1, 1, 0.45f);
            _scene.Add(punnet);
            _groups.Add(punnet);
        }
        DrawBasket(round);
    }

    // MARK: - Dealing

    /// <summary>One click of the basket: a piece of food onto every plate, or one punnet packed.</summary>
    private void Deal()
    {
        if (_round is not { } round || _dealing || DealsLeft <= 0 || _basket == null) return;
        _dealing = true;
        StopHint();
        _pulse?.Kill();
        _pulse = null;
        Buttons.Press(_basket);
        Sfx.Play(BipSounds.Effect.Pop);
        var deal = _dealt;
        _dealt += 1;
        if (round.Mode == Mode.Grouping)
        {
            var punnet = _groups[deal];
            punnet.Modulate = Colors.White;
            for (var n = 0; n < round.Each; n++)
            {
                var food = _loose[^1];
                _loose.RemoveAt(_loose.Count - 1);
                var at = food.GlobalPosition;
                food.GetParent().RemoveChild(food);
                punnet.AddChild(food);
                food.GlobalPosition = at;
                food.CreateTween().TweenProperty(food, "position", OnPlate(n, round.Each, 30), 0.3).SetDelay(n * 0.03);
                food.CreateTween().TweenProperty(food, "scale", Vector2.One * 0.22f, 0.3);
            }
        }
        else if (round.Mode == Mode.Sharing)
        {
            // One from the pile onto every plate, round and round.
            for (var i = 0; i < _groups.Count && _loose.Count > 0; i++)
            {
                var plate = _groups[i];
                var food = _loose[^1];
                _loose.RemoveAt(_loose.Count - 1);
                var at = food.GlobalPosition;
                food.GetParent().RemoveChild(food);
                plate.AddChild(food);
                food.GlobalPosition = at;
                food.ZIndex = 1;
                food.CreateTween().TweenProperty(food, "position", OnPlate(deal, round.Each), 0.3).SetDelay(i * 0.06);
                food.CreateTween().TweenProperty(food, "scale", Vector2.One * FoodOnPlate, 0.3).SetDelay(i * 0.06);
            }
        }
        else
        {
            for (var i = 0; i < _groups.Count; i++)
            {
                var plate = _groups[i];
                var food = Add(Food(round.Food, 0.3f), plate.ToLocal(_basket.GlobalPosition), 1, plate);
                food.CreateTween().TweenProperty(food, "position", OnPlate(deal, round.Each), 0.3).SetDelay(i * 0.06);
                food.CreateTween().TweenProperty(food, "scale", Vector2.One * FoodOnPlate, 0.3).SetDelay(i * 0.06);
            }
        }
        After(0.45, () =>
        {
            if (_round != round) return;
            _dealing = false;
            if (DealsLeft > 0) return;
            // All dealt: the basket goes, the cards come, and Bip asks.
            _basket.Modulate = new Color(1, 1, 1, 0.4f);
            DrawChoices(round);
            Voice.Play(_game.Question(round));
            Bip.Hop();
        });
    }

    // MARK: - Number cards

    private void DrawChoices(PicnicPlatesGame.Round round)
    {
        for (var i = 0; i < round.Choices.Count; i++)
        {
            var button = Buttons.Tappable(new Node2D(), $"choice:{i}");
            button.AddChild(Pen(Ellipse(Vector2.Zero, 95, 95), Seed(60 + i), fill: Palette.Card, lineWidth: 6));
            button.AddChild(Sketch.Letter(round.Choices[i].ToString(), 110, shadow: Palette.Red));
            button.Scale = Vector2.One * 0.01f;
            _choices.Add(Add(button, ChoiceAt[i % ChoiceAt.Length], 10));
            button.CreateTween().TweenProperty(button, "scale", Vector2.One, 0.25);
        }
        ResetKeys();
        Sfx.Play(BipSounds.Effect.Chime);
    }

    // MARK: - Voice

    private void SayPrompt(bool withIntro = false)
    {
        if (_round is not { } round) return;
        Bip.Hop();
        var ask = DealsLeft > 0 ? _game.Instruction(round) : _game.Question(round);
        Voice.Play(withIntro ? [VoiceLine.Picnic, .. ask] : ask);
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }

    // MARK: - Answers

    protected override void HandleTap(string name, Node2D node)
    {
        if (name == "replay")
        {
            Buttons.Press(node);
            SayPrompt();
            return;
        }
        if (name == "basket")
        {
            Deal();
            return;
        }
        if (_round is not { } round || !name.StartsWith("choice:", StringComparison.Ordinal)
            || !int.TryParse(name["choice:".Length..], out var index) || index >= round.Choices.Count || index >= _choices.Count) return;
        Answer(round, index);
    }

    private void Answer(PicnicPlatesGame.Round round, int index)
    {
        var card = _choices[index];
        var outcome = _attempt.Answer(_game.IsCorrect(round.Choices[index], round));
        if (outcome.Kind == AnswerOutcomeKind.Correct)
        {
            InputLocked = true;
            StopHint();
            var grow = card.CreateTween();
            grow.TweenProperty(card, "scale", Vector2.One * 1.15f, 0.15);
            grow.TweenProperty(card, "scale", Vector2.One * 1.05f, 0.1);
            Buttons.Sparkle(card.Position, Stage);
            foreach (var group in _groups) Buttons.Sparkle(Stage.ToLocal(group.GlobalPosition), Stage);
            Sfx.Play(BipSounds.Effect.Chime);
            Bip.Celebrate();
            var change = Record(outcome.FirstTry, _game.SkillId(round));
            Voice.Play([Coordinator.RandomPraise(), .. _game.SayWhenRight(round)],
                       completion: () => AfterAnswer(change, _session, AskQuestion));
            return;
        }
        Sfx.Play(BipSounds.Effect.Boop);
        Buttons.Shake(card);
        Bip.Tilt();
        if (outcome.Kind != AnswerOutcomeKind.Hint) return;
        // Two misses: the right card wiggles, and each plate, row or punnet bobs as Bip counts it.
        if (RightIndex is var r && r >= 0 && r < _choices.Count) ShowHint(_choices[r]);
        var hint = _game.Hint(round);
        After(0.4, () => Voice.Play(hint));
        // The count starts after the opening words (about a second), then a group about every 0.7 s.
        var counted = round.Mode == Mode.Sharing ? [] : _groups.ToList();
        for (var i = 0; i < counted.Count; i++)
        {
            var group = counted[i];
            After(1.6 + i * 0.7, () =>
            {
                if (_round != round || !IsInstanceValid(group)) return;
                var bob = group.CreateTween();
                bob.TweenProperty(group, "scale", Vector2.One * 1.12f, 0.15);
                bob.TweenProperty(group, "scale", Vector2.One, 0.2);
            });
        }
    }
}
