import SpriteKit

/// Big, round, picture-only buttons (no reading needed). Every one is at least 120 pt on screen.
enum Buttons {
    /// Back home: a little house.
    static func home() -> SKNode {
        let n = SKNode()
        n.name = "tap:home"
        n.addChild(Sketch.node(.ellipse(center: .zero, rx: 72, ry: 72), fill: Palette.sun, seed: 500))
        n.addChild(Sketch.node(.polygon([CGPoint(x: -34, y: -30), CGPoint(x: 34, y: -30), CGPoint(x: 34, y: 8), CGPoint(x: -34, y: 8)]), fill: Palette.card, lineWidth: 4.5, seed: 501))
        n.addChild(Sketch.node(.polygon([CGPoint(x: -46, y: 6), CGPoint(x: 0, y: 44), CGPoint(x: 46, y: 6)]), fill: Palette.red, lineWidth: 4.5, seed: 502))
        n.addChild(Sketch.node(.polygon([CGPoint(x: -10, y: -30), CGPoint(x: 10, y: -30), CGPoint(x: 10, y: -4), CGPoint(x: -10, y: -4)]), fill: Palette.brown, lineWidth: 3.5, seed: 503))
        return n
    }

    /// Hear it again: a speaker with sound waves.
    static func replay() -> SKNode {
        let n = SKNode()
        n.name = "tap:replay"
        n.addChild(Sketch.node(.ellipse(center: .zero, rx: 72, ry: 72), fill: Palette.lightTeal, seed: 510))
        n.addChild(Sketch.node(.polygon([CGPoint(x: -36, y: -14), CGPoint(x: -18, y: -14), CGPoint(x: 4, y: -34), CGPoint(x: 4, y: 34), CGPoint(x: -18, y: 14), CGPoint(x: -36, y: 14)]), fill: Palette.ink, lineWidth: 4, seed: 511))
        for (i, r) in [CGFloat(20), 34].enumerated() {
            n.addChild(Sketch.node(.arc(center: CGPoint(x: 6, y: 0), rx: r, ry: r * 1.1, from: -0.9, to: 0.9), lineWidth: 5, seed: 512 + UInt64(i)))
        }
        return n
    }

    /// Carry on: a green circle with an arrow.
    static func next() -> SKNode {
        let n = SKNode()
        n.name = "tap:next"
        n.addChild(Sketch.node(.ellipse(center: .zero, rx: 88, ry: 88), fill: Palette.go, seed: 520))
        n.addChild(Sketch.node(.polygon([CGPoint(x: -40, y: -14), CGPoint(x: 6, y: -14), CGPoint(x: 6, y: -40), CGPoint(x: 48, y: 0), CGPoint(x: 6, y: 40), CGPoint(x: 6, y: 14), CGPoint(x: -40, y: 14)]), fill: .white, lineWidth: 4.5, seed: 521))
        return n
    }

    /// Play: a green circle with a triangle.
    static func play() -> SKNode {
        let n = SKNode()
        n.name = "tap:play"
        n.addChild(Sketch.node(.ellipse(center: .zero, rx: 100, ry: 100), fill: Palette.go, lineWidth: 6, seed: 530))
        n.addChild(Sketch.node(.polygon([CGPoint(x: -30, y: -46), CGPoint(x: 52, y: 0), CGPoint(x: -30, y: 46)]), fill: .white, lineWidth: 5, seed: 531))
        return n
    }

    /// Gentle "press" bounce.
    static func press(_ node: SKNode) {
        node.run(.sequence([.scale(to: 0.9, duration: 0.06), .scale(to: 1.05, duration: 0.1), .scale(to: 1, duration: 0.08)]))
    }

    /// Slow pulse to say "click me".
    static func pulse() -> SKAction {
        let grow = SKAction.scale(to: 1.07, duration: 0.7)
        grow.timingMode = .easeInEaseOut
        let shrink = SKAction.scale(to: 1, duration: 0.7)
        shrink.timingMode = .easeInEaseOut
        return .repeatForever(.sequence([grow, shrink]))
    }

    /// Shake for a wrong answer.
    static func shake() -> SKAction {
        .sequence([
            .rotate(toAngle: 0.08, duration: 0.05), .rotate(toAngle: -0.08, duration: 0.08),
            .rotate(toAngle: 0.05, duration: 0.07), .rotate(toAngle: 0, duration: 0.06),
        ])
    }

    /// Wiggle for a hint ("look for the one that's wiggling!").
    static func hintWiggle() -> SKAction {
        .repeatForever(.sequence([
            .group([.rotate(toAngle: 0.1, duration: 0.18), .scale(to: 1.08, duration: 0.18)]),
            .group([.rotate(toAngle: -0.1, duration: 0.18), .scale(to: 1.0, duration: 0.18)]),
            .rotate(toAngle: 0, duration: 0.1),
            .wait(forDuration: 0.4),
        ]))
    }

    /// A burst of little paper confetti dots.
    static func sparkle(at point: CGPoint, in parent: SKNode) {
        let colours = [Palette.sun, Palette.red, Palette.teal, Palette.purple, Palette.orange, Palette.go]
        for i in 0..<12 {
            let dot = SKShapeNode(circleOfRadius: CGFloat.random(in: 6...11))
            dot.fillColor = colours[i % colours.count]
            dot.strokeColor = Palette.ink
            dot.lineWidth = 2
            dot.position = point
            dot.zPosition = 50
            parent.addChild(dot)
            let angle = CGFloat(i) / 12 * 2 * CGFloat.pi + CGFloat.random(in: -0.2...0.2)
            let distance = CGFloat.random(in: 90...170)
            let fly = SKAction.moveBy(x: distance * cos(angle), y: distance * sin(angle), duration: 0.5)
            fly.timingMode = .easeOut
            dot.run(.sequence([.group([fly, .fadeOut(withDuration: 0.6), .scale(to: 0.4, duration: 0.6)]), .removeFromParent()]))
        }
    }
}
