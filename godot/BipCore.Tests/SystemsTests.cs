using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>Bip's recommendations, play-time breaks and the sticker book (docs/GAMES.md).</summary>
public sealed class SystemsTests
{
    private static ContentLibrary Library() => TestContent.Library();
    private static MasteryRules Rules => TestContent.Rules;

    private static void Master(ChildProgress progress, string skill, int[] days)
    {
        foreach (var day in days)
        {
            for (var i = 0; i < 5; i++) progress.RecordAnswer(true, skill, null, day, Rules);
        }
    }

    // MARK: Recommendations

    [Fact]
    public void ReviewComesFirst()
    {
        var content = Library();
        var progress = new ChildProgress();
        Master(progress, "snd_g1", [0, 1]);
        Assert.True(progress.IsMastered("snd_g1"));
        var rng = new SeededGenerator(40);
        // Mastered on day 1, so the first review (2 days later) is due on day 3. (The Swift test used
        // day 2, when nothing is due yet, and only passed because its random pick happened to fit.)
        Assert.True(progress.IsDueForReview("snd_g1", 3, Rules));
        var suggestion = new PlayRecommender(content).Suggest(progress, Band.Foundation, 3, Rules, rng);
        Assert.NotNull(suggestion);
        var game = content.Game(suggestion.GameId)!;
        Assert.True(game.Skills.Contains("snd_g1"), $"review should practise the due skill, not {suggestion.GameId}");
        Assert.False(suggestion.IsNudge);
    }

    [Fact]
    public void SuggestsLeastPlayedIsland()
    {
        var progress = new ChildProgress();
        progress.NotePlayed("meet_the_sound");
        progress.NotePlayed("sound_hunt");
        var suggestion = new PlayRecommender(Library()).Suggest(progress, Band.Foundation, 0, Rules, new SeededGenerator(41));
        Assert.NotNull(suggestion);
        Assert.Equal(Island.Numbers, suggestion.Island); // letters was just played; numbers was never played
        Assert.False(suggestion.IsNudge, "only two visits: no nudge yet");
    }

    [Fact]
    public void NudgesAwayAfterThreeSameIslandVisits()
    {
        var progress = new ChildProgress();
        progress.NotePlayed("meet_the_sound");
        progress.NotePlayed("sound_hunt");
        progress.NotePlayed("bubble_pop");
        var suggestion = new PlayRecommender(Library()).Suggest(progress, Band.Foundation, 0, Rules, new SeededGenerator(42));
        Assert.NotNull(suggestion);
        Assert.NotEqual(Island.Letters, suggestion.Island);
        Assert.True(suggestion.IsNudge, "three letters visits in a row: Bip should steer elsewhere");
    }

    [Fact]
    public void NeverSuggestsLockedGames()
    {
        var content = Library();
        var progress = new ChildProgress();
        var rng = new SeededGenerator(43);
        var recommender = new PlayRecommender(content);
        for (var i = 0; i < 50; i++)
        {
            var suggestion = recommender.Suggest(progress, Band.Foundation, 0, Rules, rng);
            Assert.NotNull(suggestion);
            var game = content.Game(suggestion.GameId)!;
            var unlocked = game.Skills.Any(id => content.Skill(id) is Skill skill && progress.IsUnlocked(skill, Band.Foundation, content.Skill));
            Assert.True(unlocked, $"{suggestion.GameId} is still locked");
        }
    }

    // MARK: Play-time breaks

    private static BreakSettings Settings(int play = 20, int rest = 20, int? dailyMax = null) => new(play, rest, dailyMax);

    private static readonly DateTimeOffset Reference = new(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PlayThenBreakThenResume()
    {
        var state = new BreakState { DayStamp = 100 };
        var now = Reference.AddSeconds(1_000_000);
        Assert.Equal(BreakPhase.Playing, PlayBreaks.Advance(state, 19 * 60, now, 100, Settings()));
        Assert.Equal(BreakPhase.BreakTime, PlayBreaks.Advance(state, 60, now, 100, Settings()));
        // Mid-break: still charging, even with no new play.
        Assert.Equal(BreakPhase.BreakTime, PlayBreaks.Advance(state, 0, now.AddMinutes(10), 100, Settings()));
        // After the break: playing again with a clean slate.
        Assert.Equal(BreakPhase.Playing, PlayBreaks.Advance(state, 0, now.AddSeconds(20 * 60 + 1), 100, Settings()));
        Assert.Equal(0, state.PlayedSeconds);
        Assert.Null(state.BreakEndsAt);
    }

    [Fact]
    public void BreakSurvivesQuitting()
    {
        var state = new BreakState { DayStamp = 100 };
        var now = Reference.AddSeconds(2_000_000);
        Assert.Equal(BreakPhase.BreakTime, PlayBreaks.Advance(state, 20 * 60, now, 100, Settings()));
        // The app quits and reopens: the saved state still says charging.
        var loaded = BipJson.Decode<BreakState>(BipJson.Encode(state));
        Assert.Equal(BreakPhase.BreakTime, PlayBreaks.Advance(loaded, 0, now.AddMinutes(5), 100, Settings()));
    }

    [Fact]
    public void DailyMaximum()
    {
        var state = new BreakState { DayStamp = 100 };
        var now = Reference.AddSeconds(3_000_000);
        // Short breaks (1 min) so the day fills through several play sessions.
        var shortBreaks = Settings(play: 20, rest: 1, dailyMax: 30);
        Assert.Equal(BreakPhase.BreakTime, PlayBreaks.Advance(state, 20 * 60, now, 100, shortBreaks));
        var afterBreak = now.AddSeconds(61);
        Assert.Equal(BreakPhase.DayDone, PlayBreaks.Advance(state, 10 * 60, afterBreak, 100, shortBreaks));
        // A new day starts clean.
        Assert.Equal(BreakPhase.Playing, PlayBreaks.Advance(state, 0, afterBreak, 101, shortBreaks));
        Assert.Equal(0, state.PlayedTodaySeconds);
    }

    [Fact]
    public void ParentEndsBreakEarly()
    {
        var state = new BreakState { DayStamp = 100 };
        var now = Reference.AddSeconds(4_000_000);
        Assert.Equal(BreakPhase.BreakTime, PlayBreaks.Advance(state, 20 * 60, now, 100, Settings()));
        PlayBreaks.EndBreakEarly(state);
        Assert.Equal(BreakPhase.Playing, PlayBreaks.Advance(state, 0, now, 100, Settings()));
    }

    [Fact]
    public void TheBatteryFillsWithTheBreakAndCountsDownWholeMinutes()
    {
        var state = new BreakState();
        var now = new DateTimeOffset(2026, 10, 3, 10, 0, 0, TimeSpan.Zero);
        Assert.Equal(1, PlayBreaks.BreakProgress(state, now, 20));
        Assert.Equal(0, PlayBreaks.MinutesLeft(state, now));

        Assert.Equal(BreakPhase.BreakTime, PlayBreaks.Advance(state, 20 * 60, now, 100, Settings()));
        Assert.Equal(0, PlayBreaks.BreakProgress(state, now, 20), 3);
        Assert.Equal(20, PlayBreaks.MinutesLeft(state, now));

        Assert.Equal(0.5, PlayBreaks.BreakProgress(state, now.AddMinutes(10), 20), 3);
        Assert.Equal(10, PlayBreaks.MinutesLeft(state, now.AddMinutes(10)));
        // Part of a minute left still says "1 minute", until the break is over.
        Assert.Equal(1, PlayBreaks.MinutesLeft(state, now.AddMinutes(19).AddSeconds(30)));
        Assert.Equal(1, PlayBreaks.BreakProgress(state, now.AddMinutes(25), 20));
        Assert.Equal(0, PlayBreaks.MinutesLeft(state, now.AddMinutes(25)));
        // A shorter break setting after the break started can't push the battery below empty.
        Assert.Equal(0, PlayBreaks.BreakProgress(state, now, 5));
    }

    // MARK: Stickers and the mystery box

    [Fact]
    public void StarsAccrueAndPagesFill()
    {
        var progress = new ChildProgress();
        for (var i = 0; i < 9; i++) progress.RecordAnswer(true, "count_10", null, 0, Rules);
        progress.RecordAnswer(false, "count_10", null, 0, Rules);
        Assert.Equal(9, progress.Stars); // only right answers earn stars
        Assert.Equal(9, StickerBook.StarsTowardsNextPage(progress));
        progress.RecordAnswer(true, "count_10", null, 0, Rules);
        Assert.Equal(0, StickerBook.StarsTowardsNextPage(progress)); // ten stars fill a page
    }

    [Fact]
    public void MysteryBoxOpensOnceADay()
    {
        var progress = new ChildProgress();
        Assert.True(progress.ClaimMysteryBox(7));
        Assert.Equal(ChildProgress.MysteryBonusStars, progress.Stars);
        Assert.False(progress.ClaimMysteryBox(7), "already opened today");
        Assert.True(progress.ClaimMysteryBox(8));
        Assert.Equal(2 * ChildProgress.MysteryBonusStars, progress.Stars);
    }

    [Fact]
    public void StickerBookTracksMastery()
    {
        var content = Library();
        Assert.Equal(63 + 55, StickerBook.AllStickers(content).Count);
        var progress = new ChildProgress();
        Assert.Empty(StickerBook.Earned(progress, content));
        Assert.Equal(63 + 55, StickerBook.Missing(progress, content).Count);
        Master(progress, "count_10", [0, 1]);
        var earned = StickerBook.Earned(progress, content);
        Assert.Contains("skill_count_10", earned);
        Assert.DoesNotContain("skill_count_20", earned); // not mastered yet
    }

    [Fact]
    public void OldSavesStillLoad()
    {
        // Version 1 had no stars, mystery box or breaks: they decode as empty.
        var loaded = ChildProgress.Decode("""{"version":1,"sounds":{"skills":{}},"skills":{},"recentGames":[]}""");
        Assert.Equal(0, loaded.Stars);
        Assert.Null(loaded.LastMysteryDay);
        Assert.Null(loaded.Breaks);
        Assert.Empty(loaded.RecentGames);
    }
}
