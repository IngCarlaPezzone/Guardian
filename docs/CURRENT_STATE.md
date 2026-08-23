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
.\scripts\publish-release-stg.ps1 -Description "Validación STG"
```

El artefacto se monta desde `releases-stg/` y el registro se guarda en la DB STG. Nunca aparece en Admin PROD ni puede ser seleccionado por un dispositivo PROD.

## Promoción obligatoria

1. Partir de `main` actualizado y crear feature branch.
2. Ejecutar tests, build y self-test.
3. Desplegar la feature en STG, aplicar migraciones y validar Admin/API.
4. Validar Guardian TEST, RemoteConfig, misión, telemetría y, si aplica, release/updater/rollback en STG.
5. Con aprobación manual, mergear a `main` y desplegar PROD.
6. Validar primero con PC TEST/Carla contra PROD; Guille sólo después.

Nunca se utiliza una feature branch sobre PROD para una vista previa visual o funcional.
