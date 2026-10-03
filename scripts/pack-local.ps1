#requires -Version 7.2

param(
    [Parameter(Mandatory = $true)]
    [string] $Version,
    [string] $GodotPath,
    [switch] $NonGraphical
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$solution = Join-Path $repoRoot 'MiKiNuo.Mvi.slnx'
if ([IO.Path]::GetFileName($Version) -ne $Version -or $Version -in @('.', '..')) { throw 'Version must be a NuGet version, not a path.' }
$output = Join-Path $repoRoot "artifacts/packages/$Version"

& (Join-Path $repoRoot 'test/MiKiNuo.Mvi.PackageConsumers/verify-output-boundary.ps1')

dotnet restore $solution
if ($LASTEXITCODE -ne 0) { throw 'Solution restore failed.' }
dotnet build $solution -c Release --no-restore -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Solution build failed.' }
dotnet test --solution $solution -c Release --no-build --results-directory (Join-Path $repoRoot 'TestResults')
if ($LASTEXITCODE -ne 0) { throw 'Solution tests failed.' }

$arguments = @{ Version = $Version; PackageOutput = $output; NonGraphical = $NonGraphical }
if ($GodotPath) { $arguments.GodotPath = $GodotPath }
& (Join-Path $PSScriptRoot 'verify-package-consumers.ps1') @arguments
Write-Host "Verified three packages in $output"
