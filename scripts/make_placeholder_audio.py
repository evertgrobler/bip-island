#!/usr/bin/env python3
"""Make PLACEHOLDER voice clips for any clip in audio/script.csv that has no real recording yet.

Runs on the macOS build machine (GitHub Actions). It uses the Mac's built-in `say` voice, so the
placeholders sound robotic on purpose. When a real ElevenLabs clip is committed to Resources/Audio/
under the same file name, it is used instead and no placeholder is made for it.

Placeholders are listed in Resources/Audio/PLACEHOLDERS.txt, which the parent area reads to show how
many clips are still placeholders.
"""
import csv
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCRIPT = os.path.join(ROOT, "audio", "script.csv")
AUDIO_DIR = os.path.join(ROOT, "Resources", "Audio")
LIST = os.path.join(AUDIO_DIR, "PLACEHOLDERS.txt")

# Tessa is the macOS South African English voice; fall back to others if it isn't installed.
PREFERRED_VOICES = ["Tessa", "Karen", "Moira", "Samantha"]


def pick_voice():
    try:
        listing = subprocess.run(["say", "-v", "?"], capture_output=True, text=True, check=True).stdout
    except (OSError, subprocess.CalledProcessError):
        return None
    installed = {line.split()[0] for line in listing.splitlines() if line.strip()}
    for voice in PREFERRED_VOICES:
        if voice in installed:
            return voice
    return None


def main():
    if shutil.which("say") is None or shutil.which("afconvert") is None:
        print("::warning::'say'/'afconvert' not found (not a Mac?). No placeholder clips made; the game will play silence for missing clips.")
        return 0

    voice = pick_voice()
    print(f"Placeholder voice: {voice or 'system default'}")
    os.makedirs(AUDIO_DIR, exist_ok=True)

    made, real, failed = [], [], []
    with open(SCRIPT, newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            name = row["file"].strip()
            if not name:
                continue
            target = os.path.join(AUDIO_DIR, name)
            if os.path.exists(target):
                real.append(name)
                continue
            text = (row.get("placeholder") or "").strip() or row["text"].strip()
            with tempfile.TemporaryDirectory() as tmp:
                aiff = os.path.join(tmp, "clip.aiff")
                cmd = ["say", "-r", "160", "-o", aiff, text]
                if voice:
                    cmd[1:1] = ["-v", voice]
                try:
                    subprocess.run(cmd, check=True)
                    subprocess.run(["afconvert", aiff, target, "-f", "m4af", "-d", "aac"], check=True)
                    made.append(name)
                except subprocess.CalledProcessError as error:
                    failed.append(name)
                    print(f"::warning::Could not make placeholder {name}: {error}")

    with open(LIST, "w", encoding="utf-8") as f:
        f.write("# Clips in this build that are computer-voice PLACEHOLDERS, not the real narrator.\n")
        for name in made:
            f.write(name + "\n")

    print(f"Real clips: {len(real)}  Placeholders made: {len(made)}  Failed (will be silent): {len(failed)}")
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as f:
            f.write(f"**Voice clips:** {len(real)} real, {len(made)} placeholder (macOS `say`, voice {voice or 'default'}), {len(failed)} missing.\n\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
