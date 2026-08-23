$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $root ".env.stg"
if (!(Test-Path $envPath)) { throw "Falta .env.stg." }
docker compose -p guardian-stg --env-file $envPath -f (Join-Path $root "deploy\docker-compose.stg.yml") stop
Write-Host "Guardian STG detenido. PROD no fue afectado."
