using System;
using System.Collections.Generic;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// A picture sticker: the word's hand-drawn picture on a round white sticker with a wobbly ink edge
/// (the end-of-game card and the sticker book). Drawn round (0, 0), about 130 across.
/// </summary>
public static class StickerArt
{
    public const float Radius = 62;

    /// <param name="pictureId">A picture id from words.json ("pic_sun").</param>
    /// <param name="got">False draws the grey outline of a sticker still to find.</param>
    public static Node2D Make(string pictureId, ulong seed, bool got = true)
    {
        var sticker = new Node2D();
        sticker.AddChild(Pen(Ellipse(Vector2.Zero, Radius, Radius), seed,
                             fill: got ? Palette.Card : Palette.Card.WithAlpha(0.4f),
                             ink: got ? null : Palette.Ink.WithAlpha(0.3f), lineWidth: got ? 5 : 3));
        var picture = PictureNode.Make(pictureId, pictureId.StartsWith("pic_") ? pictureId[4..] : pictureId);
        picture.Scale = Vector2.One * 0.4f;
        // Still to find: a faint grey shadow of the picture.
        if (!got) picture.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.18f);
        sticker.AddChild(picture);
        return sticker;
    }

    /// <summary>Where <paramref name="count"/> stickers sit, and how big, to fit a box round a centre.</summary>
    public sealed record Layout(List<Vector2> Centres, float Scale);

    /// <summary>
    /// Lays stickers out in rows inside a <paramref name="width"/> × <paramref name="height"/> box
    /// round <paramref name="centre"/>, as big as they fit (never bigger than full size).
    /// </summary>
    public static Layout Grid(int count, Vector2 centre, float width, float height)
    {
        const float cell = 150;
        var (cols, scale) = (1, 0f);
        for (var c = 1; c <= Math.Max(count, 1); c++)
        {
            var rows = (count + c - 1) / c;
            var fit = Math.Min(1f, Math.Min(width / (c * cell), height / (rows * cell)));
            if (fit > scale) (cols, scale) = (c, fit);
        }
        var rowCount = (count + cols - 1) / Math.Max(cols, 1);
        var centres = new List<Vector2>();
        for (var i = 0; i < count; i++)
        {
            var (row, col) = (i / cols, i % cols);
            var inRow = Math.Min(cols, count - row * cols);
            centres.Add(centre + new Vector2((col - (inRow - 1) / 2f) * cell * scale, (row - (rowCount - 1) / 2f) * cell * scale));
        }
        return new Layout(centres, scale);
    }
}
