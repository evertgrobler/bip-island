import BipCore
import SwiftUI

/// The parent area behind the gate: each child's progress, the children on this Mac, and settings.
struct ParentArea: View {
    @ObservedObject var gate: ParentGateModel
    @ObservedObject var coordinator: GameCoordinator

    enum Tab: String, CaseIterable, Identifiable {
        case progress = "Progress"
        case children = "Children"
        case settings = "Settings"
        var id: String { rawValue }
    }

    @State private var tab: Tab = .progress

    var body: some View {
        VStack(spacing: 14) {
            HStack {
                Text("Parent area")
                    .font(Fonts.ui(30, bold: true))
                Spacer()
                Button("Back to the game") { gate.close() }
                    .keyboardShortcut(.defaultAction)
                    .controlSize(.large)
            }
            Picker("Show", selection: $tab) {
                ForEach(Tab.allCases) { Text($0.rawValue).tag($0) }
            }
            .pickerStyle(.segmented)
            .labelsHidden()

            ScrollView {
                Group {
                    switch tab {
                    case .progress: ProgressTab(coordinator: coordinator)
                    case .children: ChildrenTab(coordinator: coordinator)
                    case .settings: SettingsTab(gate: gate)
                    }
                }
                .frame(maxWidth: .infinity, alignment: .leading)
                .padding(.vertical, 6)
            }
        }
        .frame(height: 640)
    }
}

// MARK: - Progress

struct ProgressTab: View {
    @ObservedObject var coordinator: GameCoordinator
    @State private var selected: UUID?

    private var childID: UUID? { selected ?? coordinator.childID ?? coordinator.children.first?.id }

    var body: some View {
        VStack(alignment: .leading, spacing: 18) {
            if coordinator.children.count > 1 {
                Picker("Child", selection: Binding(get: { childID }, set: { selected = $0 })) {
                    ForEach(coordinator.children) { child in
                        Text("\(Avatars.emoji[child.avatar] ?? "") \(child.name)").tag(Optional(child.id))
                    }
                }
                .pickerStyle(.segmented)
                .labelsHidden()
            }
            if let id = childID, let report = coordinator.report(for: id) {
                ReportView(report: report)
            } else {
                Text("Progress isn't available: the game content didn't load.")
            }
        }
    }
}

struct ReportView: View {
    let report: ProgressReport

    var body: some View {
        VStack(alignment: .leading, spacing: 20) {
            HStack(spacing: 12) {
                StatTile(value: "\(report.stars)", label: "stars")
                StatTile(value: "\(report.stickersEarned) of \(report.stickersTotal)", label: "stickers")
                StatTile(value: "\(report.minutesToday) min", label: "played today")
                StatTile(value: "\(report.minutesThisWeek) min", label: "this week, \(report.daysPlayedThisWeek) of 7 days")
                StatTile(value: weekAnswers, label: "right this week")
            }

            if !report.needsPractice.isEmpty {
                ParentSection(title: "Could use more practice") {
                    ForEach(report.needsPractice) { line in
                        SkillRow(line: line)
                    }
                }
            }
            if !report.dueForReview.isEmpty {
                ParentSection(title: "Due for review (Bip will suggest these)") {
                    ForEach(report.dueForReview) { line in
                        SkillRow(line: line)
                    }
                }
            }

            ParentSection(title: "Letters and sounds") {
                SoundLegend()
                ForEach(report.phonics) { group in
                    HStack(alignment: .center, spacing: 10) {
                        Text("Group \(group.number)")
                            .font(Fonts.ui(15, bold: true))
                            .frame(width: 70, alignment: .leading)
                        HStack(spacing: 5) {
                            ForEach(group.sounds) { sound in
                                SoundChip(sound: sound)
                            }
                        }
                        Spacer(minLength: 8)
                        StatusBadge(status: group.status)
                    }
                }
            }

            ForEach(report.islands) { island in
                ParentSection(title: "\(Self.islandName(island.island)) island: \(island.doneCount) of \(island.skills.count) skills done") {
                    ForEach(island.skills) { line in
                        SkillRow(line: line)
                    }
                }
            }

            ParentSection(title: "Recently played") {
                Text(report.recentGames.isEmpty ? "Nothing yet." : report.recentGames.joined(separator: ", "))
            }
        }
    }

    private var weekAnswers: String {
        guard report.answersThisWeek > 0 else { return "–" }
        let percent = Int((Double(report.rightThisWeek) * 100 / Double(report.answersThisWeek)).rounded())
        return "\(report.rightThisWeek) of \(report.answersThisWeek) (\(percent)%)"
    }

    static func islandName(_ island: Island) -> String {
        switch island {
        case .letters: return "Letters"
        case .numbers: return "Numbers"
        case .words: return "Words"
        case .coding: return "Coding"
        }
    }
}

struct ParentSection<Content: View>: View {
    let title: String
    @ViewBuilder let content: Content

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(title)
                .font(Fonts.ui(20, bold: true))
            content
        }
        .padding(14)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(RoundedRectangle(cornerRadius: 14).fill(Color(nsColor: Palette.card)))
        .overlay(RoundedRectangle(cornerRadius: 14).stroke(Color(nsColor: Palette.ink).opacity(0.25), lineWidth: 1.5))
    }
}

struct StatTile: View {
    let value: String
    let label: String

    var body: some View {
        VStack(spacing: 4) {
            Text(value)
                .font(Fonts.ui(22, bold: true))
                .lineLimit(1)
                .minimumScaleFactor(0.6)
            Text(label)
                .font(Fonts.ui(13))
                .multilineTextAlignment(.center)
                .foregroundStyle(.secondary)
        }
        .padding(10)
        .frame(maxWidth: .infinity, minHeight: 78)
        .background(RoundedRectangle(cornerRadius: 12).fill(Color(nsColor: Palette.sun).opacity(0.35)))
    }
}

struct SkillRow: View {
    let line: ProgressReport.SkillLine

    var body: some View {
        HStack(spacing: 10) {
            Text(line.name)
                .lineLimit(1)
            Spacer(minLength: 8)
            if let percent = line.recentPercent {
                Text("\(line.recentRight) of last \(line.recentTotal) right (\(percent)%)")
                    .font(Fonts.ui(13))
                    .foregroundStyle(.secondary)
            }
            if let day = line.masteredOnDay {
                Text("since \(DayNumber.date(for: day).formatted(.dateTime.day().month(.abbreviated)))")
                    .font(Fonts.ui(13))
                    .foregroundStyle(.secondary)
            }
            StatusBadge(status: line.status)
        }
        .font(Fonts.ui(15))
    }
}

struct StatusBadge: View {
    let status: ProgressReport.SkillStatus

    var body: some View {
        Text(Self.text(status))
            .font(Fonts.ui(12, bold: true))
            .padding(.horizontal, 8)
            .padding(.vertical, 3)
            .background(Capsule().fill(Self.colour(status).opacity(0.45)))
            .frame(width: 150, alignment: .trailing)
    }

    static func text(_ status: ProgressReport.SkillStatus) -> String {
        switch status {
        case .locked: return "Not open yet"
        case .knownByAge: return "Known for age"
        case .ready: return "Ready to start"
        case .learning: return "Practising"
        case .mastered: return "Mastered"
        case .reviewDue: return "Review due"
        }
    }

    static func colour(_ status: ProgressReport.SkillStatus) -> Color {
        switch status {
        case .locked: return Color(nsColor: Palette.stone)
        case .knownByAge: return Color(nsColor: Palette.lightTeal)
        case .ready: return Color(nsColor: Palette.sea)
        case .learning: return Color(nsColor: Palette.sun)
        case .mastered: return Color(nsColor: Palette.grass)
        case .reviewDue: return Color(nsColor: Palette.orange)
        }
    }
}

/// One sound, coloured by stage: grey new, yellow met, orange recognises, green mastered.
struct SoundChip: View {
    let sound: ProgressReport.SoundLine

    var body: some View {
        Text(sound.grapheme)
            .font(Fonts.ui(15, bold: true))
            .frame(minWidth: 30, minHeight: 28)
            .padding(.horizontal, 4)
            .background(RoundedRectangle(cornerRadius: 7).fill(Self.colour(sound.stage)))
            .help(Self.text(sound.stage))
    }

    static func colour(_ stage: SoundStage) -> Color {
        switch stage {
        case .new: return Color(nsColor: Palette.stone).opacity(0.6)
        case .met: return Color(nsColor: Palette.sun).opacity(0.7)
        case .recognises: return Color(nsColor: Palette.orange).opacity(0.7)
        case .mastered: return Color(nsColor: Palette.grass)
        }
    }

    static func text(_ stage: SoundStage) -> String {
        switch stage {
        case .new: return "Not met yet"
        case .met: return "Met (Sound Hunt next)"
        case .recognises: return "Recognises it (Bubble Pop next)"
        case .mastered: return "Knows it"
        }
    }
}

struct SoundLegend: View {
    var body: some View {
        HStack(spacing: 14) {
            ForEach(SoundStage.allCases, id: \.self) { stage in
                HStack(spacing: 5) {
                    RoundedRectangle(cornerRadius: 4).fill(SoundChip.colour(stage)).frame(width: 16, height: 16)
                    Text(SoundChip.text(stage)).font(Fonts.ui(12))
                }
            }
        }
        .foregroundStyle(.secondary)
    }
}

// MARK: - Children

struct ChildrenTab: View {
    @ObservedObject var coordinator: GameCoordinator
    @State private var newName = ""
    @State private var newAge = 0
    @State private var toRemove: ChildSummary?
    @State private var addFailed = false

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text("Each child picks their animal when the game opens. Their age sets where they start; the game then adjusts to how they do.")
                .foregroundStyle(.secondary)

            ForEach(coordinator.children) { child in
                ChildRow(child: child, coordinator: coordinator, isPlaying: child.id == coordinator.childID,
                         canRemove: coordinator.children.count > 1) { toRemove = child }
            }

            if ProfileRules.canAdd(currentCount: coordinator.children.count) {
                ParentSection(title: "Add a child") {
                    HStack(spacing: 12) {
                        TextField("Name", text: $newName)
                            .textFieldStyle(.roundedBorder)
                            .frame(width: 240)
                            .onSubmit(add)
                        AgePicker(age: $newAge)
                        Button("Add", action: add)
                            .disabled(ProfileRules.cleanName(newName) == nil)
                    }
                    if addFailed {
                        Text("That child couldn't be added.").foregroundStyle(.red)
                    }
                }
            } else {
                Text("Four children is the most one Mac can have.")
                    .foregroundStyle(.secondary)
            }
        }
        .confirmationDialog("Remove \(toRemove?.name ?? "")?",
                            isPresented: Binding(get: { toRemove != nil }, set: { if !$0 { toRemove = nil } }),
                            presenting: toRemove) { child in
            Button("Remove \(child.name) and all their progress", role: .destructive) {
                coordinator.deleteChild(child.id)
            }
        } message: { _ in
            Text("Their stars, stickers and progress are deleted from this Mac. This can't be undone.")
        }
    }

    private func add() {
        addFailed = !coordinator.addChild(name: newName, age: newAge == 0 ? nil : newAge, avatar: nil)
        if !addFailed {
            newName = ""
            newAge = 0
        }
    }
}

struct ChildRow: View {
    let child: ChildSummary
    @ObservedObject var coordinator: GameCoordinator
    let isPlaying: Bool
    let canRemove: Bool
    let onRemove: () -> Void
    @State private var name: String

    init(child: ChildSummary, coordinator: GameCoordinator, isPlaying: Bool, canRemove: Bool, onRemove: @escaping () -> Void) {
        self.child = child
        _coordinator = ObservedObject(wrappedValue: coordinator)
        self.isPlaying = isPlaying
        self.canRemove = canRemove
        self.onRemove = onRemove
        _name = State(initialValue: child.name)
    }

    var body: some View {
        HStack(spacing: 12) {
            Menu {
                ForEach(ProfileRules.avatars, id: \.self) { animal in
                    Button("\(Avatars.emoji[animal] ?? "") \(animal.capitalized)") {
                        var changed = child
                        changed.avatar = animal
                        coordinator.updateChild(changed)
                    }
                }
            } label: {
                Text(Avatars.emoji[child.avatar] ?? "🦁").font(Fonts.ui(30))
            }
            .menuStyle(.borderlessButton)
            .frame(width: 60)
            .help("Change the picture")

            TextField("Name", text: $name)
                .textFieldStyle(.roundedBorder)
                .frame(width: 220)
                .onSubmit(saveName)
            Button("Save name", action: saveName)
                .disabled(name == child.name || ProfileRules.cleanName(name) == nil)

            AgePicker(age: Binding(get: { child.age ?? 0 }, set: { value in
                var changed = child
                changed.age = value == 0 ? nil : value
                coordinator.updateChild(changed)
            }))

            if isPlaying {
                Text("playing now").font(Fonts.ui(13)).foregroundStyle(.secondary)
            }
            Spacer()
            Button("Remove", role: .destructive, action: onRemove)
                .disabled(!canRemove)
        }
        .font(Fonts.ui(15))
    }

    private func saveName() {
        guard ProfileRules.cleanName(name) != nil else { return }
        var changed = child
        changed.name = name
        coordinator.updateChild(changed)
    }
}

/// Age, or "not set" (0). The age sets the starting band.
struct AgePicker: View {
    @Binding var age: Int

    var body: some View {
        Picker("Age", selection: $age) {
            Text("Age not set").tag(0)
            ForEach(Array(ProfileRules.ages), id: \.self) { value in
                Text("Age \(value)").tag(value)
            }
        }
        .labelsHidden()
        .frame(width: 130)
    }
}

// MARK: - Settings

struct SettingsTab: View {
    @ObservedObject var gate: ParentGateModel

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            if let settings = gate.playSettings {
                PlaySettingsView(settings: settings, gate: gate)
                    .frame(maxWidth: .infinity)
            }
            PasscodeSection(gate: gate)
            ParentSection(title: "This copy of Bip Island") {
                Text(gate.versionText)
                if let version = gate.updateReady {
                    Text("Version \(version) is ready to install.")
                        .font(Fonts.ui(17, bold: true))
                }
                Text(gate.voiceText)
                    .foregroundStyle(.secondary)
                HStack(spacing: 12) {
                    Button(gate.updateReady == nil ? "Check for updates now" : "Install the update") { gate.checkForUpdates() }
                        .disabled(!gate.updatesConfigured)
                    Button(role: .destructive) { gate.quit() } label: {
                        Text("Quit Bip Island")
                    }
                }
                .controlSize(.large)
                if !gate.updatesConfigured {
                    Text("Automatic updates aren't switched on in this build.")
                        .font(Fonts.ui(13))
                        .foregroundStyle(.secondary)
                }
            }
        }
    }
}

/// Set, change or remove the parent passcode. With one set, holding Esc asks for it instead of
/// a maths question.
struct PasscodeSection: View {
    @ObservedObject var gate: ParentGateModel
    @State private var editing = false
    @State private var first = ""
    @State private var second = ""
    @State private var problem: String?
    @State private var saved = false

    var body: some View {
        ParentSection(title: "Parent passcode") {
            Text(gate.hasPasscode
                 ? "A passcode is set. Holding Esc asks for it instead of a maths question (after 3 wrong tries it asks maths)."
                 : "Set a 4 to 8 digit passcode to use instead of the maths question when you hold Esc.")
                .foregroundStyle(.secondary)
            if editing {
                HStack(spacing: 12) {
                    SecureField("New passcode", text: $first)
                        .textFieldStyle(.roundedBorder)
                        .frame(width: 170)
                    SecureField("Type it again", text: $second)
                        .textFieldStyle(.roundedBorder)
                        .frame(width: 170)
                        .onSubmit(save)
                    Button("Save", action: save)
                    Button("Cancel") { reset() }
                }
                if let problem {
                    Text(problem).foregroundStyle(.red)
                }
            } else {
                HStack(spacing: 12) {
                    Button(gate.hasPasscode ? "Change passcode" : "Set a passcode") {
                        reset()
                        editing = true
                    }
                    if gate.hasPasscode {
                        Button("Remove passcode", role: .destructive) {
                            gate.removePasscode()
                            saved = false
                        }
                    }
                    if saved {
                        Text("Saved.").foregroundStyle(.secondary)
                    }
                }
            }
        }
    }

    private func save() {
        guard ParentPasscode.normalised(first) != nil else {
            problem = "Use 4 to 8 digits."
            return
        }
        guard ParentPasscode.normalised(first) == ParentPasscode.normalised(second) else {
            problem = "The two don't match. Try again."
            return
        }
        saved = gate.setPasscode(first)
        if saved { editing = false; first = ""; second = ""; problem = nil }
    }

    private func reset() {
        editing = false
        first = ""
        second = ""
        problem = nil
        saved = false
    }
}
