# Guardian — FINAL SPEC Stage 3A + 3B
## Consolidación del proyecto, rediseño Admin y Dashboard de métricas

**Versión productiva de partida:** `0.4.1`

## 1. Objetivo

Esta etapa debe consolidar Guardian sin cambiar la lógica educativa validada en producción.

Debe resolver tres áreas:

1. limpieza y documentación del proyecto;
2. rediseño funcional y visual del Admin;
3. dashboard de práctica y rendimiento.

No agregar nuevas categorías, niveles ni habilidades.

---

# STAGE 3A — CONSOLIDACIÓN + ADMIN UX

## 2. Inspección previa obligatoria

Antes de editar código, inspeccionar y documentar brevemente el estado actual de:

- `server/app/admin.py`
- `server/app/models.py`
- `server/app/api.py`
- templates de Admin
- `admin.css`
- migraciones
- scripts
- documentación
- `release/`
- `releases/`
- `dist/`
- tests
- `.gitignore`

No asumir que funcionalidades pedidas son nuevas.

Particularmente:

- `Device.display_name` ya existe;
- la edición de nombre visible ya existe;
- `Activity` ya posee `input type="date"`;
- mission config ya existe;
- JSON desplegable ya existe;
- release metadata y SHA-256 deben seguir existiendo internamente;
- la lógica 0.4.1 no debe reescribirse.

---

## 3. Limpieza del repositorio

### 3.1 Regla general

No borrar por intuición.

Primero clasificar cada archivo/directorio relevante como:

- vigente;
- histórico;
- generado;
- privado/local;
- obsoleto;
- dudoso.

Si hay duda sobre si algo participa de build/deploy/release/test, mantenerlo.

### 3.2 Mantener

Mantener:

- código productivo;
- updater;
- servidor;
- Admin;
- migraciones históricas;
- tests vigentes;
- scripts necesarios para build/test/server/deploy/backup/release;
- specs finales vigentes;
- documentación operativa vigente;
- `.env.example`;
- archivos necesarios para instalación.

### 3.3 Archivar documentación

Crear:

`docs/archive/`

Mover allí documentos históricos que ya no describan el estado actual.

Ejemplos a evaluar:

- specs de Etapa 0;
- roadmaps anteriores;
- especificaciones de etapas superadas;
- arquitectura vieja si fue reemplazada;
- documentación histórica útil.

No mover a `archive` las specs finales de Mission System v2 si siguen siendo referencia vigente.

### 3.4 Archivos locales/privados

Mantener fuera de Git:

- `.env`;
- `BITACORA.local.md`;
- `PLAN.md`;
- `LOCAL_COMMANDS.md`;
- notas privadas;
- datos personales;
- backups;
- entornos virtuales.

Revisar que `.gitignore` siga cubriéndolos.

### 3.5 Artefactos generados

Auditar:

- `dist/`
- `release/`
- `releases/`
- `obj/`
- `.venv/`
- `server/.venv/`
- `.pytest_cache/`

No versionar builds ni caches salvo los archivos explícitamente necesarios por el mecanismo actual.

No romper `publish-release.ps1`.

### 3.6 Archivos claramente accidentales

Eliminar, si no están versionados ni son necesarios:

- archivos `~$...`;
- temporales;
- caches;
- outputs antiguos;
- duplicados generados.

---

## 4. Documento único de estado actual

Crear:

`docs/CURRENT_STATE.md`

Debe ser la primera referencia para alguien que abre el repo hoy.

Debe incluir:

- versión productiva;
- arquitectura;
- componentes;
- flujo Cliente → API → DB → Admin;
- updater;
- RemoteConfig;
- modelo de misiones;
- categorías/niveles/skills actuales;
- perfil privado;
- telemetría;
- rotación;
- estructura de carpetas;
- comandos operativos;
- PC TEST;
- build;
- tests;
- actualización del server;
- release;
- rollout;
- rollback;
- migraciones;
- deudas técnicas vigentes.

No duplicar largas specs históricas: enlazarlas cuando corresponda.

---

## 5. Principios visuales obligatorios del nuevo Admin

Este punto es requisito funcional de aceptación, no una sugerencia estética.

El frontend actual se percibe:

- tosco;
- excesivamente vertical;
- con espacios mal distribuidos;
- con formularios genéricos;
- con controles alejados de su etiqueta;
- con demasiada información técnica;
- visualmente parecido a una herramienta interna antigua.

El Stage 3A debe corregirlo.

### 5.1 Objetivo visual

El Admin debe sentirse como un dashboard moderno, simple y compacto.

Referencias conceptuales:

- cards limpias;
- jerarquía clara;
- densidad media;
- spacing consistente;
- acciones agrupadas;
- datos secundarios visualmente subordinados;
- evitar bloques enormes vacíos.

No copiar librerías/frameworks externos innecesariamente.

Preferir mejorar el HTML/CSS existente.

### 5.2 Sistema de spacing

Definir tokens CSS reutilizables.

Ejemplo conceptual:

- 4 px
- 8 px
- 12 px
- 16 px
- 24 px
- 32 px

No usar márgenes/paddings arbitrarios distintos en cada componente.

### 5.3 Anchura y composición

No hacer que todos los inputs ocupen el ancho completo por defecto.

Usar:

- grids;
- flexbox;
- ancho natural;
- columnas responsivas.

Los formularios desktop deben aprovechar horizontalmente el espacio.

### 5.4 Cards

Cada card debe:

- tener título;
- agrupar información relacionada;
- evitar `fieldset` enormes;
- no tener grandes áreas vacías;
- no repetir bordes dentro de bordes sin necesidad.

### 5.5 Tipografía

Mantener tipografía del sistema si se desea, pero definir jerarquía clara:

- título de página;
- título de card;
- texto principal;
- metadata;
- ayuda.

No usar texto técnico con el mismo peso que información importante.

### 5.6 Botones

Distinguir:

- acción primaria;
- secundaria;
- destructiva;
- disabled.

No hacer que todos los enlaces parezcan botones primarios azules.

### 5.7 Responsive

Debe funcionar razonablemente en desktop y pantallas pequeñas.

No optimizar sólo para móvil.

El uso principal del Admin es escritorio.

---

## 6. Pantalla principal

### 6.1 Contenido

La página principal debe mostrar principalmente dispositivos existentes.

Cada dispositivo debe ser una card compacta.

#### Encabezado

Ejemplo:

**Dispositivo de ejemplo**<br>
`DESKTOP-OBS41NQ`

El `display_name` es principal.

El hostname queda secundario, idealmente entre paréntesis o metadata pequeña.

No mostrar `device_id` en la vista normal.

### 6.2 Estado único y claro

Mostrar exactamente uno de:

- 🟢 `Online · Activo`
- 🟠 `Online · Pausado`
- ⚫ `Offline`

No separar visualmente:

`Online`

y luego:

`Estado Guardian: Pausado`

como ocurre ahora.

### 6.3 Información visible

Mostrar:

- nombre visible;
- hostname secundario;
- estado;
- versión instalada.

Opcionalmente:

- intervalo actual, en texto secundario.

No mostrar por defecto:

- UUID técnico;
- último release global;
- heartbeat timestamp;
- otros detalles internos.

### 6.4 Acciones

Acciones principales de la card:

- `Pausar misiones` o `Reanudar misiones`;
- `Probar misión ahora`.

Acciones de navegación:

- `Actividad`;
- `Configuración`;
- `Métricas`.

Actualizar:

- selector de release;
- botón `Actualizar`.

No mezclar edición de nombre/intervalo directamente en esta card.

Eso se mueve a Configuración.

### 6.5 Offline

Si está offline:

- mostrar claramente `Offline`;
- deshabilitar acciones remotas;
- no eliminar controles de navegación;
- actualización puede conservar la semántica actual si se permite dejarla pendiente.

---

## 7. Configuración

La página actual de Misiones debe evolucionar a:

**Configuración — [display_name]**

No debe ser sólo “Configurar misiones”.

### 7.1 Sección General

Mostrar juntos:

#### Nombre del dispositivo

Editar `Device.display_name` existente.

No crear un segundo campo equivalente.

#### Hostname

Mostrar sólo lectura.

#### Intervalo

Editar intervalo en minutos.

Usar layout compacto.

Ejemplo:

| Nombre visible | Intervalo |
|---|---|
| Dispositivo de ejemplo | 15 min |

No inputs gigantes full width.

---

## 8. Configuración de misiones

Mantener:

`Categoría → Nivel → Skill`

No mostrar variants.

### 8.1 Layout obligatorio

No usar el formato actual:

- gran `fieldset`;
- checkbox separado;
- tooltip en línea aparte;
- `<br>` por habilidad.

Usar cards/secciones compactas.

Ejemplo conceptual:

**Matemática**

**Operaciones básicas** ⓘ<br>
☑ Sumas ☑ Restas ☑ Multiplicaciones

**Comprensión**

**Comprensión funcional** ⓘ

☑ Identidad<br>
☑ Edad y nacimiento<br>
☑ Fecha actual<br>
☑ Relaciones temporales<br>
☑ Calendario<br>
☑ Estaciones

En desktop, skills preferentemente en grid de 2–3 columnas cuando haya espacio.

Checkbox siempre inmediatamente junto al texto.

### 8.2 Tri-state

Mantener:

- checked;
- indeterminate;
- unchecked.

El checkbox de nivel debe estar junto a su nombre.

---

## 9. Tooltips

Reemplazar dependencia visual exclusiva de `title=` si produce experiencia pobre.

Crear un patrón reusable.

Debe:

- abrir con hover;
- abrir con keyboard focus;
- estar junto al concepto;
- no generar saltos de layout;
- no aparecer en una línea aislada;
- tener ancho máximo razonable;
- tener contraste;
- no tapar elementos esenciales.

---

## 10. Perfil privado

No mostrar permanentemente cinco inputs gigantes.

Mostrar card compacta:

**Perfil para misiones**<br>
`Configurado` / `Incompleto`

Acción:

`Editar perfil`

Abrir mediante:

- panel colapsable;
- drawer;
- modal;

elegir la solución más simple en la stack actual.

Mantener privacidad existente.

---

## 11. Timezone del dispositivo

Agregar timezone configurable a nivel dispositivo/configuración.

No asociarla al perfil personal.

Preferencia:

`DeviceConfiguration.timezone`

o equivalente coherente con la arquitectura.

Formato:

IANA.

Ejemplo:

`America/Argentina/Buenos_Aires`

Agregar migración.

La DB continúa almacenando eventos en UTC.

---

## 12. Releases en pantalla principal

Mantener sección al final.

Hacerla compacta.

Mostrar:

- versión;
- fecha;
- notas breves.

No mostrar normalmente:

- filename;
- bytes;
- SHA-256.

SHA-256 y filename no se eliminan del modelo ni del flujo updater.

Pueden quedar en detalles técnicos opcionales.

---

## 13. Actividad

### 13.1 Fecha y hora

La tabla debe contener:

| Fecha | Hora | Evento | Versión | Resumen |

No sólo Hora.

Convertir `occurred_at` UTC a timezone del dispositivo.

Mostrar claramente:

`Hora local: America/Argentina/Buenos_Aires`

o texto amigable equivalente.

### 13.2 Períodos

Agregar:

- Hoy
- Ayer
- Últimos 7 días
- Últimos 30 días
- Fecha específica
- Todos

### 13.3 Fecha específica

Actualmente ya existe un `<input type="date">`, pero queda disabled cuando `period != date`.

Corregir interacción.

Cuando el usuario seleccione `Fecha específica`:

- habilitar inmediatamente el date picker;
- permitir click;
- permitir selección;
- mantener valor seleccionado;
- enviar filtro correcto.

Cuando seleccione otro período:

- puede deshabilitarse nuevamente.

Agregar JS mínimo si es necesario.

No recargar la página sólo para poder habilitar el calendario.

---

## 14. Filtro de eventos

Usar categorías humanas:

- Todos;
- Misiones;
- Configuración;
- Actualizaciones;
- Sistema.

Mapear internamente eventos existentes.

No modificar nombres guardados en DB.

---

## 15. Eventos técnicos

Por defecto ocultar eventos muy frecuentes/diagnósticos como:

- HeartbeatSent;
- RemoteConfigFetched;
- RemoteConfigReceived.

Agregar control:

`Mostrar eventos técnicos`

Al activarlo se muestran.

No borrar estos eventos.

---

## 16. Resumen de eventos

Para eventos de misión, intentar mostrar resumen humano usando metadata existente.

Ejemplo:

**Misión resuelta**<br>
`Estaciones · 1.er intento`

En lugar de obligar al usuario a interpretar:

`MissionSolved`

como única información.

El event_type técnico puede conservarse como detalle secundario.

---

## 17. JSON

Mantener `<details>` de JSON.

Mejorar presentación visual si es necesario.

Debe seguir siendo accesible para diagnóstico.

---

# STAGE 3B — DASHBOARD

## 18. Objetivo

El Dashboard debe responder primero a preguntas globales y permitir profundizar sólo cuando el usuario quiera.

No llenar la pantalla inicial con todos los detalles.

Principio:

**overview primero → drill-down después**

---

## 19. Navegación

Desde cada dispositivo:

`Métricas`

Abre dashboard filtrado por ese dispositivo.

Mantener:

- período;
- dispositivo;
- scope.

Breadcrumb:

`Métricas > Comprensión > Comprensión funcional > Calendario`

---

## 20. Filtros

- Hoy
- 7 días
- 30 días
- Todo
- Rango personalizado

No enfocarse en tiempo de uso.

---

## 21. KPIs globales

Mostrar:

- Misiones resueltas;
- % primer intento;
- % con reintento;
- Intentos promedio por misión.

Secundario/experimental:

- Tiempo mediano de resolución.

No mostrar el tiempo como métrica principal.

---

## 22. Definición de misión

Agrupar siempre por `mission_id`.

Ejemplo:

- MissionStarted;
- Failed attempt 1;
- Failed attempt 2;
- Solved attempt 3.

Resultado:

- 1 misión;
- 3 intentos;
- clasificada en `3+`.

No contar eventos como misiones independientes.

---

## 23. Misiones por día

Gráfico de barras apiladas.

- X = día
- Y = cantidad de misiones
- stack = categoría

Debe mantener colores estables.

---

## 24. Rendimiento por intentos

Gráfico principal de dificultad.

Barras horizontales apiladas por skill.

Segmentos:

- 1.er intento;
- 2.º intento;
- 3.º+.

Permitir click.

---

## 25. Tabla resumen

Mostrar:

| Habilidad | Misiones | 1er intento | 2º | 3º+ | Intentos/misión | Tiempo mediano* |

Tiempo mediano marcado como experimental/secundario.

---

## 26. Tiempo de resolución

Calcular con:

`MissionSolved - MissionStarted`

mismo `mission_id`.

Preferir mediana.

Advertencia conceptual:

No interpretar automáticamente una duración larga como dificultad.

El niño puede haberse alejado de la pantalla.

No usar para decisiones automáticas.

---

## 27. Drill-down

Scopes:

1. Global
2. Categoría
3. Nivel
4. Skill

La misma vista se adapta al scope.

No crear templates independientes manuales por skill.

---

## 28. Categoría

Ejemplo:

`Comprensión`

Mostrar:

- KPIs;
- evolución;
- niveles;
- resultados por nivel.

---

## 29. Nivel

Ejemplo:

`Comprensión funcional`

Mostrar:

- KPIs;
- skills;
- intentos por skill;
- tabla.

---

## 30. Skill

Ejemplo:

`Calendario`

Mostrar:

- misiones;
- primer intento;
- segundo;
- 3+;
- intentos promedio;
- mediana experimental;
- evolución temporal.

Acción:

`Ver variantes`

---

## 31. Variants

No mostrar en overview.

Abrir bajo demanda mediante:

- drawer;
- panel expandible;
- modal.

Preferencia: no navegar a otra página completa.

Mostrar:

| Variante | Misiones | 1er intento | 2.º | 3.º+ | Intentos/misión |

Esto debe permitir detectar una variant problemática dentro de una skill aparentemente correcta.

---

## 32. Sistema visual de colores

Debe ser centralizado y escalable.

### Contenido

Categoría = familia cromática estable.

Nivel = tonos/intensidades de esa familia.

Skills = tonos distinguibles dentro del scope cuando corresponda.

No definir manualmente veinte colores por IDs individuales si puede generarse desde un sistema.

### Rendimiento

Usar otra semántica estable:

- 1.er intento;
- 2.º;
- 3.º+.

Los colores de rendimiento deben mantener el mismo significado en todas las pantallas.

---

## 33. Diseño del Dashboard

Aplican las mismas reglas visuales del Admin.

En particular:

- evitar gráficos gigantes innecesarios;
- cards alineadas;
- KPIs compactos;
- máximo aprovechamiento horizontal;
- no crear una columna vertical interminable;
- tablas con densidad razonable;
- espacio uniforme;
- títulos y leyendas claras;
- estados vacíos diseñados;
- tooltips legibles.

En desktop, aprovechar ancho con grids de cards/gráficos.

No apilar todo verticalmente si cabe lado a lado.

---

## 34. Backend de métricas

No calcular todo en navegador descargando miles de eventos.

Crear capa server-side de agregación.

Reutilizar:

- device_id;
- mission_id;
- category_id;
- level_id;
- skill_id;
- variant_id;
- attempt;
- occurred_at.

---

## 35. Datos históricos

Soportar:

- `mission_id`;
- legacy `missionId` cuando sea necesario.

No exigir migrar payloads históricos.

Eventos sin metadata educativa suficiente:

- no inventar categoría;
- mantenerlos en Activity;
- excluirlos del detalle educativo.

---

## 36. Índices/performance

Primero identificar queries reales.

Agregar índices sólo cuando sean necesarios.

Evaluar particularmente:

- `device_events.device_id`;
- `device_events.utc/occurred_at`;
- `event_type`;
- campos JSONB usados repetidamente.

No crear índices JSON indiscriminados.

---

## 37. Privacidad

No mostrar ni agregar a métricas:

- respuestas textuales;
- preferred_name;
- nombres personales;
- apellido;
- nacimiento;
- valores del perfil.

El dashboard usa únicamente IDs y labels del catálogo educativo.

---

## 38. Sin automatización adaptativa

No:

- apagar skills automáticamente;
- cambiar configuración;
- declarar una skill aprendida;
- modificar rotación.

El dashboard informa.

El adulto decide.

---

## 39. Tamaño de muestra

Siempre mostrar cantidad de misiones junto a porcentajes.

No presentar:

`100%`

sin contexto.

Ejemplo:

`100% · 1 misión`

---

## 40. Fuera de alcance

No implementar:

- nuevos desafíos;
- nuevos niveles;
- IA;
- adaptación automática;
- scoring educativo;
- gamificación;
- recompensas;
- rediseño completo de LockWindow;
- tablet;
- encendido físico remoto.

---

## 41. Deudas que quedan después

Mantener registradas:

- modernización de LockWindow;
- input de respuestas largas;
- UX acierto/error;
- accesibilidad cliente;
- posible unificación futura `missionId` / `mission_id`.

---

## 42. Tests

Agregar tests de:

### Configuración

- editar display_name existente;
- hostname fallback;
- intervalo;
- timezone;
- mission config;
- perfil.

### Activity

- timezone;
- múltiples fechas;
- Today;
- Yesterday;
- 7 días;
- 30 días;
- specific date;
- categorías de eventos;
- ocultar técnicos;
- mostrar técnicos.

### Métricas

Caso intento 1:

- Started;
- Solved attempt=1.

Resultado:

- missions=1;
- first_attempt=1;
- attempts=1.

Caso intento 3:

- Started;
- Failed 1;
- Failed 2;
- Solved 3.

Resultado:

- missions=1;
- third_plus=1;
- attempts=3.

Además:

- varias categorías;
- niveles;
- skills;
- variants;
- días;
- filtros;
- datos legacy;
- eventos repetidos sin doble conteo.

---

## 43. Validación visual obligatoria

No considerar terminado sólo porque los endpoints/tests funcionan.

Hacer validación manual de las páginas reales.

Revisar visualmente:

### Dashboard principal

- cards alineadas;
- sin grandes blancos inútiles;
- sin inputs dispersos;
- acciones agrupadas;
- estado visible inmediatamente.

### Configuración

- checkboxes junto a labels;
- skills compactas;
- tooltips funcionales;
- perfil no domina pantalla.

### Actividad

- filtros alineados;
- date picker funcional;
- tabla legible;
- Fecha + Hora;
- JSON no rompe ancho.

### Métricas

- cards/KPIs compactos;
- gráficos no excesivamente altos;
- leyendas legibles;
- drill-down claro;
- responsive razonable.

La frase:

**“funciona pero se ve tosco”**

NO cumple criterio de aceptación de Stage 3A/3B.

---

## 44. Criterio de aceptación final

La etapa sólo se cierra cuando:

- repo limpio/consolidado;
- `CURRENT_STATE.md` actualizado;
- documentación histórica archivada;
- Admin principal simplificado;
- `display_name` usado correctamente;
- estados inequívocos;
- Configuración centralizada;
- diseño compacto/moderno;
- Activity corregida;
- timezone local;
- date picker funcional;
- técnicos filtrables;
- Releases compactas;
- Dashboard global;
- drill-down;
- variants bajo demanda;
- métricas correctas por mission_id;
- tiempo tratado como experimental;
- tests verdes;
- validación visual satisfactoria;
- PC TEST validado;
- sin rollout automático a producción.
