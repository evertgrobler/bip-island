using BipCore;
using Godot;

namespace BipIsland.Drawing;

/// <summary>
/// Warm, bright, hand-drawn colours, the same as the Swift app's Palette:
/// ink #2B2A33, paper #FBF4E4, sunshine yellow #F7C548, sea blue #8FC9E8, leaf green #9BCB6B,
/// coral #E8654F, lilac #B9A2E8 (coding), sand #F3D9A4.
/// </summary>
public static class Palette
{
    // Colours that text is written in or on come from BipCore.PaletteColours, where a unit test checks
    // their contrast (BipCore.TextContrast).
    public static readonly Color Paper = From(PaletteColours.Paper);
    public static readonly Color Ink = From(PaletteColours.Ink);
    public static readonly Color Sea = From(PaletteColours.Sea);
    public static readonly Color Sand = Hex(0xF3D9A4);
    public static readonly Color Grass = Hex(0x9BCB6B);
    public static readonly Color Sun = From(PaletteColours.Sun);
    public static readonly Color Orange = From(PaletteColours.Orange);
    public static readonly Color Red = From(PaletteColours.Red);
    public static readonly Color Pink = new(0.97f, 0.66f, 0.73f);
    public static readonly Color DeepPink = new(0.92f, 0.45f, 0.58f);
    public static readonly Color Teal = From(PaletteColours.Teal);
    public static readonly Color LightTeal = new(0.55f, 0.87f, 0.82f);
    public static readonly Color Purple = From(PaletteColours.Purple);
    public static readonly Color Brown = new(0.60f, 0.40f, 0.24f);
    public static readonly Color LightBrown = new(0.78f, 0.58f, 0.38f);
    public static readonly Color Stone = From(PaletteColours.Stone);
    public static readonly Color Ice = new(0.88f, 0.95f, 1.00f);
    public static readonly Color EggBlue = new(0.72f, 0.88f, 0.95f);
    public static readonly Color Leaf = From(PaletteColours.Leaf);
    public static readonly Color Bubble = new(0.80f, 0.93f, 0.98f);
    public static readonly Color Go = new(0.38f, 0.75f, 0.36f);
    public static readonly Color Card = From(PaletteColours.Card);
    public static readonly Color White = From(PaletteColours.White);

    // Text colours. Every word, number or mark a child or grown-up reads uses one of these (or Ink), so
    // each meets WCAG contrast: 4.5:1 for normal text, 3:1 for big glyphs (what each is for: PaletteColours).
    public static readonly Color SoftInk = From(PaletteColours.SoftInk);
    public static readonly Color QuietInk = From(PaletteColours.QuietInk);
    public static readonly Color AlertOrange = From(PaletteColours.AlertOrange);
    public static readonly Color WrongRed = From(PaletteColours.WrongRed);
    public static readonly Color ProblemRed = From(PaletteColours.ProblemRed);
    public static readonly Color GoodGreen = From(PaletteColours.GoodGreen);
    /// <summary>Bip's snoring "z z z": decoration, exempt from the text rule.</summary>
    public static readonly Color SleepyZ = From(PaletteColours.SleepyZ);

    // Fills that text sits on, checked with the text on them.
    public static readonly Color BalanceBoxFill = From(PaletteColours.BalanceBoxFill);
    public static readonly Color GapFill = From(PaletteColours.GapFill);
    public static readonly Color PlaceholderFill = From(PaletteColours.PlaceholderFill);
    public static readonly Color ProgressTileFill = From(PaletteColours.ProgressTileFill);

    public static Color Hex(uint value) =>
        new(((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f);

    private static Color From(Rgba c) => new((float)c.R, (float)c.G, (float)c.B, (float)c.A);
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
