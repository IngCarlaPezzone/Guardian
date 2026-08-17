# Changelog

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
