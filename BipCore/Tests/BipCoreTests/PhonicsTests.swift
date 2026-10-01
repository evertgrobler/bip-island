import XCTest
import BipCore

final class PhonicsTests: XCTestCase {
    private func course() throws -> PhonicsCourse {
        PhonicsCourse(try TestContent.library())
    }

    func testGroupsFollowTheLettersAndSoundsOrder() throws {
        let course = try course()
        XCTAssertEqual(course.groups.map(\.number), Array(1...9))
        XCTAssertEqual(course.groups[0].sounds.map(\.id), ["s", "a", "t", "p", "i", "n"])
        XCTAssertEqual(course.groups[1].sounds.map(\.id), ["m", "d", "g", "o", "c", "k"])
        XCTAssertEqual(course.groups[2].sounds.map(\.id), ["ck", "e", "u", "r"])
        XCTAssertEqual(course.groups[3].sounds.map(\.id), ["h", "b", "f", "l", "ff", "ll", "ss"])
        XCTAssertEqual(course.groups[4].sounds.map(\.id), ["j", "v", "w", "x", "y", "z", "zz", "qu"])
        XCTAssertEqual(Set(course.groups[5].sounds.map(\.grapheme)), ["ch", "sh", "th", "ng"])
        XCTAssertEqual(course.firstGroup.sounds.map(\.grapheme).joined(), "satpin")
    }

    func testGroupsLinkToTheirSkills() {
        XCTAssertEqual(PhonicsCourse.skillID(forGroup: 3), "snd_g3")
        XCTAssertEqual(PhonicsCourse.group(forSkill: "snd_g7"), 7)
        XCTAssertNil(PhonicsCourse.group(forSkill: "blend_cvc"))
    }

    func testNoSoundIsTaughtTwice() throws {
        let ids = try course().allSounds.map(\.id)
        XCTAssertEqual(ids.count, Set(ids).count)
    }

    func testStopSoundsMustBeClipped() throws {
        let course = try course()
        for id in ["t", "p", "k", "c", "b", "d", "g"] {
            let sound = try XCTUnwrap(course.sound(id: id), id)
            XCTAssertEqual(sound.kind, .bouncy, id)
            XCTAssertTrue(sound.mustBeClipped, id)
        }
        for id in ["s", "m", "n", "a", "sh"] {
            XCTAssertFalse(try XCTUnwrap(course.sound(id: id)).mustBeClipped, id)
        }
    }

    func testStretchySoundsAndVowels() throws {
        let course = try course()
        let stretchy = Set(course.allSounds.filter { $0.kind == .stretchy && $0.grapheme.count == 1 }.map(\.id))
        XCTAssertEqual(stretchy, ["s", "m", "f", "n", "l", "r", "v", "w", "y", "z"])
        let vowels = Set(course.allSounds.filter { $0.kind == .vowel }.map(\.id))
        XCTAssertEqual(vowels, ["a", "e", "i", "o", "u"])
    }

    func testSoundsUpToAGroup() throws {
        let course = try course()
        XCTAssertEqual(course.sounds(upToGroup: 1).map(\.id), ["s", "a", "t", "p", "i", "n"])
        XCTAssertEqual(course.sounds(upToGroup: 2).count, 12)
        XCTAssertEqual(course.sounds(upToGroup: 9).count, course.allSounds.count)
    }

    func testSoundsAndPicturesUseTheFileNameContract() throws {
        let content = try TestContent.library()
        for sound in try course().allSounds {
            XCTAssertEqual(sound.soundClip, "snd_\(sound.id)")
            XCTAssertEqual(sound.picture, "pic_\(sound.pictureWord)")
            XCTAssertTrue(content.audioIDs.contains(sound.soundClip), sound.id)
            XCTAssertTrue(content.audioIDs.contains(sound.wordClip), sound.id)
            XCTAssertTrue(content.pictureIDs.contains(sound.picture), sound.id)
        }
    }

    func testSoundsThatCouldBeMixedUp() throws {
        let course = try course()
        func sound(_ id: String) throws -> PhonicsSound { try XCTUnwrap(course.sound(id: id), id) }
        XCTAssertTrue(try sound("c").isConfusable(with: sound("k")), "c and k sound the same")
        XCTAssertTrue(try sound("ck").isConfusable(with: sound("c")))
        XCTAssertTrue(try sound("ow").isConfusable(with: sound("ow_long")), "ow and ow look the same")
        XCTAssertTrue(try sound("ai").isConfusable(with: sound("ay")))
        XCTAssertFalse(try sound("s").isConfusable(with: sound("a")))
        XCTAssertFalse(try sound("s").isConfusable(with: sound("s")), "a sound isn't confusable with itself")
    }

    func testLetterNamesComeOnlyAfterGroupFive() throws {
        let course = try course()
        let named = course.allSounds.filter { $0.letterName != nil }
        XCTAssertTrue(named.allSatisfy { $0.group <= 5 }, "letter names belong to single letters in groups 1–5")

        XCTAssertFalse(PhonicsCourse.letterNamesUnlocked(knownSounds: []))
        XCTAssertFalse(PhonicsCourse.letterNamesUnlocked(knownSounds: Set(course.firstGroup.sounds.map(\.id))))
        let groupsOneToFive = Set(course.sounds(upToGroup: 5).map(\.id))
        XCTAssertTrue(PhonicsCourse.letterNamesUnlocked(knownSounds: groupsOneToFive))
        XCTAssertFalse(PhonicsCourse.letterNamesUnlocked(knownSounds: groupsOneToFive.subtracting(["qu"])), "q is only covered by qu")
        XCTAssertEqual(PhonicsCourse.lettersCovered(byKnownSounds: ["qu", "sh"]), ["q"])
    }
}
