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

    // MARK: Keyboard play

    /// The tap targets arrows and Enter can use right now. Empty means keys just replay.
    var keyOptions: [SKNode] { [] }
    private var keyRing: SKNode?
    private var keyIndex = 0

    /// Left/right moves the glow, Enter or Space chooses, 1-3 chooses directly.
    /// Returns true when the key did something; anything else falls back to replay.
    func handleKey(_ event: NSEvent) -> Bool {
        guard !inputLocked else { return true }
        let options = keyOptions.filter { $0.parent != nil && !$0.isHidden }
        switch event.keyCode {
        case 123, 124, 125, 126:
            guard !options.isEmpty else { return false }
            let step = event.keyCode == 123 ? options.count - 1 : 1
            keyIndex = (keyIndex + step) % options.count
            showKeyRing(on: options[keyIndex])
            sfx.play(.tick)
            return true
        case 36, 49:
            guard !options.isEmpty else { return false }
            keyIndex = min(keyIndex, options.count - 1)
            choose(options[keyIndex])
            return true
        case 18, 19, 20:
            let index = Int(event.keyCode - 18)
            guard index < options.count else { return false }
            keyIndex = index
            choose(options[index])
            return true
        default:
            return false
        }
    }

    private func choose(_ node: SKNode) {
        hideKeyRing()
        handleTap(name: node.name ?? "", node: node)
    }

    private func showKeyRing(on node: SKNode) {
        if keyRing == nil {
            let ring = Sketch.node(.ellipse(center: .zero, rx: 105, ry: 105), ink: Palette.orange, lineWidth: 7, seed: 550)
            ring.zPosition = 60
            addChild(ring)
            keyRing = ring
        }
        keyRing?.isHidden = false
        keyRing?.position = node.position
    }

    private func hideKeyRing() {
        keyRing?.isHidden = true
    }

    /// New question, new targets: the glow starts over.
    func resetKeys() {
        keyIndex = 0
        hideKeyRing()
    }

    // MARK: Levels and the end of a visit

    private var levelBadge: SKNode?

    /// Top centre: which level of this game the child is on, as filled stars (and the number for grown-ups).
    func showLevelBadge(level: Int, of count: Int) {
        levelBadge?.removeFromParent()
        let badge = SKNode()
        badge.position = CGPoint(x: 0, y: 420)
        badge.zPosition = 45
        let width = CGFloat(count) * 46 + 40
        badge.addChild(Sketch.node(.roundedRect(CGRect(x: -width / 2, y: -30, width: width, height: 60), radius: 28),
                                   fill: Palette.card, lineWidth: 4, seed: 560))
        for i in 0..<count {
            let x = (CGFloat(i) - CGFloat(count - 1) / 2) * 46
            let star = Sketch.node(.polygon(Sketch.starPoints(center: CGPoint(x: x, y: 0), radius: 18)),
                                   fill: i <= level ? Palette.sun : Palette.stone.withAlphaComponent(0.5),
                                   lineWidth: 3, wobble: 1, seed: 561 + UInt64(i))
            badge.addChild(star)
        }
        addChild(badge)
        levelBadge = badge
    }

    /// The end of a visit: a star pops in for every star earned, and a level-up gets a bigger burst
    /// and a fuller badge. Then `next` runs (usually back to the island).
    func finishVisit(then next: @escaping () -> Void) {
        let summary = coordinator.visitSummary()
        inputLocked = true
        let card = SKNode()
        card.zPosition = 80
        card.addChild(Sketch.node(.roundedRect(CGRect(x: -420, y: -170, width: 840, height: 340), radius: 40),
                                  fill: Palette.card, lineWidth: 6, seed: 570))
        card.setScale(0.01)
        addChild(card)
        card.run(.sequence([.scale(to: 1.05, duration: 0.2), .scale(to: 1, duration: 0.1)]))
        sfx.play(.chime)
        bip.celebrate()

        let shown = min(summary.starsEarned, 10)
        let spacing: CGFloat = 70
        for i in 0..<shown {
            let x = (CGFloat(i) - CGFloat(shown - 1) / 2) * spacing
            let star = Sketch.node(.polygon(Sketch.starPoints(center: .zero, radius: 30)), fill: Palette.sun,
                                   lineWidth: 4, wobble: 1, seed: 571 + UInt64(i))
            star.position = CGPoint(x: x, y: 40)
            star.setScale(0.01)
            card.addChild(star)
            star.run(.sequence([.wait(forDuration: 0.3 + 0.15 * Double(i)),
                                .run { [weak self] in self?.sfx.play(.tick) },
                                .scale(to: 1.2, duration: 0.12), .scale(to: 1, duration: 0.08)]))
        }
        let total = Sketch.label(summary.starsEarned > 0 ? "+\(summary.starsEarned) stars" : "Well played!", size: 46, colour: Palette.ink)
        total.position = CGPoint(x: 0, y: -60)
        card.addChild(total)

        var wait = 0.6 + 0.15 * Double(shown)
        if summary.levelledUp {
            let label = Sketch.label("Level \(summary.levelNow + 1)!", size: 58, colour: Palette.ink)
            label.position = CGPoint(x: 0, y: -125)
            label.alpha = 0
            card.addChild(label)
            label.run(.sequence([.wait(forDuration: wait), .fadeIn(withDuration: 0.2)]))
            run(.sequence([.wait(forDuration: wait), .run { [weak self] in
                guard let self else { return }
                self.sfx.play(.whirr)
                Buttons.sparkle(at: CGPoint(x: -200, y: 0), in: self)
                Buttons.sparkle(at: CGPoint(x: 200, y: 0), in: self)
                self.showLevelBadge(level: summary.levelNow, of: summary.levelCount)
                self.levelBadge?.run(.sequence([.scale(to: 1.3, duration: 0.2), .scale(to: 1, duration: 0.2)]))
            }]))
            wait += 1.2
        }
        after(wait + 1.2, next)
    }

    /// Runs `block` after a delay on the scene's clock (so it pauses with the parent gate).
    func after(_ seconds: TimeInterval, _ block: @escaping () -> Void) {
        run(.sequence([.wait(forDuration: seconds), .run(block)]))
    }
}
