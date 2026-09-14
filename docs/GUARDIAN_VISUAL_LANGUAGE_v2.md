# Guardian Visual Language

Versión: 2.0  
Alcance: lenguaje visual reusable para microvideos educativos de Guardian.  
Objetivo: que números, signos, íconos, composición y movimiento mantengan una identidad consistente entre videos, independientemente del contenido pedagógico de cada story.

---

## 1. Principio del sistema

Guardian debe sentirse como un mismo universo visual en todos sus microvideos.

Los elementos principales no se tratan como texto plano ni como decoraciones aisladas: se tratan como **objetos visuales reutilizables** que pueden moverse, agruparse, separarse, iluminarse o transformarse conservando siempre su identidad.

La estética general debe ser:

- brillante;
- amable;
- volumétrica;
- colorida;
- moderna;
- de lectura inmediata;
- pensada para un niño de alrededor de 12 años, evitando una estética demasiado infantil o de preescolar.

La energía puede recordar al motion graphics educativo contemporáneo, pero Guardian debe conservar una identidad propia y no copiar personajes, caras, bloques, composiciones, paletas o recursos distintivos de otras franquicias.

---

## 2. Familias de assets

El lenguaje visual se divide en tres familias:

1. **Guardian Numbers**: dígitos `0` a `9`.
2. **Guardian Signs**: signos matemáticos y de pregunta.
3. **Guardian Narrative Icons**: íconos de apoyo narrativo, limitados en video a `hint` y `think`.

Cada asset debe existir como archivo independiente. No se deben entregar planchas, sprites ni una única imagen con varios elementos cuando esos elementos vayan a animarse por separado.

---

## 3. Formato canónico de los assets

Tomar como referencia de formato los íconos Guardian existentes, como `just_number.png`.

### Archivo

- PNG con canal alfa real.
- Fondo completamente transparente.
- Un solo elemento visual por archivo.
- Sin marco, tarjeta, fondo de color ni sombra recortada por el borde del canvas.

### Canvas

- Canvas cuadrado.
- Mantener una dimensión canónica común para toda la familia. Para compatibilidad con el set actual, usar **1254 × 1254 px** como master de exportación.
- El elemento debe quedar centrado visualmente, no necesariamente geométricamente si la forma del carácter requiere compensación óptica.
- Dejar margen transparente suficiente para glow, squash/stretch y pequeños movimientos sin recortar el asset.
- Evitar márgenes exagerados: el carácter debe ocupar aproximadamente 70–82 % de la altura útil del canvas.

### Consistencia

Todos los assets de una misma familia deben mantener:

- escala visual equivalente;
- grosor comparable;
- misma lógica de luces y sombras;
- profundidad 3D similar;
- bordes suaves;
- margen transparente compatible;
- orientación frontal o con perspectiva mínima.

---

## 4. Guardian Numbers

### Rol

Los números son protagonistas visuales. No son texto de interfaz.

Cada dígito debe poder funcionar:

- aislado;
- dentro de una operación;
- como parte de un resultado;
- agrupado con otros dígitos;
- en movimiento independiente.

### Forma

- Gruesa y redondeada.
- Muy legible incluso a tamaño pequeño.
- Proporciones compactas y robustas.
- Contornos suaves.
- Sin esquinas agresivas.
- Los huecos internos de `0`, `6`, `8` y `9` deben ser amplios y claros.
- Evitar una tipografía rígida o corporativa: la forma puede tener una leve personalidad gráfica, pero sin convertirse en personaje con cara o extremidades.

### Volumen

Cada número debe sentirse como una pieza blanda y brillante, similar a plástico o goma pulida:

- cara frontal luminosa;
- gradiente suave;
- brillo especular principal en la zona superior izquierda;
- borde/profundidad más oscuro hacia la zona inferior y lateral;
- pequeña sombra o separación de volumen;
- acabado limpio, sin textura rugosa ni metalizada.

### Personalidad

Los números **no llevan ojos, boca, brazos ni piernas por defecto**.

La personalidad se obtiene mediante:

- color;
- volumen;
- brillo;
- escala;
- timing;
- movimiento.

---

## 5. Paleta Guardian Numbers

Un mismo dígito conserva siempre la misma identidad de color en todos los videos.

| Dígito | Color principal | Hex guía | Profundidad / borde |
|---|---|---|---|
| 0 | Turquesa brillante | `#28D7D2` | `#078D99` |
| 1 | Rojo coral | `#FF4E5D` | `#B61F3A` |
| 2 | Naranja | `#FFA62B` | `#D65B00` |
| 3 | Amarillo solar | `#FFD83D` | `#E08A00` |
| 4 | Verde lima | `#7ED957` | `#2E9B42` |
| 5 | Celeste | `#44B8FF` | `#1368C4` |
| 6 | Azul intenso | `#397BFF` | `#2336B8` |
| 7 | Violeta | `#9D5CFF` | `#6130B6` |
| 8 | Rosa / fucsia | `#FF69B4` | `#B52E7A` |
| 9 | Verde agua | `#45E0A8` | `#168A68` |

Los hex funcionan como anclas de identidad. El render final puede usar gradientes, luces y sombras alrededor de esos tonos.

No recolorear un número para adaptarlo a una escena. Si hace falta contraste, ajustar fondo, glow o iluminación, no la identidad base del dígito.

---

## 6. Guardian Signs

Los signos matemáticos también son assets visuales independientes y deben compartir el mismo lenguaje de volumen, brillo y borde de Guardian Numbers.

### Multiplicación `×`

- Ámbar / naranja cálido.
- Hex guía: `#FFB629`.
- Grueso, redondeado y de lectura instantánea.

### Igual `=`

- Menta brillante.
- Hex guía: `#65E6C4`.
- Barras gruesas y redondeadas.

### Pregunta `?`

- Violeta luminoso.
- Hex guía: `#A970FF`.
- Funciona como señal de curiosidad, anticipación o desafío.

### Futuros signos

`+`, `−`, `÷`, `<`, `>`, `%` u otros deben agregarse siguiendo las mismas reglas de forma, volumen, canvas y nomenclatura. No deben improvisarse con una fuente genérica si ya forman parte del vocabulario Guardian.

---

## 7. Nomenclatura de assets

### Números

```text
guardian-number-0.png
guardian-number-1.png
guardian-number-2.png
guardian-number-3.png
guardian-number-4.png
guardian-number-5.png
guardian-number-6.png
guardian-number-7.png
guardian-number-8.png
guardian-number-9.png
```

### Signos

```text
guardian-sign-multiply.png
guardian-sign-equals.png
guardian-sign-question.png
```

### Íconos narrativos

```text
guardian-icon-hint.png
guardian-icon-think.png
```

Si en el futuro existen versiones SVG/XAML, conservar el mismo nombre base.

---

## 8. Regla de composición matemática

Siempre que un elemento pueda necesitar movimiento independiente, debe existir como objeto independiente en la escena.

Ejemplo conceptual:

`23 × 100`

se compone como seis objetos:

- `2`;
- `3`;
- `×`;
- `1`;
- `0`;
- `0`.

No rasterizar una ecuación completa como una única imagen si luego alguno de sus componentes debe:

- desplazarse;
- resaltarse;
- separarse;
- desaparecer;
- agruparse;
- cambiar de escala;
- transformarse.

La composición puede reagrupar visualmente varios assets, pero la implementación debe conservar su independencia.

---

## 9. Guardian Narrative Icons

En los microvideos educativos se limita deliberadamente el vocabulario narrativo a dos íconos.

### `hint`

Función: **pista, regla, patrón, “mirá esto”**.

Usarlo cuando el video:

- revela una estrategia;
- muestra una relación reutilizable;
- marca dónde conviene prestar atención;
- ofrece una pista sin resolver por completo.

Comportamiento visual:

- aparición breve;
- pop suave;
- glow sutil;
- zona secundaria de la escena;
- no permanece durante todo el video salvo que la narración lo requiera.

### `think`

Función: **pregunta abierta, reflexión, anticipación o desafío mental**.

Usarlo cuando el video:

- deja algo para inferir;
- propone predecir el siguiente paso;
- conecta con una misión posterior;
- invita a pensar antes de responder.

Comportamiento visual:

- entrada con microbounce;
- puede acompañarse del signo `?`;
- puede mantenerse visible durante el hold final.

### Regla

No usar otros íconos Guardian como decoración dentro de videos educativos. `stop`, `spelling`, `write`, etc. pueden pertenecer a otras partes de la aplicación, pero no al vocabulario narrativo base del video salvo que se defina explícitamente una nueva necesidad y se actualice este manual.

---

## 10. Estilo general de video

Guardian debe sentirse como **motion graphics**, no como una presentación de diapositivas.

### Fondo

- Oscuro, nunca negro plano.
- Gradientes en azul profundo, violeta y turquesa.
- Luces difusas y formas abstractas sutiles.
- Partículas discretas.
- Puede reaccionar levemente a eventos importantes, sin competir con el contenido.

### Foco

- Un concepto visual principal por momento.
- Los números y signos pueden ser muy grandes.
- Mantener aire suficiente alrededor para que los movimientos sean legibles.
- Evitar llenar toda la pantalla con elementos simultáneos.

### Texto de apoyo

- Muy poco texto.
- Frases breves, idealmente de 2–6 palabras.
- Tamaño claramente legible desde lejos.
- Blanco o casi blanco con contraste alto.
- Tipografía sans serif moderna y redondeada.
- El texto explica; los números demuestran.

Los números y signos no deben reemplazarse por texto tipográfico cuando existan assets Guardian.

---

## 11. Gramática de movimiento Guardian

El movimiento debe tener intención pedagógica. No se anima “porque queda lindo”.

### Entrada / aparición

Uso: presentar un elemento nuevo.

- 250–450 ms.
- Escala aproximada `0.85 → 1.05 → 1.00`.
- Easing elástico suave.
- Puede combinarse con un pequeño desplazamiento, evitando un slide genérico.

### Highlight

Uso: indicar “mirá este elemento”.

- Glow suave.
- Escala `1.00 → 1.08 → 1.00`.
- 350–600 ms.
- Puede sumar un pulso de luz o partículas mínimas.

### Viaje

Uso: mostrar transferencia, relación, causa o cambio de posición.

- Curva Bézier o trayectoria orgánica.
- Movimiento continuo.
- Pequeña variación de escala para sugerir profundidad.
- Estela o partículas solo si ayudan a entender el movimiento.

### Aterrizaje / acople

Uso: mostrar que un elemento llegó a su nueva función o posición.

- Escala aproximada `1.12–1.18 → 0.96 → 1.00`.
- Glow breve.
- Microrebote.
- 250–400 ms.

### Agrupación / separación

Uso: demostrar partes de un número, operación o patrón.

- El grupo conserva alineación clara.
- Las piezas se desplazan solo lo necesario para hacer visible la relación.
- No dispersar elementos por la pantalla sin motivo.

### Transformación

Uso: mostrar equivalencia o resultado.

- Debe ser legible cuál elemento se transforma en cuál.
- Preferir continuidad de movimiento antes que reemplazo instantáneo.

### Hold

Uso: permitir lectura o reflexión.

- Normalmente 700–1500 ms.
- Puede ser mayor cuando el audio o la pregunta final lo requieran.

---

## 12. Movimiento a evitar

No considerar suficiente como lenguaje principal:

- fades repetidos;
- slides genéricos;
- zoom uniforme de toda la escena;
- reemplazo abrupto de texto;
- rotaciones exageradas;
- rebotes permanentes;
- partículas decorativas excesivas;
- movimiento que no comunica nada.

Una buena story debe poder explicarse parcialmente observando cómo se mueven los objetos, incluso sin escuchar el audio.

---

## 13. Composición y zonas seguras

### Zona principal

Reservada para:

- números;
- signos;
- relaciones visuales;
- resultados;
- transformaciones.

Debe ocupar el foco central y la mayor parte del espacio.

### Zona de apoyo

Reservada a `hint` y `think`.

Guía:

- margen lateral: 4–6 % del ancho;
- margen inferior: 5–8 % del alto;
- nunca superponer la operación principal;
- nunca tocar el borde;
- los íconos pueden tener microanimación, pero no deben atravesar la escena.

`hint` y `think` no necesitan posiciones rígidas universales; la composición puede elegir izquierda o derecha según el balance visual, siempre dentro de la zona secundaria.

---

## 14. Audio y timing

El audio es una fuente de timing, no una capa agregada al final.

Reglas:

- sincronizar eventos visuales con palabras o ideas clave;
- evitar que una animación importante termine antes de que la narración la explique;
- permitir pausas visuales cuando la comprensión lo necesite;
- se puede segmentar o ajustar silencios offline si ayuda a sincronizar, siempre sin alterar perceptiblemente la voz;
- no imponer una duración fija al video si el concepto necesita más o menos tiempo.

El ritmo debe sentirse ágil, pero no apurado.

---

## 15. Estructura narrativa reusable

Una story puede combinar estos bloques según el contenido. No todos son obligatorios.

1. **Presentación**: mostrar el concepto o los elementos protagonistas.
2. **Foco / regla**: señalar el patrón o idea central, potencialmente con `hint`.
3. **Demostración**: hacer que los objetos muestren físicamente la relación.
4. **Consolidación**: repetir o variar el patrón sin reiniciar la estética como una nueva slide.
5. **Cierre / desafío**: resumir o dejar una pregunta, potencialmente con `think`.

Las transiciones entre bloques deben sentirse continuas. Evitar “pantalla nueva = diapositiva nueva”.

---

## 16. Reutilización y creación de nuevos assets

Antes de crear un asset nuevo:

1. comprobar si ya existe un número, signo o ícono que cumpla la función;
2. preferir recomposición y animación sobre nuevos dibujos;
3. crear un nuevo asset solo si introduce una función visual realmente distinta;
4. documentarlo aquí si pasa a formar parte del vocabulario reusable.

Los assets nuevos deben respetar:

- canvas;
- transparencia;
- volumen;
- iluminación;
- profundidad;
- nomenclatura;
- escala visual;
- intención de uso.

---

## 17. Reglas para implementación

1. No recolorear números Guardian de forma arbitraria.
2. No reconstruir con una fuente genérica un número o signo que ya exista como asset Guardian.
3. Mantener cada objeto animable separado en la escena.
4. Mantener la proporción original de los PNG.
5. Aplicar glow, sombras y partículas adicionales con moderación.
6. Priorizar continuidad física y semántica sobre transiciones de presentación.
7. Usar `hint` y `think` por función narrativa, no como decoración.
8. Mantener audio, visual y movimiento sincronizados.
9. Asegurar que el preview permita revisar timing y composición antes de integrar una story.
10. Si una pieza se siente como PowerPoint, rehacer movimiento, continuidad o jerarquía antes de considerarla terminada.

---

## 18. Criterios visuales de aceptación

Antes de aprobar una story, revisar:

- ¿Los números y signos se reconocen como Guardian?
- ¿Cada asset conserva su color y estilo?
- ¿Los objetos que necesitan moverse son independientes?
- ¿La escena se entiende sin depender de texto largo?
- ¿El movimiento explica una relación real?
- ¿Se siente continua, no como una secuencia de slides?
- ¿`hint` o `think` aparecen solo cuando tienen función?
- ¿El fondo acompaña sin distraer?
- ¿Los textos se leen con facilidad?
- ¿Existe tiempo suficiente para comprender el resultado o la pregunta?
- ¿La story podría convivir visualmente con otras Guardian sin parecer de otro producto?

---

## 19. Fuente de verdad

Este documento define el lenguaje visual reusable de Guardian.

Las instrucciones específicas de una story —libreto, ejemplos, números concretos, orden de escenas, capturas requeridas, integración, build o despliegue— deben vivir en el **prompt o especificación propia de esa story**, no en este manual.

Cuando se cree una nueva story:

- reutilizar este lenguaje;
- reutilizar assets existentes;
- describir solo las decisiones particulares del contenido;
- actualizar este manual únicamente si cambia una regla transversal o se agrega un nuevo elemento reusable al sistema.

---

## 20. Aprendizajes de producción de la primera Visual Story

Estos lineamientos se obtuvieron al construir la story de potencias de diez y
deben aplicarse al planificar temas futuros, por ejemplo minutos de una hora o
fracciones.

### Composición

- Empezar con **una composición principal** centrada. Si se presentan varios
  casos relacionados, ordenarlos verticalmente o en una progresión clara; no
  llenar una fila horizontal sólo para reflejar el orden de la narración.
- Dejar aire entre grupos de números y signos. El protagonista no debe competir
  con una regla, un ícono o una decoración.
- Mantener los íconos narrativos dentro de la zona de apoyo, pero suficientemente
  grandes para que se reconozcan a primera vista.
- El cierre debe reducir la escena a una relación que se pueda leer de inmediato;
  no dejar copias, estados intermedios ni trayectorias activas sobre el resultado.

### Movimiento que explica

- Una transformación importante necesita una **fuente estable**, una copia o
  componente identificable que viaja y un destino legible. Evitar sustituir el
  resultado de golpe.
- Separar rutas que podrían confundirse: usar alturas, profundidades o ritmos
  distintos. Por ejemplo, un número completo puede viajar por arriba y sus
  componentes por debajo.
- Las estelas sólo acompañan el viaje: bajar su opacidad y eliminarlas antes del
  hold final.
- No hacer que los objetos esperen en posiciones ambiguas sobre signos u otros
  números. Cada fase debe conservar una lectura limpia incluso si se pausa.

### Audio y ritmo

- La animación comienza cuando la reproducción de audio está efectivamente lista,
  no cuando se solicita abrir el archivo.
- Mapear cada idea hablada a un beat visual, en lugar de distribuir la animación
  por duración uniforme.
- Antes de ralentizar artificialmente una voz, corregir composición y timing. Si
  aún falta tiempo para entender una transformación, insertar pausas naturales
  entre ideas o preparar una nueva narración con esos silencios.
- Reservar un hold breve al terminar cada demostración y uno mayor para una
  pregunta final.

### Checklist para una nueva story

1. Definir una sola idea visible por bloque y los objetos que la representan.
2. Diseñar el movimiento sin audio: debe seguir explicando el concepto.
3. Preparar capturas de inicio, regla, transformación, resultado y cierre.
4. Revisar solapes, escala, rutas residuales y legibilidad en esas capturas.
5. Recién entonces sincronizar con el audio real y validar el flujo de Guardian.
