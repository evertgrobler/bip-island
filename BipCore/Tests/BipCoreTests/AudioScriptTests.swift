import XCTest
import BipCore

/// Keeps audio/script.csv (the voice recording script) in step with the clips the game plays.
final class AudioScriptTests: XCTestCase {
    private struct Row { let file: String; let text: String; let notes: String }

    private func loadScript() throws -> [Row] {
        let repoRoot = URL(fileURLWithPath: #filePath)
            .deletingLastPathComponent() // BipCoreTests
            .deletingLastPathComponent() // Tests
            .deletingLastPathComponent() // BipCore
            .deletingLastPathComponent() // repo root
        let csv = try String(contentsOf: repoRoot.appendingPathComponent("audio/script.csv"), encoding: .utf8)
        let records = Self.parseCSV(csv)
        let header = try XCTUnwrap(records.first)
        XCTAssertEqual(Array(header.prefix(3)), ["file", "text", "notes"])
        return records.dropFirst().filter { !$0.allSatisfy(\.isEmpty) }.map {
            Row(file: $0[0], text: $0.count > 1 ? $0[1] : "", notes: $0.count > 2 ? $0[2] : "")
        }
    }

    func testScriptListsExactlyTheClipsTheGameUses() throws {
        let rows = try loadScript()
        let files = rows.map { $0.file.replacingOccurrences(of: ".m4a", with: "") }
        XCTAssertEqual(files.count, Set(files).count, "duplicate rows in audio/script.csv")
        let missing = Set(AudioCatalogue.allClips).subtracting(files)
        let extra = Set(files).subtracting(AudioCatalogue.allClips)
        XCTAssertTrue(missing.isEmpty, "add to audio/script.csv: \(missing.sorted())")
        XCTAssertTrue(extra.isEmpty, "not used by the game yet: \(extra.sorted())")
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
