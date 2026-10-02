using System;
using System.Collections.Generic;
using Godot;

namespace BipIsland.Audio;

/// <summary>
/// Bip's robot noises and the game's sound effects, made in code (no audio files needed): a port of
/// the Swift app's BipSounds with the same notes and shapes. Played through a small pool of players so
/// quick taps overlap instead of cutting each other off.
/// </summary>
public partial class BipSounds : Node
{
    public enum Effect
    {
        /// <summary>Bip's two-tone "bip-bip".</summary>
        Beep,
        /// <summary>Bip whirring with excitement.</summary>
        Whirr,
        /// <summary>A soft, low "boop" for a wrong answer. Never harsh.</summary>
        Boop,
        /// <summary>A bubble popping.</summary>
        Pop,
        /// <summary>A bright little chime for a right answer.</summary>
        Chime,
        /// <summary>A tiny click for buttons.</summary>
        Tick,
    }

    private const int SampleRate = 44_100;
    private readonly Dictionary<Effect, AudioStreamWav> _streams = new();
    private readonly List<AudioStreamPlayer> _players = new();
    private int _next;

    public override void _Ready()
    {
        for (var i = 0; i < 4; i++)
        {
            var player = new AudioStreamPlayer { VolumeDb = Mathf.LinearToDb(0.8f) };
            AddChild(player);
            _players.Add(player);
        }
        foreach (var effect in Enum.GetValues<Effect>()) _streams[effect] = ToStream(Render(effect));
    }

    public void Play(Effect effect)
    {
        if (!_streams.TryGetValue(effect, out var stream)) return;
        var player = _players[_next];
        _next = (_next + 1) % _players.Count;
        player.Stop();
        player.Stream = stream;
        player.Play();
    }

    // Synthesis (shared with the tests through the static methods)

    public static float[] Render(Effect effect)
    {
        switch (effect)
        {
            case Effect.Beep:
                return Synth(0.24, t =>
                {
                    if (t < 0.09) return (880, Envelope(t, 0.09), 0.35);
                    if (t > 0.12) return (1320, Envelope(t - 0.12, 0.12), 0.35);
                    return (880, 0, 0);
                });
            case Effect.Whirr:
                return Synth(0.45, t => (300 + 900 * (t / 0.45) + 40 * Math.Sin(2 * Math.PI * 28 * t), Envelope(t, 0.45), 0.25));
            case Effect.Boop:
                return Synth(0.32, t => (330 - 120 * (t / 0.32), Envelope(t, 0.32, attack: 0.02), 0), harmonics: 0);
            case Effect.Pop:
                var noise = new Random(1);
                return Synth(0.12, t => (1100 - 6000 * t, Envelope(t, 0.12, attack: 0.002), 0), harmonics: 0,
                    noise: () => noise.NextDouble() * 2 - 1);
            case Effect.Chime:
                return SynthChord(0.6, [1046.5, 1318.5, 1568.0], 0.08);
            case Effect.Tick:
                return Synth(0.05, t => (1600, Envelope(t, 0.05, attack: 0.002), 0), harmonics: 0);
            default:
                return [];
        }
    }

    /// <summary>Smooth attack, gentle release, so nothing clicks or startles.</summary>
    private static double Envelope(double t, double length, double attack = 0.008)
    {
        if (t < 0 || t > length) return 0;
        var rise = Math.Min(t / attack, 1);
        var fall = Math.Pow(Math.Max(1 - t / length, 0), 1.6);
        return rise * fall;
    }

    /// <summary>A tone whose frequency, amplitude and "squareness" follow <paramref name="shape"/>.</summary>
    private static float[] Synth(double duration, Func<double, (double Frequency, double Amplitude, double Squareness)> shape,
                                 double harmonics = 1, Func<double>? noise = null)
    {
        var frames = (int)(duration * SampleRate);
        var data = new float[frames];
        var phase = 0.0;
        for (var i = 0; i < frames; i++)
        {
            var t = (double)i / SampleRate;
            var (frequency, amplitude, squareness) = shape(t);
            phase += 2 * Math.PI * frequency / SampleRate;
            var sample = Math.Sin(phase);
            if (harmonics > 0) sample += squareness * Math.Sin(3 * phase) / 3 + squareness * Math.Sin(5 * phase) / 5;
            if (noise != null) sample = 0.6 * sample + 0.4 * noise();
            data[i] = (float)(sample * amplitude * 0.5);
        }
        return data;
    }

    private static float[] SynthChord(double duration, double[] notes, double spacing)
    {
        var frames = (int)(duration * SampleRate);
        var data = new float[frames];
        for (var i = 0; i < frames; i++)
        {
            var t = (double)i / SampleRate;
            var sample = 0.0;
            for (var n = 0; n < notes.Length; n++)
            {
                var start = n * spacing;
                if (t < start) continue;
                var local = t - start;
                var bell = Math.Sin(2 * Math.PI * notes[n] * local) + 0.3 * Math.Sin(2 * Math.PI * notes[n] * 2.01 * local);
                sample += bell * Envelope(local, duration - start, attack: 0.004);
            }
            data[i] = (float)(sample * 0.18);
        }
        return data;
    }

    /// <summary>16-bit mono PCM, clipped so a loud sum can't wrap around into a crackle.</summary>
    private static AudioStreamWav ToStream(float[] samples)
    {
        var bytes = new byte[samples.Length * 2];
        for (var i = 0; i < samples.Length; i++)
        {
            var value = (short)Math.Clamp(samples[i] * short.MaxValue, short.MinValue, short.MaxValue);
            bytes[i * 2] = (byte)(value & 0xFF);
            bytes[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }
        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = SampleRate,
            Stereo = false,
            Data = bytes,
        };
    }
}
