import BipCore
import SpriteKit

/// Count & Tap: tap each animal as Bip counts aloud, then tap the numeral that says how many.
/// Tapping animals is free play; only the numeral counts towards mastery.
/// Wrong → soft boop and try again; two misses → the number is said again and wiggles.
final class CountTapScene: BaseScene {
    private let game: CountTapGame
    private var session: GameSession
    private let learner: Learner
    private var round: CountTapGame.Round?
    private var attempt = QuestionAttempt()
    private var beasts: [SKNode] = []
    private var tapped: Set<Int> = []
    private var numerals: [SKNode] = []
    private static let numeralPositions = [CGPoint(x: -350, y: -260), CGPoint(x: 0, y: -260), CGPoint(x: 350, y: -260)]

    init(coordinator: GameCoordinator, game: CountTapGame, session: GameSession, learner: Learner) {
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
        beasts.forEach { $0.removeFromParent() }
        numerals.forEach { $0.removeFromParent() }
        beasts = []
        numerals = []
        tapped = []
        attempt = QuestionAttempt()
        resetKeys()
        round = next

        let perRow = 5
        for i in 0..<next.count {
            let picture = PictureNode.make(picture: next.object.picture, word: next.object.id)
            picture.setScale(0.55)
            let holder = SKNode()
            holder.name = "tap:beast:\(i)"
            holder.position = CGPoint(x: -400 + CGFloat(i % perRow) * 200, y: 260 - CGFloat(i / perRow) * 190)
            holder.zPosition = 10
            holder.addChild(picture)
            addChild(holder)
            beasts.append(holder)
        }
        inputLocked = false
        sayPrompt()
    }

    private func sayPrompt() {
        voice.play([VoiceLine.countTap.rawValue])
        bip.hop()
    }

    private func showNumerals() {
        guard let round else { return }
        for (i, numeral) in round.choices.enumerated() {
            let button = SKNode()
            button.name = "tap:num:\(i)"
            button.position = Self.numeralPositions[i % Self.numeralPositions.count]
            button.zPosition = 10
            button.addChild(Sketch.node(.ellipse(center: .zero, rx: 95, ry: 95), fill: Palette.card, lineWidth: 6, seed: 920 + UInt64(i)))
            let text = Sketch.letter("\(numeral)", size: 120, shadow: Palette.red)
            button.addChild(text)
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
        if name.hasPrefix("tap:beast:"), let index = Int(name.dropFirst("tap:beast:".count)), index < beasts.count {
            tapBeast(at: index)
            return
        }
        guard name.hasPrefix("tap:num:"), let index = Int(name.dropFirst("tap:num:".count)),
              let round, index < round.choices.count, index < numerals.count else { return }
        answer(numeral: round.choices[index], node: numerals[index])
    }

    private func tapBeast(at index: Int) {
        guard tapped.count < (round?.count ?? 0), !tapped.contains(index), index < beasts.count else { return }
        tapped.insert(index)
        let beast = beasts[index]
        beast.run(.sequence([.scale(to: 1.15, duration: 0.12), .scale(to: 0.85, duration: 0.12)]))
        sfx.play(.tick)
        voice.play([AudioCatalogue.numberClip(tapped.count)])
        if tapped.count == round?.count {
            after(0.9) { [weak self] in self?.showNumerals() }
        }
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
            voice.play([VoiceLine.levelUp.rawValue], completion: { [weak self] in self?.finishVisit { self?.finish() } })
        case .practiseAgain:
            voice.play([VoiceLine.letsPractiseAgain.rawValue], completion: { [weak self] in self?.finishVisit { self?.finish() } })
        case .roundDone:
            voice.play([VoiceLine.roundDone.rawValue], completion: { [weak self] in self?.finishVisit { self?.finish() } })
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
