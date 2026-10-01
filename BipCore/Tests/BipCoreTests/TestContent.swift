import Foundation
import BipCore

/// The real Content/ folder from the repo, loaded once for all tests.
enum TestContent {
    static let repoRoot = URL(fileURLWithPath: #filePath)
        .deletingLastPathComponent() // BipCoreTests
        .deletingLastPathComponent() // Tests
        .deletingLastPathComponent() // BipCore
        .deletingLastPathComponent() // repo root

    static let directory = repoRoot.appendingPathComponent("Content", isDirectory: true)

    private static let cached = Result { try ContentLibrary(directory: directory) }

    static func library() throws -> ContentLibrary {
        try cached.get()
    }

    /// Rules for tests that don't depend on the content's numbers.
    static let rules = MasteryRules(correctInARowToMoveUp: 3, missesInARowToDropBack: 2, masteredWindow: 10,
                                    masteredPercent: 80, masteredDistinctDays: 2, reviewAfterDays: [2, 5, 14])
}
