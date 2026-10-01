import BipCore
import Foundation
import SwiftData

/// Phase 1 progress: one row per sound, before profiles existed. Kept so an older install's progress
/// can be read and moved into the first child's profile. Nothing new is written here.
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

/// A child on this Mac (up to 4). Their progress is stored as JSON (`ChildProgress` in BipCore),
/// so new kinds of progress can be added later without a database migration.
@Model
final class ChildProfile {
    @Attribute(.unique) var id: UUID
    var name: String
    /// Sets the starting band. Nil until a parent enters it (then the youngest band is used).
    var age: Int?
    var sortOrder: Int
    var createdAt: Date
    var progressData: Data

    init(id: UUID = UUID(), name: String, age: Int?, sortOrder: Int, progress: ChildProgress) {
        self.id = id
        self.name = name
        self.age = age
        self.sortOrder = sortOrder
        createdAt = Date()
        progressData = (try? JSONEncoder().encode(progress)) ?? Data()
    }
}

/// Loads and saves each child's progress on this Mac only (SwiftData). If storage fails, the game
/// still runs; progress just isn't kept.
final class ProgressStore {
    static let maxChildren = 4

    private let context: ModelContext?
    private let folder: URL

    init() {
        let support = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
        folder = support.appendingPathComponent("Bip Island", isDirectory: true)
        do {
            try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
            let configuration = ModelConfiguration(url: folder.appendingPathComponent("progress.store"))
            let container = try ModelContainer(for: SkillProgress.self, ChildProfile.self, configurations: configuration)
            context = ModelContext(container)
        } catch {
            NSLog("Bip Island: progress won't be saved: %@", error.localizedDescription)
            context = nil
        }
    }

    /// The child who is playing. Profiles (picking a child) arrive in phase 2; until then this is the
    /// first profile, made on first launch with any phase 1 progress moved into it.
    func currentChild() -> (id: UUID?, name: String, age: Int?, progress: ChildProgress) {
        guard let context else { return (nil, "Player 1", nil, ChildProgress()) }
        let descriptor = FetchDescriptor<ChildProfile>(sortBy: [SortDescriptor(\.sortOrder), SortDescriptor(\.createdAt)])
        if let child = (try? context.fetch(descriptor))?.first {
            return (child.id, child.name, child.age, decode(child.progressData, childID: child.id))
        }
        let child = ChildProfile(name: "Player 1", age: nil, sortOrder: 0, progress: importPhaseOneProgress())
        context.insert(child)
        commit()
        return (child.id, child.name, child.age, decode(child.progressData, childID: child.id))
    }

    func save(_ progress: ChildProgress, for childID: UUID?) {
        guard let context, let childID else { return }
        let descriptor = FetchDescriptor<ChildProfile>(predicate: #Predicate<ChildProfile> { $0.id == childID })
        guard let child = (try? context.fetch(descriptor))?.first else { return }
        do {
            child.progressData = try JSONEncoder().encode(progress)
        } catch {
            NSLog("Bip Island: couldn't encode progress: %@", error.localizedDescription)
            return
        }
        commit()
    }

    private func decode(_ data: Data, childID: UUID) -> ChildProgress {
        do {
            return try JSONDecoder().decode(ChildProgress.self, from: data)
        } catch {
            // Never throw a child's progress away silently: keep a copy next to the store before
            // anything new is saved over it, so it can be recovered by hand.
            let backup = folder.appendingPathComponent("unreadable-progress-\(childID.uuidString)-\(Int(Date().timeIntervalSince1970)).json")
            try? data.write(to: backup)
            NSLog("Bip Island: couldn't read saved progress (copy kept at %@): %@", backup.path, error.localizedDescription)
            return ChildProgress()
        }
    }

    /// Phase 1 kept one row per sound (s, a, t, p, i, n): bring those levels into the new profile.
    private func importPhaseOneProgress() -> ChildProgress {
        guard let context, let rows = try? context.fetch(FetchDescriptor<SkillProgress>()), !rows.isEmpty else {
            return ChildProgress()
        }
        var levels: [String: SkillMastery] = [:]
        for row in rows {
            levels[row.skillID] = SkillMastery(level: row.level, correctStreak: row.correctStreak, missStreak: row.missStreak)
        }
        NSLog("Bip Island: moved phase 1 progress for %d sounds into the first profile", rows.count)
        return ChildProgress(sounds: MasteryTracker(skills: levels))
    }

    private func commit() {
        do {
            try context?.save()
        } catch {
            NSLog("Bip Island: couldn't save progress: %@", error.localizedDescription)
        }
    }
}
