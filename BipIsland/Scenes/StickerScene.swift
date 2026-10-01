import BipCore
import SpriteKit

/// The sticker book: every sound and skill sticker earned so far, the rest as grey outlines.
/// A page for sounds, a page for skills; the jar shows today's star count.
final class StickerScene: BaseScene {
    private var showingSkills = false
    private var page: SKNode?
    private let soundStickers: [PhonicsSound]
    private let skillStickers: [Skill]
    private let earned: Set<String>
    private let starCount: Int

    override init(coordinator: GameCoordinator) {
        let content = coordinator.content
        soundStickers = content.map { PhonicsCourse($0.phonics).allSounds } ?? []
        skillStickers = content?.skills.skills ?? []
        earned = content.map { StickerBook.earned(progress: coordinator.progress, content: $0) } ?? []
        starCount = coordinator.progress.stars
        super.init(coordinator: coordinator)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not used")
    }

    override func didMove(to view: SKView) {
        addHomeButton()
        addBip(at: CGPoint(x: -640, y: -380), scale: 0.7)

        let jar = Sketch.label("\(starCount) stars in the jar", size: 56, colour: Palette.ink)
        jar.position = CGPoint(x: 0, y: 400)
        addChild(jar)

        let toggle = Buttons.next()
        toggle.name = "tap:page"
        toggle.position = CGPoint(x: 640, y: 360)
        toggle.zPosition = 10
        addChild(toggle)

        drawPage()
    }

    private func drawPage() {
        page?.removeFromParent()
        let holder = SKNode()
        holder.zPosition = 5
        addChild(holder)
        page = holder
        if showingSkills {
            let perRow = 8
            for (i, skill) in skillStickers.enumerated() {
                let got = earned.contains("skill_\(skill.id)")
                let star = Sketch.node(.polygon(Sketch.starPoints(center: .zero, radius: 40)),
                                       fill: got ? Palette.sun : Palette.card.withAlphaComponent(0.5), lineWidth: 4, seed: 1110 + UInt64(i))
                star.position = CGPoint(x: -420 + CGFloat(i % perRow) * 120, y: 260 - CGFloat(i / perRow) * 95)
                star.alpha = got ? 1 : 0.45
                holder.addChild(star)
            }
        } else {
            let perRow = 9
            for (i, sound) in soundStickers.enumerated() {
                let got = earned.contains("sound_\(sound.id)")
                let badge = SKNode()
                badge.addChild(Sketch.node(.ellipse(center: .zero, rx: 56, ry: 56),
                                           fill: got ? Palette.sun : Palette.card.withAlphaComponent(0.5), lineWidth: 4, seed: 1120 + UInt64(i)))
                let letter = Sketch.letter(sound.grapheme, size: 64, shadow: nil)
                badge.addChild(letter)
                badge.position = CGPoint(x: -520 + CGFloat(i % perRow) * 130, y: 240 - CGFloat(i / perRow) * 105)
                badge.alpha = got ? 1 : 0.45
                holder.addChild(badge)
            }
        }
    }

    override func handleTap(name: String, node: SKNode) {
        if name == "tap:page" {
            sfx.play(.tick)
            Buttons.press(node)
            showingSkills.toggle()
            drawPage()
        }
    }

    override func goHome() {
        coordinator.showMap()
    }
}
