import XCTest
import BipCore

/// Keeps the voice script in step with the game: audio/script.csv holds instructions, praise and hints;
/// Content/asset_manifest.json holds every sound and word clip the content needs.
final class AudioScriptTests: XCTestCase {
    private struct Row { let file: String; let text: String; let notes: String }

    private func loadScript() throws -> [Row] {
        let csv = try String(contentsOf: TestContent.repoRoot.appendingPathComponent("audio/script.csv"), encoding: .utf8)
        let records = Self.parseCSV(csv)
        let header = try XCTUnwrap(records.first)
        XCTAssertEqual(Array(header.prefix(3)), ["file", "text", "notes"])
        return records.dropFirst().filter { !$0.allSatisfy(\.isEmpty) }.map {
            Row(file: $0[0], text: $0.count > 1 ? $0[1] : "", notes: $0.count > 2 ? $0[2] : "")
        }
    }

    func testScriptListsEveryInstructionPraiseAndHint() throws {
        let files = try loadScript().map { $0.file.replacingOccurrences(of: ".m4a", with: "") }
        XCTAssertEqual(files.count, Set(files).count, "duplicate rows in audio/script.csv")
        let missing = Set(AudioCatalogue.scriptedClips).subtracting(files)
        XCTAssertTrue(missing.isEmpty, "add to audio/script.csv: \(missing.sorted())")
    }

    /// Every other row must be a clip the content needs (e.g. hand-written notes for snd_s),
    /// so the script never lists clips nothing plays.
    func testEveryOtherScriptRowIsInTheManifest() throws {
        let content = try TestContent.library()
        let scripted = Set(AudioCatalogue.scriptedClips)
        for row in try loadScript() {
            let name = row.file.replacingOccurrences(of: ".m4a", with: "")
            XCTAssertTrue(scripted.contains(name) || content.audioIDs.contains(name), "\(row.file) isn't used by the game or the content")
        }
    }

    /// The Letters games only play clips the manifest lists, so the narrator script covers them.
    func testEveryClipTheLettersGamesCanPlayIsInTheManifest() throws {
        let content = try TestContent.library()
        let course = PhonicsCourse(content)
        let hunt = try SoundHuntGame(content: content, course: course)
        for sound in course.allSounds {
            XCTAssertTrue(content.audioIDs.contains(sound.soundClip), sound.soundClip)
            XCTAssertTrue(content.audioIDs.contains(sound.wordClip), sound.wordClip)
        }
        for picture in hunt.pictures {
            XCTAssertTrue(content.audioIDs.contains(picture.audio), picture.audio)
            XCTAssertTrue(content.pictureIDs.contains(picture.picture), picture.picture)
        }
    }

    func testEveryManifestClipFollowsTheNamingContract() throws {
        for clip in try TestContent.library().manifest.audio {
            XCTAssertTrue(AudioCatalogue.followsNamingContract(clip.id), clip.id)
            XCTAssertFalse(clip.text.trimmingCharacters(in: .whitespaces).isEmpty, "\(clip.id) has no text")
        }
    }

    func testEveryRowFollowsTheNamingContract() throws {
        for row in try loadScript() {
            XCTAssertTrue(row.file.hasSuffix(".m4a"), row.file)
            XCTAssertTrue(AudioCatalogue.followsNamingContract(String(row.file.dropLast(4))), row.file)
            XCTAssertFalse(row.text.trimmingCharacters(in: .whitespaces).isEmpty, "\(row.file) has no text")
        }
    }

    func testEverySoundClipHasPronunciationNotes() throws {
        for row in try loadScript() where row.file.hasPrefix("snd_") {
            XCTAssertFalse(row.notes.isEmpty, "\(row.file) needs pronunciation notes")
        }
    }

    func testNamingContract() {
        XCTAssertTrue(AudioCatalogue.followsNamingContract("snd_sh"))
        XCTAssertTrue(AudioCatalogue.followsNamingContract("vo_find_the_sound"))
        XCTAssertTrue(AudioCatalogue.followsNamingContract("praise_07"))
        XCTAssertTrue(AudioCatalogue.followsNamingContract("name_s"))
        XCTAssertTrue(AudioCatalogue.followsNamingContract("money_r5"))
        XCTAssertFalse(AudioCatalogue.followsNamingContract("praise_7"))
        XCTAssertFalse(AudioCatalogue.followsNamingContract("Snd_S"))
        XCTAssertFalse(AudioCatalogue.followsNamingContract("sound_s"))
    }

    /// Minimal CSV reader: commas, double-quoted fields, "" escapes.
    static func parseCSV(_ text: String) -> [[String]] {
        var records: [[String]] = []
        var record: [String] = []
        var field = ""
        var inQuotes = false
        var chars = Array(text.replacingOccurrences(of: "\r\n", with: "\n"))[...]
        while let c = chars.popFirst() {
            if inQuotes {
                if c == "\"" {
                    if chars.first == "\"" { field.append("\""); chars.removeFirst() } else { inQuotes = false }
                } else {
                    field.append(c)
                }
            } else if c == "\"" {
                inQuotes = true
            } else if c == "," {
                record.append(field); field = ""
            } else if c == "\n" {
                record.append(field); records.append(record); record = []; field = ""
            } else {
                field.append(c)
            }
        }
        if !field.isEmpty || !record.isEmpty { record.append(field); records.append(record) }
        return records
    }
}
