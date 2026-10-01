import SpriteKit

/// Pictures for the Morning Order steps (pic_<set>_<n>). A single emoji can't show a step
/// ("rinse" is not a shower, "turn it over" is not a spinning arrow), so each card is a small
/// scene: a hand-drawn ground the whole set shares (soil, sand, a pond) with emoji and drawn
/// pieces on top. Within a set the pictures build on each other — the seed card has bare
/// soil, then rain, then a shoot, then a flower — so a child can see what changes from one
/// step to the next. Each fits in roughly a 220 × 220 box around the origin, like PictureNode.
enum StepPictures {
    static func make(picture id: String) -> SKNode? {
        switch id {
        // Morning
        case "pic_morning_1": return scene(glyph("🛌", 120, -5, -40), glyph("⏰", 72, -60, 62), glyph("☀️", 62, 62, 66))
        case "pic_morning_2": return scene(glyph("😁", 120, -20, 12), glyph("🪥", 84, 58, -48, turn: 0.4))
        case "pic_morning_3", "pic_school_2": return breakfast()

        // Washing hands
        case "pic_hands_1": return scene(tap(), drops([(14, -4)]), glyph("👐", 110, 6, -58))
        case "pic_hands_2": return scene(glyph("🧼", 76, -52, 58), glyph("👐", 110, 8, -48), bubbles([(62, 52), (78, 6), (-70, -20)]))
        case "pic_hands_3": return scene(tap(), drops([(14, -4), (-30, -22), (44, -24)]), glyph("👐", 110, 6, -58),
                                         bubbles([(-80, -84), (82, -80), (-84, -40)]))
        case "pic_hands_4": return scene(glyph("🧻", 80, -56, 52), glyph("🙌", 110, 14, -40), glyph("✨", 46, 72, 58))

        // Sandwich: the same bread, a little more on it each time.
        case "pic_sandwich_1": return scene(slice(at: CGPoint(x: -54, y: 0), scale: 0.62), slice(at: CGPoint(x: 54, y: 0), scale: 0.62))
        case "pic_sandwich_2": return scene(slice(at: CGPoint(x: 0, y: -4), scale: 1), butter(), glyph("🧈", 52, 72, -72))
        case "pic_sandwich_3": return scene(slice(at: CGPoint(x: 0, y: -4), scale: 1), butter(), cheese())
        case "pic_sandwich_4": return scene(glyph("🥪", 160, 0, 0))

        // Planting a seed: the same patch of soil each time.
        case "pic_seed_1": return scene(soil(), seedInSoil(), glyph("👇", 72, 0, 50))
        case "pic_seed_2": return scene(soil(), seedInSoil(), glyph("🌧️", 120, 0, 36))
        case "pic_seed_3": return scene(soil(), glyph("🌱", 90, 0, -18))
        case "pic_seed_4": return scene(soil(), glyph("🌻", 130, 0, 6))

        // Sandcastle: one bucket on one beach.
        case "pic_sandcastle_1": return scene(sand(), glyph("🪣", 110, 14, -16), glyph("🤲", 64, -62, 64), sandGrains())
        case "pic_sandcastle_2": return scene(sand(), glyph("🪣", 110, 0, -16), glyph("✋", 72, 4, 74, turn: .pi))
        case "pic_sandcastle_3": return scene(sand(), glyph("🪣", 110, 0, -18, turn: .pi), turnArrow())
        case "pic_sandcastle_4": return scene(sand(), sandTower(), glyph("🪣", 66, 64, 76, turn: 0.3), upArrow())
        case "pic_sandcastle_5": return scene(sand(), sandTower(), glyph("🐚", 56, 0, 56), glyph("🚩", 40, -60, 64))

        // Frog life cycle: drawn by hand, in the same pond.
        case "pic_frog_1": return scene(pond(), frogspawn())
        case "pic_frog_2": return scene(pond(), tadpole(legs: false))
        case "pic_frog_3": return scene(pond(), tadpole(legs: true))
        case "pic_frog_4": return scene(pond(), frog(tail: true, scale: 0.7))
        case "pic_frog_5": return scene(pond(), lilyPad(), frog(tail: false, scale: 1))

        // Getting ready for school
        case "pic_school_1": return scene(glyph("👕", 100, -40, 34), glyph("👖", 100, 44, -30))
        case "pic_school_3": return scene(glyph("🎒", 130, 10, -24), glyph("📚", 62, -66, 60), glyph("✏️", 52, 64, 70, turn: 0.3))
        case "pic_school_4": return scene(glyph("🧦", 64, -64, 58), glyph("👟", 92, -40, -30), glyph("👟", 92, 50, -10))
        case "pic_school_5": return scene(path(), glyph("🏫", 110, -40, 32), glyph("🚶", 92, 60, -36))

        // Baking biscuits
        case "pic_biscuits_1": return scene(glyph("🧼", 72, -56, 58), glyph("👐", 110, 8, -42), drops([(62, 64), (74, 28)]))
        case "pic_biscuits_2": return scene(glyph("🥣", 130, 0, -28), glyph("🥄", 70, 46, 54, turn: -0.5), glyph("🥚", 52, -60, 60))
        case "pic_biscuits_3": return scene(doughWithShapes())
        case "pic_biscuits_4": return scene(oven())
        case "pic_biscuits_5": return scene(tray(), steam(), glyph("🍪", 56, -54, -40), glyph("🍪", 56, 0, -40), glyph("🍪", 56, 54, -40),
                                            glyph("🌬️", 66, -56, 62))
        case "pic_biscuits_6": return scene(glyph("😋", 116, -26, 22), glyph("🍪", 76, 56, -48))
        default: return nil
        }
    }

    // MARK: Building blocks

    private static func scene(_ parts: SKNode...) -> SKNode {
        let n = SKNode()
        for (i, part) in parts.enumerated() {
            part.zPosition = CGFloat(i)
            n.addChild(part)
        }
        return n
    }

    /// An emoji at a size and spot, optionally turned (radians).
    private static func glyph(_ text: String, _ size: CGFloat, _ x: CGFloat, _ y: CGFloat, turn: CGFloat = 0) -> SKNode {
        let label = SKLabelNode(fontNamed: "AppleColorEmoji")
        label.text = text
        label.fontSize = size
        label.verticalAlignmentMode = .center
        label.horizontalAlignmentMode = .center
        let holder = SKNode()
        holder.addChild(label)
        holder.position = CGPoint(x: x, y: y)
        holder.zRotation = turn
        return holder
    }

    private static func breakfast() -> SKNode {
        scene(glyph("🥣", 124, 0, -32), glyph("🥛", 64, -62, 58), glyph("🍌", 64, 62, 58))
    }

    private static func drops(_ spots: [(CGFloat, CGFloat)]) -> SKNode {
        let n = SKNode()
        for (x, y) in spots { n.addChild(glyph("💧", 34, x, y)) }
        return n
    }

    private static func bubbles(_ spots: [(CGFloat, CGFloat)]) -> SKNode {
        let n = SKNode()
        for (x, y) in spots { n.addChild(glyph("🫧", 40, x, y)) }
        return n
    }

    /// The hand-drawn tap from PictureNode, smaller and up in the corner.
    private static func tap() -> SKNode {
        let n = PictureNode.tap()
        n.setScale(0.6)
        n.position = CGPoint(x: -20, y: 62)
        return n
    }

    /// A slice of bread: puffy top, golden crust, soft middle.
    private static func slice(at point: CGPoint, scale: CGFloat) -> SKNode {
        let outline: [CGPoint] = [
            CGPoint(x: -66, y: -80), CGPoint(x: 66, y: -80), CGPoint(x: 68, y: 26), CGPoint(x: 88, y: 46),
            CGPoint(x: 84, y: 76), CGPoint(x: 50, y: 92), CGPoint(x: 0, y: 86), CGPoint(x: -50, y: 92),
            CGPoint(x: -84, y: 76), CGPoint(x: -88, y: 46), CGPoint(x: -68, y: 26),
        ]
        let n = SKNode()
        n.addChild(Sketch.node(.polygon(outline), fill: Palette.lightBrown, seed: 1195))
        n.addChild(Sketch.node(.polygon(outline.map { CGPoint(x: $0.x * 0.8, y: $0.y * 0.8 - 2) }),
                               fill: Palette.paper.blended(withFraction: 0.2, of: Palette.sand) ?? Palette.paper, lineWidth: 0, seed: 1196))
        n.setScale(scale)
        n.position = point
        return n
    }

    private static func butter() -> SKNode {
        Sketch.node(.polygon([CGPoint(x: -50, y: -30), CGPoint(x: -10, y: -44), CGPoint(x: 46, y: -36), CGPoint(x: 52, y: 10),
                              CGPoint(x: 40, y: 42), CGPoint(x: -20, y: 48), CGPoint(x: -54, y: 20)]),
                    fill: Palette.sun.blended(withFraction: 0.45, of: .white) ?? Palette.sun, ink: Palette.sun, lineWidth: 3, wobble: 4, seed: 1197)
    }

    /// A square cheese slice with holes, lying a little crooked.
    private static func cheese() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -50, y: -50, width: 100, height: 100), radius: 6), fill: Palette.sun, lineWidth: 4.5, seed: 1198))
        for (i, (x, y, r)) in [(CGFloat(-20), CGFloat(18), CGFloat(10)), (22, -14, 12), (-14, -26, 7), (26, 26, 6)].enumerated() {
            n.addChild(Sketch.node(.ellipse(center: CGPoint(x: x, y: y), rx: r, ry: r), fill: Palette.orange.withAlphaComponent(0.6), lineWidth: 2.5, seed: 1199 + UInt64(i) * 3))
        }
        n.zRotation = 0.18
        n.position = CGPoint(x: 6, y: 6)
        return n
    }

    /// A low mound along the bottom of the card.
    private static func mound(fill: NSColor, seed: UInt64) -> SKNode {
        var top: [CGPoint] = (0...10).map { i in
            let t = CGFloat(i) / 10
            return CGPoint(x: -108 + 216 * t, y: -66 + 22 * sin(t * .pi))
        }
        top.append(CGPoint(x: 108, y: -104))
        top.append(CGPoint(x: -108, y: -104))
        return Sketch.node(.polygon(top), fill: fill, lineWidth: 4.5, seed: seed)
    }

    private static func soil() -> SKNode { mound(fill: Palette.brown, seed: 1200) }
    private static func sand() -> SKNode { mound(fill: Palette.sand, seed: 1201) }

    private static func seedInSoil() -> SKNode {
        let n = Sketch.node(.ellipse(center: .zero, rx: 13, ry: 9), fill: Palette.sun, lineWidth: 3.5, seed: 1202)
        n.position = CGPoint(x: 0, y: -66)
        return n
    }

    private static func path() -> SKNode {
        Sketch.node(.polyline([CGPoint(x: -100, y: -60), CGPoint(x: -20, y: -72), CGPoint(x: 60, y: -86), CGPoint(x: 104, y: -94)]),
                    ink: Palette.stone.blended(withFraction: 0.3, of: Palette.ink) ?? Palette.stone, lineWidth: 14, wobble: 3, seed: 1203)
    }

    private static func sandGrains() -> SKNode {
        let n = SKNode()
        for (i, (x, y)) in [(-34, 40), (-20, 22), (-6, 34), (-28, 8)].enumerated() {
            n.addChild(Sketch.node(.ellipse(center: CGPoint(x: CGFloat(x), y: CGFloat(y)), rx: 5, ry: 5), fill: Palette.sand, lineWidth: 2.5, seed: 1204 + UInt64(i)))
        }
        return n
    }

    /// A curved arrow over the top: "turn it over".
    private static func turnArrow() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.arc(center: CGPoint(x: 0, y: 40), rx: 76, ry: 56, from: 0.25, to: .pi - 0.25), ink: Palette.orange, lineWidth: 9, seed: 1210))
        n.addChild(Sketch.node(.polygon([CGPoint(x: -92, y: 58), CGPoint(x: -52, y: 62), CGPoint(x: -78, y: 30)]), fill: Palette.orange, ink: Palette.orange, lineWidth: 4, seed: 1211))
        return n
    }

    private static func upArrow() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.polyline([CGPoint(x: 0, y: 46), CGPoint(x: 0, y: 92)]), ink: Palette.orange, lineWidth: 9, seed: 1212))
        n.addChild(Sketch.node(.polygon([CGPoint(x: -18, y: 84), CGPoint(x: 0, y: 106), CGPoint(x: 18, y: 84)]), fill: Palette.orange, ink: Palette.orange, lineWidth: 4, seed: 1213))
        n.position = CGPoint(x: -64, y: -4)
        return n
    }

    /// The bucket's shape in sand: wide at the bottom, little turrets on top.
    private static func sandTower() -> SKNode {
        Sketch.node(.polygon([
            CGPoint(x: -62, y: -62), CGPoint(x: 62, y: -62), CGPoint(x: 46, y: 28),
            CGPoint(x: 46, y: 42), CGPoint(x: 28, y: 42), CGPoint(x: 28, y: 30), CGPoint(x: 9, y: 30),
            CGPoint(x: 9, y: 42), CGPoint(x: -9, y: 42), CGPoint(x: -9, y: 30), CGPoint(x: -28, y: 30),
            CGPoint(x: -28, y: 42), CGPoint(x: -46, y: 42), CGPoint(x: -46, y: 28),
        ]), fill: Palette.sand.blended(withFraction: 0.15, of: Palette.lightBrown) ?? Palette.sand, seed: 1214)
    }

    private static func pond() -> SKNode {
        Sketch.node(.ellipse(center: CGPoint(x: 0, y: -10), rx: 112, ry: 92), fill: Palette.sea.withAlphaComponent(0.55),
                    ink: Palette.sea.blended(withFraction: 0.4, of: Palette.ink) ?? Palette.sea, lineWidth: 4, seed: 1220)
    }

    private static func frogspawn() -> SKNode {
        let n = SKNode()
        let spots: [(CGFloat, CGFloat)] = [(-46, 18), (-6, 30), (34, 20), (-28, -16), (12, -8), (52, -20), (-60, -40), (-12, -50), (28, -52)]
        for (i, (x, y)) in spots.enumerated() {
            n.addChild(Sketch.node(.ellipse(center: CGPoint(x: x, y: y), rx: 22, ry: 22), fill: Palette.ice.withAlphaComponent(0.85),
                                   ink: Palette.ink.withAlphaComponent(0.5), lineWidth: 3, seed: 1221 + UInt64(i)))
            let egg = SKShapeNode(circleOfRadius: 7)
            egg.fillColor = Palette.ink
            egg.strokeColor = .clear
            egg.position = CGPoint(x: x + 2, y: y - 1)
            n.addChild(egg)
        }
        return n
    }

    private static func tadpole(legs: Bool) -> SKNode {
        let n = SKNode()
        let dark = Palette.ink.withAlphaComponent(0.85)
        n.addChild(Sketch.node(.polyline([CGPoint(x: 0, y: 0), CGPoint(x: 34, y: 16), CGPoint(x: 64, y: -10), CGPoint(x: 96, y: 8)]),
                               ink: dark, lineWidth: 12, wobble: 1.5, seed: 1240))
        if legs {
            n.addChild(Sketch.node(.polyline([CGPoint(x: 12, y: -14), CGPoint(x: 30, y: -44), CGPoint(x: 18, y: -60)]), ink: dark, lineWidth: 7, seed: 1241))
            n.addChild(Sketch.node(.polyline([CGPoint(x: 12, y: 14), CGPoint(x: 30, y: 44), CGPoint(x: 18, y: 60)]), ink: dark, lineWidth: 7, seed: 1242))
        }
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: -26, y: 0), rx: 46, ry: 36), fill: dark, seed: 1243))
        let eye = SKShapeNode(circleOfRadius: 8)
        eye.fillColor = .white
        eye.strokeColor = .clear
        eye.position = CGPoint(x: -48, y: 12)
        n.addChild(eye)
        n.position = CGPoint(x: -10, y: -6)
        return n
    }

    private static func lilyPad() -> SKNode {
        // A round leaf with a notch cut out towards the right.
        let rim: [CGPoint] = (0...16).map { i in
            let a = 0.35 + CGFloat(i) / 16 * (2 * .pi - 0.7)
            return CGPoint(x: 100 * cos(a), y: -46 + 40 * sin(a))
        }
        return Sketch.node(.polygon(rim + [CGPoint(x: 0, y: -46)]), fill: Palette.grass.blended(withFraction: 0.25, of: Palette.leaf) ?? Palette.grass, seed: 1250)
    }

    /// A frog seen from the front. A froglet still has a stubby tail.
    private static func frog(tail: Bool, scale: CGFloat) -> SKNode {
        let n = SKNode()
        let green = Palette.leaf
        if tail {
            n.addChild(Sketch.node(.polyline([CGPoint(x: 50, y: -30), CGPoint(x: 86, y: -40), CGPoint(x: 108, y: -30)]), ink: green, lineWidth: 12, seed: 1260))
        }
        // Back legs folded at the sides, then the body, front feet and the eyes on top.
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: -64, y: -40), rx: 34, ry: 22), fill: green, lineWidth: 4.5, seed: 1261))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 64, y: -40), rx: 34, ry: 22), fill: green, lineWidth: 4.5, seed: 1262))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 0, y: -8), rx: 72, ry: 52), fill: Palette.grass, seed: 1263))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: -26, y: -58), rx: 18, ry: 10), fill: green, lineWidth: 4, seed: 1264))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 26, y: -58), rx: 18, ry: 10), fill: green, lineWidth: 4, seed: 1265))
        for (i, x) in [CGFloat(-34), 34].enumerated() {
            n.addChild(Sketch.node(.ellipse(center: CGPoint(x: x, y: 40), rx: 22, ry: 22), fill: .white, lineWidth: 4.5, seed: 1266 + UInt64(i)))
            let pupil = SKShapeNode(circleOfRadius: 9)
            pupil.fillColor = Palette.ink
            pupil.strokeColor = .clear
            pupil.position = CGPoint(x: x, y: 38)
            n.addChild(pupil)
        }
        n.addChild(Sketch.node(.arc(center: CGPoint(x: 0, y: 0), rx: 34, ry: 20, from: .pi * 1.15, to: .pi * 1.85), lineWidth: 4.5, seed: 1268))
        n.setScale(scale)
        n.position = CGPoint(x: tail ? -14 : 0, y: tail ? -6 : 4)
        return n
    }

    /// Rolled-out dough with a star and a circle already cut out.
    private static func doughWithShapes() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -100, y: -78, width: 200, height: 150), radius: 30), fill: Palette.sand, seed: 1270))
        let hole = Palette.lightBrown
        n.addChild(Sketch.node(.polygon(Sketch.starPoints(center: CGPoint(x: -40, y: 2), radius: 40)), fill: hole, lineWidth: 4, seed: 1271))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 46, y: 22), rx: 26, ry: 26), fill: hole, lineWidth: 4, seed: 1272))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 46, y: -40), rx: 20, ry: 20), fill: hole, lineWidth: 4, seed: 1273))
        return n
    }

    /// An oven with biscuits glowing behind the glass.
    private static func oven() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -96, y: -100, width: 192, height: 196), radius: 16), fill: Palette.stone, seed: 1280))
        n.addChild(Sketch.node(.polyline([CGPoint(x: -96, y: 52), CGPoint(x: 96, y: 52)]), lineWidth: 4.5, seed: 1281))
        for (i, x) in [CGFloat(-56), -16, 24, 64].enumerated() {
            n.addChild(Sketch.node(.ellipse(center: CGPoint(x: x, y: 74), rx: 11, ry: 11), fill: i == 3 ? Palette.red : Palette.ink.withAlphaComponent(0.7), lineWidth: 3, seed: 1282 + UInt64(i)))
        }
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -60, y: 26, width: 120, height: 14), radius: 7), fill: Palette.ink.withAlphaComponent(0.7), lineWidth: 3.5, seed: 1286))
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -72, y: -82, width: 144, height: 92), radius: 12), fill: Palette.orange.withAlphaComponent(0.85), lineWidth: 4.5, seed: 1287))
        for (i, x) in [CGFloat(-36), 0, 36].enumerated() {
            n.addChild(Sketch.node(.ellipse(center: CGPoint(x: x, y: -40), rx: 15, ry: 15), fill: Palette.lightBrown, lineWidth: 3, seed: 1288 + UInt64(i)))
        }
        return n
    }

    private static func tray() -> SKNode {
        Sketch.node(.roundedRect(CGRect(x: -100, y: -74, width: 200, height: 22), radius: 8), fill: Palette.stone, lineWidth: 4.5, seed: 1291)
    }

    /// Wavy lines rising: still warm.
    private static func steam() -> SKNode {
        let n = SKNode()
        for (i, x) in [CGFloat(-20), 24, 68].enumerated() {
            n.addChild(Sketch.node(.polyline([CGPoint(x: x, y: -6), CGPoint(x: x + 10, y: 14), CGPoint(x: x - 6, y: 34), CGPoint(x: x + 6, y: 54)]),
                                   ink: Palette.ink.withAlphaComponent(0.3), lineWidth: 5, wobble: 1.5, seed: 1292 + UInt64(i)))
        }
        return n
    }
}
