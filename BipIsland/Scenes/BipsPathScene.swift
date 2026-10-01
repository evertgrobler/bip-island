import BipCore
import SpriteKit

/// Bip's Path: snap arrow blocks into the strip, then press Go. Bip walks the program one
/// step at a time with a highlight on the current block, so a mistake shows itself.
/// Any working program passes; fewer blocks earns praise. Wrong → soft boop and keep
/// editing; two misses → the next right block wiggles.
final class BipsPathScene: BaseScene {
    private let game: BipsPathGame
    private var session: GameSession
    private let learner: Learner
    private var round: BipsPathGame.Round?
    private var attempt = QuestionAttempt()
    private var grid: SKNode?
    private var token: BipNode?
    private var strip: [String] = []
    private var stripNodes: [SKNode] = []
    private var palette: [SKNode] = []
    private var paletteBlocks: [String] = []
    private var goButton: SKNode?
    private static let cell: CGFloat = 100

    init(coordinator: GameCoordinator, game: BipsPathGame, session: GameSession, learner: Learner) {
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
        addBip(at: CGPoint(x: -620, y: -380), scale: 0.6)
        let replay = Buttons.replay()
        replay.position = CGPoint(x: 700, y: 360)
        replay.zPosition = 10
        addChild(replay)
        let go = Buttons.play()
        go.name = "tap:go"
        go.position = CGPoint(x: 640, y: -160)
        go.zPosition = 10
        addChild(go)
        goButton = go
        after(0.5) { [weak self] in self?.askQuestion() }
    }

    private func askQuestion() {
        guard let next = session.nextRound(of: game, for: learner, using: &coordinator.rng) else {
            return endVisit(with: .roundDone)
        }
        grid?.removeFromParent()
        palette.forEach { $0.removeFromParent() }
        stripNodes.forEach { $0.removeFromParent() }
        grid = nil
        token = nil
        palette = []
        strip = []
        stripNodes = []
        attempt = QuestionAttempt()
        round = next
        drawLevel(next.level)
        drawPalette(next.level)
        inputLocked = false
        sayPrompt()
    }

    private func cellPoint(row: Int, column: Int) -> CGPoint {
        CGPoint(x: -740 + CGFloat(column) * Self.cell + Self.cell / 2,
                y: 320 - CGFloat(row) * Self.cell - Self.cell / 2)
    }

    private func drawLevel(_ level: GridLevel) {
        let board = SKNode()
        board.zPosition = 5
        for row in 0..<level.grid.rows {
            for col in 0..<level.grid.cols {
                let cell = Sketch.node(.roundedRect(CGRect(x: 0, y: 0, width: Self.cell - 8, height: Self.cell - 8), radius: 16),
                                       fill: Palette.card, lineWidth: 4, seed: 990 + UInt64(row * 7 + col))
                cell.position = cellPoint(row: row, column: col)
                board.addChild(cell)
            }
        }
        for rock in level.rocks {
            let stone = Sketch.node(.ellipse(center: cellPoint(row: rock.row, column: rock.column), rx: 34, ry: 30),
                                    fill: Palette.stone, lineWidth: 4, seed: 991)
            board.addChild(stone)
        }
        let goal = Sketch.node(.roundedRect(CGRect(x: -34, y: -34, width: 68, height: 68), radius: 14),
                               fill: Palette.go, lineWidth: 4, seed: 992)
        goal.position = cellPoint(row: level.goal.row, column: level.goal.column)
        board.addChild(goal)
        let bolt = Sketch.node(.polygon([CGPoint(x: 6, y: 26), CGPoint(x: -12, y: 2), CGPoint(x: -2, y: 2), CGPoint(x: -6, y: -26), CGPoint(x: 12, y: -2), CGPoint(x: 2, y: -2)]),
                               fill: Palette.sun, lineWidth: 3, seed: 993)
        bolt.position = goal.position
        board.addChild(bolt)
        let bip = BipNode()
        bip.position = cellPoint(row: level.start.row, column: level.start.column)
        bip.setScale(0.32)
        board.addChild(bip)
        addChild(board)
        grid = board
        token = bip
    }

    private func drawPalette(_ level: GridLevel) {
        paletteBlocks = level.blocks
        for (i, block) in level.blocks.enumerated() {
            let button = SKNode()
            button.name = "tap:block:\(i)"
            button.position = CGPoint(x: (CGFloat(i) - CGFloat(level.blocks.count - 1) / 2) * 130, y: -340)
            button.zPosition = 10
            button.addChild(Sketch.node(.ellipse(center: .zero, rx: 56, ry: 56), fill: Palette.sun, lineWidth: 5, seed: 994 + UInt64(i)))
            button.addChild(Self.blockIcon(block))
            addChild(button)
            palette.append(button)
        }
    }

    /// Picture-only blocks: triangles for steps, a wheel for turns. No reading needed.
    static func blockIcon(_ block: String) -> SKNode {
        let arrow = Sketch.node(.polygon([CGPoint(x: -22, y: -10), CGPoint(x: 6, y: -10), CGPoint(x: 6, y: -24), CGPoint(x: 30, y: 0), CGPoint(x: 6, y: 24), CGPoint(x: 6, y: 10), CGPoint(x: -22, y: 10)]),
                                fill: Palette.ink, lineWidth: 3, seed: 995)
        switch block {
        case "up": arrow.zRotation = CGFloat.pi / 2
        case "down": arrow.zRotation = -CGFloat.pi / 2
        case "left": arrow.zRotation = CGFloat.pi
        case "forward":
            let fast = SKNode()
            fast.addChild(arrow)
            let dot = Sketch.node(.ellipse(center: CGPoint(x: -34, y: 0), rx: 8, ry: 8), fill: Palette.ink, lineWidth: 2, seed: 996)
            fast.addChild(dot)
            return fast
        case "turnLeft", "turnRight":
            let wheel = SKNode()
            wheel.addChild(Sketch.node(.arc(center: .zero, rx: 24, ry: 24, from: -0.6, to: 2.4), lineWidth: 7, seed: 997))
            let head = Sketch.node(.polygon([CGPoint(x: -10, y: 22), CGPoint(x: 10, y: 26), CGPoint(x: 2, y: 8)]),
                                   fill: Palette.ink, lineWidth: 3, seed: 998)
            if block == "turnRight" { head.xScale = -1 }
            wheel.addChild(head)
            return wheel
        default:
            break
        }
        return arrow
    }

    private func drawStrip() {
        stripNodes.forEach { $0.removeFromParent() }
        stripNodes = []
        for (i, block) in strip.enumerated() {
            let node = SKNode()
            node.name = "tap:strip:\(i)"
            // Two rows of six: the longest content program needs ten blocks.
            node.position = CGPoint(x: -100 + CGFloat(i % 6) * 95, y: 392 - CGFloat(i / 6) * 85)
            node.zPosition = 10
            node.addChild(Sketch.node(.roundedRect(CGRect(x: -40, y: -40, width: 80, height: 80), radius: 16),
                                      fill: Palette.lightTeal, lineWidth: 4, seed: 999 + UInt64(i)))
            let icon = Self.blockIcon(block)
            icon.setScale(0.8)
            node.addChild(icon)
            addChild(node)
            stripNodes.append(node)
        }
    }

    private func addBlock(_ block: String) {
        guard paletteBlocks.contains(block), strip.count < 12 else {
            sfx.play(.boop)
            return
        }
        sfx.play(.tick)
        strip.append(block)
        drawStrip()
    }

    /// The whole game from the keyboard: arrows snap blocks in (1-7 picks from the
    /// palette too), Enter presses Go, Backspace takes the last block back.
    override func handleKey(_ event: NSEvent) -> Bool {
        guard !inputLocked, let round else { return true }
        switch event.keyCode {
        case 126: addBlock("up"); return true
        case 125: addBlock("down"); return true
        case 123: addBlock("left"); return true
        case 124: addBlock("right"); return true
        case 36, 49: pressGo(in: round); return true
        case 51:
            if !strip.isEmpty {
                sfx.play(.tick)
                strip.removeLast()
                drawStrip()
            }
            return true
        case 18...24:
            let index = Int(event.keyCode) - 18
            if index < paletteBlocks.count { addBlock(paletteBlocks[index]) }
            return true
        default: return false
        }
    }

    private func sayPrompt() {
        voice.play([VoiceLine.bipsPath.rawValue])
        bip.hop()
    }

    override func handleTap(name: String, node: SKNode) {
        if name == "tap:replay" {
            Buttons.press(node)
            sayPrompt()
            return
        }
        if name.hasPrefix("tap:block:"), let index = Int(name.dropFirst("tap:block:".count)), index < paletteBlocks.count {
            guard !inputLocked else { return }
            addBlock(paletteBlocks[index])
            return
        }
        if name.hasPrefix("tap:strip:"), let index = Int(name.dropFirst("tap:strip:".count)), index < strip.count {
            guard !inputLocked else { return }
            sfx.play(.tick)
            strip.remove(at: index)
            drawStrip()
            return
        }
        guard name == "tap:go", let round else { return }
        pressGo(in: round)
    }

    private func pressGo(in round: BipsPathGame.Round) {
        guard !strip.isEmpty else {
            sfx.play(.boop)
            return
        }
        inputLocked = true
        let result = GridWalker.path(program: strip, on: round.level)
        let won = !result.crashed && result.positions.last == round.level.goal
        animateWalk(result.positions, step: 1) { [weak self] in
            guard let self else { return }
            if won {
                self.win(in: round)
            } else {
                self.miss(in: round)
            }
        }
    }

    private func animateWalk(_ positions: [GridPosition], step: Int, done: @escaping () -> Void) {
        guard let token, step < positions.count else {
            done()
            return
        }
        if step - 1 < stripNodes.count {
            stripNodes[step - 1].run(.sequence([.scale(to: 1.2, duration: 0.15), .scale(to: 1, duration: 0.15)]))
        }
        sfx.play(.tick)
        token.run(.move(to: cellPoint(row: positions[step].row, column: positions[step].column), duration: 0.35)) { [weak self] in
            self?.animateWalk(positions, step: step + 1, done: done)
        }
    }

    private func win(in round: BipsPathGame.Round) {
        Buttons.sparkle(at: token?.position ?? .zero, in: self)
        if strip.count <= round.answer.count {
            // Fewer blocks earns extra sparkles, but any working solution passes.
            Buttons.sparkle(at: token?.position ?? .zero, in: self)
        }
        sfx.play(.chime)
        bip.celebrate()
        let change = coordinator.record(correct: attempt.misses == 0, skillID: game.skillID(for: round), soundID: nil)
        voice.play([coordinator.randomPraise()], completion: { [weak self] in
            self?.afterAnswer(change)
        })
    }

    private func miss(in round: BipsPathGame.Round) {
        switch attempt.answer(correct: false) {
        case .tryAgain, .hint:
            sfx.play(.boop)
            bip.tilt()
            token?.run(Buttons.shake())
            if attempt.needsHint {
                wiggleNextRightBlock(in: round)
                after(0.4) { [weak self] in
                    self?.voice.play([self?.coordinator.randomHint() ?? "hint_01"])
                }
            }
            after(0.6) { [weak self] in
                self?.resetToken(in: round)
                self?.inputLocked = false
            }
        case .correct:
            break
        }
    }

    /// Wiggles the palette block that comes next in the working program.
    private func wiggleNextRightBlock(in round: BipsPathGame.Round) {
        var index = strip.count
        for (i, block) in strip.enumerated() where i < round.answer.count {
            if block != round.answer[i] {
                index = i
                break
            }
        }
        let wanted: String = index < round.answer.count ? round.answer[index] : (round.answer.last ?? "")
        for (i, block) in paletteBlocks.enumerated() where block == wanted && i < palette.count {
            palette[i].run(Buttons.hintWiggle(), withKey: "hint")
        }
    }

    private func resetToken(in round: BipsPathGame.Round) {
        token?.position = cellPoint(row: round.level.start.row, column: round.level.start.column)
        token?.zRotation = 0
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
