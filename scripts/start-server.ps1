$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $root ".env"
if (!(Test-Path $envPath)) {
  throw "Falta .env. Copia .env.example a .env y completa valores reales fuera de Git."
}

docker compose --env-file $envPath -f (Join-Path $root "deploy\docker-compose.yml") up -d
Write-Host "Guardian Server iniciado: http://localhost:8080"
