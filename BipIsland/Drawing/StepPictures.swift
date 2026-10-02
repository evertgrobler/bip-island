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
        // Bedtime
        case "pic_bedtime_1": return scene(glyph("😁", 120, -20, 12), glyph("🪥", 84, 58, -48, turn: 0.4))
        case "pic_bedtime_2": return scene(glyph("📖", 120, 10, -24), glyph("🧸", 72, -62, 56))
        case "pic_bedtime_3": return scene(lightSwitch(), glyph("👆", 66, 62, -56), glyph("🌙", 56, -64, 66))
        case "pic_bedtime_4": return scene(glyph("🛌", 124, 0, -34), glyph("💤", 58, 52, 60), glyph("🌙", 54, -62, 64))

        // Bath time: the same bath, tap at the corner.
        case "pic_bath_1": return scene(glyph("🛁", 130, 0, -46), drops([(-34, 66), (-6, 44), (22, 70)]))
        case "pic_bath_2": return scene(glyph("🧒", 70, -16, 6), glyph("🛁", 130, 0, -36), glyph("🦆", 40, 46, -4))
        case "pic_bath_3": return scene(glyph("🧒", 70, -16, 6), glyph("🛁", 130, 0, -36), glyph("🧼", 50, 62, 60),
                                        bubbles([(-70, 40), (30, 30), (-40, 64)]))
        case "pic_bath_4": return scene(glyph("🛁", 130, 0, -36), glyph("🌀", 54, 0, 62), glyph("🦆", 40, -70, -80))

        // Chick hatching
        case "pic_chick_1": return scene(glyph("🪺", 140, 0, -8))
        case "pic_chick_2": return scene(glyph("🥚", 130, 0, -4), crack())
        case "pic_chick_3": return scene(glyph("🐣", 140, 0, -4))
        case "pic_chick_4": return scene(glyph("🐔", 130, 10, 6), glyph("🌾", 56, -68, -64))

        // Butterfly life cycle
        case "pic_butterfly_1": return scene(leaf(at: CGPoint(x: 0, y: -4), scale: 1), tinyEggs())
        case "pic_butterfly_2": return scene(leaf(at: CGPoint(x: 0, y: -30), scale: 0.8), glyph("🐛", 84, 6, 30))
        case "pic_butterfly_3": return scene(chrysalis())
        case "pic_butterfly_4": return scene(glyph("🦋", 120, 0, 18), glyph("🌸", 56, -62, -64), glyph("🌼", 50, 62, -66))

        // Weather: the same puddle fills, then a rainbow.
        case "pic_rainbow_1": return scene(glyph("☁️", 104, -38, 34), glyph("☁️", 84, 48, -14))
        case "pic_rainbow_2": return scene(puddle(), glyph("🌧️", 124, 0, 24))
        case "pic_rainbow_3": return scene(puddle(), glyph("🌦️", 124, 0, 24))
        case "pic_rainbow_4": return scene(puddle(), glyph("🌈", 150, 0, 22))

        // Birthday
        case "pic_birthday_1": return scene(oven())
        case "pic_birthday_2": return scene(glyph("🎂", 140, 0, -18), glyph("🕯️", 52, 66, 64))
        case "pic_birthday_3": return scene(glyph("🥳", 76, -54, 54), glyph("🎂", 110, 10, -34), glyph("🎶", 52, 64, 62))
        case "pic_birthday_4": return scene(glyph("🎂", 110, 26, -34), glyph("🌬️", 70, -60, 52), glyph("💨", 46, -6, 34))

        // Walking the dog
        case "pic_dogwalk_1": return scene(lead(), glyph("🐕", 124, 10, -24))
        case "pic_dogwalk_2": return scene(path(), glyph("🌳", 96, 54, 42), glyph("🚶", 84, -52, -10), glyph("🐕", 66, 14, -52))
        case "pic_dogwalk_3": return scene(throwArc(), ball(), glyph("🐕", 104, -30, -40))
        case "pic_dogwalk_4": return scene(glyph("🐕", 104, -34, 16), waterBowl(), drops([(56, -20)]))

        // Ice cream on a hot day
        case "pic_icecream_1": return scene(glyph("🍦", 150, 0, 0))
        case "pic_icecream_2": return scene(glyph("☀️", 80, -56, 60), glyph("🍦", 124, 24, -20), creamDrips())
        case "pic_icecream_3": return scene(glyph("☀️", 80, -56, 60), meltedIceCream())

        // Pizza: the same base, a little more on it each time.
        case "pic_pizza_1": return scene(pizzaBase(), rollingPin())
        case "pic_pizza_2": return scene(pizzaBase(), sauce())
        case "pic_pizza_3": return scene(pizzaBase(), sauce(), cheeseBits())
        case "pic_pizza_4": return scene(glyph("🍕", 130, 10, -20), glyph("😋", 64, -60, 60))

        // Building a tower
        case "pic_tower_1": return scene(floor(), block(0, -66, Palette.red, seed: 1400))
        case "pic_tower_2": return scene(floor(), block(0, -66, Palette.red, seed: 1400), block(0, -2, Palette.sea, seed: 1401))
        case "pic_tower_3": return scene(floor(), block(0, -66, Palette.red, seed: 1400), block(0, -2, Palette.sea, seed: 1401),
                                         block(0, 62, Palette.sun, seed: 1402))
        case "pic_tower_4": return scene(floor(), block(-58, -66, Palette.red, seed: 1400, turn: 0.2), block(12, -70, Palette.sea, seed: 1401, turn: -0.5),
                                         block(74, -60, Palette.sun, seed: 1402, turn: 0.9), glyph("💥", 50, -10, 30))

        // Apple tree: the same tree through the seasons.
        case "pic_appletree_1": return scene(glyph("🌳", 170, 0, 6), glyph("🌸", 36, -30, 40), glyph("🌸", 34, 28, 56), glyph("🌸", 34, 8, 16))
        case "pic_appletree_2": return scene(glyph("🌳", 170, 0, 6), glyph("🍏", 30, -30, 40), glyph("🍏", 30, 28, 56), glyph("🍏", 30, 8, 16))
        case "pic_appletree_3": return scene(glyph("🌳", 170, 0, 6), glyph("🍎", 44, -30, 40), glyph("🍎", 44, 28, 56), glyph("🍎", 44, 8, 16))
        case "pic_appletree_4": return scene(glyph("🍎", 72, -40, 46), glyph("🧺", 110, 18, -40))

        // Cereal for breakfast: the same bowl fills up.
        case "pic_cereal_1": return scene(bowl(), glyph("🥄", 60, 74, 40, turn: -0.5))
        case "pic_cereal_2": return scene(cerealBits(), bowl(), fallingBits())
        case "pic_cereal_3": return scene(milk(), cerealBits(), bowl(), glyph("🥛", 66, -64, 60))
        case "pic_cereal_4": return scene(milk(), cerealBits(), bowl(), glyph("🥄", 60, 40, 50, turn: -0.5), glyph("😋", 60, -60, 62))

        // Getting ready to go out
        case "pic_goingout_1": return scene(glyph("🧦", 130, 0, 0))
        case "pic_goingout_2": return scene(glyph("🧦", 68, -58, 54), glyph("👟", 112, 14, -26))
        case "pic_goingout_3": return scene(glyph("🧢", 100, 0, 40), glyph("👟", 80, 0, -58))
        case "pic_goingout_4": return scene(glyph("🌳", 96, -52, 30), glyph("🧒", 84, 30, 10), glyph("⚽", 56, 60, -62), glyph("☀️", 46, 66, 70))

        // Car wash: the same car gets cleaner.
        case "pic_carwash_1": return scene(glyph("🚗", 140, 0, -14), mud())
        case "pic_carwash_2": return scene(glyph("🚗", 140, 0, -14), mud(), glyph("💦", 60, -56, 62), drops([(-10, 54), (30, 70)]))
        case "pic_carwash_3": return scene(glyph("🚗", 140, 0, -14), glyph("🧽", 60, 52, 58), bubbles([(-60, 30), (-10, 46), (20, 24)]))
        case "pic_carwash_4": return scene(glyph("🚗", 140, 0, -14), glyph("✨", 50, -60, 58), glyph("✨", 42, 62, 44))

        // A day: the same hill, the sun moving across the sky.
        case "pic_day_1": return scene(sky(Palette.pink), glyph("☀️", 74, -66, -40), hill())
        case "pic_day_2": return scene(sky(Palette.sea), glyph("☀️", 84, 0, 54), hill())
        case "pic_day_3": return scene(sky(Palette.orange), glyph("☀️", 74, 66, -40), hill())
        case "pic_day_4": return scene(sky(Palette.ink.blended(withFraction: 0.25, of: Palette.sea) ?? Palette.ink), glyph("🌙", 70, 40, 50),
                                       glyph("⭐", 30, -60, 60), glyph("⭐", 24, -20, 30), hill())

        // Playground slide: the same slide, the child moving along it.
        case "pic_slide_1": return scene(slide(), glyph("🧒", 58, -76, -6))
        case "pic_slide_2": return scene(slide(), glyph("🧒", 58, -30, 84))
        case "pic_slide_3": return scene(slide(), glyph("🧒", 58, 34, 22))
        case "pic_slide_4": return scene(slide(), glyph("🧒", 58, 84, -60))

        // Washing clothes
        case "pic_laundry_1": return scene(glyph("👕", 130, 0, 0), mud())
        case "pic_laundry_2": return scene(washingMachine())
        case "pic_laundry_3": return scene(washingLine(), glyph("👕", 66, -40, 6), glyph("👖", 66, 42, -2), glyph("☀️", 44, 70, 76))
        case "pic_laundry_4": return scene(foldedPile(), glyph("✨", 44, 66, 56))

        // A picnic: the same blanket under the same tree.
        case "pic_picnic_1": return scene(glyph("🧺", 120, 0, -26), glyph("🥪", 52, -56, 62), glyph("🍎", 46, 56, 62))
        case "pic_picnic_2": return scene(glyph("🌳", 96, -50, 50), blanket())
        case "pic_picnic_3": return scene(glyph("🌳", 96, -50, 50), blanket(), glyph("🥪", 48, -30, -40), glyph("🍎", 40, 26, -34),
                                          glyph("🧃", 44, 62, -56))
        case "pic_picnic_4": return scene(glyph("🌳", 96, -50, 50), glyph("🗑️", 100, 46, -24), glyph("🧃", 40, 46, 56))

        // Sending a letter
        case "pic_letter_1": return scene(glyph("📄", 140, -10, 0), scribble(), glyph("🖍️", 58, 66, -60))
        case "pic_letter_2": return scene(glyph("✉️", 140, 0, 0))
        case "pic_letter_3": return scene(glyph("📮", 140, 0, -6), glyph("✉️", 46, 66, 70, turn: -0.3))
        case "pic_letter_4": return scene(glyph("👵", 110, -30, 14), glyph("💌", 70, 56, -46))

        // Growing up
        case "pic_growing_1": return scene(glyph("👶", 140, 0, 0))
        case "pic_growing_2": return scene(glyph("🧒", 140, 0, 0))
        case "pic_growing_3": return scene(glyph("🧑", 140, 0, 0))
        case "pic_growing_4": return scene(glyph("👵", 140, 0, 0))

        // Building a house: the same house goes up.
        case "pic_house_1": return scene(soil(), glyph("🚜", 104, 0, 10))
        case "pic_house_2": return scene(houseWalls())
        case "pic_house_3": return scene(houseWalls(), roof())
        case "pic_house_4": return scene(houseWalls(), roof(), houseFront(), glyph("👨‍👩‍👧", 52, 60, -74))

        // Rocket to the moon (the rocket emoji leans; a quarter turn left stands it up).
        case "pic_rocket_1": return scene(glyph("🚀", 120, 40, 20, turn: .pi / 4), glyph("🧑‍🚀", 86, -50, -34))
        case "pic_rocket_2": return scene(glyph("3️⃣", 56, -64, 64), glyph("2️⃣", 56, 0, 64), glyph("1️⃣", 56, 64, 64),
                                          glyph("🚀", 96, 0, -40, turn: .pi / 4))
        case "pic_rocket_3": return scene(glyph("🚀", 100, 0, 30, turn: .pi / 4), glyph("🔥", 50, 0, -46), glyph("💨", 50, -54, -70),
                                          glyph("💨", 50, 54, -70, turn: .pi))
        case "pic_rocket_4": return scene(glyph("⭐", 30, -70, 70), glyph("⭐", 24, 70, 40), glyph("🌕", 140, 0, -50), glyph("🚀", 76, 0, 56, turn: .pi / 4))
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

    // MARK: More building blocks

    /// A big green leaf with a vein down the middle.
    private static func leaf(at point: CGPoint, scale: CGFloat) -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.polygon([CGPoint(x: -100, y: -10), CGPoint(x: -60, y: 46), CGPoint(x: 0, y: 62), CGPoint(x: 60, y: 46),
                                         CGPoint(x: 100, y: 0), CGPoint(x: 60, y: -50), CGPoint(x: 0, y: -66), CGPoint(x: -60, y: -52)]),
                               fill: Palette.grass, seed: 1430))
        n.addChild(Sketch.node(.polyline([CGPoint(x: -100, y: -10), CGPoint(x: 0, y: -2), CGPoint(x: 92, y: 0)]), ink: Palette.leaf, lineWidth: 4, seed: 1431))
        n.zRotation = 0.15
        n.setScale(scale)
        n.position = point
        return n
    }

    private static func ball() -> SKNode {
        let n = Sketch.node(.ellipse(center: .zero, rx: 20, ry: 20), fill: Palette.sun.blended(withFraction: 0.3, of: Palette.grass) ?? Palette.sun, lineWidth: 4, seed: 1432)
        n.position = CGPoint(x: 60, y: 66)
        return n
    }

    private static func lightSwitch() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -42, y: -62, width: 84, height: 124), radius: 12), fill: Palette.card, lineWidth: 5, seed: 1300))
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -16, y: -40, width: 32, height: 44), radius: 8), fill: Palette.stone, lineWidth: 4, seed: 1301))
        n.position = CGPoint(x: -6, y: 4)
        return n
    }

    /// A zigzag crack across the egg.
    private static func crack() -> SKNode {
        Sketch.node(.polyline([CGPoint(x: -40, y: 6), CGPoint(x: -22, y: 20), CGPoint(x: -8, y: 0), CGPoint(x: 8, y: 22),
                               CGPoint(x: 22, y: 2), CGPoint(x: 40, y: 16)]), lineWidth: 4.5, wobble: 1, seed: 1302)
    }

    private static func tinyEggs() -> SKNode {
        let n = SKNode()
        for (i, (x, y)) in [(-14, 4), (6, 12), (2, -10), (20, -4)].enumerated() {
            n.addChild(Sketch.node(.ellipse(center: CGPoint(x: CGFloat(x), y: CGFloat(y)), rx: 7, ry: 9), fill: .white, lineWidth: 2.5, seed: 1303 + UInt64(i)))
        }
        return n
    }

    /// A green chrysalis hanging from a twig.
    private static func chrysalis() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.polyline([CGPoint(x: -104, y: 80), CGPoint(x: 0, y: 72), CGPoint(x: 104, y: 84)]), ink: Palette.brown, lineWidth: 10, seed: 1310))
        n.addChild(Sketch.node(.polyline([CGPoint(x: 0, y: 74), CGPoint(x: 0, y: 54)]), lineWidth: 3.5, seed: 1311))
        n.addChild(Sketch.node(.polygon([CGPoint(x: 0, y: 56), CGPoint(x: 22, y: 40), CGPoint(x: 30, y: 0), CGPoint(x: 22, y: -44),
                                         CGPoint(x: 0, y: -74), CGPoint(x: -22, y: -44), CGPoint(x: -30, y: 0), CGPoint(x: -22, y: 40)]),
                               fill: Palette.grass, seed: 1312))
        for (i, y) in [CGFloat(20), -6, -32].enumerated() {
            n.addChild(Sketch.node(.polyline([CGPoint(x: -20, y: y), CGPoint(x: 20, y: y - 6)]), ink: Palette.leaf, lineWidth: 3, seed: 1313 + UInt64(i)))
        }
        return n
    }

    private static func puddle() -> SKNode {
        Sketch.node(.ellipse(center: CGPoint(x: 0, y: -78), rx: 92, ry: 22), fill: Palette.sea, lineWidth: 4, seed: 1320)
    }

    private static func lead() -> SKNode {
        Sketch.node(.polyline([CGPoint(x: -96, y: 96), CGPoint(x: -60, y: 70), CGPoint(x: -26, y: 30)]), ink: Palette.red, lineWidth: 7, seed: 1321)
    }

    private static func throwArc() -> SKNode {
        Sketch.node(.arc(center: CGPoint(x: 10, y: 10), rx: 60, ry: 56, from: 0.4, to: 2.2), ink: Palette.ink.withAlphaComponent(0.4), lineWidth: 4, wobble: 1.5, seed: 1322)
    }

    private static func waterBowl() -> SKNode {
        Sketch.node(.polygon([CGPoint(x: 18, y: -50), CGPoint(x: 98, y: -50), CGPoint(x: 88, y: -84), CGPoint(x: 28, y: -84)]), fill: Palette.sea, lineWidth: 4.5, seed: 1323)
    }

    private static func creamDrips() -> SKNode {
        let n = SKNode()
        for (i, (x, y)) in [(4, -92), (40, -84)].enumerated() {
            n.addChild(Sketch.node(.ellipse(center: CGPoint(x: CGFloat(x), y: CGFloat(y)), rx: 7, ry: 10), fill: Palette.pink, lineWidth: 2.5, seed: 1324 + UInt64(i)))
        }
        return n
    }

    /// A pink puddle of melted ice cream with the empty cone lying in it.
    private static func meltedIceCream() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 6, y: -54), rx: 92, ry: 30), fill: Palette.pink, lineWidth: 4, wobble: 4, seed: 1326))
        let cone = Sketch.node(.polygon([CGPoint(x: -30, y: 18), CGPoint(x: 30, y: 18), CGPoint(x: 0, y: -60)]), fill: Palette.lightBrown, lineWidth: 4.5, seed: 1327)
        cone.zRotation = 1.4
        cone.position = CGPoint(x: 26, y: -40)
        n.addChild(cone)
        return n
    }

    private static func pizzaBase() -> SKNode {
        Sketch.node(.ellipse(center: CGPoint(x: 0, y: -10), rx: 100, ry: 82), fill: Palette.sand, seed: 1330)
    }

    private static func rollingPin() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -70, y: -16, width: 140, height: 32), radius: 14), fill: Palette.lightBrown, lineWidth: 4.5, seed: 1331))
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -104, y: -7, width: 36, height: 14), radius: 6), fill: Palette.brown, lineWidth: 3.5, seed: 1332))
        n.addChild(Sketch.node(.roundedRect(CGRect(x: 68, y: -7, width: 36, height: 14), radius: 6), fill: Palette.brown, lineWidth: 3.5, seed: 1333))
        n.zRotation = 0.3
        n.position = CGPoint(x: 0, y: 6)
        return n
    }

    private static func sauce() -> SKNode {
        Sketch.node(.ellipse(center: CGPoint(x: 0, y: -10), rx: 78, ry: 62), fill: Palette.red, lineWidth: 3, wobble: 4, seed: 1334)
    }

    private static func cheeseBits() -> SKNode {
        let n = SKNode()
        let spots: [(CGFloat, CGFloat)] = [(-40, 10), (-6, 30), (34, 14), (-30, -30), (8, -10), (44, -30), (-4, -50)]
        for (i, (x, y)) in spots.enumerated() {
            n.addChild(Sketch.node(.roundedRect(CGRect(x: x - 12, y: y - 8, width: 24, height: 16), radius: 4), fill: Palette.sun, lineWidth: 2.5, seed: 1335 + UInt64(i)))
        }
        return n
    }

    private static func floor() -> SKNode {
        Sketch.node(.polyline([CGPoint(x: -110, y: -100), CGPoint(x: 110, y: -100)]), ink: Palette.brown, lineWidth: 6, seed: 1342)
    }

    /// A toy block, sitting with its middle at (x, y).
    private static func block(_ x: CGFloat, _ y: CGFloat, _ colour: NSColor, seed: UInt64, turn: CGFloat = 0) -> SKNode {
        let n = Sketch.node(.roundedRect(CGRect(x: -32, y: -32, width: 64, height: 64), radius: 8), fill: colour, seed: seed)
        n.addChild(Sketch.node(.ellipse(center: .zero, rx: 12, ry: 12), fill: .white, lineWidth: 3, seed: seed &+ 20))
        n.position = CGPoint(x: x, y: y)
        n.zRotation = turn
        return n
    }

    /// An empty bowl seen from the side.
    private static func bowl() -> SKNode {
        var rim: [CGPoint] = (0...12).map { i in
            let a = CGFloat.pi + CGFloat(i) / 12 * CGFloat.pi
            return CGPoint(x: 90 * cos(a), y: -20 + 70 * sin(a))
        }
        rim.append(CGPoint(x: -90, y: -20))
        return Sketch.node(.polygon(rim), fill: Palette.sea, seed: 1343)
    }

    private static func cerealBits() -> SKNode {
        let n = SKNode()
        let spots: [(CGFloat, CGFloat)] = [(-60, -14), (-30, -6), (0, -12), (30, -4), (60, -14), (-44, 8), (-12, 10), (18, 8), (46, 4)]
        for (i, (x, y)) in spots.enumerated() {
            n.addChild(Sketch.node(.ellipse(center: CGPoint(x: x, y: y), rx: 13, ry: 11), fill: Palette.sun.blended(withFraction: 0.3, of: Palette.lightBrown) ?? Palette.sun,
                                   lineWidth: 2.5, seed: 1344 + UInt64(i)))
        }
        return n
    }

    private static func fallingBits() -> SKNode {
        let n = SKNode()
        for (i, (x, y)) in [(-10, 60), (14, 84), (4, 38)].enumerated() {
            n.addChild(Sketch.node(.ellipse(center: CGPoint(x: CGFloat(x), y: CGFloat(y)), rx: 12, ry: 10), fill: Palette.sun.blended(withFraction: 0.3, of: Palette.lightBrown) ?? Palette.sun,
                                   lineWidth: 2.5, seed: 1354 + UInt64(i)))
        }
        return n
    }

    private static func milk() -> SKNode {
        Sketch.node(.ellipse(center: CGPoint(x: 0, y: -18), rx: 86, ry: 16), fill: .white, lineWidth: 3, seed: 1358)
    }

    private static func mud() -> SKNode {
        let n = SKNode()
        for (i, (x, y, r)) in [(CGFloat(-40), CGFloat(-20), CGFloat(14)), (10, -34, 10), (44, -12, 12), (-6, 4, 8)].enumerated() {
            n.addChild(Sketch.node(.ellipse(center: CGPoint(x: x, y: y), rx: r, ry: r * 0.8), fill: Palette.brown, lineWidth: 2, wobble: 3, seed: 1360 + UInt64(i)))
        }
        return n
    }

    private static func sky(_ colour: NSColor) -> SKNode {
        Sketch.node(.roundedRect(CGRect(x: -112, y: -104, width: 224, height: 212), radius: 22), fill: colour.withAlphaComponent(0.55), lineWidth: 0, seed: 1365)
    }

    private static func hill() -> SKNode {
        var top: [CGPoint] = (0...12).map { i in
            let t = CGFloat(i) / 12
            return CGPoint(x: -112 + 224 * t, y: -46 + 26 * sin(t * .pi))
        }
        top.append(CGPoint(x: 112, y: -104))
        top.append(CGPoint(x: -112, y: -104))
        return Sketch.node(.polygon(top), fill: Palette.grass, lineWidth: 4.5, seed: 1366)
    }

    /// A ladder up the left, a platform on top and the slide sloping down to the right.
    private static func slide() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.polyline([CGPoint(x: -110, y: -96), CGPoint(x: 110, y: -96)]), ink: Palette.leaf, lineWidth: 6, seed: 1370))
        n.addChild(Sketch.node(.polyline([CGPoint(x: -96, y: -96), CGPoint(x: -66, y: 60)]), ink: Palette.ink, lineWidth: 6, seed: 1371))
        n.addChild(Sketch.node(.polyline([CGPoint(x: -56, y: -96), CGPoint(x: -26, y: 60)]), ink: Palette.ink, lineWidth: 6, seed: 1372))
        for i in 0..<4 {
            let y = -66 + CGFloat(i) * 36
            let dx = (y + 96) / 156 * 30
            n.addChild(Sketch.node(.polyline([CGPoint(x: -96 + dx, y: y), CGPoint(x: -56 + dx, y: y)]), lineWidth: 4.5, seed: 1373 + UInt64(i)))
        }
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -70, y: 52, width: 56, height: 14), radius: 5), fill: Palette.red, lineWidth: 4, seed: 1377))
        n.addChild(Sketch.node(.polygon([CGPoint(x: -16, y: 64), CGPoint(x: 0, y: 64), CGPoint(x: 108, y: -78), CGPoint(x: 108, y: -92),
                                         CGPoint(x: 84, y: -92)]), fill: Palette.sun, lineWidth: 4.5, seed: 1378))
        return n
    }

    private static func washingMachine() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -86, y: -100, width: 172, height: 196), radius: 16), fill: Palette.card, seed: 1380))
        n.addChild(Sketch.node(.polyline([CGPoint(x: -86, y: 56), CGPoint(x: 86, y: 56)]), lineWidth: 4, seed: 1381))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 52, y: 76), rx: 11, ry: 11), fill: Palette.red, lineWidth: 3, seed: 1382))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 0, y: -24), rx: 58, ry: 58), fill: Palette.stone, lineWidth: 5, seed: 1383))
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: 0, y: -24), rx: 44, ry: 44), fill: Palette.sea, lineWidth: 3.5, seed: 1384))
        n.addChild(glyph("👕", 46, 0, -26))
        n.addChild(bubbles([(-22, -2), (24, -44)]))
        return n
    }

    private static func washingLine() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.polyline([CGPoint(x: -96, y: -100), CGPoint(x: -96, y: 52)]), ink: Palette.brown, lineWidth: 8, seed: 1385))
        n.addChild(Sketch.node(.polyline([CGPoint(x: 96, y: -100), CGPoint(x: 96, y: 52)]), ink: Palette.brown, lineWidth: 8, seed: 1386))
        n.addChild(Sketch.node(.polyline([CGPoint(x: -96, y: 46), CGPoint(x: 0, y: 36), CGPoint(x: 96, y: 46)]), lineWidth: 3.5, seed: 1387))
        return n
    }

    /// Clothes folded in a neat pile.
    private static func foldedPile() -> SKNode {
        let n = SKNode()
        let colours = [Palette.sea, Palette.red, Palette.sun, Palette.grass]
        for (i, colour) in colours.enumerated() {
            let y = -84 + CGFloat(i) * 40
            n.addChild(Sketch.node(.roundedRect(CGRect(x: -74 + CGFloat(i % 2) * 6, y: y, width: 148, height: 36), radius: 10), fill: colour, lineWidth: 4.5, seed: 1390 + UInt64(i)))
        }
        return n
    }

    /// A red-and-white checked picnic blanket.
    private static func blanket() -> SKNode {
        let n = SKNode()
        let corners = [CGPoint(x: -80, y: -96), CGPoint(x: 108, y: -96), CGPoint(x: 80, y: -10), CGPoint(x: -56, y: -10)]
        n.addChild(Sketch.node(.polygon(corners), fill: Palette.red, seed: 1395))
        for i in 1..<4 {
            let t = CGFloat(i) / 4
            let bottom = CGPoint(x: -80 + 188 * t, y: -96)
            let top = CGPoint(x: -56 + 136 * t, y: -10)
            n.addChild(Sketch.node(.polyline([bottom, top]), ink: .white, lineWidth: 7, wobble: 1, seed: 1396 + UInt64(i)))
            let y = -96 + 86 * t
            n.addChild(Sketch.node(.polyline([CGPoint(x: -80 + 24 * t, y: y), CGPoint(x: 108 - 28 * t, y: y)]), ink: .white, lineWidth: 7, wobble: 1, seed: 1400 + UInt64(i)))
        }
        return n
    }

    /// A sun drawn in crayon on the paper.
    private static func scribble() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.ellipse(center: CGPoint(x: -10, y: 4), rx: 22, ry: 22), ink: Palette.orange, lineWidth: 5, wobble: 3, seed: 1405))
        for i in 0..<6 {
            let a = CGFloat(i) / 6 * 2 * CGFloat.pi
            n.addChild(Sketch.node(.polyline([CGPoint(x: -10 + 30 * cos(a), y: 4 + 30 * sin(a)), CGPoint(x: -10 + 44 * cos(a), y: 4 + 44 * sin(a))]),
                                   ink: Palette.orange, lineWidth: 4, seed: 1406 + UInt64(i)))
        }
        return n
    }

    private static func houseWalls() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.polyline([CGPoint(x: -110, y: -96), CGPoint(x: 110, y: -96)]), ink: Palette.leaf, lineWidth: 6, seed: 1412))
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -80, y: -96, width: 160, height: 110), radius: 4), fill: Palette.red.blended(withFraction: 0.2, of: Palette.brown) ?? Palette.red, seed: 1413))
        for i in 1..<5 {
            let y = -96 + CGFloat(i) * 22
            n.addChild(Sketch.node(.polyline([CGPoint(x: -76, y: y), CGPoint(x: 76, y: y)]), ink: Palette.ink.withAlphaComponent(0.35), lineWidth: 2.5, wobble: 1, seed: 1414 + UInt64(i)))
        }
        return n
    }

    private static func roof() -> SKNode {
        Sketch.node(.polygon([CGPoint(x: -100, y: 12), CGPoint(x: 0, y: 96), CGPoint(x: 100, y: 12)]), fill: Palette.brown, seed: 1420)
    }

    private static func houseFront() -> SKNode {
        let n = SKNode()
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -16, y: -96, width: 34, height: 60), radius: 6), fill: Palette.sun, lineWidth: 4, seed: 1421))
        n.addChild(Sketch.node(.roundedRect(CGRect(x: -66, y: -50, width: 36, height: 36), radius: 4), fill: Palette.ice, lineWidth: 4, seed: 1422))
        n.addChild(Sketch.node(.roundedRect(CGRect(x: 32, y: -50, width: 36, height: 36), radius: 4), fill: Palette.ice, lineWidth: 4, seed: 1423))
        return n
    }
}
