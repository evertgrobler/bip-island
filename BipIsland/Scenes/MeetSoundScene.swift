import BipCore
import SpriteKit

/// "Meet the sound": the letter bounces in, the narrator says the pure sound three times,
/// then the picture word (s → sun). Click the letter to hear it again; the green arrow carries on.
final class MeetSoundScene: BaseScene {
    private let sound: PhonicsSound
    private let letter: SKNode
    private var picture: PictureCard?
    private let nextButton = Buttons.next()

    init(coordinator: GameCoordinator, sound: PhonicsSound) {
        self.sound = sound
        letter = Sketch.letter(sound.grapheme, size: 380, shadow: Palette.sun)
        super.init(coordinator: coordinator)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }

    override func didMove(to view: SKView) {
        addHomeButton()
        addBip(at: CGPoint(x: -560, y: -400), scale: 0.95)

        letter.name = "tap:letter"
        letter.position = CGPoint(x: -60, y: 60)
        letter.setScale(0.01)
        letter.zPosition = 10
        addChild(letter)

        nextButton.position = CGPoint(x: 600, y: -320)
        nextButton.isHidden = true
        nextButton.zPosition = 10
        addChild(nextButton)

        // Clicks wait until the introduction has finished, so it can't be cut short.
        inputLocked = true
        after(0.4) { [weak self] in self?.introduce() }
    }

    private func introduce() {
        sfx.play(.whirr)
        let grow = SKAction.scale(to: 1.15, duration: 0.25)
        grow.timingMode = .easeOut
        letter.run(.sequence([grow, .scale(to: 0.95, duration: 0.12), .scale(to: 1, duration: 0.1)]))
        bip.celebrate()

        let clips = [VoiceLine.meetNewSound.rawValue, sound.soundClip, sound.soundClip, sound.soundClip]
        voice.play(clips, onClipStart: { [weak self] _, clip in
            self?.reactToClip(clip)
        }, completion: { [weak self] in
            self?.after(0.4) { self?.showPicture() }
        })
    }

    private func showPicture() {
        let card = PictureCard(word: sound.pictureWord, seed: 800)
        card.name = "tap:picture"
        card.position = CGPoint(x: 470, y: 60)
        card.setScale(0.01)
        card.zPosition = 10
        addChild(card)
        picture = card
        card.run(.sequence([.scale(to: 1.1, duration: 0.25), .scale(to: 1, duration: 0.12)]))
        sfx.play(.chime)

        voice.play([sound.wordClip, sound.soundClip, VoiceLine.sayItWithMe.rawValue], onClipStart: { [weak self] _, clip in
            self?.reactToClip(clip)
        }, completion: { [weak self] in
            // Leave a moment for the child to say it, then model it once more.
            self?.after(1.6) {
                guard let self else { return }
                self.voice.play([self.sound.soundClip, VoiceLine.clickToHearAgain.rawValue], onClipStart: { [weak self] _, clip in
                    self?.reactToClip(clip)
                }, completion: { [weak self] in
                    self?.showNextButton()
                })
            }
        })
    }

    private func reactToClip(_ clip: String) {
        if clip == sound.soundClip {
            letter.run(.sequence([.scale(to: 1.12, duration: 0.12), .scale(to: 1, duration: 0.18)]))
            bip.hop()
        } else if clip == sound.wordClip {
            picture?.run(.sequence([.scale(to: 1.08, duration: 0.12), .scale(to: 1, duration: 0.18)]))
        }
    }

    private func showNextButton() {
        guard nextButton.isHidden else { return }
        inputLocked = false
        nextButton.isHidden = false
        nextButton.setScale(0.01)
        nextButton.run(.sequence([.scale(to: 1, duration: 0.25), Buttons.pulse()]))
    }

    override func handleTap(name: String, node: SKNode) {
        switch name {
        case "tap:letter":
            voice.play([sound.soundClip], onClipStart: { [weak self] _, clip in self?.reactToClip(clip) })
        case "tap:picture":
            voice.play([sound.wordClip, sound.soundClip], onClipStart: { [weak self] _, clip in self?.reactToClip(clip) })
        case "tap:next":
            inputLocked = true
            sfx.play(.chime)
            Buttons.press(node)
            coordinator.markMet(sound)
            bip.celebrate()
            voice.play([coordinator.randomPraise()], completion: { [weak self] in
                self?.coordinator.showLettersIsland(greet: false)
            })
        default:
            break
        }
    }

    override func replayPrompt() {
        guard !inputLocked else { return }
        voice.play([sound.soundClip], onClipStart: { [weak self] _, clip in self?.reactToClip(clip) })
    }

    override func goHome() {
        coordinator.showLettersIsland()
    }
}
