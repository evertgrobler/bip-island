import BipCore
import SpriteKit

/// "Sound hunt": "Find the picture that starts with… sss" — click the right one of three pictures.
/// Wrong → soft boop and try again; two misses → spoken hint and the right card wiggles.
final class SoundHuntScene: BaseScene {
    private let sound: PhonicsSound
    private var question: HuntQuestion?
    private var attempt = QuestionAttempt()
    private var cards: [PictureCard] = []
    private var questionsAsked = 0
    private static let cardPositions = [CGPoint(x: -400, y: 60), CGPoint(x: 0, y: 60), CGPoint(x: 400, y: 60)]

    init(coordinator: GameCoordinator, sound: PhonicsSound) {
        self.sound = sound
        super.init(coordinator: coordinator)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }

    override func didMove(to view: SKView) {
        addHomeButton()
        addBip(at: CGPoint(x: -560, y: -420), scale: 0.8)
        let replay = Buttons.replay()
        replay.position = CGPoint(x: 600, y: -330)
        replay.zPosition = 10
        addChild(replay)
        after(0.5) { [weak self] in self?.askQuestion() }
    }

    private func askQuestion() {
        cards.forEach { $0.removeFromParent() }
        attempt = QuestionAttempt()
        let q = coordinator.planner.makeHuntQuestion(target: sound, using: &coordinator.rng)
        question = q
        cards = q.choices.enumerated().map { index, choice in
            let card = PictureCard(word: choice.pictureWord, seed: 900 + UInt64(questionsAsked * 7 + index))
            card.name = "tap:card:\(index)"
            card.position = Self.cardPositions[index]
            card.zPosition = 10
            card.setScale(0.01)
            addChild(card)
            card.run(.sequence([.wait(forDuration: 0.12 * Double(index)), .scale(to: 1.08, duration: 0.2), .scale(to: 1, duration: 0.1)]))
            return card
        }
        sfx.play(.beep)
        inputLocked = false
        sayPrompt()
    }

    private func sayPrompt() {
        voice.play([VoiceLine.findTheSound.rawValue, sound.soundClip])
        bip.hop()
    }

    override func handleTap(name: String, node: SKNode) {
        if name == "tap:replay" {
            Buttons.press(node)
            sayPrompt()
            return
        }
        guard name.hasPrefix("tap:card:"), let index = Int(name.dropFirst("tap:card:".count)),
              let question, index < question.choices.count else { return }
        let card = cards[index]
        let isRight = question.choices[index] == question.target

        switch attempt.answer(correct: isRight) {
        case let .correct(firstTry):
            inputLocked = true
            questionsAsked += 1
            card.removeAllActions()
            card.zRotation = 0
            card.run(.sequence([.scale(to: 1.15, duration: 0.15), .scale(to: 1.05, duration: 0.1)]))
            Buttons.sparkle(at: card.position, in: self)
            sfx.play(.chime)
            bip.celebrate()
            let change = coordinator.record(correct: firstTry, for: sound)
            voice.play([coordinator.randomPraise(), question.target.wordClip]) { [weak self] in
                self?.afterAnswer(change)
            }
        case .tryAgain:
            sfx.play(.boop)
            card.run(Buttons.shake())
            bip.tilt()
            after(0.4) { [weak self] in
                guard let self else { return }
                self.voice.play([self.sound.soundClip])
            }
        case .hint:
            sfx.play(.boop)
            card.run(Buttons.shake())
            bip.tilt()
            if let rightIndex = question.choices.firstIndex(of: question.target) {
                cards[rightIndex].run(Buttons.hintWiggle(), withKey: "hint")
            }
            after(0.4) { [weak self] in
                guard let self else { return }
                self.voice.play([self.coordinator.randomHint(), self.sound.soundClip])
            }
        }
    }

    private func afterAnswer(_ change: MasteryChange) {
        switch change {
        case .levelledUp:
            sfx.play(.whirr)
            bip.celebrate()
            voice.play([VoiceLine.levelUp.rawValue]) { [weak self] in self?.finish() }
        case .droppedBack:
            voice.play([VoiceLine.letsPractiseAgain.rawValue]) { [weak self] in self?.finish() }
        case .none:
            if questionsAsked >= LessonPlanner.questionsPerRound {
                voice.play([VoiceLine.roundDone.rawValue]) { [weak self] in self?.finish() }
            } else {
                after(0.3) { [weak self] in self?.askQuestion() }
            }
        }
    }

    private func finish() {
        coordinator.showLettersIsland(greet: false)
    }

    override func replayPrompt() {
        guard !inputLocked else { return }
        sayPrompt()
    }

    override func goHome() {
        coordinator.showLettersIsland()
    }
}
