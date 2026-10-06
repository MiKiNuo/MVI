param([ValidateSet("Debug", "Release")][string]$Configuration = "Debug")
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
Push-Location $PSScriptRoot
try {
    New-Item -ItemType Directory -Path "artifacts" -Force | Out-Null
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        ".NET SDK not found; install a .NET 10 SDK before building." | Set-Content "artifacts/build-prerequisite.log"
        Write-Host ".NET SDK not found. Install .NET 10 SDK."
        exit 127
    }
    function Invoke-DotNet {
        param([string]$Step, [string[]]$Arguments)
        Write-Host "=== $Step ==="
        # Windows PowerShell 5.1 may wrap native stderr as ErrorRecord; retain it in the log.
        $saved = $ErrorActionPreference
        try {
            $ErrorActionPreference = "Continue"
            & dotnet @Arguments 2>&1 | Tee-Object -FilePath "artifacts/$Step.log"
            $code = $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $saved }
        if ($code -ne 0) { throw "$Step failed (exit $code). See artifacts/$Step.log." }
    }
    Invoke-DotNet -Step "environment" -Arguments @("--info")
    Invoke-DotNet -Step "restore" -Arguments @("restore", "MiKiNuo.Mvi.slnx")
    Invoke-DotNet -Step "build" -Arguments @("build", "MiKiNuo.Mvi.slnx", "-c", $Configuration, "--no-restore")
    Invoke-DotNet -Step "tests" -Arguments @("run", "--project", "test/MiKiNuo.Mvi.Tests", "-c", $Configuration, "--no-build", "--", "--maximum-parallel-tests", "1", "--results-directory", "artifacts/test-results")
    Invoke-DotNet -Step "benchmark-discovery" -Arguments @("run", "--project", "test/MiKiNuo.Mvi.Benchmarks", "-c", $Configuration, "--no-build", "--", "--list", "flat")
}
finally { Pop-Location }
