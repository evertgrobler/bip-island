import AVFoundation
import Foundation

/// Bip's robot noises and the game's sound effects, synthesised in code (no audio files needed).
final class BipSounds {
    enum Effect: CaseIterable {
        /// Bip's two-tone "bip-bip".
        case beep
        /// Bip whirring with excitement.
        case whirr
        /// A soft, low "boop" for a wrong answer. Never harsh.
        case boop
        /// A bubble popping.
        case pop
        /// A bright little chime for a right answer.
        case chime
        /// A tiny click for buttons.
        case tick
    }

    private let engine = AVAudioEngine()
    private let players: [AVAudioPlayerNode]
    private var nextPlayer = 0
    private let format: AVAudioFormat
    private var buffers: [Effect: AVAudioPCMBuffer] = [:]
    private static let sampleRate = 44_100.0

    init() {
        format = AVAudioFormat(standardFormatWithSampleRate: Self.sampleRate, channels: 1)!
        players = (0..<4).map { _ in AVAudioPlayerNode() }
        for player in players {
            engine.attach(player)
            engine.connect(player, to: engine.mainMixerNode, format: format)
        }
        engine.mainMixerNode.outputVolume = 0.8
        for effect in Effect.allCases {
            buffers[effect] = render(effect)
        }
        do {
            try engine.start()
        } catch {
            NSLog("Bip Island: sound effects unavailable: %@", error.localizedDescription)
        }
    }

    func play(_ effect: Effect) {
        guard let buffer = buffers[effect] else { return }
        if !engine.isRunning {
            try? engine.start()
        }
        guard engine.isRunning else { return }
        let player = players[nextPlayer]
        nextPlayer = (nextPlayer + 1) % players.count
        player.stop()
        player.scheduleBuffer(buffer, at: nil, options: [], completionHandler: nil)
        player.play()
    }

    // MARK: Synthesis

    private func render(_ effect: Effect) -> AVAudioPCMBuffer? {
        switch effect {
        case .beep:
            return synth(duration: 0.24) { t in
                if t < 0.09 { return (880, Self.envelope(t, length: 0.09), 0.35) }
                if t > 0.12 { return (1320, Self.envelope(t - 0.12, length: 0.12), 0.35) }
                return (880, 0, 0)
            }
        case .whirr:
            return synth(duration: 0.45) { t in
                let f = 300 + 900 * (t / 0.45) + 40 * sin(2 * Double.pi * 28 * t)
                return (f, Self.envelope(t, length: 0.45), 0.25)
            }
        case .boop:
            return synth(duration: 0.32, harmonics: 0) { t in
                (330 - 120 * (t / 0.32), Self.envelope(t, length: 0.32, attack: 0.02), 0)
            }
        case .pop:
            var noise = SystemRandomNumberGenerator()
            return synth(duration: 0.12, harmonics: 0, noise: { Double.random(in: -1...1, using: &noise) }) { t in
                (1100 - 6000 * t, Self.envelope(t, length: 0.12, attack: 0.002), 0)
            }
        case .chime:
            let notes: [Double] = [1046.5, 1318.5, 1568.0]
            return synthChord(duration: 0.6, notes: notes, spacing: 0.08)
        case .tick:
            return synth(duration: 0.05, harmonics: 0) { t in
                (1600, Self.envelope(t, length: 0.05, attack: 0.002), 0)
            }
        }
    }

    /// Smooth attack, gentle exponential-ish release, so nothing clicks or startles.
    private static func envelope(_ t: Double, length: Double, attack: Double = 0.008) -> Double {
        guard t >= 0, t <= length else { return 0 }
        let rise = min(t / attack, 1)
        let fall = pow(max(1 - t / length, 0), 1.6)
        return rise * fall
    }

    /// Renders a tone whose frequency/amplitude follow `shape(t) -> (frequency, amplitude, squareness)`.
    private func synth(duration: Double, harmonics: Double = 1, noise: (() -> Double)? = nil,
                       shape: (Double) -> (Double, Double, Double)) -> AVAudioPCMBuffer? {
        let frames = AVAudioFrameCount(duration * Self.sampleRate)
        guard let buffer = AVAudioPCMBuffer(pcmFormat: format, frameCapacity: frames),
              let data = buffer.floatChannelData?[0] else { return nil }
        buffer.frameLength = frames
        var phase = 0.0
        for i in 0..<Int(frames) {
            let t = Double(i) / Self.sampleRate
            let (frequency, amplitude, squareness) = shape(t)
            phase += 2 * Double.pi * frequency / Self.sampleRate
            var sample = sin(phase)
            if harmonics > 0 {
                sample += squareness * sin(3 * phase) / 3 + squareness * sin(5 * phase) / 5
            }
            if let noise {
                sample = 0.6 * sample + 0.4 * noise()
            }
            data[i] = Float(sample * amplitude * 0.5)
        }
        return buffer
    }

    private func synthChord(duration: Double, notes: [Double], spacing: Double) -> AVAudioPCMBuffer? {
        let frames = AVAudioFrameCount(duration * Self.sampleRate)
        guard let buffer = AVAudioPCMBuffer(pcmFormat: format, frameCapacity: frames),
              let data = buffer.floatChannelData?[0] else { return nil }
        buffer.frameLength = frames
        for i in 0..<Int(frames) {
            let t = Double(i) / Self.sampleRate
            var sample = 0.0
            for (n, frequency) in notes.enumerated() {
                let start = Double(n) * spacing
                guard t >= start else { continue }
                let local = t - start
                let bell = sin(2 * Double.pi * frequency * local) + 0.3 * sin(2 * Double.pi * frequency * 2.01 * local)
                sample += bell * Self.envelope(local, length: duration - start, attack: 0.004)
            }
            data[i] = Float(sample * 0.18)
        }
        return buffer
    }
}
