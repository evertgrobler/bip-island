#!/usr/bin/env bash
# Publishes the update (zip + appcast.xml) and the download page (site/) to Vercel, then checks it's live.
# Env: OUT_DIR, FEED_URL, ZIP_NAME, VERSION, BUILD, VERCEL_TOKEN, VERCEL_ORG_ID, VERCEL_PROJECT_ID
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"

# Never let an older run overwrite a newer one.
if [ -n "${GITHUB_SHA:-}" ]; then
  head="$(git -C "$root" ls-remote origin refs/heads/main | cut -f1)"
  if [ -n "$head" ] && [ "$head" != "$GITHUB_SHA" ]; then
    echo "::notice title=Not published::main has moved on to a newer commit, so this older build isn't published."
    exit 0
  fi
fi

site="$root/build/site"
rm -rf "$site"
mkdir -p "$site"
cp "$OUT_DIR/$ZIP_NAME" "$OUT_DIR/$(basename "$FEED_URL")" "$site/"
cp "$root/site/vercel.json" "$root/site/icon.png" "$root/site/favicon.png" "$site/"
cp -R "$root/site/fonts" "$site/fonts"
# Screenshots of the real game, taken earlier in this run (take_screenshots.sh). Without them the
# page shows marked empty frames.
if compgen -G "$root/build/screenshots/*.jpg" >/dev/null; then
  mkdir -p "$site/screenshots"
  cp "$root"/build/screenshots/*.jpg "$site/screenshots/"
else
  echo "::warning title=No screenshots::The download page was published without screenshots."
fi
cp "$OUT_DIR/$ZIP_NAME" "$site/BipIsland-latest.zip"
sed -e "s|{{VERSION}}|$VERSION|g" -e "s|{{BUILD}}|$BUILD|g" -e "s|{{ZIP}}|$ZIP_NAME|g" \
    -e "s|{{DATE}}|$(date -u '+%d %B %Y')|g" "$root/site/index.html" > "$site/index.html"

npx --yes vercel@latest deploy "$site" --prod --yes --token "$VERCEL_TOKEN"

echo "Checking $FEED_URL is live…"
for attempt in 1 2 3 4 5 6 7 8 9 10; do
  if curl -fsSL "$FEED_URL" | grep -q "$ZIP_NAME"; then
    echo "Update feed is live."
    exit 0
  fi
  sleep 6
done
echo "::error::Deployed, but $FEED_URL doesn't show $ZIP_NAME. If it asks for a login, turn off Vercel Authentication (see docs/SETUP.md)."
exit 1
