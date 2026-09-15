#requires -Version 5.1
param([Parameter(Mandatory)][string]$PackageDirectory)
$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'T2.3-D CoreHost qualification requires Windows.' }

$root = (Resolve-Path "$PSScriptRoot/..").Path
$feed = (Resolve-Path $PackageDirectory).Path
$artifacts = Join-Path $root '.artifacts/t23d'
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

$cache = Join-Path $artifacts ('packages-' + [guid]::NewGuid().ToString('N'))
dotnet restore "$root/MagasinOutil.Pilot.slnx" --configfile $config --packages $cache
if ($LASTEXITCODE -ne 0) { throw 'T2.3-D pilot restore failed.' }
dotnet build "$root/MagasinOutil.Pilot.slnx" -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'T2.3-D pilot build failed.' }

$hostDll = Join-Path $root 'src/MagasinOutil.CoreHost/bin/Release/net10.0/MagasinOutil.CoreHost.dll'
$readerDepsPath = Join-Path $root 'src/MagasinOutil.ReadClient/bin/Release/net10.0/MagasinOutil.ReadClient.deps.json'
$hostDepsPath = Join-Path $root 'src/MagasinOutil.CoreHost/bin/Release/net10.0/MagasinOutil.CoreHost.deps.json'
$state = Join-Path $artifacts ('state-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $state | Out-Null

function Start-QualifiedCore([string]$pipeName) {
    $start = [System.Diagnostics.ProcessStartInfo]::new('dotnet')
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $start.StandardErrorEncoding = [System.Text.UTF8Encoding]::new($false)
    # 120 seconds is a qualification fixture only, not the final product policy.
    $start.Arguments = '"{0}" --simulation {1} "{2}" 120' -f $hostDll, $pipeName, $state
    $process = [System.Diagnostics.Process]::Start($start)
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    while ($watch.Elapsed.TotalSeconds -lt 30) {
        $pending = $process.StandardOutput.ReadLineAsync()
        if (-not $pending.Wait(30000)) { break }
        $line = $pending.GetAwaiter().GetResult()
        if ($null -eq $line) { break }
        if ($line.StartsWith('READY ')) {
            return [pscustomobject]@{ Process = $process; Ready = $line }
        }
    }

    $stderr = $process.StandardError.ReadToEnd()
    if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
    $process.Dispose()
    throw "CoreHost did not become ready for T2.3-D. STDERR: $stderr"
}

function Stop-QualifiedCore($started) {
    if ($null -eq $started) { return }
    if (-not $started.Process.HasExited) { $started.Process.Kill(); $started.Process.WaitForExit() }
    $started.Process.Dispose()
}

$first = $null
$second = $null
try {
    $first = Start-QualifiedCore ('wm-t23d-a-' + [guid]::NewGuid().ToString('N'))
    if ($first.Ready -notmatch 'installation=(inst1_[A-Za-z0-9_-]+)') { throw 'CoreHost readiness does not expose the durable installation identity.' }
    $installationA = $Matches[1]
    if ($first.Ready -notmatch 'license=Missing') { throw 'Fresh qualification state must report a missing installed license.' }
    if ($first.Ready -notmatch 'approved-license-keys=0') { throw 'Simulation composition must not embed an approved production issuer key.' }

    $licensingDb = Join-Path $state 'licensing.db'
    if (-not (Test-Path $licensingDb)) { throw 'CoreHost did not create licensing.db.' }
    if ((Get-Item $licensingDb).Length -le 0) { throw 'CoreHost licensing.db is empty.' }
    Write-Host 'PASS CoreHost composes the durable licensing SQLite store and creates licensing.db.'
    Write-Host 'PASS CoreHost creates a cryptographic installation identity without embedding an issuer private key.'
    Write-Host 'PASS Simulation CoreHost starts with no approved production license signing key.'

    Stop-QualifiedCore $first
    $first = $null

    $second = Start-QualifiedCore ('wm-t23d-b-' + [guid]::NewGuid().ToString('N'))
    if ($second.Ready -notmatch 'installation=(inst1_[A-Za-z0-9_-]+)') { throw 'Restarted CoreHost readiness does not expose the installation identity.' }
    $installationB = $Matches[1]
    if ($installationA -ne $installationB) { throw 'CoreHost restart changed the durable installation identity.' }
    Write-Host 'PASS CoreHost restart with the same state directory preserves the authoritative installation identity.'

    $hostDeps = Get-Content $hostDepsPath -Raw
    foreach ($required in @(
        'Platform.Poc.Licensing.Contracts',
        'Platform.Poc.Licensing.Runtime',
        'Platform.Poc.Licensing.Cryptography',
        'Platform.Poc.Licensing.Persistence.Sqlite')) {
        if ($hostDeps -notmatch [regex]::Escape($required)) { throw "CoreHost does not compose required licensing package '$required'." }
    }
    Write-Host 'PASS CoreHost runtime composition contains the common licensing contracts, runtime, verifier and SQLite adapter packages.'

    $readerDeps = Get-Content $readerDepsPath -Raw
    if ($readerDeps -match 'Platform.Poc.Licensing.Runtime|Platform.Poc.Licensing.Cryptography|Platform.Poc.Licensing.Persistence.Sqlite|Microsoft.Data.Sqlite') {
        throw 'ReadClient embeds concrete licensing authority or persistence dependencies.'
    }
    Write-Host 'PASS ReadClient does not embed the concrete licensing authority, cryptography or SQLite provider.'

    $platformProject = Get-Content (Join-Path $root 'src/MagasinOutil.Platform/MagasinOutil.Platform.csproj') -Raw
    if ($platformProject -match 'Platform.Poc.Licensing.Runtime|Platform.Poc.Licensing.Cryptography|Platform.Poc.Licensing.Persistence.Sqlite|Microsoft.Data.Sqlite') {
        throw 'MagasinOutil.Platform depends on concrete licensing authority or persistence packages.'
    }
    Write-Host 'PASS Pilot business/platform module remains independent of concrete licensing runtime, cryptography and SQLite composition.'

    Write-Host 'T2.3-D pilot CoreHost durable licensing composition: PASS'
} finally {
    Stop-QualifiedCore $first
    Stop-QualifiedCore $second
}
