# Guardian — FINAL Functional Specification
## Mission System v2 + Comprensión funcional — Nivel 1

## 1. Objetivo

Evolucionar el sistema de misiones de Guardian para:

- presentar una única misión por disparo;
- permitir combinar distintos tipos de aprendizaje;
- incorporar Comprensión funcional;
- permitir activar o desactivar habilidades concretas desde Admin;
- evitar repeticiones innecesarias;
- registrar el rendimiento por habilidad;
- preparar Guardian para futuras decisiones basadas en qué habilidades ya están consolidadas.

Esta iteración no implementa todavía selección automática según desempeño.

---

# 2. Jerarquía definitiva

Todas las misiones se organizan como:

**Categoría → Nivel → Habilidad → Variante**

## Categoría

Área general.

Inicialmente:

- Matemática
- Comprensión

## Nivel

Etapa de complejidad dentro de una categoría.

Inicialmente:

### Matemática
**Nivel 1 — Operaciones básicas**

### Comprensión
**Nivel 1 — Comprensión funcional**

## Habilidad

Unidad configurable y medible.

La habilidad:

- se puede activar/desactivar;
- participa de la rotación;
- acumula estadísticas;
- representa un conocimiento suficientemente amplio como para contener distintas formulaciones.

## Variante

Pregunta concreta o forma concreta de evaluar una habilidad.

Las variantes:

- forman parte del código;
- no se configuran individualmente desde Admin;
- pueden cambiar de formulación sin cambiar la habilidad evaluada.

---

# 3. Matemática

## Categoría

**Matemática**

## Nivel 1

**Operaciones básicas**

## Habilidades iniciales

- Sumas
- Restas
- Multiplicaciones

Cada una puede habilitarse o deshabilitarse independientemente.

Ejemplo:

- Sumas OFF
- Restas ON
- Multiplicaciones ON

Guardian deberá entonces dejar de presentar sumas y concentrar las misiones matemáticas en las habilidades activas.

Los generadores y rangos matemáticos que ya funcionan actualmente deben conservar su comportamiento salvo cambios expresamente requeridos en esta spec.

---

# 4. Comprensión

## Categoría

**Comprensión**

## Nivel 1

**Comprensión funcional**

Objetivo:

Practicar la comprensión de preguntas y consignas sencillas que tengan utilidad concreta en la vida cotidiana.

No busca todavía comprensión narrativa o escolar de textos.

Busca que el usuario pueda:

- comprender distintas formas de solicitar la misma información;
- identificar qué dato corresponde responder;
- reconocer información personal básica;
- diferenciar conceptos de fecha;
- comprender relaciones temporales;
- manejar conceptos básicos de calendario;
- reconocer las estaciones del año.

---

# 5. Habilidades de Comprensión Nivel 1

Las habilidades configurables son exactamente:

1. Identidad
2. Edad y nacimiento
3. Fecha actual
4. Relaciones temporales
5. Calendario
6. Estaciones

Las preguntas particulares dentro de estas habilidades son variantes internas.

---

# 6. Habilidad: Identidad

Debe incluir variantes equivalentes a:

### Nombre

- ¿Cuál es tu nombre?
- ¿Cómo te llamás?
- Nombre:

Debe aceptar conceptualmente:

- nombre habitual;
- primer nombre;
- nombre + apellido;
- nombre completo.

### Apellido

- ¿Cuál es tu apellido?
- Apellido:

Debe aceptar el apellido configurado.

### Nombre y apellido

- ¿Cuál es tu nombre y apellido?
- Nombre y apellido:

Debe aceptar:

- primer nombre + apellido;
- nombre completo.

### Nombre completo

- ¿Cuál es tu nombre completo?

Debe aceptar únicamente el nombre completo configurado.

Estas preguntas pertenecen todas a la misma habilidad:

**Identidad**

---

# 7. Habilidad: Edad y nacimiento

Debe incluir:

### Edad

- ¿Cuántos años tenés?
- ¿Qué edad tenés?
- Edad:

La edad se calcula a partir de la fecha de nacimiento.

No debe quedar fija.

### Año de nacimiento

- ¿En qué año naciste?
- Año de nacimiento:

### Cumpleaños

- ¿Cuándo es tu cumpleaños?

Debe aceptar:

- día y mes correctos;
- o fecha completa correcta.

Todas pertenecen a:

**Edad y nacimiento**

---

# 8. Habilidad: Fecha actual

Objetivo especial:

Aprender a diferenciar:

- día de la semana;
- día del mes;
- mes;
- año;
- fecha completa.

Variantes:

### Año

- ¿En qué año estamos?
- ¿Qué año es?

### Mes

- ¿En qué mes estamos?
- ¿Qué mes es?

### Día de la semana

- ¿Qué día de la semana es hoy?

### Día del mes

- ¿Qué día del mes es hoy?

### Fecha completa

- ¿Qué fecha es hoy?

Ejemplo de diferencias que Guardian debe reforzar:

- sábado;
- 22;
- agosto;
- 2026;
- 22 de agosto de 2026.

Todas pertenecen a:

**Fecha actual**

---

# 9. Habilidad: Relaciones temporales

Variantes:

- ¿Qué día de la semana es mañana?
- ¿Qué día de la semana fue ayer?
- ¿Cuál es el mes que viene?
- ¿Qué mes viene después de este?
- ¿Cuál fue el mes pasado?

Debe manejar correctamente cambios de ciclo:

- domingo → lunes;
- lunes → domingo para ayer;
- diciembre → enero;
- enero → diciembre para mes anterior.

Todas pertenecen a:

**Relaciones temporales**

---

# 10. Habilidad: Calendario

Variantes básicas:

- ¿Cuántos días tiene una semana?
- ¿Cuántos meses tiene un año?

También debe incluir preguntas variables sobre secuencia.

Ejemplos:

- ¿Qué día viene después del lunes?
- ¿Qué día viene antes del viernes?
- ¿Qué mes viene después de enero?
- ¿Qué mes viene antes de diciembre?

Deben poder utilizarse diferentes días y meses.

No se considera cada combinación una habilidad diferente.

Todas pertenecen a:

**Calendario**

---

# 11. Habilidad: Estaciones

Debe incluir:

### Clima

- ¿Cuál es la estación del año en la que hace mucho frío?
- ¿Cuál es la estación del año en la que hace mucho calor?

### Características

- ¿En qué estación se caen muchas hojas de los árboles?
- ¿En qué estación suelen crecer muchas flores?

### Secuencia

Preguntas del tipo:

- ¿Qué estación viene después del invierno?
- ¿Qué estación viene después del verano?

Debe poder contemplar las cuatro transiciones.

Todas pertenecen a:

**Estaciones**

---

# 12. Una misión por disparo

Cada bloqueo periódico debe presentar exactamente:

**una misión**

Flujo:

1. Guardian bloquea.
2. Presenta una pregunta.
3. Si la respuesta es incorrecta, se reintenta esa misma misión.
4. Cuando la respuesta es correcta, se desbloquea.

No presentar tres ejercicios consecutivos en un mismo disparo.

---

# 13. Rotación global de habilidades

La rotación se realiza entre **todas las habilidades activas**, independientemente de su categoría.

Ejemplo:

Habilidades activas:

- Restas
- Identidad
- Fecha actual
- Estaciones

El ciclo tiene cuatro habilidades.

Guardian debe utilizar las cuatro antes de repetir cualquiera.

Secuencia válida:

1. Identidad
2. Restas
3. Estaciones
4. Fecha actual
5. Restas
6. Fecha actual
7. Identidad
8. Estaciones

Los primeros cuatro completan un ciclo.

El quinto inicia otro.

---

# 14. No repetición dentro del ciclo

Si una habilidad ya apareció, ninguna de sus otras variantes puede aparecer mientras existan habilidades activas todavía no utilizadas.

Ejemplo:

Si ya apareció:

> ¿Cómo te llamás?

la habilidad Identidad queda usada dentro de ese ciclo.

No puede aparecer después:

> Nombre y apellido:

hasta iniciar un nuevo ciclo.

---

# 15. Cambio de día

Al comenzar un nuevo día local:

- el ciclo se reinicia;
- todas las habilidades activas vuelven a estar disponibles.

No continuar al día siguiente un ciclo iniciado el día anterior.

---

# 16. Todas las habilidades ya utilizadas

Cuando todas las habilidades habilitadas ya aparecieron:

- el ciclo termina;
- comienza inmediatamente uno nuevo;
- todas vuelven a quedar disponibles.

Por lo tanto, una habilidad puede repetirse durante un mismo día, pero sólo después de completar el ciclo de habilidades activas.

---

# 17. Cero habilidades activas

Si no existe ninguna habilidad habilitada:

- Guardian no debe presentar una misión educativa;
- no debe bloquear al usuario con una pantalla imposible de resolver;
- debe volver a evaluar la configuración en el próximo disparo.

No utilizar una misión fallback oculta.

---

# 18. Variantes

La elección de variante es interna.

El Admin no permite:

- activar una pregunta concreta;
- desactivar una pregunta concreta;
- editar el texto;
- editar respuestas válidas.

Guardian debe intentar evitar usar exactamente la misma variante en apariciones consecutivas de una habilidad cuando existan alternativas.

Esto es deseable, pero la regla estricta de rotación se aplica a la habilidad.

---

# 19. Admin — pantalla de configuración

La configuración de misiones debe trasladarse o ubicarse en una pantalla/sección específica del Admin.

No acumular todos los controles en la vista principal de dispositivos.

Debe ser compacta.

Ejemplo conceptual:

### Matemática

☑ Nivel 1 — Operaciones básicas ⓘ

- ☑ Sumas ⓘ
- ☑ Restas ⓘ
- ☑ Multiplicaciones ⓘ

### Comprensión

— Nivel 1 — Comprensión funcional ⓘ

- ☑ Identidad ⓘ
- ☐ Edad y nacimiento ⓘ
- ☑ Fecha actual ⓘ
- ☐ Relaciones temporales ⓘ
- ☐ Calendario ⓘ
- ☐ Estaciones ⓘ

---

# 20. Checkbox del nivel

El nivel actúa como checkbox padre.

### Todas las habilidades seleccionadas

Nivel:

**checked**

### Algunas seleccionadas

Nivel:

**indeterminate**

### Ninguna seleccionada

Nivel:

**unchecked**

Marcar el nivel:

- activa todas sus habilidades.

Desmarcarlo:

- desactiva todas sus habilidades.

Luego deben poder activarse individualmente las habilidades deseadas.

---

# 21. Tooltips

Los tooltips son requisito funcional.

Cada:

- nivel;
- habilidad;

debe tener un ícono de información.

Al interactuar con él aparece un tooltip breve.

No mostrar permanentemente descripciones debajo de cada habilidad.

Ejemplos:

### Comprensión funcional

> Preguntas cotidianas sobre identidad, edad, fechas, calendario y estaciones.

### Identidad

> Comprender distintas formas de solicitar información básica de identificación.

### Fecha actual

> Diferenciar día, mes, año y fecha actual.

### Relaciones temporales

> Comprender referencias como ayer, mañana, mes anterior y mes siguiente.

### Calendario

> Reconocer días, meses y su secuencia.

### Estaciones

> Reconocer estaciones, características y secuencia.

### Restas

> Resolver ejercicios básicos de resta.

---

# 22. Perfil privado

Los datos personales utilizados por las preguntas NO forman parte del catálogo público de misiones.

Debe existir un perfil privado configurable que contenga como mínimo:

- nombre habitual;
- primer nombre;
- segundo nombre;
- apellido;
- fecha de nacimiento;
- zona horaria si Guardian no dispone ya de ella por dispositivo.

La información concreta:

- no debe estar hardcodeada;
- no debe estar en GitHub;
- no debe estar en documentación pública;
- no debe estar en fixtures públicos;
- no debe aparecer en logs o telemetría.

El perfil deberá poder editarse desde Admin.

---

# 23. Catálogo público vs perfil privado

El catálogo puede contener públicamente:

> ¿Cuál es tu nombre?

y definir:

> esta pregunta acepta preferredName, firstName, firstName+lastName o fullName.

Pero no debe contener los valores concretos de esos campos.

De igual manera:

> ¿Cuántos años tenés?

puede estar en el catálogo.

La respuesta debe calcularse desde `birthDate`.

---

# 24. Respuestas no personales

Las respuestas de conocimiento general sí pueden formar parte del código.

Ejemplos:

- siete días;
- doce meses;
- invierno;
- verano;
- secuencia de días;
- secuencia de estaciones.

---

# 25. Privacidad

Guardian no debe registrar el texto ingresado por el usuario en las respuestas de comprensión.

Debe registrar:

- qué habilidad fue evaluada;
- qué variante;
- si fue correcta;
- cuántos intentos necesitó.

No el contenido textual escrito.

---

# 26. Rendimiento futuro

La estructura debe permitir analizar por habilidad:

- cantidad de veces presentada;
- resoluciones correctas;
- incorrectas;
- reintentos;
- porcentaje al primer intento;
- evolución temporal.

Ejemplo futuro:

### Matemática

- Sumas: consolidadas.
- Restas: dificultad.
- Multiplicaciones: en aprendizaje.

### Comprensión

- Identidad: consolidada.
- Fecha actual: dificultad.
- Calendario: en aprendizaje.

Esta iteración no decide automáticamente qué activar/desactivar.

---

# 27. Comprensión futura

Fuera de alcance actual:

## Nivel 2 — Información explícita

Ejemplo:

> Sofía tiene una bicicleta roja.
> ¿De qué color es la bicicleta?

## Nivel 3 — Relación de información

Ejemplo:

> Juan tiene una pelota. Pedro tiene un autito.
> ¿Quién tiene la pelota?

La arquitectura actual debe permitir incorporarlos posteriormente.

---

# 28. Fuera de alcance

No implementar ahora:

- LLM;
- generación automática de preguntas;
- validación con IA;
- adaptación automática;
- promoción automática de nivel;
- editor completo de preguntas;
- configuración individual de variantes;
- Nivel 2;
- Nivel 3;
- DNI;
- domicilio;
- teléfono;
- email;
- otros datos personales sensibles.

---

# 29. Criterios funcionales de aceptación

La iteración se acepta si:

1. Existe la jerarquía Categoría → Nivel → Habilidad → Variante.
2. Matemática y Comprensión siguen el mismo modelo.
3. Sumas, Restas y Multiplicaciones son configurables individualmente.
4. Existe Comprensión funcional Nivel 1.
5. Sus seis habilidades son configurables individualmente.
6. El nivel permite seleccionar/deseleccionar todas.
7. El nivel refleja estado checked/indeterminate/unchecked.
8. Cada nivel y habilidad posee tooltip.
9. Sólo aparece una misión por disparo.
10. Los errores reintentan la misma misión.
11. La rotación combina categorías.
12. No se repite habilidad hasta completar el ciclo.
13. Un nuevo día inicia un ciclo nuevo.
14. Al completar todas las habilidades comienza otro ciclo.
15. Cero habilidades activas no produce bloqueo.
16. Las preguntas dinámicas usan fecha local.
17. Las variantes no se gestionan desde Admin.
18. El perfil personal es privado y editable.
19. Ningún dato personal real queda en el repo público.
20. No se registra el texto de respuestas personales.
21. El sistema queda preparado para analizar rendimiento por habilidad.
