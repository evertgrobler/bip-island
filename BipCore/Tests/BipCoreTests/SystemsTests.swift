import Foundation
import XCTest
import BipCore

/// Bip's recommendations, play-time breaks and the sticker book (docs/GAMES.md).
final class SystemsTests: XCTestCase {
    private func library() throws -> ContentLibrary { try TestContent.library() }
    private var rules: MasteryRules { TestContent.rules }

    private func master(_ progress: inout ChildProgress, skill: String, days: [Int]) {
        for day in days {
            for _ in 0..<5 {
                progress.recordAnswer(correct: true, skillID: skill, soundID: nil, day: day, rules: rules)
            }
        }
    }

    // MARK: Recommendations

    func testReviewComesFirst() throws {
        let content = try library()
        var progress = ChildProgress()
        master(&progress, skill: "snd_g1", days: [0, 1])
        XCTAssertTrue(progress.isMastered("snd_g1"))
        var rng = SeededGenerator(seed: 40)
        let recommender = PlayRecommender(content: content)
        // Day 2: the first review is due (reviewAfterDays starts at 2).
        let suggestion = try XCTUnwrap(recommender.suggest(progress: progress, startingBand: .foundation, day: 2,
                                                           rules: rules, using: &rng))
        let game = try XCTUnwrap(content.game(id: suggestion.gameID))
        XCTAssertTrue(game.skills.contains("snd_g1"), "review should practise the due skill, not \(suggestion.gameID)")
        XCTAssertFalse(suggestion.isNudge)
    }

    func testSuggestsLeastPlayedIsland() throws {
        let content = try library()
        var progress = ChildProgress()
        progress.notePlayed(gameID: "meet_the_sound")
        progress.notePlayed(gameID: "sound_hunt")
        var rng = SeededGenerator(seed: 41)
        let suggestion = try XCTUnwrap(PlayRecommender(content: content)
            .suggest(progress: progress, startingBand: .foundation, day: 0, rules: rules, using: &rng))
        XCTAssertEqual(suggestion.island, .numbers, "letters was just played; numbers was never played")
        XCTAssertFalse(suggestion.isNudge, "only two visits: no nudge yet")
    }

    func testNudgesAwayAfterThreeSameIslandVisits() throws {
        let content = try library()
        var progress = ChildProgress()
        progress.notePlayed(gameID: "meet_the_sound")
        progress.notePlayed(gameID: "sound_hunt")
        progress.notePlayed(gameID: "bubble_pop")
        var rng = SeededGenerator(seed: 42)
        let suggestion = try XCTUnwrap(PlayRecommender(content: content)
            .suggest(progress: progress, startingBand: .foundation, day: 0, rules: rules, using: &rng))
        XCTAssertNotEqual(suggestion.island, .letters)
        XCTAssertTrue(suggestion.isNudge, "three letters visits in a row: Bip should steer elsewhere")
    }

    func testNeverSuggestsLockedGames() throws {
        let content = try library()
        let progress = ChildProgress()
        var rng = SeededGenerator(seed: 43)
        let recommender = PlayRecommender(content: content)
        for _ in 0..<50 {
            let suggestion = try XCTUnwrap(recommender.suggest(progress: progress, startingBand: .foundation, day: 0,
                                                               rules: rules, using: &rng))
            let game = try XCTUnwrap(content.game(id: suggestion.gameID))
            let unlocked = game.skills.contains { id in
                guard let skill = content.skill(id: id) else { return false }
                return progress.isUnlocked(skill, startingBand: .foundation) { content.skill(id: $0) }
            }
            XCTAssertTrue(unlocked, "\(suggestion.gameID) is still locked")
        }
    }

    // MARK: Play-time breaks

    private func settings(play: Int = 20, rest: Int = 20, dailyMax: Int? = nil) -> BreakSettings {
        BreakSettings(playMinutes: play, breakMinutes: rest, dailyMaxMinutes: dailyMax)
    }

    func testPlayThenBreakThenResume() throws {
        var state = BreakState(dayStamp: 100)
        let now = Date(timeIntervalSinceReferenceDate: 1_000_000)
        XCTAssertEqual(PlayBreaks.advance(state: &state, elapsed: 19 * 60, now: now, day: 100, settings: settings()), .playing)
        XCTAssertEqual(PlayBreaks.advance(state: &state, elapsed: 60, now: now, day: 100, settings: settings()), .breakTime)
        // Mid-break: still charging, even with no new play.
        let mid = now.addingTimeInterval(10 * 60)
        XCTAssertEqual(PlayBreaks.advance(state: &state, elapsed: 0, now: mid, day: 100, settings: settings()), .breakTime)
        // After the break: playing again with a clean slate.
        let later = now.addingTimeInterval(20 * 60 + 1)
        XCTAssertEqual(PlayBreaks.advance(state: &state, elapsed: 0, now: later, day: 100, settings: settings()), .playing)
        XCTAssertEqual(state.playedSeconds, 0)
        XCTAssertNil(state.breakEndsAt)
    }

    func testBreakSurvivesQuitting() throws {
        var state = BreakState(dayStamp: 100)
        let now = Date(timeIntervalSinceReferenceDate: 2_000_000)
        XCTAssertEqual(PlayBreaks.advance(state: &state, elapsed: 20 * 60, now: now, day: 100, settings: settings()), .breakTime)
        // The app quits and reopens: the saved state still says charging.
        let saved = try JSONEncoder().encode(state)
        var loaded = try JSONDecoder().decode(BreakState.self, from: saved)
        let reopened = now.addingTimeInterval(5 * 60)
        XCTAssertEqual(PlayBreaks.advance(state: &loaded, elapsed: 0, now: reopened, day: 100, settings: settings()), .breakTime)
    }

    func testDailyMaximum() throws {
        var state = BreakState(dayStamp: 100)
        let now = Date(timeIntervalSinceReferenceDate: 3_000_000)
        // Short breaks (1 min) so the day fills through several play sessions.
        let short = settings(play: 20, rest: 1, dailyMax: 30)
        XCTAssertEqual(PlayBreaks.advance(state: &state, elapsed: 20 * 60, now: now, day: 100, settings: short), .breakTime)
        let afterBreak = now.addingTimeInterval(61)
        XCTAssertEqual(PlayBreaks.advance(state: &state, elapsed: 10 * 60, now: afterBreak, day: 100, settings: short), .dayDone)
        // A new day starts clean.
        XCTAssertEqual(PlayBreaks.advance(state: &state, elapsed: 0, now: afterBreak, day: 101, settings: short), .playing)
        XCTAssertEqual(state.playedTodaySeconds, 0)
    }

    func testParentEndsBreakEarly() throws {
        var state = BreakState(dayStamp: 100)
        let now = Date(timeIntervalSinceReferenceDate: 4_000_000)
        XCTAssertEqual(PlayBreaks.advance(state: &state, elapsed: 20 * 60, now: now, day: 100, settings: settings()), .breakTime)
        PlayBreaks.endBreakEarly(state: &state)
        XCTAssertEqual(PlayBreaks.advance(state: &state, elapsed: 0, now: now, day: 100, settings: settings()), .playing)
    }

    // MARK: Stickers and the mystery box

    func testStarsAccrueAndPagesFill() throws {
        var progress = ChildProgress()
        for _ in 0..<9 {
            progress.recordAnswer(correct: true, skillID: "count_10", soundID: nil, day: 0, rules: rules)
        }
        progress.recordAnswer(correct: false, skillID: "count_10", soundID: nil, day: 0, rules: rules)
        XCTAssertEqual(progress.stars, 9, "only right answers earn stars")
        XCTAssertEqual(StickerBook.starsTowardsNextPage(progress), 9)
        progress.recordAnswer(correct: true, skillID: "count_10", soundID: nil, day: 0, rules: rules)
        XCTAssertEqual(StickerBook.starsTowardsNextPage(progress), 0, "ten stars fill a page")
    }

    func testMysteryBoxOpensOnceADay() throws {
        var progress = ChildProgress()
        XCTAssertTrue(progress.claimMysteryBox(on: 7))
        XCTAssertEqual(progress.stars, ChildProgress.mysteryBonusStars)
        XCTAssertFalse(progress.claimMysteryBox(on: 7), "already opened today")
        XCTAssertTrue(progress.claimMysteryBox(on: 8))
        XCTAssertEqual(progress.stars, 2 * ChildProgress.mysteryBonusStars)
    }

    func testStickerBookTracksMastery() throws {
        let content = try library()
        XCTAssertEqual(StickerBook.allStickers(content: content).count, 63 + 55)
        var progress = ChildProgress()
        XCTAssertTrue(StickerBook.earned(progress: progress, content: content).isEmpty)
        XCTAssertEqual(StickerBook.missing(progress: progress, content: content).count, 63 + 55)
        master(&progress, skill: "count_10", days: [0, 1])
        let earned = StickerBook.earned(progress: progress, content: content)
        XCTAssertTrue(earned.contains("skill_count_10"))
        XCTAssertFalse(earned.contains("skill_count_20"), "not mastered yet")
    }

    func testOldSavesStillLoad() throws {
        // Version 1 had no stars, mystery box or breaks: they decode as empty.
        let old = #"{"version":1,"sounds":{"skills":{}},"skills":{},"recentGames":[]}"#.data(using: .utf8)!
        let loaded = try JSONDecoder().decode(ChildProgress.self, from: old)
        XCTAssertEqual(loaded.stars, 0)
        XCTAssertNil(loaded.lastMysteryDay)
        XCTAssertNil(loaded.breaks)
        XCTAssertEqual(loaded.recentGames, [])
    }
}
