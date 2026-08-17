# Guardian — Propuesta de evolución, arquitectura y roadmap

**Estado:** Documento de propuesta  
**Fecha:** 8 de agosto de 2026  
**Objetivo:** ordenar la evolución de Guardian desde el MVP inicial de desafíos matemáticos hacia una plataforma administrable, medible, adaptable y extensible.

---

## 1. Visión

Guardian comienza como una aplicación instalada en la computadora del niño que:

- detecta tiempo de exposición a la pantalla;
- dispara bloqueos en determinados momentos;
- presenta desafíos;
- valida la resolución;
- permite continuar con el uso del dispositivo una vez cumplida la misión.

La primera versión se centra en **operaciones matemáticas**, pero la arquitectura debe permitir evolucionar hacia:

- diferentes tipos de desafíos;
- dificultad dinámica;
- banco de preguntas administrable;
- métricas e historial;
- control remoto;
- actualizaciones remotas;
- múltiples dispositivos;
- selección adaptativa de desafíos;
- futura incorporación de inteligencia mediante agentes o n8n.

La meta no es construir solamente un bloqueador con cuentas matemáticas, sino un **motor de interrupciones, desafíos y recompensas administrable de forma remota**.

---

## 2. Principios de diseño

### 2.1 Guardian debe poder evolucionar sin reinstalaciones manuales

La instalación manual en la computadora administrada debería realizarse una sola vez.

A partir de allí deben existir dos mecanismos diferentes:

1. **Configuración remota**, para cambiar comportamiento sin modificar el software.
2. **Actualización remota**, para instalar nuevas capacidades cuando exista una nueva versión.

---

### 2.2 No todo cambio debe ser una salida a producción

Es fundamental separar tres conceptos:

#### Configuración

Cambios que Guardian debe recibir remotamente sin una nueva versión.

Ejemplos:

- tiempo entre desafíos;
- cantidad de ejercicios;
- nivel de dificultad;
- operaciones habilitadas;
- tipos de desafíos habilitados;
- bloqueo activo/inactivo;
- horarios;
- reglas particulares.

---

#### Contenido

Información que Guardian consume pero que no forma parte del ejecutable.

Ejemplos:

- preguntas;
- respuestas válidas;
- categorías;
- dificultad;
- estado activo/inactivo;
- variantes equivalentes;
- reglas de selección.

Agregar o quitar una pregunta no debería requerir modificar código ni publicar una nueva versión.

---

#### Software

Solo requiere una nueva versión cuando se incorpora una capacidad nueva.

Ejemplos:

- un nuevo tipo de misión;
- un nuevo mecanismo de bloqueo;
- compatibilidad con otro dispositivo;
- un nuevo motor de actualización;
- nuevas capacidades de telemetría;
- soporte para agentes.

---

## 3. Arquitectura propuesta

La primera arquitectura de Guardian será **100% self-hosted dentro de la red doméstica**.

Se aprovechará una PC de la casa que ya permanece encendida y funciona como servidor mediante Docker. No se requiere, para esta etapa, contratar una base de datos, servidor ni backend en la nube.

```text
                     RED DE CASA
                  LAN / 192.168.x.x

        ┌───────────────────────────────┐
        │       PC SERVIDOR            │
        │                               │
        │  Docker Compose               │
        │                               │
        │  ┌─────────────────────────┐  │
        │  │ Guardian Admin          │  │
        │  └────────────┬────────────┘  │
        │               │               │
        │  ┌────────────▼────────────┐  │
        │  │ Guardian API            │  │
        │  └────────────┬────────────┘  │
        │               │               │
        │  ┌────────────▼────────────┐  │
        │  │ PostgreSQL              │  │
        │  └─────────────────────────┘  │
        │                               │
        └───────────────▲───────────────┘
                        │
                        │ LAN
                        │
        ┌───────────────┴───────────────┐
        │       PC ADMINISTRADA         │
        │                               │
        │ Guardian Client               │
        │ Guardian Updater              │
        └───────────────────────────────┘
```

La computadora administradora accederá a Guardian Admin también desde la red doméstica.

Inicialmente se podrá acceder mediante la IP local del servidor, por ejemplo:

```text
http://192.168.1.xxx
```

Más adelante puede configurarse un nombre amigable dentro de la red, por ejemplo:

```text
http://guardian.local
```

El acceso desde Internet queda explícitamente **fuera del alcance inicial**.

---

## 4. Guardian Client

Es el programa instalado en la computadora administrada.

Sus responsabilidades principales son:

- detectar actividad;
- medir tiempo de exposición;
- mantener el contador correspondiente;
- disparar un bloqueo;
- presentar desafíos;
- validar respuestas;
- desbloquear cuando corresponde;
- registrar eventos;
- consultar configuración remota;
- informar su estado;
- informar su versión;
- recibir instrucciones de actualización.

Guardian Client debe poder continuar funcionando aunque exista una interrupción temporal de red o la PC servidor esté apagada.

Para ello debe conservar localmente la última configuración válida recibida. Por ejemplo:

```text
Servidor disponible
        ↓
Guardian recibe:
tiempo_disparo = 30 min
        ↓
guarda configuración local
        ↓
Servidor temporalmente apagado
        ↓
Guardian continúa usando 30 min
        ↓
Servidor vuelve a estar disponible
        ↓
Guardian sincroniza nuevamente
```

La indisponibilidad del servidor no debe desactivar el funcionamiento básico de Guardian.

---

## 5. Guardian Updater

Es el componente responsable de actualizar Guardian Client.

Su objetivo es evitar reinstalaciones manuales.

Flujo esperado:

```text
Guardian v0.1.0 instalada
        │
        ▼
consulta versión disponible
        │
        ▼
v0.2.0 publicada
        │
        ▼
descarga
        │
        ▼
instalación
        │
        ▼
reinicio de Guardian
```

Idealmente, el usuario de la computadora administrada no debe intervenir en el proceso.

---

## 6. Guardian API

Es el backend exclusivo de Guardian.

No se propone reutilizar el backend de otros proyectos. Guardian debe tener su propio dominio funcional y sus propios datos.

Responsabilidades:

- autenticación;
- registro de dispositivos;
- configuración;
- telemetría;
- banco de desafíos;
- versiones;
- órdenes de actualización;
- consultas para el dashboard;
- futura integración con otros componentes.

La API funciona como intermediario entre Guardian Client, Guardian Admin y la base de datos.

---

## 7. Base de datos

La base principal vivirá en la **PC servidor de la casa**, dentro de Docker.

Esto significa que los datos permanecen físicamente bajo control local y no requieren contratar una base administrada en la nube.

Flujo inicial:

```text
Guardian Client
      │
      │ red doméstica
      ▼
Guardian API
      │
      ▼
PostgreSQL
      │
      ▼
volumen persistente en disco
```

Guardian Client **no debe conectarse directamente a PostgreSQL**. Toda lectura y escritura debe pasar por Guardian API.

La base deberá utilizar almacenamiento persistente mediante un volumen de Docker, de modo que reiniciar o recrear un contenedor no elimine la información.

También deberá contemplarse una estrategia simple de backup local.

Esta arquitectura permite:

- centralizar datos sin depender de servicios pagos;
- consultar el dashboard desde otras computadoras de la casa;
- compartir configuración entre futuros dispositivos;
- mantener historial centralizado;
- conservar los datos aunque Guardian Client esté apagado.

PostgreSQL es la opción propuesta para la primera implementación, sujeta a confirmación en la especificación técnica de la Etapa 0.

---

## 8. Guardian Admin

Guardian Admin será una aplicación web privada accesible inicialmente **solo dentro de la red doméstica**.

El acceso podrá comenzar mediante la IP local de la PC servidor:

```text
http://192.168.1.xxx
```

y posteriormente podrá utilizarse un nombre local más amigable como:

```text
http://guardian.local
```

Debe requerir autenticación.

Desde allí se podrá administrar Guardian sin acceder físicamente a la computadora administrada.

Un subdominio público como `guardian.tudominio.com` queda reservado para una futura etapa de acceso externo, si realmente se necesita.

---

## 9. Funciones previstas de Guardian Admin

### 9.1 Estado del dispositivo

Ejemplo:

```text
Test Device
● Conectado

Versión instalada: 0.4.1
Última versión:    0.5.0

[ Actualizar ahora ]
```

---

### 9.2 Configuración

Ejemplo:

```text
Tiempo entre desafíos
[ 20 minutos ]

Dificultad
[ Automática ]

Matemática
[x] Sumas
[x] Restas
[ ] Multiplicaciones
[ ] Divisiones

Bloqueo
[x] Activado

[ GUARDAR CAMBIOS ]
```

Guardar esta configuración no debe generar una nueva versión del software.

---

### 9.3 Banco de desafíos

Ejemplo:

```text
BANCO DE DESAFÍOS

✓ ¿Cuál es tu nombre?
✓ ¿En qué ciudad vivís?
✗ ¿Cuántas uñas tenés?
✓ ¿Cuáles son las estaciones?

[ + NUEVA PREGUNTA ]
```

Debe ser posible:

- agregar preguntas;
- editar preguntas;
- activar/desactivar preguntas;
- indicar respuestas válidas;
- asignar categorías;
- asignar dificultad;
- consultar desempeño histórico.

---

### 9.4 Dashboard

El dashboard deberá construirse sobre datos recolectados por Guardian.

Métricas iniciales posibles:

- tiempo de pantalla por día;
- cantidad de desafíos disparados;
- cantidad de desafíos resueltos;
- respuestas correctas al primer intento;
- cantidad de intentos;
- desafíos con mayor dificultad;
- tiempo de resolución;
- evolución de desempeño;
- tiempo de pantalla acumulado;
- desafíos que ya parecen dominados;
- actividad voluntaria posterior al cumplimiento del desafío.

El diseño visual del dashboard debe definirse después de cerrar correctamente el modelo de telemetría.

---

## 10. Telemetría

Antes de construir métricas es necesario definir qué eventos debe registrar Guardian.

Propuesta inicial:

```text
screen_session_started
screen_session_ended

challenge_triggered
challenge_started

answer_submitted
answer_correct
answer_incorrect

challenge_completed
challenge_abandoned

screen_unlocked

guardian_started
guardian_stopped
```

Cada evento debería contener suficiente contexto para reconstruir lo ocurrido.

Posibles datos asociados:

- timestamp;
- dispositivo;
- usuario;
- sesión;
- challenge_id;
- tipo;
- dificultad;
- intento;
- respuesta;
- resultado;
- tiempo de resolución;
- tiempo de pantalla acumulado;
- versión de Guardian;
- configuración activa.

El diseño definitivo se realizará en una etapa específica.

---

## 11. Nuevos tipos de desafíos

La primera expansión luego de matemática puede ser un desafío de respuesta escrita.

Ejemplos iniciales:

- ¿Cuál es tu nombre?
- ¿Cuál es tu apellido?
- ¿Cuántos años tenés?
- ¿En qué ciudad vivís?
- ¿En qué provincia vivís?
- ¿Cuántos dedos tenés en una mano?
- ¿Cuántas uñas tenés en todo tu cuerpo?

También pueden incorporarse progresivamente desafíos como:

- estaciones del año;
- conocimiento general;
- comprensión;
- emociones;
- lógica;
- lectura;
- ortografía;
- inglés;
- temas escolares.

---

## 12. Validación flexible de respuestas

El sistema no debe exigir coincidencia textual exacta cuando la respuesta conceptual sea correcta.

Ejemplo:

```text
CIUDAD_EJEMPLO
Ciudad_Ejemplo
ciudad_ejemplo

→ equivalentes
```

También deben poder definirse respuestas equivalentes:

```text
5
cinco

→ equivalentes
```

La normalización puede contemplar:

- mayúsculas/minúsculas;
- acentos;
- espacios;
- variantes equivalentes;
- representación numérica o textual.

La ortografía no debería invalidar automáticamente una respuesta cuando el concepto es inequívocamente correcto, aunque este comportamiento debe definirse con cuidado por tipo de desafío.

---

## 13. Modelo general de misión

Conviene evitar una implementación basada en condiciones específicas por cada tipo de ejercicio.

En lugar de:

```python
if tipo == "suma":
    ...
```

Guardian debería evolucionar hacia un concepto general de misión.

Ejemplo conceptual:

```text
Mission
 ├─ id
 ├─ tipo
 ├─ categoría
 ├─ dificultad
 ├─ instrucciones
 ├─ contenido
 ├─ respuestas válidas
 ├─ estrategia de validación
 ├─ reglas de selección
 └─ recompensa
```

Esto permitirá agregar nuevas familias de desafíos sin transformar Guardian en una colección de casos especiales.

---

## 14. Selección adaptativa

Guardian debería usar el historial para decidir qué desafíos seguir mostrando.

Una pregunta que se responde correctamente de forma rápida durante varias apariciones puede pasar a estado de dominio.

Ejemplo:

```text
Pregunta A

✓ 3 s
✓ 4 s
✓ 3 s
✓ 2 s
✓ 3 s

Estado: DOMINADA
```

Una pregunta con dificultades debería conservar mayor probabilidad de aparición.

```text
Pregunta B

✗
✗
✓
✗
✓

Estado: EN APRENDIZAJE
```

La selección adaptativa puede funcionar inicialmente mediante reglas deterministas, sin inteligencia artificial.

---

## 15. Dificultad adaptativa

Una idea inicial es incrementar la dificultad según el tiempo de exposición durante el día.

Ejemplo:

```text
primer disparo
→ dificultad baja

cuarto disparo
→ dificultad mayor
```

Sin embargo, no conviene que la dificultad dependa exclusivamente del tiempo de pantalla.

Propuesta:

```text
dificultad =
nivel demostrado
+ tiempo de pantalla
+ desempeño reciente
+ tipo de desafío
```

La lógica exacta debe discutirse antes de implementarse.

El objetivo no es simplemente aumentar dificultad, sino seleccionar un desafío adecuado al contexto.

---

## 16. Inteligencia futura

La inteligencia mediante agentes o n8n se propone como una etapa posterior.

La IA no debería controlar directamente Guardian.

Arquitectura conceptual:

```text
Historial Guardian
       │
       ▼
Motor adaptativo
       │
       ▼
       IA
       │
       ▼
propuesta de desafíos
       │
       ▼
Banco de desafíos
       │
       ▼
validación / aprobación
       │
       ▼
Guardian
```

La inteligencia podría ayudar a:

- proponer nuevas preguntas;
- ajustar contenido;
- detectar temas dominados;
- detectar dificultades;
- generar ejercicios;
- relacionar desafíos con temas escolares;
- sugerir cambios al administrador.

Las reglas críticas de bloqueo y seguridad deben continuar siendo deterministas.

---

# 17. Roadmap propuesto

## ETAPA 0 — Guardian administrable

### Objetivo

Poder administrar Guardian y actualizar la PC administrada desde otra computadora de la casa, sin necesidad de acceder físicamente a ella.

La Etapa 0 utilizará exclusivamente la **red doméstica** y una **PC servidor self-hosted con Docker**.

El acceso desde Internet no forma parte de esta etapa.

### 0A — Backend local

Objetivo:

Disponer del núcleo central de Guardian en la PC servidor.

Trabajo:

- Docker Compose propio de Guardian;
- PostgreSQL;
- Guardian API;
- persistencia mediante volúmenes;
- configuración mediante variables de entorno;
- healthchecks básicos;
- backup local inicial.

Criterio de salida:

Guardian API y PostgreSQL pueden levantarse y reiniciarse sin perder información.

---

### 0B — Comunicación Client ↔ API

Objetivo:

Lograr comunicación entre Guardian Client y el servidor dentro de la LAN.

Trabajo:

- dirección configurable del servidor;
- identificación del dispositivo;
- registro inicial;
- heartbeat o señal de actividad;
- reporte de versión instalada;
- manejo de servidor no disponible.

Criterio de salida:

Desde Guardian API puede identificarse la PC administrada y conocerse su versión y última comunicación.

---

### 0C — Configuración remota

Objetivo:

Modificar el comportamiento de Guardian sin publicar una nueva versión.

Trabajo inicial:

- tiempo entre desafíos;
- bloqueo activo/inactivo;
- nivel o parámetros básicos que ya soporte el MVP;
- endpoint de configuración;
- persistencia en PostgreSQL;
- consulta periódica desde Guardian Client;
- caché local de última configuración válida.

Criterio de salida principal:

Desde la computadora administradora se cambia:

```text
tiempo entre desafíos:
20 minutos → 30 minutos
```

y Guardian Client adopta la nueva configuración sin reinstalación manual.

Si el servidor se apaga después, Guardian continúa funcionando con la última configuración recibida.

---

### 0D — Guardian Admin

Objetivo:

No depender de llamadas manuales a la API para administrar Guardian.

Trabajo:

- login;
- pantalla del dispositivo;
- estado de última conexión;
- versión instalada;
- edición de configuración;
- guardado;
- confirmación visual de cambios.

Criterio de salida:

La configuración puede modificarse desde una interfaz web accesible dentro de la red doméstica.

---

### 0E — Releases y build

Objetivo:

Crear un proceso reproducible para generar nuevas versiones de Guardian.

Trabajo:

- versionado;
- definición del artefacto instalable;
- build reproducible;
- pruebas previas al build;
- publicación de releases;
- metadata de versión;
- historial de versiones.

El mecanismo exacto de publicación se definirá en la especificación técnica.

Criterio de salida:

Una nueva versión puede generarse de forma consistente y quedar disponible para el sistema de actualización.

---

### 0F — Guardian Updater

Objetivo:

Actualizar Guardian Client sin acceder físicamente a la PC administrada.

Trabajo:

- detección de versión disponible;
- descarga desde la red doméstica;
- validación del artefacto;
- cierre controlado de Guardian;
- instalación/reemplazo;
- reinicio;
- manejo de errores;
- estrategia de rollback;
- reporte del resultado al servidor.

Criterio de salida:

Desde la computadora administradora puede iniciarse o habilitarse una actualización y la PC administrada termina ejecutando la nueva versión sin reinstalación manual.

---

### Criterio de salida global de Etapa 0

Estando ambas computadoras conectadas a la red doméstica:

1. Guardian funciona normalmente en la PC administrada.
2. La PC servidor centraliza API, Admin y PostgreSQL mediante Docker.
3. Desde Guardian Admin puede modificarse la configuración.
4. Guardian Client adopta esa configuración sin reinstalarse.
5. Guardian continúa funcionando si el servidor queda temporalmente inaccesible.
6. Puede publicarse una nueva versión.
7. Guardian Updater puede instalarla sin acceso físico a la PC administrada.

## ETAPA 1 — Telemetría

### Objetivo

Registrar datos suficientes para comprender qué ocurre durante el uso real.

### Trabajo

- definir eventos;
- definir payloads;
- identificar sesiones;
- registrar intentos;
- registrar tiempo de resolución;
- registrar tiempo de pantalla;
- enviar eventos a Guardian API;
- manejar funcionamiento offline temporal;
- evitar pérdida o duplicación innecesaria.

### Criterio de salida

Los datos permiten reconstruir una jornada de uso y responder preguntas básicas de comportamiento y rendimiento.

---

## ETAPA 2 — Guardian Dashboard

### Objetivo

Transformar la telemetría en información comprensible para el administrador.

### Métricas iniciales

- tiempo de pantalla;
- desafíos por día;
- tasa de resolución;
- intentos;
- tiempo medio;
- dificultad;
- preguntas con mayor error;
- evolución temporal.

### Criterio de salida

El administrador puede entender rápidamente qué ocurrió durante el día y durante la semana.

---

## ETAPA 3 — Banco de desafíos

### Objetivo

Separar el contenido del software.

### Trabajo

- tabla/modelo de desafíos;
- categorías;
- respuestas válidas;
- dificultad;
- estado activo;
- CRUD desde Guardian Admin;
- normalización de respuestas;
- nuevas preguntas de respuesta escrita.

### Criterio de salida

Es posible agregar, editar o retirar una pregunta sin modificar Guardian Client ni publicar una nueva versión.

---

## ETAPA 4 — Selección adaptativa

### Objetivo

Usar historial para decidir qué desafíos mostrar.

### Trabajo

- métricas por desafío;
- estado de dominio;
- reglas de repetición;
- reducción de frecuencia para contenido dominado;
- mayor frecuencia para contenido en aprendizaje.

### Criterio de salida

Dos desafíos con diferente historial no tienen la misma probabilidad de aparecer.

---

## ETAPA 5 — Dificultad adaptativa

### Objetivo

Seleccionar un nivel apropiado en función del contexto.

### Variables posibles

- nivel demostrado;
- desempeño reciente;
- tiempo de pantalla acumulado;
- cantidad de disparos;
- categoría;
- tipo de desafío.

### Criterio de salida

La dificultad deja de ser un valor fijo y responde a reglas explícitas y auditables.

---

## ETAPA 6 — Inteligencia

### Objetivo

Incorporar generación y recomendación inteligente sin entregar el control crítico del sistema a un agente.

### Posibles funciones

- sugerencia de desafíos;
- generación de contenido;
- detección de dificultades;
- propuestas basadas en temas escolares;
- sugerencias de progresión;
- análisis del historial.

### Criterio de salida

La IA aporta contenido o recomendaciones, mientras las decisiones críticas siguen bajo reglas deterministas y control del administrador.

---

## ETAPA 7 — Múltiples dispositivos

### Objetivo

Extender Guardian a otros dispositivos sin duplicar configuración e historial.

Ejemplo:

```text
                 Guardian API
                /            \
               /              \
        Guardian PC        Guardian Tablet
               \              /
                \            /
             historial compartido
```

Esto permitirá pensar reglas de exposición global entre dispositivos.

---

## 18. Releases y flujo de producción

Flujo objetivo:

```text
desarrollo
   │
   ▼
repositorio Git
   │
   ▼
tests
   │
   ▼
build
   │
   ▼
release versionado
   │
   ▼
Guardian API conoce la versión disponible
   │
   ▼
Guardian Admin la muestra
   │
   ▼
actualización remota
   │
   ▼
Guardian Client actualizado
```

Ejemplo de versionado:

```text
v0.1.0
Matemática básica

v0.2.0
Infraestructura remota

v0.3.0
Telemetría

v0.4.0
Banco de desafíos

v0.5.0
Selección adaptativa
```

El esquema exacto de versionado deberá definirse técnicamente en la Etapa 0.

---

## 19. Seguridad

Guardian controlará un dispositivo de forma remota, por lo que la seguridad debe formar parte del diseño desde el comienzo.

La especificación técnica deberá contemplar al menos:

- autenticación del administrador;
- aislamiento de servicios dentro de la LAN;
- identidad segura del dispositivo;
- autorización de endpoints;
- secretos fuera del código;
- validación de configuración;
- validación de releases;
- protección contra instalación de binarios no autorizados;
- logs de operaciones;
- tratamiento seguro de datos;
- recuperación ante errores de actualización.

No se deben exponer directamente la base de datos ni servicios internos al cliente.

---

## 20. Acceso externo futuro

El acceso a Guardian desde fuera de la red doméstica **no forma parte de la arquitectura inicial**.

Si más adelante aparece una necesidad real —por ejemplo administrar Guardian cuando un dispositivo esté fuera de casa— podrá incorporarse una etapa específica.

Posibles alternativas futuras:

- túnel seguro;
- VPN;
- subdominio público;
- HTTPS;
- autenticación reforzada;
- políticas adicionales para dispositivos remotos.

La arquitectura inicial debe evitar decisiones que hagan imposible esta evolución, pero no debe implementar ahora infraestructura que todavía no se necesita.

---

## 21. Qué no se propone hacer todavía

Para evitar sobreconstrucción, en las primeras etapas no es necesario:

- incorporar IA;
- conectar n8n;
- crear un sistema complejo de recompensas;
- soportar tablet;
- implementar perfiles múltiples;
- construir analítica avanzada;
- generar desafíos automáticamente;
- hacer un dashboard sofisticado;
- crear microservicios;
- exponer Guardian públicamente en Internet;
- configurar túneles, VPN o subdominio público.

La prioridad es construir una base que permita incorporar esas capacidades después sin rehacer Guardian.

---

## 22. Próximo trabajo: especificación de Etapa 0

El siguiente documento deberá transformar la Etapa 0 en una especificación concreta y ejecutable.

Debe definir:

### Arquitectura

- componentes exactos;
- responsabilidades;
- comunicación;
- diagramas;
- dependencias.

### Backend

- tecnología;
- endpoints;
- autenticación;
- base de datos;
- modelo de datos;
- manejo de dispositivos;
- manejo de configuración;
- manejo de versiones.

### Guardian Client

- estrategia de consulta de configuración;
- almacenamiento local;
- comportamiento offline;
- identificación del dispositivo;
- reporte de versión.

### Updater

- mecanismo de actualización;
- verificación;
- descarga;
- reemplazo;
- rollback;
- reinicio;
- permisos de Windows.

### Admin

- login;
- pantalla de dispositivo;
- edición de configuración;
- estado de conexión;
- versión;
- actualización.

### Deploy

- repositorios;
- ambientes;
- dominio/subdominio;
- secretos;
- CI/CD;
- build;
- releases.

### Calidad

- pruebas;
- logs;
- observabilidad;
- manejo de errores;
- criterios de aceptación.

### Entrega a Codex

La especificación debe terminar convertida en:

1. alcance;
2. arquitectura;
3. estructura de carpetas;
4. contratos/API;
5. esquema de base de datos;
6. flujo de actualización;
7. historias o tareas de implementación;
8. criterios de aceptación;
9. pruebas mínimas;
10. orden recomendado de ejecución.

El objetivo es que Codex pueda implementar la Etapa 0 con la menor cantidad posible de decisiones implícitas.

---

# 23. Criterio rector

Cada nueva idea de Guardian deberá clasificarse antes de implementarse:

```text
¿Es configuración?
        │
        ├─ Sí → Admin/API
        │
        └─ No
             │
             ▼
¿Es contenido?
        │
        ├─ Sí → Base de datos/Admin
        │
        └─ No
             │
             ▼
¿Es una capacidad nueva?
        │
        └─ Sí → nueva versión de Guardian
```

Esta separación debe permitir que Guardian evolucione rápidamente sin que cada modificación implique tocar la computadora administrada o publicar software innecesariamente.
