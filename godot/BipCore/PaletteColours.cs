namespace BipCore;

/// <summary>An sRGB colour (0 to 1) with alpha, in plain C# so the contrast rule runs as a unit test.</summary>
public readonly record struct Rgba(double R, double G, double B, double A = 1)
{
    public static Rgba Hex(uint value) =>
        new(((value >> 16) & 0xFF) / 255.0, ((value >> 8) & 0xFF) / 255.0, (value & 0xFF) / 255.0);

    public Rgba WithAlpha(double alpha) => this with { A = alpha };

    /// <summary>The same as Godot's Color.Darkened: each channel times (1 − amount).</summary>
    public Rgba Darkened(double amount) => new(R * (1 - amount), G * (1 - amount), B * (1 - amount), A);

    /// <summary>This colour painted onto an opaque background.</summary>
    public Rgba Over(Rgba background) =>
        new(background.R + (R - background.R) * A, background.G + (G - background.G) * A, background.B + (B - background.B) * A);
}

/// <summary>
/// The palette colours that text is written in or on, and the fills behind text. The game's Palette
/// (godot/Scripts/Drawing/Palette.cs) takes them from here, so the screens and the contrast test below
/// always use the same values.
/// </summary>
public static class PaletteColours
{
    public static readonly Rgba Paper = Rgba.Hex(0xFBF4E4);
    public static readonly Rgba Ink = Rgba.Hex(0x2B2A33);
    public static readonly Rgba Sea = Rgba.Hex(0x8FC9E8);
    public static readonly Rgba Sun = Rgba.Hex(0xF7C548);
    public static readonly Rgba Orange = new(1.00, 0.56, 0.26);
    public static readonly Rgba Red = Rgba.Hex(0xE8654F);
    public static readonly Rgba Teal = new(0.24, 0.75, 0.70);
    public static readonly Rgba Purple = Rgba.Hex(0xB9A2E8);
    public static readonly Rgba Stone = new(0.86, 0.82, 0.76);
    public static readonly Rgba Leaf = new(0.36, 0.68, 0.30);
    public static readonly Rgba Card = new(1.00, 0.99, 0.96);
    public static readonly Rgba White = new(1, 1, 1);

    // Text colours. Every word, number or mark a child or grown-up reads uses one of these (or Ink).
    /// <summary>Softer text on paper (the charging screen's hint for grown-ups, "more stars", coin labels).</summary>
    public static readonly Rgba SoftInk = Ink.WithAlpha(0.85);
    /// <summary>Quiet text and marks (grey parent text, placeholders, the "?" in a gap).</summary>
    public static readonly Rgba QuietInk = Ink.WithAlpha(0.75);
    /// <summary>Orange text and marks ("Try it now", the "?" in Bip's Balance box).</summary>
    public static readonly Rgba AlertOrange = Orange.Darkened(0.45);
    /// <summary>A wrong answer shown in red (Bip's Balance).</summary>
    public static readonly Rgba WrongRed = Red.Darkened(0.35);
    /// <summary>The parent area's problems ("That child couldn't be added"), and the Remove buttons.</summary>
    public static readonly Rgba ProblemRed = Rgba.Hex(0xC0392B);
    /// <summary>Good news for grown-ups ("Well done" in the setup guide).</summary>
    public static readonly Rgba GoodGreen = Leaf.Darkened(0.35);
    /// <summary>
    /// Bip's snoring "z z z": decoration, never words to read, so it's exempt from the text rule
    /// (WCAG excludes decoration) and kept soft on purpose.
    /// </summary>
    public static readonly Rgba SleepyZ = Ink.WithAlpha(0.6);

    // Fills that text sits on (see-through, so each check names what's under them too).
    /// <summary>Bip's Balance answer box, on a card.</summary>
    public static readonly Rgba BalanceBoxFill = Sun.WithAlpha(0.35);
    /// <summary>The gap in a Block Towers or block pattern, on paper.</summary>
    public static readonly Rgba GapFill = Card.WithAlpha(0.6);
    /// <summary>A "picture coming" placeholder, on paper.</summary>
    public static readonly Rgba PlaceholderFill = Stone.WithAlpha(0.5);
    /// <summary>A parent progress tile, on a card.</summary>
    public static readonly Rgba ProgressTileFill = Sun.WithAlpha(0.35);
}

/// <summary>The WCAG contrast rule for text: 4.5:1 for normal text, 3:1 for big glyphs.</summary>
public static class TextContrast
{
    public sealed record Check(string Name, Rgba Text, Rgba Background, double Minimum);

    /// <summary>The contrast ratio of text (blended onto its background) against that background.</summary>
    public static double Ratio(Rgba text, Rgba background)
    {
        var solid = background with { A = 1 };
        var (a, b) = (Luminance(text.Over(solid)), Luminance(solid));
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double Luminance(Rgba c)
    {
        static double Lin(double v) => v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        return 0.2126 * Lin(c.R) + 0.7152 * Lin(c.G) + 0.0722 * Lin(c.B);
    }

    /// <summary>Every text colour on every background it's drawn on.</summary>
    public static IReadOnlyList<Check> Checks { get; } = Build();

    private static List<Check> Build()
    {
        var balanceBox = PaletteColours.BalanceBoxFill.Over(PaletteColours.Card);
        var gap = PaletteColours.GapFill.Over(PaletteColours.Paper);
        var placeholder = PaletteColours.PlaceholderFill.Over(PaletteColours.Paper);
        var tile = PaletteColours.ProgressTileFill.Over(PaletteColours.Card);
        return
        [
            new("Ink on Paper", PaletteColours.Ink, PaletteColours.Paper, 4.5),
            new("SoftInk on Paper", PaletteColours.SoftInk, PaletteColours.Paper, 4.5),
            new("SoftInk on Card (coin and note labels)", PaletteColours.SoftInk, PaletteColours.Card, 4.5),
            new("QuietInk on Paper", PaletteColours.QuietInk, PaletteColours.Paper, 4.5),
            new("QuietInk on White", PaletteColours.QuietInk, PaletteColours.White, 4.5),
            new("QuietInk on a gap (the big \"?\")", PaletteColours.QuietInk, gap, 3),
            new("QuietInk on a placeholder (\"picture coming\")", PaletteColours.QuietInk, placeholder, 4.5),
            new("QuietInk on a parent progress tile", PaletteColours.QuietInk, tile, 4.5),
            new("ProblemRed on Paper", PaletteColours.ProblemRed, PaletteColours.Paper, 4.5),
            new("ProblemRed on White", PaletteColours.ProblemRed, PaletteColours.White, 4.5),
            new("GoodGreen on Paper", PaletteColours.GoodGreen, PaletteColours.Paper, 4.5),
            new("AlertOrange on Paper", PaletteColours.AlertOrange, PaletteColours.Paper, 4.5),
            new("AlertOrange on Bip's Balance box (the big \"?\")", PaletteColours.AlertOrange, balanceBox, 3),
            new("WrongRed on Bip's Balance box (a big number)", PaletteColours.WrongRed, balanceBox, 3),
            new("Ink on Teal", PaletteColours.Ink, PaletteColours.Teal, 4.5),
            new("Ink on Sea", PaletteColours.Ink, PaletteColours.Sea, 4.5),
            new("Ink on Purple", PaletteColours.Ink, PaletteColours.Purple, 4.5),
            new("Ink on Sun", PaletteColours.Ink, PaletteColours.Sun, 4.5),
        ];
    }

    /// <summary>The checks that fall below their minimum (empty when all pass).</summary>
    public static IReadOnlyList<string> Problems() =>
        Checks.Select(c => (c, Ratio: Ratio(c.Text, c.Background)))
              .Where(x => x.Ratio < x.c.Minimum)
              .Select(x => $"{x.c.Name}: {x.Ratio:0.0}:1, needs {x.c.Minimum}:1")
              .ToList();
}
