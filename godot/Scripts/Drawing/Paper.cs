using Godot;

namespace BipIsland.Drawing;

/// <summary>Cream paper with speckles and fibres, made once and reused (as in the Swift app).</summary>
public static class Paper
{
    private static ImageTexture? _texture;

    public static ImageTexture Texture => _texture ??= Make();

    private static ImageTexture Make()
    {
        const int width = 1024, height = 640;
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        image.Fill(Palette.Paper);
        var rng = new SeededRandom(2026);

        for (var i = 0; i < 9000; i++)
        {
            var shade = (float)rng.Range(0.25, 0.6);
            var colour = new Color(shade, shade * 0.8f, shade * 0.55f, (float)rng.Range(0.03, 0.09));
            var size = (float)rng.Range(0.6, 2.2);
            var x = (float)rng.Range(0, width);
            var y = (float)rng.Range(0, height);
            Dot(image, x, y, size / 2, colour);
        }
        for (var i = 0; i < 420; i++)
        {
            var colour = new Color(0.55f, 0.42f, 0.28f, (float)rng.Range(0.03, 0.07));
            rng.Range(0.5, 1.2); // line width in the Swift version; fibres here are one pixel
            var start = new Vector2((float)rng.Range(0, width), (float)rng.Range(0, height));
            var end = start + new Vector2((float)rng.Range(-14, 14), (float)rng.Range(-6, 6));
            var steps = (int)start.DistanceTo(end) + 1;
            for (var s = 0; s <= steps; s++)
            {
                var p = start.Lerp(end, (float)s / steps);
                Blend(image, (int)p.X, (int)p.Y, colour);
            }
        }
        return ImageTexture.CreateFromImage(image);
    }

    private static void Dot(Image image, float cx, float cy, float r, Color colour)
    {
        var reach = Mathf.CeilToInt(r);
        for (var y = -reach; y <= reach; y++)
        for (var x = -reach; x <= reach; x++)
            if (x * x + y * y <= r * r + 0.5f)
                Blend(image, (int)cx + x, (int)cy + y, colour);
    }

    private static void Blend(Image image, int x, int y, Color colour)
    {
        if (x < 0 || y < 0 || x >= image.GetWidth() || y >= image.GetHeight()) return;
        image.SetPixel(x, y, image.GetPixel(x, y).Blend(colour));
    }
}
