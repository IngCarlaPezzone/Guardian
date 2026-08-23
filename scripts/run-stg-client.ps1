param([switch]$ResetIdentity)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $root ".env.stg"
if (!(Test-Path $envPath)) { throw "Falta .env.stg. No se permite usar .env de PROD para Guardian TEST." }

$testHome = Join-Path $env:LOCALAPPDATA "Guardian-STG-TEST"
if ($ResetIdentity -and (Test-Path $testHome)) {
  Remove-Item -Recurse -Force -LiteralPath $testHome
}
& (Join-Path $PSScriptRoot "run-test-mode.ps1") `
  -ServerUrl "http://localhost:8081" `
  -ConfigDirectory $testHome `
  -EnvironmentFile ".env.stg"

Write-Host "Guardian TEST usa directorio aislado: $testHome"
