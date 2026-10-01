import XCTest
import BipCore

/// Levels inside each game (games.json `levels`): every level stays fair, each level changes what
/// it says it does, new children start at their band's level, and play moves the level.
final class GameLevelTests: XCTestCase {
    private let rules = TestContent.rules
    static let roundsPerLevel = 300

    private struct Games {
        let content: ContentLibrary
        let course: PhonicsCourse
        let hunt: SoundHuntGame
        let pop: BubblePopGame
        let monster: FeedMonsterGame
        let count: CountTapGame
        let quick: QuickLookGame
        let buttons: SoundButtonsGame
        let builder: WordBuilderGame
        let path: BipsPathGame
    }

    private func games() throws -> Games {
        let content = try TestContent.library()
        let course = PhonicsCourse(content)
        return Games(content: content, course: course,
                     hunt: try SoundHuntGame(content: content, course: course),
                     pop: try BubblePopGame(content: content, course: course),
                     monster: try FeedMonsterGame(content: content, course: course),
                     count: try CountTapGame(content: content),
                     quick: try QuickLookGame(content: content),
                     buttons: try SoundButtonsGame(content: content, course: course),
                     builder: try WordBuilderGame(content: content),
                     path: try BipsPathGame(content: content))
    }

    /// A random child at a given game level.
    private func learner<G: RandomNumberGenerator>(_ g: Games, level: Int, allGroups: Bool = false, using rng: inout G) -> Learner {
        let group = allGroups ? g.course.groups.count : Int.random(in: 1...g.course.groups.count, using: &rng)
        let open = g.course.sounds(upToGroup: group)
        var known = Set(open.filter { _ in Bool.random(using: &rng) }.map(\.id))
        known.insert(open.randomElement(using: &rng)!.id)
        let band = Band.allCases.randomElement(using: &rng)!
        return Learner(band: band, unlockedPhonicsGroup: group, knownSoundIDs: known, gameLevel: level)
    }

    /// Plays rounds at every level of a game and hands each to `check`.
    private func everyLevel<Game: MiniGame>(of game: Game, _ g: Games, seed: UInt64, allGroups: Bool = false,
                                            check: (Game.Round, Learner, GameLevel) -> Void) {
        var rng = SeededGenerator(seed: seed)
        let steps = game.entry.levelSteps
        XCTAssertGreaterThan(steps.count, 1, "\(Game.id) should have levels")
        for index in steps.indices {
            var played = 0
            var tries = 0
            while played < Self.roundsPerLevel && tries < Self.roundsPerLevel * 20 {
                tries += 1
                let child = learner(g, level: index, allGroups: allGroups, using: &rng)
                var session = GameSession(gameID: Game.id, skin: game.skins[0], maxRounds: 8)
                guard let round = session.nextRound(of: game, for: child, using: &rng) else { continue }
                XCTAssertEqual(game.correctChoices(in: round).count, 1, "\(Game.id) level \(index + 1): not exactly one right answer")
                check(round, child, steps[index])
                played += 1
            }
            XCTAssertEqual(played, Self.roundsPerLevel, "\(Game.id) level \(index + 1) couldn't make rounds")
        }
    }

    // MARK: Each level does what it says

    func testCountAndTapCountsHigherAtHigherLevels() throws {
        let g = try games()
        everyLevel(of: g.count, g, seed: 1) { round, _, step in
            XCTAssertLessThanOrEqual(round.count, try! XCTUnwrap(step.countTo))
            XCTAssertGreaterThanOrEqual(round.count, 1)
        }
    }

    func testQuickLookShowsMoreDotsForLessTime() throws {
        let g = try games()
        everyLevel(of: g.quick, g, seed: 2) { round, _, step in
            XCTAssertLessThanOrEqual(round.count, step.countTo ?? 10)
            XCTAssertEqual(round.flashSeconds, Double(step.flashTenths ?? 20) / 10, accuracy: 0.001)
        }
        let flashes = g.quick.entry.levelSteps.compactMap(\.flashTenths)
        XCTAssertEqual(flashes, flashes.sorted(by: >), "the flash gets shorter level by level")
    }

    func testBubblePopAddsBubblesAndSpeed() throws {
        let g = try games()
        everyLevel(of: g.pop, g, seed: 3) { round, _, step in
            XCTAssertEqual(round.choices.count, step.bubbles)
            XCTAssertEqual(round.speedPercent, step.speedPercent)
        }
    }

    func testSoundHuntAndFeedTheMonsterOfferMorePictures() throws {
        let g = try games()
        everyLevel(of: g.hunt, g, seed: 4) { round, _, step in
            XCTAssertTrue((SoundHuntGame.choiceCount...(step.choices ?? 3)).contains(round.choices.count))
            XCTAssertEqual(Set(round.choices.map(\.word)).count, round.choices.count)
        }
        everyLevel(of: g.monster, g, seed: 5) { round, _, step in
            XCTAssertTrue((FeedMonsterGame.choiceCount...(step.choices ?? 3)).contains(round.choices.count))
        }
    }

    func testWordGamesUseLongerWordsAtHigherLevels() throws {
        let g = try games()
        // With every sound unlocked, each level always has its own words.
        everyLevel(of: g.buttons, g, seed: 6, allGroups: true) { round, _, step in
            XCTAssertTrue(try! XCTUnwrap(step.soundCounts).contains(round.word.soundCount), round.word.word)
        }
        everyLevel(of: g.builder, g, seed: 7, allGroups: true) { round, _, step in
            XCTAssertTrue(try! XCTUnwrap(step.soundCounts).contains(round.word.soundCount), round.word.word)
        }
    }

    func testBipsPathMovesToBiggerGrids() throws {
        let g = try games()
        everyLevel(of: g.path, g, seed: 8) { round, _, step in
            XCTAssertEqual(round.level.band, step.gridBand, "the first puzzle of a visit comes from the level's grids")
        }
    }

    func testAHarderLevelFallsBackRatherThanEndingTheVisit() throws {
        let g = try games()
        var rng = SeededGenerator(seed: 9)
        // Group 1 has no four- or five-sound words: the top level still plays, with shorter words.
        let child = Learner(band: .foundation, unlockedPhonicsGroup: 1, knownSoundIDs: ["s", "a", "t"], gameLevel: 2)
        var session = GameSession(gameID: WordBuilderGame.id, skin: g.builder.skins[0], maxRounds: 8)
        XCTAssertNotNil(session.nextRound(of: g.builder, for: child, using: &rng))
    }

    // MARK: Starting and moving

    func testNewChildrenStartAtTheirBandsLevel() throws {
        let entry = try XCTUnwrap(TestContent.library().game(id: CountTapGame.id))
        XCTAssertEqual(entry.startingLevel(for: .foundation), 0)
        XCTAssertEqual(entry.startingLevel(for: .stage1), 2, "level 3 is marked stage1")
        XCTAssertEqual(entry.startingLevel(for: .stage3), 2)
        let single = try XCTUnwrap(TestContent.library().game(id: MeetTheSoundGame.id))
        XCTAssertEqual(single.levelSteps.count, 1, "games without levels have one")
        XCTAssertEqual(single.startingLevel(for: .stage3), 0)
    }

    func testLevelsAreClamped() throws {
        let entry = try XCTUnwrap(TestContent.library().game(id: BubblePopGame.id))
        XCTAssertEqual(entry.level(-3), entry.levelSteps[0])
        XCTAssertEqual(entry.level(99), entry.levelSteps.last)
    }

    func testThreeRightInARowMovesAGameUpAndTwoMissesBack() {
        var progress = ChildProgress()
        XCTAssertEqual(progress.gameLevel(for: "count_and_tap", startingAt: 0), 0)
        for _ in 0..<3 {
            progress.recordGameAnswer(correct: true, gameID: "count_and_tap", startingAt: 0, levelCount: 4, rules: rules)
        }
        XCTAssertEqual(progress.gameLevel(for: "count_and_tap", startingAt: 0), 1)
        progress.recordGameAnswer(correct: false, gameID: "count_and_tap", startingAt: 0, levelCount: 4, rules: rules)
        let change = progress.recordGameAnswer(correct: false, gameID: "count_and_tap", startingAt: 0, levelCount: 4, rules: rules)
        XCTAssertEqual(change, .droppedBack(to: 0))
        XCTAssertEqual(progress.gameLevel(for: "quick_look", startingAt: 2), 2, "an unplayed game starts where its band does")
    }

    func testTheTopLevelIsTheLimitAndOneLevelGamesDontMove() {
        var progress = ChildProgress()
        for _ in 0..<30 {
            progress.recordGameAnswer(correct: true, gameID: "sound_hunt", startingAt: 0, levelCount: 2, rules: rules)
        }
        XCTAssertEqual(progress.gameLevel(for: "sound_hunt", startingAt: 0), 1)
        XCTAssertEqual(progress.recordGameAnswer(correct: true, gameID: "meet_the_sound", startingAt: 0, levelCount: 1, rules: rules), .none)
        XCTAssertNil(progress.gameLevels["meet_the_sound"])
    }

    func testGameLevelsSurviveSaving() throws {
        var progress = ChildProgress()
        for _ in 0..<3 {
            progress.recordGameAnswer(correct: true, gameID: "bubble_pop", startingAt: 1, levelCount: 4, rules: rules)
        }
        let again = try JSONDecoder().decode(ChildProgress.self, from: JSONEncoder().encode(progress))
        XCTAssertEqual(again.gameLevel(for: "bubble_pop", startingAt: 0), 2)
    }

    func testVisitsAreEightQuestions() throws {
        XCTAssertEqual(try TestContent.library().games.session.roundsPerSession, 8)
    }
}
