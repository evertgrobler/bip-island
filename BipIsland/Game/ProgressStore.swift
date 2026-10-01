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
    /// The animal picture the child picks themselves by (ProfileRules.avatars). Nil in older saves.
    var avatar: String?
    var sortOrder: Int
    var createdAt: Date
    var progressData: Data

    init(id: UUID = UUID(), name: String, age: Int?, avatar: String?, sortOrder: Int, progress: ChildProgress) {
        self.id = id
        self.name = name
        self.age = age
        self.avatar = avatar
        self.sortOrder = sortOrder
        createdAt = Date()
        progressData = (try? JSONEncoder().encode(progress)) ?? Data()
    }
}

/// One child as the picker and the parent area show them.
struct ChildSummary: Identifiable, Equatable {
    let id: UUID
    var name: String
    var age: Int?
    var avatar: String
}

/// Loads and saves each child's progress on this Mac only (SwiftData). If storage fails, the game
/// still runs; progress just isn't kept.
final class ProgressStore {
    private static let lastChildKey = "bip.lastChildID"
    private static let breakKey = "bip.breakState"

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

    // MARK: Children

    /// Every child, in the order they were added. On first launch there is one, "Player 1",
    /// with any phase 1 progress moved into it.
    func children() -> [ChildSummary] {
        guard context != nil else { return [ChildSummary(id: Self.offlineID, name: "Player 1", age: nil, avatar: ProfileRules.avatars[0])] }
        var rows = profiles()
        if rows.isEmpty {
            insertChild(name: "Player 1", age: nil, avatar: ProfileRules.avatars[0], progress: importPhaseOneProgress())
            rows = profiles()
        }
        return rows.map(summary)
    }

    /// The child who played last (or the first one).
    func lastChildID() -> UUID? {
        let all = children()
        if let raw = UserDefaults.standard.string(forKey: Self.lastChildKey), let id = UUID(uuidString: raw),
           all.contains(where: { $0.id == id }) {
            return id
        }
        return all.first?.id
    }

    func setLastChild(_ id: UUID) {
        UserDefaults.standard.set(id.uuidString, forKey: Self.lastChildKey)
    }

    /// Adds a child (at most `ProfileRules.maxChildren`). Returns the new id, or nil if it couldn't.
    @discardableResult
    func addChild(name: String, age: Int?, avatar: String?) -> UUID? {
        guard context != nil, let clean = ProfileRules.cleanName(name) else { return nil }
        let existing = children()
        guard ProfileRules.canAdd(currentCount: existing.count) else { return nil }
        let picture = avatar.map { ProfileRules.validAvatar($0) } ?? ProfileRules.freeAvatar(used: existing.map(\.avatar))
        return insertChild(name: clean, age: age, avatar: picture, progress: ChildProgress())
    }

    /// Changes a child's name, age or picture. An empty name is ignored.
    func update(_ child: ChildSummary) {
        guard let row = profile(child.id) else { return }
        if let clean = ProfileRules.cleanName(child.name) { row.name = clean }
        row.age = child.age.map { min(max($0, ProfileRules.ages.lowerBound), ProfileRules.ages.upperBound) }
        row.avatar = ProfileRules.validAvatar(child.avatar)
        commit()
    }

    /// Removes a child and their progress. The last child can't be removed.
    func delete(_ id: UUID) {
        guard let context, children().count > 1, let row = profile(id) else { return }
        context.delete(row)
        commit()
    }

    func progress(for childID: UUID) -> ChildProgress {
        guard let row = profile(childID) else { return ChildProgress() }
        return decode(row.progressData, childID: childID)
    }

    func save(_ progress: ChildProgress, for childID: UUID?) {
        guard let childID, let child = profile(childID) else { return }
        do {
            child.progressData = try JSONEncoder().encode(progress)
        } catch {
            NSLog("Bip Island: couldn't encode progress: %@", error.localizedDescription)
            return
        }
        commit()
    }

    // MARK: The play-time break (one for the whole Mac)

    /// The break is shared by everyone on this Mac, so switching to another profile can't skip
    /// it. On first use it carries over the break saved in a child's progress by older versions.
    func loadBreak(carryingOver legacy: BreakState?) -> BreakState? {
        if let data = UserDefaults.standard.data(forKey: Self.breakKey),
           let state = try? JSONDecoder().decode(BreakState.self, from: data) {
            return state
        }
        return legacy
    }

    func saveBreak(_ state: BreakState) {
        if let data = try? JSONEncoder().encode(state) {
            UserDefaults.standard.set(data, forKey: Self.breakKey)
        }
    }

    // MARK: Helpers

    /// Used when storage isn't available, so the game still has one child to play as.
    static let offlineID = UUID(uuidString: "00000000-0000-0000-0000-000000000001")!

    private func profiles() -> [ChildProfile] {
        guard let context else { return [] }
        let descriptor = FetchDescriptor<ChildProfile>(sortBy: [SortDescriptor(\.sortOrder), SortDescriptor(\.createdAt)])
        return (try? context.fetch(descriptor)) ?? []
    }

    private func profile(_ id: UUID) -> ChildProfile? {
        guard let context else { return nil }
        let descriptor = FetchDescriptor<ChildProfile>(predicate: #Predicate<ChildProfile> { $0.id == id })
        return (try? context.fetch(descriptor))?.first
    }

    private func summary(_ row: ChildProfile) -> ChildSummary {
        ChildSummary(id: row.id, name: row.name, age: row.age, avatar: ProfileRules.validAvatar(row.avatar))
    }

    @discardableResult
    private func insertChild(name: String, age: Int?, avatar: String, progress: ChildProgress) -> UUID? {
        guard let context else { return nil }
        let order = (profiles().map(\.sortOrder).max() ?? -1) + 1
        let child = ChildProfile(name: name, age: age, avatar: avatar, sortOrder: order, progress: progress)
        context.insert(child)
        commit()
        return child.id
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
