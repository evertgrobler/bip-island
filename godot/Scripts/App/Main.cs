using System.Collections.Generic;
using BipIsland.Audio;
using BipIsland.Drawing;
using Godot;

namespace BipIsland.App;

/// <summary>
/// Phase 0's one screen: Bip, a letter, a picture and Bip's hello, to prove the drawing, the font,
/// the emoji pictures and the narrator clips all work on Mac and Windows. Click Bip, the letter or
/// the picture to hear them; any key plays the last sound again.
/// </summary>
public partial class Main : Node2D
{
    /// <summary>Every screen is laid out on this canvas around (0, 0), like the Swift app's scenes.</summary>
    public static readonly Vector2 SceneSize = new(1600, 1000);

    private readonly Node2D _stage = new();
    private readonly VoicePlayer _voice = new();
    private readonly List<(Node2D node, Rect2 area, string[] clips)> _targets = new();
    private TextureRect _paper = null!;
    private Bip _bip = null!;
    private string[] _lastClips = { "vo_welcome" };

    public override void _Ready()
    {
        AddChild(_voice);
        _paper = new TextureRect
        {
            Texture = Paper.Texture,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_paper);
        AddChild(_stage);
        Build();
        GetViewport().SizeChanged += Layout;
        Layout();

        if (!SelfTest.IsRequested(Boot.UserArgs)) _voice.Play(_lastClips);
    }

    private void Layout()
    {
        var visible = GetViewportRect().Size;
        _paper.Size = visible;
        _stage.Position = visible / 2;
    }

    private void Build()
    {
        var title = Sketch.Label("Bip Island", 72, Palette.Ink, Fonts.Bold);
        title.Position += new Vector2(0, -400);
        _stage.AddChild(title);

        // Bip and his speech bubble.
        _bip = new Bip { Position = new Vector2(-470, 330), Scale = new Vector2(1.25f, 1.25f) };
        _stage.AddChild(_bip);
        _targets.Add((_bip, new Rect2(-600, -30, 260, 370), new[] { "vo_welcome" }));

        var bubble = new Node2D { Position = new Vector2(-240, -190) };
        bubble.AddChild(Sketch.Node(new SketchShape.RoundedRect(new Rect2(-250, -95, 500, 190), 40), 7, fill: Palette.Card));
        bubble.AddChild(Sketch.Node(new SketchShape.Polygon(new[] { new Vector2(-150, 92), new Vector2(-210, 190), new Vector2(-80, 94) }), 9, fill: Palette.Card, lineWidth: 0));
        var hello = Sketch.Label("Hello! I'm Bip.\nClick me!", 46, Palette.Ink, Fonts.Regular);
        bubble.AddChild(hello);
        _stage.AddChild(bubble);

        // A letter sound card: s says "sss".
        var letter = new Node2D { Position = new Vector2(180, 80) };
        letter.AddChild(Sketch.Node(new SketchShape.RoundedRect(new Rect2(-150, -170, 300, 340), 36), 21, fill: Palette.Sea));
        letter.AddChild(Sketch.Letter("s", 240));
        _stage.AddChild(letter);
        _targets.Add((letter, new Rect2(30, -90, 300, 340), new[] { "snd_s" }));

        // A picture card: the emoji stand-in for the sun, with its word.
        var picture = new Node2D { Position = new Vector2(540, 80) };
        picture.AddChild(Sketch.Node(new SketchShape.RoundedRect(new Rect2(-150, -170, 300, 340), 36), 33, fill: Palette.Card));
        var sun = Sketch.Label("☀️", 150, Palette.Ink);
        sun.Position += new Vector2(0, -30);
        picture.AddChild(sun);
        var word = Sketch.Label("sun", 64, Palette.Ink);
        word.Position += new Vector2(0, 110);
        picture.AddChild(word);
        _stage.AddChild(picture);
        _targets.Add((picture, new Rect2(390, -90, 300, 340), new[] { "word_sun" }));

        var version = Updater.InstalledVersion ?? ProjectSettings.GetSetting("application/config/version").AsString();
        var footer = Sketch.Label($"Godot test build {version}. Hold Esc for 3 seconds to leave.", 26, new Color(Palette.Ink, 0.6f), Fonts.Regular);
        footer.Position += new Vector2(0, 450);
        _stage.AddChild(footer);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            var point = _stage.ToLocal(click.Position);
            foreach (var (node, area, clips) in _targets)
            {
                if (!area.HasPoint(point)) continue;
                if (node == _bip) _bip.Hop();
                else Bounce(node);
                Say(clips);
                return;
            }
        }
        else if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: not Key.Escape })
        {
            // Any key = "play that sound again".
            Say(_lastClips);
        }
    }

    private void Say(string[] clips)
    {
        _lastClips = clips;
        _voice.Play(clips);
    }

    private void Bounce(Node2D node)
    {
        var tween = CreateTween();
        tween.TweenProperty(node, "scale", new Vector2(1.08f, 1.08f), 0.1);
        tween.TweenProperty(node, "scale", Vector2.One, 0.15);
    }
}
