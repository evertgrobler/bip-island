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
            } else if gate.phase == .closed && gate.updateReady != nil {
                UpdateReadyButton { gate.openForUpdate() }
            }

            if gate.phase != .closed {
                ParentGateView(gate: gate, coordinator: coordinator)
            }
        }
        .background(Color(nsColor: Palette.paper))
    }
}

/// A small corner button when a new version of the game is waiting. It opens the parent gate;
/// the update only installs after the passcode or maths question.
struct UpdateReadyButton: View {
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            HStack(spacing: 10) {
                Image(systemName: "arrow.down.circle.fill")
                    .font(.system(size: 28))
                    .foregroundStyle(Color(nsColor: Palette.go))
                VStack(alignment: .leading, spacing: 2) {
                    Text("Update ready")
                        .font(Fonts.ui(18, bold: true))
                    Text("Grown-ups: tap here")
                        .font(Fonts.ui(14))
                }
            }
            .foregroundStyle(Color(nsColor: Palette.ink))
            .padding(.horizontal, 18)
            .padding(.vertical, 12)
            .background(Capsule().fill(Color(nsColor: Palette.card)))
            .overlay(Capsule().stroke(Color(nsColor: Palette.ink), lineWidth: 3))
            .contentShape(Capsule())
        }
        .buttonStyle(.plain)
        .padding(24)
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
