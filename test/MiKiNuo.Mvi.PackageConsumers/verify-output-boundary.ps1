#requires -Version 7.2
$ErrorActionPreference = 'Stop'
$entry = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../scripts/verify-package-consumers.ps1'))
$parseErrors = $null
$syntax = [Management.Automation.Language.Parser]::ParseFile($entry, [ref]$null, [ref]$parseErrors)
if ($parseErrors) { throw ($parseErrors | Out-String) }
$normalizeFunction = $syntax.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-NormalizedPackageVersion'
}, $true)
if (!$normalizeFunction) { throw 'NuGet version normalization function was not found.' }
. ([scriptblock]::Create($normalizeFunction.Extent.Text))
foreach ($value in @('2.0.0+build.7', '2.0', '2.0.0.0')) {
    if ((Get-NormalizedPackageVersion $value) -ne '2.0.0') { throw "NuGet identity was not normalized: $value" }
}
# Execute the entry's real export branch with already-verified package fixtures.
$branch = $syntax.Find({ param($node)
    $node -is [Management.Automation.Language.IfStatementAst] -and
    $node.Extent.Text.StartsWith('if ($PackageOutput)') -and
    $node.Extent.Text.Contains('foreach ($package in $packages)')
}, $true)
if (!$branch) { throw 'Package export branch was not found.' }
$export = [scriptblock]::Create($branch.Extent.Text)
$workspace = Join-Path ([IO.Path]::GetTempPath()) ('mvi-output-boundary-' + [Guid]::NewGuid().ToString('N'))
$feed = Join-Path $workspace 'feed'
New-Item -ItemType Directory -Path $feed | Out-Null
foreach ($name in @('MiKiNuo.Mvi', 'MiKiNuo.Mvi.Avalonia', 'MiKiNuo.Mvi.Godot')) {
    [IO.File]::WriteAllText((Join-Path $feed ($name + '.fixture.nupkg')), 'verified ' + $name)
}
$packages = @(Get-ChildItem -LiteralPath $feed -Filter '*.nupkg')
$PackageOutput = Join-Path $workspace 'mixed'
New-Item -ItemType Directory -Path $PackageOutput | Out-Null
$sentinel = Join-Path $PackageOutput 'unverified.nupkg'
[IO.File]::WriteAllText($sentinel, 'preserve this unverified package')
$rejected = $false
try { & $export } catch { $rejected = $true }
if (!$rejected) { throw 'Export accepted a fourth, unverified package.' }
if ([IO.File]::ReadAllText($sentinel) -ne 'preserve this unverified package') { throw 'The rejected output was modified.' }
if (@(Get-ChildItem -LiteralPath $PackageOutput -File).Count -ne 1) { throw 'Rejection must occur before exporting files.' }
$PackageOutput = Join-Path $workspace 'clean'
& $export
$copied = @(Get-ChildItem -LiteralPath $PackageOutput -Filter '*.nupkg')
if ($copied.Count -ne 3 -or (Compare-Object $packages.Name $copied.Name)) { throw 'Clean output must contain the verified three-package set.' }
foreach ($package in $packages) {
    if ([IO.File]::ReadAllText((Join-Path $PackageOutput $package.Name)) -ne [IO.File]::ReadAllText($package.FullName)) { throw 'Exported package bytes changed.' }
}
Write-Host "PASS: NuGet identities normalized; unexpected package refused before export and preserved; clean output contains only the verified three packages. Evidence=$workspace"
