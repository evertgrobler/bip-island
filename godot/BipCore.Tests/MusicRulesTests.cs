using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>Background music: which loop plays where, how loud, and the grown-up's setting.</summary>
public sealed class MusicRulesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "bip-music-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void EachIslandHasItsOwnLoop()
    {
        Assert.Equal("music_letters", MusicRules.IslandTrack(Island.Letters));
        Assert.Equal("music_art", MusicRules.IslandTrack(Island.Art));
        Assert.Equal(6, MusicRules.AllTracks.Count);
        Assert.Equal(MusicRules.AllTracks.Count, MusicRules.AllTracks.Distinct().Count());
        Assert.Contains(MusicRules.MapTrack, MusicRules.AllTracks);
    }

    [Fact]
    public void TheMusicDipsWhileTheNarratorSpeaks()
    {
        foreach (var level in new[] { MusicLevel.Quiet, MusicLevel.Normal })
        {
            var full = MusicRules.Volume(level, narratorSpeaking: false);
            var ducked = MusicRules.Volume(level, narratorSpeaking: true);
            Assert.InRange(full, 0.01f, 0.5f);
            Assert.True(ducked < full / 2, $"{level}: the music should drop well under the voice");
        }
        Assert.True(MusicRules.Volume(MusicLevel.Quiet, false) < MusicRules.Volume(MusicLevel.Normal, false));
    }

    [Fact]
    public void OffIsSilent()
    {
        Assert.Equal(0f, MusicRules.Volume(MusicLevel.Off, false));
        Assert.Equal(0f, MusicRules.Volume(MusicLevel.Off, true));
    }

    [Fact]
    public void TheButtonCyclesThroughEverySetting()
    {
        var seen = new List<MusicLevel>();
        var level = MusicLevel.Normal;
        for (var i = 0; i < 3; i++)
        {
            seen.Add(level);
            level = MusicRules.Next(level);
        }
        Assert.Equal(MusicLevel.Normal, level);
        Assert.Equal(Enum.GetValues<MusicLevel>().Order(), seen.Order());
    }

    [Fact]
    public void TheSettingIsKeptOnThisComputer()
    {
        var store = new SaveStore(_folder);
        Assert.Equal(MusicLevel.Normal, store.Music);
        store.Music = MusicLevel.Quiet;
        Assert.Equal(MusicLevel.Quiet, new SaveStore(_folder).Music);
    }

    [Fact]
    public void AnOlderSaveHasMusicOn()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, SaveStore.FileName), """{"version":1,"children":[]}""");
        Assert.Equal(MusicLevel.Normal, new SaveStore(_folder).Music);
    }
}
