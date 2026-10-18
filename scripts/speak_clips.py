#!/usr/bin/env python3
"""Record narrator clips with Chatterbox (MIT licence), copying the original Bip Island Narrator.

The model copies the voice from two of her own game clips (REFERENCE) and speaks each clip's text
from Content/asset_manifest.json or audio/script.csv. Each clip is trimmed, levelled to the game's
loudness and saved as Resources/Audio/<clip>.m4a (44.1 kHz mono AAC). A fixed seed and the same
reference keep the voice the same on every run. Runs on CPU; the "Narrator" GitHub Actions workflow
runs it (huggingface.co, where the model lives, is blocked in cloud sessions).

  python3 scripts/speak_clips.py 'vo_say_*' num_7_mid          # clips not recorded yet
  python3 scripts/speak_clips.py vo_and --redo                  # record again
  python3 scripts/speak_clips.py --from voice-lab/request.txt --shard 1/4
"""
import argparse, csv, fnmatch, json, re, subprocess, tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
AUDIO = ROOT / "Resources" / "Audio"
# The original narrator's own lines (recorded before 11 October 2026), about 16 seconds together.
REFERENCE = ["vo_welcome", "vo_bip_charging"]
SEED = 0
SETTINGS = {"exaggeration": 0.5, "cfg_weight": 0.5}  # Chatterbox defaults; the owner picked these by ear
TARGET_LUFS = -14.5  # the game's existing narrator clips sit at about -14 to -15.5 LUFS
TRIM = ("silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.05,areverse,"
        "silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.08,areverse")


def texts():
    """Every clip's text: the content manifest plus the voice script."""
    found = {a["id"]: a["text"] for a in json.load(open(ROOT / "Content" / "asset_manifest.json"))["audio"]}
    with open(ROOT / "audio" / "script.csv", newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            found[row["file"].removesuffix(".m4a")] = row["text"]
    return found


def ffmpeg(*args):
    return subprocess.run(["ffmpeg", "-hide_banner", "-v", "error", "-y", *args], check=True)


def loudness(path):
    out = subprocess.run(["ffmpeg", "-hide_banner", "-i", str(path), "-af", "ebur128", "-f", "null", "-"],
                         capture_output=True, text=True).stderr
    found = re.findall(r"I:\s+(-?[\d.]+) LUFS", out)
    return float(found[-1]) if found else TARGET_LUFS


def reference_wav(folder):
    parts = []
    for name in REFERENCE:
        wav = folder / f"{name}.wav"
        ffmpeg("-i", str(AUDIO / f"{name}.m4a"), "-ar", "24000", "-ac", "1", str(wav))
        parts.append(f"file '{wav}'")
    (folder / "list.txt").write_text("\n".join(parts) + "\n")
    ffmpeg("-f", "concat", "-safe", "0", "-i", str(folder / "list.txt"), str(folder / "reference.wav"))
    return folder / "reference.wav"


def wanted(patterns, all_texts):
    names = []
    for p in patterns:
        hits = sorted(c for c in all_texts if fnmatch.fnmatchcase(c, p))
        if not hits: raise SystemExit(f"No clip matches {p!r}")
        names += hits
    return list(dict.fromkeys(names))


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("clips", nargs="*", help="clip names or patterns ('vo_say_*')")
    ap.add_argument("--from", dest="from_file", help="a file of clip names or patterns, one a line (# for notes)")
    ap.add_argument("--redo", action="store_true", help="record again even if the file exists")
    ap.add_argument("--shard", default="1/1", help="k/n: do every n-th clip starting at k (for parallel jobs)")
    a = ap.parse_args()
    patterns = list(a.clips)
    if a.from_file:
        patterns += [l.split("#")[0].strip() for l in Path(a.from_file).read_text().splitlines() if l.split("#")[0].strip()]
    all_texts = texts()
    todo = [c for c in wanted(patterns, all_texts) if a.redo or not (AUDIO / f"{c}.m4a").exists()]
    k, n = (int(x) for x in a.shard.split("/"))
    todo = todo[k - 1::n]
    print(f"Speaking {len(todo)} clips (shard {a.shard}).", flush=True)
    if not todo: return

    import torch, torchaudio as ta
    from chatterbox.tts import ChatterboxTTS
    model = ChatterboxTTS.from_pretrained(device="cuda" if torch.cuda.is_available() else "cpu")
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        ref = reference_wav(tmp)
        for i, clip in enumerate(todo, 1):
            torch.manual_seed(SEED)
            wav = model.generate(all_texts[clip], audio_prompt_path=str(ref), **SETTINGS)
            raw, trimmed = tmp / "raw.wav", tmp / "trimmed.wav"
            ta.save(str(raw), wav, model.sr)
            ffmpeg("-i", str(raw), "-af", TRIM, "-ar", "44100", "-ac", "1", str(trimmed))
            gain = TARGET_LUFS - loudness(trimmed)
            ffmpeg("-i", str(trimmed), "-af", f"volume={gain:.2f}dB,alimiter=limit=0.9", "-ar", "44100", "-ac", "1",
                   "-c:a", "aac", "-b:a", "96k", str(AUDIO / f"{clip}.m4a"))
            print(f"[{i}/{len(todo)}] {clip}: {all_texts[clip]}", flush=True)


if __name__ == "__main__":
    main()
