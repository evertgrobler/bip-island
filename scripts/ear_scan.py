"""The free ear scan: open-source listening tools measure every narrator clip, so the owner hears only the odd ones out.

The owner, 9 Nov 2026: some English lines sounded British, Claude can't hear, and it must cost nothing. This runs on
GitHub's own runner with free open-source models (no key, no account, nothing sent to a company):

- accent: SpeechBrain's CommonAccent ECAPA model (English accents, trained on Mozilla Common Voice) gives each English
  clip a probability per accent. It has no South African label and never heard a synthetic voice, so it is a ranking,
  not a verdict: every clip is the same narrator, so the ones that lean most towards "england" stand out.
- pronunciation: wav2vec2's phoneme model (facebook/wav2vec2-lv-60-espeak-cv-ft) writes down the sounds it hears, and
  espeak-ng writes the sounds the words should have (English or Afrikaans); a low match marks a wrong, missing or
  swapped sound ("pee" for "P", 57 for 75). Accents shift vowels a little, so only big gaps count.
- sound: the quietest stretch's level (hiss or static) and the last 60 ms's level (a burst or a cut-off end).

Results go to voice-lab/ear_scan.json, keyed by clip, with the clip's hash so a clip is measured again only when it
changes. scripts/ear_page.py turns them into the owner's listening page. Nothing in the game changes because of it.

  python3 scripts/ear_scan.py            # in the Ear check workflow
"""
import difflib, hashlib, json, os, re, subprocess, sys, tempfile, time
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import gemini_narrator as g  # clip texts, languages and folders

RESULTS = g.ROOT / "voice-lab" / "ear_scan.json"
RATE = 16000
ACCENT_MODEL = "Jzuluaga/accent-id-commonaccent_ecapa"
PHONE_MODEL = "facebook/wav2vec2-lv-60-espeak-cv-ft"
TIME_LIMIT = float(os.environ.get("TIME_LIMIT_MINUTES", "150")) * 60
MARKS = re.compile(r"[ˈˌːˑ‿.\s-]")


def read(path):
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", str(path), "-ac", "1", "-ar", str(RATE), "-f", "f32le", "-"],
                         capture_output=True, check=True).stdout
    return np.frombuffer(raw, np.float32).copy()


def sound(x):
    """(floor_db, tail_db): the quietest tenth of 10 ms frames, and the last 60 ms, in dB below full scale."""
    n = RATE // 100
    frames = len(x) // n
    if frames < 10:
        return None, None
    db = 20 * np.log10(np.sqrt((x[: frames * n].reshape(frames, n) ** 2).mean(1)) + 1e-9)
    return round(float(np.percentile(db, 10)), 1), round(float(db[-6:].mean()), 1)


def expected_phones(text, lang):
    voice = "af" if lang == "af" else "en"
    out = subprocess.run(["espeak-ng", "-q", "--ipa", "-v", voice, text], capture_output=True, text=True).stdout
    return MARKS.sub("", out)


def match(heard, wanted):
    if not heard or not wanted:
        return None
    return round(difflib.SequenceMatcher(None, heard, wanted).ratio(), 3)


def main():
    import torch
    from speechbrain.inference.classifiers import EncoderClassifier
    from transformers import AutoProcessor, Wav2Vec2ForCTC

    torch.set_num_threads(os.cpu_count() or 2)
    results = json.loads(RESULTS.read_text()) if RESULTS.exists() else {}
    texts = g.texts()
    todo = []
    for clip in sorted(texts, key=lambda c: (g.split(c)[0] != "en", c)):
        lang, name = g.split(clip)
        path = g.LANGS[lang]["audio"] / f"{name}.m4a"
        if not path.exists():
            continue
        digest = hashlib.sha1(path.read_bytes()).hexdigest()
        if results.get(clip, {}).get("sha") != digest:
            todo.append((clip, lang, name, path, digest))
    print(f"Ear scan: {len(results)} measured before, {len(todo)} to measure.", flush=True)
    if not todo:
        return

    accent = EncoderClassifier.from_hparams(source=ACCENT_MODEL, savedir=str(Path.home() / ".cache" / "accent-ecapa"))
    labels = [accent.hparams.label_encoder.decode_ndim(i) for i in range(len(accent.hparams.label_encoder))]
    processor = AutoProcessor.from_pretrained(PHONE_MODEL)
    phones = Wav2Vec2ForCTC.from_pretrained(PHONE_MODEL).eval()
    print(f"Accent labels: {', '.join(labels)}", flush=True)

    started = time.monotonic()
    for i, (clip, lang, name, path, digest) in enumerate(todo, 1):
        if time.monotonic() - started > TIME_LIMIT:
            print(f"Stopping for time after {i - 1} clips; the next run carries on.", flush=True)
            break
        x = read(path)
        floor, tail = sound(x)
        row = {"sha": digest, "text": texts[clip], "floor_db": floor, "tail_db": tail}
        wav = torch.from_numpy(x).unsqueeze(0)
        if lang == "en" and not name.startswith("snd_") and len(x) > RATE // 4:
            with torch.no_grad():
                out = accent.classify_batch(wav)[0][0]
            probs = out.exp() if float(out.max()) <= 0 else torch.softmax(out, 0)
            probs = probs / probs.sum()
            top = sorted(zip(labels, probs.tolist()), key=lambda p: -p[1])
            row["accent"] = {k: round(v, 3) for k, v in top[:4]}
        if not name.startswith("snd_") and len(x) > RATE // 5:
            with torch.no_grad():
                logits = phones(processor(x, sampling_rate=RATE, return_tensors="pt").input_values).logits
            heard = MARKS.sub("", processor.batch_decode(logits.argmax(-1))[0])
            wanted = expected_phones(g.said(clip, texts[clip]), lang)
            row.update({"heard_phones": heard, "wanted_phones": wanted, "phone_match": match(heard, wanted)})
        results[clip] = row
        if i % 100 == 0 or i == len(todo):
            RESULTS.write_text(json.dumps(results, indent=1, sort_keys=True, ensure_ascii=False) + "\n")
            print(f"{i} of {len(todo)} measured.", flush=True)
    RESULTS.write_text(json.dumps(results, indent=1, sort_keys=True, ensure_ascii=False) + "\n")
    print(f"Ear scan: {len(results)} clips measured.", flush=True)


if __name__ == "__main__":
    main()
