#!/usr/bin/env bash
# Screenshots of the Godot game for the download page (site/index.html uses these file names).
# Opens each preview scene under a virtual screen, saves a PNG, and shrinks it to a small JPEG.
# Env: GODOT (the Godot .NET binary). Output: build/site-screenshots/*.jpg
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
out="$root/build/site-screenshots"
raw="$out/raw"
rm -rf "$out"
mkdir -p "$raw"

# page name : scene under godot/Scenes
shots=(
  map:Screens/map
  letters-game:Games/feed_the_monster
  numbers-game:Games/count_and_tap
  words-game:Games/word_builder
  coding-game:Games/bips_path
  art-game:Games/paint_pots
  stickers:Screens/stickers
  charging:Screens/charging
  parent-progress:Parent/progress
)
failed=0
for shot in "${shots[@]}"; do
  name="${shot%%:*}"
  scene="${shot#*:}"
  xvfb-run -a -s "-screen 0 1600x1000x24" "$GODOT" --path "$root/godot" --rendering-driver opengl3 \
    --resolution 1600x1000 -- --bip-windowed --bip-save-dir "$raw/saves-$name" \
    --bip-scene "res://Scenes/$scene.tscn" --bip-screenshot "$raw/$name.png" > "$raw/$name.log" 2>&1 || true
  if [ -s "$raw/$name.png" ]; then
    ffmpeg -loglevel error -y -i "$raw/$name.png" -vf scale=1200:-1 -q:v 4 "$out/$name.jpg"
  else
    echo "::warning title=Screenshot::No screenshot of $scene."
    failed=1
  fi
done
rm -rf "$raw"
ls -l "$out"
echo "Page screenshots: $(du -sk "$out" | cut -f1) KB in total."
exit "$failed"
