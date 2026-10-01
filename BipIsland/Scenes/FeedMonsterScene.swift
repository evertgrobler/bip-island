import BipCore
import SpriteKit

/// Feed the Monster: drag only the foods that start with the sound into a hungry monster.
/// Each question is a fresh round from `FeedMonsterGame`; the food never repeats in a visit.
/// Wrong → soft boop and try again; two misses → spoken hint and the right food wiggles.
final class FeedMonsterScene: BaseScene {
    private let game: FeedMonsterGame
    private var session: GameSession
    private let learner: Learner
    /// The sound Bip chose this visit for; a level change on it ends the visit.
    private let focus: PhonicsSound
    private var round: FeedMonsterGame.Round?
    private var attempt = QuestionAttempt()
    private var cards: [PictureCard] = []
    private var homes: [CGPoint] = []
    private var dragged: PictureCard?
    private var dragOffset = CGPoint.zero
    private var monsterMouth = CGPoint(x: 520, y: 60)
    /// Three foods in a row; four (a higher level) in a 2 × 2 grid, slightly smaller, left of the monster.
    private static func cardPositions(count: Int) -> [CGPoint] {
        if count <= 3 { return Array([CGPoint(x: -500, y: 60), CGPoint(x: -150, y: 60), CGPoint(x: 200, y: 60)].prefix(count)) }
        return [CGPoint(x: -420, y: 190), CGPoint(x: -100, y: 190), CGPoint(x: -420, y: -110), CGPoint(x: -100, y: -110)]
    }

    init(coordinator: GameCoordinator, game: FeedMonsterGame, session: GameSession, learner: Learner, focus: PhonicsSound) {
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
        addBip(at: CGPoint(x: -560, y: -400), scale: 0.8)

        let body = Sketch.node(.ellipse(center: CGPoint(x: 520, y: 60), rx: 150, ry: 170), fill: Palette.purple, lineWidth: 6, seed: 900)
        body.zPosition = 5
        addChild(body)
        let mouth = Sketch.node(.ellipse(center: monsterMouth, rx: 80, ry: 62), fill: Palette.ink, lineWidth: 4, seed: 901)
        mouth.zPosition = 6
        addChild(mouth)
        for (i, eye) in [-50, 50].enumerated() {
            let white = Sketch.node(.ellipse(center: CGPoint(x: 520 + CGFloat(eye), y: 190), rx: 30, ry: 36), fill: .white, lineWidth: 4, seed: 902 + UInt64(i))
            white.zPosition = 6
            addChild(white)
        }

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
        cards.forEach { $0.removeFromParent() }
        cards = []
        homes = []
        attempt = QuestionAttempt()
        round = next
        for (index, choice) in next.choices.enumerated() {
            let card = PictureCard(picture: choice.picture, word: choice.word, seed: 910 + UInt64(session.roundsPlayed * 7 + index))
            card.name = "tap:food:\(index)"
            let positions = Self.cardPositions(count: next.choices.count)
            card.position = positions[index % positions.count]
            if next.choices.count > 3 { card.setScale(0.85) }
            card.zPosition = 10
            addChild(card)
            cards.append(card)
            homes.append(card.position)
        }
        sfx.play(.beep)
        inputLocked = false
        sayPrompt()
    }

    private func sayPrompt() {
        guard let round else { return }
        voice.play([VoiceLine.feedMonster.rawValue, round.target.soundClip])
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
        guard !inputLocked, let index = cards.firstIndex(of: card), let round else {
            sendHome(card)
            return
        }
        let dx = card.position.x - monsterMouth.x
        let dy = card.position.y - monsterMouth.y
        if dx * dx + dy * dy < 150 * 150 {
            answer(food: round.choices[index], card: card)
        } else {
            sendHome(card)
        }
    }

    private func sendHome(_ card: PictureCard) {
        card.zPosition = 10
        guard let index = cards.firstIndex(of: card), index < homes.count else { return }
        card.run(.move(to: homes[index], duration: 0.25))
    }

    private func answer(food: MonsterFood, card: PictureCard) {
        card.zPosition = 10
        switch attempt.answer(correct: game.isCorrect(food, in: round!)) {
        case let .correct(firstTry):
            inputLocked = true
            card.run(.sequence([.scale(to: 0.4, duration: 0.2), .removeFromParent()]))
            sfx.play(.pop)
            after(0.15) { [weak self] in self?.sfx.play(.chime) }
            bip.celebrate()
            let change = coordinator.record(correct: firstTry, skillID: game.skillID(for: round!), soundID: round!.target.id)
            let focusChanged = round!.target.id == focus.id && change != .none
            voice.play([coordinator.randomPraise(), round!.answer.audio], completion: { [weak self] in
                self?.afterAnswer(focusChanged ? change : .none)
            })
        case .tryAgain:
            sfx.play(.boop)
            card.run(Buttons.shake())
            bip.tilt()
            sendHome(card)
            after(0.4) { [weak self] in
                guard let self, let round = self.round else { return }
                self.voice.play([round.target.soundClip])
            }
        case .hint:
            sfx.play(.boop)
            card.run(Buttons.shake())
            bip.tilt()
            sendHome(card)
            if let round, let rightIndex = round.choices.firstIndex(where: { game.isCorrect($0, in: round) }), rightIndex < cards.count {
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

    override func handleTap(name: String, node: SKNode) {
        if name == "tap:replay" {
            Buttons.press(node)
            sayPrompt()
        }
        // Food presses belong to the drag flow, not to taps.
    }

    override func replayPrompt() {
        guard !inputLocked else { return }
        sayPrompt()
    }

    override func goHome() {
        coordinator.showLettersIsland()
    }
}
