#!/usr/bin/env bash
# Prints the update feed URL (https://<project>.vercel.app/appcast.xml) for the Vercel project.
# The repository variable UPDATE_FEED_URL overrides it (e.g. for a custom domain).
# Env: VERCEL_TOKEN, VERCEL_ORG_ID, VERCEL_PROJECT_ID, optional UPDATE_FEED_URL
set -euo pipefail

if [ -n "${UPDATE_FEED_URL:-}" ]; then
  echo "$UPDATE_FEED_URL"
  exit 0
fi

team=""
case "$VERCEL_ORG_ID" in
  team_*) team="?teamId=$VERCEL_ORG_ID" ;;
esac

api() {
  curl -fsS -H "Authorization: Bearer $VERCEL_TOKEN" "https://api.vercel.com$1$team"
}

project="$(api "/v9/projects/$VERCEL_PROJECT_ID")" || {
  echo "Couldn't read the Vercel project. Check the VERCEL_TOKEN, VERCEL_ORG_ID and VERCEL_PROJECT_ID secrets." >&2
  exit 1
}
domain="$(api "/v9/projects/$VERCEL_PROJECT_ID/domains" 2>/dev/null \
  | jq -r '[.domains[]? | select((.redirect // "") == "") | .name] | (map(select(endswith(".vercel.app"))) + .) | .[0] // empty' || true)"
if [ -z "$domain" ]; then
  domain="$(printf '%s' "$project" | jq -r '.name').vercel.app"
fi
echo "https://$domain/appcast.xml"
