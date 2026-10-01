import Foundation
import Sparkle

/// Sparkle auto-updates. Only switched on when the build has a feed URL and public key
/// (release builds from `main`); local and pull-request builds leave it off.
final class UpdateController: NSObject, SPUUpdaterDelegate {
    let isConfigured: Bool
    var onWillRelaunch: (() -> Void)?
    private var controller: SPUStandardUpdaterController?

    override init() {
        let info = Bundle.main.infoDictionary ?? [:]
        let feed = (info["SUFeedURL"] as? String) ?? ""
        let key = (info["SUPublicEDKey"] as? String) ?? ""
        isConfigured = feed.hasPrefix("https://") && !key.isEmpty
        super.init()
        if isConfigured {
            controller = SPUStandardUpdaterController(startingUpdater: true, updaterDelegate: self, userDriverDelegate: nil)
        }
    }

    func checkForUpdates() {
        controller?.checkForUpdates(nil)
    }

    // Sparkle quits the app to install an update and relaunch it; let it through the kid lock.
    func updaterWillRelaunchApplication(_ updater: SPUUpdater) {
        onWillRelaunch?()
    }
}
