using System;
using BipIsland.Drawing;
using Godot;

namespace BipIsland.Screens;

/// <summary>
/// A big, bright mouse pointer that small children can find on the screen: about three times the
/// normal arrow, orange with a thick ink outline, like the Swift app's BigCursor. Drawn once at start-up.
/// </summary>
public static class BigCursor
{
    /// <summary>Width and height of the pointer, in points.</summary>
    public const float Size = 72;

    // The classic arrow, tip at the top left, in a 0…1 box (y runs down).
    private static readonly Vector2[] Shape =
    [
        new(0.10f, 0.06f), new(0.10f, 0.80f), new(0.28f, 0.63f), new(0.42f, 0.92f),
        new(0.56f, 0.86f), new(0.42f, 0.57f), new(0.66f, 0.57f),
    ];

    public static void Install()
    {
        // On a high-resolution screen the pointer image is in pixels, so draw it bigger to keep its size.
        var scale = Math.Clamp(DisplayServer.ScreenGetScale(), 1f, 3f);
        var image = Draw(Mathf.RoundToInt(Size * scale));
        Input.SetCustomMouseCursor(ImageTexture.CreateFromImage(image), Input.CursorShape.Arrow,
            Shape[0] * Size * scale);
    }

    /// <summary>The pointer as an image <paramref name="pixels"/> square: shadow, orange body, ink outline.</summary>
    public static Image Draw(int pixels)
    {
        var image = Image.CreateEmpty(pixels, pixels, false, Image.Format.Rgba8);
        var body = Array.ConvertAll(Shape, p => p * pixels);
        var shadowOffset = new Vector2(3, 3) * pixels / Size;
        var outline = 4.5f * pixels / Size / 2;
        const int samples = 4; // 4×4 samples per pixel for smooth edges
        for (var y = 0; y < pixels; y++)
        for (var x = 0; x < pixels; x++)
        {
            Color colour = new(0, 0, 0, 0);
            for (var sy = 0; sy < samples; sy++)
            for (var sx = 0; sx < samples; sx++)
            {
                var p = new Vector2(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples);
                Color sample;
                if (DistanceToEdge(p, body) <= outline) sample = Palette.Ink;
                else if (Geometry2D.IsPointInPolygon(p, body)) sample = Palette.Orange;
                else if (Geometry2D.IsPointInPolygon(p - shadowOffset, body)) sample = new Color(Palette.Ink, 0.25f);
                else sample = new Color(0, 0, 0, 0);
                colour += sample / (samples * samples);
            }
            image.SetPixel(x, y, colour);
        }
        return image;
    }

    private static float DistanceToEdge(Vector2 p, Vector2[] polygon)
    {
        var best = float.MaxValue;
        for (var i = 0; i < polygon.Length; i++)
        {
            var closest = Geometry2D.GetClosestPointToSegment(p, polygon[i], polygon[(i + 1) % polygon.Length]);
            best = Math.Min(best, p.DistanceTo(closest));
        }
        return best;
    }
}
