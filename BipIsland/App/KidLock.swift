import AppKit

/// A borderless window that can still take keyboard focus.
final class KidWindow: NSWindow {
    override var canBecomeKey: Bool { true }
    override var canBecomeMain: Bool { true }
}

/// Full-screen kid lock: covers every screen, hides the Dock and menu bar, and blocks app switching,
/// Force Quit, hiding and Cmd-Q. Only the parent gate (or a system shut down / log out) can quit.
final class KidLock {
    let enabled: Bool
    /// Set by the parent gate (or Sparkle relaunching after an update) to let the app quit.
    var allowQuit = false

    private var mainWindow: NSWindow?
    private var coverWindows: [NSWindow] = []
    private var observers: [NSObjectProtocol] = []

    static let presentationOptions: NSApplication.PresentationOptions = [
        .hideDock,
        .hideMenuBar,
        .disableProcessSwitching,
        .disableForceQuit,
        .disableHideApplication,
        .disableAppleMenu,
    ]

    init(enabled: Bool) {
        self.enabled = enabled
    }

    func install(content: NSView) {
        let window: NSWindow
        if enabled {
            let frame = Self.mainScreen?.frame ?? NSRect(x: 0, y: 0, width: 1440, height: 900)
            window = KidWindow(contentRect: frame, styleMask: [.borderless], backing: .buffered, defer: false)
            window.isMovable = false
            window.hasShadow = false
        } else {
            window = NSWindow(
                contentRect: NSRect(x: 0, y: 0, width: 1280, height: 800),
                styleMask: [.titled, .closable, .miniaturizable, .resizable],
                backing: .buffered,
                defer: false
            )
            window.title = "Bip Island (parent mode)"
            window.center()
        }
        window.isReleasedWhenClosed = false
        window.backgroundColor = Palette.paper
        window.contentView = content
        mainWindow = window

        NSApp.activate(ignoringOtherApps: true)
        window.makeKeyAndOrderFront(nil)

        guard enabled else { return }
        applyPresentationOptions()
        layoutWindows()

        let centre = NotificationCenter.default
        observers.append(centre.addObserver(forName: NSApplication.didChangeScreenParametersNotification, object: nil, queue: .main) { [weak self] _ in
            self?.layoutWindows()
        })
        observers.append(centre.addObserver(forName: NSApplication.didBecomeActiveNotification, object: nil, queue: .main) { [weak self] _ in
            self?.applyPresentationOptions()
        })
    }

    func applyPresentationOptions() {
        guard enabled, !allowQuit else { return }
        NSApp.presentationOptions = Self.presentationOptions
    }

    private static var mainScreen: NSScreen? {
        NSScreen.screens.first ?? NSScreen.main
    }

    /// Main window fills the main screen; plain paper windows cover any other screens,
    /// so a click there can't reach the desktop or another app.
    private func layoutWindows() {
        guard let mainWindow, let main = Self.mainScreen else { return }
        mainWindow.setFrame(main.frame, display: true)

        coverWindows.forEach { $0.orderOut(nil) }
        coverWindows = NSScreen.screens.dropFirst().map { screen in
            let cover = NSWindow(contentRect: screen.frame, styleMask: [.borderless], backing: .buffered, defer: false)
            cover.isReleasedWhenClosed = false
            cover.backgroundColor = Palette.paper
            cover.setFrame(screen.frame, display: true)
            cover.orderFront(nil)
            return cover
        }
        mainWindow.makeKeyAndOrderFront(nil)
    }

    func shouldAllowTermination() -> Bool {
        if !enabled || allowQuit { return true }
        return Self.isSystemShutdownRestartOrLogout()
    }

    /// Never block the Mac from shutting down, restarting or logging out.
    private static func isSystemShutdownRestartOrLogout() -> Bool {
        guard let event = NSAppleEventManager.shared().currentAppleEvent,
              let reason = event.attributeDescriptor(forKeyword: fourCharCode("why?"))?.typeCodeValue
        else { return false }
        let systemReasons = ["shut", "rest", "rlgo", "logo", "rrst", "rsdn"].map(fourCharCode)
        return systemReasons.contains(reason)
    }

    private static func fourCharCode(_ code: String) -> FourCharCode {
        code.utf8.reduce(0) { ($0 << 8) | FourCharCode($1) }
    }
}
