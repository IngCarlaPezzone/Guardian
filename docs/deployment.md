# Guardian Etapa 0 - Deploy local

## Preparacion

1. Copiar `.env.example` a `.env`.
2. Completar secretos reales solo en `.env`.
3. No versionar `.env`, backups, releases binarios ni datos locales.

## Servidor

```powershell
.\scripts\setup-server.ps1
```

Validar:

```powershell
Invoke-RestMethod http://localhost:8080/health
```

Detener sin borrar volumenes:

```powershell
.\scripts\stop-server.ps1
```

## Inicio automático tras reiniciar Windows

Para que el servidor vuelva a estar disponible después de apagar o reiniciar la PC:

1. En Docker Desktop, activar **Start Docker Desktop when you sign in**.
2. Los servicios de Guardian usan la política Compose `restart: unless-stopped`. Docker los inicia nuevamente cuando su motor queda disponible, pero respeta una detención manual explícita mediante `docker compose stop`.

La API puede demorar algunos segundos en volver: PostgreSQL debe pasar su healthcheck antes de iniciar `guardian-app`; si el perfil Cloudflare está habilitado, `cloudflared` espera el healthcheck de la API. Guardian Client sigue funcionando localmente durante ese intervalo y reanuda heartbeats cuando la API responde.

Después de introducir o actualizar esta configuración, recrear los contenedores una vez:

```powershell
.\scripts\start-server.ps1
```

Para STG, usar su proyecto aislado:

```powershell
.\scripts\start-stg.ps1
```

Backup local:

```powershell
.\scripts\backup-db.ps1
```

## Fase A/B validadas en PC servidor

- Docker Compose levanta PostgreSQL y Guardian Server.
- `/health` responde `ok`.
- Admin login responde.
- Client local con `%LOCALAPPDATA%\Guardian` se registra mediante `scripts\run-test-mode.ps1`.
- Heartbeat aparece en DB.
- Admin cambia intervalo y Client lo adopta.
- Con servidor detenido, Client conserva configuracion y sigue funcionando.
- Release manual `0.2.0` se registra con SHA-256.
- Update local de test reemplaza `0.1.0` por `0.2.0`.
- Hash invalido aborta update.
- Ejecutable invalido dispara rollback y conserva version previa.

## Pendiente exclusivo Fase C

No se instalo nada en la PC administrada real. Queda pendiente:

- configurar IP LAN estable o nombre local para el servidor;
- configurar secretos reales;
- configurar Cloudflare Tunnel real;
- instalar una sola vez en la PC administrada;
- registrar dispositivo real;
- validar autoarranque tras reinicio de Windows;
- validar update remoto real desde Admin.

## Instalacion de una PC cliente nueva

1. Generar el paquete en la PC servidor:

```powershell
.\scripts\package-installer.ps1
```

2. Copiar `release\GuardianInstaller.zip` a la PC cliente y descomprimirlo.
3. Ejecutar `Guardian.exe`.
4. Cuando Guardian abra la ventana de primera instalacion, ingresar la URL LAN del Guardian Server, por ejemplo:

```text
http://NOMBRE-SERVIDOR:8080
```

5. Ingresar el bootstrap token real provisto por el administrador.
6. Dejar marcada la opcion de autoarranque si Guardian debe iniciar con Windows.

El bootstrap token no forma parte del ZIP. Guardian lo escribe solo en el `config.json` local de esa PC para permitir el primer registro. Cuando recibe su `DeviceToken`, borra `DeviceBootstrapToken` de `config.json`.

Si `%LOCALAPPDATA%\Guardian\config.json` ya existe, el instalador lo conserva y no vuelve a pedir bootstrap. Esto permite reinstalar o actualizar encima sin cambiar la identidad del dispositivo.

Guardian valida la URL antes de guardar configuracion: recorta espacios al inicio/final, acepta solo HTTP/HTTPS y rechaza espacios internos como `http:// servidor:8080`.

## Registrar una PC de prueba/staging

La PC de prueba se registra igual que cualquier otro dispositivo:

1. instalar o ejecutar Guardian con un `GUARDIAN_HOME`/perfil propio;
2. configurar `GuardianServerUrl` con la URL LAN del servidor;
3. ingresar el bootstrap token real localmente;
4. esperar el primer registro y heartbeat;
5. desde Admin editar el nombre visible, por ejemplo `Test Device` o `Staging Device`.

No hardcodear nombres personales en codigo ni en archivos versionados. Si una instalacion vieja debe registrarse como dispositivo nuevo, cerrar Guardian, quitar autoarranque y borrar solo los datos locales de esa PC tras confirmar que no contienen informacion necesaria.

## Consultar actividad

Desde Admin:

```text
Dispositivos -> Ver actividad
```

Filtros disponibles:

- Hoy.
- Ayer.
- Fecha especifica.
- Todos.
- Misiones.
- Configuracion.
- Actualizaciones.
- Errores.

La vista muestra resumen legible y payload JSON para diagnostico.

La hora se muestra en la zona configurada por `GUARDIAN_ADMIN_TIMEZONE` si esta definida; si no, usa la zona local del proceso servidor. En Docker puede convenir definir explicitamente un valor IANA como `Etc/UTC` u otra zona disponible en la imagen.

Ejemplo de configuracion para una zona IANA disponible en la imagen Docker:

```text
GUARDIAN_ADMIN_TIMEZONE=Etc/UTC
```

Los controles remotos requieren Guardian 0.3.2 o posterior. Con el dispositivo online, Admin permite pausar/reanudar el monitoreo o disparar una mision manual. Pausar no apaga Guardian: conserva heartbeat, telemetria, polling y actualizaciones. Si el proceso esta cerrado u offline, Admin no puede ejecutar esos comandos ni encender la PC.

Para publicar una descripcion corta visible en Admin:

```powershell
.\scripts\publish-release.ps1 -Description "Corrige reanudacion remota y sincroniza el estado de monitoring."
```

La descripcion es opcional y se guarda en el campo existente `release_notes`; los releases historicos pueden quedar vacios.

## Limpieza de dispositivos obsoletos

Desde Admin se puede eliminar un dispositivo Offline usando `Eliminar dispositivo`.

La accion requiere copiar el ID tecnico en el campo de confirmacion y aceptar el dialogo del navegador. Para evitar borrar una PC que sigue funcionando, los dispositivos Online no se pueden eliminar desde Admin; primero deben cerrarse, desinstalarse o quedar Offline.

Al eliminar un dispositivo Offline, el servidor borra su configuracion remota y comandos de update asociados antes de borrar el dispositivo.

## Instalacion limpia sobre una version vieja

Ejecutar `Desinstalar-Guardian.bat`.

Por defecto el desinstalador:

- cierra Guardian;
- quita el autoarranque;
- borra los ejecutables instalados;
- conserva `config.json` y `events.jsonl`.

Para una PC con una instalacion vieja incompatible, responder `S` cuando pregunte si debe borrar datos locales. Esa opcion elimina configuracion, identidad y eventos locales, y la siguiente instalacion registra la PC como un dispositivo nuevo.
