import BipCore
import SpriteKit

/// Word Builder: build the word in the picture from letter tiles, then tap the green arrow.
/// Tap a tile to send it to the next empty space (tap it again to take it back), or drag it
/// to any space. Each tile says its sound as it lands. Spare tiles hide in the bank (more at
/// higher levels). Wrong → soft boop and try again; two misses → the word is said and the
/// wrong spaces wiggle. Right → the tiles light up one by one as the word is sounded out.
final class WordBuilderScene: BaseScene {
    private final class Tile: SKNode {
        let tile: WordBuilderGame.Tile

        init(tile: WordBuilderGame.Tile) {
            self.tile = tile
            super.init()
            addChild(Sketch.node(.roundedRect(CGRect(x: -56, y: -56, width: 112, height: 112), radius: 20),
                                 fill: Palette.teal, lineWidth: 5, seed: 972))
            let size: CGFloat = tile.text.count >= 3 ? 50 : tile.text.count == 2 ? 62 : 76
            addChild(Sketch.letter(tile.text, size: size, colour: .white, shadow: nil))
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
    private var pressPoint = CGPoint.zero
    private var draggedFromSlot: Int?
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

    /// Arrows move between the tiles and the green arrow; Enter picks one, like a tap.
    override var keyOptions: [SKNode] { bankTiles.map { $0 as SKNode } + [nextButton] }

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

        let spacing: CGFloat = next.bank.count > 6 ? 128 : 140
        for (i, piece) in next.bank.enumerated() {
            let tile = Tile(tile: piece)
            tile.name = "tap:tile:\(i)"
            tile.position = CGPoint(x: (CGFloat(i) - CGFloat(next.bank.count - 1) / 2) * spacing - 40, y: -180)
            tile.zPosition = 10
            addChild(tile)
            bankTiles.append(tile)
            bankHomes.append(tile.position)
        }
        inputLocked = false
        sayPrompt()
    }

    private func sayPrompt() {
        guard let round else { return }
        voice.play([VoiceLine.wordBuilder.rawValue, AudioCatalogue.wordClip(for: round.word.word)])
        bip.hop()
    }

    // MARK: Tapping and dragging

    override func mouseDown(with event: NSEvent) {
        let point = event.location(in: self)
        guard !inputLocked, dragged == nil,
              let tile = bankTiles.last(where: { $0.contains(point) }) else {
            super.mouseDown(with: event)
            return
        }
        dragged = tile
        pressPoint = point
        dragOffset = CGPoint(x: tile.position.x - point.x, y: tile.position.y - point.y)
        tile.zPosition = 20
        tile.removeAllActions()
        draggedFromSlot = slotTiles.firstIndex(where: { $0 === tile })
        if let slotIndex = draggedFromSlot {
            slotTiles[slotIndex] = nil
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
        tile.zPosition = 10
        guard !inputLocked else { return sendHome(tile) }
        let point = event.location(in: self)
        let moved = hypot(point.x - pressPoint.x, point.y - pressPoint.y)
        if moved < 12 {
            // A tap: a tile in a space goes back; a tile in the bank goes to the next space.
            if draggedFromSlot != nil {
                sendHome(tile)
                sfx.play(.tick)
            } else {
                placeInNextSpace(tile)
            }
            return
        }
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
        if best >= 0 {
            place(tile, in: best)
        } else {
            sendHome(tile)
        }
    }

    /// Puts a tile in a space and says its sound. A tile already there goes back to the bank.
    private func place(_ tile: Tile, in index: Int) {
        if let old = slotTiles[index], old !== tile { sendHome(old) }
        slotTiles[index] = tile
        tile.zPosition = 10
        tile.run(.move(to: slots[index].position, duration: 0.15))
        sfx.play(.tick)
        voice.play([tile.tile.soundClip])
        if slotTiles.allSatisfy({ $0 != nil }) {
            nextButton.run(.sequence([.scale(to: 1.15, duration: 0.15), .scale(to: 1, duration: 0.15)]))
        }
    }

    private func placeInNextSpace(_ tile: Tile) {
        guard let empty = slotTiles.firstIndex(where: { $0 == nil }) else {
            sendHome(tile)
            sfx.play(.boop)
            return
        }
        place(tile, in: empty)
    }

    private func sendHome(_ tile: Tile) {
        if let slotIndex = slotTiles.firstIndex(where: { $0 === tile }) { slotTiles[slotIndex] = nil }
        guard let index = bankTiles.firstIndex(where: { $0 === tile }), index < bankHomes.count else { return }
        tile.run(.move(to: bankHomes[index], duration: 0.2))
    }

    private func currentSpelling() -> [String] {
        slotTiles.map { $0?.tile.id ?? "" }
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
        // Keyboard: Enter on a tile works like tapping it.
        if name.hasPrefix("tap:tile:"), let tile = node as? Tile, dragged == nil {
            if slotTiles.contains(where: { $0 === tile }) {
                sendHome(tile)
                sfx.play(.tick)
            } else {
                placeInNextSpace(tile)
            }
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
            soundOut(round) { [weak self] in
                guard let self else { return }
                self.voice.play([self.coordinator.randomPraise()], completion: { [weak self] in
                    self?.afterAnswer(change)
                })
            }
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

    /// Each tile lights up as its sound is said, then the whole word: s-u-n, sun.
    private func soundOut(_ round: WordBuilderGame.Round, then done: @escaping () -> Void) {
        let placed = slotTiles.compactMap { $0 }
        for (i, tile) in placed.enumerated() {
            tile.run(.sequence([.wait(forDuration: 0.55 * Double(i)),
                                .scale(to: 1.18, duration: 0.15), .scale(to: 1, duration: 0.2)]))
        }
        voice.play(round.answerTiles.map(\.soundClip) + [AudioCatalogue.wordClip(for: round.word.word)], completion: done)
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
