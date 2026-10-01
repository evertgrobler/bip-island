import SpriteKit

/// Hand-drawn pictures, by picture id from Content/asset_manifest.json (pic_<word>).
/// Each fits in roughly a 220 × 220 box around the origin. Ids without real art yet show
/// an emoji stand-in (EmojiPictures) so pre-readers always see a picture, never a word.
enum PictureNode {
    static func make(picture id: String, word: String) -> SKNode {
        switch id {
        case "pic_sun": return sun()
        case "pic_ant": return ant()
        case "pic_ants": return several(ant, offsets: [CGPoint(x: -50, y: 50), CGPoint(x: 40, y: 0), CGPoint(x: -30, y: -60)], scale: 0.5)
        case "pic_tap": return tap()
        case "pic_pan": return pan()
        case "pic_ink": return ink()
        case "pic_net": return net()
        case "pic_pin": return pin()
        case "pic_pins": return several(pin, offsets: [CGPoint(x: -40, y: 20), CGPoint(x: 10, y: -10), CGPoint(x: 50, y: -40)], scale: 0.6)
        case "pic_tin": return tin()
        case "pic_apple": return apple()
        case "pic_tent": return tent()
        case "pic_pig": return pig()
        case "pic_igloo": return igloo()
        case "pic_nest": return nest()
        default: return EmojiPictures.make(picture: id) ?? placeholder(word: word)
        }
    }

    static func make(word: String) -> SKNode {
        make(picture: "pic_\(word)", word: word)
    }

    private static func sun() -> SKNode {
        let n = SKNode()
        for i in 0..<10 {
            let a = CGFloat(i) / 10 * 2 * CGFloat.pi
            let ray = SketchShape.polyline([
                CGPoint(x: 72 * cos(a), y: 72 * sin(a)),
                CGPoint(x: 100 * cos(a), y: 100 * sin(a)),
            ])
            n.addChild(Sketch.node(ray, ink: Palette.orange, lineWidth: 8, seed: 300 + UInt64(i)))
        }
        n.addChild(Sketch.node(.ellipse(center: .zero, rx: 60, ry: 60), fill: Palette.sun, seed: 311))
        n.addChild(dot(-20, 12, 6))
        n.addChild(dot(20, 12, 6))
        n.addChild(Sketch.node(.arc(center: CGPoint(x: 0, y: -2), rx: 26, ry: 20, from: CGFloat.pi * 1.15, to: CGFloat.pi * 1.85), lineWidth: 5, seed: 312))
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
            let a = CGFloat(i) / 12 * CGFloat.pi
            return CGPoint(x: 100 * cos(a), y: -60 + 110 * sin(a))
        }
        dome.append(CGPoint(x: -100, y: -60))
        n.addChild(Sketch.node(.polygon(dome), fill: Palette.ice, seed: 351))
        for (i, height) in [CGFloat(-20), 18, 52].enumerated() {
            let halfWidth = 100 * sqrt(max(0, 1 - pow((height + 60) / 110, 2)))
            n.addChild(Sketch.node(.polyline([CGPoint(x: -halfWidth + 6, y: height), CGPoint(x: halfWidth - 6, y: height)]), ink: Palette.sea, lineWidth: 3.5, seed: 352 + UInt64(i)))
        }
        var door: [CGPoint] = (0...8).map { i in
            let a = CGFloat(i) / 8 * CGFloat.pi
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

    // MARK: Phonics group 1 words

    /// A few copies of one picture, for plurals (ants, pins).
    private static func several(_ draw: () -> SKNode, offsets: [CGPoint], scale: CGFloat) -> SKNode {
        let n = SKNode()
        for offset in offsets {
            let copy = draw()
            copy.setScale(scale)
            copy.position = offset
            n.addChild(copy)
        }
        return n
    }

    private static func ant() -> SKNode {
        let n = SKNode()
        // Six legs, then antennae, then the three body parts on top.
        for (i, x) in [CGFloat(-22), 0, 22].enumerated() {
            n.addChild(Sketch.node(.polyline([CGPoint(x: x, y: -10), CGPoint(x: x - 18, y: -48), CGPoint(x: x - 30, y: -62)]), lineWidth: 5, seed: 380 + UInt64(i)))
            n.addChild(Sketch.node(.polyline([CGPoint(x: x, y: -10), CGPoint(x: x + 16, y: -48), CGPoint(x: x + 30, y: -60)]), lineWidth: 5, seed: 383 + UInt64(i)))
        }
        n.addChild(Sketch.node(.polyline([CGPoint(x: 74, y: 20), CGPoint(x: 88, y: 58), CGPoint(x: 104, y: 70)]), lineWidth: 4.5, seed: 386))
        n.addChild(Sketch.node(.polyline([CGPoint(x: 80, y: 16), CGPoint(x: 110, y: 44), CGPoint(x: 126, y: 46)]), lineWidth: 4.5, seed: 387))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: -66, y: -2), rx: 46, ry: 36), fill: Palette.red, seed: 388))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 0, y: 0), rx: 26, ry: 22), fill: Palette.red, seed: 389))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 58, y: 8), rx: 30, ry: 28), fill: Palette.red, seed: 390))
        n.addChild(dot(66, 14, 6))
        return n
    }

    private static func tap() -> SKNode {
        let n = SKNode()
        // Pipe from the wall, the spout curving down, a handle on top and a falling drop.
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -110, y: 10, width: 24, height: 70), radius: 6), fill: Palette.stone, lineWidth: 4.5, seed: 391))
        n.addChild(Sketch.node(.polygon([CGPoint(x: -88, y: 30), CGPoint(x: 40, y: 30), CGPoint(x: 62, y: 18), CGPoint(x: 70, y: -10),
                                         CGPoint(x: 42, y: -10), CGPoint(x: 38, y: 4), CGPoint(x: -88, y: 4)]),
                               fill: Palette.ice, seed: 392))
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -20, y: 30, width: 16, height: 30), radius: 4), fill: Palette.stone, lineWidth: 4, seed: 393))
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -52, y: 58, width: 80, height: 18), radius: 8), fill: Palette.red, lineWidth: 4.5, seed: 394))
        n.addChild(Sketch.node(.polygon([CGPoint(x: 56, y: -30), CGPoint(x: 70, y: -58), CGPoint(x: 62, y: -76), CGPoint(x: 50, y: -76), CGPoint(x: 42, y: -58)]),
                               fill: Palette.sea, lineWidth: 4, seed: 395))
        return n
    }

    private static func pan() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.roundedRect(CGRect(x: 52, y: -12, width: 96, height: 24), radius: 10), fill: Palette.brown, seed: 396))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: -24, y: 0), rx: 86, ry: 62), fill: Palette.ink.withAlphaComponent(0.75), seed: 397))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: -24, y: 4), rx: 66, ry: 44), fill: Palette.stone, lineWidth: 4, seed: 398))
        // A fried egg in the pan.
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: -30, y: 4), rx: 40, ry: 26), fill: .white, lineWidth: 3.5, seed: 399))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: -24, y: 6), rx: 14, ry: 12), fill: Palette.sun, lineWidth: 3.5, seed: 400))
        return n
    }

    private static func ink() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -62, y: -90, width: 124, height: 120), radius: 26), fill: Palette.purple, seed: 401))
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -26, y: 28, width: 52, height: 26), radius: 6), fill: Palette.purple, lineWidth: 4.5, seed: 402))
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -34, y: 52, width: 68, height: 30), radius: 8), fill: Palette.ink.withAlphaComponent(0.85), lineWidth: 4.5, seed: 403))
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -40, y: -60, width: 80, height: 54), radius: 8), fill: Palette.card, lineWidth: 3.5, seed: 404))
        // A drop of ink on the label.
        n.addChild(Sketch.node(.polygon([CGPoint(x: 0, y: -12), CGPoint(x: 14, y: -34), CGPoint(x: 8, y: -48), CGPoint(x: -8, y: -48), CGPoint(x: -14, y: -34)]),
                               fill: Palette.purple, lineWidth: 3.5, seed: 405))
        return n
    }

    private static func net() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.polyline([CGPoint(x: -110, y: -100), CGPoint(x: -20, y: -6)]), ink: Palette.brown, lineWidth: 10, seed: 406))
        // Mesh hanging below the hoop.
        let mesh = SKNode()
        for i in 0..<5 {
            let x = -6 + CGFloat(i) * 26
            mesh.addChild(Sketch.node(.polyline([CGPoint(x: x, y: 50), CGPoint(x: x + 10, y: -40)]), ink: Palette.ink.withAlphaComponent(0.7), lineWidth: 3, wobble: 1.5, seed: 407 + UInt64(i)))
        }
        for i in 0..<3 {
            let y = 30 - CGFloat(i) * 28
            mesh.addChild(Sketch.node(.polyline([CGPoint(x: -10 + CGFloat(i) * 8, y: y), CGPoint(x: 110 - CGFloat(i) * 6, y: y)]), ink: Palette.ink.withAlphaComponent(0.7), lineWidth: 3, wobble: 1.5, seed: 412 + UInt64(i)))
        }
        n.addChild(mesh)
        n.addChild(Sketch.node(.arc(center: CGPoint(x: 50, y: 20), rx: 62, ry: 70, from: CGFloat.pi * 1.05, to: CGFloat.pi * 1.95), lineWidth: 5, seed: 415))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 50, y: 50), rx: 66, ry: 26), ink: Palette.sun, lineWidth: 8, seed: 416))
        return n
    }

    private static func pin() -> SKNode {
        let n = SKNode()
        // A sewing pin lying across the card.
        n.addChild(Sketch.node(.polyline([CGPoint(x: -96, y: -70), CGPoint(x: 62, y: 46)]), ink: Palette.stone.blended(withFraction: 0.5, of: Palette.ink) ?? Palette.ink, lineWidth: 7, wobble: 1, seed: 417))
        n.addChild(Sketch.node(.polyline([CGPoint(x: -96, y: -70), CGPoint(x: 62, y: 46)]), ink: .white, lineWidth: 2, wobble: 1, seed: 418))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 72, y: 54), rx: 30, ry: 30), fill: Palette.red, seed: 419))
        let shine = SKShapeNode(circleOfRadius: 7)
        shine.fillColor = NSColor.white.withAlphaComponent(0.8)
        shine.strokeColor = .clear
        shine.position = CGPoint(x: 62, y: 64)
        n.addChild(shine)
        return n
    }

    private static func tin() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -64, y: -86, width: 128, height: 150), radius: 12), fill: Palette.stone, seed: 420))
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -64, y: -50, width: 128, height: 76), radius: 4), fill: Palette.red, lineWidth: 4.5, seed: 421))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 0, y: -12), rx: 26, ry: 22), fill: Palette.sun, lineWidth: 3.5, seed: 422))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 0, y: 64), rx: 64, ry: 16), fill: Palette.ice, lineWidth: 4.5, seed: 423))
        return n
    }

    /// Not drawn yet: a dashed frame with the word written in, so it's obvious this is a stand-in.
    /// Last resort for a picture with neither art nor emoji: the word, clearly marked.
    private static func placeholder(word: String) -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -100, y: -80, width: 200, height: 160), radius: 24), fill: Palette.stone.withAlphaComponent(0.5),
                               ink: Palette.ink.withAlphaComponent(0.5), lineWidth: 3, wobble: 3.5, seed: 424))
        let label = Sketch.letter(word, size: word.count > 6 ? 48 : 64, shadow: nil)
        n.addChild(label)
        let note = Sketch.label("picture coming", size: 22, colour: Palette.ink.withAlphaComponent(0.55))
        note.position = CGPoint(x: 0, y: -60)
        n.addChild(note)
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

/// A picture on a hand-drawn card, used in Meet the Sound and Sound Hunt.
final class PictureCard: SKNode {
    let word: String
    static let size: CGFloat = 270

    init(picture id: String, word: String, seed: UInt64) {
        self.word = word
        super.init()
        let half = Self.size / 2
        addChild(Sketch.node(.roundedRect(CGRect(x: -half, y: -half, width: Self.size, height: Self.size), radius: 30), fill: Palette.card, lineWidth: 6, seed: seed))
        let picture = PictureNode.make(picture: id, word: word)
        picture.setScale(0.95)
        addChild(picture)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }
}
