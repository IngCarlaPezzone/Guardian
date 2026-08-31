# Guardian — Seguridad y privacidad

Este documento resume las reglas de seguridad y privacidad que deben mantenerse en Guardian.

## Secretos

Los valores reales deben vivir únicamente en archivos locales o variables de entorno no versionadas, como `.env` o `config.json`.

Entre ellos:

- `POSTGRES_PASSWORD`
- `GUARDIAN_ADMIN_INITIAL_PASSWORD`
- `GUARDIAN_SESSION_SECRET`
- `DEVICE_BOOTSTRAP_TOKEN`
- `CLOUDFLARE_TUNNEL_TOKEN`
- `DeviceToken`

El repositorio público debe incluir únicamente `.env.example` con valores ficticios.

Nunca deben versionarse:

- contraseñas;
- tokens;
- API keys;
- credenciales;
- cookies;
- connection strings reales;
- certificados privados.

Tampoco deben aparecer secretos dentro de logs, payloads de eventos o mensajes de error.

## Autenticación

### Guardian Admin

Guardian Admin utiliza autenticación propia mediante:

- contraseña almacenada de forma hasheada;
- sesión mediante cookie `HttpOnly`;
- firma de sesión con secreto del servidor.

Cuando Admin se expone fuera de la red local, puede protegerse adicionalmente mediante Cloudflare Access.

La autenticación de Cloudflare Access no reemplaza la autenticación interna de Guardian Admin.

### Dispositivos

Cada dispositivo utiliza un Bearer token propio para comunicarse con Guardian Server.

El token del dispositivo:

- se genera durante el registro;
- se almacena localmente en el dispositivo;
- se guarda de forma hasheada en PostgreSQL;
- no debe compartirse entre dispositivos;
- no debe registrarse en telemetría.

El `DEVICE_BOOTSTRAP_TOKEN` se utiliza únicamente para el registro inicial de un dispositivo y no debe funcionar como credencial permanente.

### Telemetría

La ingesta de eventos utiliza la autenticación del dispositivo.

No depende:

- de la sesión web del Admin;
- del login de Cloudflare Access.

## Exposición de servicios

### PostgreSQL

PostgreSQL no debe exponerse directamente a Internet.

Debe permanecer accesible únicamente dentro de la infraestructura interna necesaria para Guardian Server.

### Guardian Server / Admin

Guardian Server puede exponerse mediante Cloudflare Tunnel cuando se necesita acceso remoto.

La configuración pública debe exponer únicamente los servicios necesarios.

No publicar puertos internos adicionales sin una necesidad explícita.

### Cloudflare Tunnel

Los tokens reales de Cloudflare deben permanecer únicamente en variables de entorno locales.

Nunca deben subirse al repositorio público.

## Privacidad

Guardian puede generar información sensible sobre el uso de un dispositivo.

No versionar ni publicar datos reales como:

- nombres personales;
- información de menores;
- nombres de usuario reales;
- historial de actividad;
- respuestas a misiones;
- configuración real de dispositivos;
- identificadores de dispositivos reales;
- logs reales;
- backups reales.

La documentación pública debe utilizar siempre ejemplos neutrales.

## Archivos locales

No deben versionarse archivos o directorios locales como:

```text
.env
.guardian-test-data/
config.json
events.jsonl
events-pending.jsonl
backups/
dist/
release/
```

Los binarios de releases generados localmente tampoco deben incluirse en Git salvo decisión explícita del proyecto.

## Eventos y telemetría

Los payloads de eventos no deben contener:

- contraseñas;
- tokens;
- bootstrap tokens;
- connection strings;
- secretos de Cloudflare;
- configuración privada innecesaria;
- datos personales que no sean necesarios para la funcionalidad.

La excepción aprobada desde 0.4.6 es answer en MissionFailed y MissionSolved: se conserva para analizar la secuencia de intentos. Es dato sensible y sólo viaja por la persistencia local y sincronización de telemetría existentes. No debe aparecer en consola, errores, fixtures, documentación pública, importaciones sanitizadas hacia STG ni repositorios.

Los eventos de actualización pueden almacenar información técnica como:

- versión origen;
- versión destino;
- `release_id`;
- `command_id`;
- estado;
- errores de diagnóstico.

Nunca deben incluir credenciales.

## Repositorio público

Antes de publicar cambios:

1. revisar `git status`;
2. revisar `git diff`;
3. comprobar que `.env` sigue ignorado;
4. buscar nombres personales;
5. buscar rutas locales;
6. buscar emails;
7. verificar que no se hayan agregado logs, configuraciones o backups;
8. comprobar que no existan secretos en archivos versionados.

Ejemplos de búsquedas útiles:

```powershell
git grep -n -i -E "nombre_personal|email_personal"
git grep -n -E "C:\\Users\\|@gmail\.com|@hotmail\.com|@outlook\.com"
```

## Releases y actualizaciones

Los releases públicos deben contener únicamente los artefactos y metadata necesarios.

La metadata puede incluir:

- versión;
- descripción;
- SHA-256;
- nombre del artefacto.

No incluir secretos dentro de notas de release, nombres de archivo o metadata.

Los comandos de actualización y control remoto deben autenticarse y auditarse.

## Backups

Los backups reales de PostgreSQL o de datos locales:

- no deben subirse a Git;
- no deben hacerse públicos;
- deben almacenarse fuera del repositorio;
- deben tratarse como información privada.

## Principios

Guardian debe mantener estas reglas permanentes:

```text
secretos fuera del repositorio
datos personales fuera del repositorio
PostgreSQL no expuesto públicamente
telemetría sin credenciales
dispositivos autenticados individualmente
Admin protegido
logs y backups tratados como privados
```
