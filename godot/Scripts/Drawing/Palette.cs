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

    public static Color Hex(uint value) =>
        new(((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f);
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
