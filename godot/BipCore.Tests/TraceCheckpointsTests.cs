using System.Numerics;
using Xunit;

namespace BipCore.Tests;

/// <summary>Letter Trace follows the letter's own strokes (owner, 9 Nov 2026: i, j and a couldn't be finished).</summary>
public class TraceCheckpointsTests
{
    private const float Size = 380;
    private const float Radius = 60;

    private static TraceCheckpoints For(string grapheme) =>
        new(TestContent.Library().TraceShapes.Shapes[grapheme], Vector2.Zero, Size, Radius,
            TestContent.Library().TraceShapes.Dots.GetValueOrDefault(grapheme));

    [Theory]
    [InlineData(HomeLanguage.English)]
    [InlineData(HomeLanguage.Afrikaans)]
    public void EveryGraphemeHasItsShape(HomeLanguage language)
    {
        var content = TestContent.Library(language);
        foreach (var g in content.Phonics.Graphemes)
            Assert.True(content.TraceShapes.Shapes.TryGetValue(g.Text, out var shape) && shape.Count >= 3,
                        $"no trace shape for {g.Text}");
    }

    [Fact]
    public void TheIsDotIsItsOwnStrokeAndIsNeeded()
    {
        var i = For("i");
        // The dot is the highest checkpoint, well above the rest.
        var dot = i.Points.MinBy(p => p.Y);
        var stem = i.Points.Where(p => p != dot).ToList();
        // One drag down the stem (a first stroke), then the pen lifts.
        i.Stroke(stem.MinBy(p => p.Y), stem.MaxBy(p => p.Y));
        foreach (var p in stem) i.Touch(p);
        Assert.False(i.Done);
        // A second, separate drag on the dot finishes it.
        i.Touch(dot);
        Assert.True(i.Done);
    }

    [Fact]
    public void JFinishesWithTheDotAsASecondStroke()
    {
        var j = For("j");
        var dot = j.Points.MinBy(p => p.Y);
        foreach (var p in j.Points.Where(p => p != dot)) j.Touch(p);
        Assert.False(j.Done);
        j.Touch(dot);
        Assert.True(j.Done);
    }

    [Fact]
    public void AFinishesInAnyOrderAndGoingBackOverAStroke()
    {
        var a = For("a");
        // Backwards, then over the same points again: order and repeats don't matter.
        foreach (var p in a.Points.Reverse()) a.Touch(p);
        foreach (var p in a.Points) a.Touch(p);
        Assert.True(a.Done);
    }

    [Fact]
    public void TheOldRingAroundTheLetterDoesNotTraceIt()
    {
        // Circling far round the letter (what the old checkpoints asked for) misses the strokes.
        var i = For("i");
        for (var k = 0; k < 64; k++)
        {
            var angle = k / 64f * MathF.Tau;
            i.Touch(new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 0.6f * Size);
        }
        Assert.False(i.Done);
    }

    [Fact]
    public void AFastMoveTouchesEverythingAlongTheWay()
    {
        var shape = new List<List<float>> { new() { 0, -0.5f }, new() { 0, 0 }, new() { 0, 0.5f } };
        var line = new TraceCheckpoints(shape, Vector2.Zero, Size, Radius);
        // One jump from top to bottom, as a quick mouse reports it.
        Assert.Equal(3, line.Stroke(new Vector2(0, -0.5f * Size), new Vector2(0, 0.5f * Size)));
        Assert.True(line.Done);
    }

    [Fact]
    public void MostCheckpointsAreEnoughButAFewMustAllBeTouched()
    {
        Assert.Equal(3, TraceCheckpoints.Needed(3));
        // From four up at least one may be missed (owner's i, j: a serif's tip mustn't block the letter).
        for (var n = 4; n <= 9; n++) Assert.Equal(n - 1, TraceCheckpoints.Needed(n));
        Assert.Equal(9, TraceCheckpoints.Needed(10));
        Assert.Equal(27, TraceCheckpoints.Needed(30));
    }

    [Theory]
    [InlineData("i")]
    [InlineData("j")]
    [InlineData("t")]
    [InlineData("l")]
    public void AShortLetterFinishesWithOneTipMissed(string grapheme)
    {
        var letter = For(grapheme);
        // Everything but the checkpoint furthest from the letter's middle that isn't a dot (a serif or a tail).
        var dot = grapheme is "i" or "j" ? letter.Points.MinBy(p => p.Y) : (Vector2?)null;
        var tip = letter.Points.Where(p => p != dot).MaxBy(p => p.Length());
        foreach (var p in letter.Points.Where(p => p != tip)) letter.Touch(p);
        Assert.True(letter.Done, $"{grapheme}: missing only {tip} should be enough");
    }
}
