# Guardian

Guardian es una aplicacion Windows local-first que pausa el uso de la computadora cada cierto intervalo y pide una mision matematica antes de continuar. La Etapa 1 agrega telemetria central, actividad por dispositivo y observabilidad del updater sin cambiar el motor de misiones.

Version actual: `0.3.1`.

## Componentes

- `src/Guardian/Guardian.cs`: Client Windows WPF/.NET Framework.
- `updater/src/GuardianUpdater.cs`: updater separado para reemplazo y rollback.
- `server/`: FastAPI + Admin web + SQLAlchemy/Alembic.
- `deploy/docker-compose.yml`: PostgreSQL, Guardian Server y Cloudflare Tunnel opcional.
- `scripts/`: build, pruebas, servidor, backup y releases manuales.

## Servidor local

Crear `.env` a partir de `.env.example` y completar secretos reales solo localmente.

```powershell
.\scripts\setup-server.ps1
```

URLs locales:

```text
http://localhost:8080/health
http://localhost:8080/admin/
```

Para detener sin borrar datos:

```powershell
.\scripts\stop-server.ps1
```

## Client

Build:

```powershell
.\scripts\build.ps1
```

Self-test:

```powershell
.\scripts\self-test.ps1
```

Modo test local, sin instalar en la PC administrada:

```powershell
.\scripts\run-test-mode.ps1
```

El modo test manual usa como unica configuracion `%LOCALAPPDATA%\Guardian\config.json`. Si el dispositivo ya tiene `DeviceToken`, lo reutiliza. Si todavia no lo tiene, toma `DEVICE_BOOTSTRAP_TOKEN` desde `.env` local.

`.guardian-test-data` queda reservado para `scripts\acceptance-test.ps1`, porque ese test borra y recrea datos de manera controlada.

## Configuracion remota

Etapa 0 expone solo `IntervalSeconds` como parametro funcional remoto. El Client:

- genera `DeviceId` UUID persistente;
- registra `machine_name` y version;
- guarda `DeviceToken` localmente;
- envia heartbeat;
- consulta configuracion por polling;
- conserva la ultima configuracion valida si el servidor cae.
- borra `DeviceBootstrapToken` despues del registro exitoso y conserva solo `DeviceToken`.

## Telemetria central

El Client mantiene `events.jsonl` local y ademas usa `events-pending.jsonl` como cola persistente. Cada evento tiene `eventId` UUID y `clientVersion`.

Hotfix `0.3.1`: los archivos de telemetria se escriben con mutex interproceso y los errores temporales de I/O nunca deben cerrar Guardian. Guardian tambien protege una sola instancia activa por usuario/directorio de datos.

Flujo:

1. el evento se escribe localmente;
2. queda pendiente;
3. el Client intenta enviar batches a `/api/v1/events`;
4. el servidor persiste en PostgreSQL y responde IDs aceptados;
5. el Client elimina de pendientes solo los eventos confirmados.

Si no hay servidor o red, Guardian sigue funcionando y reintenta luego con backoff. El servidor deduplica por `event_id`.

Desde Admin, cada dispositivo tiene enlace `Ver actividad` con filtros por hoy, ayer, fecha especifica, todos, misiones, configuracion, actualizaciones y errores.

## Releases

`VERSION` es la fuente principal de version.

```powershell
.\scripts\publish-release.ps1
```

El script ejecuta self-tests, build, genera ZIP, calcula SHA-256, copia a `releases/` y registra metadata en Guardian Server si `.env` existe y el stack esta arriba.

Para validar releases, usar siempre el orden:

1. publicar release;
2. actualizar primero la PC de prueba/staging;
3. revisar actividad y version reportada;
4. recien despues ordenar manualmente el mismo update al dispositivo real.

## Instalacion

```powershell
.\scripts\package-installer.ps1
```

El paquete distribuible no incluye scripts `.bat`: se descomprime y se ejecuta `Guardian.exe`. Si no existe `config.json`, la app pregunta la URL del servidor y el bootstrap token. En actualizaciones preserva `config.json`, `events.jsonl`, `DeviceId`, `DeviceToken` y `GuardianServerUrl`.

El ZIP no contiene `.env`, bootstrap tokens ni tokens de dispositivo. Para una instalacion limpia en una PC con una version vieja, cerrar Guardian, quitar autoarranque con `Guardian.exe --uninstall-startup` y borrar `%LOCALAPPDATA%\Guardian` antes de ejecutar de nuevo.
