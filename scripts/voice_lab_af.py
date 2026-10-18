#!/usr/bin/env python3
"""Voice lab, Afrikaans: two ways to get the original narrator speaking Afrikaans (a test; nothing ships).

1. chatterbox_nl: Chatterbox Multilingual has no Afrikaans, so it reads the Afrikaans text as Dutch,
   its closest language, copying the narrator from her English game clips.
2. mms_vc: Meta's MMS Afrikaans voice (CC-BY-NC, fine for a family test) says the line, then
   Chatterbox's voice conversion turns that voice into the narrator's.
Writes voice-lab/out_af/<method>/<name>.mp3.
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


def chatterbox_nl(ref):
    from chatterbox.mtl_tts import ChatterboxMultilingualTTS
    model = ChatterboxMultilingualTTS.from_pretrained(device="cpu")
    for name, text in lines():
        torch.manual_seed(0)
        wav = model.generate(text, language_id="nl", audio_prompt_path=str(ref))
        save_mp3(wav, model.sr, OUT / "chatterbox_nl" / f"{name}.mp3")
        print(f"chatterbox_nl: {name}", flush=True)


def mms_vc(ref, tmp):
    from transformers import AutoTokenizer, VitsModel
    from chatterbox.vc import ChatterboxVC
    tts = VitsModel.from_pretrained("facebook/mms-tts-afr")
    tok = AutoTokenizer.from_pretrained("facebook/mms-tts-afr")
    vc = ChatterboxVC.from_pretrained(device="cpu")
    for name, text in lines():
        torch.manual_seed(0)
        with torch.no_grad():
            wave = tts(**tok(text, return_tensors="pt")).waveform
        plain = tmp / f"{name}_mms.wav"
        ta.save(str(plain), wave, tts.config.sampling_rate)
        save_mp3(wave, tts.config.sampling_rate, OUT / "mms_plain" / f"{name}.mp3")
        wav = vc.generate(audio=str(plain), target_voice_path=str(ref))
        save_mp3(wav, vc.sr, OUT / "mms_vc" / f"{name}.mp3")
        print(f"mms_vc: {name}", flush=True)


def main():
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        ref = reference_wav(tmp)
        for method in (lambda: chatterbox_nl(ref), lambda: mms_vc(ref, tmp)):
            try:
                method()
            except Exception:
                traceback.print_exc()


if __name__ == "__main__":
    main()
