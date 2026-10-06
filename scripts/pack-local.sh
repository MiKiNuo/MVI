#!/usr/bin/env bash
set -euo pipefail
root="$(cd -- "$(dirname -- "$0")/.." && pwd)"
if [[ $# -ne 1 ]]; then echo "Usage: bash scripts/pack-local.sh 2.0.0-preview.4" >&2; exit 2; fi
exec python3 "$root/tools/release.py" pack --version "$1"
