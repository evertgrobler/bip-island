using System;
using System.Collections.Generic;
using System.Linq;
using BipIsland.Audio;
using BipIsland.Drawing;
using BipIsland.Game;
using Godot;

namespace BipIsland.Screens;

/// <summary>
/// What every screen shares (the Swift app's BaseScene): the paper background, Bip, the home button,
/// clicks routed by tap name, keyboard play and timers that stop with the screen.
/// Screens are laid out on a 1600 × 1000 canvas around (0, 0), y downwards; keep important things
/// within ±780 × ±430 so 16:9 and 16:10 screens both show them.
/// </summary>
public abstract partial class BaseScreen : Node2D
{
    /// <summary>Ignore clicks while something important is animating or being said.</summary>
    public bool InputLocked { get; set; }

    /// <summary>Everything the screen draws goes in here; it stays centred on the window.</summary>
    protected Node2D Stage { get; } = new() { Name = "Stage" };
    protected Bip Bip { get; } = Buttons.Tappable(new Bip(), "bip");
    protected GameCoordinator Coordinator => GameCoordinator.Instance;
    protected VoicePlayer Voice => Coordinator.Voice;
    protected BipSounds Sfx => Coordinator.Sounds;

    private TextureRect _paper = null!;
    private Node2D? _keyRing;
    private int _keyIndex;

    public override void _Ready()
    {
        _paper = new TextureRect
        {
            Texture = Paper.Texture,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = -100,
        };
        AddChild(_paper);
        AddChild(Stage);
        GetViewport().SizeChanged += Layout;
        Layout();
        Build();
    }

    public override void _ExitTree()
    {
        if (GetViewport() is { } viewport) viewport.SizeChanged -= Layout;
    }

    private void Layout()
    {
        var visible = GetViewportRect().Size;
        _paper.Size = visible;
        Stage.Position = visible / 2;
    }

    // For screens

    /// <summary>Draws the screen. Called once, when it opens.</summary>
    protected abstract void Build();
    protected virtual void HandleTap(string name, Node2D node) { }
    protected virtual void DidTapBip() { }
    /// <summary>Back to the map; the map and first screens override this to do nothing.</summary>
    protected virtual void GoHome() => Coordinator.ShowMap();
    /// <summary>Any key: say the current sound or instruction again.</summary>
    public virtual void ReplayPrompt() { }
    /// <summary>The targets the arrow keys and Enter can use right now. Empty means keys just replay.</summary>
    protected virtual IReadOnlyList<Node2D> KeyOptions => [];

    protected void AddHomeButton()
    {
        var home = Buttons.Home();
        home.Position = Up.P(-700, 360);
        home.ZIndex = 40;
        Stage.AddChild(home);
    }

    protected void AddBip(Vector2 at, float scale = 1)
    {
        Bip.Position = at;
        Bip.Scale = new Vector2(scale, scale);
        Bip.ZIndex = 20;
        Stage.AddChild(Bip);
    }

    /// <summary>
    /// Starts a game, or (until its screen is ported in Phase 3 of the Godot move) shows a "coming
    /// soon" card for the test build and unlocks the screen again.
    /// </summary>
    protected void StartGame(string gameId)
    {
        if (Coordinator.StartGame(gameId)) return;
        var card = new Node2D { ZIndex = 80, Scale = Vector2.One * 0.01f };
        card.AddChild(Sketch.Node(new SketchShape.RoundedRect(new Rect2(-360, -110, 720, 220), 36), 575, fill: Palette.Card, lineWidth: 6));
        card.AddChild(Sketch.Label("Coming soon!", 64, Palette.Ink));
        Stage.AddChild(card);
        var pop = card.CreateTween();
        pop.TweenProperty(card, "scale", Vector2.One * 1.05f, 0.2);
        pop.TweenProperty(card, "scale", Vector2.One, 0.1);
        pop.TweenInterval(1.4);
        pop.TweenProperty(card, "modulate:a", 0f, 0.3);
        pop.TweenCallback(Callable.From(card.QueueFree));
        Bip.Tilt();
        After(1.9, () => InputLocked = false);
    }

    /// <summary>Runs <paramref name="action"/> after a delay, unless the screen has gone by then.</summary>
    protected void After(double seconds, Action action)
    {
        GetTree().CreateTimer(seconds).Timeout += () =>
        {
            // The screen may have been closed (and freed) while the timer ran.
            if (IsInstanceValid(this) && IsInsideTree()) action();
        };
    }

    // Clicks

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            GetViewport().SetInputAsHandled();
            Tap(click.GlobalPosition);
        }
        else if (@event is InputEventKey { Pressed: true, Echo: false } key && key.Keycode != Key.Escape)
        {
            GetViewport().SetInputAsHandled();
            if (!HandleKey(key.Keycode)) ReplayPrompt();
        }
    }

    /// <summary>Handles a click at a canvas position: the topmost tappable node under it wins.</summary>
    public void Tap(Vector2 at)
    {
        var target = Tappables(Stage)
            .Where(n => Hit.IsShown(n) && Hit.GlobalArea(n) is { } area && area.HasPoint(at))
            .OrderByDescending(Depth)
            .FirstOrDefault();
        if (target == null || Buttons.TapName(target) is not { } name) return;

        // Bip and the home button always work, even mid-question.
        if (name == "bip")
        {
            Sfx.Play(BipSounds.Effect.Beep);
            Bip.Hop();
            DidTapBip();
            return;
        }
        if (name == "home")
        {
            Sfx.Play(BipSounds.Effect.Tick);
            Buttons.Press(target);
            GoHome();
            return;
        }
        if (InputLocked) return;
        HandleTap(name, target);
    }

    /// <summary>The tappable node with this tap name, if the screen shows one (for the walk-through test).</summary>
    public Node2D? FindTappable(string name) =>
        Tappables(Stage).FirstOrDefault(n => Buttons.TapName(n) == name && Hit.IsShown(n));

    private static IEnumerable<Node2D> Tappables(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Node2D n && Buttons.TapName(n) != null) yield return n;
            foreach (var inner in Tappables(child)) yield return inner;
        }
    }

    /// <summary>Higher z first; among equals, the one drawn later (on top).</summary>
    private static DepthKey Depth(Node2D node)
    {
        var z = 0;
        for (Node? n = node; n is CanvasItem c; n = n.GetParent())
        {
            z += c.ZIndex;
            if (!c.ZAsRelative) break;
        }
        return new DepthKey(z, DrawOrder(node));
    }

    /// <summary>The child index at each level from the root down: later in the tree is drawn on top.</summary>
    private static List<int> DrawOrder(Node node)
    {
        var path = new List<int>();
        for (Node? n = node; n?.GetParent() != null; n = n.GetParent()) path.Add(n.GetIndex());
        path.Reverse();
        return path;
    }

    private sealed record DepthKey(int Z, List<int> Path) : IComparable<DepthKey>
    {
        public int CompareTo(DepthKey? other)
        {
            if (other == null) return 1;
            if (Z != other.Z) return Z.CompareTo(other.Z);
            for (var i = 0; i < Math.Min(Path.Count, other.Path.Count); i++)
                if (Path[i] != other.Path[i]) return Path[i].CompareTo(other.Path[i]);
            // A child is drawn after its parent.
            return Path.Count.CompareTo(other.Path.Count);
        }
    }

    // Keyboard play: left/right moves the glow, Enter or Space chooses, 1-3 chooses directly.

    /// <summary>Returns true when the key did something; anything else falls back to replay.</summary>
    public bool HandleKey(Key key)
    {
        if (InputLocked) return true;
        var options = KeyOptions.Where(n => n.IsInsideTree() && Hit.IsShown(n)).ToList();
        switch (key)
        {
            case Key.Left or Key.Right or Key.Up or Key.Down:
                if (options.Count == 0) return false;
                var step = key is Key.Left or Key.Up ? options.Count - 1 : 1;
                _keyIndex = (_keyIndex + step) % options.Count;
                ShowKeyRing(options[_keyIndex]);
                Sfx.Play(BipSounds.Effect.Tick);
                return true;
            case Key.Enter or Key.KpEnter or Key.Space:
                if (options.Count == 0) return false;
                _keyIndex = Math.Min(_keyIndex, options.Count - 1);
                Choose(options[_keyIndex]);
                return true;
            case Key.Key1 or Key.Key2 or Key.Key3:
                var index = (int)(key - Key.Key1);
                if (index >= options.Count) return false;
                _keyIndex = index;
                Choose(options[index]);
                return true;
            default:
                return false;
        }
    }

    private void Choose(Node2D node)
    {
        HideKeyRing();
        HandleTap(Buttons.TapName(node) ?? "", node);
    }

    private void ShowKeyRing(Node2D node)
    {
        if (_keyRing == null)
        {
            _keyRing = Sketch.Node(new SketchShape.Ellipse(Vector2.Zero, 105, 105), 550, ink: Palette.Orange, lineWidth: 7);
            _keyRing.ZIndex = 60;
            Stage.AddChild(_keyRing);
        }
        _keyRing.Visible = true;
        _keyRing.Position = Stage.ToLocal(node.GlobalPosition);
    }

    private void HideKeyRing()
    {
        if (_keyRing != null) _keyRing.Visible = false;
    }

    /// <summary>New question, new targets: the glow starts over.</summary>
    protected void ResetKeys()
    {
        _keyIndex = 0;
        HideKeyRing();
    }
}
