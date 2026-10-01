#!/usr/bin/env bash
# Opens the built app in screenshot mode (BipIsland/App/ScreenshotMode.swift), which saves each
# scene as a PNG and quits. The PNGs are shrunk to small JPEGs for the download page.
# Env: APP_PATH. Output: build/screenshots/*.jpg
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
raw="$root/build/screenshots-raw"
out="$root/build/screenshots"
rm -rf "$raw" "$out"
mkdir -p "$raw" "$out"

binary="$APP_PATH/Contents/MacOS/Bip Island"
BIP_SCREENSHOTS="$raw" "$binary" &
pid=$!

# The app quits by itself when it's done (about a minute). Stop it if it hangs.
for _ in $(seq 1 180); do
  kill -0 "$pid" 2>/dev/null || break
  sleep 1
done
if kill -0 "$pid" 2>/dev/null; then
  echo "::warning title=Screenshots::The app didn't finish in 3 minutes, so it was stopped."
  kill "$pid" 2>/dev/null || true
fi
wait "$pid" 2>/dev/null || true

shopt -s nullglob
pngs=("$raw"/*.png)
if [ ${#pngs[@]} -eq 0 ]; then
  echo "::warning title=Screenshots::No screenshots were taken; the download page will show empty frames."
  exit 0
fi
for png in "${pngs[@]}"; do
  name="$(basename "$png" .png)"
  sips -s format jpeg -s formatOptions 80 --resampleWidth 1200 "$png" --out "$out/$name.jpg" >/dev/null
done
ls -l "$out"
echo "Screenshots: ${#pngs[@]}, $(du -sk "$out" | cut -f1) KB in total."
