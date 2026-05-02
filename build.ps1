$ErrorActionPreference = "Stop"
dotnet restore .\MiKiNuo.Mvi.slnx
dotnet build .\MiKiNuo.Mvi.slnx --no-restore
dotnet test .\MiKiNuo.Mvi.slnx --no-build
