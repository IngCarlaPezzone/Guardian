# AGENTS.md — Guardian

Este archivo define las reglas permanentes de trabajo para cualquier agente o instancia de Codex que opere sobre este repositorio.

Las funcionalidades, etapas activas y criterios de aceptación deben consultarse en las especificaciones vigentes dentro de `docs/`.

## 1. Fuentes de verdad

Antes de modificar código:

1. Leer este `AGENTS.md`.
2. Identificar y leer la especificación técnica de la etapa o iteración activa.
3. Revisar la documentación de arquitectura y los archivos relevantes del código existente.
4. No implementar funcionalidades futuras únicamente porque aparezcan en un roadmap.

En caso de conflicto:

1. La especificación funcional/técnica de la iteración activa tiene prioridad.
2. Luego este `AGENTS.md`.
3. Luego la documentación general de arquitectura/roadmap.
4. Las decisiones técnicas menores pueden resolverse por criterio de implementación.

## 2. Guardian es un producto existente

Guardian es una aplicación Windows funcional que debe evolucionar de forma incremental.

No sustituir innecesariamente:

- lenguaje;
- framework;
- mecanismo de bloqueo;
- lógica de misión existente;
- contador;
- watchdog;
- bandeja de Windows;
- manejo de audio;
- autoarranque;
- arquitectura servidor/cliente.

No migrar a otra tecnología únicamente por preferencia técnica.

## 3. Cambios incrementales

Trabajar en bloques pequeños.

Para cada bloque:

1. inspeccionar el código existente;
2. entender el impacto;
3. modificar lo mínimo necesario;
4. ejecutar pruebas;
5. corregir regresiones;
6. documentar cambios relevantes;
7. recién entonces continuar.

Evitar refactors masivos que no sean necesarios para cumplir la especificación.

## 4. No avanzar sobre una base rota

Si una prueba falla por un cambio nuevo:

- investigar;
- corregir;
- repetir la prueba;
- no continuar dejando la regresión abierta.

Si una prueba ya fallaba antes:

- documentarlo claramente;
- verificar que el cambio no la empeoró;
- no ocultar el problema.

No considerar una iteración completa sólo porque el código compila.

## 5. Privacidad obligatoria

El repositorio es público y debe mantenerse apto para publicación.

Nunca versionar información personal real, incluyendo:

- nombres y apellidos;
- edad;
- domicilio;
- ciudad o provincia real vinculada a una persona;
- datos de menores;
- información familiar;
- preguntas personales reales;
- historial real de uso;
- eventos reales;
- configuraciones reales;
- backups reales;
- documentos internos con contexto privado.

Usar siempre datos neutrales de ejemplo.

Ejemplos válidos:

```text
Sample-PC
Test Device
example.com
192.168.1.xxx
```

## 6. Secretos

Nunca hardcodear ni versionar:

- contraseñas;
- tokens;
- API keys;
- Cloudflare Tunnel tokens;
- credenciales PostgreSQL;
- session secrets;
- bootstrap tokens;
- device tokens;
- cookies;
- certificados privados.

Los valores reales deben vivir en:

```text
.env
```

El repositorio sólo debe incluir:

```text
.env.example
```

con valores ficticios.

Nunca registrar secretos en logs, payloads o mensajes de error.

## 7. Git y publicación

No hacer push, merge, force push ni publicar releases automáticamente salvo instrucción explícita.

Antes de publicar cambios:

- revisar `git status`;
- revisar `git diff`;
- ejecutar tests relevantes;
- comprobar que no se agregaron secretos ni datos personales.

No usar `git reset --hard`, force push ni borrar ramas con trabajo sin autorización explícita.

Trabajar las nuevas etapas en ramas `feature/...` creadas desde una base estable.

## 8. Arquitectura general

Mantener la arquitectura simple:

```text
Guardian Client Windows
        ↓
Guardian Server / API
        ↓
PostgreSQL

Guardian Admin
        ↓
Guardian Server

Guardian Updater
        ↓
Releases publicados
```

Tecnologías actuales:

- Windows;
- WPF / .NET Framework;
- FastAPI;
- SQLAlchemy;
- Alembic;
- Jinja2;
- PostgreSQL;
- Docker Compose;
- Cloudflare Tunnel/Access cuando corresponda.

No crear microservicios innecesarios.

## 9. Local-first

Guardian Client debe seguir funcionando aunque:

- Docker esté detenido;
- PostgreSQL no responda;
- Guardian API no responda;
- la red falle temporalmente;
- Internet no esté disponible.

En esos casos debe, según corresponda:

- conservar la última configuración válida;
- mantener el funcionamiento básico local;
- registrar eventos localmente;
- conservar telemetría pendiente;
- reintentar más adelante;
- evitar mostrar errores técnicos innecesarios al usuario final.

El servidor no debe convertirse en dependencia obligatoria del funcionamiento básico del cliente.

## 10. Identidad y configuración local

Cada instalación debe conservar un `DeviceId` UUID persistente.

Distinguir:

- `machine_name`: hostname técnico reportado por el cliente;
- `display_name`: nombre visible editable desde Admin.

`display_name` no debe ser sobrescrito por heartbeats o registro automático.

Las actualizaciones deben preservar datos locales necesarios, incluyendo cuando corresponda:

- `config.json`;
- `events.jsonl`;
- `events-pending.jsonl`;
- Device UUID;
- device token;
- configuración remota persistida.

Regla:

```text
instalación inicial → crear si no existe
actualización       → preservar
```

## 11. Telemetría

La telemetría nunca debe romper Guardian.

Principios:

- persistencia local primero;
- sincronización posterior;
- IDs únicos por evento;
- deduplicación en servidor;
- retry con backoff;
- tolerancia a red/servidor offline;
- ningún error de telemetría debe cerrar la aplicación;
- no registrar secretos.

Mantener acceso coordinado y seguro a los archivos locales de eventos.

## 12. Controles remotos

Los controles remotos deben reutilizar la lógica local existente siempre que sea posible.

Distinguir claramente:

- `Activo`: cliente online y monitoreo habilitado;
- `Pausado`: cliente online, conectado y sin disparos automáticos;
- `Offline`: sin heartbeat reciente.

Pausar no debe equivaler a cerrar Guardian.

Mientras está pausado, el cliente debe poder seguir:

- enviando heartbeat;
- sincronizando telemetría;
- consultando configuración;
- recibiendo updates;
- recibiendo comandos remotos.

El estado mostrado por Admin debe converger al estado real reportado por el cliente.

## 13. Comunicación

Preferir HTTP simple + polling mientras siga siendo suficiente.

No agregar sin necesidad:

- WebSockets;
- Redis;
- brokers;
- colas externas;
- infraestructura adicional.

Las acciones máquina-servidor deben usar autenticación de dispositivo y no depender de la sesión web de Admin.

## 14. Base de datos

Usar PostgreSQL como base central.

Aplicar cambios de esquema mediante Alembic.

No modificar tablas manualmente como solución permanente.

No destruir volúmenes de PostgreSQL durante actualizaciones normales.

Nunca usar:

```text
docker compose down -v
```

como parte de un update normal.

## 15. Releases y versionado

Usar Semantic Versioning:

```text
MAJOR.MINOR.PATCH
```

La fuente principal de versión es:

```text
VERSION
```

No duplicar versiones manualmente si puede evitarse.

Los releases son explícitos y deben incluir:

- versión;
- artefacto;
- SHA-256;
- descripción/notas breves cuando corresponda.

No asumir que:

```text
versión instalada == último release publicado
```

El sistema debe soportar upgrade y downgrade cuando la especificación lo requiera.

## 16. Updater

Guardian Updater es un ejecutable separado.

Nunca reemplazar el ejecutable principal desde el mismo proceso que está siendo reemplazado.

Flujo general:

```text
orden
↓
descarga
↓
validación SHA-256
↓
backup
↓
cierre real de Guardian
↓
reemplazo de binarios
↓
inicio de Guardian
↓
validación
↓
success
```

Si falla:

```text
rollback
↓
reinicio de versión anterior
↓
reporte de error
```

Mantener metadatos correctos de:

- versión origen;
- versión destino;
- dirección (`upgrade`, `downgrade`, `same`);
- command id;
- release id.

No iniciar dos instancias de Guardian durante un update.

## 17. Admin

Guardian Admin debe priorizar claridad operativa.

Cuando una acción remota sea asíncrona, mostrar estado suficiente para que no parezca que “no pasó nada”.

Distinguir cuando corresponda:

- pendiente;
- esperando dispositivo;
- recibida;
- en progreso;
- completada;
- fallida;
- cancelada.

Los errores reales deben diferenciarse de notas informativas.

## 18. Funcionamiento offline y comandos pendientes

Un dispositivo offline no puede ejecutar comandos hasta volver a conectarse.

No confundir:

```text
reanudar monitoreo
```

con:

```text
encender físicamente una PC
```

No implementar Wake-on-LAN ni control de energía salvo especificación explícita.

Los comandos pendientes deben ser idempotentes cuando corresponda y no bloquear indefinidamente comandos posteriores.

## 19. Datos y entornos de prueba

Usar dispositivos y directorios de prueba separados cuando una prueba pueda ser destructiva.

No mezclar datos de test con datos reales.

No ejecutar pruebas destructivas sobre una instalación real sin indicación explícita.

El flujo recomendado para releases es:

```text
dispositivo de prueba/staging
↓
validación
↓
dispositivo real
```

No enviar updates automáticamente al dispositivo real.

## 20. Tests

Agregar pruebas para nuevas capacidades relevantes.

No eliminar tests existentes para hacer pasar una implementación.

No reducir cobertura deliberadamente.

Ejecutar, según corresponda:

```powershell
pytest server/tests
.\scripts\build.ps1
.\scripts\self-test.ps1
```

y cualquier test adicional de updater/cliente relacionado con el cambio.

## 21. Dependencias

Agregar la menor cantidad posible.

Antes de agregar una dependencia:

1. verificar si ya existe una solución simple;
2. comprobar que sea realmente necesaria;
3. evitar frameworks grandes para problemas pequeños;
4. evaluar impacto en build, Docker y publicación.

## 22. Documentación

Actualizar documentación cuando cambien:

- arquitectura;
- variables de entorno;
- comandos;
- endpoints;
- scripts;
- flujo de actualización;
- instalación;
- comportamiento remoto;
- persistencia;
- seguridad.

No dejar documentación contradiciendo el código.

Mantener acentos, ortografía y redacción clara en documentación en español.

## 23. Scripts

Preferir PowerShell para tareas del host Windows.

Mantener scripts simples, seguros e idempotentes cuando sea razonable.

No automatizar acciones destructivas ni despliegues a dispositivos reales sin instrucción explícita.

## 24. Scope control

Si aparece una idea útil pero fuera de alcance:

1. no implementarla;
2. registrarla como pendiente si aporta valor;
3. continuar con la especificación activa.

Evitar scope creep.

## 25. Cuándo detenerse y preguntar

Pedir decisión al usuario cuando una elección:

- cambia comportamiento visible importante;
- contradice la especificación;
- implica pérdida de datos;
- implica publicar o exponer algo;
- implica un servicio pago;
- reduce seguridad;
- cambia tecnología principal;
- requiere credenciales reales;
- requiere información privada real;
- afecta un dispositivo real de manera no reversible.

Para decisiones internas menores, avanzar con criterio técnico.

## 26. Checkpoint final

Al terminar una iteración, informar como mínimo:

```text
Qué se implementó
Qué archivos principales cambiaron
Migraciones/endpoints agregados
Qué se probó
Resultado de tests
Versión final
Commit creado
Riesgos o deuda técnica
Pasos exactos de validación manual
Confirmación de qué quedó fuera de alcance
```

## 27. Principios permanentes

Guardian debe evolucionar respetando:

```text
configuración ≠ release
release ≠ reinstalación manual
pausa ≠ offline
telemetría ≠ dependencia funcional
```

Y priorizando siempre:

```text
privacidad
seguridad
funcionamiento offline
simplicidad
mantenibilidad
observabilidad
```
