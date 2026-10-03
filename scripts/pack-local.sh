#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
exec pwsh -NoProfile -File "$SCRIPT_DIR/pack-local.ps1" -Version "${1:?Usage: ./scripts/pack-local.sh <version>}" "${@:2}"
