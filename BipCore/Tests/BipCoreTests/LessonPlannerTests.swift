import XCTest
import BipCore

/// Bip's suggestions on Letters Island, worked out from the content and a child's progress.
final class LessonPlannerTests: XCTestCase {
    private let rules = TestContent.rules
    private let today = 1_000

    private func letters(_ progress: ChildProgress, band: Band = .foundation) throws -> LettersProgress {
        let content = try TestContent.library()
        return LettersProgress(content: content, course: PhonicsCourse(content), progress: progress, startingBand: band)
    }

    private func planner(_ progress: ChildProgress, band: Band = .foundation, canHunt: @escaping @Sendable (PhonicsSound) -> Bool = { _ in true }) throws -> LessonPlanner {
        LessonPlanner(letters: try letters(progress, band: band), canHunt: canHunt)
    }

    private func next(_ progress: ChildProgress, canHunt: @escaping @Sendable (PhonicsSound) -> Bool = { _ in true }) throws -> (ActivityKind, String) {
        var rng = SeededGenerator(seed: 1)
        let activity = try planner(progress, canHunt: canHunt).nextActivity(day: today, rules: rules, using: &rng)
        return (activity.kind, activity.sound.id)
    }

    // Progress helpers.
    private func met(_ ids: [String], in progress: inout ChildProgress) {
        ids.forEach { progress.markMet(soundID: $0) }
    }

    private func raise(_ id: String, to stage: SoundStage, in progress: inout ChildProgress) {
        progress.markMet(soundID: id)
        while progress.sounds.stage(of: id) < stage {
            progress.recordAnswer(correct: true, skillID: "practice", soundID: id, day: today, rules: rules)
        }
    }

    private func masterSkill(_ id: String, in progress: inout ChildProgress, finishingOn day: Int) {
        for i in 0..<10 {
            progress.recordAnswer(correct: true, skillID: id, soundID: nil, day: i < 5 ? day - 1 : day, rules: rules)
        }
    }

    // MARK: Tests

    func testAFreshChildMeetsTheFirstSound() throws {
        let state = try letters(ChildProgress())
        XCTAssertEqual(state.unlockedGroups, [1])
        XCTAssertEqual(state.currentGroup.number, 1)
        XCTAssertTrue(state.knownSoundIDs.isEmpty)
        let (kind, sound) = try next(ChildProgress())
        XCTAssertEqual(kind, .meetTheSound)
        XCTAssertEqual(sound, "s")
    }

    func testTwoSoundsAreMetBeforePractice() throws {
        var progress = ChildProgress()
        met(["s"], in: &progress)
        XCTAssertEqual(try next(progress).0, .meetTheSound)
        XCTAssertEqual(try next(progress).1, "a")

        met(["a"], in: &progress)
        let (kind, sound) = try next(progress)
        XCTAssertEqual(kind, .soundHunt, "two sounds are waiting: practise before meeting more")
        XCTAssertEqual(sound, "s")
    }

    func testRecognisedSoundsMoveOnToBubblePop() throws {
        var progress = ChildProgress()
        raise("s", to: .recognises, in: &progress)
        met(["a", "t"], in: &progress)
        let (kind, sound) = try next(progress)
        XCTAssertEqual(kind, .bubblePop)
        XCTAssertEqual(sound, "s")
    }

    func testASoundNoPictureStartsWithGoesStraightToBubblePop() throws {
        var progress = ChildProgress()
        met(["s", "a"], in: &progress)
        let (kind, _) = try next(progress, canHunt: { $0.id != "s" })
        XCTAssertEqual(kind, .bubblePop)
    }

    func testAMasteredSoundMovesOnToTheNextInOrder() throws {
        var progress = ChildProgress()
        raise("s", to: .mastered, in: &progress)
        XCTAssertEqual(try next(progress).1, "a")
        XCTAssertEqual(try planner(progress).suggestedSound()?.id, "a")
    }

    func testAWholeGroupLearntButNotYetMasteredMeansReview() throws {
        var progress = ChildProgress()
        for id in ["s", "a", "t", "p", "i", "n"] { raise(id, to: .mastered, in: &progress) }
        let state = try letters(progress)
        XCTAssertEqual(state.unlockedGroups, [1], "group 2 waits until snd_g1 is mastered over two days")
        XCTAssertEqual(state.currentGroup.number, 1)
        XCTAssertNil(try planner(progress).suggestedSound())

        var kinds = Set<ActivityKind>()
        var rng = SeededGenerator(seed: 42)
        let planner = try planner(progress)
        for _ in 0..<50 { kinds.insert(planner.nextActivity(day: today, rules: rules, using: &rng).kind) }
        XCTAssertEqual(kinds, [.soundHunt, .bubblePop])
    }

    func testMasteringAGroupOpensTheNext() throws {
        var progress = ChildProgress()
        for id in ["s", "a", "t", "p", "i", "n"] { raise(id, to: .mastered, in: &progress) }
        masterSkill("snd_g1", in: &progress, finishingOn: today)
        let state = try letters(progress)
        XCTAssertEqual(state.unlockedGroups, [1, 2])
        XCTAssertEqual(state.currentGroup.number, 2)
        XCTAssertEqual(state.learner().unlockedPhonicsGroup, 2)
        XCTAssertEqual(try next(progress).1, "m")
    }

    func testAGroupDueForReviewComesFirst() throws {
        var progress = ChildProgress()
        for id in ["s", "a", "t", "p", "i", "n"] { raise(id, to: .mastered, in: &progress) }
        masterSkill("snd_g1", in: &progress, finishingOn: today - 2)
        XCTAssertTrue(progress.isDueForReview("snd_g1", on: today, rules: rules))
        let (kind, sound) = try next(progress)
        XCTAssertNotEqual(kind, .meetTheSound, "a review, not the next new sound (m)")
        XCTAssertTrue(["s", "a", "t", "p", "i", "n"].contains(sound))
    }

    func testASixYearOldStartsAtGroupFive() throws {
        let state = try letters(ChildProgress(), band: .stage1)
        XCTAssertEqual(state.unlockedGroups, [1, 2, 3, 4, 5])
        XCTAssertEqual(state.currentGroup.number, 5)
        XCTAssertTrue(state.knownSoundIDs.isSuperset(of: ["s", "m", "ck", "h"]), "earlier groups count as known")
        XCTAssertFalse(state.knownSoundIDs.contains("j"))
        var rng = SeededGenerator(seed: 1)
        let activity = try planner(ChildProgress(), band: .stage1).nextActivity(day: today, rules: rules, using: &rng)
        XCTAssertEqual(activity, PlannedActivity(kind: .meetTheSound, sound: try XCTUnwrap(state.course.sound(id: "j"))))
    }

    func testTheLearnerCarriesTheFocus() throws {
        var progress = ChildProgress()
        met(["s"], in: &progress)
        let state = try letters(progress)
        let learner = state.learner(focus: state.course.sound(id: "s"))
        XCTAssertEqual(learner.focusSoundID, "s")
        XCTAssertEqual(learner.knownSoundIDs, ["s"])
        XCTAssertEqual(learner.unlockedPhonicsGroup, 1)
    }

    func testActivitiesMatchTheGameRegistry() throws {
        let content = try TestContent.library()
        for kind in ActivityKind.allCases {
            XCTAssertNotNil(content.game(id: kind.gameID), kind.gameID)
        }
        XCTAssertEqual(ActivityKind.meetTheSound.gameID, MeetTheSoundGame.id)
        XCTAssertEqual(ActivityKind.soundHunt.gameID, SoundHuntGame.id)
        XCTAssertEqual(ActivityKind.bubblePop.gameID, BubblePopGame.id)
    }
}
