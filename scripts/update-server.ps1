$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $root ".env"
if (!(Test-Path $envPath)) {
  throw "Falta .env."
}

docker compose --env-file $envPath -f (Join-Path $root "deploy\docker-compose.yml") build guardian-app
docker compose --env-file $envPath -f (Join-Path $root "deploy\docker-compose.yml") up -d guardian-app
Write-Host "Guardian Server actualizado sin borrar volumenes."
