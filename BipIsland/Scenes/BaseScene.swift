import BipCore
import SpriteKit

/// Shared scene behaviour: paper background, click handling by node name ("tap:…"), Bip, home button.
class BaseScene: SKScene {
    unowned let coordinator: GameCoordinator
    /// Ignore clicks while something important is animating or being said.
    var inputLocked = false
    let bip = BipNode()

    init(coordinator: GameCoordinator) {
        self.coordinator = coordinator
        super.init(size: GameCoordinator.sceneSize)
        scaleMode = .aspectFill
        anchorPoint = CGPoint(x: 0.5, y: 0.5)
        backgroundColor = Palette.paper

        let paper = SKSpriteNode(texture: Sketch.paperTexture, size: CGSize(width: size.width + 40, height: size.height + 40))
        paper.zPosition = -100
        addChild(paper)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }

    var voice: VoicePlayer { coordinator.voice }
    var sfx: BipSounds { coordinator.sounds }

    func addHomeButton() {
        let home = Buttons.home()
        home.position = CGPoint(x: -700, y: 360)
        home.zPosition = 40
        addChild(home)
    }

    func addBip(at point: CGPoint, scale: CGFloat = 1) {
        bip.position = point
        bip.setScale(scale)
        bip.zPosition = 20
        addChild(bip)
    }

    // MARK: Clicks

    override func mouseDown(with event: NSEvent) {
        let point = event.location(in: self)
        var best: SKNode?
        for node in nodes(at: point) {
            guard let target = Self.tappableAncestor(of: node), !target.isHidden, target.alpha > 0.05 else { continue }
            if best == nil || target.zPosition > best!.zPosition {
                best = target
            }
        }
        guard let target = best, let name = target.name else { return }

        // Bip and the home button always work, even mid-question.
        if name == "tap:bip" {
            sfx.play(.beep)
            bip.hop()
            didTapBip()
            return
        }
        if name == "tap:home" {
            sfx.play(.tick)
            Buttons.press(target)
            goHome()
            return
        }
        guard !inputLocked else { return }
        handleTap(name: name, node: target)
    }

    private static func tappableAncestor(of node: SKNode) -> SKNode? {
        var current: SKNode? = node
        while let n = current {
            if let name = n.name, name.hasPrefix("tap:") { return n }
            current = n.parent
        }
        return nil
    }

    // MARK: For subclasses

    func handleTap(name: String, node: SKNode) {}
    func didTapBip() {}
    func goHome() { coordinator.showMap() }
    /// Any key pressed: say the current sound or instruction again.
    func replayPrompt() {}
    func didChangePause(_ paused: Bool) {}

    /// Runs `block` after a delay on the scene's clock (so it pauses with the parent gate).
    func after(_ seconds: TimeInterval, _ block: @escaping () -> Void) {
        run(.sequence([.wait(forDuration: seconds), .run(block)]))
    }
}
