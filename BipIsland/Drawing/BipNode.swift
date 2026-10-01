import SpriteKit

/// Bip: a small, friendly hand-drawn robot. Origin is between his wheels.
final class BipNode: SKNode {
    private let head = SKNode()
    private let antenna = SKNode()
    private let leftArm = SKNode()
    private let rightArm = SKNode()
    private var eyes: [SKNode] = []
    private let chestLight: SKShapeNode

    init(seed: UInt64 = 40) {
        chestLight = SKShapeNode(circleOfRadius: 13)
        super.init()
        name = "tap:bip"
        build(seed: seed)
        startIdle()
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }

    private func build(seed: UInt64) {
        var s = seed
        func next() -> UInt64 { s &+= 13; return s }

        // Wheels
        for x in [-42.0, 42.0] {
            addChild(Sketch.node(.ellipse(center: CGPoint(x: x, y: 18), rx: 28, ry: 20), fill: Palette.ink.withAlphaComponent(0.85), seed: next()))
            addChild(Sketch.node(.ellipse(center: CGPoint(x: x, y: 18), rx: 9, ry: 7), fill: Palette.stone, lineWidth: 3, seed: next()))
        }

        // Arms (behind the body)
        for (arm, side) in [(leftArm, -1.0), (rightArm, 1.0)] {
            arm.position = CGPoint(x: 62 * side, y: 100)
            arm.addChild(Sketch.node(.polyline([.zero, CGPoint(x: 32 * side, y: -24), CGPoint(x: 40 * side, y: -50)]), lineWidth: 7, seed: next()))
            arm.addChild(Sketch.node(.ellipse(center: CGPoint(x: 40 * side, y: -56), rx: 12, ry: 12), fill: Palette.orange, lineWidth: 4, seed: next()))
            addChild(arm)
        }

        // Body
        addChild(Sketch.node(.roundedRect(CGRect(x: -66, y: 28, width: 132, height: 112), radius: 26), fill: Palette.teal, seed: next()))
        chestLight.position = CGPoint(x: 0, y: 92)
        chestLight.fillColor = Palette.sun
        chestLight.strokeColor = Palette.ink
        chestLight.lineWidth = 3
        addChild(chestLight)
        for i in 0..<3 {
            let x = -24.0 + Double(i) * 24
            addChild(Sketch.node(.ellipse(center: CGPoint(x: x, y: 54), rx: 6, ry: 6), fill: [Palette.red, Palette.sun, Palette.go][i], lineWidth: 2.5, seed: next()))
        }

        // Neck and head
        addChild(Sketch.node(.roundedRect(CGRect(x: -14, y: 136, width: 28, height: 18), radius: 4), fill: Palette.stone, lineWidth: 4, seed: next()))
        head.position = CGPoint(x: 0, y: 150)
        addChild(head)

        antenna.position = CGPoint(x: 0, y: 104)
        antenna.addChild(Sketch.node(.polyline([.zero, CGPoint(x: 2, y: 44)]), lineWidth: 5, seed: next()))
        antenna.addChild(Sketch.node(.ellipse(center: CGPoint(x: 2, y: 54), rx: 13, ry: 13), fill: Palette.red, lineWidth: 4, seed: next()))
        head.addChild(antenna)

        head.addChild(Sketch.node(.roundedRect(CGRect(x: -82, y: 0, width: 164, height: 108), radius: 30), fill: Palette.lightTeal, seed: next()))

        for x in [-34.0, 34.0] {
            let eye = SKNode()
            eye.position = CGPoint(x: x, y: 60)
            eye.addChild(Sketch.node(.ellipse(center: .zero, rx: 22, ry: 25), fill: .white, lineWidth: 4, seed: next()))
            let pupil = SKShapeNode(circleOfRadius: 10)
            pupil.fillColor = Palette.ink
            pupil.strokeColor = .clear
            pupil.position = CGPoint(x: 3, y: -3)
            eye.addChild(pupil)
            let sparkle = SKShapeNode(circleOfRadius: 3.5)
            sparkle.fillColor = .white
            sparkle.strokeColor = .clear
            sparkle.position = CGPoint(x: 7, y: 2)
            eye.addChild(sparkle)
            head.addChild(eye)
            eyes.append(eye)
        }
        for x in [-58.0, 58.0] {
            let cheek = SKShapeNode(ellipseOf: CGSize(width: 20, height: 13))
            cheek.fillColor = Palette.pink.withAlphaComponent(0.8)
            cheek.strokeColor = .clear
            cheek.position = CGPoint(x: x, y: 30)
            head.addChild(cheek)
        }
        head.addChild(Sketch.node(.arc(center: CGPoint(x: 0, y: 32), rx: 22, ry: 14, from: .pi * 1.15, to: .pi * 1.85), lineWidth: 4.5, seed: next()))
    }

    private func startIdle() {
        let blink = SKAction.sequence([
            .wait(forDuration: 2.5, withRange: 2),
            .scaleY(to: 0.1, duration: 0.07),
            .scaleY(to: 1, duration: 0.09),
        ])
        for eye in eyes { eye.run(.repeatForever(blink), withKey: "blink") }

        let bob = SKAction.sequence([
            .moveBy(x: 0, y: 6, duration: 0.9),
            .moveBy(x: 0, y: -6, duration: 0.9),
        ])
        bob.timingMode = .easeInEaseOut
        head.run(.repeatForever(bob))

        let wiggle = SKAction.sequence([
            .rotate(toAngle: 0.12, duration: 0.7),
            .rotate(toAngle: -0.12, duration: 0.7),
        ])
        antenna.run(.repeatForever(wiggle))

        let glow = SKAction.sequence([.fadeAlpha(to: 0.5, duration: 0.8), .fadeAlpha(to: 1, duration: 0.8)])
        chestLight.run(.repeatForever(glow))
    }

    /// A happy little hop (when clicked or beeping).
    func hop() {
        guard action(forKey: "hop") == nil else { return }
        let up = SKAction.moveBy(x: 0, y: 30, duration: 0.14)
        up.timingMode = .easeOut
        let down = SKAction.moveBy(x: 0, y: -30, duration: 0.16)
        down.timingMode = .easeIn
        run(.sequence([up, down]), withKey: "hop")
        antenna.run(.sequence([.rotate(toAngle: 0.4, duration: 0.08), .rotate(toAngle: -0.3, duration: 0.1), .rotate(toAngle: 0, duration: 0.1)]))
    }

    /// Arms up, spin and hop: for right answers.
    func celebrate() {
        hop()
        let wave = SKAction.sequence([.rotate(toAngle: 0.9, duration: 0.15), .rotate(toAngle: 0, duration: 0.25)])
        leftArm.run(.sequence([wave, wave]))
        rightArm.run(.sequence([wave.reversed(), wave.reversed()]))
        chestLight.run(.sequence([.scale(to: 1.6, duration: 0.15), .scale(to: 1, duration: 0.2)]))
    }

    /// A small sympathetic head tilt: for wrong answers. Never sad.
    func tilt() {
        head.run(.sequence([.rotate(toAngle: 0.15, duration: 0.15), .wait(forDuration: 0.3), .rotate(toAngle: 0, duration: 0.2)]))
    }

    /// Points one arm towards something.
    func point(right: Bool) {
        let arm = right ? rightArm : leftArm
        arm.run(.sequence([.rotate(toAngle: right ? 1.0 : -1.0, duration: 0.2), .wait(forDuration: 1.2), .rotate(toAngle: 0, duration: 0.3)]))
    }
}
