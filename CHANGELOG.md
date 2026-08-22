# Changelog

## 0.4.1

- Cierra la iteración Mission System v2, validada manualmente en PC TEST y lista para probar actualización remota en PC TEST.
- Incorpora Comprensión funcional Nivel 1 y skills configurables de Matemática y Comprensión, con una misión por disparo y rotación global persistente.
- Añade perfil privado para misiones y telemetría por categoría, nivel, skill y variante.
- Corrige el ajuste visual de preguntas y respuestas largas en la ventana de misión.
- Impide guardar una configuración de misiones sin habilidades habilitadas.
- Evita eventos repetidos de `MissionUnavailable` mientras no cambie el estado efectivo.
- Pendiente antes del rollout productivo: validar updater/release real en PC TEST. Deuda técnica/UI: modernizar Configurar misiones y LockWindow, mejorar tooltips y respuestas largas, aclarar “Pausado” y revisar `missionId` / `mission_id`.

## 0.4.0

- Incorpora Mission System v2: una misión por disparo, rotación global persistente y selección por habilidad.
- Agrega Comprensión funcional Nivel 1 y perfil privado por dispositivo para las preguntas personales.
- Amplía RemoteConfig y telemetría con identificadores estables de categoría, nivel, habilidad y variante.

## 0.2.0 - 2026-08-08

- Agrega Guardian Server local con FastAPI, PostgreSQL, migraciones y Admin web.
- Agrega registro de dispositivos con UUID persistente, heartbeat y configuracion remota de intervalo.
- Agrega publicacion manual de releases y GuardianUpdater separado con verificacion SHA-256 y rollback.
- Ajusta instaladores para preservar `config.json`, eventos y token de dispositivo durante actualizaciones.

## 0.1.0 - 2026-08-08

- Primer release instalable de Guardian para Windows por usuario.
- Agrega misiones matematicas a intervalos, bandeja de Windows, panel admin, salida admin y watchdog.
- Agrega paquete `GuardianInstaller.zip` con instalacion por doble click y autoarranque por usuario.
- Deja el instalador final en 15 minutos, con modo prueba de 60 segundos disponible solo si se activa manualmente.
- Usa interrupcion segura de audio por mute/restauracion, sin activar videos pausados.
