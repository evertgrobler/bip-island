using BipCore;
using Xunit;

namespace BipCore.Tests;

public sealed class ParentGateTests
{
    [Fact]
    public void ChallengeIsAnAdultSum()
    {
        var rng = new SeededGenerator(5);
        for (var i = 0; i < 100; i++)
        {
            var c = ParentChallenge.Random(rng);
            Assert.InRange(c.Left, 12, 19);
            Assert.InRange(c.Right, 3, 9);
            Assert.True(c.Answer >= 36);
        }
    }

    [Fact]
    public void ChallengeChecksTheAnswer()
    {
        var c = new ParentChallenge(14, 7);
        Assert.Equal("14 × 7", c.Question);
        Assert.True(c.IsCorrect("98"));
        Assert.True(c.IsCorrect(" 98 \n"));
        Assert.False(c.IsCorrect("97"));
        Assert.False(c.IsCorrect(""));
        Assert.False(c.IsCorrect("nine"));
    }

    [Fact]
    public void EscMustBeHeldForThreeSeconds()
    {
        var hold = new HoldDetector(3);
        Assert.Equal(0, hold.Progress(10));
        hold.Press(10);
        Assert.Equal(0.5, hold.Progress(11.5), 4);
        Assert.False(hold.IsComplete(12.9));
        Assert.True(hold.IsComplete(13));
    }

    [Fact]
    public void KeyRepeatDoesNotRestartTheClock()
    {
        var hold = new HoldDetector(3);
        hold.Press(0);
        hold.Press(2);
        Assert.True(hold.IsComplete(3));
    }

    [Fact]
    public void LettingGoResets()
    {
        var hold = new HoldDetector(3);
        hold.Press(0);
        hold.Release();
        Assert.False(hold.IsHeld);
        Assert.False(hold.IsComplete(5));
        hold.Press(5);
        Assert.False(hold.IsComplete(7));
    }
}

/// <summary>The optional parent passcode.</summary>
public sealed class ParentPasscodeTests
{
    [Fact]
    public void ACodeMatchesOnlyItself()
    {
        var code = ParentPasscode.Create("2468");
        Assert.NotNull(code);
        Assert.True(code.Matches("2468"));
        Assert.True(code.Matches(" 24 68 "));
        Assert.False(code.Matches("2467"));
        Assert.False(code.Matches(""));
        Assert.False(code.Matches("24680"));
    }

    [Fact]
    public void CodesMustBeFourToEightDigits()
    {
        Assert.Null(ParentPasscode.Create("123"));
        Assert.Null(ParentPasscode.Create("123456789"));
        Assert.Null(ParentPasscode.Create("12a4"));
        Assert.Null(ParentPasscode.Create("١٢٣٤")); // only plain 0–9
        Assert.NotNull(ParentPasscode.Create("1234"));
        Assert.NotNull(ParentPasscode.Create("12345678"));
    }

    [Fact]
    public void TheDigitsAreNeverStored()
    {
        var code = ParentPasscode.Create("975310")!;
        var saved = BipJson.Encode(code);
        Assert.DoesNotContain("975310", saved);
        Assert.Equal(code, BipJson.Decode<ParentPasscode>(saved));
    }

    [Fact]
    public void TheSameCodeHashesDifferentlyWithADifferentSalt()
    {
        var a = ParentPasscode.Create("1111", salt: "a")!;
        var b = ParentPasscode.Create("1111", salt: "b")!;
        Assert.NotEqual(a.Hash, b.Hash);
        Assert.True(a.Matches("1111") && b.Matches("1111"));
    }

    /// <summary>A passcode saved by the Swift app (salted SHA-256 hex) still unlocks the gate.</summary>
    [Fact]
    public void ASwiftSavedPasscodeStillWorks()
    {
        // SHA-256("salt" + "1234") in lower-case hex, as the Swift app (CryptoKit) saves it.
        var saved = """{"salt":"salt","hash":"ea32961dbd579ef5697c367f9267921ee07f14d77fb2d4fb9500d4221d615695"}""";
        var code = BipJson.Decode<ParentPasscode>(saved);
        Assert.True(code.Matches("1234"));
        Assert.False(code.Matches("4321"));
    }
}
