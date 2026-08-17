# AGENTS.md — Guardian

Este archivo define las reglas permanentes de trabajo para cualquier agente o instancia de Codex que opere sobre este repositorio.

Debe leerse junto con:

- `docs/Guardian_Propuesta_Arquitectura_Roadmap.md`
- `docs/Guardian_Etapa_0_Especificacion_Funcional_Tecnica.md`

En caso de conflicto:

1. La especificación funcional/técnica de la etapa activa tiene prioridad.
2. Luego este `AGENTS.md`.
3. Luego el roadmap general.
4. Finalmente, las decisiones técnicas menores pueden resolverse por criterio de implementación.

---

# 1. Objetivo actual

El objetivo activo es implementar la **Etapa 0 de Guardian**.

Guardian ya existe y debe evolucionarse de forma incremental.

No debe reescribirse innecesariamente.

La Etapa 0 busca principalmente:

- administración remota;
- configuración remota del intervalo de disparo;
- backend local;
- PostgreSQL;
- Guardian Admin;
- identificación de dispositivo;
- heartbeat;
- versionado;
- releases manuales;
- Guardian Updater;
- preservación de configuración;
- rollback;
- funcionamiento local-first/offline;
- preparación de un repositorio público seguro.

---

# 2. Fuente de verdad

Antes de modificar código, leer completamente:

```text
docs/Guardian_Propuesta_Arquitectura_Roadmap.md
docs/Guardian_Etapa_0_Especificacion_Funcional_Tecnica.md
```

No implementar funcionalidades futuras solamente porque aparecen en el roadmap.

El roadmap define dirección.

La especificación de la etapa activa define qué construir ahora.

---

# 3. Guardian existente

Guardian es una aplicación Windows existente.

La implementación actual debe tratarse como producto funcional a conservar.

No sustituir:

- lenguaje;
- framework;
- mecanismo de bloqueo;
- lógica de misión;
- contador;
- watchdog;
- bandeja;
- audio;
- autoarranque;

salvo que exista una necesidad técnica concreta documentada en la especificación.

No migrar a otra tecnología solamente por preferencia técnica.

---

# 4. Principio de cambios incrementales

Trabajar en pasos pequeños.

Para cada bloque:

1. inspeccionar código existente;
2. entender impacto;
3. modificar lo mínimo necesario;
4. ejecutar pruebas;
5. corregir regresiones;
6. documentar cambios relevantes;
7. recién entonces continuar.

No acumular múltiples refactors grandes antes de validar comportamiento.

---

# 5. No avanzar sobre una base rota

Si una prueba falla por un cambio nuevo:

- investigar;
- corregir;
- repetir prueba;
- no continuar a la siguiente etapa con la falla abierta.

Si una prueba ya fallaba antes del cambio:

- documentarlo claramente;
- verificar que el cambio no la empeoró;
- no ocultar el problema.

---

# 6. Privacidad obligatoria

El repositorio final será público.

No debe contener información personal.

Nunca versionar:

- nombres reales;
- apellidos;
- edad;
- domicilio;
- ciudad real;
- provincia real;
- datos de menores;
- información familiar;
- preguntas personales reales;
- historial real;
- eventos reales;
- archivos de prueba familiares;
- configuraciones reales;
- backups reales.

Usar siempre datos neutrales de ejemplo.

Ejemplos permitidos:

```text
Sample-PC
Test Device
example.com
192.168.1.xxx
```

No inventar datos personales ficticios que parezcan reales del usuario.

---

# 7. Secretos

Nunca hardcodear ni versionar:

- passwords;
- tokens;
- API keys;
- Cloudflare Tunnel tokens;
- credenciales PostgreSQL;
- session secrets;
- bootstrap tokens;
- device tokens;
- cookies;
- certificados privados.

Usar:

```text
.env
```

para valores reales.

El repositorio solo debe incluir:

```text
.env.example
```

con valores ficticios.

---

# 8. Git y repositorio público

No hacer push automáticamente.

No publicar el repositorio sin revisión.

El árbol actual puede contener:

- `.git`;
- configuraciones;
- datos locales;
- documentos privados;
- archivos de prueba.

Antes del primer repositorio público:

1. hacer backup;
2. auditar contenido;
3. sanitizar;
4. crear historial Git limpio;
5. ejecutar búsqueda de secretos;
6. ejecutar búsqueda de datos personales;
7. revisar `.gitignore`;
8. recién después preparar el primer commit público.

No confiar en borrar archivos privados mediante commits posteriores.

---

# 9. Proyectos externos

Guardian es un proyecto independiente.

No modificar ni reutilizar directamente código de:

- Gaiwyx;
- Qué Comemos;
- otros proyectos del usuario.

Se pueden tomar conceptos arquitectónicos como referencia, pero no introducir dependencias cruzadas.

---

# 10. Arquitectura de Etapa 0

Infraestructura objetivo:

```text
Windows 10 Home
Docker Desktop
Docker Compose
FastAPI
PostgreSQL
Guardian Admin
Cloudflare Tunnel
Guardian Client
Guardian Updater
```

El servidor principal vive en la PC doméstica destinada a servidor.

PostgreSQL vive en Docker.

Guardian Client se comunica por LAN.

Guardian Admin puede exponerse por subdominio mediante Cloudflare Tunnel.

No exponer PostgreSQL a Internet.

---

# 11. Cloud

No agregar servicios cloud pagos.

No introducir:

- AWS;
- Azure;
- GCP;
- Render;
- Railway;
- Supabase;
- Firebase;

salvo instrucción explícita posterior.

Cloudflare puede utilizarse únicamente según la especificación vigente.

---

# 12. n8n e inteligencia artificial

No incorporar en Etapa 0:

- n8n;
- agentes;
- LLMs;
- generación automática de desafíos;
- recomendaciones inteligentes.

Estas capacidades pertenecen a iteraciones posteriores.

---

# 13. Guardian Client es local-first

El Client debe continuar funcionando aunque:

- Docker esté detenido;
- PostgreSQL no responda;
- Guardian API no responda;
- la red doméstica falle temporalmente.

En caso de pérdida de servidor:

- continuar contador;
- continuar bloqueo;
- continuar desafíos;
- utilizar última configuración válida;
- registrar errores localmente;
- reintentar más adelante;
- no mostrar errores técnicos al niño.

Nunca convertir el servidor en dependencia necesaria para el funcionamiento básico.

---

# 14. Configuración remota

Para Etapa 0 la configuración remota obligatoria es:

```text
IntervalSeconds
```

No ampliar el Admin con configuraciones futuras salvo necesidad técnica.

La arquitectura puede quedar preparada para crecer, pero la interfaz no debe sobreconstruirse.

---

# 15. Configuración local

Una actualización nunca debe borrar:

- `config.json`;
- `events.jsonl`;
- Device UUID;
- device token;
- configuración remota persistida;
- datos locales necesarios.

Regla:

```text
instalación inicial → crear si no existe
actualización       → preservar
```

---

# 16. Identidad de dispositivo

No usar exclusivamente `Environment.MachineName` como identidad.

Cada instalación debe tener:

```text
Device UUID
```

persistente.

También registrar:

```text
machine_name
display_name
```

`display_name` es editable por Admin.

---

# 17. Comunicación

Preferir HTTP simple + polling.

No agregar WebSockets para Etapa 0.

No agregar colas de mensajes.

No agregar Redis.

No agregar brokers.

Mantener la solución simple.

---

# 18. Backend

El backend debe ser una aplicación única y simple.

Preferir:

```text
FastAPI
SQLAlchemy
Alembic
Jinja2
PostgreSQL
```

Admin y API pueden convivir en la misma aplicación.

No crear microservicios innecesarios.

---

# 19. Guardian Admin

Para Etapa 0:

- un administrador;
- usuario + contraseña;
- sin roles;
- sin multiusuario;
- sin recuperación por email;
- sin OAuth.

Debe permitir como mínimo:

- ver dispositivo;
- estado online/offline;
- versión;
- último heartbeat;
- nombre visible;
- intervalo;
- editar intervalo;
- ver releases;
- ordenar actualización.

---

# 20. Releases

No implementar CI/CD.

Los releases son manuales en Etapa 0.

Usar Semantic Versioning:

```text
MAJOR.MINOR.PATCH
```

La fuente principal de versión debe ser:

```text
VERSION
```

No duplicar valores de versión manualmente si puede evitarse.

---

# 21. Updater

Guardian Updater debe ser un ejecutable separado.

Nunca intentar reemplazar el ejecutable principal desde el mismo proceso que está siendo reemplazado.

Flujo:

```text
orden
↓
descarga
↓
SHA-256
↓
backup
↓
cierre Guardian
↓
reemplazo binarios
↓
inicio Guardian
↓
validación
↓
success
```

Si falla:

```text
rollback
↓
reinicio versión anterior
↓
reporte de error
```

---

# 22. Seguridad de releases

Cada release debe registrar:

```text
SHA-256
```

No instalar si el hash no coincide.

Firma digital de binarios queda fuera de Etapa 0.

---

# 23. Base de datos

Usar PostgreSQL.

Aplicar cambios de esquema mediante Alembic.

No modificar manualmente tablas como solución permanente.

Datos deben persistir mediante volumen Docker.

No borrar volúmenes durante actualizaciones normales.

---

# 24. Backups

Mantener script de backup local.

No enviar backups a servicios externos en Etapa 0.

No versionar backups.

---

# 25. Docker

El entorno debe poder levantarse con Docker Compose.

Scripts deben ser idempotentes cuando sea razonable.

Nunca ejecutar:

```text
docker compose down -v
```

como parte de una actualización normal.

No destruir volúmenes de PostgreSQL salvo instrucción explícita.

---

# 26. Entorno de pruebas

Primero trabajar exclusivamente en la PC servidor.

Usar modo de prueba del Client.

No instalar Guardian en la PC administrada real mientras no se complete el checklist previo.

Orden:

```text
PC servidor
↓
pruebas locales
↓
prueba instalador local
↓
prueba updater local
↓
rollback
↓
recién después PC administrada
```

---

# 27. Datos de prueba

Usar directorios separados.

Nunca ejecutar tests destructivos contra el Guardian real.

Usar por ejemplo:

```text
GUARDIAN_HOME=<test-dir>
```

Los datos de test no deben mezclarse con:

```text
%LOCALAPPDATA%\Guardian
```

real.

---

# 28. Tests

Agregar pruebas para nuevas capacidades.

No eliminar tests existentes para hacer pasar una implementación.

No reducir cobertura funcional deliberadamente.

Como mínimo validar:

- Device UUID persistente;
- registro;
- auth;
- heartbeat;
- configuración;
- fallback offline;
- releases;
- SHA;
- updater;
- rollback;
- preservación de configuración.

---

# 29. Logs

No registrar secretos.

Nunca imprimir:

- password;
- tokens completos;
- session secrets;
- connection strings con password.

Los errores deben ser útiles pero seguros.

---

# 30. Dependencias

Agregar la menor cantidad posible.

Antes de agregar una dependencia:

1. verificar si ya existe solución simple;
2. evaluar si es necesaria;
3. evitar frameworks grandes para problemas pequeños.

No introducir frontend SPA en Etapa 0.

---

# 31. Refactor

Se permite refactor incremental.

No hacer refactor masivo del Client antes de tener la infraestructura remota funcionando.

Si `Guardian.cs` debe separarse:

- hacerlo gradualmente;
- conservar comportamiento;
- mantener build;
- ejecutar tests después de cada separación significativa.

---

# 32. Documentación

Actualizar documentación cuando cambien:

- arquitectura;
- comandos;
- variables de entorno;
- scripts;
- endpoints;
- flujo de update;
- pasos de instalación.

No dejar documentación contradiciendo el código.

---

# 33. Scripts

Preferir PowerShell para tareas del host Windows.

Scripts requeridos o equivalentes:

```text
setup-server.ps1
start-server.ps1
stop-server.ps1
update-server.ps1
backup-db.ps1

build.ps1
self-test.ps1
run-test-mode.ps1
package-installer.ps1

publish-release.ps1
```

No automatizar acciones remotas todavía.

---

# 34. Decisiones menores

Si la especificación no define una decisión técnica menor:

- elegir la alternativa más simple;
- elegir la más mantenible;
- evitar nueva infraestructura;
- evitar nueva dependencia;
- documentar si la decisión afecta estructura importante.

No preguntar por detalles triviales.

---

# 35. Cuándo detenerse y preguntar

Detener el trabajo y pedir decisión al usuario solamente si aparece una cuestión que:

- cambia comportamiento visible del producto;
- contradice la especificación;
- implica pérdida de datos;
- implica publicar algo;
- implica un servicio pago;
- implica exponer un nuevo servicio a Internet;
- implica cambiar tecnología principal;
- implica reducir seguridad;
- implica instalar en la PC administrada real antes del momento acordado;
- requiere información privada real no disponible;
- requiere credenciales reales.

Para decisiones técnicas internas menores, avanzar autónomamente.

---

# 36. No pedir confirmación innecesaria

No interrumpir para preguntar:

- nombres de clases;
- organización interna menor;
- librería estándar equivalente;
- nombres de variables;
- estructura interna de funciones;
- detalles triviales de UI;
- pequeños refactors seguros.

Resolverlos con criterio técnico.

---

# 37. Checkpoints

Después de cada bloque importante, informar:

```text
Qué se implementó
Qué se probó
Resultado
Qué sigue
Bloqueos reales
```

No marcar una fase como completa solamente porque el código compila.

Debe cumplirse su criterio de aceptación.

---

# 38. Definition of Done

No considerar Etapa 0 terminada hasta que se cumplan los criterios de aceptación definidos en:

```text
docs/Guardian_Etapa_0_Especificacion_Funcional_Tecnica.md
```

para todo lo que pueda validarse en la PC servidor.

Las pruebas exclusivas de la PC administrada deben quedar claramente identificadas como pendientes de Fase C.

---

# 39. Scope control

Si durante implementación aparece una idea interesante pero fuera de alcance:

1. no implementarla;
2. registrarla en documentación de pendientes si resulta útil;
3. continuar con Etapa 0.

Evitar scope creep.

---

# 40. Principio general

Guardian debe evolucionar hacia:

```text
configuración
    ≠
release
```

y:

```text
release
    ≠
reinstalación manual
```

sin comprometer:

```text
privacidad
funcionamiento offline
simplicidad
mantenibilidad
```

Estas cuatro condiciones tienen prioridad durante toda la Etapa 0.
