using System.Collections.Generic;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Screens;

/// <summary>
/// Art Island: three game buttons on a painter's island — Shape Builder (shapes), Paint Pots (mixing
/// colours) and Mirror Magic (symmetry). Bip starts the left one; the child can pick any.
/// </summary>
public partial class ArtIslandScreen : BaseScreen
{
    private readonly bool _greet;
    private readonly List<Node2D> _keyNodes = [];

    public ArtIslandScreen() : this(false) { }
    public ArtIslandScreen(bool greet) => _greet = greet;

    protected override IReadOnlyList<Node2D> KeyOptions => _keyNodes;

    protected override void Build()
    {
        var island = Pen(Ellipse(P(0, -20), 760, 430), 3000, fill: Palette.Sand, ink: Palette.LightBrown, lineWidth: 7);
        island.ZIndex = -50;
        Stage.AddChild(island);
        var inner = Pen(Ellipse(P(0, 30), 680, 330), 3001, fill: Palette.Pink.Blend(0.5, Palette.Purple).WithAlpha(0.45),
                        ink: Palette.Purple, lineWidth: 5);
        inner.ZIndex = -49;
        Stage.AddChild(inner);

        foreach (var (button, x) in new[] { (ShapesButton(), -420.0), (PaintButton(), 0.0), (MirrorButton(), 420.0) })
        {
            button.Position = P(x, 110);
            button.ZIndex = 5;
            Stage.AddChild(button);
            _keyNodes.Add(button);
        }
        Buttons.Pulse(_keyNodes[0]);

        AddHomeButton();
        AddBip(P(0, -380), 0.8f);
        if (_greet) After(0.5, ReplayPrompt);
    }

    private static Node2D Disc(ulong seed, Color fill) =>
        Pen(Ellipse(Vector2.Zero, 170, 160), seed, fill: fill, lineWidth: 6);

    private static Node2D ShapesButton()
    {
        var button = Buttons.Tappable(Group(Disc(3010, Palette.Card)), "shapes");
        button.AddChild(Pen(Ellipse(P(-60, 40), 48, 48), 3011, fill: Palette.Red, lineWidth: 5));
        button.AddChild(Pen(Polygon(P(55, 95), P(110, 0), P(0, 0)), 3012, fill: Palette.Sun, lineWidth: 5));
        button.AddChild(Pen(Polygon(P(-40, -15), P(50, -15), P(50, -105), P(-40, -105)), 3013, fill: Palette.Teal, lineWidth: 5));
        return button;
    }

    private static Node2D PaintButton()
    {
        var button = Buttons.Tappable(Group(Disc(3020, Palette.Card)), "paint");
        foreach (var (i, (x, y, colour)) in Indexed((-65.0, 45.0, Palette.Red), (5.0, 75.0, Palette.Sun), (70.0, 30.0, Palette.Teal)))
            button.AddChild(Pen(Ellipse(P(x, y), 42, 36), (ulong)(3021 + i), fill: colour, lineWidth: 4.5, wobble: 3));
        // A brush across the bottom with an orange tip: red and yellow made orange.
        button.AddChild(Pen(Polyline(P(-80, -95), P(40, -30)), 3025, ink: Palette.Brown, lineWidth: 14, wobble: 1));
        button.AddChild(Pen(Ellipse(P(60, -20), 30, 22), 3026, fill: Palette.Orange, lineWidth: 4));
        return button;
    }

    private static Node2D MirrorButton()
    {
        var button = Buttons.Tappable(Group(Disc(3030, Palette.Card)), "mirror");
        // Two matching butterfly wings either side of the mirror line.
        foreach (var (i, side) in Indexed(-1.0, 1.0))
        {
            button.AddChild(Pen(Ellipse(P(side * 55, 35), 50, 55), (ulong)(3031 + i), fill: Palette.Purple, lineWidth: 5));
            button.AddChild(Pen(Ellipse(P(side * 45, -50), 35, 38), (ulong)(3033 + i), fill: Palette.Orange, lineWidth: 5));
            button.AddChild(Pen(Ellipse(P(side * 55, 40), 16, 16), (ulong)(3035 + i), fill: Palette.Sun, lineWidth: 3));
        }
        button.AddChild(ArtDrawing.MirrorLine(MirrorMagicGame.Vertical, 250, 3037));
        return button;
    }

    private static string GameFor(string tapName) => tapName switch
    {
        "paint" => PaintPotsGame.GameId,
        "mirror" => MirrorMagicGame.GameId,
        _ => ShapeBuilderGame.GameId,
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
        After(0.25, () => StartGame(ShapeBuilderGame.GameId));
    }

    public override void ReplayPrompt()
    {
        Voice.Play([VoiceLine.ArtIsland]);
        Bip.Hop();
    }
}
