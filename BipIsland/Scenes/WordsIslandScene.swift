import BipCore
import SpriteKit

/// Words Island: Sound Buttons and Word Builder. Bip starts blending; either button starts its game.
final class WordsIslandScene: BaseScene {
    private let greet: Bool
    private var keyNodes: [SKNode] = []

    override var keyOptions: [SKNode] { keyNodes }

    init(coordinator: GameCoordinator, greet: Bool) {
        self.greet = greet
        super.init(coordinator: coordinator)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }

    override func didMove(to view: SKView) {
        let island = Sketch.node(.ellipse(center: CGPoint(x: 0, y: -20), rx: 760, ry: 430), fill: Palette.sand, ink: Palette.lightBrown, lineWidth: 7, seed: 810)
        island.zPosition = -50
        addChild(island)
        let meadow = Sketch.node(.ellipse(center: CGPoint(x: 0, y: 30), rx: 680, ry: 330), fill: Palette.pink.withAlphaComponent(0.45), ink: Palette.purple, lineWidth: 5, seed: 811)
        meadow.zPosition = -49
        addChild(meadow)

        let buttons = SKNode()
        buttons.name = "tap:buttons"
        buttons.position = CGPoint(x: -350, y: 80)
        buttons.zPosition = 5
        for (i, letter) in ["c", "a", "t"].enumerated() {
            let x = CGFloat(i - 1) * 130
            buttons.addChild(Sketch.node(.ellipse(center: CGPoint(x: x, y: 0), rx: 62, ry: 62), fill: Palette.card, lineWidth: 5, seed: 812 + UInt64(i)))
            let text = Sketch.letter(letter, size: 84, shadow: Palette.purple)
            text.position = CGPoint(x: x, y: 0)
            buttons.addChild(text)
        }
        addChild(buttons)
        keyNodes.append(buttons)

        let builder = SKNode()
        builder.name = "tap:builder"
        builder.position = CGPoint(x: 350, y: 80)
        builder.zPosition = 5
        for (i, letter) in ["c", "a", "t"].enumerated() {
            let x = CGFloat(i - 1) * 120
            builder.addChild(Sketch.node(.roundedRect(CGRect(x: x - 52, y: -52, width: 104, height: 104), radius: 18), fill: Palette.teal, lineWidth: 5, seed: 815 + UInt64(i)))
            let text = Sketch.letter(letter, size: 76, colour: .white, shadow: nil)
            text.position = CGPoint(x: x, y: 0)
            builder.addChild(text)
        }
        addChild(builder)
        keyNodes.append(builder)
        buttons.run(Buttons.pulse())

        addHomeButton()
        addBip(at: CGPoint(x: 0, y: -360), scale: 0.85)

        if greet {
            after(0.5) { [weak self] in self?.replayPrompt() }
        }
    }

    override func handleTap(name: String, node: SKNode) {
        switch name {
        case "tap:buttons":
            sfx.play(.whirr)
            Buttons.press(node)
            guard !inputLocked else { return }
            inputLocked = true
            after(0.25) { [weak self] in self?.coordinator.startButtons() }
        case "tap:builder":
            sfx.play(.whirr)
            Buttons.press(node)
            guard !inputLocked else { return }
            inputLocked = true
            after(0.25) { [weak self] in self?.coordinator.startBuilder() }
        default:
            break
        }
    }

    override func didTapBip() {
        guard !inputLocked else { return }
        inputLocked = true
        after(0.25) { [weak self] in self?.coordinator.startButtons() }
    }

    override func replayPrompt() {
        voice.play([VoiceLine.wordsIsland.rawValue])
        bip.hop()
    }

    override func goHome() {
        coordinator.showMap()
    }
}
