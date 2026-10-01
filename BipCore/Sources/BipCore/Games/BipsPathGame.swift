/// Bip's Path: snap arrow blocks together to walk Bip to his battery, then press Go.
/// The round offers three finished strips — the working program and two broken ones that
/// crash or miss, checked by the same interpreter the scene steps through — so there is
/// exactly one right answer. Levels come from the content, so new puzzles need no new code.
public struct BipsPathGame: MiniGame {
    public static let id = "bips_path"
    public static let choiceCount = 3

    public struct Round: GameRound {
        public let level: GridLevel
        /// Candidate programs, in screen order. Exactly one reaches the battery.
        public let choices: [[String]]
        public let answer: [String]
        public var usedItems: [String] { [level.id] }
    }

    public let entry: GameEntry
    public let skins = [
        GameSkin(id: "island", name: "Island"),
        GameSkin(id: "snow", name: "Snow"),
        GameSkin(id: "space", name: "Space"),
    ]
    private let levels: [GridLevel]

    public init(content: ContentLibrary) throws {
        entry = try content.entry(forGame: Self.id)
        levels = content.levels.levels.filter { $0.game == Self.id }
    }

    /// Broken programs: the working program with one swapped block, that no longer reaches
    /// the battery. Verified by the interpreter, not by trust.
    static func brokenPrograms<G: RandomNumberGenerator>(for level: GridLevel, count: Int, using rng: inout G) -> [[String]] {
        var out: [[String]] = []
        for _ in 0..<60 where out.count < count {
            var program = level.optimalProgram
            guard !program.isEmpty else { return out }
            let at = Int.random(in: 0..<program.count, using: &rng)
            let alternatives = level.blocks.filter { $0 != program[at] }
            guard let swap = alternatives.randomElement(using: &rng) else { continue }
            program[at] = swap
            if program != level.optimalProgram && !out.contains(program)
                && !GridWalker.reachesGoal(program: program, on: level) {
                out.append(program)
            }
        }
        return out
    }

    /// Puzzles from the level's grid band (small grids first), then any the child's band allows.
    public func makeRound<G: RandomNumberGenerator>(for learner: Learner, session: GameSession, using rng: inout G) -> Round? {
        let open = levels.filter { !session.usedItems.contains($0.id) }
        let gridBand = self.level(for: learner).gridBand
        let atLevel = gridBand.map { band in open.filter { $0.band == band } } ?? []
        let fallback = open.filter { $0.band <= max(learner.band, gridBand ?? learner.band) && !atLevel.contains($0) }
        let fresh = atLevel.shuffled(using: &rng) + fallback.shuffled(using: &rng)
        for level in fresh {
            guard GridWalker.reachesGoal(program: level.optimalProgram, on: level) else { continue }
            let wrong = Self.brokenPrograms(for: level, count: Self.choiceCount - 1, using: &rng)
            guard wrong.count == Self.choiceCount - 1 else { continue }
            return Round(level: level, choices: ([level.optimalProgram] + wrong).shuffled(using: &rng),
                         answer: level.optimalProgram)
        }
        return nil
    }

    public func isCorrect(_ choice: [String], in round: Round) -> Bool {
        choice == round.answer
    }

    public func skillID(for round: Round) -> String {
        if round.level.tier.contains("turn") { return "turns" }
        return round.level.optimalLength <= 4 ? "directions" : "programs"
    }
}
