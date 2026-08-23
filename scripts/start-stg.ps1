$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $root ".env.stg"
if (!(Test-Path $envPath)) { throw "Falta .env.stg. Copia .env.stg.example y completa secretos exclusivos de STG." }
New-Item -ItemType Directory -Force -Path (Join-Path $root "releases-stg") | Out-Null
docker compose -p guardian-stg --env-file $envPath -f (Join-Path $root "deploy\docker-compose.stg.yml") up -d
Write-Host "Guardian STG iniciado: http://localhost:8081/admin/"
