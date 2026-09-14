# Guardian — Visual Story: potencias de diez

## Alcance actual

La única Visual Story de esta POC es una microanimación no interactiva sobre la
multiplicación de `23` por `10`, `100` y `1000`. Reutiliza el selector de
misiones, RemoteConfig, intervalo/test interval, bloqueo, pausa de multimedia
externa, telemetría y salida de Guardian. Matemática y Comprensión no cambian.

Cuando están habilitadas juntas Visual Story y la habilidad matemática
`multiply_by_powers_of_ten`, Guardian las encadena en dos intervalos normales:
cuando el selector elige esa habilidad muestra primero la story y se cierra con
**SALIR**. La multiplicación real queda pendiente para el próximo intervalo;
Guardian se desbloquea entre ambas. Si sólo se habilita Visual Story, conserva
el comportamiento independiente.

- Skill: `visual_story.math.multiply_by_10_100_1000`
- Categoría / nivel: `visual_story` / `math_primer`
- Audio: `src/Guardian/Assets/Audio/multiply-by-10-100-1000.mp3`
- Renderer: WPF nativo; no hay navegador, mapa, TTS ni servicio externo en
  tiempo de ejecución.

El audio original se conserva sin recortar ni alterar. `MediaPlayer` obtiene su
duración real al abrirlo y la línea de tiempo visual toma ese valor, más un hold
final de 0,85 segundos. Los segmentos son proporcionales al audio: apertura
0–18 %, descubrimiento 18–48 %, ejemplos ×10 / ×100 / ×1000 48–60 % /
60–72 % / 72–84 %, y pregunta final 84–100 %. El evento
`VisualStoryAudioStarted` registra `audio_duration_seconds` con el valor real.

## Guardian Visual Language

`docs/GUARDIAN_VISUAL_LANGUAGE_v2.md` es la fuente de verdad. Cada dígito y
signo se carga individualmente desde `src/Guardian/Assets/Icons/`:

- `guardian-number-0.png` a `guardian-number-9.png`;
- `guardian-sign-multiply.png`, `guardian-sign-equals.png` y
  `guardian-sign-question.png`;
- `hint.png` y `think.png` como apoyo narrativo.

Los PNG no se recolorean ni se convierten en una ecuación única. Los ceros
viajan por curvas Bézier como los mismos objetos que pertenecían a cada
multiplicador y aterrizan detrás de `23` con glow, squash y settle.

## Aprendizaje de la POC geográfica

La exploración geográfica basada en imágenes y zoom/crossfade validó la
integración de Visual Story, pero su montaje se percibía como una presentación.
El prototipo Cesium no ofreció, dentro del alcance acotado, una dirección
artística aceptable y añadía dependencias de cartografía/red. Su código y sus
dependencias específicas no forman parte de esta rama. Se conservó únicamente
la infraestructura de misión, preview, audio y diagnóstico que sí aporta a la
POC actual.

## Audio durante una interrupción

En un disparo automático, `MediaInterruptionSession` envía la solicitud de
pausa al reproductor multimedia externo activo antes de abrir una
`visual_story` (`MediaPauseRequested` con razón
`visual_story_external_media`). No aplica el mute global del endpoint de
Windows y deja registrado `SystemAudioMuteSkipped`; así la narración propia
puede escucharse. El preview/manual no altera multimedia ajena. Los eventos de
diagnóstico son `VisualStoryAudioLoading`, `VisualStoryAudioStarted`,
`VisualStoryAudioCompleted`, `VisualStoryAudioStopped` y
`VisualStoryAudioFailed`.

## Preview y capturas

Después de compilar:

```powershell
.\scripts\preview-visual-story.ps1
```

El preview usa exactamente `MultiplyBy100StorySurface`, el mismo renderer de la
interrupción. Reproduce automáticamente; **REINICIAR** vuelve a comenzar y la
línea de tiempo permite scrub/inspección.

Para las capturas deterministas:

```powershell
.\scripts\preview-visual-story.ps1 -Capture -OutputDirectory .\docs\assets\visual-story-powers-of-ten
```

Se generan apertura, regla con hint, viaje ×10, resultados ×100 y ×1000 y
cierre con think/pregunta. No son assets de producción.

Para reproducir el flujo de audio y mute de una interrupción sin timer ni
servidor:

```powershell
.\dist\Guardian.exe --visual-story-interruption-preview
```

## Validación STG manual

No ejecutar en PROD ni en un dispositivo productivo.

1. Instalar el ZIP STG preparado en un directorio de prueba de PC TEST.
2. Iniciar el cliente con configuración y URL de **STG** exclusivamente.
3. En Guardian Admin — STG, habilitar **Visual Story matemática → Multiplicar
   por 10, 100 y 1000** y **Matemática → Operaciones básicas → Multiplicaciones
   por 10, 100 y 1000**. Para validar el encadenamiento, desmarcar las demás
   habilidades.
4. Guardar, esperar RemoteConfig o usar **Probar misión ahora**.
5. En el primer intervalo, confirmar bloqueo, animación, narración audible,
   ausencia de interacción y **SALIR** al final.
6. Confirmar que Guardian se desbloquea al cerrar la story y que no se abre una
   multiplicación inmediatamente.
7. Esperar el siguiente intervalo de prueba: debe aparecer una multiplicación
   real por 10, 100 o 1000. Resolverla y confirmar el desbloqueo normal.
8. En Activity, verificar que se registren
   `VisualStoryStarted`, `VisualStoryAudioStarted`,
   `VisualStoryAudioCompleted`, `VisualStoryCompleted`,
   `VisualStoryPrimerScheduled` y, en el segundo intervalo, los eventos de la
   misión matemática.
