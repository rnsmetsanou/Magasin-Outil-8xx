#requires -Version 5.1
param([Parameter(Mandatory)][string]$PackageDirectory)
$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'The process qualification requires Windows.' }
$root = (Resolve-Path "$PSScriptRoot/..").Path
$feed = (Resolve-Path $PackageDirectory).Path
$artifacts = Join-Path $root '.artifacts/t01'
New-Item -ItemType Directory -Force $artifacts | Out-Null
$config = Join-Path $artifacts 'NuGet.Config'
$escapedFeed = [System.Security.SecurityElement]::Escape($feed)
@"
<configuration>
  <packageSources><clear/><add key="platform-local" value="$escapedFeed"/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources>
  <packageSourceMapping>
    <packageSource key="platform-local"><package pattern="Platform.Poc.*"/></packageSource>
    <packageSource key="nuget.org"><package pattern="*"/></packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content $config -Encoding UTF8
# Isolated package cache: do not accidentally reuse a different development build with the same version.
$cache = Join-Path $artifacts ('packages-' + [guid]::NewGuid().ToString('N'))
dotnet restore "$root/MagasinOutil.Pilot.slnx" --configfile $config --packages $cache
if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
dotnet build "$root/MagasinOutil.Pilot.slnx" -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$pipe = 'wm-pilot-' + [guid]::NewGuid().ToString('N')
$hostDll = Join-Path $root 'src/MagasinOutil.CoreHost/bin/Release/net10.0/MagasinOutil.CoreHost.dll'
$readerDll = Join-Path $root 'src/MagasinOutil.ReadClient/bin/Release/net10.0/MagasinOutil.ReadClient.dll'
$start = [System.Diagnostics.ProcessStartInfo]::new('dotnet')
$start.UseShellExecute = $false
$start.RedirectStandardOutput = $true
$start.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
# Windows file names cannot contain a quote; the generated pipe name contains only ASCII letters/digits/hyphens.
$start.Arguments = '"{0}" --simulation {1}' -f $hostDll, $pipe
$hostProcess = [System.Diagnostics.Process]::Start($start)
try {
    $readiness = [System.Diagnostics.Stopwatch]::StartNew()
    do {
        $remaining = [int][Math]::Ceiling(30000 - $readiness.Elapsed.TotalMilliseconds)
        if ($remaining -le 0) { throw 'Core readiness timed out after 30 seconds.' }
        $pendingLine = $hostProcess.StandardOutput.ReadLineAsync()
        if (-not $pendingLine.Wait($remaining)) { throw 'Core readiness timed out after 30 seconds.' }
        $line = $pendingLine.GetAwaiter().GetResult()
        if ($null -eq $line) { throw 'Core exited before readiness.' }
    } while (-not $line.StartsWith('READY '))
    $first = (& dotnet $readerDll $pipe client-a | Out-String | ConvertFrom-Json)
    if ($LASTEXITCODE -ne 0) { throw 'First client failed.' }
    $second = (& dotnet $readerDll $pipe client-b | Out-String | ConvertFrom-Json)
    if ($LASTEXITCODE -ne 0) { throw 'Second client failed.' }
    if ($first.Runtime.SessionId -ne $second.Runtime.SessionId -or $first.Runtime.RuntimeEpoch -ne $second.Runtime.RuntimeEpoch) { throw 'Client recreation changed Machine authority.' }
    if ($first.Places.Count -ne 140 -or @($first.Places | Where-Object { -not $_.Excluded }).Count -ne 137) { throw 'Wrong 8xx inventory.' }
    if ($first.Evidence.Origin -ne 'Magasin8xx.Simulation') { throw 'Simulation provenance missing.' }
    if ($first.Capabilities.Count -ne 1 -or $first.Capabilities[0] -ne 'application.tool-inventory.read') { throw 'Unexpected exposed capability.' }
    $clientDeps = Get-Content (Join-Path $root 'src/MagasinOutil.ReadClient/bin/Release/net10.0/MagasinOutil.ReadClient.deps.json') -Raw
    if ($clientDeps -match 'Platform.Poc.Machine.Runtime|Platform.Poc.Application.Runtime|Platform.Poc.Technology.Simulator|MagasinOutil.Core/') { throw 'Client embeds a concrete machine runtime.' }
    Write-Host 'Pilot T0/T1 package composition: PASS'
} finally {
    if (-not $hostProcess.HasExited) { $hostProcess.Kill(); $hostProcess.WaitForExit() }
    $hostProcess.Dispose()
}
