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
/// Bip charging (Swift: ChargingScene): the play-time break, or the end of the day's play.
/// For the child, no reading needed: Bip sleeps plugged into a battery whose five bars fill with
/// the real break, and he says why when the screen opens or he's clicked. For grown-ups, small
/// text says when games open again and how to end the break early. When the break ends while the
/// screen is open, the battery fills, Bip wakes up and the map opens.
/// </summary>
public partial class ChargingScreen : BaseScreen
{
    private const int Bars = 5;
    private const double CheckSeconds = 5;

    private readonly List<Node2D> _bars = [];
    private Node2D? _battery;
    private Node2D? _when;
    private Label? _grownUps;
    private bool _dayDone;
    private bool _waking;
    private double _sinceBeep;

    /// <summary>The grown-up line now showing (for the walk-through test).</summary>
    public string GrownUpText => _grownUps?.Text ?? "";
    public bool DayDone => _dayDone;
    /// <summary>How many battery bars are lit (for the walk-through test).</summary>
    public int LitBars => _bars.Count(b => b.Modulate.A > 0.9f);

    protected override void Build()
    {
        _dayDone = Coordinator.CurrentBreakPhase() == BreakPhase.DayDone;
        var title = new Node2D { Position = P(0, 390) };
        title.AddChild(Sketch.Label(_dayDone ? "That's all for today" : "Bip is charging", 60, Palette.Ink));
        Stage.AddChild(title);

        if (_dayDone) BuildNight();
        else BuildCharger();
        Bip.Sleep();
        AddSnores();

        var hint = new Node2D { Position = P(0, -395) };
        hint.AddChild(Sketch.Label(
            $"Grown-ups: hold Esc for {ParentGateFlow.HoldSeconds:0} seconds to {(_dayDone ? "change the daily limit" : "end the break early")}.",
            24, Palette.Ink.WithAlpha(0.6)));
        Stage.AddChild(hint);
        _when = new Node2D { Position = P(0, -340) };
        Stage.AddChild(_when);
        ShowTimes();

        Sfx.Play(BipSounds.Effect.Whirr);
        After(0.8, ReplayPrompt);
        After(CheckSeconds, Check);
    }

    /// <summary>Bip on the left, plugged into a battery by a curly cable.</summary>
    private void BuildCharger()
    {
        AddBip(P(-330, -120), 1.1f);
        var cable = Pen(Polyline(P(-262, -40), P(-200, -200), P(-40, -240), P(120, -215), P(250, -190), P(250, -172)),
                        1103, ink: Palette.Ink, lineWidth: 7);
        cable.ZIndex = 4;
        Stage.AddChild(cable);

        var battery = new Node2D { Position = P(250, 20), ZIndex = 5 };
        battery.AddChild(Pen(RoundRect(R(-110, -190, 220, 380), 36), 1100, fill: Palette.Card, lineWidth: 7));
        battery.AddChild(Pen(RoundRect(R(-40, 190, 80, 40), 14), 1101, fill: Palette.Card, lineWidth: 7));
        // Five bars, bottom to top; unlit ones are pale.
        for (var i = 0; i < Bars; i++)
        {
            var bar = Pen(RoundRect(R(-86, -170 + i * 70, 172, 58), 16), (ulong)(1110 + i), fill: Palette.Go, lineWidth: 3);
            battery.AddChild(bar);
            _bars.Add(bar);
        }
        var bolt = Pen(Polygon(P(12, 70), P(-34, 0), P(-4, 0), P(-16, -70), P(34, 6), P(4, 6)), 1120, fill: Palette.Sun, lineWidth: 4);
        bolt.ZIndex = 1;
        battery.AddChild(bolt);
        Stage.AddChild(battery);
        _battery = battery;
    }

    /// <summary>The day's play is used up: Bip asleep under the moon and stars.</summary>
    private void BuildNight()
    {
        AddBip(P(0, -150), 1.1f);
        var moon = new Node2D { Position = P(380, 210), ZIndex = 2 };
        moon.AddChild(Pen(Ellipse(Vector2.Zero, 90, 90), 1130, fill: Palette.Sun, lineWidth: 6));
        // A paper-coloured bite out of the full moon makes the crescent.
        moon.AddChild(Pen(Ellipse(P(42, 26), 78, 78), 1131, fill: Palette.Paper, lineWidth: 0));
        Stage.AddChild(moon);
        foreach (var (i, (x, y, r)) in Indexed((-420.0, 240.0, 28.0), (-560.0, 60.0, 20.0), (-250.0, 120.0, 16.0),
                                               (560.0, 40.0, 22.0), (200.0, 280.0, 18.0)))
        {
            var star = Pen(Polygon(Star(P(x, y), r)), (ulong)(1140 + i), fill: Palette.Sun, lineWidth: 3, wobble: 1);
            Stage.AddChild(star);
            var twinkle = star.CreateTween().SetLoops();
            twinkle.TweenInterval(0.4 * i);
            twinkle.TweenProperty(star, "modulate:a", 0.4f, 0.9);
            twinkle.TweenProperty(star, "modulate:a", 1f, 0.9);
        }
    }

    /// <summary>"z z Z" floating up from Bip's head, over and over.</summary>
    private void AddSnores()
    {
        var head = Bip.Position + new Vector2(70, -300) * Bip.Scale;
        for (var i = 0; i < 3; i++)
        {
            var z = new Node2D { Position = head, ZIndex = 25 };
            z.AddChild(Sketch.Label("z", 40 + i * 14, Palette.Ink.WithAlpha(0.7)));
            z.Modulate = new Color(1, 1, 1, 0);
            Stage.AddChild(z);
            var drift = z.CreateTween().SetLoops();
            drift.TweenInterval(0.9 * i);
            drift.TweenCallback(Callable.From(() => { z.Position = head; z.Modulate = Colors.White; }));
            drift.TweenProperty(z, "position", head + new Vector2(60 + 20 * i, -150), 2.4);
            drift.Parallel().TweenProperty(z, "modulate:a", 0f, 2.4);
            drift.TweenInterval(2.7 - 0.9 * i);
        }
    }

    /// <summary>A label is centred for the text it's made with, so a new line gets a new label.</summary>
    private void SetGrownUpLine(string text)
    {
        if (_when == null || _grownUps?.Text == text) return;
        _grownUps?.QueueFree();
        _grownUps = Sketch.Label(text, 34, Palette.Ink);
        _when.AddChild(_grownUps);
    }

    /// <summary>Lights the battery bars for the time gone and updates the grown-up line.</summary>
    private void ShowTimes()
    {
        if (_dayDone)
        {
            SetGrownUpLine("Games open again tomorrow.");
            return;
        }
        var lit = (int)Math.Floor(Coordinator.BreakProgress() * Bars + 0.0001);
        for (var i = 0; i < _bars.Count; i++) _bars[i].Modulate = new Color(1, 1, 1, i < lit ? 1f : 0.18f);
        if (Coordinator.BreakEndsAt is not { } ends) return;
        var minutes = Coordinator.BreakMinutesLeft();
        SetGrownUpLine($"Games open again at {ends.ToLocalTime():HH:mm} (in {minutes} minute{(minutes == 1 ? "" : "s")}).");
    }

    private void Check()
    {
        if (_waking) return;
        // The break follows the wall clock, so a clock change (or a grown-up ending it) shows here too.
        var phase = Coordinator.CurrentBreakPhase();
        if (phase == BreakPhase.Playing)
        {
            WakeUp();
            return;
        }
        if ((phase == BreakPhase.DayDone) != _dayDone)
        {
            // A break ran into the daily limit, or a new day started a break: rebuild in the right mode.
            Coordinator.ShowCharging();
            return;
        }
        ShowTimes();
        _sinceBeep += CheckSeconds;
        if (_sinceBeep >= 60 && _battery != null)
        {
            // Once a minute: a little beep and the battery bounces, so the screen doesn't look stuck.
            _sinceBeep = 0;
            Sfx.Play(BipSounds.Effect.Beep);
            var bounce = _battery.CreateTween();
            bounce.TweenProperty(_battery, "scale", Vector2.One * 1.06f, 0.15);
            bounce.TweenProperty(_battery, "scale", Vector2.One, 0.2);
        }
        After(CheckSeconds, Check);
    }

    /// <summary>The break is over: the battery fills, Bip wakes up happy, and the map opens.</summary>
    private void WakeUp()
    {
        _waking = true;
        foreach (var bar in _bars) bar.Modulate = Colors.White;
        SetGrownUpLine("Charged! Off we go.");
        Bip.Sleep(false);
        Bip.Celebrate();
        Sfx.Play(BipSounds.Effect.Chime);
        After(1.6, Coordinator.ShowMap);
    }

    protected override void DidTapBip() => ReplayPrompt();

    public override void ReplayPrompt()
    {
        if (!_waking) Voice.Play([_dayDone ? VoiceLine.DayDone : VoiceLine.BipCharging]);
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
