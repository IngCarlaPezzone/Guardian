# Guardian — Estado actual

## Versión y propósito

La versión productiva de partida es `0.4.1`. Guardian es una aplicación Windows local-first que presenta misiones educativas y dispone de administración remota simple mediante HTTP y polling.

## Arquitectura

```text
Guardian Client Windows → Guardian Server/API → PostgreSQL
Guardian Admin          → Guardian Server/API
Guardian Updater        → Releases publicadas
```

- Cliente: WPF/.NET Framework, persistencia local en `%LOCALAPPDATA%\Guardian`.
- Server/Admin: FastAPI, SQLAlchemy, Alembic y Jinja2.
- Base central: PostgreSQL; los tests usan SQLite en memoria.
- Releases: ZIP con `Guardian.exe`, `Guardian.exe.config` y `GuardianUpdater.exe`.

## Componentes y flujos vigentes

- Registro de dispositivo, heartbeat, polling de RemoteConfig, cola de telemetría y comandos remotos.
- `Device.display_name` es el nombre visible editable; `machine_name` permanece como hostname técnico.
- `DeviceConfiguration` conserva intervalo y configuración de misiones por dispositivo.
- El perfil privado de misiones se almacena separado por dispositivo y no se registra en telemetría.
- Eventos se guardan en UTC en `device_events`; el cliente mantiene `events.jsonl` y `events-pending.jsonl` para operación offline.
- El updater valida SHA-256, realiza backup, reemplaza binarios y soporta rollback/downgrade.

## Mission System v2

Las misiones siguen la jerarquía Categoría → Nivel → Skill → Variante.

- Matemática / Operaciones básicas: sumas, restas y multiplicaciones.
- Comprensión / Comprensión funcional: identidad, edad y nacimiento, fecha actual, relaciones temporales, calendario y estaciones.
- Una misión por disparo; los reintentos conservan `mission_id`, skill y variante.
- La rotación es global, persiste entre reinicios y se reinicia al cambiar el día local.
- La telemetría de misión incluye categoría, nivel, skill, variante e intento, sin respuestas ni valores privados.

## Estructura relevante

```text
src/Guardian/              Cliente Windows
updater/                   Updater separado
server/app/                API, Admin, modelos y templates
server/migrations/         Migraciones Alembic
server/tests/              Tests FastAPI/SQLAlchemy
scripts/                   Build, test, server y publicación
docs/                      Documentación vigente
docs/archive/              Referencias históricas
```

## Operación

- Build: `./scripts/build.ps1`
- Self-test: `./scripts/self-test.ps1`
- Tests server: `./.venv/Scripts/python.exe -m pytest server/tests`
- Migraciones: aplicar Alembic mediante el flujo de servidor documentado.
- Publicación de release: `./scripts/publish-release.ps1 -Description "..."`.

Las pruebas se realizan primero en PC TEST. No enviar una actualización al dispositivo productivo hasta validar el updater, el reinicio, el heartbeat y la actividad de la release en PC TEST.

## Documentación de referencia

- [Arquitectura](architecture.md), [seguridad](security.md), [despliegue](deployment.md) y [flujo de actualización](update-flow.md).
- Specs finales de Mission System v2.
- Spec Stage 3A + 3B para el rediseño de Admin y métricas.

## Deuda técnica vigente

- Modernizar LockWindow y su accesibilidad sin alterar la lógica educativa.
- Revisar UX de acierto/error y respuestas largas en cliente.
- Considerar una futura unificación de `missionId` y `mission_id`.
- Validar el valor práctico del tiempo de resolución antes de darle mayor protagonismo.
