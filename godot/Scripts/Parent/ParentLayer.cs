using System;
using BipCore;
using BipIsland.App;
using BipIsland.Drawing;
using BipIsland.Game;
using Godot;

namespace BipIsland.Parent;

/// <summary>
/// The grown-ups' layer over the game (the Swift app's RootView, ParentGateView and key handling):
/// - hold Esc for 3 seconds (a ring fills in the corner) → passcode or maths → the parent area;
/// - a fresh Esc press while it's open goes back to the game;
/// - the "Update ready" button appears when a new version has downloaded, and installs it only
///   after the same passcode or maths.
/// The game is paused while the layer is open.
/// </summary>
public partial class ParentLayer : CanvasLayer
{
    public ParentGateFlow Flow { get; private set; } = null!;
    public bool IsOpen => Flow.Phase != GatePhase.Closed;
    /// <summary>The parent area while it's showing (for the walk-through test).</summary>
    public ParentArea? Area { get; private set; }

    private GameCoordinator Coordinator => GameCoordinator.Instance;
    private HoldRing _ring = null!;
    private Button _updateButton = null!;
    private Control? _panel;
    private LineEdit? _answer;
    private bool _changed;
    private bool _updateButtonForTest;
    private string? _installProblem;

    private static double Now => Time.GetTicksMsec() / 1000.0;

    public override void _Ready()
    {
        Layer = 90;
        ProcessMode = ProcessModeEnum.Always;
        Flow = new ParentGateFlow(Coordinator.Rng, () => Coordinator.Store.Passcode);
        Flow.Opened += OnOpened;
        Flow.Closed += OnClosed;
        // On success the game quits and restarts as the new version; otherwise say why in Settings.
        Flow.InstallUpdate += () => _installProblem = Updater.InstallAndRestart();

        var corner = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        corner.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(corner);
        _ring = new HoldRing { Visible = false };
        _ring.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _ring.Position = new Vector2(-24 - HoldRing.Size, 24);
        corner.AddChild(_ring);

        _updateButton = MakeUpdateButton();
        corner.AddChild(_updateButton);
        Updater.UpdateReady += _ => RefreshUpdateButton();
        RefreshUpdateButton();
    }

    // Keys

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Keycode: Key.Escape } key) return;
        GetViewport().SetInputAsHandled();
        if (IsOpen)
        {
            // A fresh press goes back to the game; the first press's repeats and release don't.
            if (key.Pressed && !key.Echo) Flow.Close();
            return;
        }
        if (key.Pressed) Flow.EscapePressed(Now);
        else Flow.EscapeReleased();
    }

    public override void _Process(double delta)
    {
        var now = Now;
        // A missed key-up (the window lost focus mid-hold) mustn't leave the hold running.
        if (!IsOpen && !Input.IsKeyPressed(Key.Escape)) Flow.EscapeReleased();
        Flow.Tick(now);
        var progress = Flow.HoldProgress(now);
        _ring.Visible = progress > 0;
        _ring.Progress = (float)progress;
        _updateButton.Visible = !IsOpen && progress <= 0 && (Updater.ReadyVersion != null || _updateButtonForTest);
    }

    // Opening and closing

    private void OnOpened()
    {
        _changed = false;
        // Bip's voice and every screen's timers pause with the tree and carry on afterwards.
        GetTree().Paused = true;
        // Time with the gate or the parent area open isn't play time.
        Coordinator.PauseBreakClock();
        ShowQuestion();
    }

    private void OnClosed()
    {
        ClearPanel();
        GetTree().Paused = false;
        Coordinator.ResumeBreakClock();
        // Children or settings changed: start again from the map as the (possibly new) child.
        if (_changed) Coordinator.ShowMap();
        _changed = false;
    }

    /// <summary>Something a parent changed means the screen behind should be redrawn on close.</summary>
    public void MarkChanged() => _changed = true;

    private void ClearPanel()
    {
        _panel?.QueueFree();
        _panel = null;
        _answer = null;
        Area = null;
    }

    /// <summary>A dimmed full-screen backdrop with a centred card; returns the card's content column.</summary>
    private VBoxContainer Card(int width, int height = 0)
    {
        ClearPanel();
        var backdrop = new ColorRect { Color = new Color(0, 0, 0, 0.55f), MouseFilter = Control.MouseFilterEnum.Stop, Theme = ParentUi.Theme };
        backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var centre = new CenterContainer();
        centre.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        backdrop.AddChild(centre);
        var card = new PanelContainer { CustomMinimumSize = new Vector2(width, height) };
        card.AddThemeStyleboxOverride("panel", ParentUi.Box(Palette.Paper, Palette.Ink, 4, 28, 32, 28));
        centre.AddChild(card);
        var column = ParentUi.Column(16);
        card.AddChild(column);
        AddChild(backdrop);
        _panel = backdrop;
        return column;
    }

    // The question

    private void ShowQuestion()
    {
        var column = Card(520);
        column.Alignment = BoxContainer.AlignmentMode.Center;
        Centred(column, ParentUi.Text("Grown-ups only", 30, bold: true));
        var passcode = Flow.UsingPasscode;
        Centred(column, ParentUi.Text(passcode ? "Enter the parent passcode" : $"What is {Flow.Challenge.Question}?", 26, bold: true));

        _answer = new LineEdit
        {
            PlaceholderText = passcode ? "Passcode" : "Answer",
            Secret = passcode,
            Alignment = HorizontalAlignment.Center,
            CustomMinimumSize = new Vector2(200, 0),
            VirtualKeyboardType = LineEdit.VirtualKeyboardTypeEnum.Number,
        };
        _answer.AddThemeFontSizeOverride("font_size", 26);
        _answer.TextSubmitted += _ => Submit();
        Centred(column, _answer);

        if (Flow.LastAnswerWasWrong)
        {
            var text = passcode ? "That's not the passcode." : Flow.HasPasscode ? "Answer this one to come in." : "Not quite. Try this one.";
            Centred(column, ParentUi.Text(text, 18, colour: ParentUi.Problem));
        }
        if (passcode)
        {
            var forgot = new LinkButton { Text = "Forgot it? Answer a maths question instead", Underline = LinkButton.UnderlineMode.Always };
            forgot.Pressed += () =>
            {
                Flow.UseMathsInstead();
                ShowQuestion();
            };
            Centred(column, forgot);
        }
        var buttons = ParentUi.Row(16, ParentUi.Button("Back to the game", Flow.Close), ParentUi.Button("Continue", Submit));
        buttons.Alignment = BoxContainer.AlignmentMode.Center;
        column.AddChild(buttons);
        _answer.CallDeferred(Control.MethodName.GrabFocus);
    }

    private static void Centred(VBoxContainer column, Control control)
    {
        control.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        column.AddChild(control);
    }

    /// <summary>Checks the typed answer (also used by the walk-through test).</summary>
    public void Submit(string answer)
    {
        if (Flow.Submit(answer)) ShowArea();
        else if (Flow.Phase == GatePhase.Question) ShowQuestion();
    }

    private void Submit() => Submit(_answer?.Text ?? "");

    // The parent area

    private void ShowArea()
    {
        if (Flow.Phase != GatePhase.Unlocked) return;
        var column = Card(940, 640);
        Area = new ParentArea(this) { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        column.AddChild(Area);
        if (_installProblem is { } problem) Area.ShowSettings(problem);
        _installProblem = null;
    }

    /// <summary>Back to the game from the parent area or the question.</summary>
    public void Close() => Flow.Close();

    // The update button

    private Button MakeUpdateButton()
    {
        var button = new Button { Theme = ParentUi.Theme, Visible = false, FocusMode = Control.FocusModeEnum.None };
        button.AddThemeStyleboxOverride("normal", ParentUi.Box(Palette.Card, Palette.Ink, 3, 30, 22, 12));
        button.AddThemeStyleboxOverride("hover", ParentUi.Box(new Color(Palette.Sun, 0.5f), Palette.Ink, 3, 30, 22, 12));
        button.AddThemeStyleboxOverride("pressed", ParentUi.Box(Palette.Sun, Palette.Ink, 3, 30, 22, 12));
        button.AddThemeFontOverride("font", Fonts.Bold);
        button.Text = "Update ready\nGrown-ups: tap here";
        // Bottom centre: clear of the sticker book, the mystery box and the games' level badge.
        button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom, Control.LayoutPresetMode.Minsize, 24);
        button.GrowVertical = Control.GrowDirection.Begin;
        button.GrowHorizontal = Control.GrowDirection.Both;
        button.Pressed += Flow.OpenForUpdate;
        return button;
    }

    private void RefreshUpdateButton() => Area?.RefreshVersion();

    /// <summary>For screenshots and tests: show the update button as if a version had downloaded.</summary>
    public void ShowUpdateButtonForTest() => _updateButtonForTest = true;

    /// <summary>The update button, if it's showing (for the walk-through test).</summary>
    public Button? UpdateButton => _updateButton.Visible ? _updateButton : null;
}

/// <summary>A small ring in the corner that fills while Esc is held.</summary>
public partial class HoldRing : Control
{
    public new const float Size = 44;
    private float _progress;

    public float Progress
    {
        get => _progress;
        set
        {
            if (Mathf.IsEqualApprox(_progress, value)) return;
            _progress = value;
            QueueRedraw();
        }
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(Size, Size);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        var centre = new Vector2(Size / 2, Size / 2);
        DrawArc(centre, Size / 2 - 3, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * _progress, 48, new Color(Palette.Ink, 0.6f), 6, antialiased: true);
    }
}
