import BipCore
import SpriteKit

/// Morning Order: put the picture cards in order — what comes first? The shuffled cards wait
/// in a row at the bottom; tapping one sends it up to the next numbered space (1, 2, 3…) and
/// says what it shows. Tapping a placed card sends it back down. When every space is full the
/// order is checked: wrong → soft boop and the cards from the first mistake slide back down;
/// two misses → the card that belongs next wiggles and the routine is said in order.
/// Tapping instead of dragging: small hands miss drop targets, and a click always lands.
final class MorningOrderScene: BaseScene {
    private let game: MorningOrderGame
    private var session: GameSession
    private let learner: Learner
    private var round: MorningOrderGame.Round?
    private var attempt = QuestionAttempt()
    /// One holder per card (named tap:card:<i>), in the round's card order.
    private var holders: [SKNode] = []
    private var trayPositions: [CGPoint] = []
    private var slotPositions: [CGPoint] = []
    /// Which card (index into holders) sits in each numbered space.
    private var slots: [Int?] = []
    private var slotMarks: [SKNode] = []

    private static let slotY: CGFloat = 150
    private static let trayY: CGFloat = -190

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
        addBip(at: CGPoint(x: -700, y: -400), scale: 0.7)
        let replay = Buttons.replay()
        replay.position = CGPoint(x: 700, y: -400)
        replay.zPosition = 10
        addChild(replay)
        after(0.5) { [weak self] in self?.askQuestion() }
    }

    /// Arrows move between the cards still waiting, then the placed ones; Enter taps.
    override var keyOptions: [SKNode] {
        let waiting = holders.indices.filter { !slots.contains($0) }.sorted { trayPositions[$0].x < trayPositions[$1].x }
        let placed = slots.compactMap { $0 }
        return (waiting + placed).map { holders[$0] }
    }

    private func askQuestion() {
        guard let next = session.nextRound(of: game, for: learner, using: &coordinator.rng) else {
            return endVisit(with: .roundDone)
        }
        holders.forEach { $0.removeFromParent() }
        slotMarks.forEach { $0.removeFromParent() }
        holders = []
        slotMarks = []
        attempt = QuestionAttempt()
        resetKeys()
        round = next

        let cards = next.set.cards
        let count = cards.count
        // Six cards still fit across the screen with room between them.
        let scale: CGFloat = count <= 4 ? 1 : (count == 5 ? 0.86 : 0.72)
        let spacing = PictureCard.size * scale + 40
        let xs = (0..<count).map { (CGFloat($0) - CGFloat(count - 1) / 2) * spacing }
        slotPositions = xs.map { CGPoint(x: $0, y: Self.slotY) }
        slots = Array(repeating: nil, count: count)

        // Empty numbered spaces along the top, with an arrow showing which way the story goes.
        let half = PictureCard.size * scale / 2
        for (i, point) in slotPositions.enumerated() {
            let mark = SKNode()
            mark.position = point
            mark.zPosition = 2
            mark.addChild(Sketch.node(.roundedRect(CGRect(x: -half, y: -half, width: half * 2, height: half * 2), radius: 26 * scale),
                                      fill: Palette.stone.withAlphaComponent(0.35), ink: Palette.ink.withAlphaComponent(0.3),
                                      lineWidth: 4, wobble: 3, seed: 960 + UInt64(i)))
            let badge = SKNode()
            badge.position = CGPoint(x: 0, y: half + 44)
            badge.addChild(Sketch.node(.ellipse(center: .zero, rx: 34, ry: 34), fill: Palette.sun, lineWidth: 4.5, seed: 970 + UInt64(i)))
            badge.addChild(Sketch.letter("\(i + 1)", size: 46, shadow: nil))
            mark.addChild(badge)
            addChild(mark)
            slotMarks.append(mark)
        }

        // Shuffled along the bottom, never already in the right order.
        var trayOrder = Array(0..<count)
        while trayOrder == Array(0..<count) && count > 1 {
            trayOrder.shuffle(using: &coordinator.rng)
        }
        trayPositions = Array(repeating: .zero, count: count)
        for (spot, index) in trayOrder.enumerated() {
            trayPositions[index] = CGPoint(x: xs[spot], y: Self.trayY)
        }
        for (i, card) in cards.enumerated() {
            let holder = SKNode()
            holder.name = "tap:card:\(i)"
            let picture = PictureCard(picture: card.picture, word: card.text, seed: 980 + UInt64(session.roundsPlayed * 11 + i))
            picture.setScale(scale)
            holder.addChild(picture)
            holder.position = trayPositions[i]
            holder.zPosition = 10
            holder.alpha = 0
            holder.run(.sequence([.wait(forDuration: 0.08 * Double(i)), .fadeIn(withDuration: 0.25)]))
            addChild(holder)
            holders.append(holder)
        }
        inputLocked = false
        sayPrompt()
    }

    private func sayPrompt() {
        voice.play([VoiceLine.morningOrder.rawValue])
        bip.hop()
    }

    // MARK: Tapping cards

    override func handleTap(name: String, node: SKNode) {
        if name == "tap:replay" {
            Buttons.press(node)
            sayPrompt()
            return
        }
        guard name.hasPrefix("tap:card:"), let index = Int(name.dropFirst("tap:card:".count)),
              holders.indices.contains(index), let round else { return }
        let holder = holders[index]
        settle(holder)

        if let slot = slots.firstIndex(of: index) {
            // Placed already: back down to the bottom row.
            slots[slot] = nil
            sfx.play(.pop)
            holder.run(.move(to: trayPositions[index], duration: 0.22))
            return
        }
        guard let empty = slots.firstIndex(of: nil) else { return }
        slots[empty] = index
        sfx.play(.tick)
        holder.zPosition = 12
        let move = SKAction.move(to: slotPositions[empty], duration: 0.25)
        move.timingMode = .easeOut
        holder.run(.sequence([move, .run { holder.zPosition = 10 }]))

        let card = round.set.cards[index]
        if slots.contains(nil) {
            voice.play([card.audio])
        } else {
            // Last space filled: say the card, then see if the story is in order.
            inputLocked = true
            voice.play([card.audio], completion: { [weak self] in self?.check(round: round) })
        }
    }

    /// Stop any hint wiggle and stand the card up straight.
    private func settle(_ holder: SKNode) {
        holder.removeAction(forKey: "hint")
        holder.run(.group([.rotate(toAngle: 0, duration: 0.1), .scale(to: 1, duration: 0.1)]))
    }

    /// The cards in the numbered spaces, in order.
    private func currentOrder(in round: MorningOrderGame.Round) -> [SequenceCard] {
        slots.compactMap { $0 }.map { round.set.cards[$0] }
    }

    private func check(round: MorningOrderGame.Round) {
        let order = currentOrder(in: round)
        switch attempt.answer(correct: game.isCorrect(order, in: round)) {
        case let .correct(firstTry):
            sfx.play(.chime)
            bip.celebrate()
            for holder in holders { Buttons.press(holder) }
            let change = coordinator.record(correct: firstTry, skillID: game.skillID(for: round), soundID: nil)
            voice.play([coordinator.randomPraise()], completion: { [weak self] in
                self?.afterAnswer(change)
            })
        case .tryAgain:
            sfx.play(.boop)
            bip.tilt()
            sendBack(from: firstMistake(in: order, round: round))
            after(0.5) { [weak self] in self?.inputLocked = false }
        case .hint:
            sfx.play(.boop)
            bip.tilt()
            let mistake = firstMistake(in: order, round: round)
            sendBack(from: mistake)
            // The card that belongs in the first wrong space wiggles.
            let wanted = round.answer[mistake]
            if let index = round.set.cards.firstIndex(of: wanted) {
                after(0.4) { [weak self] in
                    self?.holders[index].run(Buttons.hintWiggle(), withKey: "hint")
                }
            }
            after(0.4) { [weak self] in
                guard let self else { return }
                self.voice.play([self.coordinator.randomHint()] + round.answer.map(\.audio), completion: { [weak self] in
                    self?.inputLocked = false
                })
            }
        }
    }

    private func firstMistake(in order: [SequenceCard], round: MorningOrderGame.Round) -> Int {
        order.indices.first { order[$0] != round.answer[$0] } ?? 0
    }

    /// Everything from the first mistake on goes back to the bottom row; the right start stays.
    private func sendBack(from first: Int) {
        for slot in first..<slots.count {
            guard let index = slots[slot] else { continue }
            slots[slot] = nil
            let holder = holders[index]
            holder.run(.sequence([Buttons.shake(), .move(to: trayPositions[index], duration: 0.25)]))
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
