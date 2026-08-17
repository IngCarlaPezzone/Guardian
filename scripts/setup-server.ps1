$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $root ".env"

docker --version | Out-Null
docker compose version | Out-Null

if (!(Test-Path $envPath)) {
  throw "Falta .env. Copia .env.example a .env y completa secretos reales localmente."
}

& (Join-Path $PSScriptRoot "start-server.ps1")

$deadline = (Get-Date).AddSeconds(90)
while ((Get-Date) -lt $deadline) {
  try {
    $health = Invoke-RestMethod -Uri "http://localhost:8080/health" -TimeoutSec 5
    if ($health.status -eq "ok") {
      Write-Host "Health OK: http://localhost:8080/health"
      Write-Host "Admin: http://localhost:8080/admin/"
      exit 0
    }
  } catch {
    Start-Sleep -Seconds 3
  }
}

throw "Guardian Server no respondio health OK dentro del timeout."
