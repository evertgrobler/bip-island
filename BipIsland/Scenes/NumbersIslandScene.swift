import BipCore
import SpriteKit

/// Numbers Island: Count & Tap and Quick Look. Bip starts counting; either button starts its game.
final class NumbersIslandScene: BaseScene {
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
        let island = Sketch.node(.ellipse(center: CGPoint(x: 0, y: -20), rx: 760, ry: 430), fill: Palette.sand, ink: Palette.lightBrown, lineWidth: 7, seed: 800)
        island.zPosition = -50
        addChild(island)
        let sun = Sketch.node(.ellipse(center: CGPoint(x: 0, y: 30), rx: 680, ry: 330), fill: Palette.sun.withAlphaComponent(0.5), ink: Palette.orange, lineWidth: 5, seed: 801)
        sun.zPosition = -49
        addChild(sun)

        let count = SKNode()
        count.name = "tap:count"
        count.position = CGPoint(x: -350, y: 80)
        count.zPosition = 5
        count.addChild(Sketch.node(.ellipse(center: .zero, rx: 180, ry: 160), fill: Palette.card, lineWidth: 6, seed: 802))
        let numerals = Sketch.letter("1 2 3", size: 110, shadow: Palette.red)
        numerals.position = CGPoint(x: 0, y: 10)
        count.addChild(numerals)
        addChild(count)
        keyNodes.append(count)

        let quick = SKNode()
        quick.name = "tap:quick"
        quick.position = CGPoint(x: 350, y: 80)
        quick.zPosition = 5
        quick.addChild(Sketch.node(.roundedRect(CGRect(x: -150, y: -150, width: 300, height: 300), radius: 40), fill: Palette.card, lineWidth: 6, seed: 803))
        for (i, p) in [CGPoint(x: -70, y: 70), CGPoint(x: 70, y: 70), CGPoint(x: 0, y: 0), CGPoint(x: -70, y: -70), CGPoint(x: 70, y: -70)].enumerated() {
            let dot = Sketch.node(.ellipse(center: p, rx: 24, ry: 24), fill: Palette.ink, lineWidth: 3, seed: 804 + UInt64(i))
            quick.addChild(dot)
        }
        addChild(quick)
        keyNodes.append(quick)
        count.run(Buttons.pulse())

        addHomeButton()
        addBip(at: CGPoint(x: 0, y: -360), scale: 0.85)

        if greet {
            after(0.5) { [weak self] in self?.replayPrompt() }
        }
    }

    override func handleTap(name: String, node: SKNode) {
        switch name {
        case "tap:count":
            sfx.play(.whirr)
            Buttons.press(node)
            guard !inputLocked else { return }
            inputLocked = true
            after(0.25) { [weak self] in self?.coordinator.startCount() }
        case "tap:quick":
            sfx.play(.whirr)
            Buttons.press(node)
            guard !inputLocked else { return }
            inputLocked = true
            after(0.25) { [weak self] in self?.coordinator.startQuick() }
        default:
            break
        }
    }

    override func didTapBip() {
        guard !inputLocked else { return }
        inputLocked = true
        after(0.25) { [weak self] in self?.coordinator.startCount() }
    }

    override func replayPrompt() {
        voice.play([VoiceLine.numbersIsland.rawValue])
        bip.hop()
    }

    override func goHome() {
        coordinator.showMap()
    }
}
