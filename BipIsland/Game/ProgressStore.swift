import BipCore
import Foundation
import SwiftData

/// One sound's mastery, saved on this Mac only (SwiftData). Profiles come in phase 2.
@Model
final class SkillProgress {
    @Attribute(.unique) var skillID: String
    var level: Int
    var correctStreak: Int
    var missStreak: Int

    init(skillID: String, level: Int, correctStreak: Int, missStreak: Int) {
        self.skillID = skillID
        self.level = level
        self.correctStreak = correctStreak
        self.missStreak = missStreak
    }
}

/// Loads and saves the mastery tracker. If storage fails, the game still runs (progress just isn't kept).
final class ProgressStore {
    private let context: ModelContext?

    init() {
        do {
            let support = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            let folder = support.appendingPathComponent("Bip Island", isDirectory: true)
            try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
            let configuration = ModelConfiguration(url: folder.appendingPathComponent("progress.store"))
            let container = try ModelContainer(for: SkillProgress.self, configurations: configuration)
            context = ModelContext(container)
        } catch {
            NSLog("Bip Island: progress won't be saved: %@", error.localizedDescription)
            context = nil
        }
    }

    func loadTracker() -> MasteryTracker {
        guard let context, let rows = try? context.fetch(FetchDescriptor<SkillProgress>()) else {
            return MasteryTracker()
        }
        var skills: [String: SkillMastery] = [:]
        for row in rows {
            skills[row.skillID] = SkillMastery(level: row.level, correctStreak: row.correctStreak, missStreak: row.missStreak)
        }
        return MasteryTracker(skills: skills)
    }

    func save(_ tracker: MasteryTracker) {
        guard let context else { return }
        let rows = (try? context.fetch(FetchDescriptor<SkillProgress>())) ?? []
        let existing = Dictionary(rows.map { ($0.skillID, $0) }, uniquingKeysWith: { first, _ in first })
        for (id, mastery) in tracker.skills {
            if let row = existing[id] {
                row.level = mastery.level
                row.correctStreak = mastery.correctStreak
                row.missStreak = mastery.missStreak
            } else {
                context.insert(SkillProgress(skillID: id, level: mastery.level,
                                             correctStreak: mastery.correctStreak, missStreak: mastery.missStreak))
            }
        }
        do {
            try context.save()
        } catch {
            NSLog("Bip Island: couldn't save progress: %@", error.localizedDescription)
        }
    }
}
