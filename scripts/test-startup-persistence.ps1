$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$keyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$valueName = "Guardian"
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("GuardianStartupPersistence-" + [guid]::NewGuid().ToString("N"))
$canonical = Join-Path $testRoot "canonical"
$legacy = Join-Path $testRoot "legacy-zip"
$release = Join-Path $testRoot "release-0.4.1"
$previousHome = $env:GUARDIAN_HOME
$previousValue = $null
$hadPreviousValue = $false
$running = @()

function Copy-Build([string]$target) {
  New-Item -ItemType Directory -Force -Path $target | Out-Null
  Copy-Item -Force (Join-Path $root "dist\Guardian.exe") (Join-Path $target "Guardian.exe")
  Copy-Item -Force (Join-Path $root "dist\Guardian.exe.config") (Join-Path $target "Guardian.exe.config")
  Copy-Item -Force (Join-Path $root "dist\GuardianUpdater.exe") (Join-Path $target "GuardianUpdater.exe")
}

function Stop-TestProcesses {
  foreach ($process in $running) {
    if ($process -and !$process.HasExited) { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue }
  }
  $running = @()
}

try {
  New-Item -ItemType Directory -Force -Path $canonical, $legacy, $release | Out-Null
  $env:GUARDIAN_HOME = $canonical
  $existing = Get-ItemProperty -Path $keyPath -Name $valueName -ErrorAction SilentlyContinue
  if ($null -ne $existing) { $previousValue = $existing.$valueName; $hadPreviousValue = $true }

  & (Join-Path $PSScriptRoot "build.ps1") -VersionOverride "0.4.0"
  Copy-Build $legacy

  & (Join-Path $PSScriptRoot "build.ps1") -VersionOverride "0.4.1"
  Copy-Build $release

  # Simulate an existing 0.4.0 installation and the historical stale Run value.
  Copy-Item -Force (Join-Path $legacy "*") $canonical
  Set-ItemProperty -Path $keyPath -Name $valueName -Value ('"' + (Join-Path $legacy "Guardian.exe") + '" --minimized')

  # Simulate the updater replacing only the canonical binaries with 0.4.1.
  Copy-Item -Force (Join-Path $release "*") $canonical
  @{ WatchdogEnabled = $false; AutoStartEnabled = $true; MonitoringEnabled = $true; DeviceId = [guid]::NewGuid().ToString(); GuardianServerUrl = ""; DeviceToken = ""; DeviceBootstrapToken = "" } |
    ConvertTo-Json -Compress | Set-Content -Encoding UTF8 -Path (Join-Path $canonical "config.json")

  $canonicalExe = Join-Path $canonical "Guardian.exe"
  $first = Start-Process -FilePath $canonicalExe -ArgumentList "--minimized --no-watchdog" -PassThru
  $running += $first
  Start-Sleep -Seconds 2
  $startup = (Get-ItemProperty -Path $keyPath -Name $valueName -ErrorAction Stop).$valueName
  $expected = '"' + $canonicalExe + '" --minimized --home "' + $canonical + '"'
  if ($startup -ne $expected) { throw "La migracion no reparo Run a la ruta canonica." }
  Stop-TestProcesses

  # A Windows Run process has no inherited GUARDIAN_HOME. --home must select
  # the explicit test installation before AppInfo resolves any local paths.
  $homeStartCount = @((Get-Content (Join-Path $canonical "events.jsonl") | Where-Object { $_ -match '"eventType":"GuardianStarted"' })).Count
  $env:GUARDIAN_HOME = $null
  $homeStartup = Start-Process -FilePath $canonicalExe -ArgumentList "--minimized --no-watchdog --home `"$canonical`"" -PassThru
  $running += $homeStartup
  Start-Sleep -Seconds 2
  Stop-TestProcesses
  $homeStartCountAfter = @((Get-Content (Join-Path $canonical "events.jsonl") | Where-Object { $_ -match '"eventType":"GuardianStarted"' })).Count
  if ($homeStartCountAfter -ne ($homeStartCount + 1)) { throw "--home startup did not use the explicit installation." }
  $env:GUARDIAN_HOME = $canonical

  # A reboot starts the Run target; it must be the 0.4.1 binary in canonical.
  $reboot = Start-Process -FilePath $canonicalExe -ArgumentList "--minimized --no-watchdog" -PassThru
  $running += $reboot
  Start-Sleep -Seconds 2
  Stop-TestProcesses
  $events = Get-Content -Raw -Path (Join-Path $canonical "events.jsonl")
  if ($events -notmatch '"clientVersion":"0.4.1"') { throw "El arranque simulado no reporto 0.4.1." }
  if ($events -notmatch '"startup_repair_result":"repaired_to_canonical"') { throw "No se registro la reparacion de startup." }

  # Invoking --install-startup from an extracted ZIP must never preserve that ZIP path.
  $zipExe = Join-Path $release "Guardian.exe"
  $install = Start-Process -FilePath $zipExe -ArgumentList "--install-startup" -Wait -PassThru
  if ($install.ExitCode -ne 0) { throw "--install-startup desde ZIP fallo." }
  $afterZip = (Get-ItemProperty -Path $keyPath -Name $valueName -ErrorAction Stop).$valueName
  if ($afterZip -ne $expected) { throw "--install-startup desde ZIP dejo una ruta no canonica." }
  $beforeSecondInstall = $afterZip
  $second = Start-Process -FilePath $zipExe -ArgumentList "--install-startup" -Wait -PassThru
  $afterSecondInstall = (Get-ItemProperty -Path $keyPath -Name $valueName -ErrorAction Stop).$valueName
  if ($second.ExitCode -ne 0 -or $afterSecondInstall -ne $beforeSecondInstall) { throw "La instalacion de startup no fue idempotente." }

  # A second copy sharing the data directory loses the mutex race and leaves evidence.
  $shadow = Join-Path $testRoot "shadow-copy"
  Copy-Build $shadow
  $primary = Start-Process -FilePath $canonicalExe -ArgumentList "--no-watchdog" -PassThru
  $running += $primary
  Start-Sleep -Seconds 1
  $duplicate = Start-Process -FilePath (Join-Path $shadow "Guardian.exe") -ArgumentList "--no-watchdog" -Wait -PassThru
  if ($duplicate.ExitCode -ne 0) { throw "La segunda instancia no salio limpiamente." }
  Stop-TestProcesses
  $events = Get-Content -Raw -Path (Join-Path $canonical "events.jsonl")
  if ($events -notmatch "GuardianDuplicateInstanceSkipped") { throw "La copia duplicada no quedo registrada." }

  Write-Host "STARTUP PERSISTENCE PASS"
}
finally {
  Stop-TestProcesses
  if ($hadPreviousValue) { Set-ItemProperty -Path $keyPath -Name $valueName -Value $previousValue }
  else { Remove-ItemProperty -Path $keyPath -Name $valueName -ErrorAction SilentlyContinue }
  $env:GUARDIAN_HOME = $previousHome
  if ($testRoot.StartsWith([System.IO.Path]::GetTempPath(), [System.StringComparison]::OrdinalIgnoreCase) -and (Test-Path $testRoot)) {
    Remove-Item -Recurse -Force -LiteralPath $testRoot
  }
  & (Join-Path $PSScriptRoot "build.ps1")
}
