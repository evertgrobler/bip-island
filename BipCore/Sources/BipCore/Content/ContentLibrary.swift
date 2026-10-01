import Foundation

/// A content file that couldn't be read, with the file name so the problem is easy to find.
public struct ContentLoadError: Error, CustomStringConvertible {
    public let file: String
    public let underlying: Error

    public var description: String {
        "Couldn't read Content/\(file): \(Self.explain(underlying))"
    }

    private static func explain(_ error: Error) -> String {
        guard let decoding = error as? DecodingError else { return error.localizedDescription }
        func path(_ context: DecodingError.Context) -> String {
            context.codingPath.map { $0.intValue.map { "[\($0)]" } ?? ".\($0.stringValue)" }.joined()
        }
        switch decoding {
        case let .keyNotFound(key, context): return "missing \"\(key.stringValue)\" at \(path(context))"
        case let .typeMismatch(_, context), let .valueNotFound(_, context), let .dataCorrupted(context):
            return "\(context.debugDescription) at \(path(context))"
        @unknown default: return "\(decoding)"
        }
    }
}

/// Every content file, decoded. Load it once at start-up and pass it to the games.
public struct ContentLibrary: Sendable {
    /// Every file the loader reads, relative to the Content folder.
    public static let files = [
        "curriculum/objectives.json", "curriculum/skills.json", "curriculum/games.json",
        "phonics/graphemes.json",
        "words/words.json", "words/tricky_words.json", "words/sentences.json", "words/endings.json",
        "words/homophones.json", "words/contractions.json",
        "numbers/numbers.json",
        "coding/sequences.json", "coding/patterns.json", "coding/levels.json",
        "asset_manifest.json",
    ]

    public let objectives: ObjectivesFile
    public let skills: SkillsFile
    public let games: GamesFile
    public let phonics: GraphemesFile
    public let words: WordsFile
    public let trickyWords: TrickyWordsFile
    public let sentences: SentencesFile
    public let endings: EndingsFile
    public let homophones: HomophonesFile
    public let contractions: ContractionsFile
    public let numbers: NumbersFile
    public let sequences: SequencesFile
    public let patterns: PatternsFile
    public let levels: LevelsFile
    public let manifest: AssetManifestFile

    // Lookups, built once.
    private let graphemesByID: [String: Grapheme]
    private let wordsByText: [String: Word]
    private let skillsByID: [String: Skill]
    private let gamesByID: [String: GameEntry]
    public let audioIDs: Set<String>
    public let pictureIDs: Set<String>

    /// Reads every file from a Content folder (in the app: `Bip Island.app/Contents/Resources/Content`).
    public init(directory: URL) throws {
        func load<T: Decodable>(_ file: String, as type: T.Type = T.self) throws -> T {
            do {
                let data = try Data(contentsOf: directory.appendingPathComponent(file))
                return try JSONDecoder().decode(T.self, from: data)
            } catch {
                throw ContentLoadError(file: file, underlying: error)
            }
        }
        objectives = try load("curriculum/objectives.json")
        skills = try load("curriculum/skills.json")
        games = try load("curriculum/games.json")
        phonics = try load("phonics/graphemes.json")
        words = try load("words/words.json")
        trickyWords = try load("words/tricky_words.json")
        sentences = try load("words/sentences.json")
        endings = try load("words/endings.json")
        homophones = try load("words/homophones.json")
        contractions = try load("words/contractions.json")
        numbers = try load("numbers/numbers.json")
        sequences = try load("coding/sequences.json")
        patterns = try load("coding/patterns.json")
        levels = try load("coding/levels.json")
        manifest = try load("asset_manifest.json")

        graphemesByID = Dictionary(phonics.graphemes.map { ($0.id, $0) }, uniquingKeysWith: { first, _ in first })
        wordsByText = Dictionary(words.words.map { ($0.word, $0) }, uniquingKeysWith: { first, _ in first })
        skillsByID = Dictionary(skills.skills.map { ($0.id, $0) }, uniquingKeysWith: { first, _ in first })
        gamesByID = Dictionary(games.games.map { ($0.id, $0) }, uniquingKeysWith: { first, _ in first })
        audioIDs = Set(manifest.audio.map(\.id))
        pictureIDs = Set(manifest.pictures.map(\.id))
    }

    /// Loads the Content folder bundled into the app.
    public static func bundled(in bundle: Bundle = .main) throws -> ContentLibrary {
        guard let url = bundle.url(forResource: "Content", withExtension: nil) else {
            throw ContentLoadError(file: "", underlying: CocoaError(.fileNoSuchFile))
        }
        return try ContentLibrary(directory: url)
    }

    public func grapheme(id: String) -> Grapheme? { graphemesByID[id] }
    public func word(_ text: String) -> Word? { wordsByText[text] }
    public func skill(id: String) -> Skill? { skillsByID[id] }
    public func game(id: String) -> GameEntry? { gamesByID[id] }
    public var masteryRules: MasteryRules { skills.masteryRules }

    /// The band a new child of this age starts in. Younger than the table → the first band;
    /// older → the last.
    public func startingBand(forAge age: Int) -> Band {
        let table = skills.startingBand.compactMap { key, band in Int(key).map { ($0, band) } }.sorted { $0.0 < $1.0 }
        guard let first = table.first else { return .foundation }
        if age < first.0 { return first.1 }
        return table.last { $0.0 <= age }?.1 ?? first.1
    }
}
