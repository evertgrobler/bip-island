#!/usr/bin/env bash
# Downloads Godot (.NET edition) for Linux plus the Mac and Windows export templates.
# Prints the path of the Godot binary. Re-running is quick: it skips what's already there.
# Env: GODOT_VERSION (default below), GODOT_HOME (default ~/.cache/godot)
set -euo pipefail
version="${GODOT_VERSION:-4.7.2}"
home="${GODOT_HOME:-$HOME/.cache/godot}"
release="https://github.com/godotengine/godot/releases/download/${version}-stable"
name="Godot_v${version}-stable_mono_linux_x86_64"
bin="$home/$name/Godot_v${version}-stable_mono_linux.x86_64"
templates="${XDG_DATA_HOME:-$HOME/.local/share}/godot/export_templates/${version}.stable.mono"

mkdir -p "$home"
if [ ! -x "$bin" ]; then
  curl -fsSL -o "$home/godot.zip" "$release/$name.zip" >&2
  unzip -q -o "$home/godot.zip" -d "$home" >&2
  rm "$home/godot.zip"
fi

if [ ! -f "$templates/macos.zip" ] || [ ! -f "$templates/windows_release_x86_64.exe" ]; then
  mkdir -p "$templates"
  tpz="$home/templates.tpz"
  [ -f "$tpz" ] || curl -fsSL -o "$tpz" "$release/Godot_v${version}-stable_mono_export_templates.tpz" >&2
  # Only the Mac and Windows templates (the whole set is about 2 GB unpacked).
  unzip -q -o -j "$tpz" templates/version.txt templates/macos.zip \
    templates/windows_release_x86_64.exe templates/windows_release_x86_64_console.exe \
    templates/windows_debug_x86_64.exe templates/windows_debug_x86_64_console.exe -d "$templates" >&2
  rm -f "$tpz"
fi
echo "$bin"
