#!/usr/bin/env bash
# On a Mac runner: packs the exported game with Velopack (twice: an older version and this one),
# installs the older one, lets it update itself from a local feed, and checks the update arrived
# and saved data survived. Then makes the .dmg download from this version.
#
# Env: EXPORT_ZIP  zip from Godot's macOS export (contains "Bip Island.app")
#      VERSION     this build's version, e.g. 0.2.57
#      OUT_DIR     where the release files go (osx/ feed folder + BipIsland-mac.dmg)
#      PACK_ID     Velopack app id (BipIslandTest while testing)
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
work="$RUNNER_TEMP/godot-mac"
rm -rf "$work" "$OUT_DIR"
mkdir -p "$work/export" "$OUT_DIR"

ditto -x -k "$EXPORT_ZIP" "$work/export"
app="$(find "$work/export" -maxdepth 1 -name '*.app' | head -1)"
[ -n "$app" ] || { echo "::error::No .app in $EXPORT_ZIP"; exit 1; }
exe="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$app/Contents/Info.plist")"
echo "App: $app (runs $exe)"

# The Mac packer's options, in the log, in case one of them changes in a Velopack update.
vpk pack -x -H || true

pack() { # pack <version> <output dir>
  vpk pack -x --packId "$PACK_ID" --packVersion "$1" --packDir "$app" --mainExe "$exe" \
    --packTitle "Bip Island" --noInst \
    --signAppIdentity "-" --signEntitlements "$root/godot/macos/BipIsland.entitlements" \
    --outputDir "$2"
}
old_version="$VERSION-selftest"
pack "$old_version" "$work/old"
pack "$VERSION" "$OUT_DIR/osx"
ls -la "$work/old" "$OUT_DIR/osx"

# Install the older version the way a person would: unzip the app into a folder and run it.
install_dir="$work/installed"
mkdir -p "$install_dir"
ditto -x -k "$work/old/$PACK_ID-osx-Portable.zip" "$install_dir"
installed_app="$(find "$install_dir" -maxdepth 1 -name '*.app' | head -1)"
codesign --verify --deep --strict --verbose=2 "$installed_app"

# Serve this version as the update feed and let the old one update itself.
python3 -m http.server 8765 --directory "$OUT_DIR/osx" >"$work/http.log" 2>&1 &
server=$!
trap 'kill $server 2>/dev/null || true' EXIT
sleep 2
report="$work/report.json"
rm -f "$report"
"$installed_app/Contents/MacOS/$exe" --headless -- --bip-update-test "http://127.0.0.1:8765/" "$report" \
  >"$work/old-run.log" 2>&1 || true
for _ in $(seq 1 90); do
  [ -s "$report" ] && break
  sleep 2
done
if [ ! -s "$report" ]; then
  echo "::error::The installed game didn't write its report after updating."
  cat "$work/old-run.log" || true
  find "$HOME/Library/Logs" "$install_dir" -maxdepth 3 -iname '*velopack*' -print -exec tail -n 60 {} \; 2>/dev/null || true
  exit 1
fi
sleep 2
python3 "$root/scripts/godot/check_report.py" "$report" --version "$VERSION" --expect-save

# The kid lock, on a real Mac window (not headless): the updated game opens locked, asks macOS
# whether the Dock/menu bar/Cmd-Tab lock took hold, then lifts it and quits. Reported as a warning
# for now: it's the first time it runs on a runner's screen.
lock_report="$work/kidlock.json"
rm -f "$lock_report"
"$installed_app/Contents/MacOS/$exe" -- --bip-kidlock-check "$lock_report" >"$work/kidlock.log" 2>&1 &
game=$!
for _ in $(seq 1 30); do
  [ -s "$lock_report" ] && break
  sleep 2
done
kill "$game" 2>/dev/null || true
if grep -q '"locked":true' "$lock_report" 2>/dev/null; then
  echo "Kid lock: $(cat "$lock_report")"
else
  echo "::warning::The Mac kid lock didn't confirm: $(cat "$lock_report" 2>/dev/null || echo 'no report')"
  tail -n 40 "$work/kidlock.log" || true
fi

# The .dmg: this version's app plus a shortcut to Applications, to drag across.
stage="$work/dmg"
mkdir -p "$stage"
ditto -x -k "$OUT_DIR/osx/$PACK_ID-osx-Portable.zip" "$stage"
codesign --verify --deep --strict --verbose=2 "$(find "$stage" -maxdepth 1 -name '*.app' | head -1)"
ln -s /Applications "$stage/Applications"
hdiutil create -volname "Bip Island" -srcfolder "$stage" -ov -format UDZO "$OUT_DIR/BipIsland-mac.dmg"
hdiutil verify "$OUT_DIR/BipIsland-mac.dmg"
ls -la "$OUT_DIR" "$OUT_DIR/osx"
