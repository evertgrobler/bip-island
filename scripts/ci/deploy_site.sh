#!/usr/bin/env bash
# Publishes the download page (site/) on GitHub Pages: the root of the main branch of the public
# downloads repository, next to its README. Called by the publish job in .github/workflows/godot.yml
# once the release is published. Only the page, its fonts, icons and screenshots go there: never
# code, content or keys.
#
# The download buttons use GitHub's "latest release" links, so they always fetch the newest version.
# Each publish replaces the branch with a single commit (README kept), so screenshots from old
# builds don't pile up in its history.
#
# Usage: scripts/ci/deploy_site.sh [--build-only]   (--build-only just builds build/site, for check_all.sh)
# Env:   VERSION, DOWNLOADS_TOKEN (not needed with --build-only),
#        optional DOWNLOADS_REPO (default evertgrobler/bip-island-downloads),
#        SCREENSHOTS (folder of .jpg, default build/site-screenshots)
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
repo="${DOWNLOADS_REPO:-evertgrobler/bip-island-downloads}"
shots="${SCREENSHOTS:-$root/build/site-screenshots}"
site="$root/build/site"
rm -rf "$site"
mkdir -p "$site"

cp "$root/site/icon.png" "$root/site/favicon.png" "$site/"
cp -R "$root/site/fonts" "$site/fonts"
# GitHub Pages serves the files as they are (no Jekyll).
touch "$site/.nojekyll"

if compgen -G "$shots/*.jpg" > /dev/null; then
  mkdir -p "$site/screenshots"
  cp "$shots"/*.jpg "$site/screenshots/"
else
  echo "::warning title=No screenshots::The download page was published without screenshots."
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
[ "${1:-}" = "--build-only" ] && exit 0

owner="${repo%%/*}"
name="${repo#*/}"
page="https://$owner.github.io/$name/"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
# The token goes in a header, never in the remote address, so it can't end up in git's config or logs.
auth="AUTHORIZATION: basic $(printf 'x-access-token:%s' "$DOWNLOADS_TOKEN" | base64 | tr -d '\n')"
echo "::add-mask::$(printf 'x-access-token:%s' "$DOWNLOADS_TOKEN" | base64 | tr -d '\n')"
remote="https://github.com/$repo.git"

git -C "$work" init -q -b main
git -C "$work" -c http.extraHeader="$auth" fetch -q --depth 1 "$remote" main
git -C "$work" checkout -q FETCH_HEAD -- README.md 2>/dev/null || echo "::warning::$repo has no README.md on main."
cp -R "$site"/. "$work/"
git -C "$work" add -A
git -C "$work" -c user.name="Bip Island CI" -c user.email="actions@users.noreply.github.com" \
  commit -q -m "Download page for Bip Island $VERSION"
git -C "$work" -c http.extraHeader="$auth" push -q --force "$remote" HEAD:main
echo "Pushed the download page to $repo (main)."

# GitHub Pages takes a minute or two to build, and its cache can keep the old page for up to
# 10 minutes. The release is already out, so a slow page is a warning, not a failure.
echo "Checking $page shows $VERSION…"
for _ in $(seq 1 30); do
  if curl -fsSL "$page?v=$VERSION" 2>/dev/null | grep -q "Version $VERSION"; then
    echo "The download page is live with version $VERSION."
    exit 0
  fi
  sleep 20
done
echo "::warning::Pushed, but $page doesn't show version $VERSION yet. Check that GitHub Pages serves the main branch of $repo."
