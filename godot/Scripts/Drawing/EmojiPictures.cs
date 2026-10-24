using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace BipIsland.Drawing;

/// <summary>
/// Emoji stand-ins for pictures without hand-drawn art (a port of the Swift app's EmojiPictures),
/// drawn with the bundled Noto Color Emoji font (via <see cref="Fonts"/>) so they look the same on Mac
/// and Windows. Since 18 October 2026 every picture is hand-drawn (<see cref="ArtPictures"/>), so the
/// table is empty; a new word can borrow an emoji here until its drawing lands, then lose its row.
/// </summary>
public static class EmojiPictures
{
    /// <summary>Picture id (pic_duck) to emoji.</summary>
    public static readonly IReadOnlyDictionary<string, string> Icons = new Dictionary<string, string>
    {
    };

    public static Node2D? Make(string id)
    {
        if (!Icons.TryGetValue(id, out var icons)) return null;
        // Count what a person sees (a flag or a family is one picture), like Swift's String.count.
        var count = new StringInfo(icons).LengthInTextElements;
        var size = count <= 2 ? 150 : Math.Max(30, 320 / count);
        var holder = new Node2D();
        holder.AddChild(Glyph(icons, size));
        return holder;
    }

    /// <summary>Emoji text centred on (0, 0) at a font size, the same as the Swift app's emoji labels.</summary>
    public static Label Glyph(string text, int size) => Sketch.Label(text, size, Palette.White, Fonts.Emoji);
}
