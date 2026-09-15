$ErrorActionPreference = "Stop"

# Simulates the updater found on the production device: it only installs the
# executable files and omits Assets entirely. The new Guardian binary must
# recover the narration from its embedded resource into the canonical home.
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root "dist"
$testInstall = Join-Path ([System.IO.Path]::GetTempPath()) ("GuardianLegacyUpdater-" + [guid]::NewGuid().ToString("N"))
$oldHome = $env:GUARDIAN_HOME

try {
  New-Item -ItemType Directory -Force -Path $testInstall | Out-Null
  Copy-Item -LiteralPath (Join-Path $source "Guardian.exe") -Destination (Join-Path $testInstall "Guardian.exe")
  Copy-Item -LiteralPath (Join-Path $source "Guardian.exe.config") -Destination (Join-Path $testInstall "Guardian.exe.config")
  Copy-Item -LiteralPath (Join-Path $source "GuardianUpdater.exe") -Destination (Join-Path $testInstall "GuardianUpdater.exe")
  $env:GUARDIAN_HOME = $testInstall

  $process = Start-Process -FilePath (Join-Path $testInstall "Guardian.exe") -ArgumentList "--self-test" -Wait -PassThru
  if ($process.ExitCode -ne 0) { throw "Guardian self-test failed after legacy updater simulation: $($process.ExitCode)" }

  $narration = Join-Path $testInstall "Assets\Audio\multiply-by-10-100-1000.mp3"
  if (!(Test-Path -LiteralPath $narration) -or (Get-Item -LiteralPath $narration).Length -eq 0) {
    throw "Embedded narration was not restored after legacy updater simulation."
  }
  Write-Host "LEGACY UPDATER ASSET BOOTSTRAP PASS"
}
finally {
  $env:GUARDIAN_HOME = $oldHome
  if ($testInstall.StartsWith([System.IO.Path]::GetTempPath(), [System.StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $testInstall)) {
    Remove-Item -Recurse -Force -LiteralPath $testInstall
  }
}
