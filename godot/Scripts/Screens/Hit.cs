using BipIsland.Drawing;
using Godot;

namespace BipIsland.Screens;

/// <summary>
/// Finds what a click landed on. Godot's 2D nodes don't know their size, so a tappable node's area is
/// the box around everything drawn inside it (its sketches, discs, ovals and labels), unless it sets
/// an explicit area with <see cref="SetArea"/>.
/// </summary>
public static class Hit
{
    private const string AreaMeta = "hit_area";

    /// <summary>Overrides the tappable area, in the node's own coordinates.</summary>
    public static void SetArea(Node2D node, Rect2 area) => node.SetMeta(AreaMeta, area);

    /// <summary>The node's tappable area in global (canvas) coordinates, or null if nothing is drawn.</summary>
    public static Rect2? GlobalArea(Node2D node)
    {
        if (node.HasMeta(AreaMeta)) return Transform(node.GetGlobalTransform(), node.GetMeta(AreaMeta).AsRect2());
        Rect2? box = null;
        Collect(node, ref box);
        return box;
    }

    /// <summary>True when the node and every parent are visible and not faded out.</summary>
    public static bool IsShown(CanvasItem item)
    {
        for (Node? n = item; n != null; n = n.GetParent())
        {
            if (n is CanvasItem c && (!c.Visible || c.Modulate.A < 0.05f)) return false;
        }
        return true;
    }

    private static void Collect(Node node, ref Rect2? box)
    {
        Rect2? part = node switch
        {
            IHasBounds drawn when node is Node2D n2 => Transform(n2.GetGlobalTransform(), drawn.LocalBounds),
            Control control => control.GetGlobalRect(),
            _ => null,
        };
        if (part is { } p && p.HasArea()) box = box is { } b ? b.Merge(p) : p;
        foreach (var child in node.GetChildren()) Collect(child, ref box);
    }

    private static Rect2 Transform(Transform2D transform, Rect2 rect) => transform * rect;
}
