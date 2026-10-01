import BipCore
import SpriteKit

/// Sound Hunt: "Find the picture that starts with… sss" — click the right one of three pictures.
/// Each question is a fresh round from `SoundHuntGame`; the word to find never repeats in a visit.
/// Wrong → soft boop and try again; two misses → spoken hint and the right card wiggles.
final class SoundHuntScene: BaseScene {
    private let game: SoundHuntGame
    private var session: GameSession
    private let learner: Learner
    /// The sound Bip chose this visit for; a level change on it ends the visit.
    private let focus: PhonicsSound
    private var round: SoundHuntGame.Round?
    private var attempt = QuestionAttempt()
    private var cards: [PictureCard] = []
    /// Three pictures 400 apart; four (a higher level) 350 apart, still 270 pt cards.
    private static func cardPositions(count: Int) -> [CGPoint] {
        let spacing: CGFloat = count > 3 ? 350 : 400
        return (0..<count).map { CGPoint(x: (CGFloat($0) - CGFloat(count - 1) / 2) * spacing, y: 60) }
    }

    init(coordinator: GameCoordinator, game: SoundHuntGame, session: GameSession, learner: Learner, focus: PhonicsSound) {
        self.game = game
        self.session = session
        self.learner = learner
        self.focus = focus
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

    override var keyOptions: [SKNode] { cards }

    private func askQuestion() {
        guard let next = session.nextRound(of: game, for: learner, using: &coordinator.rng) else {
            // Nothing fresh left to ask (or the visit is over).
            return endVisit(with: .roundDone)
        }
        cards.forEach { $0.removeFromParent() }
        attempt = QuestionAttempt()
        resetKeys()
        round = next
        cards = next.choices.enumerated().map { index, choice in
            let card = PictureCard(picture: choice.picture, word: choice.word, seed: 900 + UInt64(session.roundsPlayed * 7 + index))
            card.name = "tap:card:\(index)"
            card.position = Self.cardPositions(count: next.choices.count)[index]
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
        guard let round else { return }
        voice.play([VoiceLine.findTheSound.rawValue, round.target.soundClip])
        bip.hop()
    }

    override func handleTap(name: String, node: SKNode) {
        if name == "tap:replay" {
            Buttons.press(node)
            sayPrompt()
            return
        }
        guard name.hasPrefix("tap:card:"), let index = Int(name.dropFirst("tap:card:".count)),
              let round, index < round.choices.count, index < cards.count else { return }
        let card = cards[index]

        switch attempt.answer(correct: game.isCorrect(round.choices[index], in: round)) {
        case let .correct(firstTry):
            inputLocked = true
            card.removeAllActions()
            card.zRotation = 0
            card.run(.sequence([.scale(to: 1.15, duration: 0.15), .scale(to: 1.05, duration: 0.1)]))
            Buttons.sparkle(at: card.position, in: self)
            sfx.play(.chime)
            bip.celebrate()
            let change = coordinator.record(correct: firstTry, skillID: game.skillID(for: round), soundID: round.target.id)
            let focusChanged = round.target.id == focus.id && change != .none
            voice.play([coordinator.randomPraise(), round.answer.audio], completion: { [weak self] in
                self?.afterAnswer(focusChanged ? change : .none)
            })
        case .tryAgain:
            sfx.play(.boop)
            card.run(Buttons.shake())
            bip.tilt()
            after(0.4) { [weak self] in
                guard let self, let round = self.round else { return }
                self.voice.play([round.target.soundClip])
            }
        case .hint:
            sfx.play(.boop)
            card.run(Buttons.shake())
            bip.tilt()
            if let rightIndex = round.choices.firstIndex(where: { game.isCorrect($0, in: round) }), rightIndex < cards.count {
                cards[rightIndex].run(Buttons.hintWiggle(), withKey: "hint")
            }
            after(0.4) { [weak self] in
                guard let self, let round = self.round else { return }
                self.voice.play([self.coordinator.randomHint(), round.target.soundClip])
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
            voice.play([VoiceLine.levelUp.rawValue], completion: { [weak self] in self?.finishVisit { self?.finish() } })
        case .practiseAgain:
            voice.play([VoiceLine.letsPractiseAgain.rawValue], completion: { [weak self] in self?.finishVisit { self?.finish() } })
        case .roundDone:
            voice.play([VoiceLine.roundDone.rawValue], completion: { [weak self] in self?.finishVisit { self?.finish() } })
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
