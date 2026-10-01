import BipCore
import SpriteKit

/// Letters Island: the six group-1 sounds as stepping stones with progress stars.
/// Clicking Bip or the big play button starts Bip's suggested activity; clicking a stone meets that sound.
final class LettersIslandScene: BaseScene {
    private let greet: Bool

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

        let sounds = coordinator.planner.sounds
        let suggested = coordinator.planner.suggestedSound(tracker: coordinator.tracker)
        for (i, sound) in sounds.enumerated() {
            let x = -575 + CGFloat(i) * 230
            let y = 140 + 60 * sin(CGFloat(i) * 1.1)
            let stone = makeStone(for: sound, index: i)
            stone.position = CGPoint(x: x, y: y)
            addChild(stone)
            if sound == suggested {
                stone.run(Buttons.pulse())
            }
        }

        addHomeButton()
        addBip(at: CGPoint(x: -170, y: -380), scale: 0.85)

        let play = Buttons.play()
        play.position = CGPoint(x: 140, y: -230)
        play.zPosition = 10
        play.run(Buttons.pulse())
        addChild(play)

        if greet {
            after(0.5) { [weak self] in self?.replayPrompt() }
        } else {
            after(0.3) { [weak self] in
                self?.bip.celebrate()
                self?.sfx.play(.whirr)
            }
        }
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
        } else if name.hasPrefix("tap:stone:") {
            let id = String(name.dropFirst("tap:stone:".count))
            guard let sound = Phonics.sound(id: id) else { return }
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
