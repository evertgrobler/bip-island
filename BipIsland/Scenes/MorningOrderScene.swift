import BipCore
import SpriteKit

/// Morning Order: drag the picture cards into order — what comes first? — then tap the
/// green arrow. Wrong order → soft boop and try again; two misses → the first wrong card
/// wiggles and the routine is said in the right order.
final class MorningOrderScene: BaseScene {
    private let game: MorningOrderGame
    private var session: GameSession
    private let learner: Learner
    private var round: MorningOrderGame.Round?
    private var attempt = QuestionAttempt()
    private var cards: [PictureCard] = []
    private var slotCards: [PictureCard?] = []
    private var slotPositions: [CGPoint] = []
    private var dragged: PictureCard?
    private var dragOffset = CGPoint.zero
    private let nextButton = Buttons.next()

    init(coordinator: GameCoordinator, game: MorningOrderGame, session: GameSession, learner: Learner) {
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
        replay.position = CGPoint(x: 600, y: -400)
        replay.zPosition = 10
        addChild(replay)
        nextButton.position = CGPoint(x: 600, y: -180)
        nextButton.zPosition = 10
        addChild(nextButton)
        after(0.5) { [weak self] in self?.askQuestion() }
    }

    /// Enter checks the order, like the green arrow.
    override var keyOptions: [SKNode] { [nextButton] }

    private func askQuestion() {
        guard let next = session.nextRound(of: game, for: learner, using: &coordinator.rng) else {
            return endVisit(with: .roundDone)
        }
        cards.forEach { $0.removeFromParent() }
        cards = []
        slotCards = []
        slotPositions = []
        dragged = nil
        attempt = QuestionAttempt()
        round = next

        // Slots first, then the cards land shuffled on them.
        let count = CGFloat(next.set.cards.count)
        for i in 0..<next.set.cards.count {
            slotPositions.append(CGPoint(x: (CGFloat(i) - (count - 1) / 2) * 300, y: 80))
            slotCards.append(nil)
        }
        let startOrder = next.set.cards.shuffled(using: &coordinator.rng)
        for (i, card) in startOrder.enumerated() {
            let node = PictureCard(picture: card.picture, word: card.text, seed: 980 + UInt64(session.roundsPlayed * 11 + i))
            node.name = "tap:card:\(i)"
            node.position = slotPositions[i]
            node.zPosition = 10
            addChild(node)
            cards.append(node)
            slotCards[i] = node
        }
        inputLocked = false
        sayPrompt()
    }

    private func sayPrompt() {
        voice.play([VoiceLine.morningOrder.rawValue])
        bip.hop()
    }

    // MARK: Dragging

    override func mouseDown(with event: NSEvent) {
        super.mouseDown(with: event)
        guard !inputLocked, dragged == nil else { return }
        let point = event.location(in: self)
        for card in cards where card.contains(point) {
            dragged = card
            dragOffset = CGPoint(x: card.position.x - point.x, y: card.position.y - point.y)
            card.zPosition = 20
            card.removeAction(forKey: "hint")
            break
        }
    }

    override func mouseDragged(with event: NSEvent) {
        guard let card = dragged, !inputLocked else { return }
        let point = event.location(in: self)
        card.position = CGPoint(x: point.x + dragOffset.x, y: point.y + dragOffset.y)
    }

    override func mouseUp(with event: NSEvent) {
        guard let card = dragged else { return }
        dragged = nil
        guard !inputLocked else { return }
        card.zPosition = 10
        var best = -1
        var bestDistance = CGFloat(160 * 160)
        for (i, slot) in slotPositions.enumerated() {
            let dx = card.position.x - slot.x
            let dy = card.position.y - slot.y
            let distance = dx * dx + dy * dy
            if distance < bestDistance {
                bestDistance = distance
                best = i
            }
        }
        guard best >= 0 else { return }
        if let from = slotCards.firstIndex(where: { $0 === card }) {
            slotCards[from] = nil
        }
        if let occupant = slotCards[best] {
            let fromPosition = card.position
            occupant.run(.move(to: fromPosition, duration: 0.2))
            if let from = slotCards.firstIndex(where: { $0 === occupant }) {
                slotCards[from] = nil
            }
            // The occupant keeps the dragged card's old slot once it arrives.
            slotCards[best] = card
            card.run(.move(to: slotPositions[best], duration: 0.2))
            if let old = nearestSlot(to: fromPosition, excluding: best) {
                slotCards[old] = occupant
                occupant.run(.move(to: slotPositions[old], duration: 0.2))
            }
        } else {
            slotCards[best] = card
            card.run(.move(to: slotPositions[best], duration: 0.15))
        }
        sfx.play(.tick)
    }

    private func nearestSlot(to point: CGPoint, excluding: Int) -> Int? {
        var best: Int?
        var bestDistance = CGFloat.greatestFiniteMagnitude
        for (i, slot) in slotPositions.enumerated() where i != excluding && slotCards[i] == nil {
            let dx = point.x - slot.x
            let dy = point.y - slot.y
            let distance = dx * dx + dy * dy
            if distance < bestDistance {
                bestDistance = distance
                best = i
            }
        }
        return best
    }

    private func currentOrder() -> [SequenceCard] {
        slotCards.compactMap { node in round?.set.cards.first { $0.text == node?.cardWord } }
    }

    override func handleTap(name: String, node: SKNode) {
        if name == "tap:replay" {
            Buttons.press(node)
            sayPrompt()
            return
        }
        guard name == "tap:next", let round else { return }
        Buttons.press(node)
        check(round: round)
    }

    private func check(round: MorningOrderGame.Round) {
        guard slotCards.allSatisfy({ $0 != nil }) else {
            sfx.play(.boop)
            return
        }
        switch attempt.answer(correct: game.isCorrect(currentOrder(), in: round)) {
        case let .correct(firstTry):
            inputLocked = true
            sfx.play(.chime)
            bip.celebrate()
            let change = coordinator.record(correct: firstTry, skillID: game.skillID(for: round), soundID: nil)
            voice.play([coordinator.randomPraise()], completion: { [weak self] in
                self?.afterAnswer(change)
            })
        case .tryAgain:
            sfx.play(.boop)
            bip.tilt()
            for card in cards { card.run(Buttons.shake()) }
        case .hint:
            sfx.play(.boop)
            bip.tilt()
            let order = currentOrder()
            for (i, card) in order.enumerated() where card != round.answer[i] {
                if let node = slotCards[i] { node.run(Buttons.hintWiggle(), withKey: "hint") }
                break
            }
            after(0.4) { [weak self] in
                guard let self, let round = self.round else { return }
                self.voice.play([self.coordinator.randomHint()] + round.answer.map(\.audio))
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
        coordinator.showCodingIsland(greet: false)
    }

    override func replayPrompt() {
        guard !inputLocked else { return }
        sayPrompt()
    }

    override func goHome() {
        coordinator.showCodingIsland()
    }
}

private extension PictureCard {
    var cardWord: String { word }
}
