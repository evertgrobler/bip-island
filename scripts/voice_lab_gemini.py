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
OUT = ROOT / "voice-lab" / "out_gemini2"
MODELS = ["gemini-3.8-flash-tts"]  # the model Aoede was picked on
VOICES = ["Kore", "Aoede", "Leda", "Sulafat"]
STYLE = ("Say this in Afrikaans, as a warm, friendly South African primary school teacher talking to "
         "young children: clear, gentle and unhurried, with natural Afrikaans pronunciation.")


def lines():
    for row in (ROOT / "voice-lab" / "lines_af.tsv").read_text(encoding="utf-8").splitlines():
        if row.strip() and not row.startswith("#"):
            yield row.split("\t", 1)


def speak(key, model, voice, text, style=STYLE):
    body = {"contents": [{"parts": [{"text": f"{style}\n\n{text}"}]}],
            "generationConfig": {"responseModalities": ["AUDIO"],
                                 "speechConfig": {"voiceConfig": {"prebuiltVoiceConfig": {"voiceName": voice}}}}}
    req = urllib.request.Request(
        f"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent",
        data=json.dumps(body).encode(), headers={"x-goog-api-key": key, "Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=180) as r:
        data = json.load(r)
    return base64.b64decode(data["candidates"][0]["content"]["parts"][0]["inlineData"]["data"])


def takes():
    for row in (ROOT / "voice-lab" / "gemini_takes.tsv").read_text(encoding="utf-8").splitlines():
        if row.strip() and not row.startswith("#"):
            yield row.split("\t", 2)


def main():
    """One request per take in voice-lab/gemini_takes.tsv, with Aoede (the owner's pick, 18 October)."""
    key = os.environ.get("GEMINI_API_KEY")
    if not key:
        print("No GEMINI_API_KEY secret yet; nothing to do.")
        return
    with tempfile.TemporaryDirectory() as tmp:
        for name, style, text in takes():
            try:
                audio = speak(key, MODELS[0], "Aoede", text, style)
            except urllib.error.HTTPError as e:
                print(f"{name}: {e.code} {e.read()[:300]!r}", flush=True)
                if e.code == 429:
                    sys.exit("Gemini's free daily limit is used up; try again tomorrow.")
                raise
            pcm = Path(tmp) / "a.pcm"
            pcm.write_bytes(audio)
            dest = OUT / f"{name}.mp3"
            dest.parent.mkdir(parents=True, exist_ok=True)
            subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "s16le", "-ar", "24000", "-ac", "1", "-i", str(pcm),
                            "-b:a", "160k", str(dest)], check=True)
            print(f"{name}: done", flush=True)


if __name__ == "__main__":
    main()
