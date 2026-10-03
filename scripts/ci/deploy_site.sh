#!/usr/bin/env bash
# Publishes the download page (site/) to Vercel, with the Godot game's downloads and screenshots.
# Called by the publish job in .github/workflows/godot.yml after the builds are in the Blob store.
#
# Env: VERSION, MAC_URL, WIN_URL, VERCEL_TOKEN, VERCEL_ORG_ID, VERCEL_PROJECT_ID,
#      optional UPDATE_FEED_URL, SCREENSHOTS (folder of .jpg, default build/site-screenshots)
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
shots="${SCREENSHOTS:-$root/build/site-screenshots}"
site="$root/build/site"
rm -rf "$site"
mkdir -p "$site"

cp "$root/site/index.html" "$root/site/vercel.json" "$root/site/icon.png" "$root/site/favicon.png" "$site/"
cp -R "$root/site/fonts" "$site/fonts"

if compgen -G "$shots/*.jpg" > /dev/null; then
  mkdir -p "$site/screenshots"
  cp "$shots"/*.jpg "$site/screenshots/"
else
  echo "::warning title=No screenshots::The download page was published without screenshots."
fi

# The site's address, for the "is it live" check at the end.
feed_url="$("$root/scripts/ci/resolve_feed_url.sh")"
base="${feed_url%/*}"

date_text="$(date -u '+%-d %B %Y')"
sed -e "s|{{VERSION}}|$VERSION|g" -e "s|{{DATE}}|$date_text|g" \
    -e "s|{{MAC_URL}}|$MAC_URL|g" -e "s|{{WIN_URL}}|$WIN_URL|g" "$root/site/index.html" > "$site/index.html"
if grep -q '{{' "$site/index.html"; then
  echo "::error::The page still has an unfilled {{placeholder}}."
  exit 1
fi

npx --yes vercel@latest deploy "$site" --prod --yes --token "$VERCEL_TOKEN"

echo "Checking ${base}/ is live…"
for attempt in 1 2 3 4 5 6 7 8 9 10; do
  if curl -fsSL "$base/" | grep -q "$VERSION"; then
    echo "Download page is live with version $VERSION."
    exit 0
  fi
  sleep 6
done
echo "::error::Deployed, but ${base}/ doesn't show version $VERSION yet."
exit 1
