# Guardian Etapa 0 - Flujo de update

## Publicacion manual

```powershell
.\scripts\publish-release.ps1
```

El script:

1. lee `VERSION`;
2. ejecuta self-tests;
3. compila Client y Updater;
4. empaqueta `Guardian.exe`, `Guardian.exe.config` y `GuardianUpdater.exe`;
5. calcula SHA-256;
6. copia el ZIP a `releases/`;
7. registra metadata en Guardian Server si el stack esta arriba.

## Instalacion por updater

1. Admin crea `update_command = pending`.
2. Client detecta la orden y reporta `acknowledged`.
3. Client lanza `GuardianUpdater.exe` y cierra Guardian.
4. Updater descarga el ZIP autenticado.
5. Updater calcula SHA-256 y aborta si no coincide.
6. Updater crea backup de binarios vigentes.
7. Updater reemplaza `Guardian.exe` y config de aplicacion.
8. Updater agenda reemplazo diferido de `GuardianUpdater.exe`.
9. Updater inicia Guardian.
10. Si el arranque falla, restaura backup y reporta `rolled_back`.

Nunca se reemplazan ni eliminan `config.json`, `events.jsonl`, `DeviceId` ni `DeviceToken`.

## Validacion Etapa 1: staging -> real

La version `0.3.1` es un hotfix de Etapa 1 para telemetria robusta, una sola instancia y rollback/downgrade. Para probar un release:

1. Ejecutar `.\scripts\publish-release.ps1`.
2. Confirmar en Admin que el release aparece con SHA-256.
3. Enviar el update manualmente solo al dispositivo de prueba/staging.
4. Abrir `Ver actividad` de ese dispositivo y confirmar:
   - `UpdateCommandReceived`;
   - `UpdateDownloadStarted`;
   - `UpdateDownloadCompleted`;
   - `UpdateInstallStarted`;
   - `UpdateCompleted`;
   - `GuardianStarted` con la nueva version;
   - heartbeat posterior con la nueva version.
5. Confirmar que Guardian sigue funcionando normalmente.
6. Recien despues, enviar manualmente el mismo release al dispositivo real.
7. Repetir la verificacion de actividad, version y heartbeat.

Una orden aun en `pending` puede cancelarse desde Admin antes de que el cliente la tome. Si el dispositivo esta offline, Admin la muestra como pendiente y esperando conexion; no equivale a una instalacion en progreso. El cliente conserva `from_version`, `target_version` y `direction` en `update-context.json` antes de lanzar el updater, para que todos los eventos del ciclo, incluido `UpdateCompleted`, describan el cambio original.

La publicacion acepta `-Description` para asociar un resumen corto al release sin editar PostgreSQL manualmente.

No automatizar el envio al dispositivo real.

## Rollback / downgrade

Admin permite seleccionar explicitamente cualquier release publicado, incluido uno anterior a la version instalada. El cliente acepta cualquier target distinto de su version actual.

Los eventos de update incluyen:

```json
{
  "from_version": "0.3.1",
  "target_version": "0.3.0",
  "direction": "downgrade"
}
```

El Updater:

1. descarga y valida SHA-256;
2. reporta `installing`;
3. detiene procesos `Guardian`;
4. espera hasta que no queden instancias activas;
5. reemplaza binarios;
6. inicia Guardian;
7. reporta `success` o restaura backup y reporta rollback/fallo.

Los archivos locales `config.json`, `events.jsonl`, `events-pending.jsonl`, `DeviceId` y `DeviceToken` se preservan.
