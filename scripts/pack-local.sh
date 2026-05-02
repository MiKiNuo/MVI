#!/usr/bin/env bash
set -euo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: ./scripts/pack-local.sh <version>"
  echo "Example: ./scripts/pack-local.sh 1.9.2"
  exit 1
fi

VERSION="$1"
SOLUTION="MiKiNuo.Mvi.slnx"
OUTPUT="artifacts/packages"

rm -rf "$OUTPUT"
mkdir -p "$OUTPUT"

dotnet restore "$SOLUTION"
dotnet build "$SOLUTION" -c Release --no-restore -p:Version="$VERSION"
dotnet test "$SOLUTION" -c Release --no-build

PROJECTS=(
  "src/MiKiNuo.Mvi.Domain/MiKiNuo.Mvi.Domain.csproj"
  "src/MiKiNuo.Mvi.Application/MiKiNuo.Mvi.Application.csproj"
  "src/MiKiNuo.Mvi.Infrastructure/MiKiNuo.Mvi.Infrastructure.csproj"
  "src/MiKiNuo.Mvi.Presentation/MiKiNuo.Mvi.Presentation.csproj"
)

for project in "${PROJECTS[@]}"; do
  dotnet pack "$project"     -c Release     --no-build     -p:PackageVersion="$VERSION"     -p:ContinuousIntegrationBuild=true     -o "$OUTPUT"
done

echo "Packages generated in $OUTPUT"
