import Foundation
import XCTest
import BipCore

/// The parent progress view and the profile rules.
final class ProgressReportTests: XCTestCase {
    private let rules = TestContent.rules
    private let today = 1_000

    private func report(_ progress: ChildProgress, band: Band = .foundation) throws -> ProgressReport {
        ProgressReport(content: try TestContent.library(), progress: progress, startingBand: band, today: today)
    }

    private func line(_ id: String, in report: ProgressReport) throws -> ProgressReport.SkillLine {
        try XCTUnwrap(report.islands.flatMap(\.skills).first { $0.id == id }, id)
    }

    func testAFreshChild() throws {
        let r = try report(ChildProgress())
        XCTAssertEqual(r.stars, 0)
        XCTAssertEqual(r.answersThisWeek, 0)
        XCTAssertEqual(r.minutesThisWeek, 0)
        XCTAssertEqual(r.islands.map(\.island), Island.allCases)
        XCTAssertEqual(try line("snd_g1", in: r).status, .ready)
        XCTAssertEqual(try line("snd_g2", in: r).status, .locked)
        XCTAssertEqual(r.phonics.count, 9)
        XCTAssertTrue(r.phonics[0].sounds.allSatisfy { $0.stage == .new })
        XCTAssertGreaterThan(r.stickersTotal, 0)
        XCTAssertTrue(r.needsPractice.isEmpty)
    }

    func testOlderChildrenSeeEarlierSkillsAsKnown() throws {
        let r = try report(ChildProgress(), band: .stage1)
        XCTAssertEqual(try line("snd_g1", in: r).status, .knownByAge)
        XCTAssertEqual(try line("snd_g5", in: r).status, .ready)
    }

    func testLearningMasteredAndReview() throws {
        var progress = ChildProgress()
        progress.markMet(soundID: "s")
        for i in 0..<10 {
            progress.recordAnswer(correct: true, skillID: "snd_g1", soundID: "s", day: today - 3 + i / 5, rules: rules)
        }
        // Mastered two days ago: the first review (after 2 days) is due today.
        let mastered = try line("snd_g1", in: report(progress))
        XCTAssertEqual(mastered.status, .reviewDue)
        XCTAssertEqual(mastered.masteredOnDay, today - 2)
        XCTAssertEqual(mastered.recentPercent, 100)
        XCTAssertEqual(try report(progress).dueForReview.map(\.id), ["snd_g1"])
        XCTAssertEqual(try line("snd_g2", in: report(progress)).status, .ready, "mastering group 1 opens group 2")

        progress.recordAnswer(correct: false, skillID: "snd_g2", soundID: nil, day: today, rules: rules)
        let learning = try line("snd_g2", in: report(progress))
        XCTAssertEqual(learning.status, .learning)
        XCTAssertEqual(learning.recentPercent, 0)
    }

    func testSkillsThatNeedMorePractice() throws {
        var progress = ChildProgress()
        for i in 0..<6 {
            progress.recordAnswer(correct: i == 0, skillID: "snd_g1", soundID: nil, day: today, rules: rules)
        }
        let r = try report(progress)
        XCTAssertEqual(r.needsPractice.map(\.id), ["snd_g1"])
        XCTAssertEqual(r.answersThisWeek, 6)
        XCTAssertEqual(r.rightThisWeek, 1)
        XCTAssertEqual(r.stars, 1)
    }

    func testOnlyThisWeeksAnswersCount() throws {
        var progress = ChildProgress()
        progress.recordAnswer(correct: true, skillID: "snd_g1", soundID: nil, day: today - 7, rules: rules)
        progress.recordAnswer(correct: true, skillID: "snd_g1", soundID: nil, day: today - 6, rules: rules)
        XCTAssertEqual(try report(progress).answersThisWeek, 1)
    }

    func testSoundStagesShow() throws {
        var progress = ChildProgress()
        progress.markMet(soundID: "a")
        let sounds = try report(progress).phonics[0].sounds
        XCTAssertEqual(sounds.map(\.grapheme), ["s", "a", "t", "p", "i", "n"])
        XCTAssertEqual(sounds[1].stage, .met)
    }

    func testRecentGamesAreNamedNewestFirstWithoutRepeats() throws {
        var progress = ChildProgress()
        for id in ["meet_the_sound", "sound_hunt", "meet_the_sound", "not_a_game"] { progress.notePlayed(gameID: id) }
        XCTAssertEqual(try report(progress).recentGames, ["Meet the Sound", "Sound Hunt"])
    }

    // MARK: Play time

    func testPlayTimeByDay() throws {
        var progress = ChildProgress()
        progress.notePlayTime(seconds: 600, on: today)
        progress.notePlayTime(seconds: 300, on: today)
        progress.notePlayTime(seconds: 1_200, on: today - 3)
        progress.notePlayTime(seconds: 999, on: today - 7)
        progress.notePlayTime(seconds: -5, on: today)
        XCTAssertEqual(progress.secondsPlayed(lastDays: 1, endingOn: today), 900)
        XCTAssertEqual(progress.secondsPlayed(lastDays: 7, endingOn: today), 2_100)
        XCTAssertEqual(progress.daysPlayed(lastDays: 7, endingOn: today), 2)
        let r = try report(progress)
        XCTAssertEqual(r.minutesToday, 15)
        XCTAssertEqual(r.minutesThisWeek, 35)
        XCTAssertEqual(r.daysPlayedThisWeek, 2)
    }

    func testOldPlayTimeIsDropped() {
        var progress = ChildProgress()
        progress.notePlayTime(seconds: 60, on: 10)
        progress.notePlayTime(seconds: 60, on: 500)
        XCTAssertEqual(progress.playSecondsByDay.keys.sorted(), [500])
    }

    func testPlayTimeSurvivesSavingAndOldSavesLoad() throws {
        var progress = ChildProgress()
        progress.notePlayTime(seconds: 120, on: today)
        let data = try JSONEncoder().encode(progress)
        XCTAssertEqual(try JSONDecoder().decode(ChildProgress.self, from: data), progress)
        let old = try JSONDecoder().decode(ChildProgress.self, from: Data(#"{"stars": 3}"#.utf8))
        XCTAssertTrue(old.playSecondsByDay.isEmpty)
        XCTAssertEqual(old.stars, 3)
    }

    func testDayNumbersTurnBackIntoDates() throws {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = try XCTUnwrap(TimeZone(identifier: "Africa/Johannesburg"))
        let date = try XCTUnwrap(calendar.date(from: DateComponents(year: 2026, month: 10, day: 1, hour: 15)))
        let day = DayNumber.of(date, calendar: calendar)
        XCTAssertEqual(DayNumber.of(DayNumber.date(for: day, calendar: calendar), calendar: calendar), day)
        XCTAssertEqual(calendar.component(.day, from: DayNumber.date(for: day, calendar: calendar)), 1)
    }

    // MARK: Profiles

    func testNamesAreTidied() {
        XCTAssertEqual(ProfileRules.cleanName("  Lerato   Mokoena "), "Lerato Mokoena")
        XCTAssertNil(ProfileRules.cleanName("   "))
        XCTAssertEqual(ProfileRules.cleanName(String(repeating: "a", count: 40))?.count, ProfileRules.maxNameLength)
    }

    func testAvatarsAndTheFourChildLimit() {
        XCTAssertEqual(ProfileRules.freeAvatar(used: []), "lion")
        XCTAssertEqual(ProfileRules.freeAvatar(used: ["lion", "penguin"]), "tortoise")
        XCTAssertEqual(ProfileRules.freeAvatar(used: ProfileRules.avatars), "lion")
        XCTAssertEqual(ProfileRules.validAvatar("dragon"), "lion")
        XCTAssertEqual(ProfileRules.validAvatar(nil), "lion")
        XCTAssertEqual(ProfileRules.validAvatar("crab"), "crab")
        XCTAssertTrue(ProfileRules.canAdd(currentCount: 3))
        XCTAssertFalse(ProfileRules.canAdd(currentCount: 4))
        XCTAssertGreaterThanOrEqual(ProfileRules.avatars.count, ProfileRules.maxChildren)
    }
}
