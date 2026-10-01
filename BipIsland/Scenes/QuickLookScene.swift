import BipCore
import SpriteKit

/// Quick Look: dots flash for two seconds — how many, without counting?
/// (The flash is display time, not a countdown: wrong answers still get retries and hints.)
/// Then tap the numeral, like Count & Tap.
final class QuickLookScene: BaseScene {
    private let game: QuickLookGame
    private var session: GameSession
    private let learner: Learner
    private var round: QuickLookGame.Round?
    private var attempt = QuestionAttempt()
    private var dots: [SKNode] = []
    private var numerals: [SKNode] = []
    private static let numeralPositions = [CGPoint(x: -350, y: -260), CGPoint(x: 0, y: -260), CGPoint(x: 350, y: -260)]

    init(coordinator: GameCoordinator, game: QuickLookGame, session: GameSession, learner: Learner) {
        self.game = game
        self.session = session
        self.learner = learner
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

    override var keyOptions: [SKNode] { numerals }

    private func askQuestion() {
        guard let next = session.nextRound(of: game, for: learner, using: &coordinator.rng) else {
            return endVisit(with: .roundDone)
        }
        dots.forEach { $0.removeFromParent() }
        numerals.forEach { $0.removeFromParent() }
        dots = []
        numerals = []
        attempt = QuestionAttempt()
        resetKeys()
        round = next

        // Dice-like spots, so small numbers read as patterns.
        let spots = [CGPoint.zero, CGPoint(x: -90, y: 90), CGPoint(x: 90, y: 90), CGPoint(x: -90, y: -90),
                     CGPoint(x: 90, y: -90), CGPoint(x: -180, y: 90), CGPoint(x: 180, y: 90),
                     CGPoint(x: -180, y: -90), CGPoint(x: 180, y: -90), CGPoint(x: 0, y: 180)]
        for i in 0..<next.count {
            let spot = spots[i % spots.count]
            let dot = Sketch.node(.ellipse(center: CGPoint(x: spot.x, y: spot.y + 120), rx: 42, ry: 42),
                                  fill: Palette.red, lineWidth: 4, seed: 930 + UInt64(i))
            dot.zPosition = 10
            addChild(dot)
            dots.append(dot)
        }
        inputLocked = true
        sayPrompt()
        after(2.0) { [weak self] in self?.hideDots() }
    }

    private func sayPrompt() {
        voice.play([VoiceLine.quickLook.rawValue])
        bip.hop()
    }

    private func hideDots() {
        for dot in dots {
            dot.run(.sequence([.scale(to: 0.01, duration: 0.2), .removeFromParent()]))
        }
        dots = []
        showNumerals()
        inputLocked = false
    }

    private func showNumerals() {
        guard let round else { return }
        for (i, numeral) in round.choices.enumerated() {
            let button = SKNode()
            button.name = "tap:num:\(i)"
            button.position = Self.numeralPositions[i % Self.numeralPositions.count]
            button.zPosition = 10
            button.addChild(Sketch.node(.ellipse(center: .zero, rx: 95, ry: 95), fill: Palette.card, lineWidth: 6, seed: 940 + UInt64(i)))
            button.addChild(Sketch.letter("\(numeral)", size: 120, shadow: Palette.red))
            button.setScale(0.01)
            addChild(button)
            numerals.append(button)
            button.run(.scale(to: 1, duration: 0.25))
        }
        sfx.play(.chime)
    }

    override func handleTap(name: String, node: SKNode) {
        if name == "tap:replay" {
            Buttons.press(node)
            sayPrompt()
            return
        }
        guard name.hasPrefix("tap:num:"), let index = Int(name.dropFirst("tap:num:".count)),
              let round, index < round.choices.count, index < numerals.count else { return }
        answer(numeral: round.choices[index], node: numerals[index])
    }

    private func answer(numeral: Int, node: SKNode) {
        guard let round else { return }
        switch attempt.answer(correct: game.isCorrect(numeral, in: round)) {
        case let .correct(firstTry):
            inputLocked = true
            node.run(.sequence([.scale(to: 1.15, duration: 0.15), .scale(to: 1.05, duration: 0.1)]))
            Buttons.sparkle(at: node.position, in: self)
            sfx.play(.chime)
            bip.celebrate()
            let change = coordinator.record(correct: firstTry, skillID: game.skillID(for: round), soundID: nil)
            voice.play([coordinator.randomPraise(), AudioCatalogue.numberClip(round.count)], completion: { [weak self] in
                self?.afterAnswer(change)
            })
        case .tryAgain:
            sfx.play(.boop)
            node.run(Buttons.shake())
            bip.tilt()
        case .hint:
            sfx.play(.boop)
            node.run(Buttons.shake())
            bip.tilt()
            if let rightIndex = round.choices.firstIndex(where: { game.isCorrect($0, in: round) }), rightIndex < numerals.count {
                numerals[rightIndex].run(Buttons.hintWiggle(), withKey: "hint")
            }
            after(0.4) { [weak self] in
                guard let self, let round = self.round else { return }
                self.voice.play([self.coordinator.randomHint(), AudioCatalogue.numberClip(round.count)])
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
        coordinator.showNumbersIsland(greet: false)
    }

    override func replayPrompt() {
        guard !inputLocked else { return }
        sayPrompt()
    }

    override func goHome() {
        coordinator.showNumbersIsland()
    }
}
