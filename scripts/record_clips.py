#!/usr/bin/env python3
"""Record narrator clips straight from the ElevenLabs API, with no connector pop-ups.

Reads the key from the ELEVENLABS_API_KEY environment variable (set it in the cloud
environment's settings; never commit it or paste it into a chat). Speaks each clip's text
from Content/asset_manifest.json (or audio/script.csv) in the Bip Island Narrator voice with
eleven_v3, trims the silence and saves Resources/Audio/<clip>.m4a (44.1 kHz mono AAC).

  python3 scripts/record_clips.py --prefix vo_say_           # every vo_say_ clip not yet recorded
  python3 scripts/record_clips.py num_7 vo_and --redo        # these clips, even if recorded

Free plans allow only a few requests at a time, so clips go one by one and a "429 too
many requests" waits and tries again.
"""
import argparse, csv, json, os, subprocess, sys, tempfile, time, urllib.error, urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
AUDIO = ROOT / "Resources" / "Audio"
VOICE_ID = "P7kHKZAtnEEhLBZBK8pP"  # "Bip Island Narrator" (CLAUDE.md)
MODEL = "eleven_v3"
TRIM = ("silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.05,areverse,"
        "silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.08,areverse")


def texts():
    """Every clip's text: the content manifest plus the voice script."""
    found = {a["id"]: a["text"] for a in json.load(open(ROOT / "Content" / "asset_manifest.json"))["audio"]}
    with open(ROOT / "audio" / "script.csv", newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            found[row["file"].removesuffix(".m4a")] = row["text"]
    return found


def speak(key, text):
    body = json.dumps({"text": text, "model_id": MODEL}).encode()
    req = urllib.request.Request(
        f"https://api.elevenlabs.io/v1/text-to-speech/{VOICE_ID}?output_format=mp3_44100_128",
        data=body, headers={"xi-api-key": key, "Content-Type": "application/json"})
    for attempt in range(8):
        try:
            with urllib.request.urlopen(req, timeout=120) as r:
                return r.read()
        except urllib.error.HTTPError as e:
            if e.code in (429, 500, 502, 503) and attempt < 7:
                time.sleep(min(60, 5 * 2 ** attempt))
                continue
            raise SystemExit(f"ElevenLabs said {e.code}: {e.read().decode(errors='replace')[:300]}")


def save(clip, mp3):
    with tempfile.NamedTemporaryFile(suffix=".mp3") as tmp:
        tmp.write(mp3); tmp.flush()
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", tmp.name, "-af", TRIM, "-ar", "44100", "-ac", "1",
                        "-c:a", "aac", "-b:a", "96k", str(AUDIO / f"{clip}.m4a")], check=True)


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("clips", nargs="*", help="clip names (no .m4a)")
    p.add_argument("--prefix", help="every clip whose name starts with this")
    p.add_argument("--redo", action="store_true", help="record again even if the file exists")
    a = p.parse_args()
    key = os.environ.get("ELEVENLABS_API_KEY")
    if not key: raise SystemExit("Set ELEVENLABS_API_KEY in the environment first.")
    all_texts = texts()
    wanted = list(a.clips) + sorted(c for c in all_texts if a.prefix and c.startswith(a.prefix))
    missing = [c for c in wanted if c not in all_texts]
    if missing: raise SystemExit(f"No text for: {', '.join(missing)}")
    todo = [c for c in dict.fromkeys(wanted) if a.redo or not (AUDIO / f"{c}.m4a").exists()]
    print(f"Recording {len(todo)} clips ({sum(len(all_texts[c]) for c in todo)} characters).")
    for i, clip in enumerate(todo, 1):
        save(clip, speak(key, all_texts[clip]))
        print(f"[{i}/{len(todo)}] {clip}: {all_texts[clip]}", flush=True)


if __name__ == "__main__":
    main()
