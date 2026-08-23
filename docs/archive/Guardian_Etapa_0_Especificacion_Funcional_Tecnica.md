# Guardian — Especificación funcional y técnica — Etapa 0

**Estado:** Base de implementación para Codex  
**Fecha:** 8 de agosto de 2026  
**Objetivo:** transformar Guardian en una aplicación administrable y actualizable de forma remota dentro del entorno doméstico, manteniendo el funcionamiento local actual y sin introducir automatización de despliegue todavía.

---

# 1. Objetivo de la Etapa 0

La Etapa 0 debe permitir que Guardian deje de depender de modificaciones manuales en la PC administrada.

Al finalizar esta etapa debe ser posible:

1. Ejecutar Guardian normalmente en la PC administrada.
2. Administrar desde una interfaz web el intervalo de disparo.
3. Identificar el dispositivo mediante un ID técnico estable y un nombre visible editable.
4. Conocer desde el servidor la versión instalada y la última comunicación del dispositivo.
5. Publicar una nueva versión de Guardian de forma controlada.
6. Ordenar desde el panel Admin que la PC administrada actualice Guardian.
7. Actualizar Guardian sin reinstalación manual.
8. Recuperar la versión anterior si una actualización falla.
9. Mantener Guardian operativo aunque el servidor quede temporalmente inaccesible.
10. Mantener toda la infraestructura principal en la PC servidor doméstica, usando Docker Desktop y sin pagar infraestructura cloud adicional.

---

# 2. Alcance funcional

## 2.1 Incluido

### Guardian Client

- conservar el comportamiento actual de Guardian;
- continuar funcionando sin servidor;
- generar un ID técnico persistente;
- registrar el nombre de la máquina Windows;
- almacenar un nombre visible asignado desde el Admin;
- enviar heartbeat al servidor;
- reportar versión instalada;
- consultar configuración remota;
- aplicar remotamente el intervalo de disparo;
- conservar la última configuración remota válida;
- consultar órdenes de actualización;
- lanzar Guardian Updater cuando corresponda.

### Guardian Admin

- login mediante usuario y contraseña;
- listar dispositivo/s registrado/s;
- mostrar:
  - nombre visible;
  - nombre de Windows;
  - ID técnico;
  - última comunicación;
  - estado estimado online/offline;
  - versión instalada;
  - versión disponible;
  - intervalo configurado;
- editar el nombre visible;
- editar el intervalo de disparo;
- visualizar releases disponibles;
- seleccionar una versión;
- ordenar actualización;
- visualizar resultado básico de una actualización.

### Guardian Server/API

- persistir dispositivos;
- persistir configuración;
- recibir heartbeat;
- entregar configuración;
- gestionar releases;
- gestionar órdenes de actualización;
- autenticar dispositivos;
- autenticar administrador;
- servir archivos de release por LAN;
- almacenar datos en PostgreSQL.

### Guardian Updater

- ejecutarse separado de Guardian Client;
- descargar una versión;
- verificar integridad;
- detener Guardian;
- preservar configuración/datos locales;
- hacer backup del binario vigente;
- instalar la nueva versión;
- iniciar Guardian;
- reportar resultado;
- restaurar versión anterior si la actualización falla.

### Infraestructura

- Windows 10 Home como host;
- Docker Desktop;
- Docker Compose;
- PostgreSQL;
- aplicación web/API;
- Cloudflare Tunnel para acceso al Guardian Admin mediante subdominio;
- API de dispositivos usada por LAN;
- persistencia en volúmenes;
- backup local de PostgreSQL;
- scripts manuales de setup/update.

---

## 2.2 Fuera de alcance

No implementar en Etapa 0:

- n8n;
- Telegram;
- inteligencia artificial;
- dashboard analítico;
- nuevas familias de desafíos;
- banco de preguntas;
- tablet;
- múltiples perfiles familiares;
- acceso del Guardian Client desde fuera de la casa;
- CI/CD automático;
- deploy automático del servidor;
- auto-update sin autorización del Admin;
- comportamiento especial al modificar el intervalo mientras un contador ya está en curso;
- sistema parental protegido a nivel servicio/kernel;
- firma digital de binarios;
- microservicios complejos;
- alta disponibilidad.

---

# 3. Estado actual confirmado del proyecto

Guardian ya existe y debe evolucionarse, no reescribirse.

Estado actual observado:

- aplicación Windows;
- .NET Framework / WPF;
- código principal en `src/Guardian/Guardian.cs`;
- compilación mediante `csc.exe` de Windows;
- versión actual `0.1.0`;
- build mediante PowerShell;
- ejecutable en `dist/Guardian.exe`;
- empaquetado en ZIP;
- instalación por usuario;
- instalación en `%LOCALAPPDATA%\Guardian`;
- autoarranque mediante:
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`;
- watchdog básico;
- icono de bandeja;
- panel admin local;
- configuración JSON;
- eventos JSON Lines;
- campos ya existentes para configuración remota;
- cliente HTTP remoto ya iniciado en el código;
- funcionamiento offline actual.

La Etapa 0 debe conservar estas capacidades salvo que esta especificación indique explícitamente un cambio.

---

# 4. Arquitectura final de Etapa 0

```text
                         INTERNET
                            │
                            │ solo Admin
                            ▼
                 guardian.<DOMINIO>
                            │
                    Cloudflare Tunnel
                            │
                            ▼
┌──────────────────────────────────────────────────┐
│             PC SERVIDOR — WINDOWS 10             │
│                                                  │
│ Docker Desktop                                   │
│                                                  │
│  ┌────────────────────────────────────────────┐  │
│  │ Guardian Server                           │  │
│  │ FastAPI + Admin web                       │  │
│  │                                            │  │
│  │ /admin/...      interfaz web              │  │
│  │ /api/v1/...     API Guardian              │  │
│  └─────────────────┬──────────────────────────┘  │
│                    │                             │
│  ┌─────────────────▼──────────────────────────┐  │
│  │ PostgreSQL                                │  │
│  └────────────────────────────────────────────┘  │
│                                                  │
│  releases/                                       │
│  backups/                                        │
└──────────────────────────▲───────────────────────┘
                           │
                           │ LAN doméstica
                           │
                           ▼
                 ┌─────────────────────┐
                 │ PC administrada     │
                 │                     │
                 │ Guardian Client     │
                 │ Guardian Updater    │
                 └─────────────────────┘
```

---

# 5. Decisiones arquitectónicas

## 5.1 Servidor

El servidor es una PC doméstica con:

- Windows 10 Home Single Language;
- Docker Desktop;
- funcionamiento continuo habitual.

No contratar infraestructura cloud para API o PostgreSQL.

---

## 5.2 Red

Guardian Client se comunica con Guardian API por LAN.

Ejemplo configurable:

```text
http://192.168.1.xxx:8080
```

No hardcodear IP.

Variable/configuración:

```text
GUARDIAN_SERVER_URL
```

Antes de instalar en la PC administrada debe recomendarse configurar una reserva DHCP para la PC servidor, para evitar cambios de IP.

El valor real se define durante deploy.

---

## 5.3 Admin público

El Admin debe ser accesible mediante:

```text
https://guardian.<DOMINIO_REAL>
```

El dominio utiliza Cloudflare DNS.

Cloudflare Tunnel expone exclusivamente la superficie web necesaria para Admin.

La API usada por Guardian Client debe continuar orientada a LAN.

El dominio exacto no debe estar hardcodeado.

Variable:

```text
GUARDIAN_ADMIN_HOST
```

---

# 6. Stack del servidor

Para mantener la solución simple:

- Python;
- FastAPI;
- SQLAlchemy;
- Alembic;
- PostgreSQL;
- Jinja2 para Guardian Admin;
- HTML/CSS/JS liviano;
- sin React/Vue/Angular en Etapa 0;
- Docker;
- Docker Compose.

El Admin y la API forman una sola aplicación lógica.

No crear microservicios separados salvo necesidad técnica imprescindible.

---

# 7. Estructura objetivo del repositorio

Crear un monorepo público.

```text
guardian/
├── client/
│   ├── src/
│   ├── scripts/
│   └── tests/
│
├── updater/
│   ├── src/
│   ├── scripts/
│   └── tests/
│
├── server/
│   ├── app/
│   │   ├── api/
│   │   ├── admin/
│   │   ├── db/
│   │   ├── models/
│   │   ├── schemas/
│   │   ├── services/
│   │   ├── templates/
│   │   └── static/
│   ├── migrations/
│   ├── tests/
│   └── Dockerfile
│
├── deploy/
│   ├── docker-compose.yml
│   ├── cloudflared/
│   └── scripts/
│
├── releases/
│   └── .gitkeep
│
├── docs/
│   ├── architecture.md
│   ├── deployment.md
│   ├── update-flow.md
│   └── security.md
│
├── .env.example
├── .gitignore
├── README.md
└── VERSION
```

Codex puede adaptar la estructura existente, pero debe llegar a una separación equivalente.

---

# 8. Repositorio público y privacidad

## 8.1 Regla principal

El repositorio será público.

No debe contener:

- nombres reales;
- apellido;
- edad;
- ciudad;
- provincia;
- IP LAN real;
- dominio real si no es necesario;
- claves;
- tokens;
- passwords;
- cookies;
- Cloudflare Tunnel token;
- credenciales PostgreSQL;
- configuración real de dispositivos;
- eventos reales;
- backups;
- historial real;
- archivos `.guardian-local`;
- archivos `.guardian-test-data`;
- bitácoras privadas;
- contenido familiar;
- documentos de ideas con información personal.

---

## 8.2 Historial Git

El proyecto subido contiene un directorio `.git`.

No asumir que ese historial es seguro para publicación.

Antes del primer push público:

1. crear copia de seguridad local;
2. auditar archivos;
3. eliminar datos privados;
4. eliminar el historial Git existente del árbol destinado a GitHub;
5. inicializar un repositorio nuevo;
6. realizar escaneo de secretos;
7. realizar búsqueda de datos personales;
8. realizar primer commit limpio;
9. recién entonces crear/pushear el repositorio público.

No intentar “borrar” secretos solamente mediante un commit posterior.

---

## 8.3 Archivos ignorados

`.gitignore` debe cubrir al menos:

```text
.env
.env.*
!.env.example

.guardian-local/
.guardian-test-data/

**/config.json
**/events.jsonl

backups/
releases/*.zip
releases/*.exe

*.log

.vscode/
.idea/

__pycache__/
.pytest_cache/
.venv/

bin/
obj/
dist/
```

Ajustar excepciones para archivos de ejemplo.

---

# 9. Modelo de dispositivo

Cada instalación debe tener dos identificadores.

## 9.1 Device UUID

ID técnico principal.

Características:

- UUID generado la primera vez;
- persistente;
- no derivado del nombre de Windows;
- no cambia si se renombra la PC;
- guardado en configuración local;
- enviado al servidor.

Ejemplo:

```text
5ce031cf-c0dd-45c0-92b7-8e14f2c8d6d2
```

---

## 9.2 Machine name

Guardar también:

```text
Environment.MachineName
```

como dato diagnóstico.

No usarlo como primary key.

---

## 9.3 Display name

Nombre amigable editable desde Admin.

Ejemplo:

```text
Test Device
```

No hardcodearlo en el repositorio.

---

# 10. Registro del dispositivo

## 10.1 Primer registro

Flujo:

```text
Guardian inicia
    ↓
no tiene device UUID/token
    ↓
genera UUID
    ↓
POST /api/v1/devices/register
    ↓
servidor valida bootstrap token
    ↓
registra dispositivo
    ↓
devuelve device token
    ↓
Guardian guarda token localmente
```

---

## 10.2 Bootstrap token

Usar un secreto de registro inicial.

Servidor:

```text
DEVICE_BOOTSTRAP_TOKEN
```

No incluir el valor real en Git.

Durante instalación/configuración real, el token se entrega al Client de forma local.

Luego del registro, el Client debe usar un token propio del dispositivo.

---

# 11. Autenticación del dispositivo

Cada dispositivo recibe:

```text
device_id
device_token
```

Las llamadas posteriores deben enviar, por ejemplo:

```http
Authorization: Bearer <device_token>
```

El servidor valida que el token corresponda al dispositivo.

Guardar en base solo una representación hash segura del token cuando sea viable.

---

# 12. Heartbeat

Guardian debe enviar heartbeat mientras está ejecutándose.

Frecuencia propuesta:

```text
60 segundos
```

Endpoint:

```http
POST /api/v1/devices/{device_id}/heartbeat
```

Payload:

```json
{
  "machine_name": "DESKTOP-XXXX",
  "client_version": "0.2.0",
  "effective_interval_seconds": 900
}
```

Respuesta:

```json
{
  "server_time": "ISO-8601",
  "config_version": 4,
  "pending_update": false
}
```

---

## 12.1 Online/offline

Admin considera:

- online: heartbeat reciente;
- offline: último heartbeat supera el umbral.

Umbral inicial:

```text
180 segundos
```

No usar WebSockets en Etapa 0.

---

# 13. Configuración remota

## 13.1 Alcance Etapa 0

Único parámetro funcional obligatorio:

```text
IntervalSeconds
```

El sistema puede preservar campos actuales existentes, pero Admin no necesita exponerlos todavía.

---

## 13.2 Endpoint

```http
GET /api/v1/devices/{device_id}/config
```

Respuesta:

```json
{
  "version": 3,
  "interval_seconds": 900,
  "updated_at": "ISO-8601"
}
```

---

## 13.3 Persistencia local

Cuando Guardian recibe una configuración válida:

1. valida rango;
2. aplica mediante la lógica existente;
3. guarda en `config.json`;
4. registra evento local;
5. conserva esa configuración para uso offline.

Si servidor/API falla:

- no resetear configuración;
- no usar defaults por el fallo;
- mantener última configuración válida.

---

## 13.4 Semántica del contador

Fuera de alcance definir comportamiento nuevo para cambios de intervalo durante un ciclo activo.

Codex debe conservar la semántica actual del Client y no introducir lógica adicional específica para este caso.

---

## 13.5 Polling

Usar polling simple.

Propuesta inicial:

```text
RemoteConfigPollSeconds = 60
```

No implementar WebSockets/push.

---

# 14. Guardian Admin

## 14.1 Login

Guardian Admin requiere usuario y contraseña.

Un solo administrador en Etapa 0.

No implementar:

- roles;
- permisos;
- multiusuario;
- recuperación por email.

---

## 14.2 Credenciales iniciales

Configurar mediante variables de entorno al crear el sistema.

Ejemplo:

```text
GUARDIAN_ADMIN_USERNAME
GUARDIAN_ADMIN_INITIAL_PASSWORD
```

Nunca incluir password real en repo.

El servidor guarda hash de password.

No guardar password en texto plano.

---

## 14.3 Sesión

Usar cookie de sesión:

- `HttpOnly`;
- `Secure` en hostname público;
- `SameSite=Lax` o más restrictivo compatible;
- expiración razonable.

Cerrar sesión explícitamente.

---

## 14.4 Pantalla inicial

Mostrar una tarjeta por dispositivo.

Para Etapa 0 basta un dispositivo, pero el modelo debe soportar varios.

Campos:

```text
Nombre: Test Device
Estado: Online
PC Windows: DESKTOP-XXXX
Versión: 0.2.0
Último contacto: hace 24 s
Intervalo: 15 min
Última versión disponible: 0.3.0
```

Acciones:

```text
[ Editar configuración ]
[ Actualizar ]
```

---

# 15. Edición de configuración

Pantalla simple:

```text
Tiempo entre desafíos

[ 15 ] minutos

[ Guardar ]
```

Internamente almacenar segundos.

Validar rango.

Propuesta inicial:

```text
mínimo: 60 segundos
máximo: 4 horas
```

La UI puede trabajar en minutos enteros para Etapa 0.

---

# 16. Base de datos

PostgreSQL en Docker.

No exponer puerto de PostgreSQL a Internet.

Puede exponerse solo al host/LAN si una herramienta administrativa local lo requiere; preferir red interna de Docker.

---

# 17. Modelo de datos mínimo

## 17.1 admin_users

Campos:

```text
id UUID PK
username varchar unique
password_hash varchar
is_active boolean
created_at timestamptz
updated_at timestamptz
last_login_at timestamptz nullable
```

---

## 17.2 devices

Campos:

```text
id UUID PK
machine_name varchar
display_name varchar nullable
token_hash varchar
client_version varchar
last_seen_at timestamptz nullable
registered_at timestamptz
updated_at timestamptz
is_active boolean
```

---

## 17.3 device_configurations

Campos:

```text
id UUID PK
device_id UUID FK
version integer
interval_seconds integer
created_at timestamptz
updated_at timestamptz
```

Mantener una configuración vigente por dispositivo.

El `version` aumenta cuando cambia.

---

## 17.4 releases

Campos:

```text
id UUID PK
version varchar unique
filename varchar
sha256 varchar
file_size bigint
release_notes text nullable
created_at timestamptz
is_active boolean
```

---

## 17.5 update_commands

Campos:

```text
id UUID PK
device_id UUID FK
release_id UUID FK
status varchar
requested_at timestamptz
started_at timestamptz nullable
completed_at timestamptz nullable
error_message text nullable
previous_version varchar nullable
target_version varchar
```

Estados:

```text
pending
acknowledged
downloading
installing
success
failed
rolled_back
```

---

# 18. Releases

## 18.1 Etapa 0 sin CI/CD

No generar automáticamente releases al hacer push.

Flujo manual:

```text
Codex/desarrollo
    ↓
actualizar VERSION
    ↓
ejecutar tests
    ↓
build
    ↓
package
    ↓
publicar manualmente en servidor
```

---

## 18.2 Versionado

Usar Semantic Versioning:

```text
MAJOR.MINOR.PATCH
```

Ejemplos:

```text
0.1.0
0.2.0
0.2.1
```

Mientras Guardian sea experimental:

```text
0.x.y
```

---

## 18.3 Fuente única de versión

`VERSION` debe ser la fuente principal.

Eliminar duplicación manual de versión cuando sea posible.

Build debe inyectar/usar la versión del archivo `VERSION`.

---

# 19. Publicación manual de release

Crear script:

```text
scripts/publish-release.ps1
```

Responsabilidades:

1. leer `VERSION`;
2. ejecutar pruebas mínimas;
3. ejecutar build;
4. generar paquete de actualización;
5. calcular SHA-256;
6. copiar paquete a carpeta de releases del servidor;
7. registrar metadata del release en Guardian Server.

No hacer push/deploy automático.

---

# 20. Almacenamiento de releases

En servidor:

```text
/data/guardian/releases/
```

o volumen equivalente.

Los binarios no necesitan guardarse en Git.

El servidor debe servirlos por LAN mediante endpoint autenticado.

---

# 21. API de releases

Admin:

```http
GET /admin/releases
```

Internamente/API:

```http
GET /api/v1/releases
```

Dispositivo:

```http
GET /api/v1/devices/{device_id}/updates/pending
```

Descarga:

```http
GET /api/v1/releases/{release_id}/download
```

Debe requerir autenticación válida del dispositivo.

---

# 22. Orden de actualización

Desde Admin:

```text
seleccionar release
        ↓
Actualizar
        ↓
crear update_command = pending
```

Guardian Client consulta periódicamente:

```http
GET /api/v1/devices/{device_id}/updates/pending
```

Si existe:

1. valida que target != versión actual;
2. registra acknowledged;
3. lanza Updater;
4. cierra Guardian.

---

# 23. Guardian Updater

Crear ejecutable separado:

```text
GuardianUpdater.exe
```

No integrar el reemplazo del binario dentro del proceso principal.

---

## 23.1 Instalación

Guardian instalado:

```text
%LOCALAPPDATA%\Guardian\
```

Separar conceptualmente:

```text
Guardian.exe
Guardian.exe.config
GuardianUpdater.exe

config.json
events.jsonl
device.json
```

Los archivos de datos/configuración deben preservarse durante update.

---

## 23.2 Flujo del updater

```text
Guardian recibe orden
        ↓
lanza GuardianUpdater
        ↓
Updater espera cierre Guardian
        ↓
descarga release a temp
        ↓
verifica SHA-256
        ↓
crea backup binarios actuales
        ↓
reemplaza binarios
        ↓
inicia Guardian
        ↓
verifica arranque
        ↓
reporta success
```

---

## 23.3 Rollback

Si ocurre:

- descarga corrupta;
- hash incorrecto;
- error al extraer;
- error al copiar;
- Guardian nuevo no inicia;

entonces:

```text
restaurar backup
↓
reiniciar versión anterior
↓
marcar rolled_back/failed
```

Nunca dejar la PC sin una versión ejecutable si existe backup válido.

---

# 24. Preservación de configuración

El instalador actual reescribe `config.json`.

Eso debe modificarse.

Regla:

- primera instalación: crear configuración si no existe;
- actualización: nunca sobrescribir `config.json`;
- actualización: nunca borrar `events.jsonl`;
- actualización: nunca cambiar device UUID;
- actualización: nunca cambiar device token;
- actualización: nunca resetear credenciales/configuración local.

---

# 25. Integridad de release

Cada release debe registrar:

```text
SHA-256
```

Updater calcula el hash antes de instalar.

Si no coincide:

- abortar;
- no reemplazar binarios;
- reportar error.

No se requiere firma criptográfica de código en Etapa 0.

---

# 26. Eventos locales

Mantener `events.jsonl`.

No migrar telemetría completa a PostgreSQL en Etapa 0.

Agregar eventos mínimos para infraestructura remota:

```text
DeviceRegistered
HeartbeatSent
HeartbeatFailed
RemoteConfigFetched
RemoteConfigApplied
RemoteConfigFailed

UpdateCommandReceived
UpdateStarted
UpdateDownloadCompleted
UpdateVerificationFailed
UpdateInstalled
UpdateFailed
UpdateRolledBack
```

No construir todavía dashboard de eventos.

---

# 27. Funcionamiento offline

Guardian debe ser local-first.

Si servidor no responde:

- Guardian sigue contando;
- Guardian sigue bloqueando;
- Guardian sigue generando misiones;
- usa última configuración;
- guarda eventos localmente;
- reintenta comunicación después;
- no muestra errores técnicos al niño.

---

# 28. Cloudflare

Cloudflare ya gestiona el dominio.

Usar Cloudflare Tunnel.

Variables/secrets:

```text
CLOUDFLARE_TUNNEL_TOKEN
GUARDIAN_ADMIN_HOST
```

No versionar.

---

## 28.1 Exposición

Objetivo:

```text
guardian.<dominio>
```

debe exponer Guardian Admin.

No exponer PostgreSQL.

No configurar port forwarding del router.

No depender de IP pública doméstica.

---

# 29. Docker Compose

Servicios mínimos:

```text
guardian-app
postgres
cloudflared
```

Opcionalmente un servicio/job de backup.

---

## 29.1 guardian-app

Responsabilidades:

- API;
- Admin;
- archivos estáticos;
- acceso a releases;
- migraciones.

---

## 29.2 postgres

Usar volumen persistente:

```text
guardian_postgres_data
```

---

## 29.3 cloudflared

Usar token desde `.env`.

Nunca incluir token en compose público.

---

# 30. Variables de entorno

`.env.example` debe incluir nombres, no valores reales.

Ejemplo:

```env
POSTGRES_DB=guardian
POSTGRES_USER=guardian
POSTGRES_PASSWORD=change_me

DATABASE_URL=postgresql://guardian:change_me@postgres:5432/guardian

GUARDIAN_ADMIN_USERNAME=admin
GUARDIAN_ADMIN_INITIAL_PASSWORD=change_me
GUARDIAN_SESSION_SECRET=change_me

DEVICE_BOOTSTRAP_TOKEN=change_me

GUARDIAN_ADMIN_HOST=guardian.example.com

CLOUDFLARE_TUNNEL_TOKEN=
```

---

# 31. Backup

Etapa 0: backup local simple.

Una vez por día:

```text
pg_dump
```

Destino:

```text
/backups/guardian/
```

Retención:

```text
7 backups diarios
```

No subir automáticamente a cloud.

Crear script:

```text
backup-db.ps1
```

y, si resulta simple, tarea programada opcional.

La automatización del backup no es requisito bloqueante para la primera prueba funcional, pero el script sí debe existir.

---

# 32. Estrategia de desarrollo y prueba

## 32.1 Fase A — Todo en PC servidor

Esta es la primera fase obligatoria.

No usar todavía la PC administrada real.

En la PC servidor:

1. trabajar sobre repo;
2. levantar PostgreSQL;
3. levantar Guardian Server;
4. ejecutar Guardian Client localmente en modo de prueba;
5. usar `GUARDIAN_HOME` de test;
6. registrar dispositivo de prueba;
7. validar heartbeat;
8. validar Admin;
9. cambiar intervalo;
10. validar configuración remota;
11. crear release de prueba;
12. validar updater en entorno de test.

No instalar nada permanentemente mientras las pruebas básicas no pasen.

---

## 32.2 Fase B — Prueba de instalación local

En la misma PC servidor:

- probar paquete equivalente al que irá a la otra PC;
- validar primera instalación;
- validar actualización;
- validar rollback;
- validar conservación de config.

---

## 32.3 Fase C — PC administrada

Solo después de aprobar Fase A y B:

1. generar paquete final;
2. instalar una sola vez manualmente;
3. registrar dispositivo;
4. asignar nombre visible;
5. probar intervalo corto;
6. cambiar intervalo desde Admin;
7. verificar sincronización;
8. publicar release;
9. ordenar update;
10. verificar versión nueva;
11. reiniciar Windows;
12. confirmar autoarranque;
13. confirmar funcionamiento con servidor apagado temporalmente.

---

# 33. Scripts requeridos

Codex debe proporcionar scripts simples y documentados.

Servidor:

```text
setup-server.ps1
start-server.ps1
stop-server.ps1
update-server.ps1
backup-db.ps1
```

Cliente:

```text
build.ps1
self-test.ps1
run-test-mode.ps1
package-installer.ps1
```

Release:

```text
publish-release.ps1
```

No automatizar ejecuciones remotas.

---

# 34. setup-server.ps1

Debe:

1. verificar Docker Desktop disponible;
2. verificar `.env`;
3. levantar Docker Compose;
4. esperar healthchecks;
5. ejecutar migraciones;
6. crear admin inicial si no existe;
7. imprimir URLs de acceso;
8. no mostrar secretos.

Debe poder ejecutarse más de una vez sin destruir datos.

---

# 35. update-server.ps1

Debe:

1. actualizar imágenes/build local;
2. ejecutar migraciones;
3. recrear servicios necesarios;
4. conservar volúmenes;
5. no borrar DB;
6. mostrar estado final.

No hacer `git pull` automáticamente salvo que se decida luego.

---

# 36. Migraciones

Usar Alembic.

No modificar schema manualmente en producción.

Codex debe:

- crear migración inicial;
- documentar comandos;
- ejecutar migraciones desde scripts de servidor.

---

# 37. Logs

Servidor:

- salida estructurada/log clara;
- no loguear passwords;
- no loguear tokens completos;
- no loguear secretos.

Client:

- mantener eventos locales existentes;
- registrar errores de comunicación sin bloquear interfaz.

Updater:

- archivo propio:

```text
updater.log
```

sin secretos.

---

# 38. Healthchecks

Servidor:

```http
GET /health
```

Respuesta:

```json
{
  "status": "ok"
}
```

Agregar chequeo DB interno.

Docker Compose debe tener healthcheck.

---

# 39. Validaciones

## 39.1 Intervalo

- entero;
- mínimo 60 s;
- máximo 14400 s;
- rechazar valores fuera del rango.

---

## 39.2 Versiones

Validar formato semver básico.

---

## 39.3 Releases

No registrar:

- filename inexistente;
- SHA vacío;
- versión duplicada.

---

# 40. API mínima

## Pública técnica local / dispositivo

```text
POST /api/v1/devices/register
POST /api/v1/devices/{id}/heartbeat
GET  /api/v1/devices/{id}/config
GET  /api/v1/devices/{id}/updates/pending
POST /api/v1/devices/{id}/updates/{command_id}/status
GET  /api/v1/releases/{release_id}/download
```

---

## Admin

Puede usar endpoints internos o formularios server-rendered.

Funciones mínimas:

```text
login
logout
listar dispositivos
editar display_name
editar interval_seconds
listar releases
ordenar actualización
ver estado update
```

---

# 41. Seguridad mínima requerida

- secretos en `.env`;
- passwords hasheados;
- device tokens;
- UUID por dispositivo;
- sesiones Admin seguras;
- validación de inputs;
- no exponer PostgreSQL;
- no usar credenciales default `admin/guardian` en deploy real;
- no publicar información personal;
- hash SHA-256 del release;
- Cloudflare Tunnel en vez de port forwarding.

---

# 42. Compatibilidad con Guardian actual

Codex debe priorizar evolución incremental.

No reescribir el Client a .NET moderno solo por preferencia técnica.

Mantener .NET/WPF actual para Etapa 0 salvo bloqueo técnico probado.

No cambiar:

- lógica de misión;
- pantalla de bloqueo;
- contador;
- bandeja;
- audio;
- watchdog;
- autoarranque;

excepto donde sea necesario para integración remota/updater.

---

# 43. Refactor permitido

El archivo `Guardian.cs` es monolítico.

Codex puede separarlo en archivos/clases para mejorar mantenibilidad si:

- no cambia comportamiento;
- las pruebas existentes siguen pasando;
- el cambio ayuda a implementar API/updater;
- se realiza de forma incremental.

No hacer refactor masivo antes de conseguir comunicación remota funcional.

---

# 44. Criterios de aceptación — 0A Backend local

- `docker compose up` levanta app y PostgreSQL.
- datos persisten tras reinicio.
- `/health` responde OK.
- migraciones funcionan.
- Admin puede iniciar sesión.
- ningún secreto está versionado.

---

# 45. Criterios de aceptación — 0B Comunicación

- Client genera UUID persistente.
- Client se registra.
- servidor guarda machine name.
- servidor recibe heartbeat.
- Admin muestra versión.
- Admin muestra última conexión.
- Client sigue funcionando si servidor cae.

---

# 46. Criterios de aceptación — 0C Config remota

- Admin cambia intervalo.
- DB persiste valor.
- Client obtiene valor.
- `config.json` local se actualiza.
- Client conserva valor al reiniciar.
- Client conserva valor si API queda offline.
- no requiere reinstalación.

---

# 47. Criterios de aceptación — 0D Admin

- acceso con contraseña;
- subdominio funciona por Cloudflare Tunnel;
- lista dispositivo;
- permite editar display name;
- permite editar intervalo;
- muestra versión/último heartbeat;
- no expone secretos.

---

# 48. Criterios de aceptación — 0E Releases

- `VERSION` controla versión.
- build genera artefacto.
- release tiene SHA-256.
- servidor registra release.
- Admin muestra release disponible.
- no requiere CI/CD.

---

# 49. Criterios de aceptación — 0F Updater

- Admin genera orden.
- Client detecta orden.
- Updater descarga.
- hash válido requerido.
- Guardian se detiene.
- binarios se reemplazan.
- config/eventos permanecen.
- Guardian reinicia.
- versión nueva aparece en Admin.
- fallo provoca rollback.
- rollback conserva funcionamiento anterior.

---

# 50. Pruebas mínimas automatizadas

## Server

- registro correcto;
- bootstrap token inválido;
- heartbeat autenticado;
- token inválido;
- consulta config;
- cambio intervalo;
- validación rango;
- creación release;
- orden update;
- transición de status.

---

## Client

- UUID se conserva;
- config offline;
- config válida aplicada;
- config inválida ignorada;
- error HTTP no rompe Guardian.

---

## Updater

Como mínimo tests o harness reproducible para:

- hash válido;
- hash inválido;
- backup;
- reemplazo;
- rollback.

---

# 51. Pruebas manuales obligatorias antes de PC administrada

Checklist:

```text
[ ] Guardian local funciona como antes
[ ] Docker levanta
[ ] DB persiste
[ ] Admin login funciona
[ ] dispositivo local se registra
[ ] heartbeat aparece
[ ] intervalo cambia desde Admin
[ ] reinicio conserva intervalo
[ ] servidor apagado no rompe Guardian
[ ] release se publica manualmente
[ ] orden update llega
[ ] update exitoso
[ ] config no se pierde
[ ] rollback probado artificialmente
[ ] Cloudflare Admin funciona
[ ] repo escaneado sin datos privados
```

No pasar a la PC administrada hasta completar este checklist.

---

# 52. Orden recomendado para Codex

Codex debe implementar en este orden:

## Paso 1 — Sanitización y estructura

- backup;
- repo público limpio;
- `.gitignore`;
- reorganización mínima;
- documentación.

## Paso 2 — Servidor mínimo

- Docker;
- PostgreSQL;
- FastAPI;
- `/health`;
- Alembic.

## Paso 3 — Dispositivos

- tablas;
- registro;
- auth token;
- heartbeat.

## Paso 4 — Integración Client

- UUID;
- registro;
- heartbeat;
- versión.

## Paso 5 — Configuración

- DB;
- endpoint;
- polling;
- persistencia;
- Admin edición.

## Paso 6 — Guardian Admin

- login;
- dispositivo;
- estado;
- configuración.

## Paso 7 — Cloudflare Tunnel

- configuración parametrizada;
- subdominio;
- no hardcodear secretos.

## Paso 8 — Releases

- versionado;
- package;
- SHA;
- registro.

## Paso 9 — Updater

- ejecutable separado;
- download;
- backup;
- replace;
- restart;
- rollback.

## Paso 10 — Validación

- tests;
- checklist;
- documentación.

Solo después:

## Paso 11 — PC administrada

Instalación y prueba real.

---

# 53. Restricciones para Codex

Codex NO debe:

- inventar datos personales;
- hardcodear nombres reales;
- hardcodear dominio real;
- hardcodear IP;
- hardcodear secretos;
- modificar QueComemosBot;
- modificar Gaiwyx;
- agregar n8n;
- agregar servicios cloud pagos;
- agregar CI/CD sin pedido;
- transformar el proyecto a otra plataforma;
- eliminar comportamiento actual;
- borrar eventos/config local durante updates;
- publicar automáticamente en GitHub;
- hacer push antes de sanitización;
- instalar nada en la PC administrada durante desarrollo inicial.

---

# 54. Variables pendientes de despliegue

No bloquean implementación.

Se completan cuando corresponda:

```text
GUARDIAN_ADMIN_HOST=<subdominio real>
GUARDIAN_SERVER_URL=<IP LAN estable>
POSTGRES_PASSWORD=<secreto>
GUARDIAN_ADMIN_INITIAL_PASSWORD=<secreto>
GUARDIAN_SESSION_SECRET=<secreto>
DEVICE_BOOTSTRAP_TOKEN=<secreto>
CLOUDFLARE_TUNNEL_TOKEN=<secreto>
```

---

# 55. Definition of Done — Etapa 0

La Etapa 0 se considera terminada cuando, desde la PC administradora:

1. se puede entrar al Guardian Admin con contraseña;
2. se ve la PC administrada;
3. se conoce su estado y versión;
4. se puede cambiar el tiempo entre desafíos;
5. el Client recibe y conserva el cambio;
6. Guardian funciona aunque el servidor esté temporalmente offline;
7. se puede registrar una nueva versión;
8. se puede ordenar la actualización;
9. la PC administrada actualiza sin reinstalación manual;
10. una falla de actualización puede volver a la versión previa;
11. los datos/configuración local no se pierden;
12. PostgreSQL y servidor están self-hosted;
13. el repositorio público no contiene información personal ni secretos;
14. no existe dependencia de n8n, Gaiwyx o QueComemosBot;
15. el proceso completo está documentado y puede repetirse mediante scripts manuales.

---

# 56. Decisiones deliberadamente postergadas

Estas preguntas NO deben bloquear Etapa 0:

- qué ocurre exactamente con un contador activo al cambiar el intervalo;
- nuevas configuraciones remotas;
- métricas;
- telemetría centralizada;
- dificultad adaptativa;
- banco de desafíos;
- IA;
- uso fuera de casa;
- tablet;
- CI/CD;
- firma digital;
- push notifications;
- automatización de releases.

---

# 57. Principio rector

La Etapa 0 no busca construir toda la plataforma Guardian.

Busca construir una base operativa donde:

```text
configuración
    ≠
release
```

y donde:

```text
nueva versión
    ≠
reinstalación manual
```

Una vez conseguida esta base, las siguientes iteraciones podrán concentrarse en datos, desafíos y adaptación sin tener que resolver nuevamente distribución, configuración y administración.
