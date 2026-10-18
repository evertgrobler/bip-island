#!/usr/bin/env python3
"""Voice lab: speak voice-lab/lines.tsv with Chatterbox, copying the narrator from reference game clips.

Writes voice-lab/out/<model>/<name>.mp3. Runs on CPU (GitHub Actions); a fixed seed keeps runs repeatable.
"""
import subprocess, sys, tempfile
from pathlib import Path

import torch
import torchaudio as ta

ROOT = Path(__file__).resolve().parent.parent
AUDIO = ROOT / "Resources" / "Audio"
OUT = ROOT / "voice-lab" / "out"
# The original narrator's own lines (recorded before 11 October), about 16 seconds together.
REFERENCE = ["vo_welcome", "vo_bip_charging"]


def reference_wav(folder):
    parts = []
    for name in REFERENCE:
        wav = folder / f"{name}.wav"
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(AUDIO / f"{name}.m4a"), "-ar", "24000", "-ac", "1", str(wav)], check=True)
        parts.append(f"file '{wav}'")
    listing = folder / "list.txt"
    listing.write_text("\n".join(parts) + "\n")
    joined = folder / "reference.wav"
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "concat", "-safe", "0", "-i", str(listing), str(joined)], check=True)
    return joined


def lines():
    for row in (ROOT / "voice-lab" / "lines.tsv").read_text(encoding="utf-8").splitlines():
        if row.strip() and not row.startswith("#"):
            name, text = row.split("\t", 1)
            yield name, text


def save_mp3(wav, sr, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(suffix=".wav") as tmp:
        ta.save(tmp.name, wav, sr)
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", tmp.name, "-ac", "1", "-b:a", "160k", str(path)], check=True)


def models():
    from chatterbox.tts import ChatterboxTTS
    yield "chatterbox", ChatterboxTTS.from_pretrained(device="cpu"), {"exaggeration": 0.5, "cfg_weight": 0.5}
    try:
        from chatterbox.tts_turbo import ChatterboxTurboTTS
    except ImportError as e:
        print(f"No Turbo model in this version ({e}); skipping it.")
        return
    yield "chatterbox_turbo", ChatterboxTurboTTS.from_pretrained(device="cpu"), {}


def main():
    with tempfile.TemporaryDirectory() as tmp:
        ref = reference_wav(Path(tmp))
        save_mp3(*ta.load(str(ref)), OUT / "reference_original_narrator.mp3")
        for label, model, options in models():
            for name, text in lines():
                torch.manual_seed(0)
                wav = model.generate(text, audio_prompt_path=str(ref), **options)
                save_mp3(wav, model.sr, OUT / label / f"{name}.mp3")
                print(f"{label}: {name}: {text}", flush=True)
        # The same lines as the game has them now, for comparison.
        for name, _ in lines():
            src = AUDIO / f"{name}.m4a"
            if src.exists():
                (OUT / "game_now").mkdir(parents=True, exist_ok=True)
                subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(src), "-b:a", "160k", str(OUT / "game_now" / f"{name}.mp3")], check=True)


if __name__ == "__main__":
    sys.exit(main())
