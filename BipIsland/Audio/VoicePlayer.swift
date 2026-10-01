import AVFoundation
import Foundation

/// Plays narrator clips from Resources/Audio one after another ("Find the picture that starts with…" + "sss").
/// A clip that isn't in the build is replaced by a short silence, so the game never gets stuck.
final class VoicePlayer {
    private static let gapBetweenClips: TimeInterval = 0.12
    private static let silentStubLength: TimeInterval = 0.6

    private var player: AVAudioPlayer?
    private var pending: DispatchWorkItem?
    private var sequence = 0
    private let placeholders: Set<String>
    private let bundledClips: Set<String>

    init() {
        let audioURL = Bundle.main.resourceURL?.appendingPathComponent("Audio", isDirectory: true)
        let files = (audioURL.flatMap { try? FileManager.default.contentsOfDirectory(atPath: $0.path) }) ?? []
        bundledClips = Set(files.filter { $0.hasSuffix(".m4a") }.map { String($0.dropLast(4)) })

        var placeholderNames = Set<String>()
        if let listURL = audioURL?.appendingPathComponent("PLACEHOLDERS.txt"),
           let list = try? String(contentsOf: listURL, encoding: .utf8) {
            for line in list.split(separator: "\n") where !line.hasPrefix("#") {
                placeholderNames.insert(line.replacingOccurrences(of: ".m4a", with: "").trimmingCharacters(in: .whitespaces))
            }
        }
        placeholders = placeholderNames
    }

    /// For the parent area: how many clips are real narrator recordings.
    var statusText: String {
        let real = bundledClips.subtracting(placeholders).count
        if bundledClips.isEmpty { return "Voice clips: none in this build (silent)." }
        if placeholders.isEmpty { return "Voice clips: \(real) narrator recordings." }
        return "Voice clips: \(real) narrator recordings, \(placeholders.count) computer-voice placeholders."
    }

    /// Plays clips in order. Starting a new sequence stops the current one.
    /// `onClipStart` fires as each clip begins (to sync animations); `completion` after the last.
    func play(_ clips: [String], onClipStart: ((Int, String) -> Void)? = nil, completion: (() -> Void)? = nil) {
        stop()
        playClip(at: 0, of: clips, sequence: sequence, onClipStart: onClipStart, completion: completion)
    }

    func stop() {
        sequence += 1
        pending?.cancel()
        pending = nil
        player?.stop()
        player = nil
    }

    private func playClip(at index: Int, of clips: [String], sequence id: Int,
                          onClipStart: ((Int, String) -> Void)?, completion: (() -> Void)?) {
        guard id == sequence else { return }
        guard index < clips.count else {
            completion?()
            return
        }
        let name = clips[index]
        onClipStart?(index, name)

        var length = Self.silentStubLength
        if let url = Bundle.main.url(forResource: name, withExtension: "m4a", subdirectory: "Audio"),
           let clip = try? AVAudioPlayer(contentsOf: url) {
            clip.prepareToPlay()
            clip.play()
            player = clip
            length = clip.duration
        } else {
            NSLog("Bip Island: no clip %@ in this build, playing silence instead", name)
        }

        let next = DispatchWorkItem { [weak self] in
            self?.playClip(at: index + 1, of: clips, sequence: id, onClipStart: onClipStart, completion: completion)
        }
        pending = next
        DispatchQueue.main.asyncAfter(deadline: .now() + length + Self.gapBetweenClips, execute: next)
    }
}
