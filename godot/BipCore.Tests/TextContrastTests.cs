using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>Every text colour meets WCAG contrast on the backgrounds it's drawn on (CLAUDE.md, docs/STATUS.md).</summary>
public sealed class TextContrastTests
{
    [Fact]
    public void EveryTextColourIsReadableOnItsBackgrounds() =>
        Assert.Empty(TextContrast.Problems());

    [Fact]
    public void TheRatioMatchesWcag()
    {
        Assert.Equal(21, TextContrast.Ratio(new Rgba(0, 0, 0), new Rgba(1, 1, 1)), 3);
        Assert.Equal(1, TextContrast.Ratio(PaletteColours.Paper, PaletteColours.Paper), 3);
        // See-through text is blended onto its background first: fully clear text has no contrast.
        Assert.Equal(1, TextContrast.Ratio(PaletteColours.Ink.WithAlpha(0), PaletteColours.Paper), 3);
    }

    [Fact]
    public void ALightShadeWouldFail()
    {
        var faint = PaletteColours.Ink.WithAlpha(0.4);
        Assert.True(TextContrast.Ratio(faint, PaletteColours.Paper) < 4.5);
    }
}
