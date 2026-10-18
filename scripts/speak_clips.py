#!/usr/bin/env python3
"""Record narrator clips with Chatterbox (MIT licence), copying the original Bip Island Narrator.

The model copies the voice from two of her own game clips (REFERENCE) and speaks each clip's text
from Content/asset_manifest.json or audio/script.csv. Each clip is trimmed, levelled to the game's
loudness and saved as Resources/Audio/<clip>.m4a (44.1 kHz mono AAC). A fixed seed and the same
reference keep the voice the same on every run. Runs on CPU; the "Narrator" GitHub Actions workflow
runs it (huggingface.co, where the model lives, is blocked in cloud sessions).

  python3 scripts/speak_clips.py 'vo_say_*' num_7_mid          # clips not recorded yet
  python3 scripts/speak_clips.py vo_and --redo                  # record again
  python3 scripts/speak_clips.py --from voice-lab/request.txt --shard 1/4 --verify

Every new clip is checked by ear-by-machine: Whisper (speech recognition) transcribes it and the
words must match the text; a clip that doesn't match (a repeated or missing word) is recorded again
with the next seed, up to TRIES times. --verify also checks clips that already exist.
"""
import argparse, csv, fnmatch, json, re, subprocess, tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
AUDIO = ROOT / "Resources" / "Audio"
# The original narrator's own lines (recorded before 11 October 2026), about 16 seconds together.
REFERENCE = ["vo_welcome", "vo_bip_charging"]
SEED = 0
TRIES = 5  # seeds SEED, SEED+1, ... until Whisper hears the right words
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


NUMBER_WORDS = ("zero one two three four five six seven eight nine ten eleven twelve thirteen fourteen "
                "fifteen sixteen seventeen eighteen nineteen twenty").split()


def words(text):
    """The words of a line, lower case, numbers as words ("5" -> "five"), punctuation dropped."""
    out = []
    for w in re.findall(r"[a-z0-9']+", text.lower()):
        out.append(NUMBER_WORDS[int(w)] if w.isdigit() and int(w) <= 20 else w.strip("'"))
    return [w for w in out if w]


class Listener:
    """Whisper (faster-whisper, base.en) hears a clip; says whether it holds exactly the expected words."""
    def __init__(self):
        from faster_whisper import WhisperModel
        self.model = WhisperModel("base.en", device="cpu", compute_type="int8")

    def heard(self, path):
        segments, _ = self.model.transcribe(str(path), language="en", beam_size=5)
        return " ".join(seg.text for seg in segments).strip()

    def ok(self, path, text):
        return words(self.heard(path)) == words(text)


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
    ap.add_argument("--verify", action="store_true", help="also check clips that already exist; re-record any that are wrong")
    ap.add_argument("--report", help="write the clips that never came out right here, one a line")
    ap.add_argument("--shard", default="1/1", help="k/n: do every n-th clip starting at k (for parallel jobs)")
    a = ap.parse_args()
    patterns = list(a.clips)
    if a.from_file:
        patterns += [l.split("#")[0].strip() for l in Path(a.from_file).read_text().splitlines() if l.split("#")[0].strip()]
    all_texts = texts()
    clips = wanted(patterns, all_texts)
    k, n = (int(x) for x in a.shard.split("/"))
    clips = clips[k - 1::n]
    listener = Listener()
    todo = []
    for c in clips:
        path = AUDIO / f"{c}.m4a"
        if a.redo or not path.exists():
            todo.append(c)
        elif a.verify and not listener.ok(path, all_texts[c]):
            print(f"Wrong words in {c}: heard {listener.heard(path)!r}, expected {all_texts[c]!r}", flush=True)
            todo.append(c)
    print(f"Speaking {len(todo)} clips (shard {a.shard}).", flush=True)
    failed = []
    if a.report: Path(a.report).parent.mkdir(parents=True, exist_ok=True); Path(a.report).write_text("")
    if not todo: return

    import torch, torchaudio as ta
    from chatterbox.tts import ChatterboxTTS
    model = ChatterboxTTS.from_pretrained(device="cuda" if torch.cuda.is_available() else "cpu")
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        ref = reference_wav(tmp)
        for i, clip in enumerate(todo, 1):
            text, out = all_texts[clip], AUDIO / f"{clip}.m4a"
            for seed in range(SEED, SEED + TRIES):
                torch.manual_seed(seed)
                wav = model.generate(text, audio_prompt_path=str(ref), **SETTINGS)
                raw, trimmed = tmp / "raw.wav", tmp / "trimmed.wav"
                ta.save(str(raw), wav, model.sr)
                ffmpeg("-i", str(raw), "-af", TRIM, "-ar", "44100", "-ac", "1", str(trimmed))
                gain = TARGET_LUFS - loudness(trimmed)
                ffmpeg("-i", str(trimmed), "-af", f"volume={gain:.2f}dB,alimiter=limit=0.9", "-ar", "44100", "-ac", "1",
                       "-c:a", "aac", "-b:a", "96k", str(out))
                if listener.ok(out, text): break
                print(f"  seed {seed}: heard {listener.heard(out)!r}; trying again", flush=True)
            else:
                # No good take: remove it, so the game joins the line from its pieces instead.
                out.unlink(missing_ok=True)
                failed.append(clip)
                print(f"  FAILED {clip}: no take matched after {TRIES} tries; removed", flush=True)
            print(f"[{i}/{len(todo)}] {clip}: {text}", flush=True)
    if a.report:
        Path(a.report).write_text("".join(f"{c}\n" for c in failed))
    print(f"Done: {len(todo) - len(failed)} recorded, {len(failed)} removed ({', '.join(failed) or 'none'}).", flush=True)


if __name__ == "__main__":
    main()
