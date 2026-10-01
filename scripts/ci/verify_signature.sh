#!/usr/bin/env bash
# verify_signature.sh <file> <base64 EdDSA signature> <base64 public key>
# Checks a Sparkle signature independently of Sparkle, with OpenSSL 3.
set -euo pipefail
OPENSSL="${OPENSSL:-openssl}"
file="$1"; signature="$2"; public_key="$3"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
{ printf 'MCowBQYDK2VwAyEA' | "$OPENSSL" base64 -d -A; printf '%s' "$public_key" | "$OPENSSL" base64 -d -A; } > "$tmp/pub.der"
printf '%s' "$signature" | "$OPENSSL" base64 -d -A > "$tmp/sig.bin"
"$OPENSSL" pkeyutl -verify -pubin -inkey "$tmp/pub.der" -keyform DER -rawin -in "$file" -sigfile "$tmp/sig.bin"
