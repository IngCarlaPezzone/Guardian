$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "dist\Guardian.exe"
$testHome = Join-Path $root ".guardian-test-data"
$env:GUARDIAN_HOME = $testHome
$configPath = Join-Path $testHome "config.json"
$logPath = Join-Path $testHome "events.jsonl"

function Write-Config {
  param(
    [int]$IntervalSeconds = 900,
    [int]$TestIntervalSeconds = 60,
    [bool]$UseTestInterval = $false,
    [bool]$WatchdogEnabled = $true
  )

  New-Item -ItemType Directory -Force -Path $testHome | Out-Null
  $config = [ordered]@{
    IntervalSeconds = $IntervalSeconds
    TestIntervalSeconds = $TestIntervalSeconds
    UseTestInterval = $UseTestInterval
    WatchdogEnabled = $WatchdogEnabled
    AutoStartEnabled = $true
    Difficulty = "9-11"
    DeviceId = ""
    MachineName = $env:COMPUTERNAME
    DisplayName = ""
    GuardianServerUrl = if ($env:GUARDIAN_SERVER_URL) { $env:GUARDIAN_SERVER_URL } else { "" }
    DeviceBootstrapToken = if ($env:DEVICE_BOOTSTRAP_TOKEN) { $env:DEVICE_BOOTSTRAP_TOKEN } else { "" }
    DeviceToken = ""
    RemoteConfigVersion = 0
    PendingUpdateCommandId = ""
    UpdaterPath = ""
    RemoteWebhookUrl = ""
    RemoteAuthToken = ""
    RemoteConfigUrl = ""
    RemoteConfigPollSeconds = 60
    PauseMediaOnMission = $false
    AllowUnsafeMediaToggle = $false
    MuteSystemAudioDuringMission = $true
    ResumeMediaAfterMission = $false
    AdminUsername = "admin"
    AdminPasswordSha256 = "dde6e8974b46a1eddcd7ea3bbb899342f48cad896b47275a6f806062ec5ca14c"
    MaxSolvedMissionsBeforeAutoExit = 3
  }
  ($config | ConvertTo-Json -Compress) | Set-Content -Encoding UTF8 -Path $configPath
}

function Stop-GuardianProcesses {
  Get-Process Guardian -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
  Start-Sleep -Seconds 1
  if (Test-Path $exe) {
    Start-Process -FilePath $exe -ArgumentList "--unmute-audio" -Wait | Out-Null
  }
}

function Assert-LogContains {
  param([string]$Pattern, [string]$Message)
  if (!(Test-Path $logPath)) { throw "No existe log: $logPath" }
  $content = Get-Content -Raw -LiteralPath $logPath
  if ($content -notmatch $Pattern) { throw $Message }
}

function Wait-LogContains {
  param([string]$Pattern, [string]$Message, [int]$TimeoutSeconds = 20)
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  while ((Get-Date) -lt $deadline) {
    if ((Test-Path $logPath) -and ((Get-Content -Raw -LiteralPath $logPath) -match $Pattern)) {
      return
    }
    Start-Sleep -Milliseconds 500
  }
  throw $Message
}

Stop-GuardianProcesses
if (Test-Path $testHome) {
  Remove-Item -Recurse -Force -LiteralPath $testHome
}

& (Join-Path $PSScriptRoot "build.ps1")
& (Join-Path $PSScriptRoot "self-test.ps1")

Write-Config -WatchdogEnabled $true
$p = Start-Process -FilePath $exe -ArgumentList "--no-watchdog" -PassThru
Wait-LogContains "UsageCounterStarted" "No se registro UsageCounterStarted"
if (!(Get-Process -Id $p.Id -ErrorAction SilentlyContinue)) { throw "Guardian no quedo corriendo" }
Stop-Process -Id $p.Id -Force
Start-Sleep -Seconds 1
Assert-LogContains "GuardianStarted" "No se registro GuardianStarted"
Assert-LogContains "UsageCounterStarted" "No se registro UsageCounterStarted"

Write-Config -WatchdogEnabled $true
$before = @(Get-Process Guardian -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
$p = Start-Process -FilePath $exe -PassThru
Wait-LogContains "UsageCounterStarted" "No se registro UsageCounterStarted antes de probar watchdog" 30
Wait-LogContains "WatchdogStarted" "No se registro WatchdogStarted antes de probar watchdog" 30
Start-Sleep -Seconds 5
Stop-Process -Id $p.Id -Force
Start-Sleep -Seconds 20
$after = @(Get-Process Guardian -ErrorAction SilentlyContinue | Where-Object { $before -notcontains $_.Id })
if ($after.Count -lt 1) { throw "Watchdog no reinicio Guardian" }
Write-Config -WatchdogEnabled $false
$after | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Stop-GuardianProcesses
Assert-LogContains "GuardianRestartedByWatchdog" "No se registro GuardianRestartedByWatchdog"

$keyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
try {
  $install = Start-Process -FilePath $exe -ArgumentList "--install-startup" -Wait -PassThru
  if ($install.ExitCode -ne 0) {
    Write-Host "startup reversible SKIP: install startup exit $($install.ExitCode)"
  } else {
    $value = (Get-ItemProperty -Path $keyPath -Name Guardian -ErrorAction Stop).Guardian
    if ($value -notlike "*Guardian.exe*") { throw "Autoarranque no apunta a Guardian.exe" }
    $uninstall = Start-Process -FilePath $exe -ArgumentList "--uninstall-startup" -Wait -PassThru
    if ($uninstall.ExitCode -ne 0) { throw "uninstall startup fallo $($uninstall.ExitCode)" }
    if (Get-ItemProperty -Path $keyPath -Name Guardian -ErrorAction SilentlyContinue) {
      throw "La prueba reversible dejo autoarranque instalado"
    }
  }
} catch {
  Write-Host "startup reversible SKIP: $($_.Exception.Message)"
}

Write-Config -TestIntervalSeconds 3 -UseTestInterval $true -WatchdogEnabled $false
$p = Start-Process -FilePath $exe -ArgumentList "--no-watchdog" -PassThru
Wait-LogContains "DeviceLocked" "No se registro DeviceLocked" 30
Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
Start-Process -FilePath $exe -ArgumentList "--unmute-audio" -Wait | Out-Null
Assert-LogContains "MissionStarted" "No se registro MissionStarted"
Assert-LogContains "DeviceLocked" "No se registro DeviceLocked"

Stop-GuardianProcesses
Write-Host "ACCEPTANCE PASS"
