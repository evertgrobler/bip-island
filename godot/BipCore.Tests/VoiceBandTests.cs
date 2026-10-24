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
    public void ResolveFallsBackToTheStandInThenPicksTheBand()
    {
        var recorded = new HashSet<string> { "num_7", "word_and", "vo_welcome_big", "vo_welcome", "hint_01_big" };
        // A lead-on clip not recorded yet plays its plain one.
        Assert.Equal("num_7", VoiceBands.Resolve("num_7_mid", VoiceBand.Big, recorded.Contains));
        Assert.Equal("word_and", VoiceBands.Resolve(VoiceLine.And, VoiceBand.Little, recorded.Contains));
        // Bip's own lines in the band's version when it's there.
        Assert.Equal("vo_welcome_big", VoiceBands.Resolve(VoiceLine.Welcome, VoiceBand.Big, recorded.Contains));
        Assert.Equal("vo_welcome", VoiceBands.Resolve(VoiceLine.Welcome, VoiceBand.Little, recorded.Contains));
        Assert.Equal("hint_01_big", VoiceBands.Resolve("hint_01", VoiceBand.Big, recorded.Contains));
        // A clip with no recording at all stays itself (it plays as a short silence).
        Assert.Equal("vo_round_done", VoiceBands.Resolve(VoiceLine.RoundDone, VoiceBand.Big, recorded.Contains));
        // "big" is a word children learn: word_big is never taken for an older line.
        Assert.Equal("word_big", VoiceBands.Resolve("word_big", VoiceBand.Big, _ => true));
        Assert.Equal("word_cat", VoiceBands.Resolve("word_cat", VoiceBand.Big, _ => true));
    }

    [Fact]
    public void OnlyBipsOwnLinesHaveABigVersion() =>
        Assert.All(VoiceBands.BigClips, clip => Assert.Matches("^(vo|hint|praise)_", clip));

    /// <summary>Each language's voice script lists exactly the Big lines the game plays, each next to its young row.</summary>
    [Theory]
    [InlineData("audio/script.csv")]
    [InlineData("audio/af/script.csv")]
    public void EachScriptHasExactlyTheBigLinesTheGamePlays(string script)
    {
        var clips = AudioScriptTests.ScriptClips(script).ToHashSet();
        var big = clips.Where(c => c.EndsWith(VoiceBands.Suffix, StringComparison.Ordinal)).Order().ToList();
        Assert.Equal(VoiceBands.BigClips.Order(), big);
        var missingBase = VoiceBands.BigVersions.Where(c => !clips.Contains(c)).ToList();
        Assert.True(missingBase.Count == 0, $"{script} has no young row for: " + string.Join(", ", missingBase));
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
