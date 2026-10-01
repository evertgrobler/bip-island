import Foundation

/// Spoken instruction clips (`vo_<key>.m4a`). The exact words are in audio/script.csv.
public enum VoiceLine: String, CaseIterable, Sendable {
    case welcome = "vo_welcome"
    case islandSleeping = "vo_island_sleeping"
    case lettersIsland = "vo_letters_island"
    case meetNewSound = "vo_meet_new_sound"
    case sayItWithMe = "vo_say_it_with_me"
    case clickToHearAgain = "vo_click_to_hear_again"
    case findTheSound = "vo_find_the_sound"
    case popTheLetter = "vo_pop_the_letter"
    case levelUp = "vo_level_up"
    case letsPractiseAgain = "vo_lets_practise_again"
    case roundDone = "vo_round_done"
}

/// Every audio clip the game plays, following the file-name contract in CLAUDE.md.
/// A unit test checks this list matches audio/script.csv exactly.
public enum AudioCatalogue {
    public static let praiseCount = 10
    public static let hintCount = 4

    public static func soundClip(for sound: PhonicsSound) -> String { "snd_\(sound.id)" }
    public static func wordClip(for word: String) -> String { "word_\(word)" }
    public static func numberClip(_ n: Int) -> String { "num_\(n)" }

    public static let praiseClips: [String] = (1...praiseCount).map { String(format: "praise_%02d", $0) }
    public static let hintClips: [String] = (1...hintCount).map { String(format: "hint_%02d", $0) }

    /// The sounds the current build teaches (phase 1: group 1).
    public static var soundsInGame: [PhonicsSound] { Phonics.firstGroup.sounds }

    /// Every clip file name (without extension) the current build can play.
    public static var allClips: [String] {
        soundsInGame.map(\.soundClip)
            + soundsInGame.map(\.wordClip)
            + VoiceLine.allCases.map(\.rawValue)
            + praiseClips
            + hintClips
    }

    /// The naming contract: snd_, word_, num_, vo_ with lower-case keys; praise_NN and hint_NN.
    public static func followsNamingContract(_ name: String) -> Bool {
        name.range(of: #"^((snd|word|num|vo)_[a-z0-9_]+|(praise|hint)_[0-9]{2})$"#, options: .regularExpression) != nil
    }
}
