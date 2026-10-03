$ErrorActionPreference = "Stop"
dotnet restore .\MiKiNuo.Mvi.slnx
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build .\MiKiNuo.Mvi.slnx --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet test --solution .\MiKiNuo.Mvi.slnx --no-build
exit $LASTEXITCODE
