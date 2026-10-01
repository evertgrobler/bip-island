import BipCore
import SpriteKit

/// Word Builder: drag letter tiles into the slots to build the word in the picture,
/// then tap the green arrow. One decoy tile hides in the bank, so placing it is the only
/// way to go wrong. Wrong → soft boop and try again; two misses → the word is said and
/// the first wrong slot wiggles.
final class WordBuilderScene: BaseScene {
    private final class Tile: SKNode {
        let grapheme: String

        init(grapheme: String) {
            self.grapheme = grapheme
            super.init()
            addChild(Sketch.node(.roundedRect(CGRect(x: -56, y: -56, width: 112, height: 112), radius: 20),
                                 fill: Palette.teal, lineWidth: 5, seed: 972))
            addChild(Sketch.letter(grapheme, size: 76, colour: .white, shadow: nil))
        }

        required init?(coder: NSCoder) {
            fatalError("init(coder:) is not used")
        }
    }

    private let game: WordBuilderGame
    private var session: GameSession
    private let learner: Learner
    private var round: WordBuilderGame.Round?
    private var attempt = QuestionAttempt()
    private var picture: PictureCard?
    private var slots: [SKNode] = []
    private var slotTiles: [Tile?] = []
    private var bankTiles: [Tile] = []
    private var bankHomes: [CGPoint] = []
    private var dragged: Tile?
    private var dragOffset = CGPoint.zero
    private let nextButton = Buttons.next()

    init(coordinator: GameCoordinator, game: WordBuilderGame, session: GameSession, learner: Learner) {
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

    private func askQuestion() {
        guard let next = session.nextRound(of: game, for: learner, using: &coordinator.rng) else {
            return endVisit(with: .roundDone)
        }
        picture?.removeFromParent()
        slots.forEach { $0.removeFromParent() }
        bankTiles.forEach { $0.removeFromParent() }
        picture = nil
        slots = []
        slotTiles = []
        bankTiles = []
        bankHomes = []
        dragged = nil
        attempt = QuestionAttempt()
        round = next

        let card = PictureCard(picture: next.word.picture ?? "pic_\(next.word.word)", word: next.word.word,
                               seed: 970 + UInt64(session.roundsPlayed))
        card.name = "tap:picture"
        card.position = CGPoint(x: -420, y: 120)
        card.zPosition = 10
        addChild(card)
        picture = card

        let count = CGFloat(next.answer.count)
        for i in 0..<next.answer.count {
            let slot = Sketch.node(.roundedRect(CGRect(x: -58, y: -58, width: 116, height: 116), radius: 20),
                                   fill: Palette.card, lineWidth: 5, seed: 971 + UInt64(i))
            slot.position = CGPoint(x: (CGFloat(i) - (count - 1) / 2) * 140 - 40, y: 180)
            slot.zPosition = 5
            addChild(slot)
            slots.append(slot)
            slotTiles.append(nil)
        }

        var bank = next.answer.shuffled(using: &coordinator.rng)
        bank.insert(decoyLetter(avoiding: next.answer), at: Int.random(in: 0...bank.count, using: &coordinator.rng))
        for (i, grapheme) in bank.enumerated() {
            let tile = Tile(grapheme: grapheme)
            tile.name = "tap:tile:\(i)"
            tile.position = CGPoint(x: (CGFloat(i) - CGFloat(bank.count - 1) / 2) * 140 - 40, y: -180)
            tile.zPosition = 10
            addChild(tile)
            bankTiles.append(tile)
            bankHomes.append(tile.position)
        }
        inputLocked = false
        sayPrompt()
    }

    /// A letter that isn't in the word, so it can only be wrong.
    private func decoyLetter(avoiding tiles: [String]) -> String {
        let alphabet = "abcdefghijklmnopqrstuvwxyz".map { String($0) }
        let joined = tiles.joined()
        return alphabet.filter { !joined.contains($0) }.randomElement(using: &coordinator.rng) ?? "z"
    }

    private func sayPrompt() {
        guard let round else { return }
        voice.play([VoiceLine.wordBuilder.rawValue, AudioCatalogue.wordClip(for: round.word.word)])
        bip.hop()
    }

    // MARK: Dragging

    override func mouseDown(with event: NSEvent) {
        super.mouseDown(with: event)
        guard !inputLocked, dragged == nil else { return }
        let point = event.location(in: self)
        for tile in bankTiles where tile.contains(point) {
            dragged = tile
            dragOffset = CGPoint(x: tile.position.x - point.x, y: tile.position.y - point.y)
            tile.zPosition = 20
            tile.removeAllActions()
            if let slotIndex = slotTiles.firstIndex(where: { $0 === tile }) {
                slotTiles[slotIndex] = nil
            }
            break
        }
    }

    override func mouseDragged(with event: NSEvent) {
        guard let tile = dragged, !inputLocked else { return }
        let point = event.location(in: self)
        tile.position = CGPoint(x: point.x + dragOffset.x, y: point.y + dragOffset.y)
    }

    override func mouseUp(with event: NSEvent) {
        guard let tile = dragged else { return }
        dragged = nil
        guard !inputLocked else { return }
        tile.zPosition = 10
        var best = -1
        var bestDistance = CGFloat(130 * 130)
        for (i, slot) in slots.enumerated() {
            let dx = tile.position.x - slot.position.x
            let dy = tile.position.y - slot.position.y
            let distance = dx * dx + dy * dy
            if distance < bestDistance {
                bestDistance = distance
                best = i
            }
        }
        if best >= 0 && slotTiles[best] == nil {
            slotTiles[best] = tile
            tile.run(.move(to: slots[best].position, duration: 0.15))
            sfx.play(.tick)
        } else if let index = bankTiles.firstIndex(where: { $0 === tile }), index < bankHomes.count {
            tile.run(.move(to: bankHomes[index], duration: 0.2))
        }
    }

    private func currentSpelling() -> [String] {
        slotTiles.map { $0?.grapheme ?? "" }
    }

    override func handleTap(name: String, node: SKNode) {
        if name == "tap:replay" {
            Buttons.press(node)
            sayPrompt()
            return
        }
        if name == "tap:picture", let round {
            voice.play([AudioCatalogue.wordClip(for: round.word.word)])
            return
        }
        guard name == "tap:next", let round else { return }
        Buttons.press(node)
        check(round: round)
    }

    private func check(round: WordBuilderGame.Round) {
        guard currentSpelling().allSatisfy({ !$0.isEmpty }) else {
            sfx.play(.boop)
            return
        }
        switch attempt.answer(correct: game.isCorrect(currentSpelling(), in: round)) {
        case let .correct(firstTry):
            inputLocked = true
            Buttons.sparkle(at: CGPoint(x: -40, y: 180), in: self)
            sfx.play(.chime)
            bip.celebrate()
            let change = coordinator.record(correct: firstTry, skillID: game.skillID(for: round), soundID: nil)
            voice.play([coordinator.randomPraise(), AudioCatalogue.wordClip(for: round.word.word)], completion: { [weak self] in
                self?.afterAnswer(change)
            })
        case .tryAgain:
            sfx.play(.boop)
            bip.tilt()
            for (i, placed) in currentSpelling().enumerated() where placed != round.answer[i] && i < slots.count {
                slots[i].run(Buttons.shake())
            }
        case .hint:
            sfx.play(.boop)
            bip.tilt()
            for (i, placed) in currentSpelling().enumerated() where placed != round.answer[i] && i < slots.count {
                slots[i].run(Buttons.hintWiggle(), withKey: "hint")
            }
            after(0.4) { [weak self] in
                guard let self, let round = self.round else { return }
                self.voice.play([self.coordinator.randomHint(), AudioCatalogue.wordClip(for: round.word.word)])
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
