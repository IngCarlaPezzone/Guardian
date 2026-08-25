# Estado actual — Guardian

## Entornos

Guardian opera con dos entornos aislados.

| Entorno | Compose | Servicios | Puerto Admin/API | Estado mutable |
| --- | --- | --- | --- | --- |
| PROD | `deploy/docker-compose.yml` | `postgres`, `guardian-app` (y `cloudflared` opcional) | `8080` | `.env`, `guardian_postgres_data`, `releases/` |
| STG | `deploy/docker-compose.stg.yml` | `guardian-stg-db`, `guardian-stg-app` | `8081` | `.env.stg`, `guardian_stg_postgres_data`, `releases-stg/` |

STG no comparte contenedores, volumen PostgreSQL, base, registry de dispositivos, RemoteConfig, telemetría, comandos ni releases con PROD. El Admin STG muestra persistentemente **Guardian Admin — STG**. PROD no muestra ese rótulo.

## Configuración

- `.env` es exclusivamente PROD y no se versiona.
- `.env.stg` es exclusivamente STG y no se versiona. Crear desde `.env.stg.example`, usando secretos distintos de PROD.
- `GUARDIAN_ENVIRONMENT=STG` es obligatorio para STG; el seed se niega a ejecutarse si falta.
- En STG local, `GUARDIAN_ADMIN_HOST` queda vacío para que el cliente TEST pueda usar la API en `localhost`; la barrera de hostname público de PROD se conserva sin cambios.
- No configurar Cloudflare público para STG en esta etapa.

## Operación STG

Desde la raíz del repositorio:

```powershell
Copy-Item .env.stg.example .env.stg
# Completar valores STG únicos en .env.stg
.\scripts\start-stg.ps1
.\scripts\seed-stg.ps1
```

Abrir `http://localhost:8081/admin/`. Para detener sólo STG:

```powershell
.\scripts\stop-stg.ps1
```

Para reconstruir la DB y releases de STG desde cero (requiere confirmación explícita):

```powershell
.\scripts\reset-stg.ps1 -Confirm
.\scripts\seed-stg.ps1
```

`reset-stg.ps1` usa exclusivamente el proyecto Compose `guardian-stg` y el volumen `guardian_stg_postgres_data`; no opera sobre PROD.

Para reconstruir código/contenedor STG sin borrar datos:

```powershell
.\scripts\update-stg.ps1
```

## Cliente y releases STG

El cliente de prueba se ejecuta sin tocar la instalación local de PROD:

```powershell
.\scripts\run-stg-client.ps1
```

Usa `http://localhost:8081`, `.env.stg`, y `%LOCALAPPDATA%\Guardian-STG-TEST`. Por lo tanto crea identidad y token propios de STG y no reutiliza `Guardian` ni la identidad productiva.
Después de `reset-stg.ps1`, recrear esa identidad con ` .\scripts\run-stg-client.ps1 -ResetIdentity`.

Para generar, copiar y registrar una release sólo en STG:

```powershell
.\scripts\publish-release-stg.ps1 -Version "0.1.0-staging-environment" -Description "Validación STG"
```

El artefacto se monta desde `releases-stg/` y el registro se guarda en la DB STG. Nunca aparece en Admin PROD ni puede ser seleccionado por un dispositivo PROD.

### Validación RC de persistencia tras reboot

La RC `0.4.2-rc.2` validó en un cliente TEST STG el flujo completo `0.4.1 → 0.4.2-rc.2 → reboot → 0.4.2-rc.2 → segundo reboot → 0.4.2-rc.2`.

- El valor `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Guardian` apunta a la ruta canónica y usa `--minimized`.
- Para un home STG explícito, la misma entrada incluye `--home <directorio-STG>`; así un reboot no cae en la instalación local de PROD.
- Se comprobó la reparación idempotente (`already_canonical`) y la reparación de una ruta temporal histórica simulada (`repaired_to_canonical`), seguida de reboot exitoso.
- Se comprobó el arranque local sin API disponible; cuando STG volvió a estar disponible, el cliente reanudó heartbeats sin requerir inicio manual.

`0.4.2-rc.1` queda descartada para promoción: resolvía el home explícito demasiado tarde durante el proceso de arranque. No promover esa RC a PROD.

## Versionado y promoción

PROD actual usa SemVer normal: `0.4.1`. Mientras una feature está en desarrollo, STG usa versiones exclusivas con sufijo de rama, por ejemplo `0.1.0-staging-environment`, `0.1.1-staging-environment` y `0.1.2-staging-environment`. Esas versiones no representan la numeración de PROD.

No usar una versión sin sufijo en STG salvo para reproducir de forma explícita una versión ya existente de PROD. Al aprobar la feature, crear una RC basada en la próxima versión productiva: si PROD es `0.4.1`, probar `0.4.2-rc` integralmente en STG —incluido updater cuando aplique—. Si aprueba, publicar `0.4.2` en PROD. Las versiones `-staging-*` y `-rc` nunca se publican en PROD.

El rollout obligatorio en PROD es: **PC TEST → validar operación → dispositivo productivo final**.

## Importación sanitizada de telemetría PROD → STG

Para poblar Activity o métricas con comportamiento real sin acoplar entornos:

```powershell
.\scripts\import-prod-telemetry-to-stg.ps1
# Incluye sólo eventos técnicos explícitamente permitidos además de las misiones
.\scripts\import-prod-telemetry-to-stg.ps1 -IncludeTechnical
# Reemplaza el dataset importado anterior en los dispositivos ficticios STG
.\scripts\import-prod-telemetry-to-stg.ps1 -Replace
```

El script exige `.env` PROD y `.env.stg` STG, comprueba `origen=PROD`, `destino=STG` y DBs diferentes antes de operar. Lee PROD mediante consultas `SELECT` dentro de `guardian-app`; sólo escribe en `guardian-stg-app`.

La whitelist por defecto es `MissionStarted`, `MissionFailed` y `MissionSolved`. Con `-IncludeTechnical` suma solamente eventos técnicos seguros definidos en el script. Conserva UTC/timestamp, tipo, versión de cliente y los campos educativos `mission_id`/`missionId`, categoría, nivel, skill, variant, intento y resultado. No transfiere device ID, hostname, display name, token, bootstrap token, perfil, nombre, apellido, fecha de nacimiento, RemoteConfig, comandos, releases, credenciales, respuestas ni el payload completo. Los eventos se asignan a dispositivos ficticios `STG-IMPORTED-TELEMETRY-*`; una segunda ejecución no duplica el mismo dataset y `-Replace` lo recrea explícitamente.

## Validación manual STG realizada

La validación manual de STG ya confirmó: aislamiento PROD/STG; reset STG sin afectar PROD; cliente WPF real contra STG; RemoteConfig; misión manual; telemetría; pause/resume; publicación de release STG; upgrade real `0.4.1 → 0.4.2`; downgrade real `0.4.2 → 0.4.1`; y PROD no afectado.

La importación sanitizada también fue validada manualmente contra PROD: la primera ejecución importó **281 eventos sanitizados**; la segunda importó **0 eventos nuevos**, confirmando idempotencia; y una ejecución posterior con `-Replace` importó **282 eventos**, porque PROD generó un evento nuevo entre corridas. No se copiaron identidades reales ni payloads completos, y PROD permaneció operativo.

## Promoción obligatoria

1. Partir de `main` actualizado y crear feature branch.
2. Ejecutar tests, build y self-test.
3. Desplegar la feature en STG, aplicar migraciones y validar Admin/API.
4. Validar Guardian TEST, RemoteConfig, misión, telemetría y, si aplica, release/updater/rollback en STG.
5. Con aprobación manual, mergear a `main` y desplegar PROD.
6. Validar primero con PC TEST contra PROD; continuar sólo después con el dispositivo productivo final.

Nunca se utiliza una feature branch sobre PROD para una vista previa visual o funcional.
