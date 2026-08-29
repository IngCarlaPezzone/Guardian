# Changelog

## 0.4.5-staging-tray-icon

- En instalaciones con `GUARDIAN_HOME` explícito (STG/test), muestra el ícono de bandeja naranja y el tooltip `Guardian STG`; PROD conserva el ícono y tooltip azules actuales. La prerelease quedó registrada sólo en STG para validación visual.

## 0.4.4

- Corrige la aplicación de RemoteConfig cuando el cliente conserva la misma versión pero sus habilidades o perfil locales no coinciden con el servidor.
- Validada en STG con una configuración que habilita sólo Comprensión: tras reiniciar, Guardian genera Comprensión y no Matemática. La release está registrada en PROD y queda pendiente la validación obligatoria en PC TEST antes de cualquier dispositivo productivo final.

## 0.4.3

- Validada manualmente en STG con la prerelease `0.4.3-staging-comprehension-help.6` y la RC `0.4.3-rc.3`, e integrada en `main`. La release `0.4.3` está registrada en PROD; queda pendiente la validación obligatoria en PC TEST antes de cualquier dispositivo productivo final.
- Incorpora ayudas progresivas determinísticas: tras el primer error no ortográfico aparece la rutina `MIRO → PIENSO → RESPONDO` y se puede pedir la ayuda 1; los siguientes errores no ortográficos muestran las ayudas 2 y 3. Llegar a ayuda 3 no desbloquea la misión.
- Mantiene una rama ortográfica local y conservadora independiente de las ayudas de comprensión. Revelar la forma correcta nunca resuelve automáticamente la misión.
- Añade Vocabulario de consignas, Fecha de nacimiento como variante distinta de Cumpleaños, íconos PNG locales y el catálogo final editable de preguntas y ayudas de comprensión.
- Retira `next_month_ask_2`; conserva `vocab_before` y `vocab_after`.
- Amplía la telemetría de misión con niveles de habilidad y ayuda, y estado ortográfico acumulado, sin contenido de respuestas ni datos de perfiles privados.
- Corrige el updater para respaldar, instalar y restaurar `Assets\Icons` junto con los binarios, y embebe los PNG en `Guardian.exe` para que también aparezcan al actualizar desde un updater anterior. `0.4.3-rc` y `0.4.3-rc.2` quedan descartadas para promoción.

## 0.4.2

- Corrige la persistencia de versión tras reiniciar Windows: cliente, updater, instaladores, watchdog y autoarranque usan una única ubicación canónica.
- Repara idempotentemente `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Guardian` cuando conserva una ruta histórica o temporal.
- Al ejecutar Guardian desde un ZIP, instala sus binarios en la ruta canónica antes de registrar startup.
- Conserva un home explícito de STG mediante `--home`, evitando que un reboot de un cliente TEST use la instalación local de PROD.
- Añade eventos técnicos de inicio y de colisión de instancia con ruta anonimizada, resultado de reparación y PID.
- Valida en STG update, dos reboots y reparación de una entrada histórica simulada.

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
