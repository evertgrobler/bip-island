#!/usr/bin/env python3
"""Record the game's narrator clips with Gemini's "Aoede" voice (owner decision, 18 October 2026).

Gemini's free tier allows only a few requests a day, so each request speaks a batch of lines with
pauses between them. Whisper (speech recognition) hears the batch with word timings, the batch is cut
where each line starts and ends, and every piece is heard again on its own before it replaces
Resources/Audio/<clip>.m4a. A piece that doesn't
match goes back in the queue and is recorded again on a later run, in a smaller batch.

Progress is kept in voice-lab/aoede.json (clip -> the text it was recorded with), so a run picks up
where the last one stopped, and a clip whose text changes is recorded again. A run stops cleanly when
Gemini says the daily limit is used up. The "Aoede narrator" workflow runs this; it needs the
GEMINI_API_KEY repository secret (never committed).

Letter sounds (snd_*) are recorded too, calm and clean, in batches cut at the pauses. Whisper can't
check a pure sound, so they don't go into the game: they wait in voice-lab/aoede_phonics/ for the owner
to listen to, and only the approved ones are copied over the old sounds.

  python3 scripts/gemini_narrator.py                # record what's left, up to MAX_REQUESTS requests
  python3 scripts/gemini_narrator.py --status       # how many clips are done, failed and left
"""
import argparse, base64, difflib, json, os, re, subprocess, sys, tempfile, urllib.error, urllib.request
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from speak_clips import AUDIO, ROOT, TARGET_LUFS, ffmpeg, loudness, texts, words  # noqa: E402

MODEL = "gemini-3.8-flash-tts"
VOICE = "Aoede"
STATE = ROOT / "voice-lab" / "aoede.json"
MAX_REQUESTS = int(os.environ.get("MAX_REQUESTS", "40"))
# Trim the ends of each piece: Gemini's quiet has a soft noise floor, so -40 dB counts as silence.
TRIM = ("silenceremove=start_periods=1:start_threshold=-40dB:start_silence=0.05,areverse,"
        "silenceremove=start_periods=1:start_threshold=-40dB:start_silence=0.08,areverse")
GIVE_UP = 4  # failed takes before a clip is listed for a person to look at

# Instructions, praise and sentences sparkle; single words and numbers are clear and friendly.
SPARKLE = ("Read these lines as a bubbly, sparkling, warm South African English primary school teacher who is "
           "delighted to see the child: a big smile in the voice, lively and playful, full of wonder and "
           "encouragement, but still clear and unhurried for a five-year-old. Natural South African English accent.")
WORDS = ("Read these words as a warm, friendly South African English primary school teacher showing a "
         "five-year-old each word: clear, bright and cheerful, saying each word once, exactly as written. "
         "Natural South African English accent.")
PHONICS = ("You are a calm, clear South African English phonics teacher. Each line below is one letter sound. "
           "Say only the pure sound, never the letter name, once, then stop: stretchy sounds (s, m, n, f, l, r, v, z, "
           "sh, th) are held for about a second; stop sounds (t, p, k, c, b, d, g) are short and crisp with no 'uh' "
           "after them; vowels are short unless the line says long. The sounds in order:")
PHONICS_DIR = ROOT / "voice-lab" / "aoede_phonics"
PAUSE = "After every line, stay silent for two full seconds before the next one. Never read anything twice."


def group(clip):
    """Which style a clip is spoken in, or None if it isn't recorded here."""
    if clip.startswith("snd_"):
        return "phonics"
    if clip.startswith(("word_", "num_", "money_", "name_")) and not clip.endswith("_mid"):
        return "words"
    return "sparkle"


def batch_size(style, retry):
    if style == "phonics":
        return 3 if retry else 8
    if retry:
        return 4
    return 36 if style == "words" else 18


def load_state():
    if STATE.exists():
        return json.loads(STATE.read_text())
    return {"done": {}, "failed": {}}


def save_state(state):
    STATE.write_text(json.dumps(state, indent=1, sort_keys=True) + "\n")


class DailyLimit(Exception):
    pass


def sounds():
    """Each letter sound's clip -> (what to write, how to say it), from Content/phonics/graphemes.json."""
    found = {}
    for g in json.loads((ROOT / "Content" / "phonics" / "graphemes.json").read_text())["graphemes"]:
        spelt = g["grapheme"] * 3 if g.get("kind") == "stretchy" and len(g["grapheme"]) == 1 else g["grapheme"]
        # Keep the notes on how it sounds; drop the ones on how the old clips were cut ("generate 'tap' and trim").
        hint = " ".join(x for x in re.split(r"(?<=\.)\s+", g.get("narratorHint", "")) if not re.search(r"[Gg]enerate|trim|[Rr]ecord", x))
        found[g["audio"]] = (spelt, hint)
    return found


def speak(key, style, lines, hints=()):
    if style == "phonics":
        guide = " ".join(f"Line {i}, '{line}': {hint}" for i, (line, hint) in enumerate(zip(lines, hints), 1))
        prompt = f"{PHONICS} {guide} {PAUSE}\n\n" + "\n\n".join(lines)
    else:
        prompt = f"{SPARKLE if style == 'sparkle' else WORDS} {PAUSE}\n\n" + "\n\n".join(lines)
    body = {"contents": [{"parts": [{"text": prompt}]}],
            "generationConfig": {"responseModalities": ["AUDIO"],
                                 "speechConfig": {"voiceConfig": {"prebuiltVoiceConfig": {"voiceName": VOICE}}}}}
    req = urllib.request.Request(
        f"https://generativelanguage.googleapis.com/v1beta/models/{MODEL}:generateContent",
        data=json.dumps(body).encode(), headers={"x-goog-api-key": key, "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=300) as r:
            data = json.load(r)
    except urllib.error.HTTPError as e:
        if e.code == 429:
            raise DailyLimit(e.read()[:300])
        raise SystemExit(f"Gemini said {e.code}: {e.read()[:500]!r}")
    return base64.b64decode(data["candidates"][0]["content"]["parts"][0]["inlineData"]["data"])


def silence_cut(wav, count):
    """Split a batch of letter sounds at its count-1 longest pauses: [(start, end)], or None if the pauses don't fit."""
    out = subprocess.run(["ffmpeg", "-hide_banner", "-i", str(wav), "-af", "silencedetect=noise=-35dB:d=0.6", "-f", "null", "-"],
                         capture_output=True, text=True).stderr
    total = duration(wav)
    gaps = [(float(s), float(e)) for s, e in zip(re.findall(r"silence_start: (-?[\d.]+)", out), re.findall(r"silence_end: ([\d.]+)", out))]
    inner = [(s, e) for s, e in gaps if s > 0.1 and e < total - 0.1]
    if len(inner) != count - 1:
        return None
    marks = [0.0] + [(s + e) / 2 for s, e in inner] + [total]
    return list(zip(marks, marks[1:]))


def duration(wav):
    out = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(wav)],
                         capture_output=True, text=True).stdout
    return float(out or 0)


class Listener:
    def __init__(self):
        from faster_whisper import WhisperModel
        self.model = WhisperModel("small.en", device="cpu", compute_type="int8")

    def heard(self, path):
        segments, _ = self.model.transcribe(str(path), language="en", beam_size=5)
        return " ".join(seg.text for seg in segments).strip()

    def cut(self, wav, lines):
        """
        Where each line is in a batch recording: Whisper hears the batch with word timings, the heard
        words are lined up with the expected ones, and each line runs from halfway through the gap
        before it to halfway through the gap after it. [(start, end) or None per line]; a line whose
        words weren't heard is None (it goes back in the queue).
        """
        segments, _ = self.model.transcribe(str(wav), language="en", beam_size=5, word_timestamps=True)
        heard = []  # (word, start, end), one per normalised word
        for seg in segments:
            for w in seg.words:
                for n in words(w.word):
                    heard.append((n, w.start, w.end))
        expected = [(n, i) for i, line in enumerate(lines) for n in words(line)]
        match = difflib.SequenceMatcher(a=[n for n, _ in expected], b=[n for n, _, _ in heard], autojunk=False)
        first, last = {}, {}
        for block in match.get_matching_blocks():
            for k in range(block.size):
                line = expected[block.a + k][1]
                at = heard[block.b + k]
                first.setdefault(line, at[1])
                last[line] = at[2]
        total = duration(wav)
        spans = []
        for i in range(len(lines)):
            if i not in first:
                spans.append(None)
                continue
            before = max((last[j] for j in range(i) if j in last), default=0.0)
            after = min((first[j] for j in range(i + 1, len(lines)) if j in first), default=total)
            spans.append(((before + first[i]) / 2 if before else 0.0, (last[i] + after) / 2 if after < total else total))
        return spans

    def ok(self, clip, path, text):
        if clip.startswith("name_"):
            # Letter names ("ay", "bee"): Whisper writes them as letters, so only check one short sound came out.
            return 0.2 < duration(path) < 1.6
        return "".join(words(self.heard(path))) == "".join(words(text))


def status(state, all_texts):
    wanted = {c: t for c, t in all_texts.items() if group(c)}
    done = sum(1 for c, t in wanted.items() if state["done"].get(c) == t)
    stuck = [c for c, n in state["failed"].items() if n >= GIVE_UP]
    return len(wanted), done, stuck


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--status", action="store_true")
    a = ap.parse_args()
    state, all_texts = load_state(), texts()
    total, done, stuck = status(state, all_texts)
    print(f"Aoede: {done} of {total} clips done; {len(stuck)} need a person ({', '.join(stuck) or 'none'}).", flush=True)
    if a.status:
        return
    key = os.environ.get("GEMINI_API_KEY")
    if not key:
        sys.exit("No GEMINI_API_KEY secret.")

    todo = [c for c in sorted(all_texts) if group(c) and state["done"].get(c) != all_texts[c]
            and state["failed"].get(c, 0) < GIVE_UP]
    # Fresh clips first in big batches, then retries in small ones.
    batches = []
    for retry in (False, True):
        for style in ("sparkle", "words", "phonics"):
            these = [c for c in todo if group(c) == style and bool(state["failed"].get(c)) == retry]
            size = batch_size(style, retry)
            batches += [(style, these[i:i + size]) for i in range(0, len(these), size)]
    listener = Listener()
    requests = 0
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        for style, clips in batches:
            if requests >= MAX_REQUESTS:
                print(f"Stopping after {requests} requests (MAX_REQUESTS).", flush=True)
                break
            if style == "phonics":
                lines = [sounds().get(c, (all_texts[c], ""))[0] for c in clips]
                hints = [sounds().get(c, ("", ""))[1] for c in clips]
            else:
                lines, hints = [all_texts[c] for c in clips], ()
            try:
                pcm = speak(key, style, lines, hints)
            except DailyLimit as e:
                print(f"Gemini's daily limit is used up ({e}); the next run carries on.", flush=True)
                break
            requests += 1
            raw = tmp / "batch.wav"
            (tmp / "batch.pcm").write_bytes(pcm)
            ffmpeg("-f", "s16le", "-ar", "24000", "-ac", "1", "-i", str(tmp / "batch.pcm"), str(raw))
            if style == "phonics":
                pieces = silence_cut(raw, len(clips)) or [None] * len(clips)
            else:
                pieces = listener.cut(raw, lines)
            good = 0
            for clip, span in zip(clips, pieces):
                if span is None:
                    state["failed"][clip] = state["failed"].get(clip, 0) + 1
                    print(f"  {clip}: not heard in the batch", flush=True)
                    continue
                start, end = span
                piece, trimmed, out = tmp / "piece.wav", tmp / "trimmed.wav", tmp / f"{clip}.m4a"
                ffmpeg("-i", str(raw), "-ss", f"{start:.3f}", "-to", f"{end:.3f}", str(piece))
                ffmpeg("-i", str(piece), "-af", TRIM, "-ar", "44100", "-ac", "1", str(trimmed))
                gain = TARGET_LUFS - loudness(trimmed)
                ffmpeg("-i", str(trimmed), "-af", f"volume={gain:.2f}dB,alimiter=limit=0.9", "-ar", "44100", "-ac", "1",
                       "-c:a", "aac", "-b:a", "96k", str(out))
                if style == "phonics":
                    # No machine check for a pure sound: it waits for the owner's ear.
                    PHONICS_DIR.mkdir(parents=True, exist_ok=True)
                    out.replace(PHONICS_DIR / f"{clip}.m4a")
                    state["done"][clip] = all_texts[clip]
                    state["failed"].pop(clip, None)
                    good += 1
                elif listener.ok(clip, out, all_texts[clip]):
                    out.replace(AUDIO / f"{clip}.m4a")
                    state["done"][clip] = all_texts[clip]
                    state["failed"].pop(clip, None)
                    good += 1
                else:
                    state["failed"][clip] = state["failed"].get(clip, 0) + 1
                    print(f"  {clip}: heard {listener.heard(out)!r}, wanted {all_texts[clip]!r}", flush=True)
            save_state(state)
            print(f"Request {requests}: {good} of {len(clips)} {style} clips kept.", flush=True)
    total, done, stuck = status(state, all_texts)
    print(f"Aoede: {done} of {total} clips done; {len(stuck)} need a person ({', '.join(stuck) or 'none'}).", flush=True)


if __name__ == "__main__":
    main()
