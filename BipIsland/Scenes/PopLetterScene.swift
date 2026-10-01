import BipCore
import SpriteKit

/// Bubble Pop: letter bubbles float slowly up; pop the one that says the sound you hear.
/// No timer: bubbles drift round and round. Each question is a fresh round from `BubblePopGame`,
/// which keeps exactly one right letter on screen at all times.
final class PopLetterScene: BaseScene {
    private final class Bubble {
        let node: SKNode
        let lane: CGFloat
        let phase: CGFloat
        var speed: CGFloat
        var sound: PhonicsSound
        var label: SKNode

        init(node: SKNode, lane: CGFloat, phase: CGFloat, speed: CGFloat, sound: PhonicsSound, label: SKNode) {
            self.node = node
            self.lane = lane
            self.phase = phase
            self.speed = speed
            self.sound = sound
            self.label = label
        }
    }

    private let game: BubblePopGame
    private var session: GameSession
    private let learner: Learner
    /// The sound Bip chose this visit for; a level change on it ends the visit.
    private let focus: PhonicsSound
    private var round: BubblePopGame.Round?
    private var bubbles: [Bubble] = []
    private var attempt = QuestionAttempt()
    private var lastUpdate: TimeInterval?
    private var elapsed: CGFloat = 0
    private var slowDown: CGFloat = 1
    private static let radius: CGFloat = 95
    private static let top: CGFloat = 600
    private static let bottom: CGFloat = -600

    init(coordinator: GameCoordinator, game: BubblePopGame, session: GameSession, learner: Learner, focus: PhonicsSound) {
        self.game = game
        self.session = session
        self.learner = learner
        self.focus = focus
        super.init(coordinator: coordinator)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }

    override func didMove(to view: SKView) {
        let water = Sketch.node(.roundedRect(CGRect(x: -800, y: -520, width: 1600, height: 1040), radius: 0),
                                fill: Palette.sea.withAlphaComponent(0.25), lineWidth: 0, seed: 1000)
        water.zPosition = -60
        addChild(water)

        addHomeButton()
        addBip(at: CGPoint(x: -640, y: -420), scale: 0.75)
        let replay = Buttons.replay()
        replay.position = CGPoint(x: 660, y: -340)
        replay.zPosition = 40
        addChild(replay)

        guard let first = session.nextRound(of: game, for: learner, using: &coordinator.rng) else {
            after(0.3) { [weak self] in self?.finish() }
            return
        }
        round = first
        let lanes: [CGFloat] = [-440, -220, 0, 220, 440]
        let starts: [CGFloat] = [-420, -150, 120, -300, 260].shuffled(using: &coordinator.rng)
        for (i, letter) in first.choices.prefix(lanes.count).enumerated() {
            let node = SKNode()
            node.name = "tap:bubble:\(i)"
            node.zPosition = 10
            node.position = CGPoint(x: lanes[i], y: starts[i])
            node.addChild(Sketch.node(.ellipse(center: .zero, rx: Self.radius, ry: Self.radius), fill: Palette.bubble.withAlphaComponent(0.85), ink: Palette.sea, lineWidth: 5, seed: 1010 + UInt64(i)))
            let shine = SKShapeNode(ellipseOf: CGSize(width: 26, height: 44))
            shine.fillColor = NSColor.white.withAlphaComponent(0.8)
            shine.strokeColor = .clear
            shine.position = CGPoint(x: -50, y: 40)
            shine.zRotation = 0.6
            node.addChild(shine)
            let label = Sketch.letter(letter.grapheme, size: 120, shadow: nil)
            node.addChild(label)
            addChild(node)
            bubbles.append(Bubble(node: node, lane: lanes[i], phase: CGFloat(i) * 1.3,
                                  speed: CGFloat.random(in: 45...65, using: &coordinator.rng), sound: letter, label: label))
        }

        after(0.6) { [weak self] in self?.askQuestion(isFirst: true) }
    }

    override func update(_ currentTime: TimeInterval) {
        let dt = CGFloat(min(currentTime - (lastUpdate ?? currentTime), 0.1))
        lastUpdate = currentTime
        elapsed += dt
        for bubble in bubbles {
            var y = bubble.node.position.y + bubble.speed * slowDown * dt
            if y > Self.top {
                y = Self.bottom
                respawn(bubble)
            }
            let x = bubble.lane + 30 * sin(elapsed * 0.7 + bubble.phase)
            bubble.node.position = CGPoint(x: x, y: y)
        }
    }

    /// A bubble that floated off the top comes back at the bottom with a new letter.
    private func respawn(_ bubble: Bubble) {
        guard let round else { return }
        let others = bubbles.filter { $0 !== bubble }.map(\.sound)
        let letter = game.nextBubble(in: round, onScreen: others, using: &coordinator.rng)
        setLetter(letter, on: bubble)
    }

    private func setLetter(_ letter: PhonicsSound, on bubble: Bubble) {
        bubble.sound = letter
        bubble.label.removeFromParent()
        let label = Sketch.letter(letter.grapheme, size: 120, shadow: nil)
        bubble.node.addChild(label)
        bubble.label = label
        bubble.node.removeAction(forKey: "hint")
        bubble.node.setScale(1)
        bubble.node.zRotation = 0
        bubble.node.alpha = 1
        if attempt.needsHint, let round, game.isCorrect(letter, in: round) {
            bubble.node.run(Buttons.hintWiggle(), withKey: "hint")
        }
    }

    /// The first question uses the bubbles already floating; later ones get a new round of letters.
    private func askQuestion(isFirst: Bool = false) {
        if !isFirst {
            guard let next = session.nextRound(of: game, for: learner, using: &coordinator.rng) else {
                return endVisit(with: .roundDone)
            }
            round = next
            for (bubble, letter) in zip(bubbles, next.choices) {
                setLetter(letter, on: bubble)
                bubble.label.setScale(0.4)
                bubble.label.run(.scale(to: 1, duration: 0.25))
            }
        }
        attempt = QuestionAttempt()
        slowDown = 1
        bubbles.forEach { $0.node.removeAction(forKey: "hint"); $0.node.setScale(1); $0.node.zRotation = 0 }
        inputLocked = false
        sayPrompt()
    }

    private func sayPrompt() {
        guard let round else { return }
        voice.play([VoiceLine.popTheLetter.rawValue, round.target.soundClip])
        bip.hop()
    }

    override func handleTap(name: String, node: SKNode) {
        if name == "tap:replay" {
            Buttons.press(node)
            sayPrompt()
            return
        }
        guard name.hasPrefix("tap:bubble:"), let index = Int(name.dropFirst("tap:bubble:".count)), index < bubbles.count,
              let round else { return }
        let bubble = bubbles[index]

        switch attempt.answer(correct: game.isCorrect(bubble.sound, in: round)) {
        case let .correct(firstTry):
            inputLocked = true
            pop(bubble)
            bip.celebrate()
            let change = coordinator.record(correct: firstTry, skillID: game.skillID(for: round), soundID: round.target.id)
            let focusChanged = round.target.id == focus.id && change != .none
            voice.play([coordinator.randomPraise()], completion: { [weak self] in
                self?.afterAnswer(focusChanged ? change : .none)
            })
        case .tryAgain:
            sfx.play(.boop)
            bubble.node.run(Buttons.shake())
            bip.tilt()
            after(0.4) { [weak self] in
                guard let self, let round = self.round else { return }
                self.voice.play([round.target.soundClip])
            }
        case .hint:
            sfx.play(.boop)
            bubble.node.run(Buttons.shake())
            bip.tilt()
            slowDown = 0.35
            for b in bubbles where game.isCorrect(b.sound, in: round) {
                b.node.run(Buttons.hintWiggle(), withKey: "hint")
            }
            after(0.4) { [weak self] in
                guard let self, let round = self.round else { return }
                self.voice.play([self.coordinator.randomHint(), round.target.soundClip])
            }
        }
    }

    private func pop(_ bubble: Bubble) {
        sfx.play(.pop)
        after(0.08) { [weak self] in self?.sfx.play(.chime) }
        Buttons.sparkle(at: bubble.node.position, in: self)
        bubble.node.removeAction(forKey: "hint")
        bubble.node.run(.sequence([
            .group([.scale(to: 1.4, duration: 0.12), .fadeOut(withDuration: 0.12)]),
            .run { [weak self, weak bubble] in
                guard let self, let bubble else { return }
                bubble.node.position.y = Self.bottom
                self.respawn(bubble)
            },
        ]))
    }

    private enum Ending { case levelUp, practiseAgain, roundDone }

    private func afterAnswer(_ change: MasteryChange) {
        switch change {
        case .levelledUp:
            endVisit(with: .levelUp)
        case .droppedBack:
            endVisit(with: .practiseAgain)
        case .none:
            if session.isFinished {
                endVisit(with: .roundDone)
            } else {
                after(0.3) { [weak self] in self?.askQuestion() }
            }
        }
    }

    private func endVisit(with ending: Ending) {
        inputLocked = true
        switch ending {
        case .levelUp:
            sfx.play(.whirr)
            bip.celebrate()
            voice.play([VoiceLine.levelUp.rawValue], completion: { [weak self] in self?.finish() })
        case .practiseAgain:
            voice.play([VoiceLine.letsPractiseAgain.rawValue], completion: { [weak self] in self?.finish() })
        case .roundDone:
            voice.play([VoiceLine.roundDone.rawValue], completion: { [weak self] in self?.finish() })
        }
    }

    private func finish() {
        coordinator.showLettersIsland(greet: false)
    }

    override func replayPrompt() {
        guard !inputLocked else { return }
        sayPrompt()
    }

    override func goHome() {
        coordinator.showLettersIsland()
    }
}
