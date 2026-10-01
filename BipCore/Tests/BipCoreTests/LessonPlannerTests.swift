import XCTest
import BipCore

final class LessonPlannerTests: XCTestCase {
    private let sounds = Phonics.firstGroup.sounds
    private var planner: LessonPlanner { LessonPlanner(sounds: sounds) }

    func testFreshChildMeetsTheFirstSound() {
        var rng = SeededGenerator(seed: 1)
        let next = planner.nextActivity(tracker: MasteryTracker(), using: &rng)
        XCTAssertEqual(next, PlannedActivity(kind: .meetTheSound, sound: sounds[0]))
    }

    func testActivityFollowsTheStage() {
        var rng = SeededGenerator(seed: 1)
        XCTAssertEqual(planner.nextActivity(tracker: MasteryTracker(skills: ["s": SkillMastery(level: 1)]), using: &rng).kind, .soundHunt)
        XCTAssertEqual(planner.nextActivity(tracker: MasteryTracker(skills: ["s": SkillMastery(level: 2)]), using: &rng).kind, .popTheLetter)
    }

    func testMasteredSoundMovesOnToTheNextInOrder() {
        var rng = SeededGenerator(seed: 1)
        let tracker = MasteryTracker(skills: ["s": SkillMastery(level: 3)])
        XCTAssertEqual(planner.nextActivity(tracker: tracker, using: &rng), PlannedActivity(kind: .meetTheSound, sound: sounds[1]))
        XCTAssertEqual(planner.suggestedSound(tracker: tracker)?.id, "a")
    }

    func testEverythingMasteredMeansReview() {
        var rng = SeededGenerator(seed: 42)
        var skills: [String: SkillMastery] = [:]
        for s in sounds { skills[s.id] = SkillMastery(level: 3) }
        let tracker = MasteryTracker(skills: skills)
        XCTAssertNil(planner.suggestedSound(tracker: tracker))
        var kinds = Set<ActivityKind>()
        for _ in 0..<50 { kinds.insert(planner.nextActivity(tracker: tracker, using: &rng).kind) }
        XCTAssertEqual(kinds, [.soundHunt, .popTheLetter])
    }

    func testHuntQuestionHasThreeDifferentPicturesIncludingTheTarget() {
        var rng = SeededGenerator(seed: 7)
        for target in sounds {
            for _ in 0..<20 {
                let q = planner.makeHuntQuestion(target: target, using: &rng)
                XCTAssertEqual(q.choices.count, 3)
                XCTAssertEqual(Set(q.choices.map(\.pictureWord)).count, 3)
                XCTAssertEqual(q.choices.filter { $0 == target }.count, 1)
            }
        }
    }

    func testHuntPutsTheAnswerInDifferentPlaces() {
        var rng = SeededGenerator(seed: 3)
        var positions = Set<Int>()
        for _ in 0..<30 {
            let q = planner.makeHuntQuestion(target: sounds[0], using: &rng)
            positions.insert(q.choices.firstIndex(of: sounds[0])!)
        }
        XCTAssertEqual(positions, [0, 1, 2])
    }

    func testBubblesAlwaysIncludeTheTarget() {
        var rng = SeededGenerator(seed: 9)
        for target in sounds {
            let letters = planner.makeBubbleLetters(target: target, count: 5, using: &rng)
            XCTAssertEqual(letters.count, 5)
            XCTAssertTrue(letters.contains(target))
        }
    }

    func testANewBubbleIsTheTargetWhenNoOtherBubbleShowsIt() {
        var rng = SeededGenerator(seed: 11)
        let target = sounds[2]
        let others = sounds.filter { $0 != target }
        XCTAssertEqual(planner.nextBubbleLetter(target: target, onScreen: Array(others.prefix(4)), using: &rng), target)
        for _ in 0..<20 {
            XCTAssertNotEqual(planner.nextBubbleLetter(target: target, onScreen: [target, others[0]], using: &rng), target)
        }
    }
}
