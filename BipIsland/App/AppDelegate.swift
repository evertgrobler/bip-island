import AppKit
import SwiftUI
import BipCore

final class AppDelegate: NSObject, NSApplicationDelegate {
    private var kidLock: KidLock!
    private var coordinator: GameCoordinator!
    private var gate: ParentGateModel!
    private var updates: UpdateController!
    private var keyMonitor: Any?

    func applicationDidFinishLaunching(_ notification: Notification) {
        // Holding Option while the app opens starts "parent mode": a normal window, no kid lock.
        let parentMode = NSEvent.modifierFlags.contains(.option)

        updates = UpdateController()
        coordinator = GameCoordinator()
        gate = ParentGateModel(
            versionText: Self.versionText,
            voiceText: coordinator.voice.statusText,
            updatesConfigured: updates.isConfigured
        )
        kidLock = KidLock(enabled: !parentMode)

        gate.onOpen = { [weak self] in self?.coordinator.setPaused(true) }
        gate.onClose = { [weak self] in self?.coordinator.setPaused(false) }
        gate.onQuit = { [weak self] in self?.quitForReal() }
        gate.onCheckForUpdates = { [weak self] in self?.updates.checkForUpdates() }
        gate.onEndBreakEarly = { [weak self] in self?.coordinator.endBreakEarly() }
        gate.playSettings = coordinator.playSettings
        updates.onWillRelaunch = { [weak self] in self?.kidLock.allowQuit = true }

        installMainMenu()
        let root = RootView(coordinator: coordinator, gate: gate)
        kidLock.install(content: NSHostingView(rootView: root))
        installKeyMonitor()
        coordinator.start()
        BigCursor.arrow.set()
    }

    func applicationShouldTerminate(_ sender: NSApplication) -> NSApplication.TerminateReply {
        kidLock.shouldAllowTermination() ? .terminateNow : .terminateCancel
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        true
    }

    private func quitForReal() {
        kidLock.allowQuit = true
        NSApp.presentationOptions = []
        NSApp.terminate(nil)
    }

    static var versionText: String {
        let info = Bundle.main.infoDictionary ?? [:]
        let version = info["CFBundleShortVersionString"] as? String ?? "?"
        let build = info["CFBundleVersion"] as? String ?? "?"
        return "Version \(version) (build \(build))"
    }

    // MARK: Keyboard

    private static let escapeKeyCode: UInt16 = 53

    /// Every key goes through here first. While the game is showing, keys never reach the system
    /// (so Cmd-Q, Cmd-H, Cmd-W do nothing): Esc held for 3 seconds opens the parent gate, and any
    /// other key replays the current sound.
    private func installKeyMonitor() {
        keyMonitor = NSEvent.addLocalMonitorForEvents(matching: [.keyDown, .keyUp]) { [weak self] event in
            guard let self else { return event }
            return self.handle(event)
        }
    }

    private func handle(_ event: NSEvent) -> NSEvent? {
        let isEscape = event.keyCode == Self.escapeKeyCode

        if gate.phase != .closed {
            // Grown-up typing into the gate. A fresh Esc press closes it.
            if isEscape {
                if event.type == .keyDown && !event.isARepeat { gate.close() }
                return nil
            }
            return event
        }

        if !kidLock.enabled && event.modifierFlags.contains(.command) {
            return event // parent mode: normal shortcuts work
        }

        if isEscape {
            if event.type == .keyDown {
                gate.escapePressed()
            } else {
                gate.escapeReleased()
            }
            return nil
        }

        if event.type == .keyDown && !event.isARepeat {
            coordinator.handleKey(event)
        }
        return nil
    }

    private func installMainMenu() {
        let mainMenu = NSMenu()
        let appItem = NSMenuItem()
        mainMenu.addItem(appItem)
        let appMenu = NSMenu()
        appMenu.addItem(withTitle: "Quit Bip Island", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        appItem.submenu = appMenu
        NSApp.mainMenu = mainMenu
    }
}
