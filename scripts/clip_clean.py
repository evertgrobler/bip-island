"""Cleans a narrator clip (owner, 3 and 5 November 2026: static in some clips, and noise at the end of some).

Two things go wrong with Gemini's clips:
- a hiss under the voice, loudest in the pauses, and small clicks: CLEAN takes them out (a declicker, an
  FFT denoiser that tracks the noise, and a gentle gate that turns the pauses down);
- a burst at the very start or end: when a line is cut out of a batch, the cut can take in a split second
  of the next line or of noise. drop_edge_bursts removes a short, abrupt sound at either edge that is
  cut off from the rest of the clip by a pause.

Used by gemini_narrator.py on every new clip, and on its own over clips already in the game:

  python3 scripts/clip_clean.py Resources/Audio/af/num_7.m4a ...   # cleans these files in place
"""
import subprocess, sys, tempfile
from pathlib import Path

import numpy as np

RATE = 44100
CLEAN = ("adeclick=w=20:o=75,afftdn=nr=18:nf=-55:tn=1,"
         "agate=threshold=0.008:ratio=4:attack=5:release=120:range=0.05")
# Soft ends, so a clip never starts or stops with a click.
FADES = "afade=t=in:d=0.01,areverse,afade=t=in:d=0.02,areverse"

FRAME = 0.01        # seconds per loudness frame
LOUD_EDGE = -30     # dB below full scale: an edge this loud was cut mid-sound
QUIET = -45         # a pause is quieter than this
MIN_GAP = 0.08      # seconds of pause between the burst and the clip
MAX_BURST = 0.25    # seconds: anything longer is part of the line
NOISY = 0.3         # spectral flatness: Gemini's bursts measure about 0.4 to 0.5, a spoken word under 0.25


def flatness(seg):
    """How noise-like a stretch of sound is (0 = a clear tone or voice, 1 = pure hiss), 100 Hz to 8 kHz."""
    n = 1024
    if len(seg) < n:
        seg = np.pad(seg, (0, n - len(seg)))
    frames = seg[: len(seg) // n * n].reshape(-1, n)
    spec = np.abs(np.fft.rfft(frames * np.hanning(n), axis=1)) ** 2 + 1e-12
    f = np.fft.rfftfreq(n, 1 / RATE)
    spec = spec[:, (f > 100) & (f < 8000)]
    return float(np.mean(np.exp(np.log(spec).mean(1)) / spec.mean(1)))


def read(path):
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", str(path), "-ac", "1", "-ar", str(RATE), "-f", "f32le", "-"],
                         capture_output=True, check=True).stdout
    return np.frombuffer(raw, np.float32).copy()


def write(samples, path):
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "f32le", "-ar", str(RATE), "-ac", "1", "-i", "-", str(path)],
                   input=samples.astype(np.float32).tobytes(), check=True)


def edge_bursts(x, heads=True):
    """(start, end) sample range of the clip without a burst at its end (and its start, with heads)."""
    n = int(RATE * FRAME)
    frames = len(x) // n
    if frames < 10:
        return 0, len(x)
    db = 20 * np.log10(np.sqrt((x[: frames * n].reshape(frames, n) ** 2).mean(1)) + 1e-9)
    start, end = 0, frames

    def burst(order):
        # Walk in from the edge: an abrupt loud edge, at most MAX_BURST long, then a pause of MIN_GAP.
        idx = list(order)
        # The very last frame or two can be quiet (a partial frame, the encoder's own fade): look past them.
        if max(db[k] for k in idx[:3]) < LOUD_EDGE:
            return None
        i = 0
        while i < 2 and db[idx[i]] < QUIET:
            i += 1
        while i < len(idx) and db[idx[i]] >= QUIET:
            i += 1
        if i * FRAME > MAX_BURST or i == 0:
            return None
        j = i
        while j < len(idx) and db[idx[j]] < QUIET:
            j += 1
        if (j - i) * FRAME < MIN_GAP or j >= len(idx):
            return None
        # A real short word ("fiets", "or") is voiced; only a noise-like burst goes.
        burst_frames = sorted(idx[:i])
        if flatness(x[burst_frames[0] * n:(burst_frames[-1] + 1) * n]) < NOISY:
            return None
        return i + (j - i) // 2  # cut halfway into the pause

    head = burst(range(frames)) if heads else None
    if head is not None:
        start = head
    tail = burst(range(frames - 1, -1, -1))
    if tail is not None:
        end = frames - tail
    if end - start < 10:
        return 0, len(x)
    return start * n, min(len(x), end * n)


def drop_edge_bursts(wav, heads=True):
    """Removes a burst at the edges of a .wav file, in place. Returns True when it cut something."""
    x = read(wav)
    a, b = edge_bursts(x, heads)
    if (a, b) == (0, len(x)):
        return False
    write(x[a:b], wav)
    return True


def clean_file(src, dst, denoise=True, heads=True):
    """A cleaned copy of a clip: edge bursts dropped, then the hiss and clicks (unless denoise=False), soft ends."""
    with tempfile.TemporaryDirectory() as tmp:
        wav = Path(tmp) / "clip.wav"
        write(read(src), wav)
        drop_edge_bursts(wav, heads)
        chain = (CLEAN + "," if denoise else "") + FADES
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(wav), "-af", chain, "-ar", str(RATE), "-ac", "1",
                        "-c:a", "aac", "-b:a", "128k", str(dst)], check=True)


if __name__ == "__main__":
    for name in sys.argv[1:]:
        path = Path(name)
        # Letter sounds keep their hiss ("s", "f"): only the edges and fades for them.
        # A clip already in the game passed Whisper as it is, and a short first word ("car", "Een,") can look
        # like a burst at its start, so only the end is cut here; new clips are heard again after cleaning.
        clean_file(path, path.with_suffix(".tmp.m4a"), denoise=not path.name.startswith("snd_"), heads=False)
        path.with_suffix(".tmp.m4a").replace(path)
        print("cleaned", path)
