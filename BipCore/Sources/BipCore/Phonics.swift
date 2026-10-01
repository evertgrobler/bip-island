/// How a sound behaves when spoken. This drives how the voice clip has to be produced.
public enum SoundKind: String, Codable, Sendable, CaseIterable {
    /// Can be held: s m f n l r v z. Written "sssss", never "ess".
    case stretchy
    /// Short vowels a e i o u, as in apple, egg, igloo, octopus, umbrella. Never the letter name.
    case shortVowel
    /// Stop (bouncy) sounds t p k c b d g, plus ck. Must be clipped: no "tuh".
    case stop
    /// Other single sounds: h j w x y qu.
    case other
    /// Two letters, one sound: sh ch th ng ee oo ai.
    case digraph
}

/// One phonics sound, e.g. "s" as in sun.
public struct PhonicsSound: Hashable, Codable, Sendable, Identifiable {
    /// The letters that make the sound, lower case ("s", "ck", "qu", "sh").
    public let id: String
    public let kind: SoundKind
    /// The picture word used to introduce the sound (s → sun).
    public let pictureWord: String
    /// False when the sound is not at the start of the picture word (duck, box, ring, moon).
    public let isAtStartOfWord: Bool

    public init(_ id: String, _ kind: SoundKind, _ pictureWord: String, atStart: Bool = true) {
        self.id = id
        self.kind = kind
        self.pictureWord = pictureWord
        self.isAtStartOfWord = atStart
    }

    public var grapheme: String { id }
    /// Stop sounds have to be trimmed to a clean burst so children don't learn "tuh".
    public var mustBeClipped: Bool { kind == .stop }
    public var soundClip: String { AudioCatalogue.soundClip(for: self) }
    public var wordClip: String { AudioCatalogue.wordClip(for: pictureWord) }
}

/// A Jolly Phonics teaching group.
public struct PhonicsGroup: Sendable, Identifiable {
    public let number: Int
    public let minimumAge: Int
    public let sounds: [PhonicsSound]
    public var id: Int { number }

    public init(number: Int, minimumAge: Int, sounds: [PhonicsSound]) {
        self.number = number
        self.minimumAge = minimumAge
        self.sounds = sounds
    }
}

/// The phonics teaching order. Sounds come first; letter names only once all 26 letters' sounds are known.
public enum Phonics {
    public static let groups: [PhonicsGroup] = [
        PhonicsGroup(number: 1, minimumAge: 4, sounds: [
            PhonicsSound("s", .stretchy, "sun"),
            PhonicsSound("a", .shortVowel, "apple"),
            PhonicsSound("t", .stop, "tent"),
            PhonicsSound("p", .stop, "pig"),
            PhonicsSound("i", .shortVowel, "igloo"),
            PhonicsSound("n", .stretchy, "nest"),
        ]),
        PhonicsGroup(number: 2, minimumAge: 4, sounds: [
            PhonicsSound("m", .stretchy, "mop"),
            PhonicsSound("d", .stop, "dog"),
            PhonicsSound("g", .stop, "goat"),
            PhonicsSound("o", .shortVowel, "octopus"),
            PhonicsSound("c", .stop, "cat"),
            PhonicsSound("k", .stop, "kite"),
        ]),
        PhonicsGroup(number: 3, minimumAge: 4, sounds: [
            PhonicsSound("ck", .stop, "duck", atStart: false),
            PhonicsSound("e", .shortVowel, "egg"),
            PhonicsSound("u", .shortVowel, "umbrella"),
            PhonicsSound("r", .stretchy, "rabbit"),
        ]),
        PhonicsGroup(number: 4, minimumAge: 4, sounds: [
            PhonicsSound("h", .other, "hat"),
            PhonicsSound("b", .stop, "bed"),
            PhonicsSound("f", .stretchy, "fish"),
            PhonicsSound("l", .stretchy, "leg"),
        ]),
        PhonicsGroup(number: 5, minimumAge: 4, sounds: [
            PhonicsSound("j", .other, "jam"),
            PhonicsSound("v", .stretchy, "van"),
            PhonicsSound("w", .other, "web"),
            PhonicsSound("x", .other, "box", atStart: false),
            PhonicsSound("y", .other, "yoyo"),
            PhonicsSound("z", .stretchy, "zip"),
            PhonicsSound("qu", .other, "queen"),
        ]),
        PhonicsGroup(number: 6, minimumAge: 6, sounds: [
            PhonicsSound("sh", .digraph, "ship"),
            PhonicsSound("ch", .digraph, "chip"),
            PhonicsSound("th", .digraph, "thumb"),
            PhonicsSound("ng", .digraph, "ring", atStart: false),
            PhonicsSound("ee", .digraph, "bee", atStart: false),
            PhonicsSound("oo", .digraph, "moon", atStart: false),
            PhonicsSound("ai", .digraph, "rain", atStart: false),
        ]),
    ]

    public static var firstGroup: PhonicsGroup { groups[0] }

    /// Every sound in teaching order.
    public static var allSounds: [PhonicsSound] { groups.flatMap(\.sounds) }

    public static func sound(id: String) -> PhonicsSound? {
        allSounds.first { $0.id == id }
    }

    /// The groups a child of this age works through. Digraphs (group 6) wait until age 6.
    public static func groups(forAge age: Int) -> [PhonicsGroup] {
        groups.filter { $0.minimumAge <= age }
    }

    public static func teachingOrder(forAge age: Int) -> [PhonicsSound] {
        groups(forAge: age).flatMap(\.sounds)
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
