# Propuesta de iteración — nuevas habilidades de Matemática y Comprensión

**Estado:** implementación aprobada en STG y publicada como `0.4.9` en PROD; la actualización se validó únicamente en PC TEST. El dispositivo productivo final permanece fuera del rollout.

## 1. Objetivo y criterios

Incorporar habilidades configurables e independientes dentro de la jerarquía existente:

`Categoría → Nivel → Habilidad → Variante`

La propuesta busca ampliar la práctica sin mezclar demandas que deben poder observarse por separado:

- cálculo directo;
- comprensión de qué operación describe una situación;
- comprensión de información escrita de forma literal;
- orientación geográfica personal básica.

Cada habilidad se puede habilitar o pausar individualmente desde Admin. Las variantes no se administran individualmente: evitan repeticiones y permiten ver resultados por tipo de pregunta.

Las ayudas conservan la rutina actual **Miro → Pienso → Respondo**, se solicitan después de un error y no resuelven ni desbloquean solas la misión.

## 2. Alcance propuesto

| Categoría | Nivel | Habilidades nuevas |
| --- | --- | --- |
| Matemática | Operaciones básicas | Divisiones exactas: divisor de una cifra; divisor de dos cifras sin ceros; decenas exactas. Multiplicaciones por 10, 100 y 1000. |
| Matemática | Situaciones problemáticas | Problemas cotidianos de suma; Problemas cotidianos de resta. |
| Comprensión | Comprensión funcional | Ubicación personal. |
| Comprensión | Información explícita | Información explícita en una oración. |

No se incluyen todavía divisiones con resto, problemas de multiplicación/división, problemas de dos pasos, datos irrelevantes, comprensión inferencial ni textos de más de una oración.

## 3. Decisiones definidas para la implementación

1. **Implementación completa.** La iteración implementa y prueba todas las habilidades y variantes de este documento. La selección de qué habilidades se habilitan para la práctica será una decisión posterior del adulto desde Admin; no forma parte de esta especificación.
2. **Divisiones.** Las tres clases serán habilidades independientes, no simples variantes, para poder habilitarlas y medirlas por separado.
3. **Multiplicación por potencias de diez.** Será una habilidad nueva e independiente de las multiplicaciones actuales de factores 3 a 12.
4. **Ubicación personal.** Requerirá que ciudad, provincia y país estén completos. No se usará una API geográfica: el adulto responsable confirma los tres valores al guardarlos.
5. **Privacidad.** Ciudad, provincia y país se guardarán en el perfil privado y sus respuestas se conservarán en el mismo circuito protegido que las respuestas de identidad y nacimiento, para permitir revisar cómo respondió la persona. No se incluirán en documentación, fixtures, reportes públicos ni importaciones sanitizadas de STG.
6. **Ayudas en Matemática.** El soporte progresivo de esta iteración se aplica exclusivamente a **Situaciones problemáticas**, donde ayuda a comprender la consigna y la operación. Las operaciones directas —incluidas las nuevas divisiones y multiplicaciones por potencias de diez— no muestran ayudas.

## 4. Diseño de ayudas

Cada ayuda tiene una función distinta:

| Nivel | Función | Regla de redacción |
| --- | --- | --- |
| 1 — Decilo de otra manera | Aclara qué pide la consigna. | No nombra todavía el procedimiento completo. |
| 2 — Dame una pista | Define el concepto o marca la palabra clave que organiza la consigna. | No repite las cantidades: aclara qué significa una palabra decisiva, como “más”, “quedan”, “provincia” o “país”. |
| 3 — Guiame un poco más | Propone el armado concreto para que la persona lo complete. | No escribe directamente la respuesta final. |

Las ayudas se muestran con los valores reales de la variante, pero esos valores quedan sólo en la computadora para las misiones de perfil privado.

## 5. Matemática — Operaciones básicas

### 5.1 Divisiones exactas con divisor de una cifra

**Clave propuesta:** `math.basic_operations_1.exact_division_one_digit`
**Tooltip:** “Resolver divisiones exactas con divisor de una cifra.”

**Reglas de generación:** dividendo entre 12 y 81; divisor entre 2 y 9; resultado entero entre 2 y 12; sin resto. Se alternan los pares de la tabla de multiplicar. Ejemplos: `24 : 4`, `42 : 6`, `56 : 7`, `72 : 8`.

**Variante:** `division_one_digit` — `{dividendo} : {divisor} = ?`.

### 5.2 Divisiones exactas con divisor de dos cifras sin ceros

**Clave propuesta:** `math.basic_operations_1.exact_division_two_digits`
**Tooltip:** “Resolver divisiones exactas con divisor de dos cifras.”

**Reglas de generación:** divisor de dos cifras sin cero, entre 11 y 25; cociente entre 2 y 12; dividendo igual a divisor × cociente. Ejemplos: `36 : 12`, `72 : 18`, `84 : 21`, `144 : 12`, `180 : 15`.

**Variante:** `division_two_digits` — `{dividendo} : {divisor} = ?`.

### 5.3 Divisiones exactas con decenas

**Clave propuesta:** `math.basic_operations_1.exact_division_tens`
**Tooltip:** “Resolver divisiones exactas con decenas.”

**Reglas de generación:** divisor 10, 20, 30, 40 o 50; cociente entre 2 y 12; dividendo igual a divisor × cociente. Ejemplos: `60 : 10`, `120 : 20`, `240 : 40`, `360 : 30`, `500 : 50`.

**Variante:** `division_tens` — `{dividendo} : {divisor} = ?`.

### 5.4 Multiplicaciones por 10, 100 y 1000

**Clave propuesta:** `math.basic_operations_1.multiply_by_powers_of_ten`
**Tooltip:** “Multiplicar números por 10, 100 y 1000.”

**Reglas de generación:** primer factor entero entre 1 y 20; segundo factor 10, 100 o 1000. No se mezclará aún con multiplicaciones de dos factores arbitrarios.

| Variante | Pregunta |
| --- | --- |
| `multiply_by_10` | `{numero} × 10 = ?` |
| `multiply_by_100` | `{numero} × 100 = ?` |
| `multiply_by_1000` | `{numero} × 1000 = ?` |

## 6. Matemática — Situaciones problemáticas

**Nivel propuesto:** `math.word_problems_1` — **Situaciones problemáticas**
**Tooltip:** “Resolver situaciones cotidianas de una sola acción.”

Las consignas serán de una oración o dos muy cortas, sin información irrelevante. Las respuestas se escriben sólo como número. En esta primera versión no habrá una pantalla separada para elegir la operación: las ayudas permiten hacer explícita la relación matemática.

### 6.1 Problemas cotidianos de suma

**Clave propuesta:** `math.word_problems_1.everyday_addition`
**Tooltip:** “Resolver situaciones en las que se agregan cantidades.”

**Reglas de generación:** dos cantidades entre 1 y 10; total máximo 20; siempre se agrega una cantidad a otra existente.

| Variante | Pregunta | Ayuda 1 | Ayuda 2 | Ayuda 3 |
| --- | --- | --- | --- | --- |
| `add_strawberries` | “Hay {a} frutillas en casa. Papá compra {b} frutillas más. ¿Cuántas frutillas hay en total?” | “La pregunta pide saber cuántas frutillas hay en total.” | “La palabra MÁS avisa que se agregan frutillas.” | “Cuando se agregan más frutillas, se suman: {a} + {b} = ___.” |
| `add_stickers` | “{a} figuritas están en el álbum. Agregás {b} figuritas. ¿Cuántas figuritas hay en el álbum?” | “La pregunta pide el total de figuritas que hay en el álbum.” | “AGREGAR significa sumar algo a lo que ya había.” | “Agregar figuritas es sumar: {a} + {b} = ___.” |
| `add_pencils` | “En la cartuchera hay {a} lápices. Guardás {b} lápices más. ¿Cuántos lápices hay en total?” | “La pregunta pide cuántos lápices hay en total.” | “MÁS indica que entran lápices nuevos.” | “Como entran más lápices, sumá: {a} + {b} = ___.” |
| `add_balloons` | “Hay {a} globos en la mesa. Traen {b} globos más. ¿Cuántos globos hay en total?” | “La pregunta pide el total de globos.” | “La palabra MÁS indica que la cantidad aumenta.” | “Para saber el total cuando llegan más, sumá: {a} + {b} = ___.” |
| `add_cookies` | “Hay {a} galletitas en el plato. Ponés {b} galletitas más. ¿Cuántas galletitas hay en total?” | “La pregunta pide cuántas galletitas hay en total.” | “PONER MÁS hace que haya una cantidad mayor.” | “Al poner más, la cantidad aumenta: {a} + {b} = ___.” |
| `add_blocks` | “Tenés {a} bloques. Te regalan {b} bloques. ¿Cuántos bloques tenés en total?” | “La pregunta pide cuántos bloques tenés en total.” | “Un REGALO agrega bloques a los que ya tenías.” | “Un regalo agrega bloques: {a} + {b} = ___.” |

### 6.2 Problemas cotidianos de resta

**Clave propuesta:** `math.word_problems_1.everyday_subtraction`
**Tooltip:** “Resolver situaciones en las que se quitan cantidades.”

**Reglas de generación:** cantidad inicial entre 2 y 20; cantidad retirada entre 1 y inicial − 1; resultado siempre positivo. La relación siempre es quitar, gastar, comer, usar o regalar.

| Variante | Pregunta | Ayuda 1 | Ayuda 2 | Ayuda 3 |
| --- | --- | --- | --- | --- |
| `subtract_strawberries` | “Hay {a} frutillas en casa. Comen {b}. ¿Cuántas frutillas quedan?” | “La pregunta pide saber cuántas frutillas quedan.” | “COMEN indica que se sacan frutillas de las que había.” | “Cuando se sacan frutillas, se resta: {a} − {b} = ___.” |
| `subtract_stickers` | “Tenés {a} figuritas. Regalás {b}. ¿Cuántas figuritas te quedan?” | “La pregunta pide las figuritas que quedan después de regalar.” | “REGALAR hace que tengas menos figuritas.” | “Regalar quita figuritas: {a} − {b} = ___.” |
| `subtract_pencils` | “Tenés {a} lápices. Perdés {b}. ¿Cuántos lápices te quedan?” | “La pregunta pide los lápices que te quedan.” | “PERDER significa que ya no tenés algunos lápices.” | “Los lápices perdidos se sacan de los que tenías: {a} − {b} = ___.” |
| `subtract_balloons` | “Hay {a} globos en la mesa. Se pinchan {b}. ¿Cuántos globos quedan?” | “La pregunta pide los globos que quedan.” | “SE PINCHAN indica que algunos globos ya no están.” | “Si algunos globos ya no están, restá: {a} − {b} = ___.” |
| `subtract_cookies` | “Hay {a} galletitas en el plato. Comés {b}. ¿Cuántas galletitas quedan?” | “La pregunta pide cuántas galletitas quedan en el plato.” | “COMER quita galletitas del plato.” | “Comer quita galletitas del plato: {a} − {b} = ___.” |
| `subtract_blocks` | “Tenés {a} bloques tirados en el piso. Guardás {b} en una caja. ¿Cuántos bloques te faltan guardar?” | “La pregunta pide los bloques que todavía faltan guardar.” | “GUARDAR algunos bloques deja otros sin guardar.” | “Los que guardaste se sacan de los que había en el piso: {a} − {b} = ___.” |

## 7. Comprensión funcional — Ubicación personal

**Clave propuesta:** `comprehension.functional_1.personal_location`
**Tooltip:** “Reconocer ciudad, provincia y país de residencia.”

### 7.1 Perfil privado y resguardo

Se agregan al perfil privado los campos `city`, `province` y `country`. Los tres se guardan con la misma protección que los datos de identidad y nacimiento.

- Admin muestra campos “Ciudad”, “Provincia” y “País”.
- La habilidad sólo puede generar una misión cuando los tres campos tienen valor.
- Los valores reales no se incluyen en este repositorio, fixtures, documentación, reportes públicos ni importaciones sanitizadas de STG. Se conservan en el perfil y en el detalle protegido de las misiones, igual que los datos personales ya existentes.
- Los ejemplos de esta tabla usan `{ciudad}`, `{provincia}` y `{pais}` como marcadores; no son datos de una persona real.

### 7.2 Variantes y ayudas

| Variante | Pregunta | Ayuda 1 | Ayuda 2 | Ayuda 3 |
| --- | --- | --- | --- | --- |
| `location_city_ask_1` | “¿En qué ciudad vivís?” | “La pregunta pide el nombre de tu ciudad.” | Según la respuesta anterior; ver §7.3. | “Tu ciudad es {ciudad}. Escribila.” |
| `location_city_ask_2` | “¿Cuál es la ciudad donde vivís?” | “La pregunta pide el nombre de tu ciudad.” | Según la respuesta anterior; ver §7.3. | “Tu ciudad es {ciudad}. Escribila.” |
| `location_province_ask_1` | “¿En qué provincia vivís?” | “La pregunta pide el nombre de tu provincia.” | Según la respuesta anterior; ver §7.3. | “Tu provincia es {provincia}. Escribila.” |
| `location_province_ask_2` | “¿Cuál es la provincia donde vivís?” | “La pregunta pide el nombre de tu provincia.” | Según la respuesta anterior; ver §7.3. | “Tu provincia es {provincia}. Escribila.” |
| `location_country_ask_1` | “¿En qué país vivís?” | “La pregunta pide el nombre de tu país.” | Según la respuesta anterior; ver §7.3. | “Tu país es {pais}. Escribilo.” |
| `location_country_ask_2` | “¿Cuál es el país donde vivís?” | “La pregunta pide el nombre de tu país.” | Según la respuesta anterior; ver §7.3. | “Tu país es {pais}. Escribilo.” |
| `location_city_to_province` | “¿En qué provincia se encuentra {ciudad}?” | “La pregunta pide el nombre de una provincia.” | Según la respuesta anterior; ver §7.3. | “{ciudad} está en la provincia de {provincia}. Escribí {provincia}.” |
| `location_province_to_country` | “¿En qué país se encuentra {provincia}?” | “La pregunta pide el nombre de un país.” | Según la respuesta anterior; ver §7.3. | “{provincia} está en {pais}. Escribí {pais}.” |
| `location_city_to_country` | “¿En qué país se encuentra {ciudad}?” | “La pregunta pide el nombre de un país.” | Según la respuesta anterior; ver §7.3. | “{ciudad} está en {pais}. Escribí {pais}.” |

### 7.3 Ayuda 2 según la respuesta escrita

La segunda ayuda no será un texto fijo: se construye después de analizar la respuesta equivocada y utiliza los datos privados configurados. Así corrige una confusión concreta sin asumir que cualquier error es una confusión entre los tres lugares.

Antes de esta etapa se conserva el comportamiento existente:

1. Si la respuesta coincide con la respuesta esperada tras normalizar mayúsculas, tildes y espacios, la misión se resuelve; no se muestra ayuda.
2. Si es una respuesta textualmente cercana, se muestra el apoyo ortográfico actual. No se habilita la siguiente ayuda de comprensión.
3. Sólo una respuesta semánticamente incorrecta, después de pedir la ayuda 1, habilita esta ayuda 2.

| Lo que pide la pregunta | Si respondió otra categoría personal | Si respondió otra cosa |
| --- | --- | --- |
| Ciudad | Si escribió `{provincia}`: “{provincia} es la provincia. Te pregunta la ciudad: recordá cómo se llama la ciudad donde está tu casa.” Si escribió `{pais}`: “{pais} es el país. Te pregunta la ciudad: recordá cómo se llama la ciudad donde está tu casa.” | “Recordá cómo se llama la ciudad donde está tu casa.” |
| Provincia | Si escribió `{ciudad}`: “{ciudad} es la ciudad. Te pregunta la provincia donde está esa ciudad.” Si escribió `{pais}`: “{pais} es el país. Te pregunta la provincia donde está tu ciudad.” | “Recordá cómo se llama la provincia donde está tu ciudad.” |
| País | Si escribió `{ciudad}`: “{ciudad} es la ciudad. Te pregunta el país donde vivís.” Si escribió `{provincia}`: “{provincia} es una provincia. Pensá: vivís en la provincia de {provincia}, que es una de las provincias de tu país.” | “Pensá: vivís en la provincia de {provincia}, que es una de las provincias de tu país.” |

Para las preguntas de relación, se aplica la fila según el dato solicitado: `location_city_to_province` usa **Provincia**; `location_province_to_country` y `location_city_to_country` usan **País**. Si la respuesta no coincide, tras normalizar, con ciudad/provincia/país configurados, se usa la última columna.

## 8. Comprensión — Información explícita en una oración

**Nivel propuesto:** `comprehension.explicit_information_1` — **Información explícita**
**Tooltip:** “Encontrar información escrita de forma literal en una oración breve.”

**Clave propuesta:** `comprehension.explicit_information_1.one_sentence_literal`
**Tooltip de habilidad:** “Responder usando un dato que aparece en una oración.”

Cada misión tiene una sola oración, pregunta por un único dato y no requiere inferir ni combinar información.

| Variante | Pregunta | Ayuda 1 | Ayuda 2 | Ayuda 3 |
| --- | --- | --- | --- | --- |
| `explicit_color_balloon` | “Emma tiene un globo rojo. ¿De qué color es el globo?” | “La pregunta pide el color del globo.” | “Buscá la palabra que dice cómo es el globo.” | “La oración dice ‘un globo rojo’. El color es rojo.” |
| `explicit_when_doctor` | “Matías tiene turno con la doctora el miércoles. ¿Cuándo tiene turno Matías con la doctora?” | “La pregunta pide saber cuándo tiene turno Matías.” | “Buscá la palabra que dice el día del turno.” | “La oración dice ‘el miércoles’. Matías tiene turno el miércoles.” |
| `explicit_when_party` | “La fiesta de Ana es el sábado. ¿Cuándo es la fiesta de Ana?” | “La pregunta pide saber cuándo es la fiesta.” | “Buscá la palabra que dice el día de la fiesta.” | “La oración dice ‘el sábado’. La fiesta es el sábado.” |
| `explicit_owner_ball` | “Tomás tiene una pelota. ¿Quién tiene la pelota?” | “La pregunta pide el nombre de la persona que tiene la pelota.” | “Buscá quién aparece junto a la pelota.” | “La oración dice ‘Tomás tiene una pelota’. La tiene Tomás.” |
| `explicit_owner_book` | “Lucía tiene un libro. ¿Quién tiene el libro?” | “La pregunta pide el nombre de la persona que tiene el libro.” | “Buscá quién aparece junto al libro.” | “La oración dice ‘Lucía tiene un libro’. Lo tiene Lucía.” |
| `explicit_location_cup` | “La taza está en la mesa. ¿Dónde está la taza?” | “La pregunta pide el lugar de la taza.” | “Buscá la palabra que dice dónde está.” | “La oración dice ‘en la mesa’. La taza está en la mesa.” |
| `explicit_location_ball` | “La pelota está debajo de la silla. ¿Dónde está la pelota?” | “La pregunta pide el lugar de la pelota.” | “Buscá las palabras que dicen dónde está.” | “La oración dice ‘debajo de la silla’. La pelota está debajo de la silla.” |
| `explicit_quantity_cats` | “Hay 3 gatos en el patio. ¿Cuántos gatos hay?” | “La pregunta pide una cantidad de gatos.” | “Buscá el número que acompaña a la palabra gatos.” | “La oración dice ‘Hay 3 gatos’. Escribí 3.” |
| `explicit_quantity_pencils` | “Hay 4 lápices en la caja. ¿Cuántos lápices hay?” | “La pregunta pide una cantidad de lápices.” | “Buscá el número que acompaña a la palabra lápices.” | “La oración dice ‘Hay 4 lápices’. Escribí 4.” |
| `explicit_action_nina` | “Nina dibuja una flor. ¿Qué dibuja Nina?” | “La pregunta pide qué está dibujando Nina.” | “Buscá la palabra que aparece después de ‘dibuja’.” | “La oración dice ‘dibuja una flor’. Nina dibuja una flor.” |
| `explicit_action_mateo` | “Mateo come una manzana. ¿Qué come Mateo?” | “La pregunta pide qué está comiendo Mateo.” | “Buscá la palabra que aparece después de ‘come’.” | “La oración dice ‘come una manzana’. Mateo come una manzana.” |
| `explicit_object_dog` | “El perro duerme en su cama. ¿Dónde duerme el perro?” | “La pregunta pide el lugar donde duerme el perro.” | “Buscá las palabras que aparecen después de ‘duerme’.” | “La oración dice ‘duerme en su cama’. El perro duerme en su cama.” |

Los nombres de personajes son ficticios y no pertenecen al perfil de la persona usuaria.

## 9. Cambios técnicos previstos

1. **Catálogo de habilidades.** Agregar las claves de niveles y habilidades al cliente, Admin, API y catálogo de métricas; preservar compatibilidad con configuraciones anteriores.
2. **Contenido pedagógico.** Mantener prompts, valores dinámicos y las tres ayudas de cada variante que las utiliza en una única fuente de contenido editable. Ninguna ayuda requerida debe quedar sin definición; el auto-test debe fallar si sucede. Ubicación personal requiere que la ayuda 2 se genere después de normalizar la respuesta equivocada, para aplicar la tabla de §7.3.
3. **Ayudas en Matemática.** Permitir `HelpSteps` sólo para Situaciones problemáticas, sin cambiar la validación numérica argentina ni las habilidades matemáticas existentes. La ayuda se habilita tras cada respuesta numérica incorrecta, respetando la progresión actual.
4. **Perfil privado.** Migración Alembic que agrega `city`, `province` y `country` a `device_mission_profiles`; DTO de RemoteConfig y configuración local extensible; comparación de perfiles actualizada.
5. **Privacidad de telemetría.** Las preguntas y respuestas de Ubicación personal se guardarán como datos sensibles en el pipeline privado existente, igual que las de Identidad. El acceso de Admin seguirá sujeto a la sesión protegida; esos datos no se incluirán en reportes agregados, documentación, logs auxiliares ni en la importación sanitizada PROD → STG.
6. **Métricas.** Mantener por misión: primer intento, reintentos, intentos promedio, máximo de ayuda y variante. El panel debe mostrar ayudas también en los nuevos niveles matemáticos que las usen. No etiquetar los errores como “de comprensión” en Matemática: en UI y métricas se denominarán “Ayuda de la misión”.
7. **Rotación.** Las habilidades nuevas participan de la rotación global existente, sin repetir una habilidad antes de completar el ciclo de las habilitadas.

## 10. Estrategia de implementación y validación

1. Aprobar o ajustar este catálogo, especialmente ayudas, rangos y revelación de Ubicación personal.
2. Crear la especificación funcional/técnica de la iteración a partir de las decisiones aprobadas.
3. Implementar primero el soporte técnico común: catálogo ampliado, ayudas de Matemática y protección de telemetría privada.
4. Implementar las habilidades con tests unitarios de generación, respuestas, ayudas, rotación, RemoteConfig, Admin y métricas.
5. Aplicar y validar la migración únicamente en STG.
6. Ejecutar tests de servidor, build y self-test del cliente.
7. Validar manualmente en Guardian TEST contra STG: perfil incompleto/completo; una misión de cada habilidad; tres niveles de ayuda; conservación protegida de respuestas de ubicación; exclusión de esas respuestas de la importación sanitizada; métricas por variante; rotación; funcionamiento sin API.
8. Publicar una release STG con sufijo de feature y validar actualización/rollback si la modificación del cliente lo requiere.
9. Tras aprobación, seguir el flujo obligatorio: merge a `main` → deploy PROD → PC TEST → dispositivo productivo final.

## 11. Criterios de aceptación propuestos

- Cada habilidad nueva puede activarse y pausarse desde Admin.
- Las divisiones generan siempre resultados enteros, positivos y sin resto dentro de su familia.
- Multiplicar por 10, 100 y 1000 conserva la validación numérica actual.
- Los problemas de suma/resta contienen exactamente una transformación de cantidad y las tres ayudas usan los valores de su enunciado.
- Ubicación personal no genera misiones con datos incompletos; sus preguntas y respuestas quedan disponibles sólo en el flujo privado protegido de Admin y se excluyen de logs auxiliares, documentación, reportes públicos y dataset sanitizado de STG.
- Todas las variantes de Información explícita requieren un único dato literal de una sola oración.
- Cada variante de Situaciones problemáticas y Comprensión tiene exactamente tres ayudas revisadas y no genéricas; las operaciones directas no tienen ayudas.
- Las métricas distinguen habilidad, variante, intento y máximo apoyo, sin promoción automática de habilidades.

## 12. Fuera de alcance explícito

- Recomendación automática de qué habilidad activar o pausar.
- Evaluación clínica o diagnóstico.
- Divisiones con resto, decimales o algoritmos de división larga.
- Problemas de dos pasos, con incógnita intermedia, varias operaciones o datos distractores.
- Validación geográfica mediante servicios externos.
- Editor libre de preguntas en Admin.
- Publicación o despliegue a un dispositivo real.
