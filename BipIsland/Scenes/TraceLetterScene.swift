import BipCore
import SpriteKit

/// Letter Trace: follow the letter with the mouse while sparkles trail behind.
/// Wide checkpoints (not a tight path) so a mouse works for small hands; tracing is
/// savoured, never gated. One letter per visit.
final class TraceLetterScene: BaseScene {
    private let game: LetterTraceGame
    private let sound: PhonicsSound
    private var letter: SKNode?
    private var checkpoints: [CGPoint] = []
    private var touched: Set<Int> = []
    private var started = false
    private static let touchRadius: CGFloat = 120

    init(coordinator: GameCoordinator, game: LetterTraceGame, sound: PhonicsSound) {
        self.game = game
        self.sound = sound
        super.init(coordinator: coordinator)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }

    override func didMove(to view: SKView) {
        addHomeButton()
        addBip(at: CGPoint(x: -560, y: -400), scale: 0.95)

        let big = Sketch.letter(sound.grapheme, size: 380, shadow: Palette.sun)
        big.name = "tap:letter"
        big.position = CGPoint(x: 0, y: 60)
        big.zPosition = 10
        addChild(big)
        letter = big

        // A loose ring around the letter plus its middle: follow it roughly and it counts.
        checkpoints = (0..<8).map { i in
            let angle = CGFloat(i) / 8 * 2 * CGFloat.pi
            return CGPoint(x: 170 * cos(angle), y: 60 + 170 * sin(angle))
        } + [CGPoint(x: 0, y: 60)]

        inputLocked = true
        after(0.5) { [weak self] in
            guard let self else { return }
            self.voice.play([VoiceLine.traceLetter.rawValue, self.sound.soundClip])
            self.bip.hop()
            self.inputLocked = false
            self.started = true
        }
    }

    override func mouseDragged(with event: NSEvent) {
        guard started, !inputLocked else { return }
        let point = event.location(in: self)
        let dot = SKShapeNode(circleOfRadius: CGFloat.random(in: 8...14))
        dot.fillColor = [Palette.sun, Palette.pink, Palette.teal, Palette.purple].randomElement(using: &coordinator.rng) ?? Palette.sun
        dot.strokeColor = .clear
        dot.position = point
        dot.zPosition = 30
        addChild(dot)
        dot.run(.sequence([.fadeOut(withDuration: 0.7), .removeFromParent()]))

        for (i, check) in checkpoints.enumerated() where !touched.contains(i) {
            let dx = point.x - check.x
            let dy = point.y - check.y
            if dx * dx + dy * dy < Self.touchRadius * Self.touchRadius {
                touched.insert(i)
                sfx.play(.tick)
            }
        }
        if touched.count == checkpoints.count {
            finishTrace()
        }
    }

    private func finishTrace() {
        inputLocked = true
        letter?.run(.sequence([.scale(to: 1.12, duration: 0.15), .scale(to: 1, duration: 0.15)]))
        Buttons.sparkle(at: CGPoint(x: 0, y: 60), in: self)
        sfx.play(.chime)
        bip.celebrate()
        coordinator.record(correct: true, skillID: game.skillID(for: LetterTraceGame.Round(sound: sound)), soundID: sound.id)
        voice.play([coordinator.randomPraise(), sound.soundClip], completion: { [weak self] in
            self?.coordinator.showLettersIsland(greet: false)
        })
    }

    override func handleTap(name: String, node: SKNode) {
        if name == "tap:letter" {
            voice.play([sound.soundClip])
        }
    }

    override func replayPrompt() {
        guard !inputLocked else { return }
        voice.play([VoiceLine.traceLetter.rawValue, sound.soundClip])
    }

    override func goHome() {
        coordinator.showLettersIsland()
    }
}
