# Guardian Event Schema

Guardian escribe eventos locales en JSON Lines y, desde Etapa 1, sincroniza una copia pendiente hacia Guardian Server.

## Evento

```json
{
  "eventId": "11111111-1111-4111-8111-111111111111",
  "timestampLocal": "2026-08-07T22:13:51.0920964-03:00",
  "timestampUtc": "2026-08-08T01:13:51.0920964Z",
  "deviceId": "00000000-0000-4000-8000-000000000001",
  "machineName": "Sample-PC",
  "windowsUser": "SampleUser",
  "eventType": "MissionStarted",
  "clientVersion": "0.3.1",
  "payload": {
    "missionId": "c84750c2086d494aadb93318dff4eb7b",
    "prompt": "7 x 12 = ?",
    "elapsedSeconds": 900
  }
}
```

## Eventos V1

- `GuardianStarted`
- `GuardianStopped`
- `GuardianRestartedByWatchdog`
- `UsageCounterStarted`
- `UsageCounterPaused`
- `MissionStarted`
- `MissionHelpRequested`
- `MissionWritingHintShown`
- `MissionFailed`
- `MissionSolved`
- `DeviceLocked`
- `DeviceUnlocked`
- `RemoteConfigApplied`
- `DeviceRegistered`
- `HeartbeatSent`
- `HeartbeatFailed`
- `RemoteConfigReceived`
- `RemoteConfigFetched`
- `RemoteConfigFailed`
- `UpdateCommandReceived`
- `UpdateDownloadStarted`
- `UpdateDownloadCompleted`
- `UpdateInstallStarted`
- `UpdateCompleted`
- `UpdateFailed`
- `MediaPauseRequested`
- `MediaPauseSkipped`
- `MediaResumeRequested`
- `SystemAudioMuted`
- `SystemAudioRestored`
- `ExitAvailable`
- `ExitClicked`
- `AdminExitPromptOpened`
- `AdminExitFailed`
- `AdminExitSucceeded`
- `AdminShutdownRequested`
- `AutoExitAfterSolvedMissions`
- `TrayAdminActionSucceeded`
- `TrayAdminActionCancelled`
- `AdminPanelLoginFailed`
- `AdminPanelLoginSucceeded`
- `Error`
- `UnhandledError`

## Sincronizacion

`events.jsonl` se conserva como bitacora local. `events-pending.jsonl` contiene eventos todavia no confirmados por el servidor.

El servidor deduplica por `event_id`. El cliente elimina de pendientes solo los IDs incluidos en `accepted_event_ids`.

Desde `0.3.1`, el acceso local a `events-pending.jsonl` usa locking interproceso y los errores de telemetria no deben cerrar Guardian.

Desde `0.4.0`, los eventos de misión nuevos incluyen `mission_id`, `category_id`, `level_id`, `skill_id`, `variant_id` y `attempt`. Las respuestas ingresadas y los valores del perfil privado no se registran en `events.jsonl`, `events-pending.jsonl` ni se envían al servidor.

Desde `0.4.3-staging-comprehension-help`, los eventos de misión incluyen además `skill_level_id` (nivel pedagógico), `max_help_level`, `help_requests_count`, `had_orthographic_error`, `writing_correction_count` y `writing_answer_revealed`. `MissionHelpRequested` añade `help_level`; `MissionWritingHintShown` añade `writing_hint_stage`. Estos payloads nunca incluyen el input, la respuesta aceptada, texto de ayuda, prefijos ni datos del perfil privado.

## Configuracion Remota

Si `GuardianServerUrl` esta configurado, Guardian consulta la configuracion remota de Etapa 0.

Ejemplo:

```json
{
  "version": 3,
  "interval_seconds": 900,
  "updated_at": "2026-08-08T21:00:00Z"
}
```

Guardian debe seguir funcionando si el endpoint falla o no hay red.
