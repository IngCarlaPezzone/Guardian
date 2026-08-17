$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "dist\Guardian.exe"
$updater = Join-Path $root "dist\GuardianUpdater.exe"

if (!(Test-Path $exe)) {
  & (Join-Path $PSScriptRoot "build.ps1")
}

$previousGuardianHome = $env:GUARDIAN_HOME
$testHome = Join-Path ([System.IO.Path]::GetTempPath()) ("GuardianSelfTest-" + [guid]::NewGuid().ToString("N"))

try {
  New-Item -ItemType Directory -Force -Path $testHome | Out-Null
  $env:GUARDIAN_HOME = $testHome

  $process = Start-Process -FilePath $exe -ArgumentList "--self-test" -Wait -PassThru
  if ($process.ExitCode -eq 0) {
    Write-Host "SELF-TEST PASS"
  } else {
    Write-Host "SELF-TEST FAIL $($process.ExitCode)"
    exit $process.ExitCode
  }
} finally {
  $env:GUARDIAN_HOME = $previousGuardianHome
  if ($testHome.StartsWith([System.IO.Path]::GetTempPath(), [System.StringComparison]::OrdinalIgnoreCase) -and (Test-Path $testHome)) {
    Remove-Item -Recurse -Force -LiteralPath $testHome
  }
}

$updaterProcess = Start-Process -FilePath $updater -ArgumentList "--self-test" -Wait -PassThru
if ($updaterProcess.ExitCode -eq 0) {
  Write-Host "UPDATER SELF-TEST PASS"
} else {
  Write-Host "UPDATER SELF-TEST FAIL $($updaterProcess.ExitCode)"
  exit $updaterProcess.ExitCode
}
