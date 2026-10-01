import XCTest
import BipCore

final class QuestionAttemptTests: XCTestCase {
    func testRightFirstTime() {
        var q = QuestionAttempt()
        XCTAssertEqual(q.answer(correct: true), .correct(firstTry: true))
        XCTAssertTrue(q.countsAsCorrect)
    }

    func testFirstMissMeansTryAgain() {
        var q = QuestionAttempt()
        XCTAssertEqual(q.answer(correct: false), .tryAgain)
        XCTAssertFalse(q.needsHint)
    }

    func testSecondMissGivesAHint() {
        var q = QuestionAttempt()
        _ = q.answer(correct: false)
        XCTAssertEqual(q.answer(correct: false), .hint)
        XCTAssertTrue(q.needsHint)
        XCTAssertEqual(q.answer(correct: false), .hint, "keeps hinting, never fails")
    }

    func testRightAfterAMissDoesNotCountForMastery() {
        var q = QuestionAttempt()
        _ = q.answer(correct: false)
        XCTAssertEqual(q.answer(correct: true), .correct(firstTry: false))
        XCTAssertTrue(q.isAnswered)
        XCTAssertFalse(q.countsAsCorrect)
    }
}
