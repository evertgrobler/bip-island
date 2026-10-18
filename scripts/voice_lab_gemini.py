#!/usr/bin/env python3
"""Voice lab: Gemini text-to-speech in Afrikaans (a test; nothing ships).

Gemini TTS can't copy a voice, but it has about 30 ready-made voices and takes a spoken style
direction, and it lists Afrikaans among its languages. This speaks voice-lab/lines_af.tsv with a
few warm female voices. Needs GEMINI_API_KEY (a GitHub Actions secret; never committed).
Writes voice-lab/out_gemini/<voice>.mp3, all five lines in one clip.
"""
import base64, json, os, subprocess, sys, tempfile, urllib.error, urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "voice-lab" / "out_gemini"
MODELS = ["gemini-3.8-flash-tts", "gemini-2.5-pro-preview-tts", "gemini-2.5-flash-preview-tts"]
VOICES = ["Kore", "Aoede", "Leda", "Sulafat"]
STYLE = ("Say this in Afrikaans, as a warm, friendly South African primary school teacher talking to "
         "young children: clear, gentle and unhurried, with natural Afrikaans pronunciation.")


def lines():
    for row in (ROOT / "voice-lab" / "lines_af.tsv").read_text(encoding="utf-8").splitlines():
        if row.strip() and not row.startswith("#"):
            yield row.split("\t", 1)


def speak(key, model, voice, text):
    body = {"contents": [{"parts": [{"text": f"{STYLE}\n\n{text}"}]}],
            "generationConfig": {"responseModalities": ["AUDIO"],
                                 "speechConfig": {"voiceConfig": {"prebuiltVoiceConfig": {"voiceName": voice}}}}}
    req = urllib.request.Request(
        f"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent",
        data=json.dumps(body).encode(), headers={"x-goog-api-key": key, "Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=180) as r:
        data = json.load(r)
    return base64.b64decode(data["candidates"][0]["content"]["parts"][0]["inlineData"]["data"])


def main():
    """One request per voice, all five lines in it (the free tier allows only about 10 a day)."""
    key = os.environ.get("GEMINI_API_KEY")
    if not key:
        print("No GEMINI_API_KEY secret yet; nothing to do.")
        return
    script = "\n\n".join(text for _, text in lines())
    models = list(MODELS)
    with tempfile.TemporaryDirectory() as tmp:
        for voice in VOICES:
            while models:
                try:
                    audio = speak(key, models[0], voice, script)
                    break
                except urllib.error.HTTPError as e:
                    detail = e.read()[:300]
                    print(f"{models[0]} / {voice}: {e.code} {detail!r}", flush=True)
                    if e.code == 429:
                        sys.exit("Gemini's free daily limit is used up; try again tomorrow.")
                    models.pop(0)  # this key can't use that model: try the next one
            else:
                sys.exit("No Gemini TTS model worked with this key.")
            pcm = Path(tmp) / "a.pcm"
            pcm.write_bytes(audio)
            dest = OUT / f"{voice}.mp3"
            dest.parent.mkdir(parents=True, exist_ok=True)
            subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "s16le", "-ar", "24000", "-ac", "1", "-i", str(pcm),
                            "-b:a", "160k", str(dest)], check=True)
            print(f"{voice}: done with {models[0]}", flush=True)


if __name__ == "__main__":
    main()
