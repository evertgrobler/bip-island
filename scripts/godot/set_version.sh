#!/usr/bin/env bash
# Stamps the version into the Godot project before export.
# Usage: set_version.sh 0.2.57 57
set -euo pipefail
version="$1"; build="$2"
dir="$(cd "$(dirname "$0")/../../godot" && pwd)"
sed -i.bak -E "s|^config/version=.*|config/version=\"$version\"|" "$dir/project.godot"
sed -i.bak -E "s|^application/short_version=.*|application/short_version=\"$version\"|; s|^application/version=.*|application/version=\"$build\"|" "$dir/export_presets.cfg"
rm -f "$dir/project.godot.bak" "$dir/export_presets.cfg.bak"
grep -q "config/version=\"$version\"" "$dir/project.godot"
grep -q "application/short_version=\"$version\"" "$dir/export_presets.cfg"
echo "Version $version (build $build)"
