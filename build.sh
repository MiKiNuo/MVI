#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")"
configuration="${1:-Debug}"
case "$configuration" in Debug|Release) ;; *) echo 'Configuration must be Debug or Release.' >&2; exit 2;; esac
mkdir -p artifacts
if ! command -v dotnet >/dev/null 2>&1; then
    echo '.NET SDK not found. Install a .NET 10 SDK; no build or C# tests were executed.' >&2
    exit 127
fi
run_step() {
    local name="$1"; shift
    printf '\n=== %s ===\n' "$name"
    set +e
    dotnet "$@" 2>&1 | tee "artifacts/$name.log"
    local statuses=("${PIPESTATUS[@]}")
    set -e
    if (( statuses[0] != 0 )); then
        echo "$name failed with exit code ${statuses[0]}." >&2
        return "${statuses[0]}"
    fi
    if (( statuses[1] != 0 )); then return "${statuses[1]}"; fi
}
run_step environment --info
run_step restore restore MiKiNuo.Mvi.slnx
run_step build build MiKiNuo.Mvi.slnx -c "$configuration" --no-restore -bl:artifacts/build.binlog
run_step main-tests run --project test/MiKiNuo.Mvi.Tests/MiKiNuo.Mvi.Tests.csproj -c "$configuration" --no-build --no-restore -- --maximum-parallel-tests 1
run_step v2-runtime-tests run --project test/MiKiNuo.Mvi.V2.Framework.Tests/MiKiNuo.Mvi.V2.Framework.Tests.csproj -c "$configuration" --no-build --no-restore -- --maximum-parallel-tests 1
run_step v2-generator-tests run --project test/MiKiNuo.Mvi.V2.Generator.Tests/MiKiNuo.Mvi.V2.Generator.Tests.csproj -c "$configuration" --no-build --no-restore -- --maximum-parallel-tests 1
run_step v2-auth-tests run --project sample/AuthenticationV2/Sample.Auth.Tests/Sample.Auth.Tests.csproj -c "$configuration" --no-build --no-restore -- --maximum-parallel-tests 1
run_step v2-headless run --project sample/AuthenticationV2/Sample.Auth.SmokeTests/Sample.Auth.SmokeTests.csproj -c "$configuration" --no-build --no-restore
run_step benchmark-discovery run --project test/MiKiNuo.Mvi.Benchmarks/MiKiNuo.Mvi.Benchmarks.csproj -c "$configuration" --no-build --no-restore -- --list flat
printf '\nAll requested processes succeeded. GUI, SMTP, AOT and performance require separate validation.\n'
