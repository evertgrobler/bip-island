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
                             fill: got ? Palette.Card : Palette.Card.WithAlpha(0.5f), lineWidth: 5));
        var picture = PictureNode.Make(pictureId, pictureId.StartsWith("pic_") ? pictureId[4..] : pictureId);
        picture.Scale = Vector2.One * 0.4f;
        if (!got) picture.Modulate = new Color(0.55f, 0.55f, 0.55f, 0.35f);
        sticker.AddChild(picture);
        return sticker;
    }
}
