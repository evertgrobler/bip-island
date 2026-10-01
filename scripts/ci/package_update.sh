#!/usr/bin/env bash
# Ad-hoc signs the built app, zips it, and makes a Sparkle appcast.xml signed with the EdDSA key.
# Then checks the signature independently, so a broken update can never be published.
#
# Env: APP_PATH       path to "Bip Island.app"
#      KEY_FILE       file holding the Sparkle private key
#      FEED_URL       https://…/appcast.xml the app checks
#      OUT_DIR        where the zip and appcast go
#      SPARKLE_BIN    folder with Sparkle's generate_appcast
#      OPENSSL        OpenSSL 3 binary
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"

echo "Signing ad hoc: $APP_PATH"
codesign --force --deep --sign - "$APP_PATH"
codesign --verify --deep --strict --verbose=2 "$APP_PATH"

plist="$APP_PATH/Contents/Info.plist"
version="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$plist")"
build="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleVersion' "$plist")"
app_key="$(/usr/libexec/PlistBuddy -c 'Print :SUPublicEDKey' "$plist")"
app_feed="$(/usr/libexec/PlistBuddy -c 'Print :SUFeedURL' "$plist")"

expected_key="$("$here/sparkle_public_key.sh" < "$KEY_FILE")"
if [ "$app_key" != "$expected_key" ]; then
  echo "::error::The app's SUPublicEDKey doesn't match the signing key."
  exit 1
fi
if [ "$app_feed" != "$FEED_URL" ]; then
  echo "::error::The app's SUFeedURL ($app_feed) isn't $FEED_URL."
  exit 1
fi

# Every clip in audio/script.csv must be inside the app (real or placeholder).
root="$(cd "$here/../.." && pwd)"
expected_clips="$(tail -n +2 "$root/audio/script.csv" | grep -c '^[a-z].*\.m4a,' || true)"
bundled_clips="$(find "$APP_PATH/Contents/Resources/Audio" -name '*.m4a' 2>/dev/null | wc -l | tr -d ' ')"
echo "Voice clips in the app: $bundled_clips of $expected_clips"
if [ "$bundled_clips" -eq 0 ]; then
  echo "::error::No voice clips were bundled into the app (Resources/Audio is missing)."
  exit 1
elif [ "$bundled_clips" -lt "$expected_clips" ]; then
  echo "::warning::Only $bundled_clips of $expected_clips voice clips are in the app; the rest will be silent."
fi

rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"
zip_name="BipIsland-$version-$build.zip"
ditto -c -k --sequesterRsrc --keepParent "$APP_PATH" "$OUT_DIR/$zip_name"
echo "Zipped $zip_name ($(du -h "$OUT_DIR/$zip_name" | cut -f1))"

"$SPARKLE_BIN/generate_appcast" \
  --ed-key-file "$KEY_FILE" \
  --download-url-prefix "${FEED_URL%/*}/" \
  "$OUT_DIR"

appcast="$OUT_DIR/$(basename "$FEED_URL")"
if [ ! -f "$appcast" ]; then
  echo "::error::generate_appcast didn't make $(basename "$FEED_URL")"
  ls -la "$OUT_DIR"
  exit 1
fi
cat "$appcast"

signature="$(grep -o 'sparkle:edSignature="[^"]*"' "$appcast" | head -1 | cut -d'"' -f2)"
if [ -z "$signature" ]; then
  echo "::error::The appcast has no EdDSA signature."
  exit 1
fi
grep -Eq "sparkle:version=\"$build\"|<sparkle:version>$build</sparkle:version>" "$appcast" || {
  echo "::error::The appcast doesn't list build $build."
  exit 1
}
"$here/verify_signature.sh" "$OUT_DIR/$zip_name" "$signature" "$expected_key"
echo "Update signature checked."

if [ -n "${GITHUB_OUTPUT:-}" ]; then
  {
    echo "zip_name=$zip_name"
    echo "version=$version"
    echo "build=$build"
  } >> "$GITHUB_OUTPUT"
fi
