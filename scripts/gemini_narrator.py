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
import argparse, base64, csv, difflib, fnmatch, itertools, json, os, re, subprocess, sys, tempfile, time, urllib.error, urllib.request
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import clip_clean  # noqa: E402
from speak_clips import AUDIO, NUMBER_WORDS, ROOT, TARGET_LUFS, ffmpeg, loudness, words  # noqa: E402

MODEL = "gemini-3.8-flash-tts"
VOICE = "Aoede"
STATE = ROOT / "voice-lab" / "aoede.json"
MAX_REQUESTS = int(os.environ.get("MAX_REQUESTS") or 40)
# No new batch starts after this many minutes, so the run always ends (and commits) inside the
# workflow's 180-minute limit.
# Counted from the script's start, after a few minutes of setup; one batch can then take up to about 30
# minutes (four tries of a slow reply plus per-minute waits, and Whisper on CPU), so 120 leaves room.
MAX_MINUTES = 120
# Trim the ends of each piece: Gemini's quiet has a soft noise floor, so -40 dB counts as silence.
TRIM = ("silenceremove=start_periods=1:start_threshold=-40dB:start_silence=0.05,areverse,"
        "silenceremove=start_periods=1:start_threshold=-40dB:start_silence=0.08,areverse")
GIVE_UP = 4  # failed takes before a clip is listed for a person to look at
# A clip past GIVE_UP gets up to EAR_TRIES more takes, each kept in voice-lab/aoede_review/ (not the
# game) for the owner to listen to, since Whisper mishears some short Afrikaans lines that are fine
# ("'n slak", "eier"). The newest take waits there; one the owner approves is copied into the game.
EAR_TRIES = 3
# Whisper hears Afrikaans poorly (English for "Dubbel drie. Hoeveel is dit?": "Double three. Who feel is
# that?"), so an Afrikaans take goes to the owner's ear from its second failed take; English from GIVE_UP.
EAR_FROM = {"en": GIVE_UP, "af": 1}
REVIEW_DIR = ROOT / "voice-lab" / "aoede_review"

# Every English style ends with this (owner, 9 Nov 2026: some lines, Coding Island's among them, still
# sounded British). It starts "Natural South African English accent", which in_afrikaans() looks for.
ACCENT = ("Natural South African English accent, the way an English-speaking teacher in Johannesburg or Cape Town "
          "talks: South African vowels and rhythm in every word, never British Received Pronunciation, never posh or "
          "plummy, and not American.")

# Words of the accent sentence a line or a mishearing can easily hold: never taken as instructions read out.
ACCENT_EVERYDAY = {"natural", "english", "speaking", "teacher", "talks", "every", "rhythm", "south", "african",
                   "vowels"}

# The styles (docs/VOICE_PLAN.md, section 6): Bip's own lines sparkle for young children (sparkle) and are
# upbeat but relaxed for older ones (big, the <clip>_big lines); instructions and sentences are clear and
# friendly for every age (clear); words and numbers are even and clear (words).
SPARKLE = ("Read these lines as a bubbly, sparkling, warm South African English teacher of four- to six-year-olds "
           "who is delighted to see the child: a big smile in the voice, playful and full of wonder, a little "
           "sing-song on the happy words, unhurried and very clear, with tiny pauses so a young child can follow. "
           "Friendly, never shrill, squeaky or baby-talk. " + ACCENT)
BIG = ("Read these lines as the same warm South African English teacher, now talking to a seven- to ten-year-old "
       "she respects: friendly, upbeat and genuinely pleased, with a real smile in the voice, but relaxed and "
       "natural, like a favourite teacher or an older cousin chatting, not performing for a toddler. Normal "
       "talking speed, a little quicker than for little ones; no sing-song, no exaggerated excitement, no baby "
       "talk, never sarcastic or bored. Confident and encouraging. " + ACCENT)
CLEAR = ("Read these lines as a warm, friendly South African English primary school teacher giving clear "
         "instructions to children aged four to ten: a smile in the voice and a bright, positive tone, but natural "
         "and not sing-song, so it suits a five-year-old and doesn't sound babyish to a ten-year-old. Unhurried and "
         "very clear; say each line once, exactly as written. A line that ends without a full stop, or with a "
         "comma or colon, leads straight into another recording: keep the voice up and unfinished, as if the "
         "sentence carries on. " + ACCENT)
WORDS = ("Read these words as a warm, friendly South African English teacher showing a child each word: clear, "
         "bright and natural, each word once, exactly as written, all at the same even pitch and speed, so the "
         "first and last words sound alike and none sounds like the end of a list. " + ACCENT)
PHONICS = ("You are a calm, clear South African English phonics teacher. Each line below is one letter sound. "
           "Say only the pure sound, never the letter name, once, then stop: stretchy sounds (s, m, n, f, l, r, v, z, "
           "sh, th) are held for about a second; stop sounds (t, p, k, c, b, d, g) are short and crisp with no 'uh' "
           "after them; vowels are short unless the line says long. South African English vowels, never British Received "
           "Pronunciation. The sounds in order:")
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
           "phonics": PHONICS_DIR, "styles": {"sparkle": SPARKLE, "big": BIG, "clear": CLEAR, "words": WORDS, "names": WORDS, "phonics": PHONICS}},
    "af": {"content": ROOT / "Content" / "af", "script": ROOT / "audio" / "af" / "script.csv", "audio": AUDIO / "af",
           "phonics": PHONICS_DIR / "af", "styles": {"sparkle": SPARKLE_AF, "big": BIG_AF, "clear": CLEAR_AF, "words": WORDS_AF, "names": WORDS_AF, "phonics": PHONICS_AF}},
}
AF_NUMBERS = ("nul een twee drie vier vyf ses sewe agt nege tien elf twaalf dertien veertien vyftien sestien "
              "sewentien agtien negentien twintig").split()
# Afrikaans: Whisper can hear Afrikaans as half Dutch ("Precies raag" for "Presies reg"), so a
# line passes when, after DUTCH spellings are made Afrikaans, the whole line is this close (0 to 1)...
AF_LINE_CLOSE = 0.65
# ...and each changed stretch of words is this close; numbers and "nie" (DUTCH_NOT) must match exactly.
AF_WORD_CLOSE = 0.5
DUTCH = [("sch", "sk"), ("ij", "y"), ("ch", "g"), ("ae", "e"), ("c", "k"), ("z", "s"), ("uu", "u"), ("oo", "o"),
         ("aa", "a"), ("ee", "e"), ("\u00eb", "e"), ("\u00ef", "i")]
DUTCH_NOT = {"nie", "geen", "nooit", "niks", "niemand"}
# English: a person's name (Sipho, Thandi, Dlamini) counts when it is this close (0 to 1), as Whisper often
# spells it as it hears it ("Sipo", "Tandi"). Every other word must match, apart from SPELLINGS.
EN_NAME_CLOSE = 0.5
# How far before or after a line's first or last heard word its piece is cut when the line next to it
# wasn't found in the batch (so the piece never takes in that line's audio).
EDGE = 0.4


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
    """Which style a clip is spoken in (every clip in texts() has one)."""
    clip = split(key)[1]
    if clip.startswith("snd_"):
        return "phonics"
    # Older children's lines are only ever vo_/hint_/praise_ (BipCore.VoiceBands): a word_big is a word.
    if clip.endswith("_big") and clip.startswith(("vo_", "hint_", "praise_")):
        return "big"
    # Letter names ("ay", "bee") have their own batches, cut at the pauses: Whisper writes them as letters.
    if clip.startswith("name_"):
        return "names"
    if clip.startswith(("word_", "num_", "money_", "learn_")) and not clip.endswith("_mid"):
        return "words"
    if clip.startswith(("praise_", "hint_")) or clip in OWN_LINES:
        return "sparkle"
    return "clear"


def priority(key):
    """Most-heard first (docs/VOICE_PLAN.md, section 7): Bip's own lines, instructions, the older lines,
    small numbers, letter sounds, words, stories and reading sentences, Language Island words, and the
    number sentences last (the game joins those from pieces meanwhile). The English Bip's Stories lines
    come before everything (owner, 25 October 2026): the game has no other recording of them. So do the
    Afrikaans stories (approved 1 November 2026) and the Afrikaans Language Island words, which an English
    child learns and which have no recording either."""
    lang, clip = split(key)
    style = group(key)
    if clip.startswith("vo_story_") or clip == "vo_bips_stories" or (lang == "af" and clip.startswith("learn_")):
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
    if style in ("phonics", "names"):
        return 3 if retry else 8
    if retry:
        return 4
    return 36 if style == "words" else 18


def reads_instructions(heard, text, lang, style):
    """
    Whether a take has Gemini reading its instructions aloud: much longer than the line, or two or more of the
    instructions' longer words that are not in the line ("read anything twice" for "twee", owner, 5 Nov 2026).
    Only words of five letters or more count: Whisper hears Afrikaans as English ("Click on the dog and the
    cat"), and short words like "the", "and" or "two" are in the instructions too, so counting them would
    throw away good Afrikaans takes instead of sending them to the owner's ear.
    """
    if len(heard) > 2 * len(text) + 20:
        return True
    # Gemini sometimes reads them out in Afrikaans too ("Never lees iets twee keer nie", 6 Nov 2026).
    # The accent sentence's everyday words are left out (a misheard line can have them); its telling ones
    # ("british", "received", "pronunciation", "american") still count.
    prompt = (set(words(LANGS[lang]["styles"][style] + " " + PAUSE)) - (ACCENT_EVERYDAY if lang == "en" else set())) | {
        "lees", "keer", "volgende", "sekondes", "stil", "instruksies", "notes", "director", "never"}
    line = set(line_words(lang, text))
    extra = {w for w in words(heard) if len(w) >= 5 and w in prompt and w not in line}
    return len(extra) >= 2


def finish(style):
    """
    The last filters on every clip. Gemini's audio carries a hiss above about 8 kHz (static to the owner's
    ear, 29 Oct 2026), which the volume raise makes louder: narration is cut above 8 kHz (a voice needs
    little there), letter sounds only above 11 kHz so "s", "f" and "th" keep their hiss. Then a gentle limit.
    Then the hiss under the voice and the clicks come out (clip_clean.CLEAN; static to the owner's ear again on
    3 and 5 Nov), except on letter sounds, and every clip gets soft ends.
    """
    top = 11000 if style in ("phonics", "words", "names") else 8000
    # Words and letter names keep their quiet f, th and s (the gate would turn them down): only the clicks.
    clean = ("" if style == "phonics" else clip_clean.DECLICK + "," if style in ("words", "names")
             else clip_clean.CLEAN + ",")
    return (f"lowpass=f={top}:poles=2,lowpass=f={top}:poles=2,highpass=f=70,"
            f"alimiter=limit=0.85:attack=5:release=60:level=disabled,{clean}{clip_clean.FADES}")


def load_state():
    state = json.loads(STATE.read_text()) if STATE.exists() else {"done": {}, "failed": {}}
    state.setdefault("failed_text", {})
    state.setdefault("review", {})
    state.setdefault("retake", [])  # finished clips recorded again for the owner's ear (--retake)
    return state


def fails(state, clip, text):
    """Failed takes of a clip's current text: a clip whose text has changed since starts again at 0."""
    return state["failed"].get(clip, 0) if state["failed_text"].get(clip, text) == text else 0


def failed_take(state, clip, text):
    state["failed"][clip] = fails(state, clip, text) + 1
    state["failed_text"][clip] = text


def took(state, clip, text):
    state["done"][clip] = text
    if state["review"].pop(clip, None) is not None:
        review_path(clip).unlink(missing_ok=True)  # no orphan take left where no option can reach it
    if clip in state["retake"]:
        state["retake"].remove(clip)
    state["failed"].pop(clip, None)
    state["failed_text"].pop(clip, None)


def review_path(clip):
    """Where a clip's take waits for the owner's ear: voice-lab/aoede_review/ (af/ for Afrikaans)."""
    lang, name = split(clip)
    return REVIEW_DIR / ("af" if lang == "af" else "") / f"{name}.m4a"


def to_review(state, clip, out, text, heard):
    """A take that waits in voice-lab/aoede_review/ for the owner's ear (--approve or --redo)."""
    path = review_path(clip)
    path.parent.mkdir(parents=True, exist_ok=True)
    out.replace(path)
    state["review"][clip] = {"text": text, "heard": heard}


def is_retake(state, clip, text):
    """A finished clip recorded again for the owner's ear (--retake): only while the game's clip still says this text."""
    return clip in state["retake"] and state["done"].get(clip) == text


def save_state(state):
    STATE.write_text(json.dumps(state, indent=1, sort_keys=True) + "\n")


class DailyLimit(Exception):
    pass


class SetupError(Exception):
    """The key, the model or the request is wrong (any 4xx but 429): no batch can work until a person fixes it."""


class BatchFailed(Exception):
    """
    Gemini couldn't make this batch: try the next one. <lines_fault> is True when the reply itself was the
    problem (a reply with no audio), so the clips count a failed take and move on to small retry
    batches, then GIVE_UP; a network error or a 5xx is no one's fault and counts nothing.
    """
    def __init__(self, message, lines_fault=False):
        super().__init__(message)
        self.lines_fault = lines_fault


def sounds(lang):
    """Each letter sound's clip -> (what to write, how to say it), from the language's phonics/graphemes.json."""
    found = {}
    for g in json.loads((LANGS[lang]["content"] / "phonics" / "graphemes.json").read_text())["graphemes"]:
        spelt = g["grapheme"] * 3 if g.get("kind") == "stretchy" and len(g["grapheme"]) == 1 else g["grapheme"]
        # Keep the notes on how it sounds; drop the ones on how the old clips were cut ("generate 'tap' and trim").
        hint = " ".join(x for x in re.split(r"(?<=\.)\s+", g.get("narratorHint", "")) if not re.search(r"[Gg]enerate|trim|[Rr]ecord", x))
        spelt, hint = SAY_AS.get((lang, g["audio"]), (spelt, hint))
        found[g["audio"]] = (spelt, hint)
    return found


# Sounds the owner flagged on the Letter Sounds Check page (1 November 2026), written the way they sound:
# "igh" came out as the three letters and "er" and "a" not as the sound.
SAY_AS = {
    ("en", "snd_igh"): ("eye", "The long i sound in light and night, said once. Never the letters i, g, h."),
    ("en", "snd_er"): ("uh", "The soft, short 'uh' at the end of ladder and sister: no 'r', never the letters e, r."),
    ("en", "snd_a"): ("a", "The short a in ant, cat and apple, said very briefly: not 'ay', not 'ah', and no word after it."),
}


# Lines written differently for Gemini than in the game, where it read the game's spelling wrong: the
# Afrikaans letter name "pee" came out as the English word twice (owner, 5 Nov 2026); the capital letter
# alone makes it say the Afrikaans name.
SAY_LINE = {"af/name_p": "P"}


def af_number_words(n):
    """An Afrikaans number in words, the small part first as Afrikaans says it: 75 -> "vyf-en-sewentig"."""
    if n <= 20:
        return AF_NUMBERS[n]
    if n == 100:
        return "honderd"
    tens = AF_TENS[n // 10 - 2]
    return tens if n % 10 == 0 else f"{AF_NUMBERS[n % 10]}-en-{tens}"


def said(clip, text):
    """
    What Gemini is given to say for a clip. An Afrikaans number written in digits goes as words: from "75"
    Gemini said "sewe-en-vyftig" (the digits in English order) and from "84" "agt-en-sestig" (owner, 6 Nov 2026).
    """
    if clip in SAY_LINE:
        return SAY_LINE[clip]
    lang, _ = split(clip)
    if lang == "af" and re.fullmatch(r"\d{1,3}", text.strip()) and int(text) <= 100:
        return af_number_words(int(text))
    return text


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
    for attempt in range(4):
        try:
            with urllib.request.urlopen(req, timeout=300) as r:
                data = json.load(r)
            return base64.b64decode(data["candidates"][0]["content"]["parts"][0]["inlineData"]["data"])
        except urllib.error.HTTPError as e:
            text = e.read().decode("utf-8", "replace")
            # Any other 4xx is the request's fault (a key, a model, a request body or a region), never the
            # lines': it stops the run for a person rather than counting failed takes against good lines.
            if 400 <= e.code < 500 and e.code != 429:
                raise SetupError(f"Gemini said {e.code}: {text[:300]!r}")
            if e.code != 429:
                raise BatchFailed(f"Gemini said {e.code}: {text[:300]!r}")
            # The free tier has a per-minute limit as well as the daily one: wait out a per-minute 429.
            wait = re.search(r'"retryDelay":\s*"(\d+)(?:\.\d+)?s"', text)
            # The last try never waits: a fourth per-minute 429 in a row ends the day's run.
            if "PerDay" in text or attempt == 3 or not ("PerMinute" in text or wait):
                raise DailyLimit(text[:300])
            time.sleep(min(int(wait.group(1)) if wait else 30, 90) + 2)
        except (KeyError, IndexError, ValueError) as e:
            raise BatchFailed(f"no audio in the reply ({type(e).__name__}: {e})", lines_fault=True)
        except (urllib.error.URLError, TimeoutError, OSError) as e:
            raise BatchFailed(f"{type(e).__name__}: {e}")
    raise AssertionError("unreachable: the last try returns or raises")


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


# The most a piece is turned up: a quiet take is better left a little quiet than pushed into the limiter.
MAX_GAIN = 20.0


def gain_for(wav):
    """
    How many dB to turn a piece up or down to TARGET_LUFS. The loudness meter needs 0.4 s of sound, and
    reads a shorter piece (a stop sound like "b", about 0.3 s) as silence (-70 LUFS, so +55 dB): a short
    piece is measured looped end to end, and no piece goes up more than MAX_GAIN.
    """
    measured = wav
    if duration(wav) < 0.45:
        measured = wav.with_name("looped.wav")
        ffmpeg("-stream_loop", "4", "-i", str(wav), str(measured))
    return min(TARGET_LUFS - loudness(measured), MAX_GAIN)


def names(text):
    """The people's names in a line (Sipho, Dlamini; KNOWN_NAMES), as Whisper hotwords; None if there are none.
    Only names, never a line's other words, so the check isn't nudged towards hearing what it expects."""
    found = [w for w in dict.fromkeys(re.findall(r"\b[A-Z][a-z']+\b", text)) if w.lower() in KNOWN_NAMES]
    return " ".join(found) or None


# The people in the game's lines (Sipho, Thandi, Mrs Dlamini): set from the English texts as a run starts.
KNOWN_NAMES = set()


def known_names(all_texts):
    """
    Words written with a capital in the middle of a sentence somewhere in the English lines: the names.
    (A capital at the start of a sentence says nothing, so "Find" is never a name.)
    """
    found, lower = set(), set()
    for key, text in all_texts.items():
        if split(key)[0] != "en":
            continue
        for m in re.finditer(r"\b[A-Z][a-z']+\b", text):
            before = text[:m.start()].rstrip()
            after = text[m.end():]
            # "Numbers Island", "Language Island": an island's name, not a person's.
            if before and before[-1] not in ".!?:\"\u201c" and not re.match(r"\s+Island\b", after):
                found.add(m.group().lower())
        # A word also written in lower case somewhere ("letters", "blue", "go") is an ordinary word.
        lower.update(re.findall(r"\b[a-z][a-z']*\b", text))
    return {w for w in found - lower if re.sub(r"'s$", "", w) not in NOT_NAMES}


# Words written with a capital that aren't a person's name: a different day, month or family word must
# never pass as a name ("Sunday" for "Saturday", "Grand" for "Gran").
NOT_NAMES = {"i", "english", "afrikaans", "grade", "mr", "mrs", "ms", "mum", "dad", "gran", "granny", "gogo",
             "grandpa", "grandma", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday",
             "january", "february", "march", "april", "may", "june", "july", "august", "september", "october",
             "november", "december"}


# How Whisper (which spells the American way) may write a South African word: practices for practises,
# realize for realise, color for colour, center for centre, gray for grey. Nothing else counts as a spelling.
SPELLINGS = [(r"^practic", "practis"), (r"^licenc", "licens"), (r"iz(e|ed|es|ing|ation|ations)$", r"is\1"),
             (r"yz(e|ed|es|ing)$", r"ys\1"), (r"(?<=[a-z]{3})(?<!o)or(s|ed|ing|ful|ite|ites)?$", r"our\1"), (r"ter(s)?$", r"tre\1"),
             (r"^gray", "grey")]


def spelling(word):
    """A word with Whisper's American spellings made South African, so only spelling differences match."""
    for pattern, sa in SPELLINGS:
        word = re.sub(pattern, sa, word)
    return word


def letters(text):
    """The letters of joined words with doubled letters made single: "all together" and "altogether" match."""
    return re.sub(r"(.)\1+", r"\1", text)


def same_english(heard, text, names_=None):
    """
    Whether Whisper heard the English line: the same words in the same order, where a person's name of 4
    letters or more may differ as Whisper spells it (EN_NAME_CLOSE) and any other word only by one of
    Whisper's American spellings (SPELLINGS: "practices" for "practises", "color" for "colour"). Nothing
    missing or extra, though spaces may differ when the letters match ("all together" for "altogether").
    """
    got, want = words(heard), words(text)
    if "".join(got) == "".join(want):
        return True
    people = KNOWN_NAMES if names_ is None else names_
    for op, a1, a2, b1, b2 in difflib.SequenceMatcher(a=want, b=got, autojunk=False).get_opcodes():
        if op == "equal":
            continue
        if op != "replace":
            return False
        if a2 - a1 != b2 - b1:
            # Split or joined differently ("all together" for "altogether"): the letters must match closely.
            if letters("".join(want[a1:a2])) != letters("".join(got[b1:b2])):
                return False
            continue
        for w, g in zip(want[a1:a2], got[b1:b2]):
            if w in people and len(w) >= 4:
                if difflib.SequenceMatcher(a=w, b=g, autojunk=False).ratio() < EN_NAME_CLOSE:
                    return False
            elif spelling(w) != spelling(g):
                return False
    return True


AF_NUMBER_WORDS = set(AF_NUMBERS)


def af_sound(word):
    """An Afrikaans word as it sounds, so Whisper's Dutch spellings match it: "rachtig" and "regtig" both
    become "ragtig"/"regtig", "jij" becomes "jy", doubled letters count once. "een" (one) stays whole, so
    it never sounds like "en" (and)."""
    if word == "een":
        return word
    for dutch, af in DUTCH:
        word = word.replace(dutch, af)
    return re.sub(r"(.)\1+", r"\1", word)


def same_afrikaans(heard, text):
    """
    Whether Whisper heard the Afrikaans line. Whisper's Afrikaans is rough, so the words are compared as they
    sound (af_sound) and loosely: the whole line must be AF_LINE_CLOSE alike and each changed stretch of
    words AF_WORD_CLOSE alike ("Klik op die hond" for "kat" fails), while the numbers and the words for
    "not" must be exactly the same ("drie" for "vier", or an extra "nie", fails). Only a word of one or two
    letters ("'n", "en") may be missing or extra. Whisper writes Afrikaans "'n" (a) as Dutch "een", so a
    heard "een" may stand for an "'n", but every "een" (one) in the line must be heard.
    """
    got, want = [af_sound(w) for w in af_words(heard)], [af_sound(w) for w in af_words(text)]
    numbers = {af_sound(w) for w in AF_NUMBER_WORDS} - {"een"}
    if [w for w in got if w in numbers or w.isdigit()] != [w for w in want if w in numbers or w.isdigit()]:
        return False
    if not want.count("een") <= got.count("een") <= want.count("een") + want.count("n"):
        return False
    nots = {af_sound(w) for w in DUTCH_NOT}
    if [w for w in got if w in nots] != [w for w in want if w in nots]:
        return False
    alike = lambda a, b: difflib.SequenceMatcher(a=a, b=b, autojunk=False).ratio()
    if alike("".join(got), "".join(want)) < AF_LINE_CLOSE:
        return False
    for op, a1, a2, b1, b2 in difflib.SequenceMatcher(a=want, b=got, autojunk=False).get_opcodes():
        if op in ("delete", "insert"):
            if max(len(w) for w in (want[a1:a2] if op == "delete" else got[b1:b2])) > 2:
                return False
        elif op == "replace" and alike("".join(want[a1:a2]), "".join(got[b1:b2])) < AF_WORD_CLOSE:
            return False
    return True


# A number said on its own (num_21: "21"): Whisper writes it as digits or as words, and Afrikaans often
# in Dutch spelling ("twee en negentig", "eenentwintig", "zevenentachtig"), so it passes when the heard
# words are that exact number in any of these spellings. (Before 2 November 2026 every number over 20 was
# turned down, as "ninety two" never matched "92".)
EN_TENS = "twenty thirty forty fifty sixty seventy eighty ninety".split()
AF_TENS = "twintig dertig veertig vyftig sestig sewentig tagtig negentig".split()
NL_UNITS = ("nul een twee drie vier vijf zes zeven acht negen tien elf twaalf dertien veertien vijftien zestien "
            "zeventien achttien negentien").split()
NL_TENS = "twintig dertig veertig vijftig zestig zeventig tachtig negentig".split()


def number_spellings(lang, n):
    """Every way the number n (0-100) may be written, as letters only (af_sound for Afrikaans)."""
    if n == 100:
        forms = ["hundred", "onehundred", "ahundred"] if lang == "en" else ["honderd", "eenhonderd"]
    elif lang == "en":
        forms = [NUMBER_WORDS[n]] if n <= 20 else [EN_TENS[n // 10 - 2] + (NUMBER_WORDS[n % 10] if n % 10 else "")]
    else:
        # Afrikaans as af_number_words writes it (the game's own spelling), and Whisper's Dutch spelling.
        forms = [af_number_words(n).replace("-", ""),
                 NL_UNITS[n] if n < 20 else NL_TENS[n // 10 - 2] if n % 10 == 0
                 else NL_UNITS[n % 10] + "en" + NL_TENS[n // 10 - 2]]
    return {af_sound(f) if lang == "af" else f for f in forms}


def same_number(lang, heard, text):
    """Whether a clip whose text is a number (num_21: "21") was heard as exactly that number."""
    want = re.sub(r"[^0-9]", "", text)
    if not want or re.sub(r"[\s.,!?]", "", text) != want:
        return False
    got = heard.lower().replace("\u00eb", "e")
    digits = re.sub(r"[^0-9]", "", got)
    letters_ = re.sub(r"[^a-z]", "", got)
    if digits:
        return digits == want and not letters_
    if lang == "af":
        letters_ = af_sound(letters_)
    return letters_ in number_spellings(lang, int(want))


# Words Whisper adds round a single word ("an egg", Dutch "een" for Afrikaans "'n"): ignored there.
LONE_WORD_EXTRAS = {"a", "an", "the", "n", "een", "die", "de", "het"}


def one_word_alike(lang, heard, text):
    """
    Whether Whisper heard a one-word line (a word, a Language Island word, a story answer). On a word on its
    own Whisper often picks a near word ("and" for "ant", "call" for "car", "bowl" for "ball", "B" for
    "bee"), while Gemini, asked for one word, says that word: so the word may differ as a near sound
    (half its letters alike, as it sounds), or be heard as its first letter. Numbers must still match
    exactly, and there must be exactly one word.
    """
    want = line_words(lang, text)
    got = [w for w in line_words(lang, heard) if w not in LONE_WORD_EXTRAS]
    if len(want) != 1 or len(got) != 1:
        return False
    sound = af_sound if lang == "af" else spelling
    w, g = sound(want[0]), sound(got[0])
    numbers = {sound(n) for n in (AF_NUMBER_WORDS if lang == "af" else NUMBER_WORDS)}
    if w in numbers or g in numbers or w.isdigit() or g.isdigit():
        return w == g
    if len(g) == 1:
        return w.startswith(g) and len(w) <= 3
    return len(g) <= len(w) + 3 and difflib.SequenceMatcher(a=w, b=g, autojunk=False).ratio() >= 0.5


# Whisper's models: small.en hears English well; small heard long Afrikaans lines with numbers as Dutch
# ("Ian in Ian" for "Een en een", 0 of 18 passing on 31 October), so Afrikaans uses the bigger turbo model.
WHISPER_MODELS = {"en": "small.en", "af": "large-v3-turbo"}


class Listener:
    """Whisper (WHISPER_MODELS), each model loaded when first needed."""
    def __init__(self):
        self.models = {}

    def model(self, lang):
        if lang not in self.models:
            from faster_whisper import WhisperModel
            self.models[lang] = WhisperModel(WHISPER_MODELS[lang], device="cpu", compute_type="int8")
        return self.models[lang]

    def heard(self, path, lang="en", text=""):
        segments, _ = self.model(lang).transcribe(str(path), language=lang, beam_size=5, hotwords=names(text))
        return " ".join(seg.text for seg in segments).strip()

    def cut(self, wav, lines, lang="en"):
        """
        Where each line is in a batch recording: Whisper hears the batch with word timings, the heard
        words are lined up with the expected ones, and each line runs from halfway through the gap
        before it to halfway through the gap after it (or EDGE seconds past its own words, when the line
        next to it wasn't found). [(start, end) or None per line]; a line whose
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
            # Halfway into the gap to the line right next to it; if that line wasn't heard, EDGE past this
            # line's own words, so its piece never takes in the unheard line's audio.
            start = 0.0 if i == 0 else (last[i - 1] + first[i]) / 2 if i - 1 in last else max(0.0, first[i] - EDGE)
            end = (total if i == len(lines) - 1 else (last[i] + first[i + 1]) / 2 if i + 1 in first
                   else min(total, last[i] + EDGE))
            spans.append((start, end))
        return spans

    def ok(self, key, path, text):
        """(whether Whisper heard the line, what it heard)."""
        lang = split(key)[0]
        heard = self.heard(path, lang, text)
        same = same_english(heard, text) if lang == "en" else same_afrikaans(heard, text)
        return same or one_word_alike(lang, heard, text) or same_number(lang, heard, text), heard


def retakes(state, all_texts):
    """(waiting to be recorded, given up): retakes with no take for the ear yet, and those out of tries."""
    hold = held()
    live = [c for c in state["retake"] if c in all_texts and is_retake(state, c, all_texts[c]) and c not in state["review"]
            and not is_held(c, hold)]
    gave_up = [c for c in live if fails(state, c, all_texts[c]) >= GIVE_UP + EAR_TRIES]
    return [c for c in live if c not in gave_up], gave_up


def status(state, all_texts):
    # A retake whose line has changed since is recorded as a new line, straight into the game.
    state["retake"] = [c for c in state["retake"] if c in all_texts and state["done"].get(c) == all_texts[c]]
    done = sum(1 for c, t in all_texts.items() if state["done"].get(c) == t)
    stuck = [c for c in state["failed"] if c in all_texts and fails(state, c, all_texts[c]) >= GIVE_UP
             and state["review"].get(c, {}).get("text") != all_texts[c] and state["done"].get(c) != all_texts[c]]
    hold = held()
    waiting = sum(1 for c, t in all_texts.items() if state["done"].get(c) != t and is_held(c, hold))
    return len(all_texts), done, stuck, waiting


def report(state, all_texts):
    """The one status line --status and every run print."""
    total, done, stuck, waiting = status(state, all_texts)
    queued, gave_up = retakes(state, all_texts)
    print(f"Aoede: {done} of {total} clips done; {waiting} waiting for the owner's check; {len(state['review'])} takes "
          f"waiting for the owner's ear; {len(stuck)} need a person ({', '.join(stuck) or 'none'}); {len(queued)} retakes "
          f"to record; {len(gave_up)} retakes given up ({', '.join(gave_up) or 'none'}).", flush=True)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--status", action="store_true")
    ap.add_argument("--approve", nargs="+", metavar="CLIP",
                    help="the owner's ear passed these review takes or letter sounds/names: into the game")
    ap.add_argument("--redo", nargs="+", metavar="CLIP",
                    help="the owner's ear turned these review takes or letter sounds/names down: record again")
    ap.add_argument("--keep", nargs="+", metavar="CLIP",
                    help="the owner keeps the clip the game has now (an older narrator's): never recorded again")
    ap.add_argument("--retake", nargs="+", metavar="CLIP",
                    help="record these finished clips again; every new take waits for the owner's ear and the "
                         "current clip keeps playing until --approve (--keep turns the retake down)")
    a = ap.parse_args()
    state, all_texts = load_state(), texts()
    if a.retake:
        if a.keep or a.approve or a.redo:
            sys.exit("--retake goes on its own: run --keep, --approve or --redo first, then --retake.")
        a.retake = list(dict.fromkeys(a.retake))
        hold = held()
        for clip in a.retake:
            if clip not in all_texts:
                sys.exit(f"{clip} is not a clip the game needs.")
            if is_held(clip, hold):
                sys.exit(f"{clip} is held (voice-lab/aoede-hold.txt): it wouldn't be recorded.")
            if state["done"].get(clip) != all_texts[clip]:
                sys.exit(f"{clip} isn't finished yet: the next run records it anyway.")
            lang, name = split(clip)
            if clip in state["review"] or (LANGS[lang]["phonics"] / f"{name}.m4a").exists():
                sys.exit(f"{clip} has a take waiting for the owner's ear: --approve, --redo or --keep it first.")
        for clip in a.retake:
            lang, name = split(clip)
            state["failed"].pop(clip, None)
            state["failed_text"].pop(clip, None)
            if name.startswith(("snd_", "name_")):
                # Letter sounds and names always wait in aoede_phonics/ for the ear: record one again as new
                # (the game's clip plays until --approve moves the new take in).
                state["done"].pop(clip, None)
            elif clip not in state["retake"]:
                # "done" stays: the clip in the game is fine and keeps playing until the owner approves a retake.
                state["retake"].append(clip)
        save_state(state)
        letters = sum(1 for c in a.retake if split(c)[1].startswith(("snd_", "name_")))
        print(f"To record again for the owner's ear: {len(a.retake)} ({letters} letter sounds or names back in the "
              f"phonics queue, counted as not done until approved).")
        a.status = True
    if a.keep:
        for clip in a.keep:
            if clip not in all_texts:
                sys.exit(f"{clip} isn't a clip the game needs.")
            if clip in state["review"]:  # a waiting take (a retake turned down, say) is thrown away
                review_path(clip).unlink(missing_ok=True)
            took(state, clip, all_texts[clip])  # done as it is: the clip in Resources/Audio stays
        save_state(state)
        print(f"Kept {len(a.keep)} as they are.")
        a.status = True
    if a.approve or a.redo:
        if set(a.approve or []) & set(a.redo or []):
            sys.exit(f"Both approved and to redo: {', '.join(sorted(set(a.approve) & set(a.redo)))}.")
        def waiting(clip):
            """Where the clip's take waits for the owner's ear: a review take, or a letter sound or name."""
            lang, name = split(clip)
            if clip in state["review"]:
                return review_path(clip)
            phonics = LANGS[lang]["phonics"] / f"{name}.m4a"
            return phonics if phonics.exists() else None
        for clip in (a.approve or []) + (a.redo or []):
            if waiting(clip) is None:
                sys.exit(f"{clip} has no take waiting for the owner's ear.")
        for clip in a.approve or []:
            lang, name = split(clip)
            take = waiting(clip)
            LANGS[lang]["audio"].mkdir(parents=True, exist_ok=True)
            take.replace(LANGS[lang]["audio"] / f"{name}.m4a")
            if clip in state["review"]:
                took(state, clip, state["review"][clip]["text"])
        for clip in a.redo or []:
            waiting(clip).unlink()
            state["review"].pop(clip, None)
            if clip not in state["retake"]:
                state["done"].pop(clip, None)  # recorded again, in a fresh batch (a retake keeps its clip)
            state["failed"].pop(clip, None)
            state["failed_text"].pop(clip, None)
        save_state(state)
        print(f"Approved {len(a.approve or [])}, to record again {len(a.redo or [])}.")
        a.status = True
    KNOWN_NAMES.update(known_names(all_texts))
    report(state, all_texts)
    if a.status:
        return
    key = os.environ.get("GEMINI_API_KEY")
    if not key:
        sys.exit("No GEMINI_API_KEY secret.")

    hold = held()
    todo = [c for c in sorted(all_texts, key=lambda c: (priority(c), c)) if (state["done"].get(c) != all_texts[c] or is_retake(state, c, all_texts[c]))
            and not is_held(c, hold)
            and state["review"].get(c, {}).get("text") != all_texts[c]  # a take already waits for the owner's ear
            and fails(state, c, all_texts[c]) < GIVE_UP + EAR_TRIES]
    # Fresh clips first in big batches, most-heard first, then retries in small ones; English and
    # Afrikaans take turns, except that step 0 (the stories) goes before any turn-taking. Each batch is
    # one style.
    queues = {lang: [] for lang in LANGS}
    first = []
    sorted_todo = {}
    for c in todo:
        bucket = (bool(fails(state, c, all_texts[c])), priority(c), group(c), split(c)[0])
        sorted_todo.setdefault(bucket, []).append(c)
    for retry in (False, True):
        for step in range(0, 10):
            for style in ("sparkle", "clear", "big", "words", "names", "phonics"):
                for lang in LANGS:
                    these = sorted_todo.get((retry, step, style, lang), [])
                    size = batch_size(style, retry)
                    chunks = [(lang, style, these[i:i + size]) for i in range(0, len(these), size)]
                    if step == 0:
                        first += chunks
                    else:
                        queues[lang] += chunks
    batches = first + [b for turn in itertools.zip_longest(*queues.values()) for b in turn if b]
    listener = Listener()
    requests = errors = 0
    setup_error = False
    started = time.monotonic()
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        for lang, style, clips in batches:
            if requests >= MAX_REQUESTS:
                print(f"Stopping after {requests} requests (MAX_REQUESTS).", flush=True)
                break
            if time.monotonic() - started > MAX_MINUTES * 60:
                print(f"Stopping after {MAX_MINUTES} minutes, so the clips are committed in time.", flush=True)
                break
            if style == "phonics":
                known = sounds(lang)
                lines = [known.get(split(c)[1], (all_texts[c], ""))[0] for c in clips]
                hints = [known.get(split(c)[1], ("", ""))[1] for c in clips]
            else:
                lines, hints = [said(c, all_texts[c]) for c in clips], ()
            try:
                pcm = speak(key, lang, style, lines, hints)
            except DailyLimit as e:
                print(f"Gemini's daily limit is used up ({e}); the next run carries on.", flush=True)
                break
            except SetupError as e:
                # Not the lines' fault: no failed takes, and the run fails so a person sees it.
                print(f"Stopping: the key or the model needs a person ({e}).", flush=True)
                setup_error = True
                break
            except BatchFailed as e:
                # A reply that was the lines' fault counts a failed take for each, so they move to small
                # retry batches and then GIVE_UP; a network or server error counts nothing. Three in a row
                # and the run stops for today.
                if e.lines_fault:
                    for clip in clips:
                        failed_take(state, clip, all_texts[clip])
                    save_state(state)
                requests += 1
                errors += 1
                print(f"Request {requests} failed ({e}); {'stopping' if errors >= 3 else 'going on'}.", flush=True)
                if errors >= 3:
                    break
                continue
            errors = 0
            requests += 1
            raw = tmp / "batch.wav"
            (tmp / "batch.pcm").write_bytes(pcm)
            ffmpeg("-f", "s16le", "-ar", "24000", "-ac", "1", "-i", str(tmp / "batch.pcm"), str(raw))
            if style in ("phonics", "names"):
                pieces = silence_cut(raw, len(clips)) or [None] * len(clips)
            elif style == "words":
                # Words are said one to a line with a pause between: cut at the pauses, as Whisper places a
                # word on its own badly; when the pauses don't fit, fall back to Whisper's word timings.
                pieces = silence_cut(raw, len(clips)) or listener.cut(raw, lines, lang)
            else:
                pieces = listener.cut(raw, lines, lang)
            good = 0
            for clip, span in zip(clips, pieces):
                if span is None:
                    failed_take(state, clip, all_texts[clip])
                    print(f"  {clip}: not heard in the batch", flush=True)
                    continue
                start, end = span
                name = split(clip)[1]
                piece, trimmed, out = tmp / "piece.wav", tmp / "trimmed.wav", tmp / f"{name}.m4a"
                ffmpeg("-i", str(raw), "-ss", f"{start:.3f}", "-to", f"{end:.3f}", str(piece))
                ffmpeg("-i", str(piece), "-af", TRIM, "-ar", "44100", "-ac", "1", str(trimmed))
                # A burst of noise or of the next line at the end of the cut ("static at the end", owner, 5 Nov).
                clip_clean.drop_edge_bursts(trimmed, heads=False)
                gain = gain_for(trimmed)
                ffmpeg("-i", str(trimmed), "-af", f"volume={gain:.2f}dB,{finish(style)}", "-ar", "44100", "-ac", "1",
                       "-c:a", "aac", "-b:a", "128k", "-metadata", f"comment={clip_clean.MARK}", str(out))
                if style in ("phonics", "names"):
                    # No machine check for a pure sound or a letter name: it waits for the owner's ear. But a take
                    # where Whisper hears several words is Gemini reading its instructions aloud: thrown away.
                    heard = listener.heard(out, lang)
                    # Whisper makes up a few words for a clip with none ("Thank you for watching"), so only the
                    # instructions' own words count, or far more than a letter could hold.
                    if len(heard) > 60 or reads_instructions(heard, all_texts[clip], lang, style):
                        failed_take(state, clip, all_texts[clip])
                        print(f"  {clip}: heard {heard!r}, a letter sound or name with words read out", flush=True)
                        continue
                    LANGS[lang]["phonics"].mkdir(parents=True, exist_ok=True)
                    out.replace(LANGS[lang]["phonics"] / f"{name}.m4a")
                    took(state, clip, all_texts[clip])
                    good += 1
                    continue
                ok, heard = listener.ok(clip, out, all_texts[clip])
                if ok:
                    if is_retake(state, clip, all_texts[clip]):
                        # A retake (--retake): the owner hears it first; the clip in the game keeps playing till then.
                        to_review(state, clip, out, all_texts[clip], heard)
                    else:
                        LANGS[lang]["audio"].mkdir(parents=True, exist_ok=True)
                        out.replace(LANGS[lang]["audio"] / f"{name}.m4a")
                        took(state, clip, all_texts[clip])
                    good += 1
                else:
                    # Past EAR_FROM failed takes the take waits for the owner's ear instead of being thrown away,
                    # unless Gemini plainly read more than the line (its instructions, out loud).
                    if (fails(state, clip, all_texts[clip]) >= EAR_FROM[lang]
                            and not reads_instructions(heard, all_texts[clip], lang, style)):
                        to_review(state, clip, out, all_texts[clip], heard)
                    failed_take(state, clip, all_texts[clip])
                    print(f"  {clip}: heard {heard!r}, wanted {all_texts[clip]!r}", flush=True)
            save_state(state)
            print(f"Request {requests}: {good} of {len(clips)} {lang} {style} clips kept.", flush=True)
    report(state, all_texts)
    if setup_error:
        sys.exit(1)


if __name__ == "__main__":
    main()
