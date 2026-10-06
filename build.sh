#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "$0")"
configuration="${1:-Debug}"
case "$configuration" in Debug|Release) ;; *) echo "Use Debug or Release." >&2; exit 2;; esac
mkdir -p artifacts
if ! command -v dotnet >/dev/null 2>&1; then
  echo ".NET SDK not found; install a .NET 10 SDK before building." | tee artifacts/build-prerequisite.log >&2
  exit 127
fi
run() { local name="$1"; shift; echo "=== $name ==="; dotnet "$@" 2>&1 | tee "artifacts/$name.log"; }
run environment --info
run restore restore MiKiNuo.Mvi.slnx
run build build MiKiNuo.Mvi.slnx -c "$configuration" --no-restore
run tests run --project test/MiKiNuo.Mvi.Tests -c "$configuration" --no-build -- --maximum-parallel-tests 1 --results-directory artifacts/test-results
# List only: proves discovery when executed, not a performance measurement.
run benchmark-discovery run --project test/MiKiNuo.Mvi.Benchmarks -c "$configuration" --no-build -- --list flat
