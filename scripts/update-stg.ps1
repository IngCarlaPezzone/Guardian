$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $root ".env.stg"
if (!(Test-Path $envPath)) { throw "Falta .env.stg." }
docker compose -p guardian-stg --env-file $envPath -f (Join-Path $root "deploy\docker-compose.stg.yml") build guardian-stg-app
docker compose -p guardian-stg --env-file $envPath -f (Join-Path $root "deploy\docker-compose.stg.yml") up -d guardian-stg-app
Write-Host "Guardian STG actualizado sin tocar PROD."
