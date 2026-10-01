import AppKit

/// Starts the app with an AppKit delegate, so we control the kid-lock window ourselves.
@main
enum BipIslandMain {
    @MainActor
    static func main() {
        let app = NSApplication.shared
        let delegate = AppDelegate()
        app.delegate = delegate
        app.setActivationPolicy(.regular)
        withExtendedLifetime(delegate) {
            app.run()
        }
    }
}
