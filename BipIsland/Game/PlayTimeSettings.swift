import BipCore
import Combine

/// Play length, break length and the optional daily maximum, set by parents behind the
/// parent gate. Stored on this Mac only.
final class PlayTimeSettings: ObservableObject {
    private static let playKey = "bip.playMinutes"
    private static let breakKey = "bip.breakMinutes"
    private static let dailyMaxKey = "bip.dailyMaxMinutes"

    @Published var playMinutes: Int { didSet { save() } }
    @Published var breakMinutes: Int { didSet { save() } }
    /// Nil means no daily maximum.
    @Published var dailyMaxMinutes: Int? { didSet { save() } }

    init(defaults: UserDefaults = .standard) {
        self.defaults = defaults
        let play = defaults.integer(forKey: Self.playKey)
        playMinutes = play > 0 ? play : 20
        let rest = defaults.integer(forKey: Self.breakKey)
        breakMinutes = rest > 0 ? rest : 20
        let max = defaults.integer(forKey: Self.dailyMaxKey)
        dailyMaxMinutes = max > 0 ? max : nil
    }

    var breakSettings: BreakSettings {
        BreakSettings(playMinutes: playMinutes, breakMinutes: breakMinutes, dailyMaxMinutes: dailyMaxMinutes)
    }

    private let defaults: UserDefaults

    private func save() {
        defaults.set(playMinutes, forKey: Self.playKey)
        defaults.set(breakMinutes, forKey: Self.breakKey)
        defaults.set(dailyMaxMinutes ?? 0, forKey: Self.dailyMaxKey)
    }
}
