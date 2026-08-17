$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $root ".env"
$backupDir = Join-Path $root "backups"
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$target = Join-Path $backupDir "guardian-$stamp.sql"

docker compose --env-file $envPath -f (Join-Path $root "deploy\docker-compose.yml") exec -T postgres sh -c 'pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB"' | Set-Content -Encoding UTF8 -Path $target

Get-ChildItem $backupDir -Filter "guardian-*.sql" | Sort-Object LastWriteTime -Descending | Select-Object -Skip 7 | Remove-Item -Force
Write-Host "Backup creado: $target"
