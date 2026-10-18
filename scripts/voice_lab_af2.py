#!/usr/bin/env python3
"""Voice lab, Afrikaans round 3 (free tools only; a test, nothing ships).

Round 2 turned Adri's Afrikaans into the narrator with Chatterbox's converter, which is built mainly for
English. Here two converters made to work in any language do it instead:
1. seedvc: Seed-VC (GPL-3.0, zero-shot voice conversion).
2. openvoice: OpenVoice V2's tone colour converter (MIT).
Source: Microsoft's Afrikaans voice Adri (edge-tts, test only). Target: the narrator's own game clips.
Writes voice-lab/out_af2/<method>/<name>.mp3.
"""
import asyncio, subprocess, sys, tempfile, traceback
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from voice_lab import reference_wav  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "voice-lab" / "out_af2"


def lines():
    for row in (ROOT / "voice-lab" / "lines_af.tsv").read_text(encoding="utf-8").splitlines():
        if row.strip() and not row.startswith("#"):
            yield row.split("\t", 1)


def mp3(src, dest):
    dest.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(src), "-ac", "1", "-b:a", "160k", str(dest)], check=True)


def adri_clips(tmp):
    import edge_tts
    clips = {}
    for name, text in lines():
        m = tmp / f"{name}_adri.mp3"
        asyncio.run(edge_tts.Communicate(text, "af-ZA-AdriNeural").save(str(m)))
        wav = tmp / f"{name}_adri.wav"
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(m), "-ar", "22050", "-ac", "1", str(wav)], check=True)
        mp3(wav, OUT / "adri" / f"{name}.mp3")
        clips[name] = wav
    return clips


def seedvc(clips, ref, tmp):
    repo = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT.parent / "seed-vc"
    for name, wav in clips.items():
        out = tmp / f"seedvc_{name}"
        out.mkdir(exist_ok=True)
        subprocess.run([sys.executable, "inference.py", "--source", str(wav), "--target", str(ref), "--output", str(out),
                        "--diffusion-steps", "30", "--length-adjust", "1.0", "--inference-cfg-rate", "0.7",
                        "--f0-condition", "False", "--auto-f0-adjust", "False", "--semi-tone-shift", "0", "--fp16", "False"],
                       cwd=repo, check=True)
        result = sorted(out.glob("*.wav"))[-1]
        mp3(result, OUT / "seedvc" / f"{name}.mp3")
        print(f"seedvc: {name}", flush=True)


def openvoice(clips, ref, tmp):
    import torch
    from huggingface_hub import snapshot_download
    from openvoice import se_extractor
    from openvoice.api import ToneColorConverter
    ckpt = Path(snapshot_download("myshell-ai/OpenVoiceV2")) / "converter"
    conv = ToneColorConverter(str(ckpt / "config.json"), device="cpu")
    conv.load_ckpt(str(ckpt / "checkpoint.pth"))
    target_se, _ = se_extractor.get_se(str(ref), conv, vad=False)
    for name, wav in clips.items():
        source_se, _ = se_extractor.get_se(str(wav), conv, vad=False)
        out = tmp / f"openvoice_{name}.wav"
        conv.convert(audio_src_path=str(wav), src_se=source_se, tgt_se=target_se, output_path=str(out))
        mp3(out, OUT / "openvoice" / f"{name}.mp3")
        print(f"openvoice: {name}", flush=True)


def main():
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        ref = reference_wav(tmp)
        clips = adri_clips(tmp)
        for method in (lambda: seedvc(clips, ref, tmp), lambda: openvoice(clips, ref, tmp)):
            try:
                method()
            except Exception:
                traceback.print_exc()


if __name__ == "__main__":
    main()
