import XCTest
import BipCore

final class MasteryTests: XCTestCase {
    private let s = Phonics.firstGroup.sounds[0]

    func testThreeRightInARowMovesUp() {
        var m = SkillMastery(level: 1)
        XCTAssertEqual(m.record(correct: true, maxLevel: 3), .none)
        XCTAssertEqual(m.record(correct: true, maxLevel: 3), .none)
        XCTAssertEqual(m.record(correct: true, maxLevel: 3), .levelledUp(to: 2))
        XCTAssertEqual(m.level, 2)
        XCTAssertEqual(m.correctStreak, 0, "streak restarts at the new level")
    }

    func testAMissBreaksTheCorrectStreak() {
        var m = SkillMastery(level: 1)
        m.record(correct: true, maxLevel: 3)
        m.record(correct: true, maxLevel: 3)
        m.record(correct: false, maxLevel: 3)
        XCTAssertEqual(m.record(correct: true, maxLevel: 3), .none)
        XCTAssertEqual(m.level, 1)
    }

    func testTwoMissesInARowDropBack() {
        var m = SkillMastery(level: 2)
        XCTAssertEqual(m.record(correct: false, maxLevel: 3), .none)
        XCTAssertEqual(m.record(correct: false, maxLevel: 3), .droppedBack(to: 1))
        XCTAssertEqual(m.missStreak, 0)
    }

    func testMissesSeparatedByARightAnswerDoNotDropBack() {
        var m = SkillMastery(level: 2)
        m.record(correct: false, maxLevel: 3)
        m.record(correct: true, maxLevel: 3)
        XCTAssertEqual(m.record(correct: false, maxLevel: 3), .none)
        XCTAssertEqual(m.level, 2)
    }

    func testLevelStaysWithinBounds() {
        var top = SkillMastery(level: 3)
        for _ in 0..<6 { XCTAssertEqual(top.record(correct: true, maxLevel: 3), .none) }
        XCTAssertEqual(top.level, 3)

        var bottom = SkillMastery(level: 0)
        for _ in 0..<6 { XCTAssertEqual(bottom.record(correct: false, maxLevel: 3), .none) }
        XCTAssertEqual(bottom.level, 0)
    }

    func testTrackerStagesForASound() {
        var tracker = MasteryTracker()
        XCTAssertEqual(tracker.stage(of: s), .new)
        tracker.markMet(s)
        XCTAssertEqual(tracker.stage(of: s), .met)
        for _ in 0..<3 { tracker.record(correct: true, for: s) }
        XCTAssertEqual(tracker.stage(of: s), .recognises)
        for _ in 0..<3 { tracker.record(correct: true, for: s) }
        XCTAssertEqual(tracker.stage(of: s), .mastered)
    }

    func testTwoMissesAfterMeetingGoesBackToMeetTheSound() {
        var tracker = MasteryTracker()
        tracker.markMet(s)
        tracker.record(correct: false, for: s)
        XCTAssertEqual(tracker.record(correct: false, for: s), .droppedBack(to: 0))
        XCTAssertEqual(tracker.stage(of: s), .new)
    }

    func testMarkMetNeverLowersALevel() {
        var tracker = MasteryTracker(skills: ["s": SkillMastery(level: 3)])
        tracker.markMet(s)
        XCTAssertEqual(tracker.stage(of: s), .mastered)
    }

    func testTrackerSurvivesEncoding() throws {
        var tracker = MasteryTracker()
        tracker.markMet(s)
        tracker.record(correct: true, for: s)
        let data = try JSONEncoder().encode(tracker)
        XCTAssertEqual(try JSONDecoder().decode(MasteryTracker.self, from: data), tracker)
    }
}
