#!/usr/bin/env bash
# Checks the download page's two "install without the warning" lines (site/index.html, cmd-mac and
# cmd-win) at least parse: the Mac line in bash (and zsh, the Mac's shell, when installed), the Windows
# line with PowerShell's own parser (pwsh, when installed). Runs on every pull request (check_all.sh and
# the Linux CI job); the real Mac and Windows runs are in godot.yml's macos and windows jobs.
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
python3 - "$root/site/index.html" "$work" <<'PY'
import html, re, sys
page = open(sys.argv[1], encoding="utf-8").read()
for pre in ("cmd-mac", "cmd-win"):
    match = re.search(r'<pre id="%s">(.*?)</pre>' % pre, page, re.S)
    if not match:
        sys.exit(f"site/index.html has no <pre id=\"{pre}\">.")
    open(f"{sys.argv[2]}/{pre}.txt", "w").write(html.unescape(match.group(1)))
PY
bash -n "$work/cmd-mac.txt"
if command -v zsh > /dev/null; then zsh -n "$work/cmd-mac.txt"; else echo "zsh isn't installed: the Mac line was checked with bash only."; fi
if command -v pwsh > /dev/null; then
  pwsh -NoProfile -Command '
    $errors = $null
    [System.Management.Automation.Language.Parser]::ParseInput((Get-Content -Raw $args[0]), [ref]$null, [ref]$errors) | Out-Null
    if ($errors.Count) { $errors | ForEach-Object { Write-Error $_.Message }; exit 1 }' "$work/cmd-win.txt"
else
  echo "pwsh isn't installed: the Windows line wasn't parsed here (the Linux CI job does)."
fi
echo "Both install lines parse."
