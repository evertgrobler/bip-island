import XCTest
import BipCore

/// Per-child, per-skill progress: mastery (80% of the last 10, on 2 different days), reviews after
/// 2, 5 and 14 days, unlocking by prerequisites, and saved progress that always loads.
final class ProgressTests: XCTestCase {
    private let rules = TestContent.rules

    // MARK: Mastery

    func testTenRightOnOneDayIsNotMastery() {
        var record = SkillRecord()
        for _ in 0..<10 { record.record(correct: true, on: 100, rules: rules) }
        XCTAssertFalse(record.isMastered, "one lucky session doesn't count")
    }

    func testTenRightOverTwoDaysIsMastery() {
        var record = SkillRecord()
        for _ in 0..<5 { record.record(correct: true, on: 100, rules: rules) }
        for i in 0..<5 {
            let newlyMastered = record.record(correct: true, on: 101, rules: rules)
            XCTAssertEqual(newlyMastered, i == 4, "mastered on the tenth answer")
        }
        XCTAssertTrue(record.isMastered)
        XCTAssertEqual(record.masteredOnDay, 101)
    }

    func testEightOutOfTenIsEnoughButSevenIsNot() {
        var eight = SkillRecord()
        for i in 0..<10 { eight.record(correct: i >= 2, on: 100 + i / 5, rules: rules) }
        XCTAssertTrue(eight.isMastered)

        var seven = SkillRecord()
        for i in 0..<10 { seven.record(correct: i >= 3, on: 100 + i / 5, rules: rules) }
        XCTAssertFalse(seven.isMastered)
    }

    func testFewerThanTenAnswersIsNeverMastery() {
        var record = SkillRecord()
        for i in 0..<9 { record.record(correct: true, on: 100 + i, rules: rules) }
        XCTAssertFalse(record.isMastered)
    }

    func testOnlyTheLastTenAnswersCount() {
        var record = SkillRecord()
        for _ in 0..<10 { record.record(correct: false, on: 100, rules: rules) }
        XCTAssertFalse(record.isMastered)
        for i in 0..<10 { record.record(correct: true, on: 101 + i / 5, rules: rules) }
        XCTAssertTrue(record.isMastered, "early mistakes are forgotten once there are ten newer answers")
        XCTAssertEqual(record.totalAttempts, 20)
    }

    func testMasteryStaysOnceEarned() {
        var record = masteredRecord(on: 100)
        for _ in 0..<10 { record.record(correct: false, on: 101, rules: rules) }
        XCTAssertTrue(record.isMastered)
    }

    // MARK: Review

    func testMasteredSkillsComeBackAfterTwoFiveAndFourteenDays() {
        var record = masteredRecord(on: 100)
        XCTAssertFalse(record.isDueForReview(on: 101, rules: rules))
        XCTAssertTrue(record.isDueForReview(on: 102, rules: rules), "2 days after mastery")

        record.record(correct: true, on: 102, rules: rules)
        XCTAssertEqual(record.reviewsDone, 1)
        XCTAssertFalse(record.isDueForReview(on: 106, rules: rules))
        XCTAssertTrue(record.isDueForReview(on: 107, rules: rules), "5 days after the first review")

        record.record(correct: true, on: 107, rules: rules)
        XCTAssertFalse(record.isDueForReview(on: 120, rules: rules))
        XCTAssertTrue(record.isDueForReview(on: 121, rules: rules), "14 days after the second review")

        record.record(correct: true, on: 121, rules: rules)
        XCTAssertEqual(record.reviewsDone, 3)
        XCTAssertFalse(record.isDueForReview(on: 500, rules: rules), "no more reviews after the last one")
    }

    func testUnmasteredSkillsAreNeverDueForReview() {
        var record = SkillRecord()
        record.record(correct: true, on: 100, rules: rules)
        XCTAssertFalse(record.isDueForReview(on: 200, rules: rules))
    }

    // MARK: Unlocking by the skill map

    func testPhonicsGroupsUnlockInOrder() throws {
        let content = try TestContent.library()
        let g1 = try XCTUnwrap(content.skill(id: "snd_g1"))
        let g2 = try XCTUnwrap(content.skill(id: "snd_g2"))
        var progress = ChildProgress()
        XCTAssertTrue(progress.isUnlocked(g1, startingBand: .foundation, lookup: content.skill(id:)))
        XCTAssertFalse(progress.isUnlocked(g2, startingBand: .foundation, lookup: content.skill(id:)))

        master("snd_g1", in: &progress)
        XCTAssertTrue(progress.isUnlocked(g2, startingBand: .foundation, lookup: content.skill(id:)))
    }

    func testOlderChildrenStartAtTheirBand() throws {
        let content = try TestContent.library()
        let progress = ChildProgress()
        let g4 = try XCTUnwrap(content.skill(id: "snd_g4"))
        let g5 = try XCTUnwrap(content.skill(id: "snd_g5"))
        let g6 = try XCTUnwrap(content.skill(id: "snd_g6"))
        XCTAssertTrue(progress.isTreatedAsKnown(g4, startingBand: .stage1), "foundation sounds count as known for a 6-year-old")
        XCTAssertTrue(progress.isUnlocked(g5, startingBand: .stage1, lookup: content.skill(id:)))
        XCTAssertFalse(progress.isUnlocked(g6, startingBand: .stage1, lookup: content.skill(id:)), "but stage 1 still goes in order")
        XCTAssertFalse(progress.isUnlocked(g5, startingBand: .foundation, lookup: content.skill(id:)))
    }

    func testAnUnknownPrerequisiteNeverUnlocks() throws {
        let skill = try JSONDecoder().decode(Skill.self, from: Data("""
        {"id": "x", "island": "letters", "band": "foundation", "name": "X", "objectives": [], "prerequisites": ["nope"]}
        """.utf8))
        XCTAssertFalse(ChildProgress().isUnlocked(skill, startingBand: .stage3, lookup: { _ in nil }))
    }

    // MARK: Recording answers

    func testAnAnswerCountsForTheSkillAndTheSound() {
        var progress = ChildProgress()
        progress.markMet(soundID: "s")
        var change = MasteryChange.none
        for _ in 0..<3 {
            change = progress.recordAnswer(correct: true, skillID: "snd_g1", soundID: "s", day: 100, rules: rules)
        }
        XCTAssertEqual(change, .levelledUp(to: SoundStage.recognises.rawValue))
        XCTAssertEqual(progress.sounds.stage(of: "s"), .recognises)
        XCTAssertEqual(progress.skill("snd_g1").totalAttempts, 3)
        XCTAssertEqual(progress.skill("snd_g2").totalAttempts, 0)
    }

    func testRecentGamesAreKeptShort() {
        var progress = ChildProgress()
        for i in 0..<30 { progress.notePlayed(gameID: "game_\(i)") }
        XCTAssertEqual(progress.recentGames.count, 20)
        XCTAssertEqual(progress.recentGames.last, "game_29")
    }

    // MARK: Saved progress

    func testProgressSurvivesSaving() throws {
        var progress = ChildProgress()
        progress.markMet(soundID: "s")
        master("snd_g1", in: &progress)
        progress.notePlayed(gameID: "sound_hunt")
        let data = try JSONEncoder().encode(progress)
        XCTAssertEqual(try JSONDecoder().decode(ChildProgress.self, from: data), progress)
    }

    func testEmptyOrPartialSavesStillLoad() throws {
        let empty = try JSONDecoder().decode(ChildProgress.self, from: Data("{}".utf8))
        XCTAssertEqual(empty, ChildProgress())

        // An older save with only sound levels, and a newer save with a field this version doesn't know.
        let old = try JSONDecoder().decode(ChildProgress.self, from: Data("""
        {"sounds": {"skills": {"s": {"level": 2, "correctStreak": 1, "missStreak": 0}}}, "fromTheFuture": true,
         "skills": {"snd_g1": {"recent": [{"correct": true, "day": 5}]}}}
        """.utf8))
        XCTAssertEqual(old.sounds.stage(of: "s"), .recognises)
        XCTAssertEqual(old.skill("snd_g1").totalAttempts, 1)
        XCTAssertEqual(old.version, ChildProgress.currentVersion)
    }

    // MARK: Days

    func testDayNumbersCountCalendarDays() throws {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = try XCTUnwrap(TimeZone(identifier: "Africa/Johannesburg"))
        let morning = try XCTUnwrap(calendar.date(from: DateComponents(year: 2026, month: 10, day: 1, hour: 7)))
        let evening = try XCTUnwrap(calendar.date(from: DateComponents(year: 2026, month: 10, day: 1, hour: 23, minute: 59)))
        let tomorrow = try XCTUnwrap(calendar.date(from: DateComponents(year: 2026, month: 10, day: 2, hour: 0, minute: 1)))
        XCTAssertEqual(DayNumber.of(morning, calendar: calendar), DayNumber.of(evening, calendar: calendar))
        XCTAssertEqual(DayNumber.of(tomorrow, calendar: calendar), DayNumber.of(morning, calendar: calendar) + 1)
    }

    // MARK: Helpers

    private func masteredRecord(on day: Int) -> SkillRecord {
        var record = SkillRecord()
        for _ in 0..<5 { record.record(correct: true, on: day - 1, rules: rules) }
        for _ in 0..<5 { record.record(correct: true, on: day, rules: rules) }
        XCTAssertEqual(record.masteredOnDay, day)
        return record
    }

    private func master(_ skillID: String, in progress: inout ChildProgress) {
        for i in 0..<10 {
            progress.recordAnswer(correct: true, skillID: skillID, soundID: nil, day: 100 + i / 5, rules: rules)
        }
        XCTAssertTrue(progress.isMastered(skillID))
    }
}
