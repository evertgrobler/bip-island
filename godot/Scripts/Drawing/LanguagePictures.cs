using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Game;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// The Language Island's own pictures (Content/language/words.json): a splash of paint for each
/// colour (pic_colour_red) and a set of counting dots for each number (pic_count_3), in the 220 × 220
/// box every picture fits.
/// </summary>
public static class LanguagePictures
{
    /// <summary>
    /// The paint colours Paint Pots uses (Content/art/paints.json), read from the content so both games
    /// always show the same shade for a colour's name.
    /// </summary>
    private static IReadOnlyList<PaintColour> Colours => GameCoordinator.Instance?.Content?.Paints.Colours ?? [];

    public const int MostDots = 10;

    public static IEnumerable<string> Ids =>
        Colours.Select(c => $"pic_colour_{c.Id}").Concat(Enumerable.Range(1, MostDots).Select(n => $"pic_count_{n}"));

    public static Node2D? Make(string id)
    {
        if (id.StartsWith("pic_colour_", StringComparison.Ordinal))
        {
            var colours = Colours.ToList();
            var index = colours.FindIndex(c => c.Id == id["pic_colour_".Length..]);
            return index < 0 ? null : Splash(Color.FromHtml(colours[index].Hex), (ulong)(5000 + index * 10));
        }
        if (id.StartsWith("pic_count_", StringComparison.Ordinal) && int.TryParse(id["pic_count_".Length..], out var n) && n is >= 1 and <= MostDots)
            return Dots(n);
        return null;
    }

    /// <summary>A round splash of paint with a few drops beside it.</summary>
    private static Node2D Splash(Color colour, ulong seed)
    {
        var n = new Node2D();
        // White paint on a white card would be only an outline: it sits on a soft grey swatch instead,
        // like a dab of white on a grey palette, so it still reads as white paint.
        if (colour.Luminance > 0.9f)
            n.AddChild(Pen(RoundRect(R(-108, -104, 216, 216), 26), seed + 9, fill: Palette.Stone, lineWidth: 3, wobble: 1.5));
        var edge = Enumerable.Range(0, 28).Select(i =>
        {
            var a = i / 28.0 * 2 * Math.PI;
            var r = 82 + 12 * Math.Sin(a * 5 + 0.7) + 6 * Math.Sin(a * 3);
            return P(r * Math.Cos(a), r * Math.Sin(a) + 6);
        });
        n.AddChild(Pen(Polygon(edge), seed, fill: colour, lineWidth: 5));
        foreach (var (i, (x, y, r)) in Indexed((96.0, -70.0, 14.0), (-98.0, 74.0, 10.0), (70.0, 92.0, 8.0)))
            n.AddChild(Pen(Ellipse(P(x, y), r, r), seed + 1 + (ulong)i, fill: colour, lineWidth: 3.5, wobble: 1.5));
        return n;
    }

    /// <summary>Counting dots: up to five in a row, the rest in a row below, like a ten frame.</summary>
    private static Node2D Dots(int count)
    {
        var n = new Node2D();
        var rows = count > 5 ? new[] { 5, count - 5 } : new[] { count };
        for (var row = 0; row < rows.Length; row++)
        {
            var y = rows.Length == 1 ? 0 : row == 0 ? 42 : -42;
            for (var i = 0; i < rows[row]; i++)
            {
                var x = (i - (rows[row] - 1) / 2.0) * 44;
                n.AddChild(Pen(Ellipse(P(x, y), 18, 18), (ulong)(5200 + count * 10 + row * 5 + i), fill: Palette.Red, lineWidth: 3.5, wobble: 1.5));
            }
        }
        return n;
    }
}
