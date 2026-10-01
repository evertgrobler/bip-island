import AppKit
import SpriteKit

/// A big, bright mouse pointer that small children can find on the screen: about three times
/// the size of the normal arrow, orange with a thick ink outline in the game's hand-drawn style.
enum BigCursor {
    /// Width and height of the pointer, in points.
    static let size: CGFloat = 72

    static let arrow: NSCursor = {
        // The classic arrow, tip at the top left, in a 0…1 box (y runs down, like the hot spot).
        let shape: [CGPoint] = [
            CGPoint(x: 0.10, y: 0.06), CGPoint(x: 0.10, y: 0.80), CGPoint(x: 0.28, y: 0.63),
            CGPoint(x: 0.42, y: 0.92), CGPoint(x: 0.56, y: 0.86), CGPoint(x: 0.42, y: 0.57),
            CGPoint(x: 0.66, y: 0.57),
        ]
        let image = NSImage(size: NSSize(width: size, height: size), flipped: true) { _ in
            func path(offset: CGFloat) -> NSBezierPath {
                let p = NSBezierPath()
                for (i, point) in shape.enumerated() {
                    let scaled = NSPoint(x: point.x * size + offset, y: point.y * size + offset)
                    if i == 0 { p.move(to: scaled) } else { p.line(to: scaled) }
                }
                p.close()
                p.lineJoinStyle = .round
                p.lineWidth = 4.5
                return p
            }
            // A soft shadow, the orange body, then the ink outline on top.
            Palette.ink.withAlphaComponent(0.25).setFill()
            path(offset: 3).fill()
            let body = path(offset: 0)
            Palette.orange.setFill()
            body.fill()
            Palette.ink.setStroke()
            body.stroke()
            return true
        }
        return NSCursor(image: image, hotSpot: NSPoint(x: shape[0].x * size, y: shape[0].y * size))
    }()
}

/// The game's SpriteKit view: shows the big pointer whenever the mouse is over the game.
final class GameSKView: SKView {
    override func resetCursorRects() {
        discardCursorRects()
        addCursorRect(bounds, cursor: BigCursor.arrow)
    }

    override func updateTrackingAreas() {
        super.updateTrackingAreas()
        if !trackingAreas.contains(where: { $0.owner === self && $0.options.contains(.cursorUpdate) }) {
            addTrackingArea(NSTrackingArea(rect: .zero, options: [.cursorUpdate, .activeAlways, .inVisibleRect], owner: self))
        }
    }

    override func cursorUpdate(with event: NSEvent) {
        BigCursor.arrow.set()
    }
}
