import AppKit

/// Warm, bright, hand-drawn colours.
enum Palette {
    static let paper = rgb(0.98, 0.95, 0.87)
    static let ink = rgb(0.22, 0.16, 0.12)
    static let sea = rgb(0.45, 0.76, 0.90)
    static let sand = rgb(0.97, 0.86, 0.60)
    static let grass = rgb(0.47, 0.76, 0.38)
    static let sun = rgb(1.00, 0.80, 0.24)
    static let orange = rgb(1.00, 0.56, 0.26)
    static let red = rgb(0.91, 0.30, 0.25)
    static let pink = rgb(0.97, 0.66, 0.73)
    static let deepPink = rgb(0.92, 0.45, 0.58)
    static let teal = rgb(0.24, 0.75, 0.70)
    static let lightTeal = rgb(0.55, 0.87, 0.82)
    static let purple = rgb(0.61, 0.45, 0.83)
    static let brown = rgb(0.60, 0.40, 0.24)
    static let lightBrown = rgb(0.78, 0.58, 0.38)
    static let stone = rgb(0.86, 0.82, 0.76)
    static let ice = rgb(0.88, 0.95, 1.00)
    static let eggBlue = rgb(0.72, 0.88, 0.95)
    static let leaf = rgb(0.36, 0.68, 0.30)
    static let bubble = rgb(0.80, 0.93, 0.98)
    static let go = rgb(0.38, 0.75, 0.36)
    static let card = rgb(1.00, 0.99, 0.96)

    static func rgb(_ r: CGFloat, _ g: CGFloat, _ b: CGFloat) -> NSColor {
        NSColor(srgbRed: r, green: g, blue: b, alpha: 1)
    }
}

/// The chalky handwritten font for letters (built into macOS), with a fallback.
enum Fonts {
    static let letters = NSFont(name: "ChalkboardSE-Bold", size: 12) != nil ? "ChalkboardSE-Bold" : "MarkerFelt-Wide"
}
