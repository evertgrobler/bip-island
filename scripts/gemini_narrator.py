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

Both home languages are recorded (docs/LANGUAGES.md), a batch of each in turn: English from
Content/asset_manifest.json and audio/script.csv into Resources/Audio/, Afrikaans from Content/af/
and audio/af/script.csv into Resources/Audio/af/ (keys "af/<clip>" in the progress file). Whisper
hears Afrikaans less exactly than English, so an Afrikaans piece passes when what it heard is close
to the line rather than identical.

  python3 scripts/gemini_narrator.py                # record what's left, up to MAX_REQUESTS requests
  python3 scripts/gemini_narrator.py --status       # how many clips are done, failed and left
"""
import argparse, base64, csv, difflib, fnmatch, itertools, json, os, re, subprocess, sys, tempfile, urllib.error, urllib.request
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from speak_clips import AUDIO, ROOT, TARGET_LUFS, ffmpeg, loudness, words  # noqa: E402

MODEL = "gemini-3.8-flash-tts"
VOICE = "Aoede"
STATE = ROOT / "voice-lab" / "aoede.json"
MAX_REQUESTS = int(os.environ.get("MAX_REQUESTS", "40"))
# Trim the ends of each piece: Gemini's quiet has a soft noise floor, so -40 dB counts as silence.
TRIM = ("silenceremove=start_periods=1:start_threshold=-40dB:start_silence=0.05,areverse,"
        "silenceremove=start_periods=1:start_threshold=-40dB:start_silence=0.08,areverse")
GIVE_UP = 4  # failed takes before a clip is listed for a person to look at

# The styles (docs/VOICE_PLAN.md, section 6): Bip's own lines sparkle for young children (sparkle) and are
# upbeat but relaxed for older ones (big, the <clip>_big lines); instructions and sentences are clear and
# friendly for every age (clear); words and numbers are even and clear (words).
SPARKLE = ("Read these lines as a bubbly, sparkling, warm South African English teacher of four- to six-year-olds "
           "who is delighted to see the child: a big smile in the voice, playful and full of wonder, a little "
           "sing-song on the happy words, unhurried and very clear, with tiny pauses so a young child can follow. "
           "Friendly, never shrill, squeaky or baby-talk. Natural South African English accent, not British and not American.")
BIG = ("Read these lines as the same warm South African English teacher, now talking to a seven- to ten-year-old "
       "she respects: friendly, upbeat and genuinely pleased, with a real smile in the voice, but relaxed and "
       "natural, like a favourite teacher or an older cousin chatting, not performing for a toddler. Normal "
       "talking speed, a little quicker than for little ones; no sing-song, no exaggerated excitement, no baby "
       "talk, never sarcastic or bored. Confident and encouraging. Natural South African English accent, not "
       "British and not American.")
CLEAR = ("Read these lines as a warm, friendly South African English primary school teacher giving clear "
         "instructions to children aged four to ten: a smile in the voice and a bright, positive tone, but natural "
         "and not sing-song, so it suits a five-year-old and doesn't sound babyish to a ten-year-old. Unhurried and "
         "very clear; say each line once, exactly as written. A line that ends without a full stop, or with a "
         "comma or colon, leads straight into another recording: keep the voice up and unfinished, as if the "
         "sentence carries on. Natural South African English accent, not British and not American.")
WORDS = ("Read these words as a warm, friendly South African English teacher showing a child each word: clear, "
         "bright and natural, each word once, exactly as written, all at the same even pitch and speed, so the "
         "first and last words sound alike and none sounds like the end of a list. Natural South African English "
         "accent, not British and not American.")
PHONICS = ("You are a calm, clear South African English phonics teacher. Each line below is one letter sound. "
           "Say only the pure sound, never the letter name, once, then stop: stretchy sounds (s, m, n, f, l, r, v, z, "
           "sh, th) are held for about a second; stop sounds (t, p, k, c, b, d, g) are short and crisp with no 'uh' "
           "after them; vowels are short unless the line says long. The sounds in order:")
PHONICS_DIR = ROOT / "voice-lab" / "aoede_phonics"
PAUSE = "After every line, stay silent for two full seconds before the next one. Never read anything twice."

AFRIKAANS = ("Everything is in Afrikaans: say it with natural, fluent South African Afrikaans pronunciation, "
             "as a first-language Afrikaans speaker would, never with an English or Dutch accent.")


def in_afrikaans(notes):
    """A style's notes for the Afrikaans lines: an Afrikaans teacher, with the Afrikaans accent sentence."""
    notes = notes.replace("South African English teacher", "Afrikaans teacher").replace(
        "South African English primary school teacher", "Afrikaans primary school teacher")
    return notes.split(" Natural South African English accent")[0] + " " + AFRIKAANS


SPARKLE_AF, BIG_AF, CLEAR_AF, WORDS_AF = (in_afrikaans(n) for n in (SPARKLE, BIG, CLEAR, WORDS))
PHONICS_AF = ("You are a calm, clear Afrikaans phonics teacher (klanke). Each line below is one Afrikaans letter sound. "
              "Say only the pure Afrikaans sound, never the letter name, once, then stop: stretchy sounds (s, m, n, f, "
              "l, r, v, w) are held for about a second; stop sounds (t, p, k, d, b) are short and crisp with no 'uh' "
              "after them; g is the Afrikaans throat sound. " + AFRIKAANS + " The sounds in order:")

# Each language: where its words come from and where its clips go.
LANGS = {
    "en": {"content": ROOT / "Content", "script": ROOT / "audio" / "script.csv", "audio": AUDIO,
           "phonics": PHONICS_DIR, "styles": {"sparkle": SPARKLE, "big": BIG, "clear": CLEAR, "words": WORDS, "phonics": PHONICS}},
    "af": {"content": ROOT / "Content" / "af", "script": ROOT / "audio" / "af" / "script.csv", "audio": AUDIO / "af",
           "phonics": PHONICS_DIR / "af", "styles": {"sparkle": SPARKLE_AF, "big": BIG_AF, "clear": CLEAR_AF, "words": WORDS_AF, "phonics": PHONICS_AF}},
}
AF_NUMBERS = ("nul een twee drie vier vyf ses sewe agt nege tien elf twaalf dertien veertien vyftien sestien "
              "sewentien agtien negentien twintig").split()
AF_CLOSE = 0.75  # how close (0 to 1) what Whisper heard must be to an Afrikaans line
# English: a heard word counts as the expected one when it is this close (0 to 1): spellings Whisper writes
# its own way ("practices" for "practises"), but never another word ("bull" for "ball").
EN_WORD_CLOSE = 0.85
# ...and a name (Sipho, Thandi, Dlamini), which Whisper often spells as it hears it ("Sipo", "Tandi").
EN_NAME_CLOSE = 0.5


def split(key):
    """A progress key -> (language, clip): "af/word_kat" -> ("af", "word_kat"), "word_sun" -> ("en", "word_sun")."""
    lang, _, clip = key.rpartition("/")
    return (lang or "en"), clip


def texts():
    """Every clip's text in both languages, keyed "word_sun" (English) and "af/word_kat" (Afrikaans)."""
    found = {}
    for lang, where in LANGS.items():
        manifest = where["content"] / "asset_manifest.json"
        if not manifest.exists():
            continue
        prefix = "" if lang == "en" else f"{lang}/"
        for a in json.loads(manifest.read_text())["audio"]:
            found[prefix + a["id"]] = a["text"]
        if where["script"].exists():
            with open(where["script"], newline="", encoding="utf-8") as f:
                for row in csv.DictReader(f):
                    found[prefix + row["file"].removesuffix(".m4a")] = row["text"]
    return found


def af_words(text):
    """The words of an Afrikaans line, lower case, numbers as Afrikaans words, "'n" as "n"."""
    out = []
    for w in re.findall(r"[a-z0-9\u00e0-\u00ff']+", text.lower()):
        out.append(AF_NUMBERS[int(w)] if w.isdigit() and int(w) <= 20 else w.strip("'"))
    return [w for w in out if w]


def line_words(lang, text):
    return af_words(text) if lang == "af" else words(text)


# Bip's own lines (BipCore.VoiceBands.BigVersions): sparkling for young children, <clip>_big for older ones.
OWN_LINES = {"vo_level_up", "vo_lets_practise_again", "vo_round_done", "vo_welcome", "vo_letters_island",
             "vo_numbers_island", "vo_words_island", "vo_coding_island", "vo_art_island", "vo_language_island",
             "vo_island_sleeping", "vo_who_is_playing", "vo_bip_charging", "vo_day_done"}
HOLD = ROOT / "voice-lab" / "aoede-hold.txt"


def group(key):
    """Which style a clip is spoken in, or None if it isn't recorded here."""
    clip = split(key)[1]
    if clip.startswith("snd_"):
        return "phonics"
    # Older children's lines are only ever vo_/hint_/praise_ (BipCore.VoiceBands): a word_big is a word.
    if clip.endswith("_big") and clip.startswith(("vo_", "hint_", "praise_")):
        return "big"
    if clip.startswith(("word_", "num_", "money_", "name_", "learn_")) and not clip.endswith("_mid"):
        return "words"
    if clip.startswith(("praise_", "hint_")) or clip in OWN_LINES:
        return "sparkle"
    return "clear"


def priority(key):
    """Most-heard first (docs/VOICE_PLAN.md, section 7): Bip's own lines, instructions, the older lines,
    small numbers, letter sounds, words, stories and reading sentences, Language Island words, and the
    number sentences last (the game joins those from pieces meanwhile). The English Bip's Stories lines
    come before everything (owner, 25 October 2026): the game has no other recording of them."""
    lang, clip = split(key)
    style = group(key)
    if lang == "en" and (clip.startswith("vo_story_") or clip == "vo_bips_stories"):
        return 0
    if style == "sparkle":
        return 1
    if clip.startswith("vo_say_"):
        return 9
    if clip.startswith("learn_"):
        return 8
    if re.fullmatch(r"vo_[a-z]+_\d+", clip) or clip.startswith(("vo_sent_", "vo_hom_")):
        return 7
    if clip.startswith("money_") or re.fullmatch(r"num_(\d|1\d|20)(_mid)?", clip) or clip.endswith("_mid"):
        return 4
    if style == "clear":
        return 2
    if style == "big":
        return 3
    if style == "phonics":
        return 5
    return 6


def held():
    """Clips not to record yet (voice-lab/aoede-hold.txt: one pattern per line, like af/learn_*): lines
    still waiting for the owner's check."""
    if not HOLD.exists():
        return []
    return [line.strip() for line in HOLD.read_text().splitlines() if line.strip() and not line.startswith("#")]


def is_held(key, patterns):
    return any(fnmatch.fnmatchcase(key, p) for p in patterns)


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


def sounds(lang):
    """Each letter sound's clip -> (what to write, how to say it), from the language's phonics/graphemes.json."""
    found = {}
    for g in json.loads((LANGS[lang]["content"] / "phonics" / "graphemes.json").read_text())["graphemes"]:
        spelt = g["grapheme"] * 3 if g.get("kind") == "stretchy" and len(g["grapheme"]) == 1 else g["grapheme"]
        # Keep the notes on how it sounds; drop the ones on how the old clips were cut ("generate 'tap' and trim").
        hint = " ".join(x for x in re.split(r"(?<=\.)\s+", g.get("narratorHint", "")) if not re.search(r"[Gg]enerate|trim|[Rr]ecord", x))
        found[g["audio"]] = (spelt, hint)
    return found


def speak(key, lang, style, lines, hints=()):
    how = LANGS[lang]["styles"][style]
    # The directions sit under their own heading and only the transcript is read: with the directions
    # run into the lines, Gemini sometimes read them out at the start of the first line.
    if style == "phonics":
        guide = " ".join(f"Line {i}, '{line}': {hint}" for i, (line, hint) in enumerate(zip(lines, hints), 1))
        notes = f"{how} {guide} {PAUSE}"
    else:
        notes = f"{how} {PAUSE}"
    prompt = (f"### DIRECTOR'S NOTES (never read these out)\n{notes}\n\n"
              "#### TRANSCRIPT (read only this)\n" + "\n\n".join(lines))
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


def names(text):
    """The capitalised words of a line (names such as Sipho or Dlamini), as Whisper hotwords; None if there are none."""
    found = list(dict.fromkeys(re.findall(r"\b[A-Z][a-z']+\b", text)))
    return " ".join(found) or None


def same_english(heard, text):
    """
    Whether Whisper heard the English line: the same words in the same order, where a word may differ only
    in spelling (EN_WORD_CLOSE) and a name only as Whisper spells it (EN_NAME_CLOSE). Nothing missing or extra.
    """
    got, want = words(heard), words(text)
    proper = {w.lower() for w in re.findall(r"\b[A-Z][a-z']+\b", text)}
    for op, a1, a2, b1, b2 in difflib.SequenceMatcher(a=want, b=got, autojunk=False).get_opcodes():
        if op == "equal":
            continue
        if op != "replace" or a2 - a1 != b2 - b1:
            return False
        for w, g in zip(want[a1:a2], got[b1:b2]):
            close = EN_NAME_CLOSE if w in proper else EN_WORD_CLOSE
            if difflib.SequenceMatcher(a=w, b=g, autojunk=False).ratio() < close:
                return False
    return True


class Listener:
    """Whisper: small.en for English, the multilingual small model for Afrikaans (loaded when first needed)."""
    def __init__(self):
        self.models = {}

    def model(self, lang):
        if lang not in self.models:
            from faster_whisper import WhisperModel
            self.models[lang] = WhisperModel("small.en" if lang == "en" else "small", device="cpu", compute_type="int8")
        return self.models[lang]

    def heard(self, path, lang="en", text=""):
        segments, _ = self.model(lang).transcribe(str(path), language=lang, beam_size=5, hotwords=names(text))
        return " ".join(seg.text for seg in segments).strip()

    def cut(self, wav, lines, lang="en"):
        """
        Where each line is in a batch recording: Whisper hears the batch with word timings, the heard
        words are lined up with the expected ones, and each line runs from halfway through the gap
        before it to halfway through the gap after it. [(start, end) or None per line]; a line whose
        words weren't heard is None (it goes back in the queue). Afrikaans words Whisper heard nearly
        right count as the word they are closest to.
        """
        segments, _ = self.model(lang).transcribe(str(wav), language=lang, beam_size=5, word_timestamps=True,
                                                  hotwords=names(" ".join(lines)))
        expected = [(n, i) for i, line in enumerate(lines) for n in line_words(lang, line)]
        vocabulary = sorted({n for n, _ in expected})
        heard = []  # (word, start, end), one per normalised word
        for seg in segments:
            for w in seg.words:
                for n in line_words(lang, w.word):
                    if lang != "en" and n not in vocabulary:
                        n = next(iter(difflib.get_close_matches(n, vocabulary, n=1, cutoff=0.6)), n)
                    heard.append((n, w.start, w.end))
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

    def ok(self, key, path, text):
        lang, clip = split(key)
        if clip.startswith("name_"):
            # Letter names ("ay", "bee"): Whisper writes them as letters, so only check one short sound came out.
            return 0.2 < duration(path) < 1.6
        heard = self.heard(path, lang, text)
        if lang == "en":
            return same_english(heard, text)
        got, want = "".join(line_words(lang, heard)), "".join(line_words(lang, text))
        return difflib.SequenceMatcher(a=got, b=want, autojunk=False).ratio() >= AF_CLOSE


def status(state, all_texts):
    wanted = {c: t for c, t in all_texts.items() if group(c)}
    done = sum(1 for c, t in wanted.items() if state["done"].get(c) == t)
    stuck = [c for c, n in state["failed"].items() if n >= GIVE_UP]
    hold = held()
    waiting = sum(1 for c, t in wanted.items() if state["done"].get(c) != t and is_held(c, hold))
    return len(wanted), done, stuck, waiting


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--status", action="store_true")
    a = ap.parse_args()
    state, all_texts = load_state(), texts()
    total, done, stuck, waiting = status(state, all_texts)
    print(f"Aoede: {done} of {total} clips done; {waiting} waiting for the owner's check; {len(stuck)} need a person ({', '.join(stuck) or 'none'}).", flush=True)
    if a.status:
        return
    key = os.environ.get("GEMINI_API_KEY")
    if not key:
        sys.exit("No GEMINI_API_KEY secret.")

    hold = held()
    todo = [c for c in sorted(all_texts, key=lambda c: (priority(c), c)) if group(c) and state["done"].get(c) != all_texts[c]
            and state["failed"].get(c, 0) < GIVE_UP and not is_held(c, hold)]
    # Fresh clips first in big batches, most-heard first, then retries in small ones; English and
    # Afrikaans take turns, except that step 0 (the stories) goes before any turn-taking. Each batch is
    # one style.
    queues = {lang: [] for lang in LANGS}
    first = []
    for retry in (False, True):
        for step in range(0, 10):
            for style in ("sparkle", "clear", "big", "words", "phonics"):
                for lang in LANGS:
                    these = [c for c in todo if split(c)[0] == lang and group(c) == style and priority(c) == step
                             and bool(state["failed"].get(c)) == retry]
                    size = batch_size(style, retry)
                    chunks = [(lang, style, these[i:i + size]) for i in range(0, len(these), size)]
                    if step == 0:
                        first += chunks
                    else:
                        queues[lang] += chunks
    batches = first + [b for turn in itertools.zip_longest(*queues.values()) for b in turn if b]
    listener = Listener()
    requests = 0
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        for lang, style, clips in batches:
            if requests >= MAX_REQUESTS:
                print(f"Stopping after {requests} requests (MAX_REQUESTS).", flush=True)
                break
            if style == "phonics":
                known = sounds(lang)
                lines = [known.get(split(c)[1], (all_texts[c], ""))[0] for c in clips]
                hints = [known.get(split(c)[1], ("", ""))[1] for c in clips]
            else:
                lines, hints = [all_texts[c] for c in clips], ()
            try:
                pcm = speak(key, lang, style, lines, hints)
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
                pieces = listener.cut(raw, lines, lang)
            good = 0
            for clip, span in zip(clips, pieces):
                if span is None:
                    state["failed"][clip] = state["failed"].get(clip, 0) + 1
                    print(f"  {clip}: not heard in the batch", flush=True)
                    continue
                start, end = span
                name = split(clip)[1]
                piece, trimmed, out = tmp / "piece.wav", tmp / "trimmed.wav", tmp / f"{name}.m4a"
                ffmpeg("-i", str(raw), "-ss", f"{start:.3f}", "-to", f"{end:.3f}", str(piece))
                ffmpeg("-i", str(piece), "-af", TRIM, "-ar", "44100", "-ac", "1", str(trimmed))
                gain = TARGET_LUFS - loudness(trimmed)
                ffmpeg("-i", str(trimmed), "-af", f"volume={gain:.2f}dB,alimiter=limit=0.9", "-ar", "44100", "-ac", "1",
                       "-c:a", "aac", "-b:a", "96k", str(out))
                if style == "phonics":
                    # No machine check for a pure sound: it waits for the owner's ear.
                    LANGS[lang]["phonics"].mkdir(parents=True, exist_ok=True)
                    out.replace(LANGS[lang]["phonics"] / f"{name}.m4a")
                    state["done"][clip] = all_texts[clip]
                    state["failed"].pop(clip, None)
                    good += 1
                elif listener.ok(clip, out, all_texts[clip]):
                    LANGS[lang]["audio"].mkdir(parents=True, exist_ok=True)
                    out.replace(LANGS[lang]["audio"] / f"{name}.m4a")
                    state["done"][clip] = all_texts[clip]
                    state["failed"].pop(clip, None)
                    good += 1
                else:
                    state["failed"][clip] = state["failed"].get(clip, 0) + 1
                    print(f"  {clip}: heard {listener.heard(out, lang, all_texts[clip])!r}, wanted {all_texts[clip]!r}", flush=True)
            save_state(state)
            print(f"Request {requests}: {good} of {len(clips)} {lang} {style} clips kept.", flush=True)
    total, done, stuck, waiting = status(state, all_texts)
    print(f"Aoede: {done} of {total} clips done; {waiting} waiting for the owner's check; {len(stuck)} need a person ({', '.join(stuck) or 'none'}).", flush=True)


if __name__ == "__main__":
    main()
