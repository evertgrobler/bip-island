import BipCore
import SpriteKit

/// The island map. An island is open when its games loaded; the rest stay asleep.
final class MapScene: BaseScene {
    private let greet: Bool
    private var lettersIsland: SKNode?
    private var numbersIsland: SKNode?
    private var wordsIsland: SKNode?
    private var codingIsland: SKNode?

    override var keyOptions: [SKNode] {
        [lettersIsland, numbersIsland, wordsIsland, codingIsland].compactMap { $0 }
    }

    init(coordinator: GameCoordinator, greet: Bool) {
        self.greet = greet
        super.init(coordinator: coordinator)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }

    override func didMove(to view: SKView) {
        drawSea()
        lettersIsland = addIsland(id: "letters", at: CGPoint(x: -400, y: 170), colour: Palette.grass, open: coordinator.letters != nil, seed: 600) { n in
            let letters = Sketch.letter("s a t", size: 110, shadow: Palette.orange)
            letters.position = CGPoint(x: 0, y: 30)
            n.addChild(letters)
        }
        numbersIsland = addIsland(id: "numbers", at: CGPoint(x: 400, y: 190), colour: Palette.sun, open: coordinator.numbers != nil, seed: 620) { n in
            let label = Sketch.letter("1 2 3", size: 96, shadow: Palette.red)
            label.position = CGPoint(x: 0, y: 30)
            n.addChild(label)
        }
        wordsIsland = addIsland(id: "words", at: CGPoint(x: -380, y: -230), colour: Palette.pink, open: coordinator.words != nil, seed: 640) { n in
            let picture = PictureNode.make(word: "apple")
            picture.setScale(0.55)
            picture.position = CGPoint(x: -60, y: 30)
            n.addChild(picture)
            let word = Sketch.letter("abc", size: 80, shadow: Palette.purple)
            word.position = CGPoint(x: 60, y: 25)
            n.addChild(word)
        }
        codingIsland = addIsland(id: "coding", at: CGPoint(x: 400, y: -220), colour: Palette.lightTeal, open: coordinator.coding != nil, seed: 660) { n in
            for (i, angle) in [CGFloat(0), CGFloat.pi / 2, CGFloat(0)].enumerated() {
                let arrow = Sketch.node(.polygon([CGPoint(x: -26, y: -9), CGPoint(x: 4, y: -9), CGPoint(x: 4, y: -24), CGPoint(x: 30, y: 0), CGPoint(x: 4, y: 24), CGPoint(x: 4, y: 9), CGPoint(x: -26, y: 9)]),
                                        fill: Palette.orange, lineWidth: 4, seed: 670 + UInt64(i))
                arrow.zRotation = angle
                arrow.position = CGPoint(x: -75 + CGFloat(i) * 75, y: 30)
                n.addChild(arrow)
            }
        }

        addBip(at: CGPoint(x: -110, y: -40), scale: 0.75)

        let stickers = SKNode()
        stickers.name = "tap:stickers"
        stickers.position = CGPoint(x: 700, y: 360)
        stickers.zPosition = 10
        stickers.addChild(Sketch.node(.ellipse(center: .zero, rx: 64, ry: 64), fill: Palette.sun, lineWidth: 5, seed: 680))
        stickers.addChild(Sketch.node(.polygon(Sketch.starPoints(center: .zero, radius: 34)), fill: Palette.orange, lineWidth: 4, seed: 681))
        addChild(stickers)

        if coordinator.mysteryAvailable {
            let box = SKNode()
            box.name = "tap:mystery"
            box.position = CGPoint(x: -700, y: 340)
            box.zPosition = 10
            box.addChild(Sketch.node(.roundedRect(CGRect(x: -70, y: -70, width: 140, height: 140), radius: 24),
                                     fill: Palette.purple, lineWidth: 6, seed: 682))
            let bow = Sketch.node(.ellipse(center: CGPoint(x: 0, y: 70), rx: 26, ry: 18), fill: Palette.pink, lineWidth: 4, seed: 683)
            addChild(box)
            box.addChild(bow)
            let question = Sketch.label("?", size: 84, colour: .white)
            box.addChild(question)
            box.run(Buttons.pulse())
        }
        // Bip's suggestion glows; the child still picks freely.
        let glowing: SKNode?
        switch coordinator.islandSuggestion?.island {
        case .numbers: glowing = numbersIsland
        case .words: glowing = wordsIsland
        case .coding: glowing = codingIsland
        default: glowing = lettersIsland
        }
        glowing?.run(Buttons.pulse(), withKey: "pulse")

        if greet {
            after(0.6) { [weak self] in self?.sayWelcome() }
        }
    }

    private func drawSea() {
        let sea = Sketch.node(.roundedRect(CGRect(x: -790, y: -480, width: 1580, height: 960), radius: 90),
                              fill: Palette.sea.withAlphaComponent(0.5), ink: Palette.sea, lineWidth: 6, seed: 590)
        sea.zPosition = -50
        addChild(sea)
        var rng = SeededGenerator(seed: 591)
        for i in 0..<26 {
            let x = CGFloat.random(in: -720...720, using: &rng)
            let y = CGFloat.random(in: -420...420, using: &rng)
            let wave = Sketch.node(.polyline([CGPoint(x: x - 24, y: y), CGPoint(x: x - 12, y: y + 8), CGPoint(x: x, y: y), CGPoint(x: x + 12, y: y + 8), CGPoint(x: x + 24, y: y)]),
                                   ink: NSColor.white.withAlphaComponent(0.8), lineWidth: 4, wobble: 1, seed: 592 + UInt64(i))
            wave.zPosition = -40
            addChild(wave)
        }
    }

    @discardableResult
    private func addIsland(id: String, at point: CGPoint, colour: NSColor, open: Bool, seed: UInt64, decorate: (SKNode) -> Void) -> SKNode {
        let island = SKNode()
        island.name = "tap:island:\(id)"
        island.position = point
        island.addChild(Sketch.node(.ellipse(center: CGPoint(x: 0, y: -18), rx: 250, ry: 135), fill: Palette.sand, seed: seed))
        island.addChild(Sketch.node(.ellipse(center: CGPoint(x: 0, y: 6), rx: 205, ry: 100), fill: colour, seed: seed + 1))
        let decoration = SKNode()
        decorate(decoration)
        island.addChild(decoration)
        if !open {
            decoration.alpha = 0.45
            let sleepy = Sketch.label("z z z", size: 52, colour: Palette.ink.withAlphaComponent(0.6))
            sleepy.position = CGPoint(x: 150, y: 110)
            island.addChild(sleepy)
            sleepy.run(.repeatForever(.sequence([.moveBy(x: 0, y: 12, duration: 1.2), .moveBy(x: 0, y: -12, duration: 1.2)])))
        }
        addChild(island)
        return island
    }

    private func sayWelcome() {
        bip.hop()
        sfx.play(.beep)
        voice.play([VoiceLine.welcome.rawValue])
        bip.point(right: false)
    }

    override func handleTap(name: String, node: SKNode) {
        switch name {
        case "tap:island:letters" where coordinator.letters != nil:
            sfx.play(.whirr)
            Buttons.press(node)
            inputLocked = true
            after(0.25) { [weak self] in self?.coordinator.showLettersIsland() }
        case "tap:island:numbers" where coordinator.numbers != nil:
            sfx.play(.whirr)
            Buttons.press(node)
            inputLocked = true
            after(0.25) { [weak self] in self?.coordinator.showNumbersIsland() }
        case "tap:island:words" where coordinator.words != nil:
            sfx.play(.whirr)
            Buttons.press(node)
            inputLocked = true
            after(0.25) { [weak self] in self?.coordinator.showWordsIsland() }
        case "tap:island:coding" where coordinator.coding != nil:
            sfx.play(.whirr)
            Buttons.press(node)
            inputLocked = true
            after(0.25) { [weak self] in self?.coordinator.showCodingIsland() }
        case "tap:stickers":
            sfx.play(.tick)
            Buttons.press(node)
            guard !inputLocked else { return }
            inputLocked = true
            after(0.25) { [weak self] in self?.coordinator.showStickers() }
        case "tap:mystery":
            guard !inputLocked else { return }
            inputLocked = true
            sfx.play(.chime)
            bip.celebrate()
            Buttons.sparkle(at: node.position, in: self)
            node.run(.sequence([.scale(to: 1.2, duration: 0.15), .fadeOut(withDuration: 0.2), .removeFromParent()]))
            if coordinator.claimMystery() {
                voice.play([coordinator.randomPraise()])
            }
            after(1.2) { [weak self] in
                self?.inputLocked = false
            }
        case let other where other.hasPrefix("tap:island:"):
            sfx.play(.boop)
            node.run(Buttons.shake())
            voice.play([VoiceLine.islandSleeping.rawValue])
            bip.point(right: false)
        default:
            break
        }
    }

    override func didTapBip() {
        voice.play([VoiceLine.welcome.rawValue])
    }

    override func replayPrompt() {
        sayWelcome()
    }

    override func goHome() {}
}
