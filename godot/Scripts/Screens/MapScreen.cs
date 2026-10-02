using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Screens;

/// <summary>The island map (Swift: MapScene). An island is open when its games loaded; the rest stay asleep.</summary>
public partial class MapScreen : BaseScreen
{
    private readonly bool _greet;
    private readonly Dictionary<Island, Node2D> _islands = new();

    public MapScreen() : this(false) { }
    public MapScreen(bool greet) => _greet = greet;

    protected override IReadOnlyList<Node2D> KeyOptions =>
        new[] { Island.Letters, Island.Numbers, Island.Words, Island.Coding }.Select(i => _islands[i]).ToList();

    protected override void Build()
    {
        DrawSea();
        _islands[Island.Letters] = AddIsland("letters", P(-400, 170), Palette.Grass, Coordinator.LettersOpen, 600, n =>
        {
            var letters = Sketch.Letter("s a t", 110, shadow: Palette.Orange);
            letters.Position = P(0, 30);
            n.AddChild(letters);
        });
        _islands[Island.Numbers] = AddIsland("numbers", P(400, 190), Palette.Sun, Coordinator.NumbersOpen, 620, n =>
        {
            var label = Sketch.Letter("1 2 3", 96, shadow: Palette.Red);
            label.Position = P(0, 30);
            n.AddChild(label);
        });
        _islands[Island.Words] = AddIsland("words", P(-380, -230), Palette.Pink, Coordinator.WordsOpen, 640, n =>
        {
            var picture = PictureNode.Make("apple");
            picture.Scale = Vector2.One * 0.55f;
            picture.Position = P(-60, 30);
            n.AddChild(picture);
            var word = Sketch.Letter("abc", 80, shadow: Palette.Purple);
            word.Position = P(60, 25);
            n.AddChild(word);
        });
        _islands[Island.Coding] = AddIsland("coding", P(400, -220), Palette.LightTeal, Coordinator.CodingOpen, 660, n =>
        {
            foreach (var (i, angle) in Indexed(0.0, Math.PI / 2, 0.0))
            {
                var arrow = Pen(Polygon(P(-26, -9), P(4, -9), P(4, -24), P(30, 0), P(4, 24), P(4, 9), P(-26, 9)),
                                (ulong)(670 + i), fill: Palette.Orange, lineWidth: 4);
                arrow.Rotation = Turn(angle);
                arrow.Position = P(-75 + i * 75, 30);
                n.AddChild(arrow);
            }
        });

        AddBip(P(-110, -40), 0.75f);

        var stickers = Buttons.Tappable(Group(
            Pen(Ellipse(Vector2.Zero, 64, 64), 680, fill: Palette.Sun, lineWidth: 5),
            Pen(Polygon(Star(Vector2.Zero, 34)), 681, fill: Palette.Orange, lineWidth: 4)), "stickers");
        stickers.Position = P(700, 360);
        stickers.ZIndex = 10;
        Stage.AddChild(stickers);

        // Whose turn it is: tap the animal to go back to "Who's playing?".
        if (Coordinator.Children.Count > 1 && Coordinator.CurrentChild is { } child)
        {
            var badge = Buttons.Tappable(Avatars.Badge(child.Avatar, 64, 684), "profiles");
            badge.Position = P(-700, -370);
            badge.ZIndex = 10;
            Stage.AddChild(badge);
        }

        if (Coordinator.MysteryAvailable)
        {
            var box = Buttons.Tappable(new Node2D(), "mystery");
            box.Position = P(-700, 340);
            box.ZIndex = 10;
            box.AddChild(Pen(RoundRect(R(-70, -70, 140, 140), 24), 682, fill: Palette.Purple, lineWidth: 6));
            box.AddChild(Pen(Ellipse(P(0, 70), 26, 18), 683, fill: Palette.Pink, lineWidth: 4));
            box.AddChild(Sketch.Label("?", 84, Palette.White));
            Stage.AddChild(box);
            Buttons.Pulse(box);
        }

        // Bip's suggestion glows; the child still picks freely.
        var glowing = Coordinator.IslandSuggestion?.Island ?? Island.Letters;
        Buttons.Pulse(_islands[glowing]);

        if (_greet) After(0.6, SayWelcome);
    }

    private void DrawSea()
    {
        var sea = Pen(RoundRect(R(-790, -480, 1580, 960), 90), 590, fill: Palette.Sea.WithAlpha(0.5), ink: Palette.Sea, lineWidth: 6);
        sea.ZIndex = -50;
        Stage.AddChild(sea);
        var rng = new SeededRandom(591);
        for (var i = 0; i < 26; i++)
        {
            var x = rng.Range(-720, 720);
            var y = rng.Range(-420, 420);
            var wave = Sketch.Node(Polyline(P(x - 24, y), P(x - 12, y + 8), P(x, y), P(x + 12, y + 8), P(x + 24, y)),
                                   (ulong)(592 + i), ink: Palette.White.WithAlpha(0.8), lineWidth: 4, wobble: 1);
            wave.ZIndex = -40;
            Stage.AddChild(wave);
        }
    }

    private Node2D AddIsland(string id, Vector2 at, Color colour, bool open, ulong seed, Action<Node2D> decorate)
    {
        var island = Buttons.Tappable(new Node2D(), "island:" + id);
        island.Position = at;
        island.AddChild(Pen(Ellipse(P(0, -18), 250, 135), seed, fill: Palette.Sand));
        island.AddChild(Pen(Ellipse(P(0, 6), 205, 100), seed + 1, fill: colour));
        var decoration = new Node2D();
        decorate(decoration);
        island.AddChild(decoration);
        if (!open)
        {
            decoration.Modulate = new Color(1, 1, 1, 0.45f);
            var sleepy = Sketch.Label("z z z", 52, Palette.Ink.WithAlpha(0.6));
            var holder = new Node2D { Position = P(150, 110) };
            holder.AddChild(sleepy);
            island.AddChild(holder);
            var drift = holder.CreateTween().SetLoops();
            drift.TweenProperty(holder, "position:y", holder.Position.Y - 12, 1.2);
            drift.TweenProperty(holder, "position:y", holder.Position.Y, 1.2);
        }
        Stage.AddChild(island);
        return island;
    }

    private void SayWelcome()
    {
        Bip.Hop();
        Sfx.Play(BipSounds.Effect.Beep);
        Voice.Play([VoiceLine.Welcome]);
        Bip.Point(right: false);
    }

    protected override void HandleTap(string name, Node2D node)
    {
        switch (name)
        {
            case "island:letters" when Coordinator.LettersOpen:
                OpenIsland(node, Island.Letters);
                break;
            case "island:numbers" when Coordinator.NumbersOpen:
                OpenIsland(node, Island.Numbers);
                break;
            case "island:words" when Coordinator.WordsOpen:
                OpenIsland(node, Island.Words);
                break;
            case "island:coding" when Coordinator.CodingOpen:
                OpenIsland(node, Island.Coding);
                break;
            case "stickers":
                Sfx.Play(BipSounds.Effect.Tick);
                Buttons.Press(node);
                InputLocked = true;
                After(0.25, Coordinator.ShowStickers);
                break;
            case "profiles":
                InputLocked = true;
                Sfx.Play(BipSounds.Effect.Tick);
                Buttons.Press(node);
                After(0.25, Coordinator.ShowProfiles);
                break;
            case "mystery":
                InputLocked = true;
                Sfx.Play(BipSounds.Effect.Chime);
                Bip.Celebrate();
                Buttons.Sparkle(node.Position, Stage);
                var pop = node.CreateTween();
                pop.TweenProperty(node, "scale", Vector2.One * 1.2f, 0.15);
                pop.TweenProperty(node, "modulate:a", 0f, 0.2);
                pop.TweenCallback(Callable.From(node.QueueFree));
                if (Coordinator.ClaimMystery()) Voice.Play([Coordinator.RandomPraise()]);
                After(1.2, () => InputLocked = false);
                break;
            case var other when other.StartsWith("island:", StringComparison.Ordinal):
                Sfx.Play(BipSounds.Effect.Boop);
                Buttons.Shake(node);
                Voice.Play([VoiceLine.IslandSleeping]);
                Bip.Point(right: false);
                break;
        }
    }

    private void OpenIsland(Node2D node, Island island)
    {
        Sfx.Play(BipSounds.Effect.Whirr);
        Buttons.Press(node);
        InputLocked = true;
        After(0.25, () => Coordinator.ShowIsland(island));
    }

    protected override void DidTapBip() => Voice.Play([VoiceLine.Welcome]);
    public override void ReplayPrompt() => SayWelcome();
    protected override void GoHome() { }
}
