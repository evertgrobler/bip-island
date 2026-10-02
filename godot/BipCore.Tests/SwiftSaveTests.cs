using System.Text.Json.Nodes;
using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>
/// The switchover imports progress saved by the Swift app, so a Swift-written save must load here
/// unchanged and save again in the same shape.
/// </summary>
public sealed class SwiftSaveTests
{
    /// <summary>A child's progress exactly as the Swift app's JSONEncoder writes it.</summary>
    private const string SwiftSave = """
        {"version":2,"sounds":{"skills":{"s":{"level":2,"correctStreak":1,"missStreak":0},"a":{"level":1,"correctStreak":0,"missStreak":0}}},
         "skills":{"snd_g1":{"recent":[{"correct":true,"day":9403},{"correct":false,"day":9404}],"totalAttempts":12,
                             "masteredOnDay":9400,"reviewsDone":1,"lastReviewDay":9402},
                   "count_10":{"recent":[{"correct":true,"day":9404}],"totalAttempts":1,"reviewsDone":0}},
         "recentGames":["sound_hunt","count_and_tap"],"stars":42,"lastMysteryDay":9404,
         "breaks":{"playedSeconds":300,"breakEndsAt":812567890.5,"dayStamp":9404,"playedTodaySeconds":900},
         "playSecondsByDay":{"9403":600,"9404":900},
         "gameLevels":{"count_and_tap":{"level":2,"correctStreak":0,"missStreak":1}}}
        """;

    [Fact]
    public void ASwiftSaveLoads()
    {
        var progress = ChildProgress.Decode(SwiftSave);
        Assert.Equal(2, progress.Version);
        Assert.Equal(SoundStage.Recognises, progress.Sounds.Stage("s"));
        Assert.Equal(SoundStage.Met, progress.Sounds.Stage("a"));
        Assert.Equal(1, progress.Sounds.Mastery("s").CorrectStreak);

        var g1 = progress.Skill("snd_g1");
        Assert.Equal(12, g1.TotalAttempts);
        Assert.Equal(9400, g1.MasteredOnDay);
        Assert.Equal(1, g1.ReviewsDone);
        Assert.Equal(9402, g1.LastReviewDay);
        Assert.Equal([new Attempt(true, 9403), new Attempt(false, 9404)], g1.Recent);
        Assert.True(progress.IsMastered("snd_g1"));
        Assert.Null(progress.Skill("count_10").MasteredOnDay);

        Assert.Equal(["sound_hunt", "count_and_tap"], progress.RecentGames);
        Assert.Equal(42, progress.Stars);
        Assert.Equal(9404, progress.LastMysteryDay);
        Assert.Equal(1500, progress.SecondsPlayed(7, 9404));
        Assert.Equal(2, progress.GameLevel("count_and_tap", 0));

        var breaks = Assert.IsType<BreakState>(progress.Breaks);
        Assert.Equal(300, breaks.PlayedSeconds);
        Assert.Equal(9404, breaks.DayStamp);
        Assert.Equal(900, breaks.PlayedTodaySeconds);
        // Swift saves dates as seconds since 1 January 2001 (UTC).
        Assert.Equal(new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(812567890.5), breaks.BreakEndsAt);
    }

    [Fact]
    public void ASwiftSaveSavesAgainInTheSameShape()
    {
        var again = ChildProgress.Decode(SwiftSave).Encode();
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(SwiftSave), JsonNode.Parse(again)), again);
    }

    [Fact]
    public void ASwiftBreakStateLoads()
    {
        // The Mac-wide break is saved on its own (UserDefaults "bip.breakState").
        var state = BipJson.Decode<BreakState>("""{"playedSeconds":0,"dayStamp":9404,"playedTodaySeconds":1200}""");
        Assert.Null(state.BreakEndsAt);
        Assert.Equal(1200, state.PlayedTodaySeconds);
        Assert.DoesNotContain("breakEndsAt", BipJson.Encode(state));
    }
}
