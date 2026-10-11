#!/usr/bin/env bash
# Turns a generated music track into a seamless background loop for the game:
# the last 3 seconds blend into the first (so the loop has no jump), the loudness is evened out
# (-20 LUFS, quiet enough to sit under the narrator), and it's saved as Resources/Music/<name>.m4a.
#
#   scripts/make_music_loop.sh <generated.mp3> <track name, e.g. music_map>
#
# Track names come from BipCore.MusicRules (music_map, music_letters, music_numbers, music_words,
# music_coding, music_art). Needs ffmpeg.
set -euo pipefail
src="$1"
name="$2"
root="$(cd "$(dirname "$0")/.." && pwd)"
out="$root/Resources/Music/$name.m4a"
blend=3
# Decode first: an mp3's reported length can be a few milliseconds off, which would leave the blend empty.
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
ffmpeg -nostdin -loglevel error -y -i "$src" -ar 44100 -ac 2 "$work/track.wav"
src="$work/track.wav"
length="$(ffprobe -v error -show_entries format=duration -of csv=p=0 "$src")"
body_end="$(python3 -c "print(round($length - $blend, 3))")"
# One fixed volume change to -20 LUFS (a changing one could jump at the loop point).
measured="$(ffmpeg -nostdin -hide_banner -i "$src" -af loudnorm=I=-20:print_format=json -f null - 2>&1 \
  | python3 -c "import json,sys; t=sys.stdin.read(); print(json.loads(t[t.rindex('{'):])['input_i'])")"
gain="$(python3 -c "print(round(-20 - ($measured), 2))")"
mkdir -p "$(dirname "$out")"
ffmpeg -nostdin -loglevel error -y -i "$src" -filter_complex "
  [0:a]asplit=3[a][b][c];
  [a]atrim=0:$blend,asetpts=PTS-STARTPTS[head];
  [b]atrim=$blend:$body_end,asetpts=PTS-STARTPTS[body];
  [c]atrim=$body_end,asetpts=PTS-STARTPTS[tail];
  [tail]afade=t=out:d=$blend[tailout];
  [head]afade=t=in:d=$blend[headin];
  [tailout][headin]amix=inputs=2:normalize=0:duration=longest[seam];
  [seam][body]concat=n=2:v=0:a=1,volume=${gain}dB,alimiter=limit=0.89[out]" \
  -map "[out]" -ac 2 -c:a aac -b:a 160k "$out"
echo "Made $out ($(ffprobe -v error -show_entries format=duration -of csv=p=0 "$out") s)."
