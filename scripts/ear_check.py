"""The ear check: a listening AI hears every narrator clip and notes its accent, pronunciation and sound.

The owner, 9 Nov 2026: some English lines still sounded British, and Claude can't hear. Gemini can take audio in and
answer about it in writing, so this sends the clips to it in small batches, each with the words it should say, and
asks per clip: which accent it hears, how South African (or, for Afrikaans, how first-language Afrikaans) it sounds,
any word said wrongly or missing, and any static, click, cut-off or stray sound. The answers go to
voice-lab/ear_check.json, keyed by clip, with the clip's hash, so a clip is heard again only when it changes.

It uses only a free key (GEMINI_FREE_KEY, from a Google project with no billing), so it never costs money.
It runs in the Ear check workflow (push voice-lab/ear-run.txt on a claude/ branch); MAX_REQUESTS caps a day's
requests and the free daily limit ends a run early. scripts/ear_page.py (in a session) makes the listening page.
It is a second opinion for the owner's ear, never a judge: nothing in the game changes because of it.

  GEMINI_API_KEY=... MAX_REQUESTS=50 python3 scripts/ear_check.py
"""
import base64, hashlib, json, os, re, sys, time, urllib.error, urllib.request
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import gemini_narrator as g  # clip texts, languages and folders

RESULTS = g.ROOT / "voice-lab" / "ear_check.json"
BATCH = 10
# A model that takes audio in and writes text; LISTEN_MODEL overrides. If it is missing, the newest flash model
# that can generate content (and isn't a speech, image or live model) is used.
PREFERRED = os.environ.get("LISTEN_MODEL", "")

ASK = """You are checking narrator voice clips for a South African children's learning game. Listen to each clip
carefully. Each clip comes after a line giving its number, its language and the exact words it should say.

For every clip, answer:
- accent: the accent you actually hear, one of "south_african_english", "british", "american", "australian",
  "afrikaans", "dutch", "other", "unclear".
- local_score: 0 to 10. For an English clip, how clearly South African English it sounds (10 = clearly South African,
  0 = clearly British or American). For an Afrikaans clip, how much it sounds like a first-language South African
  Afrikaans speaker (10 = native Afrikaans, 0 = English or Dutch accent).
- words_ok: true only if it says exactly the expected words, nothing missing, nothing extra, in the right order.
- word_problem: if not, what is wrong (a missing, extra, swapped or wrong word, or numbers in the wrong order), else "".
- pronunciation: any word pronounced wrongly or oddly for a South African speaker, else "".
- sound_problem: static, hiss, a click, a cut-off start or end, a stray sound or another voice, else "".
- note: one short sentence on anything else a parent would notice, else "".

Letter-sound clips (expected text like "the sound s") are one pure speech sound, not a word: judge only whether it
is that clean sound. Be strict and honest; most clips will be fine. Answer with JSON only."""

SCHEMA = {
    "type": "ARRAY",
    "items": {"type": "OBJECT", "properties": {
        "clip": {"type": "INTEGER"}, "accent": {"type": "STRING"}, "local_score": {"type": "INTEGER"},
        "words_ok": {"type": "BOOLEAN"}, "word_problem": {"type": "STRING"}, "pronunciation": {"type": "STRING"},
        "sound_problem": {"type": "STRING"}, "note": {"type": "STRING"}},
        "required": ["clip", "accent", "local_score", "words_ok", "word_problem", "pronunciation", "sound_problem",
                     "note"]}}


class DailyLimit(Exception):
    pass


def call(key, path, body=None):
    req = urllib.request.Request(f"https://generativelanguage.googleapis.com/v1beta/{path}",
                                 data=json.dumps(body).encode() if body else None,
                                 headers={"x-goog-api-key": key, "Content-Type": "application/json"})
    for attempt in range(4):
        try:
            with urllib.request.urlopen(req, timeout=300) as r:
                return json.load(r)
        except urllib.error.HTTPError as e:
            text = e.read().decode("utf-8", "replace")
            if e.code != 429:
                raise RuntimeError(f"Gemini said {e.code}: {text[:300]}")
            wait = re.search(r'"retryDelay":\s*"(\d+)(?:\.\d+)?s"', text)
            if "PerDay" in text or attempt == 3 or not ("PerMinute" in text or wait):
                raise DailyLimit(text[:300])
            time.sleep(min(int(wait.group(1)) if wait else 30, 90) + 2)
    raise AssertionError("unreachable")


def pick_model(key):
    if PREFERRED:
        return PREFERRED
    names = []
    for m in call(key, "models?pageSize=1000").get("models", []):
        name = m["name"].split("/", 1)[1]
        if ("generateContent" in m.get("supportedGenerationMethods", []) and "flash" in name
                and not re.search(r"tts|image|live|audio|embed|lite|exp|preview", name)):
            names.append(name)
    if not names:
        sys.exit("No listening model found: set LISTEN_MODEL.")
    version = lambda n: [int(x) for x in re.findall(r"\d+", n)]
    return max(names, key=version)


def clip_path(clip):
    lang, name = g.split(clip)
    return g.LANGS[lang]["audio"] / f"{name}.m4a"


def expected(clip, text):
    lang, name = g.split(clip)
    language = "Afrikaans" if lang == "af" else "South African English"
    if name.startswith("snd_"):
        return f"{language}, the sound {name[4:]} (one pure speech sound, not the letter name)"
    return f"{language}: \"{text}\""


def main():
    key = os.environ.get("GEMINI_API_KEY") or sys.exit("No GEMINI_FREE_KEY secret (a key from a Google project with no billing).")
    limit = int(os.environ.get("MAX_REQUESTS") or 50)
    results = json.loads(RESULTS.read_text()) if RESULTS.exists() else {}
    texts = g.texts()
    todo = []
    # English first (the owner's complaint), and in each language Bip's spoken lines before single words.
    for clip in sorted(texts, key=lambda c: (g.split(c)[0] != "en", not g.split(c)[1].startswith(("vo_", "hint_", "praise_")), c)):
        path = clip_path(clip)
        if not path.exists():
            continue
        digest = hashlib.sha1(path.read_bytes()).hexdigest()
        if results.get(clip, {}).get("sha") != digest:
            todo.append((clip, path, digest))
    print(f"Ear check: {len(results)} heard before, {len(todo)} to hear.", flush=True)
    if not todo:
        return
    model = pick_model(key)
    print(f"Listening with {model}.", flush=True)
    requests = 0
    for start in range(0, len(todo), BATCH):
        if requests >= limit:
            print(f"Stopping after {requests} requests (MAX_REQUESTS).", flush=True)
            break
        batch = todo[start:start + BATCH]
        parts = [{"text": ASK}]
        for n, (clip, path, _) in enumerate(batch, 1):
            parts.append({"text": f"Clip {n}. {expected(clip, texts[clip])}"})
            parts.append({"inlineData": {"mimeType": "audio/mp4", "data": base64.b64encode(path.read_bytes()).decode()}})
        body = {"contents": [{"parts": parts}],
                "generationConfig": {"responseMimeType": "application/json", "responseSchema": SCHEMA,
                                     "temperature": 0}}
        try:
            data = call(key, f"models/{model}:generateContent", body)
            requests += 1
            answers = json.loads(data["candidates"][0]["content"]["parts"][0]["text"])
        except DailyLimit as e:
            print(f"Daily limit reached after {requests} requests: {e}", flush=True)
            break
        except (RuntimeError, KeyError, IndexError, ValueError) as e:
            requests += 1
            print(f"Batch from {batch[0][0]} failed: {e}", flush=True)
            continue
        got = 0
        for a in answers:
            n = a.get("clip")
            if not isinstance(n, int) or not 1 <= n <= len(batch):
                continue
            clip, _, digest = batch[n - 1]
            a.pop("clip")
            results[clip] = {**a, "sha": digest, "text": texts[clip], "model": model}
            got += 1
        RESULTS.write_text(json.dumps(results, indent=1, sort_keys=True, ensure_ascii=False) + "\n")
        print(f"Request {requests}: {got} of {len(batch)} clips heard.", flush=True)
    left = sum(1 for c, _, d in todo if results.get(c, {}).get("sha") != d)
    print(f"Ear check: {len(results)} clips heard; {left} still to hear.", flush=True)


if __name__ == "__main__":
    main()
