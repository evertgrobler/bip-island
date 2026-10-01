#!/usr/bin/env bash
# Makes sure the public Vercel Blob store for the Godot downloads exists, connected to the Vercel
# project that hosts the download page (it uses the existing VERCEL_* secrets; nothing new to set up).
# Prints the store's read-write token on line 1 and its public web address on line 2.
# Env: VERCEL_TOKEN, VERCEL_ORG_ID, VERCEL_PROJECT_ID, optional BLOB_STORE_NAME
set -euo pipefail
store_name="${BLOB_STORE_NAME:-bip-island-downloads}"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
cd "$work"

vercel() { npx --yes vercel@latest "$@" --token "$VERCEL_TOKEN"; }

# Connecting a Blob store to a project gives the project a BLOB_READ_WRITE_TOKEN variable.
pull_token() {
  rm -f .env.blob
  vercel env pull .env.blob --environment=production --yes >/dev/null 2>&1 || true
  sed -n -E 's/^BLOB_READ_WRITE_TOKEN="?([^"]*)"?$/\1/p' .env.blob 2>/dev/null | head -1
}

token="$(pull_token)"
if [ -z "$token" ]; then
  echo "Making the Blob store $store_name…" >&2
  vercel blob create-store "$store_name" --access public --environment production --yes >&2
  token="$(pull_token)"
fi
if [ -z "$token" ]; then
  echo "::error::Couldn't make or find the Vercel Blob store. Check VERCEL_TOKEN can manage the project's storage." >&2
  exit 1
fi
# Tokens look like vercel_blob_rw_<storeId>_<secret>; public files live at <storeId>.public.blob.vercel-storage.com.
store_id="$(printf '%s' "$token" | cut -d_ -f4 | tr '[:upper:]' '[:lower:]')"
echo "$token"
echo "https://$store_id.public.blob.vercel-storage.com"
