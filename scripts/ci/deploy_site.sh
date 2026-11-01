#!/usr/bin/env bash
# Builds the download page (site/) into build/site, with the game's screenshots and the version filled
# in. godot.yml's publish job then puts that folder on GitHub Pages (actions/deploy-pages); check_all.sh
# runs this to check the page builds. Only the page, its fonts, icons and screenshots go there: never
# code, content or keys.
#
# The download buttons and install lines use GitHub's "latest release" links, so they always fetch the
# newest version.
#
# Env: VERSION, optional DOWNLOADS_REPO (owner/name, default evertgrobler/bip-island),
#      SCREENSHOTS (folder of .jpg, default build/site-screenshots)
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
repo="${DOWNLOADS_REPO:-evertgrobler/bip-island}"
shots="${SCREENSHOTS:-$root/build/site-screenshots}"
site="$root/build/site"
rm -rf "$site"
mkdir -p "$site"

cp "$root/site/icon.png" "$root/site/favicon.png" "$root/site/bip.png" "$site/"
cp -R "$root/site/fonts" "$site/fonts"
# GitHub Pages serves the files as they are (no Jekyll).
touch "$site/.nojekyll"

if compgen -G "$shots/*.jpg" > /dev/null; then
  mkdir -p "$site/screenshots"
  cp "$shots"/*.jpg "$site/screenshots/"
else
  echo "::warning title=No screenshots::The download page was built without screenshots."
fi

latest="https://github.com/$repo/releases/latest/download"
date_text="$(TZ=Africa/Johannesburg date '+%-d %B %Y')"
sed -e "s|{{VERSION}}|$VERSION|g" -e "s|{{DATE}}|$date_text|g" \
    -e "s|{{MAC_URL}}|$latest/BipIsland-mac.dmg|g" -e "s|{{WIN_URL}}|$latest/BipIsland-win-Setup.exe|g" \
    "$root/site/index.html" > "$site/index.html"
if grep -q '{{' "$site/index.html"; then
  echo "::error::The page still has an unfilled {{placeholder}}."
  exit 1
fi
echo "Built the download page in ${site#$root/}."
