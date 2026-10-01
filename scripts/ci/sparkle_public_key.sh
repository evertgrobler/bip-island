#!/usr/bin/env bash
# Prints the Sparkle public key (SUPublicEDKey) for the Sparkle private key read from stdin.
# Sparkle's private key is a base64 Ed25519 seed (32 bytes), or the older 96-byte form whose
# last 32 bytes are the public key. Needs OpenSSL 3 (set $OPENSSL to its path if not on PATH).
set -euo pipefail
OPENSSL="${OPENSSL:-openssl}"

key="$(tr -d ' \r\n')"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

printf '%s' "$key" | "$OPENSSL" base64 -d -A > "$tmp/raw"
size="$(wc -c < "$tmp/raw" | tr -d ' ')"
case "$size" in
  32)
    # Wrap the seed in a PKCS#8 header so OpenSSL can derive the public key.
    { printf 'MC4CAQAwBQYDK2VwBCIEIA==' | "$OPENSSL" base64 -d -A; cat "$tmp/raw"; } > "$tmp/key.der"
    "$OPENSSL" pkey -inform DER -in "$tmp/key.der" -pubout -outform DER | tail -c 32 | "$OPENSSL" base64 -A
    ;;
  96)
    tail -c 32 "$tmp/raw" | "$OPENSSL" base64 -A
    ;;
  *)
    echo "That doesn't look like a Sparkle private key (expected 32 or 96 bytes, got $size)." >&2
    exit 1
    ;;
esac
echo
