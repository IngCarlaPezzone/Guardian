# Guardian Etapa 0 - Seguridad y privacidad

## Secretos

Los valores reales viven solo en `.env` o `config.json` local:

- `POSTGRES_PASSWORD`
- `GUARDIAN_ADMIN_INITIAL_PASSWORD`
- `GUARDIAN_SESSION_SECRET`
- `DEVICE_BOOTSTRAP_TOKEN`
- `CLOUDFLARE_TUNNEL_TOKEN`
- `DeviceToken`

El repositorio versiona solo `.env.example` con valores ficticios.

## Autenticacion

- Admin usa cookie `HttpOnly`, firma HMAC y password hasheado.
- Device usa Bearer token por dispositivo.
- Token de dispositivo se guarda hasheado en DB.
- Bootstrap token solo se usa para registro inicial.
- La ingesta de eventos usa el mismo Bearer token de dispositivo; no depende del login web ni de Cloudflare Access.

## Exposicion

- PostgreSQL no se publica a Internet.
- Cloudflare Tunnel es opcional y debe exponer solo Admin/API web.
- No hay CI/CD ni push automatico.

## Privacidad

No versionar datos locales:

- `.guardian-test-data/`
- `config.json`
- `events.jsonl`
- `events-pending.jsonl`
- backups
- releases binarios

Los payloads de eventos no deben contener tokens, passwords, bootstrap tokens, connection strings, datos personales ni configuracion privada. Los eventos de update guardan versiones, IDs tecnicos de release/comando y errores diagnosticos, no secretos.

Antes de publicar el repo, crear historial Git limpio y escanear nuevamente secretos/datos personales.
