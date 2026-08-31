# Guardian Etapa 0 - Arquitectura

## Componentes

- Guardian Client: WPF/.NET Framework, local-first, instalado por usuario.
- Guardian Updater: ejecutable separado que descarga, valida, reemplaza binarios y hace rollback.
- Guardian Server: FastAPI, Admin web Jinja2 y API de dispositivos.
- PostgreSQL: persistencia via volumen Docker.
- Cloudflare Tunnel: opcional, parametrizado, solo para Admin.

## Flujo Client/API

1. Client carga `config.json` desde `GUARDIAN_HOME` si se define explicitamente; si no, usa `%LOCALAPPDATA%\Guardian`.
2. Si no tiene `DeviceId` UUID, genera uno y lo persiste.
3. Si tiene `GuardianServerUrl` y bootstrap token, se registra en `/api/v1/devices/register`.
4. Guarda `DeviceToken` local, borra `DeviceBootstrapToken` y usa `DeviceToken` como Bearer token.
5. Envia heartbeat y consulta config/update por polling.
6. Si el servidor falla, conserva la ultima configuracion valida.

## Modelo minimo

Tablas implementadas por Alembic:

- `admin_users`
- `devices`
- `device_configurations`
- `releases`
- `update_commands`
- `device_commands` para pausa, reanudacion y disparo manual remoto.
- `device_events`

`DeviceToken` y password admin se guardan hasheados.

## Etapa 1 - Telemetria central

El Client conserva `events.jsonl` como bitacora local y agrega `events-pending.jsonl` como cola persistente de sincronizacion. Cada evento incluye:

- `eventId` UUID generado en cliente;
- timestamps local/UTC;
- `deviceId`;
- `eventType`;
- `clientVersion`;
- `payload`.

El servidor expone `POST /api/v1/events`, autenticado con el Bearer token del dispositivo. Acepta batches, guarda los eventos en PostgreSQL y responde `accepted_event_ids`. La tabla `device_events` tiene `event_id` unico para tolerar reenvios sin duplicar datos.

La sincronizacion nunca bloquea misiones ni UI. Si falla, los eventos siguen en la cola pendiente y se reintentan mas tarde con backoff. Clientes viejos siguen siendo compatibles porque el endpoint nuevo es aditivo.

Desde `0.3.1`, todo acceso a `events-pending.jsonl` dentro de Guardian usa una capa unica con mutex interproceso. `EventLogger` y `TelemetrySync` no acceden directamente al archivo. Si el archivo esta temporalmente ocupado o hay un error de I/O, la telemetria falla de manera silenciosa y Guardian continua funcionando.

Desde `0.4.6`, los eventos `MissionFailed` y `MissionSolved` incluyen la respuesta original enviada, el intento y el nivel de ayuda vigente; los fallos agregan `failureReason`. Es una excepción acotada para análisis educativo: la respuesta se conserva sólo mediante el pipeline de telemetría local y central, no se escribe en consola ni se replica en la importación sanitizada hacia STG.

Guardian usa un mutex por directorio de datos para evitar dos instancias activas del cliente en el mismo usuario/maquina. Esto reduce colisiones durante arranque manual, watchdog o update. El Updater tambien espera explicitamente a que no queden procesos `Guardian` antes de reemplazar binarios.

## Controles remotos

El heartbeat persiste `monitoring_enabled` en el dispositivo. Admin muestra `Activo` cuando el dispositivo esta online y ese valor es verdadero, `Pausado` cuando sigue online pero es falso, y `Offline` cuando supera el umbral de heartbeat. La fuente de verdad local es `MonitoringEnabled` en `config.json`, por lo que un reinicio conserva la pausa.

Los controles usan `device_commands`, separados de `update_commands`: `pause_monitoring`, `resume_monitoring` y `trigger_mission_now`. El cliente los toma por polling autenticado, confirma estado y ejecuta los mismos metodos que usa la bandeja. Mientras esta pausado, la mision manual se permite y al resolverla Guardian continua pausado.

El heartbeat envía el valor vivo del controlador (`monitoring_enabled`), no una inferencia del servidor. Por eso una accion desde bandeja, reinicio o watchdog converge nuevamente en Admin con el siguiente heartbeat.
