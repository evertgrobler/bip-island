import BipCore
import SpriteKit

/// Letters Island: the sounds of the child's current phonics group (from Content/phonics/graphemes.json)
/// as stepping stones with progress stars. Clicking Bip or the big play button starts Bip's suggested
/// activity; clicking a stone meets that sound.
final class LettersIslandScene: BaseScene {
    private let greet: Bool
    private var keyNodes: [SKNode] = []

    override var keyOptions: [SKNode] { keyNodes }

    init(coordinator: GameCoordinator, greet: Bool) {
        self.greet = greet
        super.init(coordinator: coordinator)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }

    override func didMove(to view: SKView) {
        let island = Sketch.node(.ellipse(center: CGPoint(x: 0, y: -20), rx: 760, ry: 430), fill: Palette.sand, ink: Palette.lightBrown, lineWidth: 7, seed: 700)
        island.zPosition = -50
        addChild(island)
        let grass = Sketch.node(.ellipse(center: CGPoint(x: 0, y: 30), rx: 680, ry: 330), fill: Palette.grass.withAlphaComponent(0.55), ink: Palette.leaf, lineWidth: 5, seed: 701)
        grass.zPosition = -49
        addChild(grass)

        let sounds = coordinator.lettersProgress?.currentGroup.sounds ?? []
        let suggested = coordinator.planner?.suggestedSound()
        let layout = Self.stoneLayout(count: sounds.count)
        for (i, sound) in sounds.enumerated() {
            let stone = makeStone(for: sound, index: i)
            // The holder sets the size, so the pulse and press animations (which scale to 1) still work.
            let holder = SKNode()
            holder.position = layout.positions[i]
            holder.setScale(layout.scale)
            holder.zPosition = stone.zPosition
            holder.addChild(stone)
            addChild(holder)
            keyNodes.append(stone)
            if sound == suggested {
                stone.run(Buttons.pulse())
            }
        }

        addHomeButton()
        addBip(at: CGPoint(x: -170, y: -380), scale: 0.85)

        let trace = SKNode()
        trace.name = "tap:trace"
        trace.position = CGPoint(x: -560, y: -300)
        trace.zPosition = 10
        trace.addChild(Sketch.node(.ellipse(center: .zero, rx: 100, ry: 100), fill: Palette.card, lineWidth: 6, seed: 760))
        let traceLetter = Sketch.letter("s", size: 110, shadow: Palette.teal)
        trace.addChild(traceLetter)
        addChild(trace)
        keyNodes.append(trace)

        let monster = SKNode()
        monster.name = "tap:monster"
        monster.position = CGPoint(x: 560, y: -300)
        monster.zPosition = 10
        monster.addChild(Sketch.node(.ellipse(center: .zero, rx: 100, ry: 100), fill: Palette.purple, lineWidth: 6, seed: 770))
        monster.addChild(Sketch.node(.ellipse(center: CGPoint(x: 0, y: -10), rx: 44, ry: 34), fill: Palette.ink, lineWidth: 4, seed: 771))
        for (i, eye) in [-30, 30].enumerated() {
            monster.addChild(Sketch.node(.ellipse(center: CGPoint(x: CGFloat(eye), y: 44), rx: 16, ry: 20), fill: .white, lineWidth: 4, seed: 772 + UInt64(i)))
        }
        addChild(monster)
        keyNodes.append(monster)
        let play = Buttons.play()
        play.position = layout.positions.count > 6 ? CGPoint(x: 380, y: -320) : CGPoint(x: 140, y: -230)
        play.zPosition = 10
        play.run(Buttons.pulse())
        addChild(play)
        keyNodes.append(play)

        if greet {
            after(0.5) { [weak self] in self?.replayPrompt() }
        } else {
            after(0.3) { [weak self] in
                self?.bip.celebrate()
                self?.sfx.play(.whirr)
            }
        }
    }

    /// One wavy row for up to six stones; two rows, slightly smaller, for bigger groups.
    /// Stones stay at least 150 pt across, above the 120 pt minimum.
    private static func stoneLayout(count: Int) -> (positions: [CGPoint], scale: CGFloat) {
        func row(_ n: Int, y: CGFloat, spacing: CGFloat, wave: CGFloat) -> [CGPoint] {
            (0..<n).map { i in
                let x = (CGFloat(i) - CGFloat(n - 1) / 2) * spacing
                return CGPoint(x: x, y: y + wave * sin(CGFloat(i) * 1.1))
            }
        }
        if count <= 6 {
            return (row(count, y: 140, spacing: 230, wave: 60), 1)
        }
        let top = (count + 1) / 2
        return (row(top, y: 250, spacing: 230, wave: 18) + row(count - top, y: 10, spacing: 230, wave: 18), 0.85)
    }

    private func makeStone(for sound: PhonicsSound, index: Int) -> SKNode {
        let stone = SKNode()
        stone.name = "tap:stone:\(sound.id)"
        stone.zPosition = 5
        let stage = coordinator.stage(of: sound)
        let colour = stage == .new ? Palette.stone : [Palette.sun, Palette.orange, Palette.teal, Palette.purple, Palette.pink, Palette.lightTeal][index % 6]
        stone.addChild(Sketch.node(.ellipse(center: .zero, rx: 92, ry: 84), fill: colour, lineWidth: 6, seed: 710 + UInt64(index) * 3))
        let letter = Sketch.letter(sound.grapheme, size: 120, shadow: .white)
        letter.position = CGPoint(x: 0, y: 6)
        stone.addChild(letter)

        // Three stars: met, recognises, mastered.
        for star in 0..<3 {
            let earned = stage.rawValue > star
            let shape = SketchShape.polygon(Sketch.starPoints(center: CGPoint(x: -44 + CGFloat(star) * 44, y: -118), radius: 20))
            stone.addChild(Sketch.node(shape, fill: earned ? Palette.sun : Palette.card, lineWidth: 3.5, wobble: 1, seed: 740 + UInt64(index * 3 + star)))
        }
        return stone
    }

    override func handleTap(name: String, node: SKNode) {
        if name == "tap:play" {
            sfx.play(.whirr)
            Buttons.press(node)
            startSuggested()
        } else if name == "tap:trace" {
            sfx.play(.whirr)
            Buttons.press(node)
            guard !inputLocked else { return }
            inputLocked = true
            after(0.25) { [weak self] in self?.coordinator.startTrace() }
        } else if name == "tap:monster" {
            sfx.play(.whirr)
            Buttons.press(node)
            guard !inputLocked else { return }
            inputLocked = true
            after(0.25) { [weak self] in self?.coordinator.startMonster() }
        } else if name.hasPrefix("tap:stone:") {
            let id = String(name.dropFirst("tap:stone:".count))
            guard let sound = coordinator.letters?.course.sound(id: id) else { return }
            sfx.play(.tick)
            Buttons.press(node)
            inputLocked = true
            voice.play([sound.soundClip])
            after(0.5) { [weak self] in
                self?.coordinator.start(PlannedActivity(kind: .meetTheSound, sound: sound))
            }
        }
    }

    override func didTapBip() {
        startSuggested()
    }

    private func startSuggested() {
        guard !inputLocked else { return }
        inputLocked = true
        bip.celebrate()
        after(0.5) { [weak self] in self?.coordinator.startNextActivity() }
    }

    override func replayPrompt() {
        voice.play([VoiceLine.lettersIsland.rawValue])
        bip.hop()
    }

    override func goHome() {
        coordinator.showMap()
    }
}
