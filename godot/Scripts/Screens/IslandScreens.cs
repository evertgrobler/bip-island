using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Screens;

/// <summary>
/// Letters Island (Swift: LettersIslandScene): the sounds of the child's current phonics group as
/// stepping stones with progress stars. Bip or the play button starts Bip's suggested activity; a
/// stone meets that sound; the two round buttons start Trace and Feed the Monster.
/// </summary>
public partial class LettersIslandScreen : BaseScreen
{
    public override string? MusicTrack => MusicRules.IslandTrack(Island.Letters);

    private readonly bool _greet;
    private readonly List<Node2D> _keyNodes = [];

    public LettersIslandScreen() : this(false) { }
    public LettersIslandScreen(bool greet) => _greet = greet;

    protected override IReadOnlyList<Node2D> KeyOptions => _keyNodes;

    protected override void Build()
    {
        var island = Pen(Ellipse(P(0, -20), 760, 430), 700, fill: Palette.Sand, ink: Palette.LightBrown, lineWidth: 7);
        island.ZIndex = -50;
        Stage.AddChild(island);
        var grass = Pen(Ellipse(P(0, 30), 680, 330), 701, fill: Palette.Grass.WithAlpha(0.55), ink: Palette.Leaf, lineWidth: 5);
        grass.ZIndex = -49;
        Stage.AddChild(grass);

        var sounds = Coordinator.LettersProgress?.CurrentGroup.Sounds ?? [];
        var suggested = Coordinator.Planner?.SuggestedSound();
        var (positions, scale) = StoneLayout(sounds.Count);
        for (var i = 0; i < sounds.Count; i++)
        {
            var stone = MakeStone(sounds[i], i);
            // The holder sets the size, so the pulse and press animations (which scale to 1) still work.
            var holder = new Node2D { Position = positions[i], Scale = Vector2.One * scale, ZIndex = 5 };
            holder.AddChild(stone);
            Stage.AddChild(holder);
            _keyNodes.Add(stone);
            if (sounds[i].Equals(suggested)) Buttons.Pulse(stone);
        }

        AddHomeButton();
        AddBip(P(-170, -380), 0.85f);

        var trace = Buttons.Tappable(Group(
            Pen(Ellipse(Vector2.Zero, 100, 100), 760, fill: Palette.Card, lineWidth: 6),
            Sketch.Letter("s", 110, shadow: Palette.Teal)), "trace");
        trace.Position = P(-560, -300);
        trace.ZIndex = 10;
        Stage.AddChild(trace);
        _keyNodes.Add(trace);

        // Feed the Monster waits until there are foods the child can read (from group 4).
        if (Coordinator.FeedMonsterReady())
        {
            var monster = Buttons.Tappable(Group(
                Pen(Ellipse(Vector2.Zero, 100, 100), 770, fill: Palette.Purple, lineWidth: 6),
                Pen(Ellipse(P(0, -10), 44, 34), 771, fill: Palette.Ink, lineWidth: 4)), "monster");
            foreach (var (i, eye) in Indexed(-30, 30))
                monster.AddChild(Pen(Ellipse(P(eye, 44), 16, 20), (ulong)(772 + i), fill: Palette.White, lineWidth: 4));
            monster.Position = P(560, -300);
            monster.ZIndex = 10;
            Stage.AddChild(monster);
            _keyNodes.Add(monster);
        }

        var play = Buttons.Play();
        play.Position = positions.Count > 6 ? P(380, -320) : P(140, -230);
        play.ZIndex = 10;
        Buttons.Pulse(play);
        Stage.AddChild(play);
        _keyNodes.Add(play);

        if (_greet) After(0.5, ReplayPrompt);
        else After(0.3, () =>
        {
            Bip.Celebrate();
            Sfx.Play(BipSounds.Effect.Whirr);
        });
    }

    /// <summary>One wavy row for up to six stones; two rows, slightly smaller, for bigger groups (stones stay at least 150 pt).</summary>
    private static (List<Vector2> Positions, float Scale) StoneLayout(int count)
    {
        static IEnumerable<Vector2> Row(int n, double y, double spacing, double wave) =>
            Enumerable.Range(0, n).Select(i => P((i - (n - 1) / 2.0) * spacing, y + wave * Math.Sin(i * 1.1)));
        if (count <= 6) return (Row(count, 140, 230, 60).ToList(), 1);
        var top = (count + 1) / 2;
        return (Row(top, 250, 230, 18).Concat(Row(count - top, 10, 230, 18)).ToList(), 0.85f);
    }

    private Node2D MakeStone(PhonicsSound sound, int index)
    {
        var stone = Buttons.Tappable(new Node2D(), "stone:" + sound.Id);
        var stage = Coordinator.Stage(sound);
        Color[] colours = [Palette.Sun, Palette.Orange, Palette.Teal, Palette.Purple, Palette.Pink, Palette.LightTeal];
        var colour = stage == SoundStage.New ? Palette.Stone : colours[index % 6];
        stone.AddChild(Pen(Ellipse(Vector2.Zero, 92, 84), (ulong)(710 + index * 3), fill: colour, lineWidth: 6));
        var letter = Sketch.Letter(sound.Grapheme, 120, shadow: Palette.White);
        letter.Position = P(0, 6);
        stone.AddChild(letter);
        // Three stars: met, recognises, mastered.
        for (var star = 0; star < 3; star++)
        {
            var earned = (int)stage > star;
            stone.AddChild(Pen(Polygon(Star(P(-44 + star * 44, -118), 20)), (ulong)(740 + index * 3 + star),
                fill: earned ? Palette.Sun : Palette.Card, lineWidth: 3.5, wobble: 1));
        }
        return stone;
    }

    protected override void HandleTap(string name, Node2D node)
    {
        if (name == "play")
        {
            Sfx.Play(BipSounds.Effect.Whirr);
            Buttons.Press(node);
            StartSuggested();
        }
        else if (name is "trace" or "monster")
        {
            Sfx.Play(BipSounds.Effect.Whirr);
            Buttons.Press(node);
            InputLocked = true;
            After(0.25, () => StartGame(name == "trace" ? LetterTraceGame.GameId : FeedMonsterGame.GameId));
        }
        else if (name.StartsWith("stone:", StringComparison.Ordinal) && Coordinator.Course?.Sound(name["stone:".Length..]) is { } sound)
        {
            Sfx.Play(BipSounds.Effect.Tick);
            Buttons.Press(node);
            InputLocked = true;
            Voice.Play([sound.SoundClip]);
            After(0.5, () => StartGame(MeetTheSoundGame.GameId, sound));
        }
    }

    protected override void DidTapBip() => StartSuggested();

    private void StartSuggested()
    {
        if (InputLocked) return;
        InputLocked = true;
        Bip.Celebrate();
        // Bip's suggestion: meet a new sound, hunt for a met one, or pop a recognised one.
        After(0.5, () =>
        {
            if (Coordinator.NextActivity() is { } next) StartGame(next.Kind.GameId(), next.Sound);
            else StartGame(SoundHuntGame.GameId);
        });
    }

    public override void ReplayPrompt()
    {
        Voice.Play([VoiceLine.LettersIsland]);
        Bip.Hop();
    }
}

/// <summary>
/// Numbers, Words and Coding Islands (Swift: NumbersIslandScene, WordsIslandScene, CodingIslandScene):
/// the same layout with a row of game buttons (four on Numbers and Words, two on Coding). Bip starts the left game.
/// </summary>
public partial class GameIslandScreen : BaseScreen
{
    public override string? MusicTrack => MusicRules.IslandTrack(_island);

    private readonly Island _island;
    private readonly bool _greet;
    private readonly List<Node2D> _keyNodes = [];
    private string _leftGame = "";

    public GameIslandScreen() : this(Island.Numbers, false) { }

    public GameIslandScreen(Island island, bool greet)
    {
        _island = island;
        _greet = greet;
    }

    protected override IReadOnlyList<Node2D> KeyOptions => _keyNodes;

    protected override void Build()
    {
        var (seed, ground, groundInk) = _island switch
        {
            Island.Numbers => (800UL, Palette.Sun.WithAlpha(0.5), Palette.Orange),
            Island.Words => (810UL, Palette.Pink.WithAlpha(0.45), Palette.Purple),
            Island.Language => (850UL, Palette.Sea.Blend(0.35, Palette.Grass).WithAlpha(0.5), Palette.Teal),
            _ => (820UL, Palette.LightTeal.WithAlpha(0.5), Palette.Teal),
        };
        var island = Pen(Ellipse(P(0, -20), 760, 430), seed, fill: Palette.Sand, ink: Palette.LightBrown, lineWidth: 7);
        island.ZIndex = -50;
        Stage.AddChild(island);
        var inner = Pen(Ellipse(P(0, 30), 680, 330), seed + 1, fill: ground, ink: groundInk, lineWidth: 5);
        inner.ZIndex = -49;
        Stage.AddChild(inner);

        Node2D[] games = _island switch
        {
            Island.Numbers => [CountButton(), BlocksButton(), QuickButton(), BalanceButton()],
            Island.Words => [SoundButtonsButton(), BuilderButton(), RocketButton(), StoriesButton()],
            Island.Language => [ShowMeButton(), WhatIsItButton(), BipsDayButton()],
            _ => [OrderButton(), PathButton()],
        };
        _leftGame = GameFor(Buttons.TapName(games[0])!);
        var spacing = games.Length switch { > 3 => 370.0, > 2 => 480.0, _ => 700.0 };
        for (var i = 0; i < games.Length; i++)
        {
            games[i].Position = P((i - (games.Length - 1) / 2.0) * spacing, 80);
            games[i].ZIndex = 5;
            Stage.AddChild(games[i]);
            _keyNodes.Add(games[i]);
        }
        Buttons.Pulse(games[0]);

        AddHomeButton();
        AddBip(P(0, -360), 0.85f);
        if (_greet) After(0.5, ReplayPrompt);
    }

    private static Node2D CountButton()
    {
        var count = Buttons.Tappable(Group(Pen(Ellipse(Vector2.Zero, 180, 160), 802, fill: Palette.Card, lineWidth: 6)), "count");
        var numerals = Sketch.Letter("1 2 3", 110, shadow: Palette.Red);
        numerals.Position = P(0, 10);
        count.AddChild(numerals);
        return count;
    }

    private static Node2D QuickButton()
    {
        var quick = Buttons.Tappable(Group(Pen(RoundRect(R(-150, -150, 300, 300), 40), 803, fill: Palette.Card, lineWidth: 6)), "quick");
        foreach (var (i, p) in Indexed(P(-70, 70), P(70, 70), P(0, 0), P(-70, -70), P(70, -70)))
            quick.AddChild(Pen(Ellipse(p, 24, 24), (ulong)(804 + i), fill: Palette.Ink, lineWidth: 3));
        return quick;
    }

    /// <summary>Block Towers: towers of one, two and three cubes, like steps.</summary>
    private static Node2D BlocksButton()
    {
        var blocks = Buttons.Tappable(Group(Pen(Ellipse(Vector2.Zero, 170, 160), 806, fill: Palette.Card, lineWidth: 6)), "blocks");
        foreach (var (i, colour) in Indexed(Palette.Teal, Palette.Orange, Palette.Purple))
        {
            var tower = BlockDrawing.Tower(i + 1, colour, 54, (ulong)(807 + i * 5));
            tower.Position = P((i - 1) * 64, -85);
            blocks.AddChild(tower);
        }
        return blocks;
    }

    /// <summary>Bip's Balance: a little see-saw, level, with a cube on each end.</summary>
    private static Node2D BalanceButton()
    {
        var balance = Buttons.Tappable(Group(Pen(Ellipse(Vector2.Zero, 160, 150), 840, fill: Palette.Card, lineWidth: 6)), "balance");
        balance.AddChild(Pen(Polygon(P(-34, -70), P(34, -70), P(0, -10)), 841, fill: Palette.Orange, lineWidth: 5));
        balance.AddChild(new BlockPatch(BlockDrawing.RoundedBox(-120, -6, 240, 22, 8), Palette.LightBrown));
        balance.AddChild(Pen(RoundRect(R(-120, -16, 240, 22), 8), 842, lineWidth: 5));
        foreach (var (i, x) in Indexed(-90.0, 90.0))
        {
            var cube = BlockDrawing.Cube(i == 0 ? Palette.Teal : Palette.Purple, 44, (ulong)(843 + i * 3));
            cube.Position = P(x, 28);
            balance.AddChild(cube);
        }
        var equals = Sketch.Letter("=", 64, shadow: Palette.Orange);
        equals.Position = P(0, 50);
        balance.AddChild(equals);
        return balance;
    }

    private static Node2D SoundButtonsButton()
    {
        var buttons = Buttons.Tappable(new Node2D(), "buttons");
        buttons.AddChild(Pen(Ellipse(Vector2.Zero, 172, 110), 818, fill: Palette.Card, lineWidth: 6));
        foreach (var (i, letter) in Indexed("c", "a", "t"))
        {
            // Four games share Words Island, so the letters sit a little closer than on a three-game island.
            var x = (i - 1) * 100.0;
            buttons.AddChild(Pen(Ellipse(P(x, 0), 48, 48), (ulong)(812 + i), fill: Palette.Card, lineWidth: 5));
            var text = Sketch.Letter(letter, 66, shadow: Palette.Purple);
            text.Position = P(x, 0);
            buttons.AddChild(text);
        }
        return buttons;
    }

    private static Node2D BuilderButton()
    {
        var builder = Buttons.Tappable(new Node2D(), "builder");
        builder.AddChild(Pen(Ellipse(Vector2.Zero, 165, 110), 819, fill: Palette.Card, lineWidth: 6));
        foreach (var (i, letter) in Indexed("c", "a", "t"))
        {
            var x = (i - 1) * 96.0;
            builder.AddChild(Pen(RoundRect(R(x - 42, -42, 84, 84), 16), (ulong)(815 + i), fill: Palette.Teal, lineWidth: 5));
            var text = Sketch.Letter(letter, 62, colour: Palette.White, withShadow: false);
            text.Position = P(x, 0);
            builder.AddChild(text);
        }
        return builder;
    }

    /// <summary>Word Rocket: a little rocket standing ready, with a letter in its window.</summary>
    private static Node2D RocketButton()
    {
        var rocket = Buttons.Tappable(new Node2D(), "rocket");
        rocket.AddChild(Pen(Ellipse(Vector2.Zero, 130, 130), 830, fill: Palette.Card, lineWidth: 6));
        foreach (var (i, side) in Indexed(-1.0, 1.0))
            rocket.AddChild(Pen(Polygon(P(side * 30, -40), P(side * 78, -92), P(side * 78, -40), P(side * 30, 10)), (ulong)(831 + i), fill: Palette.Red, lineWidth: 4));
        rocket.AddChild(Pen(Polygon(P(-22, -70), P(0, -112), P(22, -70)), 833, fill: Palette.Orange, lineWidth: 4));
        rocket.AddChild(Pen(Polygon(P(-44, 40), P(0, 112), P(44, 40)), 834, fill: Palette.Red, lineWidth: 5));
        rocket.AddChild(Pen(RoundRect(R(-44, -76, 88, 122), 30), 835, fill: Palette.White, lineWidth: 5));
        rocket.AddChild(Pen(Ellipse(P(0, 4), 30, 30), 836, fill: Palette.Ice, lineWidth: 4));
        var letter = Sketch.Letter("a", 46, shadow: Palette.Sun);
        letter.Position = P(0, 6);
        rocket.AddChild(letter);
        return rocket;
    }

    /// <summary>Bip's Stories: an open story book with a little picture on one page and lines of words on the other.</summary>
    private static Node2D StoriesButton()
    {
        var book = Buttons.Tappable(new Node2D(), "stories");
        book.AddChild(Pen(Ellipse(Vector2.Zero, 160, 140), 870, fill: Palette.Card, lineWidth: 6));
        book.AddChild(Pen(Polygon(P(-120, -60), P(0, -80), P(120, -60), P(120, 70), P(0, 50), P(-120, 70)), 871, fill: Palette.Teal, lineWidth: 5));
        book.AddChild(Pen(Polygon(P(-108, -48), P(-4, -66), P(-4, 54), P(-108, 60)), 872, fill: Palette.White, lineWidth: 4));
        book.AddChild(Pen(Polygon(P(4, -66), P(108, -48), P(108, 60), P(4, 54)), 873, fill: Palette.White, lineWidth: 4));
        book.AddChild(Pen(Ellipse(P(-56, 14), 30, 24), 874, fill: Palette.Sun, lineWidth: 3.5, wobble: 1));
        book.AddChild(Pen(Polygon(P(-96, -36), P(-60, 0), P(-30, -26), P(-16, -40)), 875, fill: Palette.Grass, lineWidth: 3.5, wobble: 1));
        foreach (var (i, y) in Indexed(28.0, 6.0, -16.0, -38.0))
            book.AddChild(Pen(Polyline(P(20, y), P(i == 3 ? 66 : 94, y + 2)), (ulong)(876 + i), lineWidth: 4, wobble: 1));
        return book;
    }

    private static Node2D OrderButton()
    {
        var order = Buttons.Tappable(new Node2D(), "order");
        foreach (var (i, number) in Indexed("1", "2", "3"))
        {
            var x = (i - 1) * 130.0;
            order.AddChild(Pen(RoundRect(R(x - 56, -70, 112, 140), 18), (ulong)(822 + i), fill: Palette.Card, lineWidth: 5));
            var text = Sketch.Letter(number, 72, shadow: Palette.Orange);
            text.Position = P(x, 0);
            order.AddChild(text);
        }
        return order;
    }

    private static Node2D PathButton()
    {
        var path = Buttons.Tappable(new Node2D(), "path");
        foreach (var (i, angle) in Indexed(0.0, Math.PI / 2, 0.0))
        {
            var arrow = Pen(Polygon(P(-30, -11), P(5, -11), P(5, -28), P(35, 0), P(5, 28), P(5, 11), P(-30, 11)),
                            (ulong)(825 + i), fill: Palette.Orange, lineWidth: 4);
            arrow.Rotation = Turn(angle);
            arrow.Position = P((i - 1) * 110, 0);
            path.AddChild(arrow);
        }
        return path;
    }

    /// <summary>Show Me: three little picture cards, the middle one ringed.</summary>
    private static Node2D ShowMeButton()
    {
        var show = Buttons.Tappable(new Node2D(), "showme");
        show.AddChild(Pen(Ellipse(Vector2.Zero, 175, 150), 850, fill: Palette.Card, lineWidth: 6));
        foreach (var (i, picture) in Indexed("pic_cat", "pic_apple", "pic_sun"))
        {
            var x = (i - 1) * 100.0;
            show.AddChild(Pen(RoundRect(R(x - 44, -44, 88, 88), 16), (ulong)(851 + i), fill: Palette.Card, lineWidth: 4));
            var drawn = PictureNode.Make(picture, picture);
            drawn.Scale = Vector2.One * 0.34f;
            drawn.Position = P(x, 0);
            show.AddChild(drawn);
        }
        show.AddChild(Pen(RoundRect(R(-56, -56, 112, 112), 20), 854, ink: Palette.Orange, lineWidth: 5));
        return show;
    }

    /// <summary>What Is It?: a picture with two speech bubbles beside it.</summary>
    private static Node2D WhatIsItButton()
    {
        var what = Buttons.Tappable(new Node2D(), "whatisit");
        what.AddChild(Pen(Ellipse(Vector2.Zero, 150, 140), 856, fill: Palette.Card, lineWidth: 6));
        var dog = PictureNode.Make("pic_dog", "dog");
        dog.Scale = Vector2.One * 0.5f;
        dog.Position = P(-55, -20);
        what.AddChild(dog);
        foreach (var (i, y) in Indexed(50.0, -40.0))
        {
            what.AddChild(Pen(Ellipse(P(75, y), 48, 32), (ulong)(857 + i), fill: i == 0 ? Palette.Sun : Palette.LightTeal, lineWidth: 4));
            var mark = Sketch.Letter("?", 40, withShadow: false);
            mark.Position = P(75, y);
            what.AddChild(mark);
        }
        return what;
    }

    /// <summary>Bip's Day: a sun over a little card of a morning routine.</summary>
    private static Node2D BipsDayButton()
    {
        var day = Buttons.Tappable(new Node2D(), "bipsday");
        day.AddChild(Pen(Ellipse(Vector2.Zero, 150, 140), 860, fill: Palette.Card, lineWidth: 6));
        var sun = PictureNode.Make("pic_sun", "sun");
        sun.Scale = Vector2.One * 0.45f;
        sun.Position = P(55, 50);
        day.AddChild(sun);
        var morning = PictureNode.Make("pic_morning_2", "Brush your teeth");
        morning.Scale = Vector2.One * 0.5f;
        morning.Position = P(-30, -25);
        day.AddChild(morning);
        return day;
    }

    private static string GameFor(string tapName) => tapName switch
    {
        "showme" => ShowMeGame.GameId,
        "whatisit" => WhatIsItGame.GameId,
        "bipsday" => BipsDayGame.GameId,
        "count" => CountTapGame.GameId,
        "quick" => QuickLookGame.GameId,
        "blocks" => BlockTowersGame.GameId,
        "balance" => BipsBalanceGame.GameId,
        "buttons" => SoundButtonsGame.GameId,
        "builder" => WordBuilderGame.GameId,
        "rocket" => WordRocketGame.GameId,
        "stories" => StoriesGame.GameId,
        "order" => MorningOrderGame.GameId,
        _ => BipsPathGame.GameId,
    };

    protected override void HandleTap(string name, Node2D node)
    {
        Sfx.Play(BipSounds.Effect.Whirr);
        Buttons.Press(node);
        InputLocked = true;
        After(0.25, () => StartGame(GameFor(name)));
    }

    protected override void DidTapBip()
    {
        if (InputLocked) return;
        InputLocked = true;
        After(0.25, () => StartGame(_leftGame));
    }

    public override void ReplayPrompt()
    {
        Voice.Play([_island switch
        {
            Island.Numbers => VoiceLine.NumbersIsland,
            Island.Words => VoiceLine.WordsIsland,
            Island.Language => VoiceLine.LanguageIsland,
            _ => VoiceLine.CodingIsland,
        }]);
        Bip.Hop();
    }
}
