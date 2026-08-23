# Guardian — STAGING ENVIRONMENT SPEC

## 1. Objetivo

Crear un entorno permanente de **STG (staging)** completamente separado de **PROD (producción)** para Guardian.

El objetivo es que toda nueva funcionalidad pueda validarse primero en STG —incluyendo Admin, API, migraciones, métricas, RemoteConfig, releases, updater, rollback y cliente Guardian— sin modificar ni arriesgar el entorno productivo.

STG debe convertirse en la instancia normal de validación de cualquier feature antes de mergear/desplegar a PROD.

La versión productiva de referencia al iniciar esta etapa es `0.4.1`.

---

## 2. Principio general

Guardian debe operar con dos entornos inequívocamente separados:

### PROD

Entorno real.

Contiene:

- servidor/API real;
- Admin real;
- PostgreSQL real;
- dispositivos reales;
- releases productivas;
- Cloudflare/hostname productivo;
- configuración real;
- telemetría real.

PROD no debe utilizarse para revisar iteraciones visuales o funcionalidad experimental.

### STG

Entorno de validación.

Debe contener:

- servidor/API STG;
- Admin STG;
- PostgreSQL STG separado;
- releases STG separadas;
- puerto/hostname diferente;
- dispositivos exclusivamente de prueba;
- perfiles ficticios;
- telemetría STG;
- migraciones STG;
- cliente Guardian TEST conectado explícitamente a STG.

STG no debe poder emitir comandos hacia dispositivos registrados en PROD.

---

## 3. Alcance de STG

STG debe permitir probar, en la medida en que la arquitectura actual lo permita:

- Admin;
- API;
- migraciones;
- RemoteConfig;
- registro/configuración de dispositivos de prueba;
- pausa/reanudación;
- `Probar misión ahora`;
- configuración de intervalo;
- Mission System;
- perfiles ficticios;
- telemetría;
- Activity;
- métricas;
- releases;
- updater;
- upgrade;
- downgrade/rollback;
- reinicio de Guardian;
- compatibilidad cliente/servidor.

La PC de prueba debe poder ejecutar Guardian apuntando explícitamente al servidor STG.

---

## 4. Aislamiento obligatorio

STG y PROD no deben compartir estado mutable.

Separar como mínimo:

- contenedor/servicio de aplicación;
- PostgreSQL;
- volumen PostgreSQL;
- registro/tabla de releases;
- almacenamiento de artefactos de release;
- puerto;
- variables de entorno;
- device registry;
- RemoteConfig;
- telemetría;
- comandos remotos.

Nunca reutilizar la DB de PROD para STG.

Nunca copiar perfiles reales a STG.

---

## 5. Docker / Compose

Mantener el deployment PROD existente.

Agregar una configuración de STG claramente identificable.

Preferencia conceptual:

```text
deploy/
  docker-compose.yml
  docker-compose.stg.yml
```

No romper `deploy/docker-compose.yml`.

Los nombres reales pueden adaptarse a la arquitectura actual si existe una solución mejor.

Los servicios y volúmenes STG deben tener nombres inequívocos, por ejemplo:

```text
guardian-stg-app
guardian-stg-db
guardian-stg-postgres-data
```

No deben reutilizar nombres/volúmenes productivos.

---

## 6. Variables de entorno

Mantener PROD mediante `.env` actual.

Agregar un entorno STG separado, por ejemplo:

```text
.env.stg
```

Debe estar ignorado por Git.

Agregar plantilla sanitizada:

```text
.env.stg.example
```

No contener:

- tokens reales;
- contraseñas reales;
- perfiles;
- datos personales;
- dominios privados no destinados al repo.

STG debe tener su propia configuración de DB y secretos.

---

## 7. Puerto y acceso

STG debe usar un puerto local diferente al de PROD.

Preferencia inicial:

```text
http://localhost:8081
```

si está disponible.

El Admin STG debe ser accesible localmente sin reemplazar el Admin PROD.

No configurar Cloudflare público para STG en esta etapa salvo necesidad explícita futura.

Debe quedar visualmente claro dentro del Admin que se está usando STG, por ejemplo mediante:

```text
Guardian Admin — STG
```

o un badge persistente `STG`.

El entorno PROD no debe mostrar ese badge.

---

## 8. Base de datos STG

Crear PostgreSQL/volumen independiente.

Al iniciar STG:

- ejecutar migraciones Alembic hasta `head`;
- no modificar DB productiva;
- permitir resetear STG fácilmente;
- permitir seed ficticio.

Debe existir una forma clara de recrear STG desde cero.

---

## 9. Seed de datos ficticios

Crear un mecanismo de seed repetible para STG.

Debe poblar datos suficientes para poder revisar Admin y métricas sin depender de dispositivos reales.

Incluir al menos:

### Dispositivos

1. dispositivo ficticio Online/Activo;
2. dispositivo ficticio Online/Pausado;
3. dispositivo ficticio Offline.

Usar nombres/hostnames totalmente ficticios.

### Configuración

- diferentes intervalos;
- skills activadas/desactivadas;
- perfiles ficticios cuando sean necesarios.

### Actividad

Generar eventos sintéticos representativos:

- MissionStarted;
- MissionFailed;
- MissionSolved;
- RemoteConfig;
- Update;
- Heartbeat;
- pausa/reanudación.

### Métricas

Generar suficiente historia ficticia para cubrir:

- varios días;
- categorías;
- niveles;
- skills;
- variants;
- misiones al primer intento;
- segundo intento;
- 3+ intentos.

No usar datos reales exportados de PROD.

---

## 10. Scripts operativos

Crear scripts claros y simples para operar STG.

Preferencia conceptual:

```text
scripts/start-stg.ps1
scripts/stop-stg.ps1
scripts/update-stg.ps1
scripts/reset-stg.ps1
scripts/seed-stg.ps1
scripts/run-stg-client.ps1
scripts/publish-release-stg.ps1
```

No es obligatorio crear siete scripts separados si reutilizar scripts existentes con parámetros produce una solución más simple y segura.

El criterio principal es que para la usuaria sea inequívoco qué comando opera STG y cuál PROD.

Los scripts STG deben evitar por diseño tocar servicios/volúmenes productivos.

---

## 11. Cliente Guardian contra STG

Debe existir una forma simple de levantar un cliente Guardian de prueba conectado explícitamente a STG.

Preferencia:

```text
.\scripts\run-stg-client.ps1
```

o extensión segura del `run-test-mode.ps1` existente.

Debe:

- usar directorio/config local de prueba;
- conectarse únicamente al server STG;
- no reutilizar identidad/token de un dispositivo productivo;
- permitir registrar un dispositivo TEST en STG;
- evitar watchdog/instalación productiva cuando corresponda al modo test;
- permitir probar misiones y RemoteConfig.

No modificar la instalación productiva de Guardian en esa PC.

---

## 12. Releases STG

STG debe tener un registro de releases independiente.

Una release publicada en STG:

- no debe aparecer en Admin PROD;
- no debe estar disponible para dispositivos PROD;
- debe permitir probar updater/upgrade/downgrade con cliente TEST.

Reutilizar el mecanismo existente tanto como sea posible.

Crear un flujo claramente separado, por ejemplo:

```text
publish-release-stg.ps1
```

o parametrización equivalente.

No cambiar el esquema de versionado salvo necesidad técnica.

Puede probarse la misma versión lógica en STG si el aislamiento del servidor/registro es suficiente.

---

## 13. Seguridad operativa

STG debe tener barreras explícitas contra errores humanos.

Como mínimo:

- DB distinta;
- servicios con nombres STG;
- puerto distinto;
- release registry distinto;
- client TEST con server URL STG;
- UI identificada visualmente como STG;
- scripts con nombres STG;
- validaciones que eviten usar configuración PROD accidentalmente cuando sea razonable.

Nunca registrar un dispositivo productivo real en STG.

Nunca usar un dispositivo productivo final como dispositivo de prueba.

---

## 14. Flujo de desarrollo obligatorio

El flujo permanente debe ser:

### Desarrollo

1. partir de `main` actualizado;
2. crear feature branch;
3. implementar cambios;
4. ejecutar tests/build/self-test;
5. desplegar feature branch a STG;
6. ejecutar/aplicar migraciones STG;
7. validar Admin/API en STG;
8. validar cliente TEST contra STG;
9. validar release/update/rollback STG cuando aplique;
10. iterar en la feature branch hasta aprobar STG.

No desplegar una feature branch sobre PROD para revisión visual o exploratoria.

### Paso a PROD

Sólo después de STG aprobado:

1. mergear feature branch a `main`;
2. push de `main`;
3. desplegar server/Admin PROD;
4. validar health y migraciones;
5. probar funcionalidad primero con PC TEST contra PROD;
6. publicar/seleccionar release productiva si corresponde;
7. validar update real en PC TEST;
8. recién después actualizar/configurar el dispositivo productivo final.

El dispositivo productivo final nunca es el primer target de validación.

---

## 15. AGENTS.md

Actualizar `AGENTS.md` con este flujo como regla permanente del repositorio.

Debe dejar explícito:

- qué es STG;
- qué es PROD;
- que las features se validan en STG antes de PROD;
- que no se usa PROD para preview visual;
- que el rollout PROD es PC TEST primero y el dispositivo productivo final después;
- que STG no comparte DB, releases ni dispositivos con PROD;
- que no se deben usar datos personales en STG;
- que cualquier migración debe probarse primero en STG;
- que cualquier cambio de updater/release debe probarse primero en STG cuando sea relevante.

---

## 16. CURRENT_STATE.md

Actualizar `docs/CURRENT_STATE.md` para documentar:

- arquitectura STG/PROD;
- servicios;
- puertos;
- scripts;
- variables;
- flujo de desarrollo;
- cómo iniciar/detener/resetear STG;
- cómo ejecutar cliente STG;
- cómo publicar release STG;
- cómo promover una feature hacia PROD.

---

## 17. Compatibilidad

No romper:

- PROD `0.4.1`;
- `deploy/docker-compose.yml`;
- DB productiva;
- Cloudflare;
- release productiva actual;
- updater productivo;
- dispositivos registrados;
- telemetría;
- Mission System v2;
- RemoteConfig;
- rollback.

La creación de STG debe ser aditiva.

---

## 18. Pruebas requeridas

Validar como mínimo:

### Infraestructura

- PROD sigue arrancando con su configuración actual;
- STG arranca simultáneamente;
- ambos servicios pueden convivir;
- STG usa DB distinta;
- detener/resetear STG no afecta PROD.

### Migraciones

- STG vacío llega correctamente a `head`;
- reset STG + migración funciona nuevamente.

### Seed

- seed crea dispositivos ficticios;
- seed crea telemetría;
- seed es repetible de forma segura;
- no toca PROD.

### Cliente

- cliente TEST puede registrarse/conectarse a STG;
- heartbeat llega a STG;
- RemoteConfig STG llega al cliente;
- `Probar misión ahora` STG funciona.

### Releases

Cuando sea viable en esta etapa:

- publicar release sólo en STG;
- aparece sólo en Admin STG;
- cliente TEST puede actualizar desde STG;
- PROD no ve esa release.

---

## 19. Validación manual

Antes de considerar terminada esta etapa:

1. abrir Admin PROD y confirmar que sigue operativo;
2. abrir Admin STG simultáneamente;
3. confirmar badge/título STG;
4. confirmar dispositivos ficticios;
5. confirmar que PROD y STG muestran datos diferentes;
6. resetear STG y verificar que PROD no cambia;
7. ejecutar cliente TEST contra STG;
8. comprobar heartbeat/RemoteConfig;
9. disparar misión desde STG;
10. revisar Activity STG;
11. si está implementado, probar release/update STG;
12. detener STG;
13. confirmar que PROD continúa operativo.

---

## 20. Git

Implementar esta infraestructura en una rama nueva desde `main`:

```text
feature/staging-environment
```

No retomar todavía `feature/stage3-admin-metrics`.

No hacer merge a `main` automáticamente al finalizar.

No desplegar cambios funcionales Stage 3.

---

## 21. Criterio de aceptación

La etapa STG se considera completa cuando:

- PROD permanece operativo e intacto;
- STG puede correr simultáneamente;
- DB/volúmenes son independientes;
- Admin STG está claramente identificado;
- existe seed ficticio;
- STG puede resetearse sin afectar PROD;
- cliente TEST puede conectarse explícitamente a STG;
- RemoteConfig/misiones pueden probarse en STG;
- releases están separadas o existe aislamiento equivalente verificado;
- scripts son simples e inequívocos;
- `AGENTS.md` documenta el workflow obligatorio;
- `CURRENT_STATE.md` documenta operación STG/PROD;
- tests relevantes están verdes;
- la feature queda lista para validación manual antes de merge.

---

## 22. Versionado y promoción

- PROD usa versiones SemVer sin sufijo, por ejemplo `0.4.1` y luego `0.4.2`.
- Durante desarrollo, STG usa versiones exclusivas por rama: `0.1.0-<rama>`, `0.1.1-<rama>`, etc. No representan la numeración de PROD.
- Si PROD actual es `0.4.1`, la Release Candidate es `0.4.2-rc`; debe probarse integralmente en STG, incluido updater cuando aplique.
- Las prereleases (`-staging-*` y `-rc`) se publican sólo en STG. La release final sin sufijo se publica sólo en PROD.
- El rollout de PROD siempre es: **PC TEST → validación → dispositivo productivo final**.

## 23. Validación final registrada

Se validaron aislamiento PROD/STG, reset de STG sin afectar PROD, cliente WPF TEST, RemoteConfig, misión manual, telemetría, pausa/reanudación, release STG y upgrade/downgrade entre `0.4.1` y `0.4.2`.

La importación sanitizada de telemetría fue validada manualmente: una primera ejecución importó 281 eventos, la segunda agregó 0 eventos y una ejecución con `-Replace` importó 282 porque existía un evento nuevo en PROD. Sólo se importó telemetría permitida hacia dispositivos ficticios STG; no se copiaron identidades ni payloads completos, y PROD permaneció operativo.
