/// One phonics sound as the games use it, built from a grapheme in Content/phonics/graphemes.json.
public struct PhonicsSound: Hashable, Sendable, Identifiable {
    /// Unique id ("s", "ck", "ow_long").
    public let id: String
    /// The letters the child sees ("s", "ow", "a-e").
    public let grapheme: String
    public let group: Int
    /// The sound itself. Two graphemes with the same IPA sound the same (c, k and ck).
    public let ipa: String
    public let kind: GraphemeKind
    /// The pure-sound clip, e.g. snd_s.
    public let soundClip: String
    /// The picture word used to introduce the sound (s → sun).
    public let pictureWord: String
    public let picture: String
    public let letterName: String?

    public init(_ grapheme: Grapheme) {
        id = grapheme.id
        self.grapheme = grapheme.grapheme
        group = grapheme.group
        ipa = grapheme.ipa
        kind = grapheme.kind
        soundClip = grapheme.audio
        pictureWord = grapheme.mnemonicWord
        picture = grapheme.mnemonicPicture
        letterName = grapheme.letterName
    }

    public var wordClip: String { AudioCatalogue.wordClip(for: pictureWord) }
    /// Stop sounds have to be trimmed to a clean burst so children don't learn "tuh".
    public var mustBeClipped: Bool { kind == .bouncy }

    /// True when a child could mix the two up in a game: the same sound (c and k) or the
    /// same letters (ow in cow and ow in snow). A sound is not confusable with itself.
    public func isConfusable(with other: PhonicsSound) -> Bool {
        id != other.id && (ipa == other.ipa || grapheme == other.grapheme)
    }
}

/// One teaching group, e.g. group 1: s a t p i n.
public struct PhonicsGroup: Sendable, Identifiable {
    public let number: Int
    public let band: Band
    public let sounds: [PhonicsSound]
    public var id: Int { number }
    /// The skill in skills.json that this group teaches.
    public var skillID: String { PhonicsCourse.skillID(forGroup: number) }
}

/// The phonics teaching order, read from the content. Sounds come first; letter names only after group 5.
public struct PhonicsCourse: Sendable {
    public let groups: [PhonicsGroup]

    public init(_ file: GraphemesFile) {
        groups = file.groups.sorted { $0.group < $1.group }.map { entry in
            PhonicsGroup(number: entry.group, band: entry.band,
                         sounds: file.graphemes.filter { $0.group == entry.group }.map(PhonicsSound.init))
        }
    }

    public init(_ content: ContentLibrary) {
        self.init(content.phonics)
    }

    /// Phonics group skills are called snd_g1 … snd_g9 in skills.json.
    public static func skillID(forGroup number: Int) -> String { "snd_g\(number)" }

    /// The group number a phonics skill id belongs to, or nil for other skills.
    public static func group(forSkill id: String) -> Int? {
        id.hasPrefix("snd_g") ? Int(id.dropFirst("snd_g".count)) : nil
    }

    /// Every sound in teaching order.
    public var allSounds: [PhonicsSound] { groups.flatMap(\.sounds) }

    public var firstGroup: PhonicsGroup { groups[0] }

    public func group(_ number: Int) -> PhonicsGroup? {
        groups.first { $0.number == number }
    }

    public func sound(id: String) -> PhonicsSound? {
        allSounds.first { $0.id == id }
    }

    /// Every sound in groups 1…`group`, in teaching order.
    public func sounds(upToGroup group: Int) -> [PhonicsSound] {
        groups.filter { $0.number <= group }.flatMap(\.sounds)
    }

    public static let alphabet: [String] = "abcdefghijklmnopqrstuvwxyz".map { String($0) }

    /// Which of the 26 letters a set of known sounds covers. "qu" covers q.
    public static func lettersCovered(byKnownSounds known: Set<String>) -> Set<String> {
        var letters = Set(known.filter { $0.count == 1 })
        if known.contains("qu") { letters.insert("q") }
        return letters.intersection(alphabet)
    }

    /// Letter names ("this is ess") are only taught once the sounds of all 26 letters are known.
    public static func letterNamesUnlocked(knownSounds: Set<String>) -> Bool {
        lettersCovered(byKnownSounds: knownSounds).count == alphabet.count
    }
}
