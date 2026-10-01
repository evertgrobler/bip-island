import XCTest
import BipCore

/// Every content file decodes, and the Swift models keep every key in it.
final class ContentLoaderTests: XCTestCase {
    func testTheWholeLibraryLoads() throws {
        let content = try TestContent.library()
        XCTAssertFalse(content.objectives.objectives.isEmpty)
        XCTAssertFalse(content.skills.skills.isEmpty)
        XCTAssertFalse(content.games.games.isEmpty)
        XCTAssertFalse(content.phonics.graphemes.isEmpty)
        XCTAssertFalse(content.words.words.isEmpty)
        XCTAssertFalse(content.trickyWords.stage1.words.isEmpty)
        XCTAssertFalse(content.sentences.sentences.isEmpty)
        XCTAssertFalse(content.endings.plurals_s.isEmpty)
        XCTAssertFalse(content.homophones.sets.isEmpty)
        XCTAssertFalse(content.contractions.pairs.isEmpty)
        XCTAssertFalse(content.numbers.countingObjects.isEmpty)
        XCTAssertFalse(content.sequences.sets.isEmpty)
        XCTAssertFalse(content.patterns.rules.isEmpty)
        XCTAssertFalse(content.levels.levels.isEmpty)
        XCTAssertFalse(content.manifest.audio.isEmpty)
    }

    func testEveryFileDecodesWithoutLosingAnyKey() throws {
        try checkRoundTrip(ObjectivesFile.self, "curriculum/objectives.json")
        try checkRoundTrip(SkillsFile.self, "curriculum/skills.json")
        try checkRoundTrip(GamesFile.self, "curriculum/games.json")
        try checkRoundTrip(GraphemesFile.self, "phonics/graphemes.json")
        try checkRoundTrip(WordsFile.self, "words/words.json")
        try checkRoundTrip(TrickyWordsFile.self, "words/tricky_words.json")
        try checkRoundTrip(SentencesFile.self, "words/sentences.json")
        try checkRoundTrip(EndingsFile.self, "words/endings.json")
        try checkRoundTrip(HomophonesFile.self, "words/homophones.json")
        try checkRoundTrip(ContractionsFile.self, "words/contractions.json")
        try checkRoundTrip(NumbersFile.self, "numbers/numbers.json")
        try checkRoundTrip(SequencesFile.self, "coding/sequences.json")
        try checkRoundTrip(PatternsFile.self, "coding/patterns.json")
        try checkRoundTrip(LevelsFile.self, "coding/levels.json")
        try checkRoundTrip(AssetManifestFile.self, "asset_manifest.json")
    }

    /// A new JSON file in Content/ must get a model and be added to the loader.
    func testTheLoaderReadsEveryContentFile() throws {
        let enumerator = try XCTUnwrap(FileManager.default.enumerator(at: TestContent.directory, includingPropertiesForKeys: nil))
        let base = TestContent.directory.standardizedFileURL.path + "/"
        var found = Set<String>()
        for case let url as URL in enumerator where url.pathExtension == "json" {
            found.insert(url.standardizedFileURL.path.replacingOccurrences(of: base, with: ""))
        }
        XCTAssertEqual(found, Set(ContentLibrary.files))
    }

    func testAMissingFileNamesTheFile() throws {
        let copy = try copyOfContent()
        try FileManager.default.removeItem(at: copy.appendingPathComponent("words/words.json"))
        XCTAssertThrowsError(try ContentLibrary(directory: copy)) { error in
            XCTAssertTrue(String(describing: error).contains("words/words.json"), "\(error)")
        }
    }

    func testABrokenFileNamesTheFileAndTheKey() throws {
        let copy = try copyOfContent()
        let file = copy.appendingPathComponent("curriculum/games.json")
        let text = try String(contentsOf: file, encoding: .utf8).replacingOccurrences(of: "\"buildPhase\"", with: "\"buildPhaze\"")
        try text.write(to: file, atomically: true, encoding: .utf8)
        XCTAssertThrowsError(try ContentLibrary(directory: copy)) { error in
            let message = String(describing: error)
            XCTAssertTrue(message.contains("curriculum/games.json"), message)
            XCTAssertTrue(message.contains("buildPhase"), message)
        }
    }

    func testLookups() throws {
        let content = try TestContent.library()
        XCTAssertEqual(content.grapheme(id: "s")?.mnemonicWord, "sun")
        XCTAssertEqual(content.word("sat")?.decodableFromGroup, 1)
        XCTAssertEqual(content.skill(id: "snd_g2")?.prerequisites, ["snd_g1"])
        XCTAssertEqual(content.game(id: "sound_hunt")?.island, .letters)
        XCTAssertNil(content.word("spaza"))
        XCTAssertTrue(content.audioIDs.contains("snd_s"))
        XCTAssertTrue(content.pictureIDs.contains("pic_sun"))
    }

    func testThePhaseOneGamesAreInTheRegistry() throws {
        let content = try TestContent.library()
        for id in [MeetTheSoundGame.id, SoundHuntGame.id, BubblePopGame.id] {
            let entry = try content.entry(forGame: id)
            XCTAssertEqual(entry.island, .letters, id)
            XCTAssertEqual(entry.buildPhase, 1, id)
            XCTAssertTrue(entry.ages.contains(4), id)
            for skill in entry.skills {
                XCTAssertNotNil(content.skill(id: skill), "\(id): \(skill)")
            }
        }
        XCTAssertThrowsError(try content.entry(forGame: "not_a_game"))
    }

    func testEveryGameAndSkillReferenceExists() throws {
        let content = try TestContent.library()
        let objectives = Set(content.objectives.objectives.map(\.code))
        for game in content.games.games {
            XCTAssertTrue(game.skills.allSatisfy { content.skill(id: $0) != nil }, game.id)
            XCTAssertTrue(Set(game.objectives).isSubset(of: objectives), game.id)
        }
        for skill in content.skills.skills {
            XCTAssertTrue(skill.prerequisites.allSatisfy { content.skill(id: $0) != nil }, skill.id)
        }
        for group in content.phonics.groups {
            XCTAssertNotNil(content.skill(id: PhonicsCourse.skillID(forGroup: group.group)), "group \(group.group)")
        }
    }

    func testMasteryRulesComeFromTheContent() throws {
        let rules = try TestContent.library().masteryRules
        XCTAssertEqual(rules, TestContent.rules, "skills.json masteryRules should say 3 up, 2 back, 80% of 10 over 2 days, review 2/5/14")
    }

    func testStartingBandByAge() throws {
        let content = try TestContent.library()
        XCTAssertEqual(content.startingBand(forAge: 3), .foundation)
        XCTAssertEqual(content.startingBand(forAge: 4), .foundation)
        XCTAssertEqual(content.startingBand(forAge: 5), .foundation)
        XCTAssertEqual(content.startingBand(forAge: 6), .stage1)
        XCTAssertEqual(content.startingBand(forAge: 7), .stage2)
        XCTAssertEqual(content.startingBand(forAge: 8), .stage3)
        XCTAssertEqual(content.startingBand(forAge: 11), .stage3)
    }

    func testAgeRangesAndGridPositionsDecode() throws {
        let range = try JSONDecoder().decode([AgeRange].self, from: Data(#"["4-6"]"#.utf8))[0]
        XCTAssertEqual(range, AgeRange(youngest: 4, oldest: 6))
        XCTAssertTrue(range.contains(5))
        XCTAssertFalse(range.contains(7))
        XCTAssertThrowsError(try JSONDecoder().decode([AgeRange].self, from: Data(#"["four"]"#.utf8)))
        XCTAssertThrowsError(try JSONDecoder().decode([GridPosition].self, from: Data("[[1, 2, 3]]".utf8)))
        XCTAssertEqual(Band.foundation < Band.stage1, true)
        XCTAssertEqual(Band.stage3 > Band.stage2, true)
    }

    // MARK: Helpers

    /// Decodes a file, encodes the model again, and checks nothing was dropped or changed.
    private func checkRoundTrip<T: Codable>(_ type: T.Type, _ file: String, line: UInt = #line) throws {
        let data = try Data(contentsOf: TestContent.directory.appendingPathComponent(file))
        let model: T
        do {
            model = try JSONDecoder().decode(T.self, from: data)
        } catch {
            return XCTFail("\(file) didn't decode: \(error)", line: line)
        }
        let original = try canonical(data)
        let again = try canonical(JSONEncoder().encode(model))
        if original != again {
            XCTFail("\(file): the Swift model loses or changes something. Add the missing fields to ContentModels.swift. "
                    + firstDifference(original, again), line: line)
        }
    }

    private func canonical(_ data: Data) throws -> String {
        let object = try JSONSerialization.jsonObject(with: data)
        let sorted = try JSONSerialization.data(withJSONObject: object, options: [.sortedKeys])
        return String(decoding: sorted, as: UTF8.self)
    }

    private func firstDifference(_ a: String, _ b: String) -> String {
        let index = zip(a, b).prefix { $0 == $1 }.count
        let start = max(0, index - 60)
        return "Around: …\(a.dropFirst(start).prefix(140))… vs …\(b.dropFirst(start).prefix(140))…"
    }

    private func copyOfContent() throws -> URL {
        let copy = FileManager.default.temporaryDirectory.appendingPathComponent("bip-content-\(UUID().uuidString)", isDirectory: true)
        try FileManager.default.copyItem(at: TestContent.directory, to: copy)
        addTeardownBlock { try? FileManager.default.removeItem(at: copy) }
        return copy
    }
}
