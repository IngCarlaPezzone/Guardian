# Timing y validación — Visual Story potencias de diez

El archivo fuente de narración es
`src/Guardian/Assets/Audio/multiply-by-10-100-1000.mp3`. Su duración efectiva
validada por `MediaPlayer.NaturalDuration` es **17,868 s**; no se aplicó
procesamiento ni recorte offline. El log `VisualStoryAudioStarted` deja ese
valor exacto en `audio_duration_seconds`.

| Segmento | Rango relativo | Narración / acción |
| --- | --- | --- |
| Apertura | 0,000–3,216 s (0–18 %) | ×10, ×100 y ×1000 entran rítmicamente. |
| Regla | 3,216–8,577 s (18–48 %) | `hint`; los ceros después del 1 reaccionan. |
| ×10 | 8,577–10,722 s (48–60 %) | Un 0 viaja y forma 230. |
| ×100 | 10,722–12,867 s (60–72 %) | Dos 0 viajan y forman 2300. |
| ×1000 | 12,867–15,012 s (72–84 %) | Tres 0 viajan y forman 23000. |
| Pregunta | 15,012–17,868 s (84–100 %) | `think`, `?` y ×1 000 000 sin resolver. |

Hay un hold visual adicional de 0,85 s tras la voz (duración total esperada:
**18,718 s**). El reloj visual se inicia sólo cuando `MediaPlayer` confirma que
el MP3 abrió, por lo que no adelanta la animación durante la carga del decoder.

Validación visual obligatoria: los objetos Guardian deben verse nítidos, los
ceros tienen que poder seguirse durante sus trayectorias y los números deben
explicar el patrón sin depender de texto. Si alguno de esos criterios falla, se
ajusta la composición antes de promover la build a STG.
