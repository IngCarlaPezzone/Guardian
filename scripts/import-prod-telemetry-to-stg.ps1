param(
  [switch]$IncludeTechnical,
  [switch]$Replace,
  [ValidateRange(1, 100000)][int]$Limit = 10000
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$prodEnvPath = Join-Path $root ".env"
$stgEnvPath = Join-Path $root ".env.stg"
if (!(Test-Path $prodEnvPath) -or !(Test-Path $stgEnvPath)) { throw "Se requieren .env (PROD) y .env.stg (STG)." }

function Read-DotEnv {
  param([string]$Path)
  $values = @{}
  foreach ($line in Get-Content -LiteralPath $Path) {
    $trimmed = $line.Trim()
    if (!$trimmed -or $trimmed.StartsWith("#")) { continue }
    $parts = $trimmed.Split("=", 2)
    if ($parts.Count -eq 2) { $values[$parts[0].Trim()] = $parts[1].Trim().Trim('"').Trim("'") }
  }
  return $values
}

$prod = Read-DotEnv $prodEnvPath
$stg = Read-DotEnv $stgEnvPath
$prodEnvironment = if ($prod.ContainsKey("GUARDIAN_ENVIRONMENT")) { $prod["GUARDIAN_ENVIRONMENT"].ToUpperInvariant() } else { "PROD" }
$stgEnvironment = if ($stg.ContainsKey("GUARDIAN_ENVIRONMENT")) { $stg["GUARDIAN_ENVIRONMENT"].ToUpperInvariant() } else { "" }
if ($prodEnvironment -ne "PROD" -or $stgEnvironment -ne "STG") { throw "Abortado: no se pudo verificar origen=PROD y destino=STG." }
if ($prod["DATABASE_URL"] -eq $stg["DATABASE_URL"] -or $prod["POSTGRES_DB"] -eq $stg["POSTGRES_DB"]) { throw "Abortado: PROD y STG no tienen DB inequívocamente distintas." }

$includeTechnicalValue = if ($IncludeTechnical) { "1" } else { "0" }

# This command runs only SELECT queries in the existing PROD app container. It emits no device IDs or full payloads.
$exportCode = @'
import json, os
from server.app.db import SessionLocal
from server.app.models import DeviceEvent
types = {"MissionStarted", "MissionFailed", "MissionSolved"}
if os.environ.get("TELEMETRY_INCLUDE_TECHNICAL") == "1":
    types.update({"Heartbeat", "RemoteConfigApplied", "UpdateCompleted", "MonitoringPaused", "MonitoringResumed", "TriggerMissionCommandReceived", "RemoteMissionTriggered"})
limit = int(os.environ["TELEMETRY_LIMIT"])
safe = {"mission_id", "missionId", "category_id", "categoryId", "level_id", "levelId", "skill_id", "skillId", "variant_id", "variantId", "attempt", "result"}
with SessionLocal() as db:
    rows = db.query(DeviceEvent).filter(DeviceEvent.event_type.in_(types)).order_by(DeviceEvent.occurred_at.asc(), DeviceEvent.id.asc()).limit(limit).all()
    slots = {}
    events = []
    for row in rows:
        slot = slots.setdefault(row.device_id, len(slots) + 1)
        payload = row.payload or {}
        events.append({"slot": slot, "occurred_at": row.occurred_at.isoformat(), "event_type": row.event_type, "client_version": row.client_version, "payload": {key: value for key, value in payload.items() if key in safe and (value is None or isinstance(value, (str, int, float, bool)))}})
print(json.dumps({"schema": "prod-telemetry-sanitized-v1", "events": events}, separators=(",", ":")))
'@

# Feed Python source through stdin. This avoids PowerShell/Docker argument quoting entirely.
$dataset = $exportCode | & docker compose --env-file $prodEnvPath -f (Join-Path $root "deploy\docker-compose.yml") exec -T -e "TELEMETRY_INCLUDE_TECHNICAL=$includeTechnicalValue" -e "TELEMETRY_LIMIT=$Limit" guardian-app python -
if ($LASTEXITCODE -ne 0) { throw "La exportación de sólo lectura desde PROD falló; STG no fue modificado." }
try { $parsed = $dataset | ConvertFrom-Json } catch { throw "La exportación no produjo un dataset sanitizado válido; STG no fue modificado." }
if ($parsed.schema -ne "prod-telemetry-sanitized-v1") { throw "Schema de exportación inesperado; STG no fue modificado." }

$importArgs = @("compose", "-p", "guardian-stg", "--env-file", $stgEnvPath, "-f", (Join-Path $root "deploy\docker-compose.stg.yml"), "exec", "-T", "guardian-stg-app", "python", "-m", "server.app.import_sanitized_telemetry")
if ($Replace) { $importArgs += "--replace" }
$dataset | & docker @importArgs
if ($LASTEXITCODE -ne 0) { throw "La importación STG falló." }
Write-Host "Importación terminada. Sólo se copiaron eventos sanitizados a dispositivos ficticios STG."
