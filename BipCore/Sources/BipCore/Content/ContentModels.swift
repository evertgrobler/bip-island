import Foundation

// Codable models for every file in Content/. The JSON is the single source of truth: these types
// only describe its shape. A unit test decodes each file and encodes it again, and fails if any key
// would be lost, so a new field in the JSON must be added here too.

// MARK: - Shared

/// The four child bands, youngest first.
public enum Band: String, Codable, Sendable, CaseIterable, Comparable {
    case foundation, stage1, stage2, stage3

    public static func < (lhs: Band, rhs: Band) -> Bool {
        allCases.firstIndex(of: lhs)! < allCases.firstIndex(of: rhs)!
    }
}

public enum Island: String, Codable, Sendable, CaseIterable {
    case letters, numbers, words, coding
}

/// An age range written as "4-6" in the content.
public struct AgeRange: Codable, Hashable, Sendable {
    public let youngest: Int
    public let oldest: Int

    public init(youngest: Int, oldest: Int) {
        self.youngest = youngest
        self.oldest = oldest
    }

    public func contains(_ age: Int) -> Bool { (youngest...oldest).contains(age) }

    public init(from decoder: Decoder) throws {
        let container = try decoder.singleValueContainer()
        let text = try container.decode(String.self)
        let parts = text.split(separator: "-").compactMap { Int($0.trimmingCharacters(in: .whitespaces)) }
        guard parts.count == 2, parts[0] <= parts[1] else {
            throw DecodingError.dataCorruptedError(in: container, debugDescription: "Age range '\(text)' should look like 4-6")
        }
        youngest = parts[0]
        oldest = parts[1]
    }

    public func encode(to encoder: Encoder) throws {
        var container = encoder.singleValueContainer()
        try container.encode("\(youngest)-\(oldest)")
    }
}

// MARK: - curriculum/objectives.json

public struct ObjectivesFile: Codable, Sendable {
    public let note: String?
    public let objectives: [Objective]
}

public struct Objective: Codable, Hashable, Sendable {
    public let code: String
    public let framework: String
    public let stage: Int
    public let strand: String
    public let summary: String
    public let source: String
}

// MARK: - curriculum/skills.json

public struct SkillsFile: Codable, Sendable {
    public let note: String?
    /// The rules in words, for people reading the file.
    public let mastery: [String: String]
    /// The same rules as numbers, for the game.
    public let masteryRules: MasteryRules
    /// Age (as text, "4") to the band a new child starts in.
    public let startingBand: [String: Band]
    public let skills: [Skill]
}

/// How progress moves, from skills.json. See docs/CURRICULUM.md "Levels, mastery and review".
public struct MasteryRules: Codable, Equatable, Sendable {
    /// Right answers in a row that move a level up.
    public let correctInARowToMoveUp: Int
    /// Misses in a row that drop a level back.
    public let missesInARowToDropBack: Int
    /// How many recent attempts mastery looks at.
    public let masteredWindow: Int
    /// Percentage of those that must be right.
    public let masteredPercent: Int
    /// Different days those attempts must be spread over.
    public let masteredDistinctDays: Int
    /// Days until each review after mastery (2, then 5, then 14).
    public let reviewAfterDays: [Int]

    public init(correctInARowToMoveUp: Int, missesInARowToDropBack: Int, masteredWindow: Int,
                masteredPercent: Int, masteredDistinctDays: Int, reviewAfterDays: [Int]) {
        self.correctInARowToMoveUp = correctInARowToMoveUp
        self.missesInARowToDropBack = missesInARowToDropBack
        self.masteredWindow = masteredWindow
        self.masteredPercent = masteredPercent
        self.masteredDistinctDays = masteredDistinctDays
        self.reviewAfterDays = reviewAfterDays
    }
}

public struct Skill: Codable, Hashable, Sendable, Identifiable {
    public let id: String
    public let island: Island
    public let band: Band
    public let name: String
    public let objectives: [String]
    public let prerequisites: [String]
}

// MARK: - curriculum/games.json

public struct GamesFile: Codable, Sendable {
    public let note: String?
    public let session: SessionSettings
    public let games: [GameEntry]
}

public struct SessionSettings: Codable, Equatable, Sendable {
    /// Questions in one visit to a game, unless the game runs out of fresh content first.
    public let roundsPerSession: Int
}

/// One mini-game in the registry.
public struct GameEntry: Codable, Hashable, Sendable, Identifiable {
    public let id: String
    public let island: Island
    public let name: String
    public let ages: AgeRange
    public let skills: [String]
    public let objectives: [String]
    /// Which content file the rounds come from (for people; the game type knows how to read it).
    public let content: String
    public let buildPhase: Int
    /// Difficulty steps, easiest first. Missing or empty means the game has one level.
    public let levels: [GameLevel]?

    /// The steps, never empty.
    public var levelSteps: [GameLevel] {
        let steps = levels ?? []
        return steps.isEmpty ? [GameLevel.single] : steps
    }

    /// A level by index, clamped to the ones that exist.
    public func level(_ index: Int) -> GameLevel {
        let steps = levelSteps
        return steps[min(max(index, 0), steps.count - 1)]
    }

    /// Where a new child starts: the first level of their band (the last level that is marked
    /// with a band no older than theirs). A level without a band carries on the band before it.
    public func startingLevel(for band: Band) -> Int {
        var start = 0
        for (index, step) in levelSteps.enumerated() {
            if let marked = step.band, marked <= band { start = index }
        }
        return start
    }
}

/// One difficulty step of a game. Each game reads the fields it understands; the rest stay empty.
public struct GameLevel: Codable, Hashable, Sendable {
    /// Children of this band (and older) start at this level or above.
    public let band: Band?
    /// Counting games: the largest number asked.
    public let countTo: Int?
    /// Answers to pick from on screen.
    public let choices: Int?
    /// Quick Look: how long the dots show, in tenths of a second.
    public let flashTenths: Int?
    /// Bubble Pop: bubbles on screen and drift speed (100 = normal).
    public let bubbles: Int?
    public let speedPercent: Int?
    /// Word games: how many sounds the words have.
    public let soundCounts: [Int]?
    /// Bip's Path: the band of grid puzzles to use.
    public let gridBand: Band?
    /// Picture-ordering games: the most picture cards in one story.
    public let cards: Int?
    /// For people reading the file.
    public let note: String?

    public init(band: Band? = nil, countTo: Int? = nil, choices: Int? = nil, flashTenths: Int? = nil, bubbles: Int? = nil,
                speedPercent: Int? = nil, soundCounts: [Int]? = nil, gridBand: Band? = nil, cards: Int? = nil,
                note: String? = nil) {
        self.band = band
        self.countTo = countTo
        self.choices = choices
        self.flashTenths = flashTenths
        self.bubbles = bubbles
        self.speedPercent = speedPercent
        self.soundCounts = soundCounts
        self.gridBand = gridBand
        self.cards = cards
        self.note = note
    }

    static let single = GameLevel()
}

// MARK: - phonics/graphemes.json

public struct GraphemesFile: Codable, Sendable {
    public let note: String?
    public let groups: [PhonicsGroupEntry]
    public let graphemes: [Grapheme]
}

public struct PhonicsGroupEntry: Codable, Hashable, Sendable {
    public let group: Int
    public let band: Band
    public let ages: AgeRange
    public let objectives: [String]
    public let note: String?
}

/// How a sound behaves when spoken. This drives how its voice clip has to be made.
public enum GraphemeKind: String, Codable, Sendable, CaseIterable {
    /// Can be held: s m f n l r v z. Written "sssss", never "ess".
    case stretchy
    /// Stop sounds (t p k c b d g): must be clipped, no "tuh".
    case bouncy
    /// Short vowels a e i o u.
    case vowel
    /// More than one letter, one sound.
    case digraph
}

public struct Grapheme: Codable, Hashable, Sendable, Identifiable {
    /// Unique id ("s", "ow_long", "a_e").
    public let id: String
    /// The letters as the child sees them ("s", "ow", "a-e").
    public let grapheme: String
    public let group: Int
    public let ipa: String
    public let kind: GraphemeKind
    public let audio: String
    public let mnemonicWord: String
    public let mnemonicPicture: String
    public let letterName: String?
    public let handwritingFamily: String?
    public let narratorHint: String
    /// Another spelling of this grapheme's sound (ay is an alternative of ai).
    public let alternativeOf: String?
}

// MARK: - words/words.json

public struct WordsFile: Codable, Sendable {
    public let note: String?
    public let words: [Word]
}

public struct Word: Codable, Hashable, Sendable, Identifiable {
    public let word: String
    /// Grapheme ids in reading order.
    public let graphemes: [String]
    public let soundCount: Int
    /// Only offer the word once this phonics group is unlocked.
    public let decodableFromGroup: Int
    public let band: Band
    /// Grapheme id of the first sound.
    public let firstSound: String
    public let rime: String
    public let audio: String
    public let picturable: Bool
    public let objectives: [String]
    public let picture: String?
    public let tags: [String]?

    public var id: String { word }
}

// MARK: - words/tricky_words.json

public struct TrickyWordsFile: Codable, Sendable {
    public let note: String?
    public let stage1: TrickyWordList
    public let stage2: TrickyWordList
}

public struct TrickyWordList: Codable, Hashable, Sendable {
    public let objectives: [String]
    public let words: [String]
}

// MARK: - words/sentences.json

public struct SentencesFile: Codable, Sendable {
    public let note: String?
    public let sentences: [Sentence]
}

public struct Sentence: Codable, Hashable, Sendable, Identifiable {
    public let id: String
    public let text: String
    public let decodableFromGroup: Int
    public let audio: String
    public let pictureRight: String
    public let pictureRightBrief: String
    public let pictureWrong: String
    public let pictureWrongBrief: String
    public let objectives: [String]
}

// MARK: - words/endings.json

public struct EndingsFile: Codable, Sendable {
    public let note: String?
    public let plurals_s: [PluralEnding]
    public let plurals_es: [PluralEnding]
    public let verbs: [VerbEndings]
}

public struct PluralEnding: Codable, Hashable, Sendable {
    public let word: String
    public let plural: String
}

public struct VerbEndings: Codable, Hashable, Sendable {
    public let word: String
    public let s: String
    /// Missing when the past tense isn't a plain -ed (sing → sang).
    public let ed: String?
    public let ing: String
    public let note: String?
}

// MARK: - words/homophones.json

public struct HomophonesFile: Codable, Sendable {
    public let note: String?
    public let sets: [HomophoneSet]
}

public struct HomophoneSet: Codable, Hashable, Sendable {
    public let words: [String]
    public let sentences: [HomophoneSentence]
}

public struct HomophoneSentence: Codable, Hashable, Sendable {
    /// Has exactly one "___" gap.
    public let text: String
    public let answer: String
}

// MARK: - words/contractions.json

public struct ContractionsFile: Codable, Sendable {
    public let note: String?
    public let pairs: [ContractionPair]
}

public struct ContractionPair: Codable, Hashable, Sendable {
    public let long: String
    public let short: String
}

// MARK: - numbers/numbers.json

public struct NumbersFile: Codable, Sendable {
    public let note: String?
    public let bands: [String: NumberBand]
    public let countingObjects: [CountingObject]
    public let currency: Currency
}

/// What each band works on in the Numbers island. Fields only appear where they apply.
public struct NumberBand: Codable, Hashable, Sendable {
    public let ages: AgeRange
    public let countTo: Int
    public let subitiseTo: Int?
    public let subitiseUnfamiliarPatterns: Bool?
    public let bondsTo: Int?
    public let complementsOf: Int?
    public let addSubWithin: Int?
    public let addSubTwoDigitNoRegroup: Bool?
    public let doublesTo: Int?
    public let stepCounts: [Int]?
    public let timesTables: [Int]?
    public let ordinalsTo: Int?
    public let sequenceRules: [String]?
    public let growingPatterns: Bool?
    public let unknownsAsObjects: Bool?
    public let moneyWithChange: Bool?
    public let objectives: [String]
}

public struct CountingObject: Codable, Hashable, Sendable {
    public let id: String
    public let picture: String
    public let audioPlural: String
}

public struct Currency: Codable, Hashable, Sendable {
    public let name: String
    public let symbol: String
    public let coins: [Money]
    public let notes: [Money]
    public let byBand: [String: MoneyBand]
    public let shopItems: [ShopItem]
}

public struct Money: Codable, Hashable, Sendable {
    public let id: String
    public let label: String
    public let cents: Int
}

public struct MoneyBand: Codable, Hashable, Sendable {
    /// Coin and note ids.
    public let use: [String]
    public let maxTotal: String
    public let compareCombinations: Bool?
    public let giveChange: Bool?
    public let objectives: [String]
}

public struct ShopItem: Codable, Hashable, Sendable {
    public let id: String
    public let picture: String
}

// MARK: - coding/sequences.json

public struct SequencesFile: Codable, Sendable {
    public let note: String?
    public let sets: [SequenceSet]
}

public struct SequenceSet: Codable, Hashable, Sendable, Identifiable {
    public let id: String
    public let band: Band
    public let cards: [SequenceCard]
}

public struct SequenceCard: Codable, Hashable, Sendable {
    /// Position in the right order, from 1.
    public let n: Int
    public let text: String
    public let picture: String
    public let audio: String
}

// MARK: - coding/patterns.json

public struct PatternsFile: Codable, Sendable {
    public let note: String?
    public let itemSets: [String: [String]]
    public let rules: [PatternRule]
}

public struct PatternRule: Codable, Hashable, Sendable, Identifiable {
    public let id: String
    public let band: Band
    /// The repeating unit ("AB", "AAB"), or "any".
    public let unit: String?
    public let show: Int
    public let ask: Int
    public let mode: String?
    public let start: Int?
    public let step: Int?
    public let note: String?
}

// MARK: - coding/levels.json

public struct LevelsFile: Codable, Sendable {
    public let note: String?
    public let levels: [GridLevel]
    public let puddleRules: [PuddleLevel]
}

/// A grid cell. Rows count from the top. Written [row, column] in the content.
public struct GridPosition: Codable, Hashable, Sendable {
    public let row: Int
    public let column: Int

    public init(row: Int, column: Int) {
        self.row = row
        self.column = column
    }

    public init(from decoder: Decoder) throws {
        var container = try decoder.unkeyedContainer()
        row = try container.decode(Int.self)
        column = try container.decode(Int.self)
        guard container.isAtEnd else {
            throw DecodingError.dataCorruptedError(in: container, debugDescription: "A grid position is [row, column]")
        }
    }

    public func encode(to encoder: Encoder) throws {
        var container = encoder.unkeyedContainer()
        try container.encode(row)
        try container.encode(column)
    }
}

public struct GridSize: Codable, Hashable, Sendable {
    public let rows: Int
    public let cols: Int
}

/// One block repeated, written ["up", 3] in the content.
public struct RepeatStep: Codable, Hashable, Sendable {
    public let block: String
    public let times: Int

    public init(from decoder: Decoder) throws {
        var container = try decoder.unkeyedContainer()
        block = try container.decode(String.self)
        times = try container.decode(Int.self)
        guard container.isAtEnd else {
            throw DecodingError.dataCorruptedError(in: container, debugDescription: "A repeat step is [block, times]")
        }
    }

    public func encode(to encoder: Encoder) throws {
        var container = encoder.unkeyedContainer()
        try container.encode(block)
        try container.encode(times)
    }
}

/// A grid puzzle for Bip's Path, Fix-It and Repeat Robot.
public struct GridLevel: Codable, Hashable, Sendable, Identifiable {
    public let id: String
    public let game: String
    public let tier: String
    public let band: Band
    public let grid: GridSize
    public let start: GridPosition
    public let startFacing: String
    public let goal: GridPosition
    public let rocks: [GridPosition]
    public let blocks: [String]
    public let optimalProgram: [String]
    public let optimalLength: Int
    public let objectives: [String]
    // Fix-It levels only.
    public let buggyProgram: [String]?
    public let bugIndex: Int?
    public let fix: String?
    // Repeat Robot levels only.
    public let optimalWithRepeat: [RepeatStep]?
    public let blocksWithRepeat: Int?
}

/// A Puddle Rules corridor: one program must work on every map.
public struct PuddleLevel: Codable, Hashable, Sendable, Identifiable {
    public let id: String
    public let game: String
    public let band: Band
    public let corridorLength: Int
    public let maps: [PuddleMap]
    public let blocks: [String]
    public let solution: PuddleSolution
    public let objectives: [String]
    public let note: String?
}

public struct PuddleMap: Codable, Hashable, Sendable {
    public let puddlesAt: [Int]
}

public struct PuddleSolution: Codable, Hashable, Sendable {
    public let `repeat`: Int
    public let body: [PuddleRule]
}

/// "If puddle ahead, jump, otherwise go forward."
public struct PuddleRule: Codable, Hashable, Sendable {
    public let condition: String
    public let then: [String]
    public let otherwise: [String]

    enum CodingKeys: String, CodingKey {
        case condition = "if"
        case then
        case otherwise = "else"
    }
}

// MARK: - asset_manifest.json (generated)

public struct AssetManifestFile: Codable, Sendable {
    public let note: String?
    public let audio: [AudioAsset]
    public let pictures: [PictureAsset]
}

public struct AudioAsset: Codable, Hashable, Sendable, Identifiable {
    public let id: String
    /// Exactly what the narrator says.
    public let text: String
    public let notes: String
}

public struct PictureAsset: Codable, Hashable, Sendable, Identifiable {
    public let id: String
    public let brief: String
}
