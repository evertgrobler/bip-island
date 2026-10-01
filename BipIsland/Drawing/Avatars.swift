import SpriteKit

/// The animal pictures children pick their profile by (ProfileRules.avatars): a big animal on a
/// coloured circle, so a child who can't read yet can still find their own.
enum Avatars {
    static let emoji: [String: String] = [
        "lion": "🦁", "penguin": "🐧", "tortoise": "🐢", "zebra": "🦓",
        "giraffe": "🦒", "elephant": "🐘", "crab": "🦀", "rhino": "🦏",
    ]

    static let colours: [String: NSColor] = [
        "lion": Palette.sun, "penguin": Palette.sea, "tortoise": Palette.grass, "zebra": Palette.purple,
        "giraffe": Palette.orange, "elephant": Palette.lightTeal, "crab": Palette.red, "rhino": Palette.pink,
    ]

    /// A round badge with the animal, about `radius` points across from the centre.
    static func badge(_ id: String, radius: CGFloat, seed: UInt64) -> SKNode {
        let node = SKNode()
        node.addChild(Sketch.node(.ellipse(center: .zero, rx: radius, ry: radius),
                                  fill: colours[id] ?? Palette.sun, lineWidth: 5, seed: seed))
        let animal = SKLabelNode(fontNamed: "AppleColorEmoji")
        animal.text = emoji[id] ?? "🦁"
        animal.fontSize = radius * 1.15
        animal.verticalAlignmentMode = .center
        animal.horizontalAlignmentMode = .center
        node.addChild(animal)
        return node
    }
}
