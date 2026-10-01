import Foundation

/// How long play lasts before Bip's battery runs low, and how long he charges.
/// Parents change both behind the parent gate; the daily maximum is optional.
public struct BreakSettings: Codable, Equatable, Sendable {
    public var playMinutes: Int
    public var breakMinutes: Int
    public var dailyMaxMinutes: Int?

    public init(playMinutes: Int = 20, breakMinutes: Int = 20, dailyMaxMinutes: Int? = nil) {
        self.playMinutes = playMinutes
        self.breakMinutes = breakMinutes
        self.dailyMaxMinutes = dailyMaxMinutes
    }
}

/// Where the play-time break stands. Saved in the child's profile, and the break counts down
/// on the wall clock, so quitting and reopening the app cannot skip it.
public struct BreakState: Codable, Equatable, Sendable {
    /// Play seconds banked since the last break.
    public var playedSeconds: Int
    /// When the break ends (nil while playing).
    public var breakEndsAt: Date?
    /// The day the daily total belongs to.
    public var dayStamp: Int
    /// Play seconds banked today.
    public var playedTodaySeconds: Int

    public init(playedSeconds: Int = 0, breakEndsAt: Date? = nil, dayStamp: Int = 0, playedTodaySeconds: Int = 0) {
        self.playedSeconds = playedSeconds
        self.breakEndsAt = breakEndsAt
        self.dayStamp = dayStamp
        self.playedTodaySeconds = playedTodaySeconds
    }
}

public enum BreakPhase: Equatable, Sendable {
    /// Playing. Bip's battery is fine.
    case playing
    /// Charging: games stay closed until the break ends.
    case breakTime
    /// The daily maximum is reached: no more play today.
    case dayDone
}

/// Play-time breaks (docs/GAMES.md): after `playMinutes` of play Bip's battery runs low and he
/// charges for `breakMinutes`. The child always finishes the current game first — the app calls
/// `advance` with the seconds played since the last check and acts when the phase changes.
public enum PlayBreaks {
    /// Banks `elapsed` play seconds and reports where things stand. Pure apart from the clock
    /// passed in, so the unit tests drive it with fixed dates.
    public static func advance(state: inout BreakState, elapsed: Int, now: Date, day: Int,
                               settings: BreakSettings) -> BreakPhase {
        if day != state.dayStamp {
            state.dayStamp = day
            state.playedTodaySeconds = 0
        }
        if let endsAt = state.breakEndsAt {
            if now >= endsAt {
                state.breakEndsAt = nil
                state.playedSeconds = 0
            } else {
                return .breakTime
            }
        }
        state.playedSeconds += max(0, elapsed)
        state.playedTodaySeconds += max(0, elapsed)
        if let max = settings.dailyMaxMinutes, state.playedTodaySeconds >= max * 60 {
            return .dayDone
        }
        if state.playedSeconds >= settings.playMinutes * 60 {
            state.breakEndsAt = now.addingTimeInterval(TimeInterval(settings.breakMinutes * 60))
            return .breakTime
        }
        return .playing
    }

    /// A parent ends the break early from settings.
    public static func endBreakEarly(state: inout BreakState) {
        state.breakEndsAt = nil
        state.playedSeconds = 0
    }
}
