#!/usr/bin/env bash
# Prints a brand-new Sparkle private key (base64 Ed25519 seed, the format Sparkle's generate_keys exports).
set -euo pipefail
OPENSSL="${OPENSSL:-openssl}"
"$OPENSSL" genpkey -algorithm ed25519 -outform DER | tail -c 32 | "$OPENSSL" base64 -A
echo
