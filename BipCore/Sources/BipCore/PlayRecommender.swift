/// Bip's suggestion of what to play next, across all four islands. The child always picks
/// freely — one island glows, and Bip nudges towards another island when one island
/// dominates recent play. Never forced.
public struct IslandSuggestion: Equatable, Sendable {
    public let gameID: String
    public let island: Island
    /// True when Bip is steering away from an over-played island ("How about some numbers?").
    public let isNudge: Bool
}

/// Picks the glowing island. Rules, in order: a skill due for review comes first; otherwise
/// the island played least lately; and when the last three visits were all one island, Bip
/// suggests another one instead.
public struct PlayRecommender: Sendable {
    /// How many same-island visits in a row trigger Bip's balancing nudge.
    public static let sameIslandVisitsBeforeNudge = 3

    private let content: ContentLibrary

    public init(content: ContentLibrary) {
        self.content = content
    }

    /// Games the child can open: at least one listed skill is unlocked.
    func unlockedGames(progress: ChildProgress, startingBand: Band) -> [GameEntry] {
        content.games.games.filter { entry in
            entry.skills.contains { id in
                guard let skill = content.skill(id: id) else { return false }
                return progress.isUnlocked(skill, startingBand: startingBand) { content.skill(id: $0) }
            }
        }
    }

    public func suggest<G: RandomNumberGenerator>(progress: ChildProgress, startingBand: Band, day: Int,
                                                  rules: MasteryRules, using rng: inout G) -> IslandSuggestion? {
        let open = unlockedGames(progress: progress, startingBand: startingBand)
        guard !open.isEmpty else { return nil }

        // 1. Reviews first: a mastered skill that is due, through one of its games.
        let due = open.filter { entry in
            entry.skills.contains { progress.isDueForReview($0, on: day, rules: rules) }
        }
        if let review = due.randomElement(using: &rng) {
            return IslandSuggestion(gameID: review.id, island: review.island, isNudge: false)
        }

        // 2. The island played least lately (never played counts as longest ago). When the last
        // three visits were all one island, suggesting another one is Bip's balancing nudge.
        func lastPlayed(_ island: Island) -> Int {
            progress.recentGames.lastIndex { content.game(id: $0)?.island == island } ?? -1
        }
        let rest = Island.allCases.sorted { lastPlayed($0) < lastPlayed($1) }
        guard let pick = rest.first,
              let game = open.filter({ $0.island == pick }).randomElement(using: &rng) else { return nil }

        let recent = progress.recentGames.suffix(Self.sameIslandVisitsBeforeNudge)
        let dominated = recent.count == Self.sameIslandVisitsBeforeNudge
            ? recent.compactMap({ content.game(id: $0)?.island }).first
            : nil
        let dominatedAll = dominated.map { dom in recent.allSatisfy({ content.game(id: $0)?.island == dom }) } ?? false
        let isNudge = dominatedAll && pick != dominated
        return IslandSuggestion(gameID: game.id, island: pick, isNudge: isNudge)
    }
}
