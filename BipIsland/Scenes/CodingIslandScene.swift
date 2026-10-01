import BipCore
import SpriteKit

/// Coding Island: Morning Order and Bip's Path. Bip starts programming; either button starts its game.
final class CodingIslandScene: BaseScene {
    private let greet: Bool

    init(coordinator: GameCoordinator, greet: Bool) {
        self.greet = greet
        super.init(coordinator: coordinator)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }

    override func didMove(to view: SKView) {
        let island = Sketch.node(.ellipse(center: CGPoint(x: 0, y: -20), rx: 760, ry: 430), fill: Palette.sand, ink: Palette.lightBrown, lineWidth: 7, seed: 820)
        island.zPosition = -50
        addChild(island)
        let lab = Sketch.node(.ellipse(center: CGPoint(x: 0, y: 30), rx: 680, ry: 330), fill: Palette.lightTeal.withAlphaComponent(0.5), ink: Palette.teal, lineWidth: 5, seed: 821)
        lab.zPosition = -49
        addChild(lab)

        let order = SKNode()
        order.name = "tap:order"
        order.position = CGPoint(x: -350, y: 80)
        order.zPosition = 5
        for (i, number) in ["1", "2", "3"].enumerated() {
            let x = CGFloat(i - 1) * 130
            order.addChild(Sketch.node(.roundedRect(CGRect(x: x - 56, y: -70, width: 112, height: 140), radius: 18), fill: Palette.card, lineWidth: 5, seed: 822 + UInt64(i)))
            let text = Sketch.letter(number, size: 72, shadow: Palette.orange)
            text.position = CGPoint(x: x, y: 0)
            order.addChild(text)
        }
        addChild(order)

        let path = SKNode()
        path.name = "tap:path"
        path.position = CGPoint(x: 350, y: 80)
        path.zPosition = 5
        for (i, angle) in [CGFloat(0), CGFloat.pi / 2, CGFloat(0)].enumerated() {
            let arrow = Sketch.node(.polygon([CGPoint(x: -30, y: -11), CGPoint(x: 5, y: -11), CGPoint(x: 5, y: -28), CGPoint(x: 35, y: 0), CGPoint(x: 5, y: 28), CGPoint(x: 5, y: 11), CGPoint(x: -30, y: 11)]),
                                    fill: Palette.orange, lineWidth: 4, seed: 825 + UInt64(i))
            arrow.zRotation = angle
            arrow.position = CGPoint(x: CGFloat(i - 1) * 110, y: 0)
            path.addChild(arrow)
        }
        addChild(path)
        order.run(Buttons.pulse())

        addHomeButton()
        addBip(at: CGPoint(x: 0, y: -360), scale: 0.85)

        if greet {
            after(0.5) { [weak self] in self?.replayPrompt() }
        }
    }

    override func handleTap(name: String, node: SKNode) {
        switch name {
        case "tap:order":
            sfx.play(.whirr)
            Buttons.press(node)
            guard !inputLocked else { return }
            inputLocked = true
            after(0.25) { [weak self] in self?.coordinator.startOrder() }
        case "tap:path":
            sfx.play(.whirr)
            Buttons.press(node)
            guard !inputLocked else { return }
            inputLocked = true
            after(0.25) { [weak self] in self?.coordinator.startPath() }
        default:
            break
        }
    }

    override func didTapBip() {
        guard !inputLocked else { return }
        inputLocked = true
        after(0.25) { [weak self] in self?.coordinator.startOrder() }
    }

    override func replayPrompt() {
        voice.play([VoiceLine.codingIsland.rawValue])
        bip.hop()
    }

    override func goHome() {
        coordinator.showMap()
    }
}
