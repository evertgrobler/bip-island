using System.Collections.Generic;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// The animal pictures children pick their profile by (ProfileRules avatars): a big animal on a
/// coloured circle, so a child who can't read yet can still find their own. A port of the Swift
/// app's Avatars, with the animal drawn in the bundled Noto Color Emoji font.
/// </summary>
public static class Avatars
{
    public static readonly IReadOnlyDictionary<string, string> Emoji = new Dictionary<string, string>
    {
        ["lion"] = "🦁", ["penguin"] = "🐧", ["tortoise"] = "🐢", ["zebra"] = "🦓",
        ["giraffe"] = "🦒", ["elephant"] = "🐘", ["crab"] = "🦀", ["rhino"] = "🦏",
    };

    public static readonly IReadOnlyDictionary<string, Color> Colours = new Dictionary<string, Color>
    {
        ["lion"] = Palette.Sun, ["penguin"] = Palette.Sea, ["tortoise"] = Palette.Grass, ["zebra"] = Palette.Purple,
        ["giraffe"] = Palette.Orange, ["elephant"] = Palette.LightTeal, ["crab"] = Palette.Red, ["rhino"] = Palette.Pink,
    };

    /// <summary>A round badge with the animal, about <paramref name="radius"/> across from the centre.</summary>
    public static Node2D Badge(string id, float radius, ulong seed)
    {
        var node = new Node2D();
        node.AddChild(Pen(Ellipse(Vector2.Zero, radius, radius), seed,
                          fill: Colours.TryGetValue(id, out var colour) ? colour : Palette.Sun));
        node.AddChild(EmojiPictures.Glyph(Emoji.TryGetValue(id, out var animal) ? animal : "🦁", (int)(radius * 1.15f)));
        return node;
    }
}
