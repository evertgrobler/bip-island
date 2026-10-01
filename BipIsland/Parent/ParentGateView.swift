import SwiftUI

/// The grown-ups-only panel. Children can't read it, and the maths keeps them out.
struct ParentGateView: View {
    @ObservedObject var gate: ParentGateModel
    @FocusState private var answerFocused: Bool

    var body: some View {
        ZStack {
            Color.black.opacity(0.55).ignoresSafeArea()

            VStack(spacing: 22) {
                switch gate.phase {
                case .question:
                    question
                case .unlocked:
                    parentArea
                case .closed:
                    EmptyView()
                }
            }
            .padding(40)
            .frame(width: 520)
            .background(
                RoundedRectangle(cornerRadius: 28, style: .continuous)
                    .fill(Color(nsColor: Palette.paper))
            )
            .overlay(
                RoundedRectangle(cornerRadius: 28, style: .continuous)
                    .stroke(Color(nsColor: Palette.ink), lineWidth: 4)
            )
            // The panel is always cream: pin it to light mode so text never
            // flips white-on-cream when the Mac is in Dark Mode.
            .colorScheme(.light)
            .foregroundStyle(Color(nsColor: Palette.ink))
        }
    }

    private var question: some View {
        VStack(spacing: 18) {
            Text("Grown-ups only")
                .font(.system(size: 30, weight: .bold, design: .rounded))
            Text("What is \(gate.challenge.question)?")
                .font(.system(size: 26, weight: .semibold, design: .rounded))
            TextField("Answer", text: $gate.answer)
                .textFieldStyle(.roundedBorder)
                .font(.system(size: 26, design: .rounded))
                .multilineTextAlignment(.center)
                .frame(width: 200)
                .focused($answerFocused)
                .onSubmit { gate.submitAnswer() }
            if gate.lastAnswerWasWrong {
                Text("Not quite. Try this one.")
                    .foregroundStyle(.red)
            }
            HStack(spacing: 16) {
                Button("Back to the game") { gate.close() }
                    .keyboardShortcut(.cancelAction)
                Button("Continue") { gate.submitAnswer() }
                    .keyboardShortcut(.defaultAction)
            }
            .controlSize(.large)
        }
        .onAppear { answerFocused = true }
    }

    private var parentArea: some View {
        VStack(spacing: 16) {
            Text("Parent area")
                .font(.system(size: 30, weight: .bold, design: .rounded))
            Text(gate.versionText)
                .foregroundStyle(.secondary)
            Text(gate.voiceText)
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.center)

            if let settings = gate.playSettings {
                PlaySettingsView(settings: settings, gate: gate)
            }

            VStack(spacing: 12) {
                Button("Back to the game") { gate.close() }
                    .keyboardShortcut(.defaultAction)
                Button("Check for updates now") { gate.checkForUpdates() }
                    .disabled(!gate.updatesConfigured)
                if !gate.updatesConfigured {
                    Text("Automatic updates aren't switched on in this build.")
                        .font(.footnote)
                        .foregroundStyle(.secondary)
                }
                Button(role: .destructive) { gate.quit() } label: {
                    Text("Quit Bip Island")
                }
            }
            .controlSize(.large)
            .padding(.top, 8)
        }
    }
}

/// Play length, break length and the daily maximum. Grown-ups only.
struct PlaySettingsView: View {
    @ObservedObject var settings: PlayTimeSettings
    @ObservedObject var gate: ParentGateModel
    @State private var dailyMaxOn: Bool = false

    var body: some View {
        VStack(spacing: 10) {
            Text("Play time")
                .font(.system(size: 22, weight: .bold, design: .rounded))
            Stepper("Play for \(settings.playMinutes) minutes", value: $settings.playMinutes, in: 5...120, step: 5)
            Stepper("Break for \(settings.breakMinutes) minutes", value: $settings.breakMinutes, in: 5...60, step: 5)
            Toggle("Daily maximum", isOn: $dailyMaxOn)
                .onChange(of: dailyMaxOn) { _, on in
                    settings.dailyMaxMinutes = on ? (settings.dailyMaxMinutes ?? 60) : nil
                }
            if dailyMaxOn {
                Stepper("At most \(settings.dailyMaxMinutes ?? 60) minutes a day",
                        value: Binding(get: { settings.dailyMaxMinutes ?? 60 },
                                       set: { settings.dailyMaxMinutes = $0 }),
                        in: 10...240, step: 10)
            }
            Button("End Bip's break now") { gate.endBreakEarly() }
        }
        .controlSize(.large)
        .onAppear { dailyMaxOn = settings.dailyMaxMinutes != nil }
    }
}

/// A small ring in the corner that fills while Esc is held.
struct HoldRing: View {
    let progress: Double

    var body: some View {
        Circle()
            .trim(from: 0, to: progress)
            .stroke(Color(nsColor: Palette.ink).opacity(0.6), style: StrokeStyle(lineWidth: 6, lineCap: .round))
            .rotationEffect(.degrees(-90))
            .frame(width: 44, height: 44)
            .padding(24)
    }
}
