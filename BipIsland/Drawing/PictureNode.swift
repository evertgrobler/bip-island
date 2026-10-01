import SpriteKit

/// Hand-drawn pictures for the picture words. Each fits in roughly a 220 × 220 box around the origin.
enum PictureNode {
    static func make(word: String) -> SKNode {
        switch word {
        case "sun": return sun()
        case "apple": return apple()
        case "tent": return tent()
        case "pig": return pig()
        case "igloo": return igloo()
        case "nest": return nest()
        default: return mystery(word)
        }
    }

    private static func sun() -> SKNode {
        let n = SKNode()
        for i in 0..<10 {
            let a = CGFloat(i) / 10 * 2 * .pi
            let ray = SketchShape.polyline([
                CGPoint(x: 72 * cos(a), y: 72 * sin(a)),
                CGPoint(x: 100 * cos(a), y: 100 * sin(a)),
            ])
            n.addChild(Sketch.node(ray, ink: Palette.orange, lineWidth: 8, seed: 300 + UInt64(i)))
        }
        n.addChild(Sketch.node(.ellipse(center: .zero, rx: 60, ry: 60), fill: Palette.sun, seed: 311))
        n.addChild(dot(-20, 12, 6))
        n.addChild(dot(20, 12, 6))
        n.addChild(Sketch.node(.arc(center: CGPoint(x: 0, y: -2), rx: 26, ry: 20, from: .pi * 1.15, to: .pi * 1.85), lineWidth: 5, seed: 312))
        return n
    }

    private static func apple() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.polyline([CGPoint(x: 0, y: 48), CGPoint(x: 4, y: 70), CGPoint(x: 10, y: 88)]), ink: Palette.brown, lineWidth: 9, seed: 320))
        let leaf = Sketch.node(.ellipse(center: .zero, rx: 28, ry: 12), fill: Palette.leaf, lineWidth: 4, seed: 321)
        leaf.position = CGPoint(x: 36, y: 78)
        leaf.zRotation = 0.5
        n.addChild(leaf)
        let body = SketchShape.polygon([
            CGPoint(x: 0, y: 44), CGPoint(x: 30, y: 60), CGPoint(x: 66, y: 44), CGPoint(x: 80, y: 0),
            CGPoint(x: 66, y: -50), CGPoint(x: 34, y: -76), CGPoint(x: 0, y: -66), CGPoint(x: -34, y: -76),
            CGPoint(x: -66, y: -50), CGPoint(x: -80, y: 0), CGPoint(x: -66, y: 44), CGPoint(x: -30, y: 60),
        ])
        n.addChild(Sketch.node(body, fill: Palette.red, seed: 322))
        let shine = SKShapeNode(ellipseOf: CGSize(width: 18, height: 34))
        shine.fillColor = NSColor.white.withAlphaComponent(0.55)
        shine.strokeColor = .clear
        shine.position = CGPoint(x: -40, y: 10)
        shine.zRotation = -0.3
        n.addChild(shine)
        return n
    }

    private static func tent() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.polyline([CGPoint(x: -110, y: -70), CGPoint(x: 110, y: -70)]), ink: Palette.leaf, lineWidth: 7, seed: 330))
        n.addChild(Sketch.node(.polyline([CGPoint(x: 0, y: 80), CGPoint(x: 0, y: 112)]), lineWidth: 5, seed: 331))
        n.addChild(Sketch.node(.polygon([CGPoint(x: 0, y: 112), CGPoint(x: 34, y: 102), CGPoint(x: 0, y: 92)]), fill: Palette.red, lineWidth: 4, seed: 332))
        n.addChild(Sketch.node(.polygon([CGPoint(x: -100, y: -68), CGPoint(x: 0, y: 84), CGPoint(x: 100, y: -68)]), fill: Palette.orange, seed: 333))
        n.addChild(Sketch.node(.polygon([CGPoint(x: -34, y: -68), CGPoint(x: 0, y: 14), CGPoint(x: 34, y: -68)]), fill: Palette.brown, lineWidth: 4, seed: 334))
        return n
    }

    private static func pig() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.polygon([CGPoint(x: -58, y: 30), CGPoint(x: -70, y: 92), CGPoint(x: -18, y: 58)]), fill: Palette.deepPink, lineWidth: 4.5, seed: 340))
        n.addChild(Sketch.node(.polygon([CGPoint(x: 58, y: 30), CGPoint(x: 70, y: 92), CGPoint(x: 18, y: 58)]), fill: Palette.deepPink, lineWidth: 4.5, seed: 341))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 0, y: -5), rx: 82, ry: 72), fill: Palette.pink, seed: 342))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 0, y: -30), rx: 32, ry: 22), fill: Palette.deepPink, lineWidth: 4.5, seed: 343))
        n.addChild(dot(-11, -30, 5))
        n.addChild(dot(11, -30, 5))
        n.addChild(dot(-30, 18, 7))
        n.addChild(dot(30, 18, 7))
        return n
    }

    private static func igloo() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.polyline([CGPoint(x: -115, y: -62), CGPoint(x: 115, y: -62)]), ink: Palette.sea, lineWidth: 6, seed: 350))
        var dome: [CGPoint] = (0...12).map { i in
            let a = CGFloat(i) / 12 * .pi
            return CGPoint(x: 100 * cos(a), y: -60 + 110 * sin(a))
        }
        dome.append(CGPoint(x: -100, y: -60))
        n.addChild(Sketch.node(.polygon(dome), fill: Palette.ice, seed: 351))
        for (i, height) in [CGFloat(-20), 18, 52].enumerated() {
            let halfWidth = 100 * sqrt(max(0, 1 - pow((height + 60) / 110, 2)))
            n.addChild(Sketch.node(.polyline([CGPoint(x: -halfWidth + 6, y: height), CGPoint(x: halfWidth - 6, y: height)]), ink: Palette.sea, lineWidth: 3.5, seed: 352 + UInt64(i)))
        }
        var door: [CGPoint] = (0...8).map { i in
            let a = CGFloat(i) / 8 * .pi
            return CGPoint(x: 30 * cos(a), y: -60 + 50 * sin(a))
        }
        door.append(CGPoint(x: -30, y: -60))
        n.addChild(Sketch.node(.polygon(door), fill: Palette.ink.withAlphaComponent(0.8), lineWidth: 4, seed: 356))
        return n
    }

    private static func nest() -> SKNode {
        let n = SKNode()
        for (i, x) in [CGFloat(-36), 0, 36].enumerated() {
            n.addChild(Sketch.node(.ellipse(center: CGPoint(x: x, y: i == 1 ? 18 : 6), rx: 23, ry: 30), fill: Palette.eggBlue, lineWidth: 4, seed: 360 + UInt64(i)))
        }
        var bowl: [CGPoint] = (0...14).map { i in
            let a = CGFloat.pi + CGFloat(i) / 14 * CGFloat.pi
            return CGPoint(x: 100 * cos(a), y: -4 + 62 * sin(a))
        }
        bowl.append(CGPoint(x: -100, y: -4))
        n.addChild(Sketch.node(.polygon(bowl), fill: Palette.brown, seed: 364))
        for i in 0..<6 {
            let y = -14 - CGFloat(i) * 8
            let w = 92 - CGFloat(i) * 11
            n.addChild(Sketch.node(.polyline([CGPoint(x: -w, y: y), CGPoint(x: -w / 3, y: y - 6), CGPoint(x: w / 3, y: y + 4), CGPoint(x: w, y: y - 3)]),
                                   ink: Palette.lightBrown, lineWidth: 3.5, wobble: 3, seed: 370 + UInt64(i)))
        }
        return n
    }

    private static func mystery(_ word: String) -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.ellipse(center: .zero, rx: 70, ry: 70), fill: Palette.stone, seed: 390))
        n.addChild(Sketch.label("?", size: 90, colour: Palette.ink))
        return n
    }

    private static func dot(_ x: CGFloat, _ y: CGFloat, _ r: CGFloat) -> SKNode {
        let d = SKShapeNode(circleOfRadius: r)
        d.fillColor = Palette.ink
        d.strokeColor = .clear
        d.position = CGPoint(x: x, y: y)
        return d
    }
}

/// A picture on a hand-drawn card, used in "Sound hunt".
final class PictureCard: SKNode {
    let word: String
    static let size: CGFloat = 270

    init(word: String, seed: UInt64) {
        self.word = word
        super.init()
        let half = Self.size / 2
        addChild(Sketch.node(.roundedRect(CGRect(x: -half, y: -half, width: Self.size, height: Self.size), radius: 30), fill: Palette.card, lineWidth: 6, seed: seed))
        let picture = PictureNode.make(word: word)
        picture.setScale(0.95)
        addChild(picture)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }
}
