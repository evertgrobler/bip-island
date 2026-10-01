import BipCore
import SpriteKit

/// Bip is charging: games stay closed until the break ends. The scene checks every few
/// seconds; the break counts on the wall clock, so quitting can't skip it. Parents can end
/// the break early behind the parent gate.
final class ChargingScene: BaseScene {
    private var batteryFill: SKNode?
    private var checks = 0
    private static let maxChecks = 240 // About 20 minutes, in case the clock jumps.

    override func didMove(to view: SKView) {
        addBip(at: CGPoint(x: -200, y: -40), scale: 1.1)
        bip.tilt()

        let battery = Sketch.node(.roundedRect(CGRect(x: -110, y: -190, width: 220, height: 380), radius: 36),
                                  fill: Palette.card, lineWidth: 7, seed: 1100)
        battery.position = CGPoint(x: 320, y: 20)
        battery.zPosition = 5
        addChild(battery)
        let nub = Sketch.node(.roundedRect(CGRect(x: -40, y: -30, width: 80, height: 60), radius: 14),
                              fill: Palette.card, lineWidth: 7, seed: 1101)
        nub.position = CGPoint(x: 320, y: 230)
        nub.zPosition = 5
        addChild(nub)
        let fill = Sketch.node(.roundedRect(CGRect(x: -88, y: -168, width: 176, height: 60), radius: 20),
                               fill: Palette.go, lineWidth: 0, seed: 1102)
        fill.position = CGPoint(x: 320, y: -100)
        fill.zPosition = 6
        addChild(fill)
        batteryFill = fill

        let plug = Sketch.label("charging…", size: 64, colour: Palette.ink)
        plug.position = CGPoint(x: 0, y: -360)
        addChild(plug)

        sfx.play(.whirr)
        after(5) { [weak self] in self?.check() }
    }

    private func check() {
        checks += 1
        if coordinator.currentBreakPhase() == .playing || checks >= Self.maxChecks {
            coordinator.showMap()
            return
        }
        // The fill grows while charging; it is decoration, not a countdown.
        batteryFill?.run(.scale(to: 1.15, duration: 2.5))
        after(5) { [weak self] in self?.check() }
    }

    override func goHome() {}
}
