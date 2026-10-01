#!/usr/bin/env python3
"""Copies the shared game assets into the Godot project (godot/assets/, not committed).

- Resources/Fonts/*.ttf      -> godot/assets/fonts/
- Content/**/*.json          -> godot/assets/content/
- Resources/Audio/*.m4a      -> godot/assets/audio/*.ogg  (Godot can't play .m4a; needs ffmpeg)

Run it before opening, testing or exporting the Godot project. Clips already converted and
newer than their source are skipped, so a second run is quick.
"""
import shutil
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "godot" / "assets"


def copy_tree(src: Path, dst: Path, pattern: str) -> int:
    count = 0
    for path in sorted(src.rglob(pattern)):
        target = dst / path.relative_to(src)
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, target)
        count += 1
    return count


def convert(clip: Path, audio_out: Path) -> str:
    target = audio_out / (clip.stem + ".ogg")
    if target.exists() and target.stat().st_mtime >= clip.stat().st_mtime:
        return "skipped"
    result = subprocess.run(
        ["ffmpeg", "-nostdin", "-loglevel", "error", "-y", "-i", str(clip),
         "-ac", "1", "-c:a", "libvorbis", "-q:a", "5", str(target)],
        capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(f"ffmpeg failed on {clip.name}: {result.stderr.strip()}")
    return "converted"


def main() -> int:
    if shutil.which("ffmpeg") is None:
        print("ffmpeg is needed to convert the voice clips (apt install ffmpeg / brew install ffmpeg).")
        return 1

    fonts = copy_tree(ROOT / "Resources" / "Fonts", OUT / "fonts", "*.ttf")
    content_out = OUT / "content"
    if content_out.exists():
        shutil.rmtree(content_out)
    content = copy_tree(ROOT / "Content", content_out, "*.json")

    audio_out = OUT / "audio"
    audio_out.mkdir(parents=True, exist_ok=True)
    clips = sorted((ROOT / "Resources" / "Audio").glob("*.m4a"))
    wanted = {clip.stem + ".ogg" for clip in clips}
    for stale in audio_out.glob("*.ogg"):
        if stale.name not in wanted:
            stale.unlink()
    with ThreadPoolExecutor() as pool:
        results = list(pool.map(lambda c: convert(c, audio_out), clips))

    print(f"Fonts: {fonts}. Content files: {content}. "
          f"Voice clips: {len(clips)} ({results.count('converted')} converted, {results.count('skipped')} up to date).")
    if fonts < 2 or content == 0 or not clips:
        print("Something is missing: expected the Atkinson fonts, Content JSON and voice clips.")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
