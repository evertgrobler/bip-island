import BipCore
import SpriteKit

/// "Who's playing?": one big card per child, with their animal, name and stars. A child who
/// can't read yet finds their animal. Parents add, rename and remove children behind the
/// parent gate.
final class ProfilesScene: BaseScene {
    private var cards: [SKNode] = []

    override var keyOptions: [SKNode] { cards }

    override func didMove(to view: SKView) {
        addBip(at: CGPoint(x: -620, y: -380), scale: 0.7)

        let title = Sketch.label("Who's playing?", size: 72, colour: Palette.ink)
        title.position = CGPoint(x: 0, y: 360)
        addChild(title)

        let children = coordinator.children
        let spacing: CGFloat = 370
        for (i, child) in children.enumerated() {
            let card = makeCard(for: child, index: i)
            card.position = CGPoint(x: (CGFloat(i) - CGFloat(children.count - 1) / 2) * spacing, y: 20)
            addChild(card)
            cards.append(card)
            card.setScale(0.01)
            card.run(.sequence([.wait(forDuration: 0.1 * Double(i)), .scale(to: 1.06, duration: 0.2), .scale(to: 1, duration: 0.1)]))
        }

        after(0.6) { [weak self] in self?.replayPrompt() }
    }

    private func makeCard(for child: ChildSummary, index: Int) -> SKNode {
        let card = SKNode()
        card.name = "tap:child:\(child.id.uuidString)"
        card.zPosition = 10
        card.addChild(Sketch.node(.roundedRect(CGRect(x: -160, y: -200, width: 320, height: 400), radius: 34),
                                  fill: Palette.card, lineWidth: 6, seed: 1300 + UInt64(index) * 5))
        let badge = Avatars.badge(child.avatar, radius: 100, seed: 1301 + UInt64(index) * 5)
        badge.position = CGPoint(x: 0, y: 55)
        card.addChild(badge)

        let name = Sketch.label(child.name, size: child.name.count > 10 ? 34 : 46, colour: Palette.ink)
        name.position = CGPoint(x: 0, y: -100)
        card.addChild(name)

        let stars = coordinator.starCount(for: child.id)
        let star = Sketch.node(.polygon(Sketch.starPoints(center: .zero, radius: 20)), fill: Palette.sun, lineWidth: 3.5,
                               wobble: 1, seed: 1302 + UInt64(index) * 5)
        star.position = CGPoint(x: -34, y: -158)
        card.addChild(star)
        let count = Sketch.label("\(stars)", size: 36, colour: Palette.ink)
        count.horizontalAlignmentMode = .left
        count.position = CGPoint(x: -6, y: -158)
        card.addChild(count)
        return card
    }

    override func handleTap(name: String, node: SKNode) {
        guard name.hasPrefix("tap:child:"), let id = UUID(uuidString: String(name.dropFirst("tap:child:".count))) else { return }
        inputLocked = true
        sfx.play(.chime)
        Buttons.press(node)
        bip.celebrate()
        after(0.4) { [weak self] in self?.coordinator.choose(childID: id) }
    }

    override func didTapBip() {
        replayPrompt()
    }

    override func replayPrompt() {
        voice.play([VoiceLine.whoIsPlaying.rawValue])
        bip.hop()
    }

    /// Already the first screen: home does nothing here.
    override func goHome() {}
}
