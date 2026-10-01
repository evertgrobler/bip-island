import BipCore
import SpriteKit

/// A shape drawn with a slightly wobbly hand. The same seed always gives the same wobble.
enum SketchShape {
    case ellipse(center: CGPoint, rx: CGFloat, ry: CGFloat)
    case roundedRect(CGRect, radius: CGFloat)
    case polygon([CGPoint])
    case polyline([CGPoint])
    /// An arc of an ellipse from one angle to another (radians), open.
    case arc(center: CGPoint, rx: CGFloat, ry: CGFloat, from: CGFloat, to: CGFloat)

    var isClosed: Bool {
        switch self {
        case .polyline, .arc: return false
        default: return true
        }
    }

    func points(spacing: CGFloat = 18) -> [CGPoint] {
        switch self {
        case let .ellipse(c, rx, ry):
            let count = max(14, Int(2 * .pi * max(rx, ry) / spacing))
            return (0..<count).map { i in
                let a = CGFloat(i) / CGFloat(count) * 2 * .pi
                return CGPoint(x: c.x + rx * cos(a), y: c.y + ry * sin(a))
            }
        case let .arc(c, rx, ry, from, to):
            let count = max(6, Int(abs(to - from) * max(rx, ry) / spacing))
            return (0...count).map { i in
                let a = from + (to - from) * CGFloat(i) / CGFloat(count)
                return CGPoint(x: c.x + rx * cos(a), y: c.y + ry * sin(a))
            }
        case let .roundedRect(rect, radius):
            let r = min(radius, rect.width / 2, rect.height / 2)
            var corners: [CGPoint] = []
            let centres: [(CGPoint, CGFloat)] = [
                (CGPoint(x: rect.maxX - r, y: rect.minY + r), -CGFloat.pi / 2),
                (CGPoint(x: rect.maxX - r, y: rect.maxY - r), 0),
                (CGPoint(x: rect.minX + r, y: rect.maxY - r), CGFloat.pi / 2),
                (CGPoint(x: rect.minX + r, y: rect.minY + r), CGFloat.pi),
            ]
            for (centre, start) in centres {
                for step in 0...4 {
                    let a = start + CGFloat(step) / 4 * (.pi / 2)
                    corners.append(CGPoint(x: centre.x + r * cos(a), y: centre.y + r * sin(a)))
                }
            }
            return Sketch.subdivide(corners, spacing: spacing, closed: true)
        case let .polygon(points):
            return Sketch.subdivide(points, spacing: spacing, closed: true)
        case let .polyline(points):
            return Sketch.subdivide(points, spacing: spacing, closed: false)
        }
    }
}

enum Sketch {
    static func subdivide(_ points: [CGPoint], spacing: CGFloat, closed: Bool) -> [CGPoint] {
        guard points.count > 1 else { return points }
        var result: [CGPoint] = []
        let edgeCount = closed ? points.count : points.count - 1
        for i in 0..<edgeCount {
            let a = points[i]
            let b = points[(i + 1) % points.count]
            let length = hypot(b.x - a.x, b.y - a.y)
            let steps = max(1, Int(length / spacing))
            for k in 0..<steps {
                let f = CGFloat(k) / CGFloat(steps)
                result.append(CGPoint(x: a.x + (b.x - a.x) * f, y: a.y + (b.y - a.y) * f))
            }
        }
        if !closed, let last = points.last { result.append(last) }
        return result
    }

    static func jitter(_ points: [CGPoint], amount: CGFloat, rng: inout SeededGenerator) -> [CGPoint] {
        guard amount > 0 else { return points }
        return points.map {
            CGPoint(x: $0.x + CGFloat.random(in: -amount...amount, using: &rng),
                    y: $0.y + CGFloat.random(in: -amount...amount, using: &rng))
        }
    }

    /// A smooth curve through the points (Catmull–Rom), which turns jittered points into a soft wobbly line.
    static func smoothPath(_ p: [CGPoint], closed: Bool) -> CGPath {
        let path = CGMutablePath()
        guard let first = p.first else { return path }
        path.move(to: first)
        guard p.count > 2 else {
            p.dropFirst().forEach { path.addLine(to: $0) }
            if closed { path.closeSubpath() }
            return path
        }
        let n = p.count
        let segments = closed ? n : n - 1
        for i in 0..<segments {
            let p0 = closed ? p[(i - 1 + n) % n] : p[max(i - 1, 0)]
            let p1 = p[i]
            let p2 = p[(i + 1) % n]
            let p3 = closed ? p[(i + 2) % n] : p[min(i + 2, n - 1)]
            let c1 = CGPoint(x: p1.x + (p2.x - p0.x) / 6, y: p1.y + (p2.y - p0.y) / 6)
            let c2 = CGPoint(x: p2.x - (p3.x - p1.x) / 6, y: p2.y - (p3.y - p1.y) / 6)
            path.addCurve(to: p2, control1: c1, control2: c2)
        }
        if closed { path.closeSubpath() }
        return path
    }

    static func path(_ shape: SketchShape, wobble: CGFloat, seed: UInt64) -> CGPath {
        var rng = SeededGenerator(seed: seed)
        return smoothPath(jitter(shape.points(), amount: wobble, rng: &rng), closed: shape.isClosed)
    }

    /// A hand-drawn shape: a colour fill slightly off the line (like colouring in), a wobbly ink outline,
    /// and a fainter second pass of the pen.
    static func node(_ shape: SketchShape, fill: NSColor? = nil, ink: NSColor = Palette.ink,
                     lineWidth: CGFloat = 5, wobble: CGFloat = 2.2, seed: UInt64) -> SKNode {
        let container = SKNode()
        if let fill, shape.isClosed {
            let fillNode = SKShapeNode(path: path(shape, wobble: wobble * 0.6, seed: seed &+ 101))
            fillNode.fillColor = fill
            fillNode.strokeColor = .clear
            fillNode.lineWidth = 0
            fillNode.position = CGPoint(x: 2, y: -2)
            container.addChild(fillNode)
        }
        if lineWidth > 0 {
            let line = SKShapeNode(path: path(shape, wobble: wobble, seed: seed))
            line.strokeColor = ink
            line.fillColor = .clear
            line.lineWidth = lineWidth
            line.lineCap = .round
            line.lineJoin = .round
            line.isAntialiased = true
            container.addChild(line)

            let secondPass = SKShapeNode(path: path(shape, wobble: wobble * 1.4, seed: seed &+ 7))
            secondPass.strokeColor = ink.withAlphaComponent(0.35)
            secondPass.fillColor = .clear
            secondPass.lineWidth = max(1.5, lineWidth * 0.4)
            secondPass.lineCap = .round
            secondPass.isAntialiased = true
            container.addChild(secondPass)
        }
        return container
    }

    /// A letter in chalky handwriting with a coloured shadow behind it.
    static func letter(_ text: String, size: CGFloat, colour: NSColor = Palette.ink, shadow: NSColor? = Palette.sun) -> SKNode {
        let container = SKNode()
        if let shadow {
            let back = label(text, size: size, colour: shadow)
            back.position = CGPoint(x: size * 0.035, y: -size * 0.035)
            container.addChild(back)
        }
        container.addChild(label(text, size: size, colour: colour))
        return container
    }

    static func label(_ text: String, size: CGFloat, colour: NSColor) -> SKLabelNode {
        let node = SKLabelNode(fontNamed: Fonts.letters)
        node.text = text
        node.fontSize = size
        node.fontColor = colour
        node.verticalAlignmentMode = .center
        node.horizontalAlignmentMode = .center
        return node
    }

    /// A five-pointed star outline, for progress.
    static func starPoints(center: CGPoint, radius: CGFloat) -> [CGPoint] {
        (0..<10).map { i in
            let r = i.isMultiple(of: 2) ? radius : radius * 0.45
            let a = CGFloat.pi / 2 + CGFloat(i) * .pi / 5
            return CGPoint(x: center.x + r * cos(a), y: center.y + r * sin(a))
        }
    }

    /// Cream paper with speckles and fibres, made once and reused.
    static let paperTexture: SKTexture = {
        let width = 1024, height = 640
        guard let context = CGContext(data: nil, width: width, height: height, bitsPerComponent: 8, bytesPerRow: 0,
                                      space: CGColorSpaceCreateDeviceRGB(),
                                      bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)
        else { return SKTexture() }
        context.setFillColor(Palette.paper.cgColor)
        context.fill(CGRect(x: 0, y: 0, width: width, height: height))

        var rng = SeededGenerator(seed: 2026)
        for _ in 0..<9000 {
            let shade = CGFloat.random(in: 0.25...0.6, using: &rng)
            context.setFillColor(NSColor(srgbRed: shade, green: shade * 0.8, blue: shade * 0.55,
                                         alpha: CGFloat.random(in: 0.03...0.09, using: &rng)).cgColor)
            let size = CGFloat.random(in: 0.6...2.2, using: &rng)
            context.fillEllipse(in: CGRect(x: CGFloat.random(in: 0...CGFloat(width), using: &rng),
                                           y: CGFloat.random(in: 0...CGFloat(height), using: &rng),
                                           width: size, height: size))
        }
        context.setLineCap(.round)
        for _ in 0..<420 {
            context.setStrokeColor(NSColor(srgbRed: 0.55, green: 0.42, blue: 0.28,
                                           alpha: CGFloat.random(in: 0.03...0.07, using: &rng)).cgColor)
            context.setLineWidth(CGFloat.random(in: 0.5...1.2, using: &rng))
            let x = CGFloat.random(in: 0...CGFloat(width), using: &rng)
            let y = CGFloat.random(in: 0...CGFloat(height), using: &rng)
            context.move(to: CGPoint(x: x, y: y))
            context.addLine(to: CGPoint(x: x + CGFloat.random(in: -14...14, using: &rng),
                                        y: y + CGFloat.random(in: -6...6, using: &rng)))
            context.strokePath()
        }
        guard let image = context.makeImage() else { return SKTexture() }
        return SKTexture(cgImage: image)
    }()
}
