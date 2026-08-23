$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $root ".env.stg"
if (!(Test-Path $envPath)) { throw "Falta .env.stg." }
docker compose -p guardian-stg --env-file $envPath -f (Join-Path $root "deploy\docker-compose.stg.yml") exec -T guardian-stg-app python -m server.app.seed_stg
