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

Los pasos 3 a 5 no incrementan el intento, no registran `MissionFailed`, no abren un nivel de ayuda y no se acumulan visualmente junto a las ayudas previas.

## Clasificación

- **Número requerido:** para una misión de Matemática, una entrada que no se puede interpretar como número argentino válido muestra `La respuesta es un NÚMERO.`
- **No-intento:** para Comprensión, una entrada vacía, de caracteres repetidos, una secuencia del teclado o sin vocales y sin forma de palabra muestra `LEÉ la pregunta y PENSÁ qué te está pidiendo. Vos podés.`
- **Feedback contextual:** reconoce días de la semana, meses y estaciones incluso con una falta ortográfica leve. Una categoría equivocada muestra inmediatamente la lámpara. Una opción equivocada de la categoría correcta mantiene el primer error como intento genuino; recién una segunda opción distinta de la misma categoría muestra la lámpara.

El mensaje contextual nombra la respuesta escrita y formula qué dato preciso pide la consigna, sin revelar la respuesta final. Si reconoce una falta ortográfica leve de una opción conocida, usa `Quisiste decir <opción>...` para mostrar la intención detectada.

## Telemetría y privacidad

Cada intervención standalone registra `MissionFeedbackShown` con `feedback_kind` (`no_attempt`, `number_required` o `contextual`), el texto que se mostró y los metadatos no sensibles de la misión. En el caso contextual, ese texto sólo puede usar el vocabulario cerrado de días, meses y estaciones; no registra una respuesta libre. El detalle de conversación de Métricas muestra estos eventos en orden, sin contarlos como intento ni como ayuda de niveles 1 a 3.

## Fuera de alcance

- Cambiar los textos o la estructura de las ayudas 1, 2 y 3.
- Selección automática de habilidades según desempeño.
- Nuevas preguntas, variantes o cambios de perfil.
