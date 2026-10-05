param([ValidateSet("Debug", "Release")][string]$Configuration = "Debug")
$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot
try {
    New-Item -ItemType Directory -Force -Path "artifacts" | Out-Null
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        [Console]::Error.WriteLine(".NET SDK not found. Install a .NET 10 SDK; no build or C# tests were executed.")
        exit 127
    }
    function Invoke-DotNetStep {
        param([string]$Name, [string[]]$Arguments)
        Write-Host "`n=== $Name ==="
        $previous = $ErrorActionPreference
        try {
            # Windows PowerShell 5 does not turn native nonzero exit codes into terminating errors.
            $ErrorActionPreference = "Continue"
            & dotnet @Arguments 2>&1 | Tee-Object -FilePath (Join-Path "artifacts" "$Name.log")
            $nativeExit = $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $previous }
        if ($nativeExit -ne 0) { throw "$Name failed with exit code $nativeExit. See artifacts/$Name.log." }
    }
    Invoke-DotNetStep -Name "environment" -Arguments @("--info")
    Invoke-DotNetStep -Name "restore" -Arguments @("restore", "MiKiNuo.Mvi.slnx")
    Invoke-DotNetStep -Name "build" -Arguments @("build", "MiKiNuo.Mvi.slnx", "-c", $Configuration, "--no-restore", "-bl:artifacts/build.binlog")
    $tests = [ordered]@{
        "main-tests" = "test/MiKiNuo.Mvi.Tests/MiKiNuo.Mvi.Tests.csproj"
        "v2-runtime-tests" = "test/MiKiNuo.Mvi.V2.Framework.Tests/MiKiNuo.Mvi.V2.Framework.Tests.csproj"
        "v2-generator-tests" = "test/MiKiNuo.Mvi.V2.Generator.Tests/MiKiNuo.Mvi.V2.Generator.Tests.csproj"
        "v2-auth-tests" = "sample/AuthenticationV2/Sample.Auth.Tests/Sample.Auth.Tests.csproj"
    }
    foreach ($entry in $tests.GetEnumerator()) {
        Invoke-DotNetStep -Name $entry.Key -Arguments @("run", "--project", $entry.Value, "-c", $Configuration, "--no-build", "--no-restore", "--", "--maximum-parallel-tests", "1")
    }
    Invoke-DotNetStep -Name "v2-headless" -Arguments @("run", "--project", "sample/AuthenticationV2/Sample.Auth.SmokeTests/Sample.Auth.SmokeTests.csproj", "-c", $Configuration, "--no-build", "--no-restore")
    Invoke-DotNetStep -Name "benchmark-discovery" -Arguments @("run", "--project", "test/MiKiNuo.Mvi.Benchmarks/MiKiNuo.Mvi.Benchmarks.csproj", "-c", $Configuration, "--no-build", "--no-restore", "--", "--list", "flat")
    Write-Host "All requested build/test processes exited successfully. This is not an AOT, GUI, SMTP or performance certification."
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
finally { Pop-Location }
