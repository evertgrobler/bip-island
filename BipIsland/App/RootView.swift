import SpriteKit
import SwiftUI

/// The game scene full screen, with the parent gate on top when it's open.
struct RootView: View {
    let coordinator: GameCoordinator
    @ObservedObject var gate: ParentGateModel

    var body: some View {
        ZStack(alignment: .topTrailing) {
            GameView(skView: coordinator.skView)
                .ignoresSafeArea()

            if gate.phase == .closed && gate.holdProgress > 0 {
                HoldRing(progress: gate.holdProgress)
            }

            if gate.phase != .closed {
                ParentGateView(gate: gate)
            }
        }
        .background(Color(nsColor: Palette.paper))
    }
}

/// Hosts the coordinator's SpriteKit view.
struct GameView: NSViewRepresentable {
    let skView: SKView

    func makeNSView(context: Context) -> SKView {
        skView
    }

    func updateNSView(_ nsView: SKView, context: Context) {}
}
