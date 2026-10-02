using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Screens;

/// <summary>"Who's playing?" (Swift: ProfilesScene): one card per child with their animal, name and stars.</summary>
public partial class ProfilesScreen : BaseScreen
{
    private readonly List<Node2D> _cards = [];

    protected override IReadOnlyList<Node2D> KeyOptions => _cards;

    protected override void Build()
    {
        AddBip(P(-620, -380), 0.7f);
        var title = Sketch.Label("Who's playing?", 72, Palette.Ink);
        var titleHolder = new Node2D { Position = P(0, 360) };
        titleHolder.AddChild(title);
        Stage.AddChild(titleHolder);

        var children = Coordinator.Children;
        const double spacing = 370;
        for (var i = 0; i < children.Count; i++)
        {
            var card = MakeCard(children[i], i);
            card.Position = P((i - (children.Count - 1) / 2.0) * spacing, 20);
            card.Scale = Vector2.One * 0.01f;
            Stage.AddChild(card);
            _cards.Add(card);
            var grow = card.CreateTween();
            grow.TweenInterval(0.1 * i);
            grow.TweenProperty(card, "scale", Vector2.One * 1.06f, 0.2);
            grow.TweenProperty(card, "scale", Vector2.One, 0.1);
        }
        After(0.6, ReplayPrompt);
    }

    private Node2D MakeCard(ChildSummary child, int index)
    {
        var card = Buttons.Tappable(new Node2D(), "child:" + child.Id);
        card.ZIndex = 10;
        var seed = (ulong)(1300 + index * 5);
        card.AddChild(Pen(RoundRect(R(-160, -200, 320, 400), 34), seed, fill: Palette.Card, lineWidth: 6));
        var badge = Avatars.Badge(child.Avatar, 100, seed + 1);
        badge.Position = P(0, 55);
        card.AddChild(badge);

        var name = Sketch.Label(child.Name, child.Name.Length > 10 ? 34 : 46, Palette.Ink);
        var nameHolder = new Node2D { Position = P(0, -100) };
        nameHolder.AddChild(name);
        card.AddChild(nameHolder);

        var star = Pen(Polygon(Star(Vector2.Zero, 20)), seed + 2, fill: Palette.Sun, lineWidth: 3.5, wobble: 1);
        star.Position = P(-34, -158);
        card.AddChild(star);
        var count = Sketch.Label(Coordinator.StarCount(child.Id).ToString(), 36, Palette.Ink);
        // Left-aligned just after the star (Swift: horizontalAlignmentMode = .left).
        count.Position = new Vector2(P(-6, -158).X, P(-6, -158).Y - count.Size.Y / 2);
        card.AddChild(count);
        return card;
    }

    protected override void HandleTap(string name, Node2D node)
    {
        if (!name.StartsWith("child:", StringComparison.Ordinal) || !Guid.TryParse(name["child:".Length..], out var id)) return;
        InputLocked = true;
        Sfx.Play(BipSounds.Effect.Chime);
        Buttons.Press(node);
        Bip.Celebrate();
        After(0.4, () => Coordinator.Choose(id));
    }

    protected override void DidTapBip() => ReplayPrompt();

    public override void ReplayPrompt()
    {
        Voice.Play([VoiceLine.WhoIsPlaying]);
        Bip.Hop();
    }

    /// <summary>Already the first screen: home does nothing here.</summary>
    protected override void GoHome() { }
}

/// <summary>
/// Bip charging (Swift: ChargingScene): the play-time break. Games stay closed until the break ends;
/// the battery fill is decoration, not a countdown.
/// </summary>
public partial class ChargingScreen : BaseScreen
{
    private Node2D? _batteryFill;

    protected override void Build()
    {
        AddBip(P(-200, -40), 1.1f);
        Bip.Tilt();

        var battery = Pen(RoundRect(R(-110, -190, 220, 380), 36), 1100, fill: Palette.Card, lineWidth: 7);
        battery.Position = P(320, 20);
        battery.ZIndex = 5;
        Stage.AddChild(battery);
        var nub = Pen(RoundRect(R(-40, -30, 80, 60), 14), 1101, fill: Palette.Card, lineWidth: 7);
        nub.Position = P(320, 230);
        nub.ZIndex = 5;
        Stage.AddChild(nub);
        // Grows upwards from the bottom of the battery as it charges.
        var fill = Pen(RoundRect(R(-88, 0, 176, 60), 20), 1102, fill: Palette.Go, lineWidth: 0);
        fill.Position = P(320, -168);
        fill.ZIndex = 6;
        Stage.AddChild(fill);
        _batteryFill = fill;

        var label = new Node2D { Position = P(0, -360) };
        label.AddChild(Sketch.Label("charging…", 64, Palette.Ink));
        Stage.AddChild(label);

        Sfx.Play(BipSounds.Effect.Whirr);
        After(5, Check);
    }

    private void Check()
    {
        // The break follows the wall clock, so a clock change ends it here too.
        if (Coordinator.CurrentBreakPhase() == BreakPhase.Playing)
        {
            Coordinator.ShowMap();
            return;
        }
        if (_batteryFill != null)
        {
            var grow = _batteryFill.CreateTween();
            grow.TweenProperty(_batteryFill, "scale:y", Math.Min(_batteryFill.Scale.Y * 1.15f, 5.6f), 2.5);
        }
        After(5, Check);
    }

    protected override void GoHome() { }
}

/// <summary>
/// The sticker book (Swift: StickerScene): a sticker for every sound and skill, bright when earned.
/// The arrow switches between the sound page and the skill page.
/// </summary>
public partial class StickerScreen : BaseScreen
{
    private bool _showingSkills;
    private Node2D? _page;

    protected override void Build()
    {
        AddHomeButton();
        AddBip(P(-640, -380), 0.7f);

        var jar = new Node2D { Position = P(0, 400) };
        jar.AddChild(Sketch.Label($"{Coordinator.Progress.Stars} stars in the jar", 56, Palette.Ink));
        Stage.AddChild(jar);

        var toggle = Buttons.Tappable(Buttons.Next(), "page");
        toggle.Position = P(640, 360);
        toggle.ZIndex = 10;
        Stage.AddChild(toggle);

        DrawPage();
    }

    private void DrawPage()
    {
        _page?.QueueFree();
        _page = new Node2D { ZIndex = 5 };
        Stage.AddChild(_page);
        if (Coordinator.Content is not { } content) return;
        var earned = StickerBook.Earned(Coordinator.Progress, content);
        if (_showingSkills)
        {
            const int perRow = 8;
            var skills = content.Skills.Skills;
            for (var i = 0; i < skills.Count; i++)
            {
                var got = earned.Contains("skill_" + skills[i].Id);
                var star = Pen(Polygon(Star(Vector2.Zero, 40)), (ulong)(1110 + i),
                    fill: got ? Palette.Sun : Palette.Card.WithAlpha(0.5), lineWidth: 4);
                star.Position = P(-420 + i % perRow * 120, 260 - i / perRow * 95);
                star.Modulate = new Color(1, 1, 1, got ? 1 : 0.45f);
                _page.AddChild(star);
            }
        }
        else
        {
            const int perRow = 9;
            var sounds = new PhonicsCourse(content).AllSounds;
            for (var i = 0; i < sounds.Count; i++)
            {
                var got = earned.Contains("sound_" + sounds[i].Id);
                var badge = Group(
                    Pen(Ellipse(Vector2.Zero, 56, 56), (ulong)(1120 + i), fill: got ? Palette.Sun : Palette.Card.WithAlpha(0.5), lineWidth: 4),
                    Sketch.Letter(sounds[i].Grapheme, 64, withShadow: false));
                badge.Position = P(-520 + i % perRow * 130, 240 - i / perRow * 105);
                badge.Modulate = new Color(1, 1, 1, got ? 1 : 0.45f);
                _page.AddChild(badge);
            }
        }
    }

    protected override void HandleTap(string name, Node2D node)
    {
        if (name != "page") return;
        Sfx.Play(BipSounds.Effect.Tick);
        Buttons.Press(node);
        _showingSkills = !_showingSkills;
        DrawPage();
    }
}
