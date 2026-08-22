# Guardian — FINAL Technical Specification
## Mission System v2 + Comprensión funcional Nivel 1

# 1. Propósito de esta spec

Esta es la especificación técnica definitiva para implementar la iteración.

No existe una Technical Design Spec posterior.

Codex debe:

1. inspeccionar el código actual;
2. identificar las implementaciones existentes;
3. mapear esta spec a esas implementaciones;
4. realizar los cambios;
5. probarlos;
6. documentar exactamente qué modificó.

No debe inventar una arquitectura paralela cuando Guardian ya tenga un mecanismo equivalente.

---

# 2. Regla obligatoria: relevar antes de modificar

Antes de editar código, Codex debe inspeccionar como mínimo:

- estructura de la solución/repositorio;
- proyecto cliente Windows;
- flujo actual de bloqueo;
- generación matemática;
- validación de respuestas;
- flujo de reintentos;
- RemoteConfig;
- persistencia local;
- single-instance/mutex existente;
- telemetría;
- `events.jsonl`;
- `events-pending.jsonl`;
- server/API;
- modelo actual de dispositivo/configuración;
- PostgreSQL;
- Admin;
- pantalla actual de configuración;
- tests;
- mecanismo de build/versionado.

Debe identificar concretamente:

```text
CURRENT IMPLEMENTATION MAP

Mission trigger:
<archivo/clase/método real>

Math generator:
<archivo/clase/método real>

Mission UI:
<archivo/clase/método real>

Retry/solve flow:
<archivo/clase/método real>

Remote config DTO:
<archivo/clase real>

Remote config endpoint:
<endpoint/controlador real>

Server device/config persistence:
<modelo/tabla real>

Client local persistence:
<mecanismo real>

Telemetry emitter:
<archivo/clase real>

Admin device config:
<archivo/componente real>

Existing tests:
<proyectos/archivos reales>
```

Este mapa debe formar parte de las notas de implementación de Codex.

Después debe implementar directamente.

No debe detenerse esperando aprobación después del relevamiento.

---

# 3. Git

Crear una rama nueva partiendo de `main` actualizado y limpio.

Nombre recomendado:

```text
feature/stage2-mission-system-comprehension
```

No trabajar directamente en `main`.

No mezclar cambios ajenos a esta iteración.

---

# 4. Modelo técnico definitivo

Toda misión debe identificarse mediante:

```text
category_id
level_id
skill_id
variant_id
mission_id
```

IDs estables en inglés y `snake_case`.

---

# 5. IDs iniciales

## Matemática

```text
category_id = math
```

Nivel:

```text
level_id = basic_operations_1
```

Skills:

```text
addition
subtraction
multiplication
```

Canonical skill keys:

```text
math.basic_operations_1.addition
math.basic_operations_1.subtraction
math.basic_operations_1.multiplication
```

---

## Comprensión

```text
category_id = comprehension
```

Nivel:

```text
level_id = functional_1
```

Skills:

```text
identity
age_birth
current_date
temporal_relations
calendar
seasons
```

Canonical keys:

```text
comprehension.functional_1.identity
comprehension.functional_1.age_birth
comprehension.functional_1.current_date
comprehension.functional_1.temporal_relations
comprehension.functional_1.calendar
comprehension.functional_1.seasons
```

Estos IDs son contratos.

No utilizar los textos visibles como IDs.

---

# 6. Estructura de catálogo

Debe existir un catálogo central de misiones.

Debe reutilizar las abstracciones actuales si existen.

Si no existe una abstracción adecuada, crear una equivalente a:

```text
MissionCatalog
MissionSkill
MissionDefinition
MissionContext
MissionValidator
```

No es obligatorio utilizar exactamente esos nombres.

Sí es obligatorio evitar lógica distribuida mediante múltiples `if/else` sin una fuente central.

Una skill debe poder resolver conceptualmente:

```text
CategoryId
LevelId
SkillId
DisplayName
GenerateMission(context)
```

Una misión generada debe contener:

```text
MissionId
CategoryId
LevelId
SkillId
VariantId
Prompt
Validator
```

---

# 7. Mission ID

Cada presentación real debe tener:

```text
mission_id = UUID
```

Se genera una vez al iniciar la misión.

Debe mantenerse durante todos sus reintentos.

Ejemplo:

```text
MissionStarted mission_id=A
MissionFailed  mission_id=A
MissionFailed  mission_id=A
MissionSolved  mission_id=A
```

Nunca crear nuevo `mission_id` por intento.

---

# 8. Matemática

No reescribir innecesariamente la lógica matemática actual.

Codex debe localizar:

- generación actual;
- rangos;
- operaciones;
- validación.

Separar la lógica actual en skills:

```text
addition
subtraction
multiplication
```

preservando los rangos/reglas actuales.

La reorganización no debe modificar la dificultad matemática salvo que sea imprescindible para separar las operaciones.

---

# 9. Una misión por trigger

Reemplazar el comportamiento de múltiples preguntas por:

```text
Trigger
  ↓
Select one skill
  ↓
Generate one mission
  ↓
MissionStarted
  ↓
Show question
  ↓
Wrong → MissionFailed → retry SAME mission
  ↓
Correct → MissionSolved
  ↓
Unlock
```

Un reintento:

- no selecciona otra skill;
- no cambia variant;
- no cambia mission_id.

---

# 10. Configuración remota de skills

Extender el mecanismo actual de RemoteConfig.

No crear una segunda API de configuración.

Representación recomendada:

```json
{
  "missionConfig": {
    "enabledSkills": [
      "math.basic_operations_1.subtraction",
      "comprehension.functional_1.identity",
      "comprehension.functional_1.current_date"
    ]
  }
}
```

Si el DTO actual usa otra estructura extensible, integrarlo allí conservando el contrato funcional.

No crear booleanos rígidos como:

```text
enableAddition
enableSubtraction
enableIdentity
```

porque dificultan agregar nuevas skills.

---

# 11. Fuente efectiva de estado

El estado real del sistema es:

```text
enabledSkills
```

Los niveles no necesitan un booleano persistido independiente.

Su estado se deriva de las skills hijas.

---

# 12. Compatibilidad con configuración anterior

Codex debe inspeccionar cómo están representadas actualmente las misiones matemáticas.

Debe migrar/converter esa configuración al nuevo modelo sin cambiar inesperadamente los dispositivos existentes.

Requisito:

Un dispositivo con configuración anterior debe seguir funcionando tras actualizar.

Comprensión no debe activarse accidentalmente sólo por instalar la nueva versión.

Debe activarse explícitamente desde Admin.

---

# 13. Admin — pantalla de misiones

Usar la navegación/estructura existente del Admin.

La configuración detallada de misiones debe estar en una pantalla o sección específica, no ocupando permanentemente la tarjeta principal de dispositivo.

Debe permitir configuración **por dispositivo**.

PC de prueba y PC principal pueden tener skills diferentes.

---

# 14. UI de niveles

Render:

```text
☑ Nivel 1 — Operaciones básicas  ⓘ
   ☑ Sumas                       ⓘ
   ☑ Restas                      ⓘ
   ☑ Multiplicaciones            ⓘ
```

y:

```text
— Nivel 1 — Comprensión funcional ⓘ
   ☑ Identidad                    ⓘ
   ☐ Edad y nacimiento            ⓘ
   ☑ Fecha actual                 ⓘ
   ☐ Relaciones temporales        ⓘ
   ☐ Calendario                   ⓘ
   ☐ Estaciones                   ⓘ
```

---

# 15. Checkbox padre

Estado calculado:

```text
all children enabled
→ checked=true
→ indeterminate=false
```

```text
some children enabled
→ checked=false
→ indeterminate=true
```

```text
zero children enabled
→ checked=false
→ indeterminate=false
```

Acciones:

### marcar padre

Habilitar todas sus skills.

### desmarcar padre

Deshabilitar todas.

No persistir simultáneamente:

```text
levelEnabled
```

y:

```text
skillEnabled
```

si pueden entrar en contradicción.

---

# 16. Tooltips

Son obligatorios.

Usar el componente de tooltip existente si existe.

Si no existe, implementar uno reutilizable y accesible.

No usar texto permanente debajo de las skills.

Textos:

### Operaciones básicas

> Operaciones matemáticas básicas.

### Sumas

> Resolver sumas básicas.

### Restas

> Resolver restas básicas.

### Multiplicaciones

> Resolver multiplicaciones básicas.

### Comprensión funcional

> Preguntas cotidianas sobre identidad, edad, fechas, calendario y estaciones.

### Identidad

> Comprender distintas formas de solicitar información básica de identificación.

### Edad y nacimiento

> Reconocer preguntas sobre edad, nacimiento y cumpleaños.

### Fecha actual

> Diferenciar día, mes, año y fecha actual.

### Relaciones temporales

> Comprender referencias como ayer, mañana, mes anterior y mes siguiente.

### Calendario

> Reconocer días, meses y su secuencia.

### Estaciones

> Reconocer estaciones, características y secuencia.

---

# 17. Perfil privado

Agregar un perfil privado asociado al dispositivo/persona que permita resolver las preguntas personales.

Campos lógicos:

```text
preferred_name
first_name
middle_name
last_name
birth_date
```

La timezone sólo debe agregarse al perfil si Guardian no dispone ya de una configuración de timezone apropiada por dispositivo.

---

# 18. Persistencia del perfil privado

Orden obligatorio de decisión:

## A. Si el modelo/configuración existente del dispositivo ya soporta datos estructurados privados extensibles

Extender ese mecanismo.

## B. Si no existe

Agregar persistencia específica en PostgreSQL para el perfil asociado al `device_id`.

No guardar estos datos en:

- código fuente;
- archivos versionados;
- `appsettings` commiteados;
- fixtures públicos;
- documentación pública.

---

# 19. Edición del perfil

Agregar en Admin una sección compacta:

**Perfil para misiones**

Campos:

```text
Nombre habitual
Nombre
Segundo nombre
Apellido
Fecha de nacimiento
```

No incorporar otros datos.

El guardado debe seguir el patrón actual del Admin para configuración de dispositivo.

---

# 20. Transporte del perfil al cliente

El cliente necesita estos datos para:

- generar respuestas válidas;
- calcular edad;
- validar identificación.

Transportarlos utilizando el mecanismo actual de configuración remota.

No crear otro canal.

Enviar sólo los campos necesarios.

---

# 21. Persistencia local del perfil

Si RemoteConfig actualmente se persiste localmente para funcionamiento offline, integrar el perfil allí.

Si no se persiste, agregar únicamente la persistencia mínima necesaria usando el storage local existente de Guardian.

No guardar el perfil dentro de:

```text
events.jsonl
events-pending.jsonl
```

---

# 22. Seguridad de logs

No escribir valores personales en logs.

Incorrecto:

```text
Loaded private mission profile.
birthDate=...
```

Correcto:

```text
Private mission profile loaded.
```

Puede registrarse:

```text
profileConfigured=true
```

o versión/hash técnico no reversible si fuese necesario.

---

# 23. RemoteConfigReceived

No incluir el contenido del perfil en el payload de telemetría de `RemoteConfigReceived`.

Puede incluir:

```json
{
  "missionConfigChanged": true,
  "profileConfigured": true
}
```

No:

```json
{
  "firstName": "...",
  "birthDate": "..."
}
```

---

# 24. Catálogo público de Comprensión

Las preguntas sí pueden vivir en el repo.

Los datos reales no.

Ejemplo permitido:

```text
variant_id = identity_name_question
prompt = ¿Cuál es tu nombre?
validator = personal_name
```

El validator obtiene los valores desde el perfil privado.

---

# 25. Identity validator

Crear validadores conceptuales reutilizables.

### personal_name

Aceptar combinaciones normalizadas de:

```text
preferred_name
first_name
first_name + last_name
full_name
```

### last_name

Aceptar:

```text
last_name
```

### name_and_last_name

Aceptar:

```text
first_name + last_name
full_name
```

### full_name

Aceptar únicamente:

```text
first_name + middle_name + last_name
```

Cuando `middle_name` no exista, manejarlo sin espacios dobles.

---

# 26. Identity variants

IDs estables sugeridos:

```text
identity_name_ask_1
identity_name_ask_2
identity_name_field
identity_last_name_ask
identity_last_name_field
identity_name_last_name_ask
identity_name_last_name_field
identity_full_name_ask
```

Prompts:

```text
¿Cuál es tu nombre?
¿Cómo te llamás?
Nombre:
¿Cuál es tu apellido?
Apellido:
¿Cuál es tu nombre y apellido?
Nombre y apellido:
¿Cuál es tu nombre completo?
```

No incluir respuestas reales en el catálogo.

---

# 27. Age/birth

Variants sugeridas:

```text
age_ask_1
age_ask_2
age_field
birth_year_ask
birth_year_field
birthday_ask
```

Prompts:

```text
¿Cuántos años tenés?
¿Qué edad tenés?
Edad:
¿En qué año naciste?
Año de nacimiento:
¿Cuándo es tu cumpleaños?
```

---

# 28. Cálculo de edad

Calcular desde `birth_date` y la fecha local.

No utilizar:

```text
currentYear - birthYear
```

sin comprobar si el cumpleaños ya ocurrió.

Debe manejar correctamente el período anterior al cumpleaños.

---

# 29. Current date variants

IDs sugeridos:

```text
current_year_ask_1
current_year_ask_2
current_month_ask_1
current_month_ask_2
current_weekday
current_day_of_month
current_full_date
```

Prompts:

```text
¿En qué año estamos?
¿Qué año es?
¿En qué mes estamos?
¿Qué mes es?
¿Qué día de la semana es hoy?
¿Qué día del mes es hoy?
¿Qué fecha es hoy?
```

Calcular la respuesta al generar la misión.

---

# 30. Temporal relations variants

```text
tomorrow_weekday
yesterday_weekday
next_month_ask_1
next_month_ask_2
previous_month
```

Prompts:

```text
¿Qué día de la semana es mañana?
¿Qué día de la semana fue ayer?
¿Cuál es el mes que viene?
¿Qué mes viene después de este?
¿Cuál fue el mes pasado?
```

---

# 31. Calendar variants

Estáticas:

```text
days_in_week
months_in_year
```

Generadas:

```text
weekday_after
weekday_before
month_after
month_before
```

El generador debe elegir el valor de referencia.

Ejemplo:

```text
weekday_after(reference=Monday)
```

produce:

```text
¿Qué día viene después del lunes?
```

No crear 7 variants distintas sólo para cambiar el día.

---

# 32. Seasons variants

Estáticas:

```text
season_cold
season_hot
season_falling_leaves
season_flowers
```

Generada:

```text
season_after
```

Debe poder utilizar cualquiera de las cuatro estaciones.

---

# 33. Normalizador textual común

Antes de comparar:

1. trim;
2. lowercase;
3. Unicode normalization;
4. eliminación/normalización de tildes para comparación;
5. normalización de espacios consecutivos;
6. eliminación de puntuación irrelevante.

Ejemplo:

```text
"  NOMBRE DE EJEMPLO   "
```

debe normalizar consistentemente.

No utilizar fuzzy matching libre.

---

# 34. Validación determinística

No utilizar:

- LLM;
- embeddings;
- fuzzy similarity general.

Usar:

- conjuntos de valores;
- enums;
- parsers;
- fechas;
- números;
- reglas explícitas.

Una respuesta que “se parece” pero es conceptualmente incorrecta debe fallar.

---

# 35. Números

Para valores pequeños conocidos, aceptar número y palabra cuando sea simple.

Ejemplos:

```text
7
siete
```

```text
12
doce
```

Para edad también puede soportarse número/palabra si el helper ya resulta general.

No convertir esta iteración en un parser completo de números españoles.

---

# 36. Fechas

Para fecha completa aceptar formatos razonables equivalentes.

Ejemplo para una fecha:

```text
22/08/2026
22-08-2026
22 de agosto de 2026
22 agosto 2026
```

Para cumpleaños:

```text
día/mes
día de mes
fecha completa
```

Validar por valor de fecha, no por string exacto.

---

# 37. Zona horaria

Crear/reutilizar una única abstracción de reloj local.

Ejemplo conceptual:

```text
IGuardianClock
NowLocal
TodayLocal
```

No dispersar llamadas directas a:

```text
DateTime.Now
DateTime.UtcNow
```

por generadores diferentes si puede evitarse.

Todas estas funciones deben usar el mismo concepto de fecha local:

- fecha actual;
- edad;
- ayer;
- mañana;
- próximo mes;
- mes anterior;
- reset diario del ciclo.

---

# 38. Timezone actual

Guardian debe producir resultados correctos para la timezone local configurada del dispositivo.

No calcular preguntas de calendario usando UTC.

Debe evitar el problema de que un evento a última hora de Argentina corresponda al día siguiente UTC.

---

# 39. Mission selector

Implementar/reutilizar un único selector global.

Input:

```text
enabledSkills
rotationState
missionContext
```

Proceso:

```text
availableSkills = enabledSkills - usedSkillsInCycle
```

Si:

```text
availableSkills.Count > 0
```

seleccionar aleatoriamente una.

Si:

```text
availableSkills.Count == 0
```

entonces:

```text
usedSkillsInCycle = empty
availableSkills = enabledSkills
```

y seleccionar.

---

# 40. Rotación entre categorías

No mantener pools independientes de Matemática y Comprensión.

Si activas:

```text
subtraction
identity
current_date
seasons
```

esas cuatro skills compiten dentro del mismo ciclo.

---

# 41. Momento en que una skill queda usada

Marcarla como utilizada cuando se crea/presenta efectivamente la misión y se emite `MissionStarted`.

No esperar a `MissionSolved`.

Un error no debe permitir que el selector presente otra skill dentro del mismo bloqueo.

---

# 42. Persistencia de rotación

Debe sobrevivir:

- cierre de Guardian;
- reinicio del proceso;
- reinicio de Windows.

Persistir como mínimo:

```json
{
  "localDate": "YYYY-MM-DD",
  "usedSkillsInCycle": [],
  "lastVariantBySkill": {}
}
```

Usar el storage local actual si existe.

No crear SQLite adicional sólo para esto.

---

# 43. Reset diario

Al cargar estado o seleccionar una misión:

```text
if storedLocalDate != currentLocalDate
```

entonces:

```text
storedLocalDate = currentLocalDate
usedSkillsInCycle = []
```

Puede conservarse o resetearse `lastVariantBySkill`.

La preferencia es conservarlo para evitar repetir la misma variante al comenzar el día siguiente cuando existan alternativas.

---

# 44. Cambios de configuración

Antes de cada selección:

```text
usedEffective =
usedSkillsInCycle intersect enabledSkills
```

Una skill deshabilitada deja de afectar el ciclo.

---

# 45. Skill activada durante ciclo

Ejemplo:

```text
enabled antes: A,B
used: A
```

se activa C.

Disponibles:

```text
B,C
```

C participa inmediatamente.

---

# 46. Cero skills

Si:

```text
enabledSkills.Count == 0
```

el scheduler/trigger:

- no bloquea;
- no crea `MissionStarted`;
- no crea misión fallback;
- no lanza error.

Puede registrar un evento diagnóstico no ruidoso si encaja con la telemetría actual.

---

# 47. Selección de variante

Una vez elegida una skill:

```text
GenerateMission(context)
```

Debe elegir/generar una variante.

Si existen múltiples variants:

- evitar `lastVariantBySkill[skill]` cuando haya alternativa;
- elegir aleatoriamente entre las restantes.

Si sólo existe una:

- reutilizarla.

---

# 48. Retry

Respuesta incorrecta:

```text
MissionFailed
```

incrementa intento.

Debe mostrar nuevamente la misma pregunta.

No volver a invocar MissionSelector.

---

# 49. Telemetría

Mantener los tipos actuales:

```text
MissionStarted
MissionFailed
MissionSolved
```

Extender payload.

Campos:

```json
{
  "mission_id": "...",
  "category_id": "comprehension",
  "level_id": "functional_1",
  "skill_id": "current_date",
  "variant_id": "current_month_ask_1",
  "attempt": 1
}
```

---

# 50. Matemática en telemetría

Ejemplo:

```json
{
  "mission_id": "...",
  "category_id": "math",
  "level_id": "basic_operations_1",
  "skill_id": "subtraction",
  "variant_id": "generated",
  "attempt": 2
}
```

Si actualmente existe información útil de operandos/resultado, preservarla si no genera un problema de compatibilidad.

---

# 51. Respuestas textuales y privacidad

No incluir en eventos:

```text
raw_answer
answer
expected_answer
profile_value
birth_date
first_name
last_name
```

para comprensión.

Registrar solamente:

- misión;
- habilidad;
- variante;
- resultado;
- intento.

---

# 52. Semántica de intentos

Codex debe revisar cómo funciona hoy.

Si ya existe una semántica consistente, preservarla.

Debe poder inferirse claramente:

- resuelto al primer intento;
- cantidad de fallos;
- cantidad total de intentos.

No cambiar eventos históricos innecesariamente.

---

# 53. Compatibilidad de telemetría

El servidor/Admin debe soportar eventos antiguos sin:

```text
mission_id
category_id
level_id
skill_id
variant_id
```

No hacer migración destructiva.

---

# 54. PostgreSQL / device_events

Si `device_events.payload` continúa siendo JSONB y es suficiente:

- almacenar allí los campos nuevos;
- no agregar columnas únicamente por esta iteración.

Las futuras métricas pueden crear vistas/columnas posteriormente.

---

# 55. Perfil en PostgreSQL

Antes de crear tabla, inspeccionar el modelo.

## Si existe almacenamiento JSON/config privado por dispositivo

Extenderlo.

## Si no existe

Crear una tabla de perfil claramente separada.

Conceptualmente:

```text
device_mission_profiles

device_id
preferred_name
first_name
middle_name
last_name
birth_date
created_at
updated_at
```

Debe respetar las convenciones reales del proyecto:

- naming;
- PK;
- FK;
- timestamps;
- migrations.

No copiar esta estructura literalmente si contradice el modelo existente.

---

# 56. API

Reutilizar los endpoints actuales de configuración del dispositivo.

El Admin modifica:

```text
missionConfig.enabledSkills
privateMissionProfile
```

siguiendo el patrón actual.

No crear endpoints adicionales si el endpoint existente admite extender su DTO.

Si el diseño actual separa correctamente config pública/privada y requiere endpoints diferentes, respetar ese patrón.

---

# 57. Contrato de privacidad de API

Los endpoints del perfil:

- sólo deben estar disponibles detrás de la protección del Admin ya existente;
- no deben exponerse públicamente a rutas no autenticadas;
- no deben aparecer en respuestas de listado si no son necesarias.

Mantener las protecciones actuales de Cloudflare/Admin.

---

# 58. Admin y datos privados

El Admin puede mostrar los valores del perfil para edición porque es el lugar autorizado.

No mostrarlos en:

- actividad;
- telemetría;
- dashboard público futuro;
- logs.

---

# 59. Archivo events.jsonl

Mantener el formato/eventos actuales.

Las entradas de misión pueden incorporar los nuevos IDs.

No incluir la respuesta textual ni valores privados.

---

# 60. events-pending.jsonl

Misma regla.

No debe transformarse accidentalmente en una copia del perfil o de la respuesta escrita.

---

# 61. Tests públicos y datos ficticios

Todos los tests que necesiten perfil utilizarán valores ficticios.

Ejemplo:

```text
preferredName = "Tomi"
firstName = "Tomás"
middleName = "Luis"
lastName = "Pérez"
birthDate = fixed test date
```

No usar información personal real.

---

# 62. Tests de Identity

Ejemplo con perfil ficticio.

Para `personal_name`:

Correctos:

```text
Tomi
Tomás
Tomás Pérez
Tomás Luis Pérez
```

Incorrectos:

```text
Pérez
Luis
```

Para `last_name`:

```text
Pérez → correct
Tomás → incorrect
```

Para `full_name`:

```text
Tomás Luis Pérez → correct
Tomás Pérez → incorrect
Tomi → incorrect
```

---

# 63. Tests de normalización

Debe probar:

```text
TOMÁS
tomás
Tomas
 " tomás "
```

según la normalización definida.

No exigir tildes.

---

# 64. Clock inyectable/testeable

Los tests de calendario no dependen del reloj real.

Usar/reutilizar un clock inyectable.

Fecha de ejemplo:

```text
2026-08-22
```

Esperado:

```text
year: 2026
month: agosto
weekday: sábado
day: 22
tomorrow: domingo
yesterday: viernes
next month: septiembre
previous month: julio
```

---

# 65. Tests de cambio de año

Caso:

```text
2026-12-31
```

Esperar:

```text
next month = enero
tomorrow date = 2027-01-01
```

Caso:

```text
2027-01-01
```

Esperar:

```text
previous month = diciembre
yesterday date = 2026-12-31
```

---

# 66. Tests de edad

Probar:

- día anterior al cumpleaños;
- día del cumpleaños;
- día posterior;
- cambio de año.

No asumir edad sólo por diferencia de años.

---

# 67. Tests de Calendar generator

Debe probar:

```text
Monday → after = Tuesday
Monday → before = Sunday
December → after = January
January → before = December
```

y otros ejemplos.

---

# 68. Tests de Seasons

Probar:

```text
winter → spring
spring → summer
summer → autumn
autumn → winter
```

Además:

```text
cold → winter
hot → summer
falling leaves → autumn
flowers → spring
```

---

# 69. Tests de rotación

Con:

```text
A,B,C
```

primeras tres selecciones:

```text
must all be different
```

La cuarta:

```text
may be A/B/C
```

No probar un orden aleatorio concreto.

---

# 70. Tests de rotación global

Skills:

```text
math.subtraction
comprehension.identity
comprehension.current_date
```

Los tres deben aparecer antes de repetir cualquiera.

Esto verifica que no haya ciclos separados por categoría.

---

# 71. Test de reinicio

Persistir:

```text
used = [A,B]
enabled = [A,B,C]
date = today
```

Recrear el selector simulando restart.

Próxima skill:

```text
C
```

obligatoriamente.

---

# 72. Test de cambio de día

Persistir:

```text
used = [A,B]
storedDate = yesterday
```

Inicializar hoy.

Debe resetear:

```text
used = []
```

---

# 73. Test cambio de config

Inicial:

```text
enabled = A,B,C
used = A
```

Desactivar B.

Resultado:

```text
next = C
```

Otro:

```text
enabled = A,B
used = A
```

Activar C.

Siguiente debe ser:

```text
B or C
```

Nunca A.

---

# 74. Test cero skills

Debe comprobar:

- no lock;
- no `MissionStarted`;
- no crash;
- no fallback.

---

# 75. Test retry

Una misión:

```text
mission_id = X
skill = identity
variant = V
```

Primer error:

```text
MissionFailed X
```

Segundo intento correcto:

```text
MissionSolved X
```

Debe mantenerse:

```text
skill = identity
variant = V
mission_id = X
```

---

# 76. Tests Admin checkbox

### Todas

```text
checked=true
indeterminate=false
```

### Algunas

```text
checked=false
indeterminate=true
```

### Ninguna

```text
checked=false
indeterminate=false
```

Click padre debe actualizar todas las hijas.

---

# 77. Test de tooltips

Cada nivel y skill debe tener texto accesible.

No necesita screenshot test si la stack actual no lo utiliza.

Sí testear componente/atributos si ya existen tests UI equivalentes.

---

# 78. Test perfil ausente

Si una skill requiere perfil y faltan datos necesarios, la misión no debe quedar imposible de resolver.

Ejemplo:

`age_birth` activa pero no existe `birth_date`.

Comportamiento:

1. esa skill se considera temporalmente no disponible;
2. el selector usa otra skill disponible;
3. registrar diagnóstico técnico sin valor sensible.

No inventar respuestas.

---

# 79. Perfil parcialmente configurado

La disponibilidad puede calcularse por variante.

Ejemplo:

Si existen:

```text
first_name
last_name
```

pero no:

```text
birth_date
```

Identity puede funcionar.

Age/birth no.

Una skill sólo debe entrar en el pool si puede generar al menos una misión válida con la configuración actual.

---

# 80. Configuración Admin inválida

El Admin debe validar:

```text
birth_date
```

como fecha real.

Campos de nombre:

- trim;
- no guardar strings vacíos como valores significativos.

No imponer middle name obligatorio.

---

# 81. Configuración y skill availability

Distinguir:

```text
enabled
```

de:

```text
available
```

Una skill puede estar habilitada en Admin pero no estar disponible temporalmente por falta de datos.

El selector utiliza:

```text
effectiveSkills =
enabledSkills ∩ availableSkills
```

---

# 82. Cero effective skills

Si hay skills habilitadas pero ninguna disponible por perfil/config inválido:

aplicar la misma regla que cero skills:

- no bloquear;
- diagnóstico;
- no fallback.

---

# 83. Remote config update durante misión

Si llega nueva configuración mientras una misión ya está visible:

- no reemplazar la misión actual;
- completar/reintentar esa misión;
- aplicar la nueva configuración en el próximo trigger.

Evitar cambios inesperados en medio de una respuesta.

---

# 84. Concurrencia

Respetar los locks/mutex existentes.

No introducir escrituras simultáneas inseguras en:

- rotation state;
- remote config;
- telemetry queue.

Si existe una infraestructura de sincronización actual, reutilizarla.

---

# 85. Update/rollback

No modificar el updater salvo que sea estrictamente necesario por cambio de config.

La nueva versión debe seguir soportando:

- upgrade;
- downgrade;
- rollback;
- telemetría Update*.

---

# 86. Config compatibility

Cliente nuevo + server nuevo:

funcionalidad completa.

Cliente viejo + server nuevo:

- debe seguir recibiendo config compatible;
- ignorar campos nuevos si corresponde;
- no crash.

Eventos viejos + Admin nuevo:

- deben seguir visualizándose.

---

# 87. No romper Stage 1

Mantener funcionando:

- registro de dispositivos;
- heartbeat;
- actividad;
- telemetría batch;
- update;
- rollback;
- RemoteConfig;
- bloqueo;
- unlock;
- single instance;
- events queue.

---

# 88. Build/version

Usar el mecanismo actual de versionado.

Incrementar versión según la convención real del proyecto.

No decidir una versión arbitraria antes de inspeccionar cómo se versiona Guardian.

Codex debe informar la versión final generada.

---

# 89. Documentación pública

No commitear documentos que contengan:

- nombres reales;
- fecha de nacimiento real;
- respuestas personales;
- secretos;
- dominio/credenciales privadas si ya existe una política de exclusión.

Si estas specs se agregan al repo, utilizar exclusivamente placeholders/datos ficticios.

---

# 90. Validación manual en PC de prueba

Antes de rollout al dispositivo principal:

Configurar por ejemplo:

```text
Sumas OFF
Restas ON
Multiplicaciones OFF

Identidad ON
Edad y nacimiento según perfil
Fecha actual ON
Relaciones temporales OFF
Calendario OFF
Estaciones ON
```

Verificar que sólo participen:

```text
Restas
Identidad
Edad y nacimiento si disponible
Fecha actual
Estaciones
```

---

# 91. Validación del ciclo

Con cuatro skills efectivas:

1. provocar cuatro triggers;
2. comprobar cuatro skills diferentes;
3. quinto trigger inicia nuevo ciclo;
4. puede repetirse cualquiera.

---

# 92. Validación de restart

Después de usar dos skills de un ciclo:

1. cerrar Guardian;
2. abrir Guardian;
3. provocar siguiente misión;
4. comprobar que no repite las utilizadas si existen pendientes.

---

# 93. Validación de variantes

Esperar un ciclo nuevo.

Si vuelve Identidad:

- puede elegir otra formulación;
- preferentemente no repetir la variante anterior.

---

# 94. Validación Admin

Comprobar:

- checked;
- unchecked;
- indeterminate;
- selección padre;
- selección individual;
- tooltips;
- guardado;
- llegada de RemoteConfig;
- diferencias por dispositivo.

---

# 95. Validación de privacidad

Buscar en:

```text
repo
logs
events.jsonl
events-pending.jsonl
PostgreSQL device_events payload
```

y comprobar que los datos privados y respuestas textuales no se filtren donde no corresponde.

Los datos privados sólo deben existir en el storage destinado al perfil y en la configuración local privada necesaria.

---

# 96. Cambios de DB

Si Codex necesita una migration:

- crearla mediante el mecanismo existente;
- hacerla forward-compatible;
- no borrar datos existentes;
- documentar upgrade;
- documentar rollback si aplica.

---

# 97. Deliverable obligatorio de Codex

Al finalizar debe entregar un resumen con:

## A. Relevamiento

Archivos/clases reales encontrados para:

- mission flow;
- math;
- config;
- persistence;
- telemetry;
- server;
- Admin.

## B. Cambios

Lista exacta de:

- archivos modificados;
- archivos agregados;
- migrations;
- endpoints/DTOs modificados;
- config nueva.

## C. Tests

- tests ejecutados;
- resultado;
- tests agregados.

## D. Build

- versión;
- artefacto generado;
- ubicación.

## E. Validación manual

Pasos exactos para probar en PC de prueba.

## F. Riesgos o pendientes

Sólo pendientes reales.

No proponer Stage 3 ni ampliar alcance.

---

# 98. No hacer

Codex NO debe:

- mergear a `main`;
- desplegar automáticamente al dispositivo principal;
- habilitar Comprensión automáticamente;
- inventar datos personales;
- poner información personal en Git;
- agregar un LLM;
- crear un editor de preguntas;
- crear nuevos niveles;
- implementar analytics adaptativos;
- reescribir el sistema completo;
- crear otra DB local innecesaria;
- crear otra API de configuración si ya existe una apropiada;
- eliminar telemetría histórica;
- modificar features no relacionadas.

---

# 99. Definition of Done

Esta iteración está técnicamente completa cuando:

1. El código actual fue relevado antes de modificarlo.
2. Matemática está integrada a Category → Level → Skill.
3. Addition, subtraction y multiplication son configurables.
4. Comprensión funcional está implementada.
5. Sus seis skills están implementadas.
6. Las variants viven en el catálogo y no en Admin.
7. El perfil privado está fuera de Git.
8. El perfil se edita desde Admin.
9. El cliente recibe los datos mínimos necesarios.
10. Datos privados no aparecen en logs/telemetría.
11. Una misión se presenta por trigger.
12. Retry conserva misión, skill, variant y mission_id.
13. Selector global combina categorías.
14. No repite skill hasta completar ciclo.
15. Estado de ciclo persiste tras restart.
16. Cambio de día resetea ciclo.
17. Cambio de configuración funciona en runtime.
18. Zero effective skills no bloquea.
19. Skills con datos faltantes no generan misiones imposibles.
20. Fechas usan timezone local.
21. Edad se calcula correctamente.
22. Validación textual es determinística y normalizada.
23. Nivel padre checked/indeterminate/unchecked funciona.
24. Tooltips están implementados.
25. Config sigue siendo por dispositivo.
26. Telemetría incluye mission/category/level/skill/variant.
27. Eventos históricos siguen siendo compatibles.
28. Stage 1 continúa funcionando.
29. Tests nuevos y existentes pasan.
30. Existe build listo para validación manual.
31. No se hizo merge a `main`.
32. Codex entrega el informe final especificado.
