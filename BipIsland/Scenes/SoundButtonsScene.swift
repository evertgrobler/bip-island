import BipCore
import SpriteKit

/// Sound Buttons: push each letter button to hear its sound, then tap the picture of the word
/// they make together. Buttons are free play; only the picture counts towards mastery.
/// Wrong → soft boop and try again; two misses → the word is said and the picture wiggles.
final class SoundButtonsScene: BaseScene {
    private let game: SoundButtonsGame
    private var session: GameSession
    private let learner: Learner
    private let course: PhonicsCourse
    private var round: SoundButtonsGame.Round?
    private var attempt = QuestionAttempt()
    private var tiles: [SKNode] = []
    private var cards: [PictureCard] = []
    private var clips: [String] = []
    private static let cardPositions = [CGPoint(x: -400, y: -140), CGPoint(x: 0, y: -140), CGPoint(x: 400, y: -140)]

    init(coordinator: GameCoordinator, game: SoundButtonsGame, session: GameSession, learner: Learner, course: PhonicsCourse) {
        self.game = game
        self.session = session
        self.learner = learner
        self.course = course
        super.init(coordinator: coordinator)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }

    override func didMove(to view: SKView) {
        addHomeButton()
        addBip(at: CGPoint(x: -560, y: -400), scale: 0.8)
        let replay = Buttons.replay()
        replay.position = CGPoint(x: 600, y: -330)
        replay.zPosition = 10
        addChild(replay)
        after(0.5) { [weak self] in self?.askQuestion() }
    }

    private func askQuestion() {
        guard let next = session.nextRound(of: game, for: learner, using: &coordinator.rng) else {
            return endVisit(with: .roundDone)
        }
        tiles.forEach { $0.removeFromParent() }
        cards.forEach { $0.removeFromParent() }
        tiles = []
        cards = []
        attempt = QuestionAttempt()
        round = next
        clips = next.word.graphemes.compactMap { course.sound(id: $0)?.soundClip }

        let tileCount = CGFloat(max(clips.count, 1))
        for (i, grapheme) in next.word.graphemes.enumerated() {
            let tile = SKNode()
            tile.name = "tap:tile:\(i)"
            tile.position = CGPoint(x: (CGFloat(i) - (tileCount - 1) / 2) * 170, y: 220)
            tile.zPosition = 10
            tile.addChild(Sketch.node(.ellipse(center: .zero, rx: 72, ry: 72), fill: Palette.sun, lineWidth: 6, seed: 950 + UInt64(i)))
            let text = Sketch.letter(grapheme, size: 88, shadow: Palette.orange)
            tile.addChild(text)
            tile.setScale(0.01)
            addChild(tile)
            tiles.append(tile)
            tile.run(.sequence([.wait(forDuration: 0.1 * Double(i)), .scale(to: 1, duration: 0.2)]))
        }
        for (index, choice) in next.choices.enumerated() {
            let card = PictureCard(picture: choice.picture, word: choice.word, seed: 960 + UInt64(session.roundsPlayed * 7 + index))
            card.name = "tap:card:\(index)"
            card.position = Self.cardPositions[index % Self.cardPositions.count]
            card.zPosition = 10
            card.setScale(0.01)
            addChild(card)
            cards.append(card)
            card.run(.scale(to: 1, duration: 0.25))
        }
        sfx.play(.beep)
        inputLocked = false
        sayPrompt()
    }

    private func sayPrompt() {
        voice.play([VoiceLine.soundButtons.rawValue])
        bip.hop()
    }

    override func handleTap(name: String, node: SKNode) {
        if name == "tap:replay" {
            Buttons.press(node)
            sayPrompt()
            return
        }
        if name.hasPrefix("tap:tile:"), let index = Int(name.dropFirst("tap:tile:".count)), index < clips.count {
            node.run(.sequence([.scale(to: 1.12, duration: 0.1), .scale(to: 1, duration: 0.12)]))
            voice.play([clips[index]])
            return
        }
        guard name.hasPrefix("tap:card:"), let index = Int(name.dropFirst("tap:card:".count)),
              let round, index < round.choices.count, index < cards.count else { return }
        answer(picture: round.choices[index], card: cards[index])
    }

    private func answer(picture: HuntPicture, card: PictureCard) {
        guard let round else { return }
        switch attempt.answer(correct: game.isCorrect(picture, in: round)) {
        case let .correct(firstTry):
            inputLocked = true
            card.removeAllActions()
            card.zRotation = 0
            card.run(.sequence([.scale(to: 1.15, duration: 0.15), .scale(to: 1.05, duration: 0.1)]))
            Buttons.sparkle(at: card.position, in: self)
            sfx.play(.chime)
            bip.celebrate()
            let change = coordinator.record(correct: firstTry, skillID: game.skillID(for: round), soundID: nil)
            voice.play([coordinator.randomPraise(), round.answer.audio], completion: { [weak self] in
                self?.afterAnswer(change)
            })
        case .tryAgain:
            sfx.play(.boop)
            card.run(Buttons.shake())
            bip.tilt()
        case .hint:
            sfx.play(.boop)
            card.run(Buttons.shake())
            bip.tilt()
            if let rightIndex = round.choices.firstIndex(where: { game.isCorrect($0, in: round) }), rightIndex < cards.count {
                cards[rightIndex].run(Buttons.hintWiggle(), withKey: "hint")
            }
            after(0.4) { [weak self] in
                guard let self, let round = self.round else { return }
                self.voice.play([self.coordinator.randomHint(), round.answer.audio])
            }
        }
    }

    private enum Ending { case levelUp, practiseAgain, roundDone }

    private func afterAnswer(_ change: MasteryChange) {
        switch change {
        case .levelledUp:
            endVisit(with: .levelUp)
        case .droppedBack:
            endVisit(with: .practiseAgain)
        case .none:
            if session.isFinished {
                endVisit(with: .roundDone)
            } else {
                after(0.3) { [weak self] in self?.askQuestion() }
            }
        }
    }

    private func endVisit(with ending: Ending) {
        inputLocked = true
        switch ending {
        case .levelUp:
            sfx.play(.whirr)
            bip.celebrate()
            voice.play([VoiceLine.levelUp.rawValue], completion: { [weak self] in self?.finish() })
        case .practiseAgain:
            voice.play([VoiceLine.letsPractiseAgain.rawValue], completion: { [weak self] in self?.finish() })
        case .roundDone:
            voice.play([VoiceLine.roundDone.rawValue], completion: { [weak self] in self?.finish() })
        }
    }

    private func finish() {
        coordinator.showWordsIsland(greet: false)
    }

    override func replayPrompt() {
        guard !inputLocked else { return }
        sayPrompt()
    }

    override func goHome() {
        coordinator.showWordsIsland()
    }
}
