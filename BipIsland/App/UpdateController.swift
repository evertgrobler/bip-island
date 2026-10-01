import Foundation
import Sparkle

/// Sparkle updates. Only switched on when the build has a feed URL and public key
/// (release builds from `main`); local and pull-request builds leave it off.
///
/// Updates never install on their own. When the game opens (and every hour after), Sparkle
/// checks quietly. If a new version is found, Sparkle's window stays hidden and the game shows
/// a small "Update ready" button instead. A grown-up taps it, passes the parent gate, and only
/// then does Sparkle show its install window.
final class UpdateController: NSObject, SPUUpdaterDelegate, SPUStandardUserDriverDelegate {
    let isConfigured: Bool
    var onWillRelaunch: (() -> Void)?
    /// A new version was found (its version number), or nil once the update is installed or dismissed.
    var onUpdateReady: ((String?) -> Void)?
    private var controller: SPUStandardUpdaterController?

    override init() {
        let info = Bundle.main.infoDictionary ?? [:]
        let feed = (info["SUFeedURL"] as? String) ?? ""
        let key = (info["SUPublicEDKey"] as? String) ?? ""
        isConfigured = feed.hasPrefix("https://") && !key.isEmpty
        super.init()
        if isConfigured {
            controller = SPUStandardUpdaterController(startingUpdater: true, updaterDelegate: self, userDriverDelegate: self)
            // Older builds installed updates silently on quit; make sure that's off.
            controller?.updater.automaticallyDownloadsUpdates = false
        }
    }

    /// A quiet check when the game opens, so a waiting update shows up straight away.
    func checkQuietlyOnLaunch() {
        DispatchQueue.main.asyncAfter(deadline: .now() + 5) { [weak self] in
            guard let updater = self?.controller?.updater, updater.canCheckForUpdates, !updater.sessionInProgress else { return }
            updater.checkForUpdatesInBackground()
        }
    }

    /// Shows Sparkle's install window. Only called after the parent gate.
    func checkForUpdates() {
        controller?.checkForUpdates(nil)
    }

    // Sparkle quits the app to install an update and relaunch it; let it through the kid lock.
    func updaterWillRelaunchApplication(_ updater: SPUUpdater) {
        onWillRelaunch?()
    }

    // MARK: Keep Sparkle's window hidden until a grown-up asks for it

    var supportsGentleScheduledUpdateReminders: Bool { true }

    func standardUserDriverShouldHandleShowingScheduledUpdate(_ update: SUAppcastItem, andInImmediateFocus immediateFocus: Bool) -> Bool {
        false
    }

    func standardUserDriverWillHandleShowingUpdate(_ handleShowingUpdate: Bool, forUpdate update: SUAppcastItem, state: SPUUserUpdateState) {
        guard !handleShowingUpdate else { return }
        onUpdateReady?(update.displayVersionString)
    }

    func standardUserDriverWillFinishUpdateSession() {
        onUpdateReady?(nil)
    }
}
