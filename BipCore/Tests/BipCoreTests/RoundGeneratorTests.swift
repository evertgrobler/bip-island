import XCTest
import BipCore

/// "Every round is fair" (docs/GAMES.md): each game plays 1,000 random rounds for random children,
/// and every round must have exactly one right answer, use only content the child has unlocked,
/// and never ask for the same word twice in a session.
final class RoundGeneratorTests: XCTestCase {
    static let roundsPerGame = 1_000

    private struct Fixture {
        let content: ContentLibrary
        let course: PhonicsCourse
        let meet: MeetTheSoundGame
        let hunt: SoundHuntGame
        let pop: BubblePopGame
    }

    private func fixture() throws -> Fixture {
        let content = try TestContent.library()
        let course = PhonicsCourse(content)
        return Fixture(content: content, course: course,
                       meet: try MeetTheSoundGame(content: content, course: course),
                       hunt: try SoundHuntGame(content: content, course: course),
                       pop: try BubblePopGame(content: content, course: course))
    }

    /// A random child: some groups unlocked, some sounds met, sometimes a focus sound.
    private func randomLearner<G: RandomNumberGenerator>(_ f: Fixture, using rng: inout G) -> Learner {
        let group = Int.random(in: 1...f.course.groups.count, using: &rng)
        let open = f.course.sounds(upToGroup: group)
        var known = Set(open.filter { _ in Bool.random(using: &rng) }.map(\.id))
        known.insert(open.randomElement(using: &rng)!.id)
        let focus = Bool.random(using: &rng) ? open.filter { known.contains($0.id) }.randomElement(using: &rng)?.id : nil
        return Learner(band: .foundation, unlockedPhonicsGroup: group, knownSoundIDs: known, focusSoundID: focus)
    }

    /// Plays whole sessions until `count` rounds have been checked.
    private func playSessions<Game: MiniGame>(of game: Game, _ f: Fixture, rounds count: Int, seed: UInt64,
                                              check: (Game.Round, Learner, GameSession) -> Void) {
        var rng = SeededGenerator(seed: seed)
        var played = 0
        var sessions = 0
        while played < count {
            let learner = randomLearner(f, using: &rng)
            var session = GameSession(game: game, settings: f.content.games.session, drawableSkins: [], using: &rng)
            while let round = session.nextRound(of: game, for: learner, using: &rng) {
                check(round, learner, session)
                played += 1
            }
            sessions += 1
            XCTAssertLessThan(sessions, count * 10, "too many empty sessions: the generator can't make rounds")
            if sessions >= count * 10 { return }
        }
    }

    // MARK: Sound Hunt

    func testSoundHuntRoundsAreFair() throws {
        let f = try fixture()
        var answerPositions = Set<Int>()
        var targets = Set<String>()
        playSessions(of: f.hunt, f, rounds: Self.roundsPerGame, seed: 1) { round, learner, session in
            XCTAssertEqual(round.choices.count, SoundHuntGame.choiceCount)
            XCTAssertEqual(Set(round.choices.map(\.word)).count, round.choices.count, "a picture twice in one round")

            // Exactly one right answer, by the game's own rule…
            XCTAssertEqual(f.hunt.correctChoices(in: round), [round.answer])
            // …and checked again straight from the content: only the answer starts with the target sound.
            let starting = round.choices.filter { firstSoundIPA(of: $0.word, f.content) == round.target.ipa }
            XCTAssertEqual(starting.map(\.word), [round.answer.word], "\(round.target.id): \(round.choices.map(\.word))")

            XCTAssertTrue(learner.knownSoundIDs.contains(round.target.id), "asked about a sound the child hasn't met")
            XCTAssertLessThanOrEqual(round.target.group, learner.unlockedPhonicsGroup)
            for choice in round.choices {
                XCTAssertTrue(isShowable(choice.word, upToGroup: learner.unlockedPhonicsGroup, f.content),
                              "\(choice.word) isn't decodable at group \(learner.unlockedPhonicsGroup)")
                XCTAssertTrue(f.content.pictureIDs.contains(choice.picture), choice.picture)
                XCTAssertTrue(f.content.audioIDs.contains(choice.audio), choice.audio)
            }
            XCTAssertTrue(f.content.audioIDs.contains(round.target.soundClip))
            answerPositions.insert(round.choices.firstIndex(of: round.answer)!)
            targets.insert(round.target.id)
        }
        XCTAssertEqual(answerPositions, [0, 1, 2], "the answer should turn up in every position")
        XCTAssertGreaterThan(targets.count, 20, "rounds should cover many different sounds")
    }

    func testSoundHuntNeverRepeatsAWordInASession() throws {
        let f = try fixture()
        var rng = SeededGenerator(seed: 2)
        for _ in 0..<200 {
            let learner = randomLearner(f, using: &rng)
            // A long session, to run each child out of fresh words.
            var session = GameSession(gameID: SoundHuntGame.id, skin: f.hunt.skins[0], maxRounds: 500)
            var answers: [String] = []
            while let round = session.nextRound(of: f.hunt, for: learner, using: &rng) {
                answers.append(round.answer.word)
            }
            XCTAssertEqual(answers.count, Set(answers).count, "repeated: \(answers)")
        }
    }

    func testSoundHuntAlternatesWithTheFocusSound() throws {
        let f = try fixture()
        var rng = SeededGenerator(seed: 3)
        let learner = Learner(band: .foundation, unlockedPhonicsGroup: 2, knownSoundIDs: ["p", "t", "m", "c"], focusSoundID: "p")
        var session = GameSession(gameID: SoundHuntGame.id, skin: f.hunt.skins[0], maxRounds: 6)
        let first = try XCTUnwrap(session.nextRound(of: f.hunt, for: learner, using: &rng))
        XCTAssertEqual(first.target.id, "p", "the first question is about the focus sound")
        let second = try XCTUnwrap(session.nextRound(of: f.hunt, for: learner, using: &rng))
        XCTAssertNotEqual(second.target.id, "p", "then another sound the child knows")
    }

    func testSoundHuntWithOnlyOneSoundMetEndsWhenItsPicturesRunOut() throws {
        let f = try fixture()
        var rng = SeededGenerator(seed: 4)
        let learner = Learner(band: .foundation, unlockedPhonicsGroup: 1, knownSoundIDs: ["s"], focusSoundID: "s")
        var session = GameSession(gameID: SoundHuntGame.id, skin: f.hunt.skins[0], maxRounds: 6)
        let round = try XCTUnwrap(session.nextRound(of: f.hunt, for: learner, using: &rng))
        XCTAssertEqual(round.answer.word, "sun", "in group 1 only the sun starts with s")
        XCTAssertNil(session.nextRound(of: f.hunt, for: learner, using: &rng))
    }

    func testSoundHuntOnlyOffersSoundsThatStartWords() throws {
        let f = try fixture()
        let course = f.course
        XCTAssertTrue(f.hunt.canHunt(try XCTUnwrap(course.sound(id: "s")), upToGroup: 1))
        XCTAssertFalse(f.hunt.canHunt(try XCTUnwrap(course.sound(id: "ng")), upToGroup: 9), "no word starts with ng")
        XCTAssertFalse(f.hunt.canHunt(try XCTUnwrap(course.sound(id: "x")), upToGroup: 9), "no word starts with x")
    }

    // MARK: Bubble Pop

    func testBubblePopRoundsAreFair() throws {
        let f = try fixture()
        var rng = SeededGenerator(seed: 5)
        playSessions(of: f.pop, f, rounds: Self.roundsPerGame, seed: 6) { round, learner, _ in
            XCTAssertEqual(round.choices.count, BubblePopGame.bubbleCount)
            XCTAssertEqual(f.pop.correctChoices(in: round), [round.target])
            XCTAssertEqual(lookAlikesOrSoundAlikes(of: round.target, in: round.choices), 1,
                           "\(round.target.id): \(round.choices.map(\.grapheme))")
            if let focus = learner.focusSoundID { XCTAssertEqual(round.target.id, focus) }
            XCTAssertTrue(learner.knownSoundIDs.contains(round.target.id))
            for letter in round.choices + round.others {
                XCTAssertLessThanOrEqual(letter.group, learner.unlockedPhonicsGroup, "\(letter.id) isn't unlocked yet")
            }
            XCTAssertTrue(f.content.audioIDs.contains(round.target.soundClip))

            // Bubbles float off the top and come back with new letters: still exactly one right one.
            var onScreen = round.choices
            for _ in 0..<40 {
                let index = Int.random(in: 0..<onScreen.count, using: &rng)
                var others = onScreen
                others.remove(at: index)
                onScreen[index] = f.pop.nextBubble(in: round, onScreen: others, using: &rng)
                XCTAssertEqual(lookAlikesOrSoundAlikes(of: round.target, in: onScreen), 1, onScreen.map(\.grapheme).joined(separator: " "))
            }
        }
    }

    func testBubblePopUsesDifferentLettersWhenThereAreEnough() throws {
        let f = try fixture()
        var rng = SeededGenerator(seed: 7)
        let learner = Learner(band: .foundation, unlockedPhonicsGroup: 1, knownSoundIDs: ["s", "a"], focusSoundID: "s")
        for _ in 0..<100 {
            var session = GameSession(gameID: BubblePopGame.id, skin: f.pop.skins[0], maxRounds: 1)
            let round = try XCTUnwrap(session.nextRound(of: f.pop, for: learner, using: &rng))
            XCTAssertEqual(Set(round.choices.map(\.id)).count, BubblePopGame.bubbleCount, "group 1 has five other letters to show")
        }
    }

    // MARK: Meet the Sound

    func testMeetTheSoundRoundsAreFair() throws {
        let f = try fixture()
        playSessions(of: f.meet, f, rounds: Self.roundsPerGame, seed: 8) { round, learner, session in
            XCTAssertEqual(session.maxRounds, 1, "one sound per visit")
            XCTAssertEqual(f.meet.correctChoices(in: round).map(\.id), [round.sound.id])
            XCTAssertLessThanOrEqual(round.sound.group, learner.unlockedPhonicsGroup)
            if let focus = learner.focusSoundID { XCTAssertEqual(round.sound.id, focus) }
            XCTAssertTrue(f.content.audioIDs.contains(round.sound.soundClip))
            XCTAssertTrue(f.content.audioIDs.contains(round.sound.wordClip))
            XCTAssertTrue(f.content.pictureIDs.contains(round.sound.picture))
        }
    }

    func testMeetTheSoundIntroducesTheNextNewSoundInOrder() throws {
        let f = try fixture()
        var rng = SeededGenerator(seed: 9)
        let learner = Learner(band: .foundation, unlockedPhonicsGroup: 1, knownSoundIDs: ["s", "a"])
        var session = GameSession(gameID: MeetTheSoundGame.id, skin: f.meet.skins[0], maxRounds: 10)
        var met: [String] = []
        while let round = session.nextRound(of: f.meet, for: learner, using: &rng) {
            met.append(round.sound.id)
        }
        XCTAssertEqual(Array(met.prefix(4)), ["t", "p", "i", "n"])
        XCTAssertEqual(met.count, Set(met).count, "no sound twice in a session")
        XCTAssertEqual(met.count, 6, "then the known ones, until all six have been used")
    }

    // MARK: The template

    func testEveryGameListsAtLeastThreeSkins() throws {
        let f = try fixture()
        XCTAssertGreaterThanOrEqual(f.meet.skins.count, 3)
        XCTAssertGreaterThanOrEqual(f.hunt.skins.count, 3)
        XCTAssertGreaterThanOrEqual(f.pop.skins.count, 3)
    }

    func testSessionsPickOnlySkinsTheAppCanDraw() throws {
        let f = try fixture()
        var rng = SeededGenerator(seed: 10)
        for _ in 0..<20 {
            let session = GameSession(game: f.pop, settings: f.content.games.session, drawableSkins: ["balloons", "fireflies"], using: &rng)
            XCTAssertTrue(["balloons", "fireflies"].contains(session.skin.id))
            XCTAssertEqual(session.maxRounds, f.content.games.session.roundsPerSession)
        }
        let fallback = GameSession(game: f.pop, settings: f.content.games.session, drawableSkins: [], using: &rng)
        XCTAssertEqual(fallback.skin, f.pop.skins[0])
    }

    func testASessionStopsAfterItsRounds() throws {
        let f = try fixture()
        var rng = SeededGenerator(seed: 11)
        let learner = Learner(band: .foundation, unlockedPhonicsGroup: 3, knownSoundIDs: Set(f.course.sounds(upToGroup: 3).map(\.id)))
        var session = GameSession(gameID: BubblePopGame.id, skin: f.pop.skins[0], maxRounds: 6)
        var count = 0
        while session.nextRound(of: f.pop, for: learner, using: &rng) != nil { count += 1 }
        XCTAssertEqual(count, 6)
        XCTAssertTrue(session.isFinished)
    }

    func testNothingToAskMeansNoRound() throws {
        let f = try fixture()
        var rng = SeededGenerator(seed: 12)
        let nobody = Learner(band: .foundation, unlockedPhonicsGroup: 1, knownSoundIDs: [])
        var hunt = GameSession(gameID: SoundHuntGame.id, skin: f.hunt.skins[0], maxRounds: 6)
        var pop = GameSession(gameID: BubblePopGame.id, skin: f.pop.skins[0], maxRounds: 6)
        XCTAssertNil(hunt.nextRound(of: f.hunt, for: nobody, using: &rng), "no sounds met yet: nothing to hunt")
        XCTAssertNil(pop.nextRound(of: f.pop, for: nobody, using: &rng), "no sounds met yet: nothing to pop")
    }

    // MARK: Independent checks, straight from the content

    /// The IPA of a word's first sound: from the word bank, or (for a sound's own picture not in the
    /// bank, like ink) from the grapheme it belongs to.
    private func firstSoundIPA(of word: String, _ content: ContentLibrary) -> String? {
        if let banked = content.word(word) {
            return content.grapheme(id: banked.firstSound)?.ipa
        }
        return content.phonics.graphemes.first { $0.mnemonicWord == word }?.ipa
    }

    /// Words may only be shown once their phonics group is unlocked, except a sound's own picture,
    /// which the child meets with that sound.
    private func isShowable(_ word: String, upToGroup group: Int, _ content: ContentLibrary) -> Bool {
        if let banked = content.word(word), banked.decodableFromGroup <= group { return true }
        return content.phonics.graphemes.contains { $0.mnemonicWord == word && $0.group <= group }
    }

    private func lookAlikesOrSoundAlikes(of target: PhonicsSound, in letters: [PhonicsSound]) -> Int {
        letters.filter { $0.ipa == target.ipa || $0.grapheme == target.grapheme }.count
    }
}
