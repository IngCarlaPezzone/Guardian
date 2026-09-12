# Iteración — feedback de respuesta antes de las ayudas

## Objetivo

Evitar que una respuesta sin intención pedagógica o el tanteo de opciones conocidas avance las ayudas progresivas. La iteración conserva sin cambios los tres niveles de ayuda existentes.

## Flujo de prioridad

1. Respuesta correcta: resuelve la misión.
2. Respuesta cercana a la correcta: usa el corrector ortográfico existente.
3. Respuesta inválida de Matemática: muestra el feedback de número.
4. No-intento en Comprensión: muestra el feedback con mano de stop.
5. Confusión de días, meses o estaciones: muestra feedback contextual con lámpara.
6. Respuesta incorrecta genuina: conserva la rutina y progresión de ayudas actual.

Los pasos 3 a 5 registran la respuesta y cuentan como intento para reflejar la conversación completa. No registran un fallo semántico para la progresión, no abren un nivel de ayuda y no se acumulan visualmente junto a las ayudas previas.

## Clasificación

- **Número requerido:** para una misión de Matemática, una entrada que no se puede interpretar como número argentino válido muestra `La respuesta es un NÚMERO.`
- **No-intento:** para Comprensión, una entrada vacía, de caracteres repetidos, una secuencia del teclado, sin vocales y sin forma de palabra, o la repetición de una respuesta ya enviada muestra `LEÉ la pregunta y PENSÁ qué te está pidiendo. Vos podés.` Una misma opción de calendario escrita luego con una falta ortográfica leve también es repetición.
- **Feedback contextual:** reconoce días de la semana, meses y estaciones incluso con una falta ortográfica leve. Una categoría equivocada muestra inmediatamente la lámpara. Una opción equivocada de la categoría correcta mantiene el primer error como intento genuino; recién una segunda opción distinta de la misma categoría muestra la lámpara.

El mensaje contextual nombra la respuesta escrita, identifica si es un día de la semana, un mes del año o una estación del año, y formula qué dato preciso pide la consigna, sin revelar la respuesta final. Si reconoce una falta ortográfica leve de una opción conocida, usa `Quisiste decir <opción>...` para mostrar la intención detectada. También puede explicar esa categoría cuando la pregunta pide edad, año, fecha o una ubicación.

## Telemetría y privacidad

Cada intervención standalone registra la respuesta como `MissionFailed` con un motivo pedagógico y registra `MissionFeedbackShown` con `feedback_kind` (`no_attempt`, `number_required` o `contextual`), el texto que se mostró y los metadatos no sensibles de la misión. En el caso contextual, ese texto sólo puede usar el vocabulario cerrado de días, meses y estaciones; no registra una respuesta libre. El detalle de conversación de Métricas muestra ambos eventos en orden; cuenta la respuesta como intento, pero no como ayuda de niveles 1 a 3. En el resumen de Comprensión, `Ayuda personalizada` y `No intento` se muestran dentro de `Apoyo de comprensión` y pueden superponerse con las ayudas de nivel.

## Alcance publicado

La versión `0.4.10` incorpora esta iteración en Cliente, Server/Admin y Métricas. Sus
íconos (`stop`, `just_number` y lámpara) se muestran en el mensaje de misión y en el
detalle cronológico. La POC de misión visual no forma parte de esta versión.

La validación de la feature se realiza con el flujo aislado de STG y, tras la promoción,
de forma manual en `PC TEST` de PROD. Que la release aparezca disponible no actualiza
ningún dispositivo: Administración decide y ejecuta cada actualización.

## Fuera de alcance

- Cambiar los textos o la estructura de las ayudas 1, 2 y 3.
- Selección automática de habilidades según desempeño.
- Nuevas preguntas, variantes o cambios de perfil.
