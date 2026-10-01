import XCTest
import BipCore

/// Within a game: 3 right in a row moves up a level, 2 misses in a row drops back.
final class MasteryTests: XCTestCase {
    private let rules = TestContent.rules

    func testThreeRightInARowMovesUp() {
        var m = SkillMastery(level: 1)
        XCTAssertEqual(m.record(correct: true, rules: rules, maxLevel: 3), .none)
        XCTAssertEqual(m.record(correct: true, rules: rules, maxLevel: 3), .none)
        XCTAssertEqual(m.record(correct: true, rules: rules, maxLevel: 3), .levelledUp(to: 2))
        XCTAssertEqual(m.level, 2)
        XCTAssertEqual(m.correctStreak, 0, "streak restarts at the new level")
    }

    func testAMissBreaksTheCorrectStreak() {
        var m = SkillMastery(level: 1)
        m.record(correct: true, rules: rules, maxLevel: 3)
        m.record(correct: true, rules: rules, maxLevel: 3)
        m.record(correct: false, rules: rules, maxLevel: 3)
        XCTAssertEqual(m.record(correct: true, rules: rules, maxLevel: 3), .none)
        XCTAssertEqual(m.level, 1)
    }

    func testTwoMissesInARowDropBack() {
        var m = SkillMastery(level: 2)
        XCTAssertEqual(m.record(correct: false, rules: rules, maxLevel: 3), .none)
        XCTAssertEqual(m.record(correct: false, rules: rules, maxLevel: 3), .droppedBack(to: 1))
        XCTAssertEqual(m.missStreak, 0)
    }

    func testMissesSeparatedByARightAnswerDoNotDropBack() {
        var m = SkillMastery(level: 2)
        m.record(correct: false, rules: rules, maxLevel: 3)
        m.record(correct: true, rules: rules, maxLevel: 3)
        XCTAssertEqual(m.record(correct: false, rules: rules, maxLevel: 3), .none)
        XCTAssertEqual(m.level, 2)
    }

    func testLevelStaysWithinBounds() {
        var top = SkillMastery(level: 3)
        for _ in 0..<6 { XCTAssertEqual(top.record(correct: true, rules: rules, maxLevel: 3), .none) }
        XCTAssertEqual(top.level, 3)

        var bottom = SkillMastery(level: 0)
        for _ in 0..<6 { XCTAssertEqual(bottom.record(correct: false, rules: rules, maxLevel: 3), .none) }
        XCTAssertEqual(bottom.level, 0)
    }

    func testTheNumbersComeFromTheRules() {
        let quick = MasteryRules(correctInARowToMoveUp: 1, missesInARowToDropBack: 1, masteredWindow: 10,
                                 masteredPercent: 80, masteredDistinctDays: 2, reviewAfterDays: [2])
        var m = SkillMastery(level: 1)
        XCTAssertEqual(m.record(correct: true, rules: quick, maxLevel: 3), .levelledUp(to: 2))
        XCTAssertEqual(m.record(correct: false, rules: quick, maxLevel: 3), .droppedBack(to: 1))
    }

    func testSoundStages() {
        var tracker = MasteryTracker()
        XCTAssertEqual(tracker.stage(of: "s"), .new)
        tracker.markMet("s")
        XCTAssertEqual(tracker.stage(of: "s"), .met)
        for _ in 0..<3 { tracker.record(correct: true, for: "s", rules: rules) }
        XCTAssertEqual(tracker.stage(of: "s"), .recognises)
        for _ in 0..<3 { tracker.record(correct: true, for: "s", rules: rules) }
        XCTAssertEqual(tracker.stage(of: "s"), .mastered)
    }

    func testTwoMissesAfterMeetingGoesBackToMeetTheSound() {
        var tracker = MasteryTracker()
        tracker.markMet("s")
        tracker.record(correct: false, for: "s", rules: rules)
        XCTAssertEqual(tracker.record(correct: false, for: "s", rules: rules), .droppedBack(to: 0))
        XCTAssertEqual(tracker.stage(of: "s"), .new)
    }

    func testMarkMetNeverLowersALevel() {
        var tracker = MasteryTracker(skills: ["s": SkillMastery(level: 3)])
        tracker.markMet("s")
        XCTAssertEqual(tracker.stage(of: "s"), .mastered)
    }

    func testTrackerSurvivesEncoding() throws {
        var tracker = MasteryTracker()
        tracker.markMet("s")
        tracker.record(correct: true, for: "s", rules: rules)
        let data = try JSONEncoder().encode(tracker)
        XCTAssertEqual(try JSONDecoder().decode(MasteryTracker.self, from: data), tracker)
    }
}
