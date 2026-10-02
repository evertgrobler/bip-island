#!/usr/bin/env bash
# Everything CI checks, run inside a cloud session (Linux) for free. Run it before every merge
# while GitHub Actions minutes are used up (see CLAUDE.md, "Working rules"), and paste the summary
# into the PR. It doesn't run the real Mac/Windows install-and-update test; that needs GitHub's
# Mac and Windows machines.
#
# Usage: scripts/check_all.sh            (from anywhere in the repo)
# Env:   GODOT  path to the Godot .NET binary (default: scripts/godot/install_godot.sh installs it)
set -uo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
out="$root/build/check"
rm -rf "$out"
mkdir -p "$out/screenshots"
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
results=()
failed=0

step() { # step <name> <command...>: runs it, keeps the log, records pass/fail
  local name="$1"; shift
  local log="$out/$(echo "$name" | tr ' /' '__').log"
  printf '▶ %s… ' "$name"
  if "$@" >"$log" 2>&1; then
    echo "ok"
    results+=("✅ $name")
  else
    echo "FAILED (log: ${log#$root/})"
    tail -n 25 "$log" | sed 's/^/    /'
    results+=("❌ $name")
    failed=1
  fi
}

# Tools a fresh session needs.
for tool in dotnet ffmpeg xvfb-run python3; do
  command -v "$tool" > /dev/null || {
    echo "Missing $tool. Install: sudo apt-get install -y dotnet-sdk-8.0 ffmpeg xvfb"
    exit 2
  }
done
GODOT="${GODOT:-$("$root/scripts/godot/install_godot.sh")}"
export PATH="$PATH:$HOME/.dotnet/tools"
command -v vpk > /dev/null || dotnet tool install -g vpk --version 1.2.161 > /dev/null

cd "$root"
step "Content" python3 scripts/validate_content.py
step "Copy assets into Godot" python3 scripts/godot/prepare_assets.py
step "C# build (warnings are errors)" dotnet build godot/BipIsland.sln -warnaserror
step "Logic tests" dotnet test godot/BipCore.Tests --no-build
step "Godot import" "$GODOT" --headless --path godot --import

self_test() {
  "$GODOT" --headless --path godot -- --bip-report "$out/report.json" &&
    python3 scripts/godot/check_report.py "$out/report.json"
}
step "Game self-test (fonts, content, clips)" self_test

screenshots() {
  local shot=(xvfb-run -a -s "-screen 0 1600x1000x24" "$GODOT" --path godot --rendering-driver opengl3 --resolution 1600x1000)
  "${shot[@]}" -- --bip-windowed --bip-screenshot "$out/screenshots/main.png" || return 1
  test -s "$out/screenshots/main.png" || return 1
  # Every other screen, when Boot supports opening a scene directly.
  if grep -q -- "--bip-scene" godot/Scripts/App/Boot.cs; then
    while IFS= read -r scene; do
      local name
      name="$(echo "${scene#godot/Scenes/}" | sed 's|/|_|g; s|\.tscn$||')"
      [ "$name" = "Main" ] && continue
      "${shot[@]}" -- --bip-windowed --bip-scene "res://${scene#godot/}" --bip-screenshot "$out/screenshots/$name.png" ||
        { echo "Screenshot failed: $scene"; return 1; }
      test -s "$out/screenshots/$name.png" || { echo "No screenshot for $scene"; return 1; }
    done < <(find godot/Scenes -name '*.tscn' | sort)
  fi
}
step "Screenshots of every screen" screenshots

export_both() {
  mkdir -p godot/build/windows godot/build/macos &&
    "$GODOT" --headless --path godot --export-release "Windows" "build/windows/Bip Island.exe" &&
    "$GODOT" --headless --path godot --export-release "macOS" "build/macos/BipIsland.zip" &&
    test -f "godot/build/windows/Bip Island.exe" && test -f "godot/build/windows/Bip Island.pck" &&
    test -s godot/build/macos/BipIsland.zip
}
step "Export for Mac and Windows" export_both
step "Pack the Windows installer" vpk "[win]" pack -x --packId BipIslandCheck --packVersion 0.0.1 \
  --packDir godot/build/windows --mainExe "Bip Island.exe" --packTitle "Bip Island" --skipVeloAppCheck \
  --outputDir "$out/windows-package"

diff_hygiene() {
  local base
  base="$(git merge-base HEAD origin/main 2>/dev/null || echo HEAD)"
  git diff --check "$base" -- . ':!*.uid' || return 1
  # Nothing that should never be committed.
  if git diff --name-only "$base" | grep -E '(^|/)(build|bin|obj|\.godot|assets)/|\.key$|sparkle_private|\.env'; then
    echo "Build output or a secret is in the diff (listed above)."
    return 1
  fi
  if git diff "$base" | grep -E '^\+.*(vercel_blob_rw_|BEGIN (RSA|OPENSSH|PRIVATE)|ghp_[A-Za-z0-9]{20})'; then
    echo "Something that looks like a secret is in the diff."
    return 1
  fi
}
step "Diff hygiene (whitespace, build files, secrets)" diff_hygiene

summary="$out/summary.md"
{
  echo "### Local checks ($(date -u '+%d %b %Y %H:%M UTC'), $(git rev-parse --short HEAD))"
  printf '%s\n' "${results[@]}" | sed 's/^/- /'
  echo "- Screenshots: $(find "$out/screenshots" -name '*.png' | wc -l | tr -d ' ') in build/check/screenshots (looked at by the session)"
} > "$summary"
echo
cat "$summary"
if [ "$failed" != 0 ]; then
  echo
  echo "Some checks failed: don't merge."
  exit 1
fi
