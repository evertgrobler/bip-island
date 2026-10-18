using System;
using Godot;

namespace BipIsland.Drawing;

/// <summary>
/// Warm, bright, hand-drawn colours, the same as the Swift app's Palette:
/// ink #2B2A33, paper #FBF4E4, sunshine yellow #F7C548, sea blue #8FC9E8, leaf green #9BCB6B,
/// coral #E8654F, lilac #B9A2E8 (coding), sand #F3D9A4.
/// </summary>
public static class Palette
{
    public static readonly Color Paper = Hex(0xFBF4E4);
    public static readonly Color Ink = Hex(0x2B2A33);
    public static readonly Color Sea = Hex(0x8FC9E8);
    public static readonly Color Sand = Hex(0xF3D9A4);
    public static readonly Color Grass = Hex(0x9BCB6B);
    public static readonly Color Sun = Hex(0xF7C548);
    public static readonly Color Orange = new(1.00f, 0.56f, 0.26f);
    public static readonly Color Red = Hex(0xE8654F);
    public static readonly Color Pink = new(0.97f, 0.66f, 0.73f);
    public static readonly Color DeepPink = new(0.92f, 0.45f, 0.58f);
    public static readonly Color Teal = new(0.24f, 0.75f, 0.70f);
    public static readonly Color LightTeal = new(0.55f, 0.87f, 0.82f);
    public static readonly Color Purple = Hex(0xB9A2E8);
    public static readonly Color Brown = new(0.60f, 0.40f, 0.24f);
    public static readonly Color LightBrown = new(0.78f, 0.58f, 0.38f);
    public static readonly Color Stone = new(0.86f, 0.82f, 0.76f);
    public static readonly Color Ice = new(0.88f, 0.95f, 1.00f);
    public static readonly Color EggBlue = new(0.72f, 0.88f, 0.95f);
    public static readonly Color Leaf = new(0.36f, 0.68f, 0.30f);
    public static readonly Color Bubble = new(0.80f, 0.93f, 0.98f);
    public static readonly Color Go = new(0.38f, 0.75f, 0.36f);
    public static readonly Color Card = new(1.00f, 0.99f, 0.96f);
    public static readonly Color White = new(1, 1, 1);

    // Text colours. Every word, number or mark a child or grown-up reads uses one of these (or Ink), so
    // each meets WCAG contrast: 4.5:1 for normal text, 3:1 for big glyphs. ContrastProblems() checks
    // them in the game's self-test, so a lighter shade can't creep back in.
    /// <summary>Softer text on paper (the charging screen's hint for grown-ups, "more stars"): 8:1.</summary>
    public static readonly Color SoftInk = new(Ink, 0.85f);
    /// <summary>Quiet text and marks (grey parent text, placeholders, the "?" in a gap): 6:1.</summary>
    public static readonly Color QuietInk = new(Ink, 0.75f);
    /// <summary>Orange text and marks ("Try it now", the "?" in Bip's Balance box): 5.4:1 or more.</summary>
    public static readonly Color AlertOrange = Orange.Darkened(0.45f);
    /// <summary>A wrong answer shown in red (Bip's Balance): 5.6:1 on its box.</summary>
    public static readonly Color WrongRed = Red.Darkened(0.35f);

    public static Color Hex(uint value) =>
        new(((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f);

    /// <summary>The WCAG contrast ratio of a colour (blended onto its background) against that background.</summary>
    public static double Contrast(Color text, Color background)
    {
        var over = background.Lerp(new Color(text, 1), text.A);
        double Luminance(Color c)
        {
            double Lin(float v) => v <= 0.03928 ? v / 12.92 : Mathf.Pow((v + 0.055) / 1.055, 2.4);
            return 0.2126 * Lin(c.R) + 0.7152 * Lin(c.G) + 0.0722 * Lin(c.B);
        }
        var (a, b) = (Luminance(over), Luminance(new Color(background, 1)));
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>Text colours that fall below their contrast minimum on the backgrounds they're used on (empty when all pass).</summary>
    public static System.Collections.Generic.List<string> ContrastProblems()
    {
        var balanceBox = Card.Lerp(Sun, 0.35f);
        var checks = new (string Name, Color Text, Color Background, double Minimum)[]
        {
            ("Ink on Paper", Ink, Paper, 4.5), ("SoftInk on Paper", SoftInk, Paper, 4.5),
            ("QuietInk on Paper", QuietInk, Paper, 4.5), ("QuietInk on White", QuietInk, White, 4.5),
            ("QuietInk on Card at 60%", QuietInk, Paper.Lerp(Card, 0.6f), 3),
            ("AlertOrange on Paper", AlertOrange, Paper, 4.5), ("AlertOrange on Bip's Balance box", AlertOrange, balanceBox, 3),
            ("WrongRed on Bip's Balance box", WrongRed, balanceBox, 3),
            ("Ink on Teal", Ink, Teal, 4.5), ("Ink on Sea", Ink, Sea, 4.5), ("Ink on Purple", Ink, Purple, 4.5), ("Ink on Sun", Ink, Sun, 4.5),
        };
        var problems = new System.Collections.Generic.List<string>();
        foreach (var (name, text, background, minimum) in checks)
        {
            var ratio = Contrast(text, background);
            if (ratio < minimum) problems.Add($"{name}: {ratio:0.0}:1, needs {minimum}:1");
        }
        return problems;
    }
}

/// <summary>
/// Atkinson Hyperlegible is the one font for every word in the game (owner decision, CLAUDE.md).
/// Noto Color Emoji is its fallback, so the stand-in emoji pictures look the same on Mac and Windows.
/// Both are bundled; nothing is fetched over the network.
/// </summary>
public static class Fonts
{
    public const string RegularPath = "res://assets/fonts/AtkinsonHyperlegible-Regular.ttf";
    public const string BoldPath = "res://assets/fonts/AtkinsonHyperlegible-Bold.ttf";
    public const string EmojiPath = "res://fonts/NotoColorEmoji.ttf";

    public static Font Regular { get; private set; } = ThemeDB.FallbackFont;
    public static Font Bold { get; private set; } = ThemeDB.FallbackFont;
    /// <summary>Picture emoji (stand-in pictures, avatars): Noto Color Emoji, the same on Mac and Windows.</summary>
    public static Font Emoji { get; private set; } = ThemeDB.FallbackFont;
    /// <summary>Letters and words the child reads.</summary>
    public static Font Letters => Bold;

    /// <summary>True when Atkinson Hyperlegible and the emoji font both loaded.</summary>
    public static bool IsInstalled { get; private set; }
    public static bool EmojiInstalled { get; private set; }

    /// <summary>Loads the bundled fonts and makes Atkinson the default for every control.</summary>
    public static void Load()
    {
        var emoji = ResourceLoader.Exists(EmojiPath) ? GD.Load<FontFile>(EmojiPath) : null;
        EmojiInstalled = emoji != null;
        if (emoji != null) Emoji = emoji;
        var regular = LoadWithFallback(RegularPath, emoji);
        var bold = LoadWithFallback(BoldPath, emoji);
        IsInstalled = regular != null && bold != null;
        if (regular != null) Regular = regular;
        if (bold != null) Bold = bold;
        ThemeDB.FallbackFont = Regular;
        if (!IsInstalled) GD.PushWarning("Bip Island: Atkinson Hyperlegible is missing; using Godot's default font.");
    }

    private static FontFile? LoadWithFallback(string path, FontFile? emoji)
    {
        if (!ResourceLoader.Exists(path)) return null;
        var font = GD.Load<FontFile>(path);
        if (emoji != null) font.Fallbacks = new Godot.Collections.Array<Font> { emoji };
        return font;
    }
}
