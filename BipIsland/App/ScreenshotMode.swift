import AppKit
import SpriteKit

/// Screenshots for the download page, taken by CI on the macOS runner (scripts/ci/take_screenshots.sh).
/// Only runs when the app is started with BIP_SCREENSHOTS=<folder>: it opens each scene in turn,
/// saves it as a PNG, then quits. A normal launch never touches this.
final class ScreenshotMode {
    static var outputFolder: URL? {
        guard let path = ProcessInfo.processInfo.environment["BIP_SCREENSHOTS"], !path.isEmpty else { return nil }
        return URL(fileURLWithPath: path, isDirectory: true)
    }

    /// How long each scene gets to fade in, animate and settle before it's captured.
    private static let settleTime: TimeInterval = 4

    private let coordinator: GameCoordinator
    private let folder: URL
    private let onFinish: () -> Void

    init(coordinator: GameCoordinator, folder: URL, onFinish: @escaping () -> Void) {
        self.coordinator = coordinator
        self.folder = folder
        self.onFinish = onFinish
    }

    /// File name and how to get there. The page (site/index.html) uses these names.
    private var shots: [(String, () -> Void)] {
        let c = coordinator
        return [
            ("map", { c.showMap() }),
            ("letters-island", { c.showLettersIsland(greet: false) }),
            ("letters-game", { c.startMonster() }),
            ("numbers-island", { c.showNumbersIsland(greet: false) }),
            ("numbers-game", { c.startCount() }),
            ("words-island", { c.showWordsIsland(greet: false) }),
            ("words-game", { c.startBuilder() }),
            ("coding-island", { c.showCodingIsland(greet: false) }),
            ("coding-game", { c.startPath() }),
            ("coding-order", { c.startOrder() }),
            ("stickers", { c.showStickers() }),
            ("charging", { c.showCharging() }),
        ]
    }

    func run() {
        try? FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        take(shots, index: 0)
    }

    private func take(_ list: [(String, () -> Void)], index: Int) {
        guard index < list.count else {
            NSLog("Bip Island screenshots: done")
            onFinish()
            return
        }
        let (name, open) = list[index]
        open()
        DispatchQueue.main.asyncAfter(deadline: .now() + Self.settleTime) { [weak self] in
            guard let self else { return }
            self.capture(name)
            self.take(list, index: index + 1)
        }
    }

    private func capture(_ name: String) {
        let view = coordinator.skView
        guard let scene = view.scene else {
            NSLog("Bip Island screenshots: %@ has no scene", name)
            return
        }
        let crop = CGRect(x: -scene.size.width * scene.anchorPoint.x, y: -scene.size.height * scene.anchorPoint.y,
                          width: scene.size.width, height: scene.size.height)
        guard let image = view.texture(from: scene, crop: crop)?.cgImage() else {
            NSLog("Bip Island screenshots: %@ didn't render", name)
            return
        }
        let rep = NSBitmapImageRep(cgImage: image)
        guard let png = rep.representation(using: .png, properties: [:]) else { return }
        let url = folder.appendingPathComponent("\(name).png")
        do {
            try png.write(to: url)
            NSLog("Bip Island screenshots: saved %@ (%dx%d, %@)", name, image.width, image.height,
                  String(describing: type(of: scene)))
        } catch {
            NSLog("Bip Island screenshots: couldn't save %@: %@", name, error.localizedDescription)
        }
    }
}
