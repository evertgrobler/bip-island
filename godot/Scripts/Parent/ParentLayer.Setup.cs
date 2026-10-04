using BipCore;
using BipIsland.App;
using BipIsland.Drawing;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Parent;

/// <summary>
/// The first-time setup guide for grown-ups (steps in <see cref="SetupGuide"/>): what the child lock
/// does and practising the way into the grown-up area, an optional passcode, the children, play time,
/// a short tour of what the child sees, then handing over. It opens by itself the first time the game
/// runs on a computer (<see cref="SaveStore.IsNew"/>), and again from Settings. The game is paused
/// behind it, as behind the parent area. On a first run nobody has passed the gate, so the parent
/// controls in it come only after practising the gate (<see cref="SetupGuide.GatedAlready"/>).
/// </summary>
public partial class ParentLayer
{
    /// <summary>The guide while it's showing (for the walk-through test and screenshots).</summary>
    public SetupGuide? Guide { get; private set; }

    private bool _guideFirstRun;

    /// <summary>Opens the setup guide at its first step: by itself on a first run, or from Settings.</summary>
    public void ShowSetupGuide(bool firstRun = false)
    {
        var wasOpen = IsOpen || Guide != null;
        Guide = new SetupGuide(gatedAlready: !firstRun);
        _guideFirstRun = firstRun;
        if (firstRun) _changed = false;
        if (Flow.Phase != GatePhase.Closed)
        {
            // From Settings: the gate closes behind the guide (OnClosed sees the guide and stays paused).
            Flow.Close();
            return;
        }
        if (!wasOpen)
        {
            GetTree().Paused = true;
            Coordinator.PauseBreakClock();
        }
        ShowGuideStep();
    }

    /// <summary>The guide's Next (also used by the walk-through test).</summary>
    public void GuideNext()
    {
        if (Guide is not { } guide) return;
        // A passcode typed but not saved yet is saved now; if it can't be, stay and show why.
        if (guide.Current == SetupGuide.Step.Passcode && Area?.SavePendingPasscode() != null) return;
        if (!guide.Next()) return;
        if (guide.Done) FinishGuide();
        else ShowGuideStep();
    }

    public void GuideBack()
    {
        if (Guide?.Back() == true) ShowGuideStep();
    }

    public void GuideSkipStep()
    {
        if (Guide?.SkipStep() == true) ShowGuideStep();
    }

    /// <summary>Ends the guide (finished or skipped): marks it done and hands the game to the child.</summary>
    public void FinishGuide()
    {
        if (Guide == null) return;
        Guide = null;
        Coordinator.Store.MarkSetupDone();
        ClearPanel();
        GetTree().Paused = false;
        Coordinator.ResumeBreakClock();
        var changed = _changed;
        _changed = false;
        if (_guideFirstRun)
        {
            // The child's first look at the game: "Who's playing?" or the map, with Bip's welcome.
            Coordinator.ResetWelcome();
            Coordinator.Start();
        }
        // From Settings: back to the same screen, unless children or settings changed (as the parent area does).
        else if (changed) Coordinator.ShowMap();
    }

    /// <summary>For the walk-through test: the hold ring is showing, drawn above the guide's card.</summary>
    public bool RingShowsOverCard => _ring.Visible && _panel != null && _ringCorner.GetIndex() > _panel.GetIndex();

    /// <summary>While practising, a right answer comes back to the guide instead of opening the parent area.</summary>
    private bool PractisingGate => Guide is { Current: SetupGuide.Step.GrownUpArea };

    private void ShowGuideStep()
    {
        if (Guide is not { } guide) return;
        var column = Card(860);
        column.AddChild(ParentUi.Row(12,
            ParentUi.Text($"Setting up Bip Island · step {guide.Number} of {SetupGuide.Count}", 16, colour: ParentUi.Secondary),
            ParentUi.Spacer(),
            ParentUi.Button("Skip setup", FinishGuide)));

        switch (guide.Current)
        {
            case SetupGuide.Step.Welcome: GuideWelcome(column); break;
            case SetupGuide.Step.GrownUpArea: GuideGrownUpArea(column, guide); break;
            case SetupGuide.Step.Passcode: GuidePart(column, "A parent passcode (optional)",
                "If you'd rather not answer a maths question each time, set a passcode of 4 to 8 digits. You can skip this and use the maths questions.",
                ParentArea.Part.Passcode); break;
            case SetupGuide.Step.Children: GuidePart(column, "Your children",
                "Add each child who will play (up to four), with their name, age and an animal picture." +
                (_guideFirstRun ? " The first child you add replaces \"Player 1\"." : "") +
                " Each child picks their animal when the game opens; their age sets where they start, and the game then adjusts to how they do.",
                ParentArea.Part.Children); break;
            case SetupGuide.Step.PlayTime: GuidePart(column, "Play time and breaks",
                "After the play time, Bip's battery runs low: your child finishes the game they're in, then Bip charges and the games stay closed for the break. Quitting and reopening the game doesn't skip a break. You can change these any time.",
                ParentArea.Part.PlayTime); break;
            case SetupGuide.Step.Tour: GuideTour(column, guide.TourCard); break;
            case SetupGuide.Step.AllSet: GuideAllSet(column); break;
        }

        var back = ParentUi.Button("Back", GuideBack);
        back.Disabled = guide.Current == SetupGuide.Step.Welcome;
        var buttons = ParentUi.Row(16, back, ParentUi.Spacer());
        if (guide.CanSkipStep)
            buttons.AddChild(ParentUi.Button(guide.Current == SetupGuide.Step.Tour ? "Skip the tour" : "Skip this step", GuideSkipStep));
        var next = ParentUi.Button(guide.Current == SetupGuide.Step.AllSet ? "Hand over to your child" : "Next", GuideNext);
        next.Disabled = !guide.CanGoOn;
        buttons.AddChild(next);
        column.AddChild(buttons);
        if (!next.Disabled) next.CallDeferred(Control.MethodName.GrabFocus);
    }

    private static void GuideWelcome(VBoxContainer column)
    {
        column.AddChild(ParentUi.Text("Welcome to Bip Island", 30, bold: true));
        column.AddChild(ParentUi.Text(
            "Let's get it ready for your children. It takes about 3 minutes: how to reach the grown-up area, your children, play time, and what your child will see.",
            18, wrap: true));
        column.AddChild(ParentUi.Text("This guide is for grown-ups. Your child doesn't need to read anything to play.", 16, colour: ParentUi.Secondary, wrap: true));
    }

    private void GuideGrownUpArea(VBoxContainer column, SetupGuide guide)
    {
        column.AddChild(ParentUi.Text("The child lock and the grown-up area", 26, bold: true));
        column.AddChild(ParentUi.Text(LockText(), 17, wrap: true));
        column.AddChild(ParentUi.Text(
            "To leave the game, change settings or see your child's progress, hold the Esc key for 3 seconds (a ring fills in the top corner), then answer a maths question, or enter your passcode if you set one.",
            17, bold: true, wrap: true));
        if (guide.PractisedGate)
            column.AddChild(ParentUi.Text("Well done: that's how you open the grown-up area any time.", 18, bold: true, colour: Palette.Leaf.Darkened(0.35f), wrap: true));
        else
            column.AddChild(ParentUi.Text("Try it now: press and hold Esc for 3 seconds.", 20, bold: true, colour: Palette.Orange.Darkened(0.25f), wrap: true));
        column.AddChild(ParentUi.Text(
            "Tip: hold Option (Mac) or Alt (Windows) while the game opens to play in a normal window without the lock.",
            15, colour: ParentUi.Secondary, wrap: true));
    }

    private static string LockText() => KidLock.Status switch
    {
        "mac" => "While Bip Island is open, the child lock stops your child switching to other apps or quitting by accident: the Dock, menu bar, Cmd-Tab and Cmd-Q are blocked. Shutting down and restarting always work.",
        "windows" => "While Bip Island is open, the child lock stops your child switching to other apps or quitting by accident: the Windows key, Alt-Tab and Alt-F4 are blocked. Ctrl-Alt-Del always works.",
        _ => "On the family computer, the child lock stops your child switching to other apps or quitting by accident. Shutting down always works.",
    };

    private void GuidePart(VBoxContainer column, string title, string text, ParentArea.Part part)
    {
        column.AddChild(ParentUi.Text(title, 26, bold: true));
        column.AddChild(ParentUi.Text(text, 17, wrap: true));
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 360),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        var area = new ParentArea(this, part) { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(area);
        column.AddChild(scroll);
        Area = area;
    }

    private static void GuideTour(VBoxContainer column, int card)
    {
        var (title, text) = card switch
        {
            0 => ("The island map",
                "Your child picks any island and any game. Bip suggests one by making it glow, and nudges towards another island when one gets all the attention, but never forces a game."),
            1 => ("Stars and stickers",
                "Right answers earn stars, stars fill the sticker book, and a mystery box opens once a day. Nothing is ever bought, and there are no timers or \"game over\": a wrong answer just gets another go, then a spoken hint."),
            _ => ("Bip's breaks",
                "When the play time is up, Bip charges his battery and the games stay closed until the break ends. When a new version is ready, an \"Update ready\" button appears for you; it installs only after the grown-up question."),
        };
        column.AddChild(ParentUi.Text($"What your child will see ({card + 1} of {SetupGuide.TourCards})", 16, colour: ParentUi.Secondary));
        column.AddChild(ParentUi.Text(title, 26, bold: true));
        var picture = TourPicture(card);
        picture.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        column.AddChild(picture);
        column.AddChild(ParentUi.Text(text, 17, wrap: true));
    }

    private static void GuideAllSet(VBoxContainer column)
    {
        column.AddChild(ParentUi.Text("All set", 30, bold: true));
        column.AddChild(ParentUi.Text("Remember: hold Esc for 3 seconds any time for the grown-up area.", 20, bold: true, wrap: true));
        column.AddChild(ParentUi.Text(
            "There you can see each child's progress, add or change children, change play time and the passcode, install updates and quit. This guide is there too, under Settings.",
            17, wrap: true));
    }

    /// <summary>A small drawing for each tour card, in the game's hand-drawn style.</summary>
    private static Control TourPicture(int card)
    {
        var viewport = new SubViewport { Size = new Vector2I(420, 210), TransparentBg = true, Disable3D = true };
        var stage = new Node2D { Position = new Vector2(210, 110) };
        viewport.AddChild(stage);
        switch (card)
        {
            case 0:
                foreach (var (i, (x, colour)) in Indexed((-150.0, Palette.Grass), (-30.0, Palette.Sand), (90.0, Palette.Teal)))
                {
                    if (i == 1) stage.AddChild(Pen(Ellipse(P(x, 20), 66, 42), 1500, ink: Palette.Sun, lineWidth: 10));
                    stage.AddChild(Pen(Ellipse(P(x, 20), 54, 32), (ulong)(1501 + i), fill: colour, lineWidth: 4));
                }
                stage.AddChild(new Bip(1510) { Position = new Vector2(170, 40), Scale = Vector2.One * 0.4f });
                break;
            case 1:
                foreach (var i in new[] { 0, 1, 2 })
                    stage.AddChild(Pen(Polygon(Star(P(-150 + i * 60, 20), 24)), (ulong)(1520 + i), fill: Palette.Sun, lineWidth: 3));
                stage.AddChild(Pen(RoundRect(R(40, -60, 140, 110), 14), 1530, fill: Palette.Card, lineWidth: 4));
                var sticker = PictureNode.Make("pic_sun", "sun");
                sticker.Scale = Vector2.One * 0.35f;
                sticker.Position = P(110, -5);
                stage.AddChild(sticker);
                break;
            default:
                stage.AddChild(new Bip(1540) { Position = new Vector2(-70, 10), Scale = Vector2.One * 0.45f });
                stage.AddChild(Pen(RoundRect(R(40, -70, 80, 130), 12), 1541, fill: Palette.Card, lineWidth: 5));
                stage.AddChild(Pen(RoundRect(R(65, 60, 30, 14), 4), 1542, fill: Palette.Card, lineWidth: 4));
                foreach (var i in new[] { 0, 1 })
                    stage.AddChild(Pen(Polygon(P(48, -62 + i * 30), P(112, -62 + i * 30), P(112, -38 + i * 30), P(48, -38 + i * 30)), (ulong)(1543 + i), fill: Palette.Go, lineWidth: 3));
                break;
        }
        var container = new SubViewportContainer { CustomMinimumSize = new Vector2(420, 210) };
        container.AddChild(viewport);
        return container;
    }
}
