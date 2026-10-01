import AppKit

/// Warm, bright, hand-drawn colours. The named ones match the approved mock-ups:
/// ink #2B2A33, paper #FBF4E4, sunshine yellow #F7C548, sea blue #8FC9E8, leaf green #9BCB6B,
/// coral #E8654F, lilac #B9A2E8 (coding), sand #F3D9A4.
enum Palette {
    static let paper = hex(0xFBF4E4)
    static let ink = hex(0x2B2A33)
    static let sea = hex(0x8FC9E8)
    static let sand = hex(0xF3D9A4)
    static let grass = hex(0x9BCB6B)
    static let sun = hex(0xF7C548)
    static let orange = rgb(1.00, 0.56, 0.26)
    static let red = hex(0xE8654F)
    static let pink = rgb(0.97, 0.66, 0.73)
    static let deepPink = rgb(0.92, 0.45, 0.58)
    static let teal = rgb(0.24, 0.75, 0.70)
    static let lightTeal = rgb(0.55, 0.87, 0.82)
    static let purple = hex(0xB9A2E8)
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

    static func hex(_ value: UInt32) -> NSColor {
        rgb(CGFloat((value >> 16) & 0xFF) / 255, CGFloat((value >> 8) & 0xFF) / 255, CGFloat(value & 0xFF) / 255)
    }
}

/// The chalky handwritten font for letters (built into macOS), with a fallback.
enum Fonts {
    static let letters = NSFont(name: "ChalkboardSE-Bold", size: 12) != nil ? "ChalkboardSE-Bold" : "MarkerFelt-Wide"
}
