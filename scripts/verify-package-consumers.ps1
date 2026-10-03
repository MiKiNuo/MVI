#requires -Version 7.2

param(
    [string] $GodotPath = (Join-Path $PSScriptRoot '../.scratch/mvi-v2/tools/godot-4.6.2/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64_console.exe')
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runId = [Guid]::NewGuid().ToString('N')
$version = '2.0.0-consumer19.' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss') + '.run' + $runId
$workspace = Join-Path ([IO.Path]::GetTempPath()) ('mvi-package-consumers-' + $runId)
$feed = Join-Path $workspace 'feed'
$logs = Join-Path $workspace 'logs'
$templates = Join-Path $repoRoot 'test/MiKiNuo.Mvi.PackageConsumers'
$commands = [Collections.Generic.List[object]]::new()
$results = [Collections.Generic.List[object]]::new()
$encoding = [Text.UTF8Encoding]::new($true)
$oldPackages = $env:NUGET_PACKAGES

function Write-Utf8([string] $Path, [string] $Content) {
    [IO.File]::WriteAllText($Path, $Content.Replace("`r`n", "`n").Replace("`n", "`r`n"), $encoding)
}

function Invoke-Logged([string] $Name, [string] $Executable, [string[]] $Arguments, [switch] $AllowFailure) {
    $log = Join-Path $logs ($Name + '.log')
    & $Executable @Arguments 2>&1 | Out-File -LiteralPath $log -Encoding utf8
    $code = $LASTEXITCODE
    $commands.Add([ordered]@{ name = $Name; executable = $Executable; arguments = $Arguments; exitCode = $code; log = $log })
    Write-Utf8 (Join-Path $workspace 'commands.json') ($commands | ConvertTo-Json -Depth 8)
    Write-Host "$Name exit=$code"
    if ($code -ne 0 -and !$AllowFailure) { throw "$Name failed: $log" }
    return $code
}

function Invoke-Gui([string] $Name, [string] $Executable, [string[]] $Arguments) {
    $stdout = Join-Path $logs ($Name + '.stdout.log')
    $stderr = Join-Path $logs ($Name + '.stderr.log')
    $quoted = ($Arguments | ForEach-Object { '"' + $_ + '"' }) -join ' '
    $process = Start-Process -FilePath $Executable -ArgumentList $quoted -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    if (!$process.WaitForExit(120000)) {
        $process.Kill($true)
        $process.WaitForExit()
        throw "$Name timed out: $stdout, $stderr"
    }
    $process.Refresh()
    $commands.Add([ordered]@{ name = $Name; executable = $Executable; arguments = $Arguments; exitCode = $process.ExitCode; stdout = $stdout; stderr = $stderr })
    Write-Utf8 (Join-Path $workspace 'commands.json') ($commands | ConvertTo-Json -Depth 8)
    Write-Host "$Name exit=$($process.ExitCode)"
    if ($process.ExitCode -ne 0) { throw "$Name failed: $stdout, $stderr" }
    if ($Name -eq 'godot-run' -and ((Get-Content -LiteralPath $stdout -Raw) + (Get-Content -LiteralPath $stderr -Raw)) -match '(?m)^(ERROR|WARNING):') {
        throw 'Godot emitted an engine error or warning.'
    }
}

function New-Consumer([string] $Name, [string[]] $Packages, [switch] $Godot, [switch] $Library) {
    $directory = Join-Path $workspace $Name
    New-Item -ItemType Directory -Path $directory | Out-Null
    $sdk = if ($Godot) { 'Godot.NET.Sdk/4.6.1' } else { 'Microsoft.NET.Sdk' }
    $outputType = if ($Library -or $Godot) { 'Library' } else { 'Exe' }
    $hostProperties = if ($Godot) {
        '<AssemblyName>MiKiNuo.Mvi.Samples.Godot</AssemblyName><EnableDynamicLoading>true</EnableDynamicLoading><PackageVersion_GodotSharp>4.6.2</PackageVersion_GodotSharp><PackageVersion_Godot_SourceGenerators>4.6.2</PackageVersion_Godot_SourceGenerators>'
    } else { '' }
    $references = ($Packages | ForEach-Object { '<PackageReference Include="' + $_ + '" Version="' + $version + '" />' }) -join "`n"
    if ($Name -eq 'Avalonia') {
        $references += "`n" + '<PackageReference Include="Avalonia.Desktop" Version="12.0.2" /><PackageReference Include="Avalonia.Themes.Fluent" Version="12.0.2" />'
    }
    Write-Utf8 (Join-Path $directory 'Consumer.csproj') @"
<Project Sdk="$sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>$outputType</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>obj/generated</CompilerGeneratedFilesOutputPath>
    <ErrorLog>diagnostics.sarif,version=2.1</ErrorLog>
    $hostProperties
  </PropertyGroup>
  <ItemGroup>
    $references
  </ItemGroup>
  <Target Name="RecordPackageAssets" BeforeTargets="CoreCompile">
    <WriteLinesToFile File="analyzers.txt" Lines="@(Analyzer->'%(FullPath)')" Overwrite="true" />
    <WriteLinesToFile File="references.txt" Lines="@(ReferencePath->'%(FullPath)')" Overwrite="true" />
    <WriteLinesToFile File="project-references.txt" Lines="@(ProjectReference)" Overwrite="true" />
    <WriteLinesToFile File="imports.txt" Lines="`$(MSBuildAllProjects)" Overwrite="true" />
  </Target>
</Project>
"@
    return $directory
}

function Check-ConsumerAssets([string] $Directory, [int] $ExpectedGenerated, [string[]] $ExpectedFrameworkPackages) {
    $analyzers = @(Get-Content -LiteralPath (Join-Path $Directory 'analyzers.txt') | Where-Object { $_ -like '*MiKiNuo.Mvi.Generators.dll' })
    $references = @(Get-Content -LiteralPath (Join-Path $Directory 'references.txt') | Where-Object { $_ -like '*MiKiNuo.Mvi*.dll' })
    $generated = @(Get-ChildItem -LiteralPath (Join-Path $Directory 'obj/generated/MiKiNuo.Mvi.Generators') -Recurse -Filter '*.cs')
    $assetFiles = @(Get-ChildItem -LiteralPath $Directory -Recurse -Filter 'project.assets.json')
    if ($assetFiles.Count -ne 1) { throw "Expected one NuGet asset manifest in $Directory." }
    $assets = Get-Content -LiteralPath $assetFiles[0].FullName -Raw | ConvertFrom-Json
    $framework = @($assets.libraries.PSObject.Properties.Name | Where-Object { $_ -like 'MiKiNuo.Mvi*' } | Sort-Object)
    $expected = @($ExpectedFrameworkPackages | ForEach-Object { $_ + '/' + $version } | Sort-Object)
    if ($analyzers.Count -ne 1 -or $generated.Count -ne $ExpectedGenerated -or $references.Count -ne $expected.Count `
        -or @($references | Sort-Object -Unique).Count -ne $references.Count -or (Compare-Object $framework $expected)) {
        throw "Unexpected framework assets in $Directory (analyzers=$($analyzers.Count), generated=$($generated.Count), references=$($references.Count))."
    }
    if ((Get-Content -LiteralPath (Join-Path $Directory 'project-references.txt') -Raw) -or `
        (Get-Content -LiteralPath (Join-Path $Directory 'imports.txt') -Raw).Contains($repoRoot)) {
        throw "Source project or repository build configuration leaked into $Directory."
    }
    $deps = @(Get-ChildItem -LiteralPath $Directory -Filter '*.deps.json' -Recurse | Where-Object { $_.FullName -notmatch '[\\/]obj[\\/]' })
    $runtimeFramework = @()
    foreach ($dependencyFile in $deps) {
        $dependency = Get-Content -LiteralPath $dependencyFile.FullName -Raw | ConvertFrom-Json
        $names = @($dependency.libraries.PSObject.Properties.Name)
        if ($names -match 'MiKiNuo.Mvi.Generators|MiKiNuo.Mvi.Infrastructure|Microsoft.CodeAnalysis') {
            throw "Compile-time tools entered runtime dependencies: $($dependencyFile.FullName)."
        }
        if ((Split-Path $Directory -Leaf) -eq 'Core' -and $names -match 'Avalonia|Godot') { throw 'Core has GUI dependencies.' }
        if ((Split-Path $Directory -Leaf) -eq 'Avalonia' -and $names -match 'Godot') { throw 'Avalonia has Godot dependencies.' }
        if ((Split-Path $Directory -Leaf) -eq 'Godot' -and $names -match 'Avalonia') { throw 'Godot has Avalonia dependencies.' }
        $runtimeFramework += @($names | Where-Object { $_ -like 'MiKiNuo.Mvi*' })
    }
    $results.Add([ordered]@{ consumer = (Split-Path $Directory -Leaf); analyzerPaths = $analyzers; generatedFiles = @($generated.FullName); referencePaths = $references; packages = $framework; runtimePackages = $runtimeFramework })
    Write-Utf8 (Join-Path $workspace 'assets.json') ($results | ConvertTo-Json -Depth 8)
}

New-Item -ItemType Directory -Path $feed, $logs | Out-Null
Write-Host "Workspace=$workspace"
Write-Host "Version=$version"
if ($workspace.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Consumers must be outside the repository.' }
for ($ancestor = [IO.DirectoryInfo]::new($workspace).Parent; $null -ne $ancestor; $ancestor = $ancestor.Parent) {
    foreach ($name in @('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', '.editorconfig')) {
        if (Test-Path -LiteralPath (Join-Path $ancestor.FullName $name)) { throw "Ambient build configuration: $($ancestor.FullName)/$name" }
    }
}
$configFeed = [Security.SecurityElement]::Escape($feed)
Write-Utf8 (Join-Path $workspace 'NuGet.Config') @"
<configuration>
  <packageSources><clear /><add key="candidate" value="$configFeed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping><packageSource key="candidate"><package pattern="MiKiNuo.Mvi*" /></packageSource><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping>
</configuration>
"@
$candidateDirectories = @('src/MiKiNuo.Mvi', 'src/MiKiNuo.Mvi.Avalonia', 'src/MiKiNuo.Mvi.Godot', 'src/MiKiNuo.Mvi.Generators', `
    'sample/MiKiNuo.Mvi.Samples.Avalonia/Features/V2Auth', 'sample/MiKiNuo.Mvi.Samples.Avalonia/Features/Auth', 'sample/MiKiNuo.Mvi.Samples.Godot', 'test/MiKiNuo.Mvi.PackageConsumers')
$sourceFiles = @($candidateDirectories | ForEach-Object { Get-ChildItem -LiteralPath (Join-Path $repoRoot $_) -Recurse -File } | `
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|\.godot)[\\/]' -and $_.Extension -in @('.cs', '.csproj', '.json', '.godot', '.tscn') })
$sourceFiles += @(Get-Item -LiteralPath $PSCommandPath, (Join-Path $repoRoot 'Directory.Build.props'), (Join-Path $repoRoot 'Directory.Build.targets'), `
    (Join-Path $repoRoot 'Directory.Packages.props'), (Join-Path $repoRoot '.editorconfig'))
$sourceHashes = @($sourceFiles | Sort-Object FullName -Unique | ForEach-Object { [ordered]@{ path = [IO.Path]::GetRelativePath($repoRoot, $_.FullName); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } })
$candidateText = ($sourceHashes | ForEach-Object { $_.path + '|' + $_.sha256 }) -join "`n"
$candidateHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($candidateText)))
$head = (& git -C $repoRoot rev-parse HEAD) | Select-Object -Last 1
Write-Utf8 (Join-Path $workspace 'candidate.json') ([ordered]@{ head = $head; sourceSha256 = $candidateHash; version = $version; files = $sourceHashes } | ConvertTo-Json -Depth 8)

try {
    Set-Location -LiteralPath $repoRoot
    $env:NUGET_PACKAGES = Join-Path $workspace 'cache'
    $projects = @('MiKiNuo.Mvi', 'MiKiNuo.Mvi.Avalonia', 'MiKiNuo.Mvi.Godot')
    foreach ($project in $projects) {
        $file = Join-Path $repoRoot "src/$project/$project.csproj"
        [void](Invoke-Logged "$project-build" 'dotnet' @('build', $file, '-t:Rebuild', '-c', 'Release', "-p:Version=$version", '-p:UseSharedCompilation=false', '-p:NuGetAudit=false'))
    }
    foreach ($project in $projects) {
        $file = Join-Path $repoRoot "src/$project/$project.csproj"
        [void](Invoke-Logged "$project-pack" 'dotnet' @('pack', $file, '-c', 'Release', '--no-build', "-p:Version=$version", "-p:PackageVersion=$version", '-o', $feed))
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $packages = @(Get-ChildItem -LiteralPath $feed -Filter '*.nupkg')
    if ($packages.Count -ne 3) { throw 'The candidate must contain exactly three packages.' }
    $packageEvidence = @()
    foreach ($package in $packages) {
        $zip = [IO.Compression.ZipFile]::OpenRead($package.FullName)
        try {
            $entries = @($zip.Entries.FullName)
            $analyzerEntries = @($entries | Where-Object { $_ -like '*MiKiNuo.Mvi.Generators.dll' })
            $isCore = $package.Name -eq "MiKiNuo.Mvi.$version.nupkg"
            if (($isCore -and ($analyzerEntries.Count -ne 1 -or $analyzerEntries[0] -ne 'analyzers/dotnet/cs/MiKiNuo.Mvi.Generators.dll')) `
                -or (!$isCore -and $analyzerEntries.Count -ne 0) -or ($entries -match 'lib/.*/MiKiNuo.Mvi.Generators|Infrastructure|Directory.Build|editorconfig')) {
                throw "Invalid compile-time package boundary: $($package.Name)"
            }
            $nuspec = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' }
            $reader = [IO.StreamReader]::new($nuspec.Open())
            try { $nuspecContent = $reader.ReadToEnd() } finally { $reader.Dispose() }
            if ($nuspecContent -match 'MiKiNuo.Mvi.Generators|MiKiNuo.Mvi.Infrastructure|MiKiNuo.Mvi.Domain|MiKiNuo.Mvi.Application|MiKiNuo.Mvi.Presentation') { throw 'A fourth framework package is required.' }
            if ($isCore -and $nuspecContent -match 'Avalonia|Godot') { throw 'Core package depends on a GUI platform.' }
            $packageEvidence += [ordered]@{ file = $package.FullName; sha256 = (Get-FileHash -LiteralPath $package.FullName -Algorithm SHA256).Hash; entries = $entries; nuspec = $nuspecContent }
        } finally { $zip.Dispose() }
    }
    Write-Utf8 (Join-Path $workspace 'packages.json') ($packageEvidence | ConvertTo-Json -Depth 8)

    $core = New-Consumer 'Core' @('MiKiNuo.Mvi')
    Copy-Item -LiteralPath (Join-Path $templates 'Core/Program.cs') -Destination $core
    $avalonia = New-Consumer 'Avalonia' @('MiKiNuo.Mvi.Avalonia')
    Copy-Item -LiteralPath (Join-Path $templates 'Avalonia/Program.cs') -Destination $avalonia
    Get-ChildItem -LiteralPath (Join-Path $repoRoot 'sample/MiKiNuo.Mvi.Samples.Avalonia/Features/V2Auth') -Filter '*.cs' | `
        Where-Object { $_.Name -notlike 'Remount*' } | Copy-Item -Destination $avalonia
    Get-ChildItem -LiteralPath (Join-Path $repoRoot 'sample/MiKiNuo.Mvi.Samples.Avalonia/Features/Auth') -Filter '*.cs' | Copy-Item -Destination $avalonia
    # The v1 sample's registration attribute is excluded only from this external staging copy; slice 20 removes the old registration at its source.
    $httpCopy = Join-Path $avalonia 'HttpAuthService.cs'
    $httpSource = [IO.File]::ReadAllText($httpCopy).Replace('using MiKiNuo.Mvi.Domain.DI;', '').Replace('[DiService(ServiceLifetime.Singleton, ServiceType = typeof(IAuthService))]', '')
    Write-Utf8 $httpCopy $httpSource
    $godot = New-Consumer 'Godot' @('MiKiNuo.Mvi.Godot') -Godot
    Get-ChildItem -LiteralPath (Join-Path $repoRoot 'sample/MiKiNuo.Mvi.Samples.Godot') -File | `
        Where-Object { $_.Extension -in @('.cs', '.tscn', '.godot') } | Copy-Item -Destination $godot
    $dual = New-Consumer 'Dual' @('MiKiNuo.Mvi.Avalonia', 'MiKiNuo.Mvi.Godot')
    Copy-Item -LiteralPath (Join-Path $templates 'Core/Program.cs'), (Join-Path $templates 'Dual/PlatformAssets.cs') -Destination $dual

    foreach ($consumer in @($core, $avalonia, $godot, $dual)) {
        $name = Split-Path $consumer -Leaf
        [void](Invoke-Logged "$name-build" 'dotnet' @('build', (Join-Path $consumer 'Consumer.csproj'), '-c', 'Debug', '-p:UseSharedCompilation=false', '-p:NuGetAudit=false'))
        $expected = switch ($name) { 'Core' { @('MiKiNuo.Mvi') }; 'Avalonia' { @('MiKiNuo.Mvi', 'MiKiNuo.Mvi.Avalonia') }; 'Godot' { @('MiKiNuo.Mvi', 'MiKiNuo.Mvi.Godot') }; 'Dual' { @('MiKiNuo.Mvi', 'MiKiNuo.Mvi.Avalonia', 'MiKiNuo.Mvi.Godot') } }
        $count = if ($name -in @('Core', 'Dual')) { 1 } else { 3 }
        Check-ConsumerAssets $consumer $count $expected
    }
    [void](Invoke-Logged 'core-run' 'dotnet' @((Join-Path $core 'bin/Debug/net10.0/Consumer.dll')))
    [void](Invoke-Logged 'dual-run' 'dotnet' @((Join-Path $dual 'bin/Debug/net10.0/Consumer.dll')))
    $avaloniaResult = Join-Path $workspace 'avalonia-result.txt'
    Invoke-Gui 'avalonia-run' 'dotnet' @((Join-Path $avalonia 'bin/Debug/net10.0/Consumer.dll'), "--result-path=$avaloniaResult")
    if (!(Get-Content -LiteralPath $avaloniaResult -Raw).StartsWith('PASS v2-auth:')) { throw 'Avalonia acceptance did not pass.' }
    $godotResult = Join-Path $workspace 'godot-result.json'
    Invoke-Gui 'godot-run' ([IO.Path]::GetFullPath($GodotPath)) @('--path', $godot, 'res://Composition.tscn', '--rendering-method', 'gl_compatibility', '--resolution', '1000x700', '--', '--composition-self-test', "--result-path=$godotResult")
    $godotReport = Get-Content -LiteralPath $godotResult -Raw | ConvertFrom-Json
    if ($godotReport.passed -ne $true) { throw 'Godot acceptance did not pass.' }

    $diagnosticResults = @()
    foreach ($case in (Get-Content -LiteralPath (Join-Path $templates 'diagnostics.json') -Raw | ConvertFrom-Json)) {
        $directory = New-Consumer $case.name @('MiKiNuo.Mvi') -Library
        Write-Utf8 (Join-Path $directory 'Declaration.cs') $case.source
        $code = Invoke-Logged $case.name 'dotnet' @('build', (Join-Path $directory 'Consumer.csproj'), '-p:UseSharedCompilation=false', '-p:NuGetAudit=false') -AllowFailure
        $sarif = Get-Content -LiteralPath (Join-Path $directory 'diagnostics.sarif') -Raw | ConvertFrom-Json
        $reported = @($sarif.runs.results | Where-Object { $_.ruleId -eq $case.id })
        $sourceLines = $case.source -split "`n"
        $expectedLines = @(for ($index = 0; $index -lt $sourceLines.Count; $index++) { if ($sourceLines[$index] -eq '// diagnostic') { $index + 2 } })
        $locations = @($reported.locations.physicalLocation)
        $actualLines = @($locations.region.startLine | Sort-Object -Unique)
        if ($code -eq 0 -or $reported.Count -eq 0 -or (Compare-Object $expectedLines $actualLines) `
            -or @($locations.artifactLocation.uri | Where-Object { $_ -notmatch 'Declaration\.cs$' }).Count -ne 0) {
            throw "Missing or misplaced $($case.id) in $($case.name): expected=$expectedLines, actual=$actualLines."
        }
        $diagnosticResults += [ordered]@{ name = $case.name; id = $case.id; exitCode = $code; declaration = (Join-Path $directory 'Declaration.cs'); locations = $locations; expectedLines = $expectedLines; sarif = (Join-Path $directory 'diagnostics.sarif') }
        Write-Utf8 (Join-Path $workspace 'diagnostic-results.json') ($diagnosticResults | ConvertTo-Json -Depth 12)
    }
    foreach ($file in $sourceHashes) {
        if ((Get-FileHash -LiteralPath (Join-Path $repoRoot $file.path) -Algorithm SHA256).Hash -ne $file.sha256) { throw "Candidate changed during validation: $($file.path)" }
    }
    Write-Utf8 (Join-Path $workspace 'result.json') ([ordered]@{ status = 'PASS'; version = $version; candidateSha256 = $candidateHash; packages = 3; consumers = 4; diagnosticCases = $diagnosticResults.Count; diagnosticIds = @($diagnosticResults.id | Sort-Object -Unique); avaloniaResult = $avaloniaResult; godotResult = $godotResult } | ConvertTo-Json -Depth 8)
    Write-Host "PASS: three fresh packages, four isolated consumers, both native UI scenarios, $($diagnosticResults.Count) located diagnostic cases. Evidence=$workspace"
} finally {
    $env:NUGET_PACKAGES = $oldPackages
}
