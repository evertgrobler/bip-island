using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>Bip talks to a seven-year-old differently from a four-year-old (BipCore.VoiceBands).</summary>
public sealed class VoiceBandTests
{
    [Theory]
    [InlineData(null, VoiceBand.Little)]
    [InlineData(3, VoiceBand.Little)]
    [InlineData(6, VoiceBand.Little)]
    [InlineData(7, VoiceBand.Big)]
    [InlineData(10, VoiceBand.Big)]
    public void TheBandFollowsTheChildsAge(int? age, VoiceBand band) => Assert.Equal(band, VoiceBands.For(age));

    [Fact]
    public void ABigChildHearsTheBigLineOnlyWhenItIsRecorded()
    {
        var recorded = new HashSet<string> { "vo_welcome_big" };
        Assert.Equal("vo_welcome_big", VoiceBands.Pick(VoiceLine.Welcome, VoiceBand.Big, recorded.Contains));
        Assert.Equal("vo_round_done", VoiceBands.Pick(VoiceLine.RoundDone, VoiceBand.Big, recorded.Contains));
        Assert.Equal("vo_welcome", VoiceBands.Pick(VoiceLine.Welcome, VoiceBand.Little, recorded.Contains));
        // Lines the child learns from never change with age.
        Assert.Equal("vo_find_the_sound", VoiceBands.Pick(VoiceLine.FindTheSound, VoiceBand.Big, _ => true));
    }

    [Fact]
    public void BigPraiseWaitsUntilThereIsEnoughOfIt()
    {
        var some = VoiceBands.BigPraiseClips.Take(VoiceBands.FewestBigPraise - 1).ToHashSet();
        Assert.Equal(AudioCatalogue.PraiseClips, VoiceBands.PraisePool(VoiceBand.Big, some.Contains));
        var enough = VoiceBands.BigPraiseClips.Take(VoiceBands.FewestBigPraise).ToHashSet();
        Assert.Equal(enough.Order(), VoiceBands.PraisePool(VoiceBand.Big, enough.Contains).Order());
        Assert.Equal(AudioCatalogue.PraiseClips, VoiceBands.PraisePool(VoiceBand.Little, _ => true));
    }

    [Fact]
    public void EveryBigClipFollowsTheNamingContract() =>
        Assert.All(VoiceBands.BigClips, clip => Assert.True(AudioCatalogue.FollowsNamingContract(clip), clip));
}
