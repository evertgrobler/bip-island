import XCTest
import BipCore

final class PhonicsTests: XCTestCase {
    func testGroupsFollowJollyPhonicsOrder() {
        let order = Phonics.groups.map { $0.sounds.map(\.id) }
        XCTAssertEqual(order, [
            ["s", "a", "t", "p", "i", "n"],
            ["m", "d", "g", "o", "c", "k"],
            ["ck", "e", "u", "r"],
            ["h", "b", "f", "l"],
            ["j", "v", "w", "x", "y", "z", "qu"],
            ["sh", "ch", "th", "ng", "ee", "oo", "ai"],
        ])
        XCTAssertEqual(Phonics.groups.map(\.number), [1, 2, 3, 4, 5, 6])
    }

    func testFirstGroupIsSATPIN() {
        XCTAssertEqual(Phonics.firstGroup.sounds.map(\.id).joined(), "satpin")
    }

    func testNoSoundIsTaughtTwice() {
        let ids = Phonics.allSounds.map(\.id)
        XCTAssertEqual(ids.count, Set(ids).count)
    }

    func testStopSoundsMustBeClipped() {
        for id in ["t", "p", "k", "c", "b", "d", "g", "ck"] {
            let sound = try XCTUnwrap(Phonics.sound(id: id), id)
            XCTAssertEqual(sound.kind, .stop, id)
            XCTAssertTrue(sound.mustBeClipped, id)
        }
        for id in ["s", "m", "n", "a", "sh"] {
            XCTAssertFalse(try XCTUnwrap(Phonics.sound(id: id)).mustBeClipped, id)
        }
    }

    func testStretchySoundsAndVowels() {
        let stretchy = Phonics.allSounds.filter { $0.kind == .stretchy }.map(\.id)
        XCTAssertEqual(Set(stretchy), ["s", "m", "f", "n", "l", "r", "v", "z"])
        let vowels = Phonics.allSounds.filter { $0.kind == .shortVowel }.map(\.id)
        XCTAssertEqual(Set(vowels), ["a", "e", "i", "o", "u"])
    }

    func testDigraphsWaitUntilAgeSix() {
        XCTAssertEqual(Phonics.groups(forAge: 4).map(\.number), [1, 2, 3, 4, 5])
        XCTAssertEqual(Phonics.groups(forAge: 5).map(\.number), [1, 2, 3, 4, 5])
        XCTAssertEqual(Phonics.groups(forAge: 6).map(\.number), [1, 2, 3, 4, 5, 6])
        XCTAssertFalse(Phonics.teachingOrder(forAge: 4).contains { $0.id == "sh" })
        XCTAssertEqual(Phonics.teachingOrder(forAge: 8).first?.id, "s")
    }

    func testPictureWordsStartWithTheirSound() {
        for sound in Phonics.allSounds where sound.isAtStartOfWord {
            XCTAssertTrue(sound.pictureWord.hasPrefix(sound.id), "\(sound.id) → \(sound.pictureWord)")
        }
        for sound in Phonics.allSounds where !sound.isAtStartOfWord {
            XCTAssertTrue(sound.pictureWord.contains(sound.id), "\(sound.id) → \(sound.pictureWord)")
        }
    }

    func testLetterNamesLockedUntilAll26SoundsKnown() {
        XCTAssertFalse(Phonics.letterNamesUnlocked(knownSounds: []))
        XCTAssertFalse(Phonics.letterNamesUnlocked(knownSounds: Set(Phonics.firstGroup.sounds.map(\.id))))

        let groupsOneToFive = Set(Phonics.groups(forAge: 4).flatMap(\.sounds).map(\.id))
        XCTAssertTrue(Phonics.letterNamesUnlocked(knownSounds: groupsOneToFive))

        // q is only covered by "qu".
        XCTAssertFalse(Phonics.letterNamesUnlocked(knownSounds: groupsOneToFive.subtracting(["qu"])))
        XCTAssertEqual(Phonics.lettersCovered(byKnownSounds: ["qu", "sh"]), ["q"])
    }
}
