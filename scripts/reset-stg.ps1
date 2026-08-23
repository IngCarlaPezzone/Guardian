param([switch]$Confirm)
$ErrorActionPreference = "Stop"
if (!$Confirm) { throw "Este comando borra solo la DB y releases de STG. Reejecuta con -Confirm." }
$root = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $root ".env.stg"
if (!(Test-Path $envPath)) { throw "Falta .env.stg." }
docker compose -p guardian-stg --env-file $envPath -f (Join-Path $root "deploy\docker-compose.stg.yml") down -v --remove-orphans
if (Test-Path (Join-Path $root "releases-stg")) { Remove-Item -Recurse -Force -LiteralPath (Join-Path $root "releases-stg") }
& (Join-Path $PSScriptRoot "start-stg.ps1")
Write-Host "STG recreado desde cero. PROD no fue afectado. Ejecuta .\scripts\seed-stg.ps1 para datos ficticios."
