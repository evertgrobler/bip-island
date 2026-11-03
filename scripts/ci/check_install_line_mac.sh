#!/usr/bin/env bash
# On a Mac runner: runs the download page's "install without the warning" line (site/index.html,
# cmd-mac) in zsh, the Mac's own shell, against this build's .dmg served over local HTTP, without its
# last step (opening the game). Checks the app lands in Applications, still signed and with no
# quarantine mark. (curl never adds that mark, so this mostly proves the line itself works: the
# download, the mount, the copy and the swap. The real https download is only exercised by people.)
# Run twice, so the second run proves replacing an older copy works too.
# Env: DMG  path of BipIsland-mac.dmg
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
dir="$(cd "$(dirname "$DMG")" && pwd)"
python3 -m http.server 8766 --bind 127.0.0.1 --directory "$dir" >/dev/null 2>&1 &
server=$!
trap 'kill $server 2>/dev/null || true' EXIT
sleep 2

line="$(python3 - "$root/site/index.html" "http://127.0.0.1:8766/$(basename "$DMG")" <<'PY'
import html, re, sys
page = open(sys.argv[1], encoding="utf-8").read()
line = html.unescape(re.search(r'<pre id="cmd-mac">(.*?)</pre>', page, re.S).group(1))
opener = ' && open "/Applications/Bip Island.app"'
assert line.count(opener) == 1, "the Mac install line should open the game once"
print(line.replace(opener, "").replace("{{MAC_URL}}", sys.argv[2]))
PY
)"
app="/Applications/Bip Island.app"
for run in 1 2; do
  zsh -c "$line"
  [ -d "$app" ] || { echo "::error::Run $run: the page's install line didn't put Bip Island in Applications."; exit 1; }
  if xattr "$app" | grep com.apple.quarantine > /dev/null; then
    echo "::error::Run $run: the page's install line left a quarantine mark."; exit 1
  fi
  codesign --verify --deep --strict "$app"
done
if mount | grep "/disk (" > /dev/null; then mount; echo "::error::The line left the disk image mounted."; exit 1; fi
rm -rf "$app"
echo "The download page's Mac install line works."
