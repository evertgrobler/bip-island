import XCTest
import BipCore

final class ParentGateTests: XCTestCase {
    func testChallengeIsAnAdultSum() {
        var rng = SeededGenerator(seed: 5)
        for _ in 0..<100 {
            let c = ParentChallenge.random(using: &rng)
            XCTAssertTrue((12...19).contains(c.left))
            XCTAssertTrue((3...9).contains(c.right))
            XCTAssertGreaterThanOrEqual(c.answer, 36)
        }
    }

    func testChallengeChecksTheAnswer() {
        let c = ParentChallenge(left: 14, right: 7)
        XCTAssertEqual(c.question, "14 × 7")
        XCTAssertTrue(c.isCorrect("98"))
        XCTAssertTrue(c.isCorrect(" 98 \n"))
        XCTAssertFalse(c.isCorrect("97"))
        XCTAssertFalse(c.isCorrect(""))
        XCTAssertFalse(c.isCorrect("nine"))
    }

    func testEscMustBeHeldForThreeSeconds() {
        var hold = HoldDetector(duration: 3)
        XCTAssertEqual(hold.progress(at: 10), 0)
        hold.press(at: 10)
        XCTAssertEqual(hold.progress(at: 11.5), 0.5, accuracy: 0.0001)
        XCTAssertFalse(hold.isComplete(at: 12.9))
        XCTAssertTrue(hold.isComplete(at: 13))
    }

    func testKeyRepeatDoesNotRestartTheClock() {
        var hold = HoldDetector(duration: 3)
        hold.press(at: 0)
        hold.press(at: 2)
        XCTAssertTrue(hold.isComplete(at: 3))
    }

    func testLettingGoResets() {
        var hold = HoldDetector(duration: 3)
        hold.press(at: 0)
        hold.release()
        XCTAssertFalse(hold.isHeld)
        XCTAssertFalse(hold.isComplete(at: 5))
        hold.press(at: 5)
        XCTAssertFalse(hold.isComplete(at: 7))
    }
}
