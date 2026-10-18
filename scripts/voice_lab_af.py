#!/usr/bin/env python3
"""Voice lab, Afrikaans (a test; nothing ships). Round 1 (Chatterbox reading Afrikaans as Dutch)
sounded wrong to the owner. Round 2: real Afrikaans voices, each also turned into the narrator's voice
with Chatterbox voice conversion (it keeps the words and rhythm, swaps the voice):
1. adri: Microsoft's Afrikaans neural voice Adri.
2. simba: UBC-NLP's open Simba-TTS-afr.
Writes voice-lab/out_af/<method>[_as_narrator]/<name>.mp3.
"""
import subprocess, sys, tempfile, traceback
from pathlib import Path

import torch
import torchaudio as ta

sys.path.insert(0, str(Path(__file__).resolve().parent))
from voice_lab import reference_wav, save_mp3  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "voice-lab" / "out_af"


def lines():
    for row in (ROOT / "voice-lab" / "lines_af.tsv").read_text(encoding="utf-8").splitlines():
        if row.strip() and not row.startswith("#"):
            yield row.split("\t", 1)


def vc_model():
    from chatterbox.vc import ChatterboxVC
    return ChatterboxVC.from_pretrained(device="cpu")


def convert(vc, plain, ref, method, name):
    """Turn a clip spoken by another Afrikaans voice into the narrator's voice."""
    wav = vc.generate(audio=str(plain), target_voice_path=str(ref))
    save_mp3(wav, vc.sr, OUT / f"{method}_as_narrator" / f"{name}.mp3")


def simba(ref, tmp, vc):
    """UBC-NLP Simba-TTS-afr: a VITS model fine-tuned for Afrikaans (EMNLP 2025)."""
    from transformers import AutoTokenizer, VitsModel
    tts = VitsModel.from_pretrained("UBC-NLP/Simba-TTS-afr")
    tok = AutoTokenizer.from_pretrained("UBC-NLP/Simba-TTS-afr")
    for name, text in lines():
        torch.manual_seed(0)
        with torch.no_grad():
            wave = tts(**tok(text, return_tensors="pt")).waveform
        plain = tmp / f"{name}_simba.wav"
        ta.save(str(plain), wave, tts.config.sampling_rate)
        save_mp3(wave, tts.config.sampling_rate, OUT / "simba" / f"{name}.mp3")
        convert(vc, plain, ref, "simba", name)
        print(f"simba: {name}", flush=True)


def adri(ref, tmp, vc):
    """Microsoft's Afrikaans neural voice Adri (af-ZA-AdriNeural), through the free edge-tts reader.
    A test only: real use would go through an Azure Speech account (free tier 500,000 characters a month)."""
    import asyncio, edge_tts
    for name, text in lines():
        mp3 = tmp / f"{name}_adri.mp3"
        asyncio.run(edge_tts.Communicate(text, "af-ZA-AdriNeural").save(str(mp3)))
        plain = tmp / f"{name}_adri.wav"
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(mp3), "-ar", "24000", "-ac", "1", str(plain)], check=True)
        save_mp3(*ta.load(str(plain)), OUT / "adri" / f"{name}.mp3")
        convert(vc, plain, ref, "adri", name)
        print(f"adri: {name}", flush=True)


def main():
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        ref = reference_wav(tmp)
        vc = vc_model()
        for method in (lambda: adri(ref, tmp, vc), lambda: simba(ref, tmp, vc)):
            try:
                method()
            except Exception:
                traceback.print_exc()


if __name__ == "__main__":
    main()
