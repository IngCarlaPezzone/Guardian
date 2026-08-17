$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "dist\Guardian.exe"
$configDir = Join-Path $env:LOCALAPPDATA "Guardian"
$configPath = Join-Path $configDir "config.json"
$serverUrl = "http://localhost:8080"

function Read-DotEnvValue {
  param([string]$Name)

  $envPath = Join-Path $root ".env"
  if (!(Test-Path $envPath)) { return "" }

  foreach ($line in Get-Content -LiteralPath $envPath) {
    $trimmed = $line.Trim()
    if ($trimmed.Length -eq 0 -or $trimmed.StartsWith("#")) { continue }
    $parts = $trimmed.Split("=", 2)
    if ($parts.Count -ne 2) { continue }
    if ($parts[0].Trim() -eq $Name) {
      return $parts[1].Trim().Trim('"').Trim("'")
    }
  }
  return ""
}

function Stop-GuardianForTest {
  $processes = @(Get-Process Guardian -ErrorAction SilentlyContinue)
  if ($processes.Count -gt 0) {
    $processes | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
  }

  if (Test-Path $exe) {
    $previousHome = $env:GUARDIAN_HOME
    try {
      $env:GUARDIAN_HOME = $configDir
      Start-Process -FilePath $exe -ArgumentList "--unmute-audio" -Wait | Out-Null
    } finally {
      $env:GUARDIAN_HOME = $previousHome
    }
  }
}

function New-DefaultConfig {
  return [ordered]@{
    IntervalSeconds = 900
    TestIntervalSeconds = 60
    UseTestInterval = $true
    WatchdogEnabled = $false
    AutoStartEnabled = $true
    Difficulty = "9-11"
    DeviceId = ""
    MachineName = $env:COMPUTERNAME
    DisplayName = ""
    GuardianServerUrl = $serverUrl
    DeviceBootstrapToken = ""
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
}

function ConvertTo-ConfigMap {
  param($Config)

  $map = New-DefaultConfig
  if ($null -eq $Config) { return $map }

  foreach ($property in $Config.PSObject.Properties) {
    if ($property.Name -eq "EffectiveIntervalSeconds") { continue }
    $map[$property.Name] = $property.Value
  }
  return $map
}

if (!(Test-Path $exe)) {
  & (Join-Path $PSScriptRoot "build.ps1")
}

New-Item -ItemType Directory -Force -Path $configDir | Out-Null

$existing = $null
if (Test-Path $configPath) {
  try {
    $existing = Get-Content -Raw -LiteralPath $configPath | ConvertFrom-Json
  } catch {
    throw "No se pudo leer config existente en $configPath. Revisala antes de ejecutar el test: $($_.Exception.Message)"
  }
}

$config = ConvertTo-ConfigMap $existing
$bootstrapToken = if ($env:DEVICE_BOOTSTRAP_TOKEN) { $env:DEVICE_BOOTSTRAP_TOKEN } else { Read-DotEnvValue "DEVICE_BOOTSTRAP_TOKEN" }

$config["IntervalSeconds"] = if (($config["IntervalSeconds"] -as [int]) -gt 0) { [int]$config["IntervalSeconds"] } else { 900 }
$config["TestIntervalSeconds"] = 60
$config["UseTestInterval"] = $true
$config["WatchdogEnabled"] = $false
$config["MachineName"] = $env:COMPUTERNAME
$config["GuardianServerUrl"] = $serverUrl
$config["RemoteConfigPollSeconds"] = 60
$config["PauseMediaOnMission"] = $false
$config["AllowUnsafeMediaToggle"] = $false
$config["MuteSystemAudioDuringMission"] = $true
$config["ResumeMediaAfterMission"] = $false

if ([string]::IsNullOrWhiteSpace([string]$config["DeviceToken"])) {
  if ([string]::IsNullOrWhiteSpace($bootstrapToken)) {
    throw "No hay DeviceToken guardado y no se encontro DEVICE_BOOTSTRAP_TOKEN en el entorno ni en .env. Completa .env localmente para registrar el dispositivo de test."
  }
  $config["DeviceBootstrapToken"] = $bootstrapToken
}

($config | ConvertTo-Json -Compress) | Set-Content -Encoding UTF8 -Path $configPath

Stop-GuardianForTest

$previousHome = $env:GUARDIAN_HOME
try {
  $env:GUARDIAN_HOME = $configDir
  Start-Process -FilePath $exe -WorkingDirectory $root
} finally {
  $env:GUARDIAN_HOME = $previousHome
}

Start-Sleep -Seconds 3
$running = @(Get-Process Guardian -ErrorAction SilentlyContinue)
if ($running.Count -ne 1) {
  throw "Se esperaba una unica instancia de Guardian, pero hay $($running.Count)."
}

Write-Host "Guardian iniciado en modo prueba."
Write-Host "Config unica de test: $configPath"
Write-Host "Servidor: $serverUrl"
