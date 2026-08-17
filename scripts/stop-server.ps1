$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $root ".env"
docker compose --env-file $envPath -f (Join-Path $root "deploy\docker-compose.yml") stop
Write-Host "Guardian Server detenido sin borrar volumenes."
