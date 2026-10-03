using System.Text.Json.Serialization;

namespace BipCore;

/// <summary>
/// How long play lasts before Bip's battery runs low, and how long he charges.
/// Parents change both behind the parent gate; the daily maximum is optional.
/// </summary>
public sealed record BreakSettings
{
    public int PlayMinutes { get; set; } = 20;
    public int BreakMinutes { get; set; } = 20;
    public int? DailyMaxMinutes { get; set; }

    public BreakSettings() { }

    public BreakSettings(int playMinutes = 20, int breakMinutes = 20, int? dailyMaxMinutes = null)
    {
        PlayMinutes = playMinutes;
        BreakMinutes = breakMinutes;
        DailyMaxMinutes = dailyMaxMinutes;
    }
}

/// <summary>
/// Where the play-time break stands. Saved, and the break counts down on the wall clock, so
/// quitting and reopening the app cannot skip it. The end time is saved the way the Swift app saves
/// it (seconds since 1 January 2001), so a break in progress carries over.
/// </summary>
public sealed record BreakState
{
    /// <summary>Play seconds banked since the last break.</summary>
    public int PlayedSeconds { get; set; }
    /// <summary>When the break ends (null while playing).</summary>
    [JsonConverter(typeof(ReferenceDateConverter))]
    public DateTimeOffset? BreakEndsAt { get; set; }
    /// <summary>The day the daily total belongs to.</summary>
    public int DayStamp { get; set; }
    /// <summary>Play seconds banked today.</summary>
    public int PlayedTodaySeconds { get; set; }
}

public enum BreakPhase
{
    /// <summary>Playing. Bip's battery is fine.</summary>
    Playing,
    /// <summary>Charging: games stay closed until the break ends.</summary>
    BreakTime,
    /// <summary>The daily maximum is reached: no more play today.</summary>
    DayDone,
}

/// <summary>
/// Play-time breaks (docs/GAMES.md): after playMinutes of play Bip's battery runs low and he
/// charges for breakMinutes. The child always finishes the current game first — the app calls
/// <see cref="Advance"/> with the seconds played since the last check and acts when the phase changes.
/// </summary>
public static class PlayBreaks
{
    /// <summary>
    /// Banks <paramref name="elapsed"/> play seconds and reports where things stand. Pure apart from
    /// the clock passed in, so the unit tests drive it with fixed dates.
    /// </summary>
    public static BreakPhase Advance(BreakState state, int elapsed, DateTimeOffset now, int day, BreakSettings settings)
    {
        if (day != state.DayStamp)
        {
            state.DayStamp = day;
            state.PlayedTodaySeconds = 0;
        }
        if (state.BreakEndsAt is DateTimeOffset endsAt)
        {
            if (now >= endsAt)
            {
                state.BreakEndsAt = null;
                state.PlayedSeconds = 0;
            }
            else
            {
                return BreakPhase.BreakTime;
            }
        }
        state.PlayedSeconds += Math.Max(0, elapsed);
        state.PlayedTodaySeconds += Math.Max(0, elapsed);
        if (settings.DailyMaxMinutes is int max && state.PlayedTodaySeconds >= max * 60) return BreakPhase.DayDone;
        if (state.PlayedSeconds >= settings.PlayMinutes * 60)
        {
            state.BreakEndsAt = now.AddSeconds(settings.BreakMinutes * 60);
            return BreakPhase.BreakTime;
        }
        return BreakPhase.Playing;
    }

    /// <summary>
    /// How far through the break Bip is, from 0 (just started) to 1 (charged), for the battery on
    /// the charging screen. 1 when there is no break.
    /// </summary>
    public static double BreakProgress(BreakState state, DateTimeOffset now, int breakMinutes)
    {
        if (state.BreakEndsAt is not DateTimeOffset endsAt || breakMinutes <= 0) return 1;
        var left = (endsAt - now).TotalSeconds;
        return Math.Clamp(1 - left / (breakMinutes * 60.0), 0, 1);
    }

    /// <summary>Whole minutes until games open again, rounded up (so "in 1 minute" until it ends). 0 when there is no break.</summary>
    public static int MinutesLeft(BreakState state, DateTimeOffset now) =>
        state.BreakEndsAt is DateTimeOffset endsAt && endsAt > now ? (int)Math.Ceiling((endsAt - now).TotalMinutes) : 0;

    /// <summary>A parent ends the break early from settings.</summary>
    public static void EndBreakEarly(BreakState state)
    {
        state.BreakEndsAt = null;
        state.PlayedSeconds = 0;
    }
}
