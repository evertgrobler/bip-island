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

/// The optional parent passcode.
final class ParentPasscodeTests: XCTestCase {
    func testACodeMatchesOnlyItself() throws {
        let code = try XCTUnwrap(ParentPasscode(code: "2468"))
        XCTAssertTrue(code.matches("2468"))
        XCTAssertTrue(code.matches(" 24 68 "))
        XCTAssertFalse(code.matches("2467"))
        XCTAssertFalse(code.matches(""))
        XCTAssertFalse(code.matches("24680"))
    }

    func testCodesMustBeFourToEightDigits() {
        XCTAssertNil(ParentPasscode(code: "123"))
        XCTAssertNil(ParentPasscode(code: "123456789"))
        XCTAssertNil(ParentPasscode(code: "12a4"))
        XCTAssertNil(ParentPasscode(code: "١٢٣٤"), "only plain 0–9")
        XCTAssertNotNil(ParentPasscode(code: "1234"))
        XCTAssertNotNil(ParentPasscode(code: "12345678"))
    }

    func testTheDigitsAreNeverStored() throws {
        let code = try XCTUnwrap(ParentPasscode(code: "975310"))
        let saved = String(decoding: try JSONEncoder().encode(code), as: UTF8.self)
        XCTAssertFalse(saved.contains("975310"))
        XCTAssertEqual(try JSONDecoder().decode(ParentPasscode.self, from: Data(saved.utf8)), code)
    }

    func testTheSameCodeHashesDifferentlyWithADifferentSalt() throws {
        let a = try XCTUnwrap(ParentPasscode(code: "1111", salt: "a"))
        let b = try XCTUnwrap(ParentPasscode(code: "1111", salt: "b"))
        XCTAssertNotEqual(a.hash, b.hash)
        XCTAssertTrue(a.matches("1111") && b.matches("1111"))
    }
}
