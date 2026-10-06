param([Parameter(Mandatory = $true)][string]$Version)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot
if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    throw "Python 3.10+ is required for package validation. Install it and make python available on PATH."
}
& python (Join-Path $root "tools/release.py") pack --version $Version
if ($LASTEXITCODE -ne 0) { throw "Release gates failed (exit $LASTEXITCODE). No publication was attempted." }
