param(
    [Parameter(Mandatory = $true)]
    [string] $Version
)

$ErrorActionPreference = "Stop"

$solution = "MiKiNuo.Mvi.slnx"
$output = "artifacts/packages"

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}

New-Item -ItemType Directory -Path $output | Out-Null

dotnet restore $solution
dotnet build $solution -c Release --no-restore -p:Version=$Version
dotnet test $solution -c Release --no-build

$projects = @(
    "src/MiKiNuo.Mvi.Domain/MiKiNuo.Mvi.Domain.csproj",
    "src/MiKiNuo.Mvi.Application/MiKiNuo.Mvi.Application.csproj",
    "src/MiKiNuo.Mvi.Infrastructure/MiKiNuo.Mvi.Infrastructure.csproj",
    "src/MiKiNuo.Mvi.Presentation/MiKiNuo.Mvi.Presentation.csproj"
)

foreach ($project in $projects) {
    dotnet pack $project `
        -c Release `
        --no-build `
        -p:PackageVersion=$Version `
        -p:ContinuousIntegrationBuild=true `
        -o $output
}

Write-Host "Packages generated in $output"
