# Guardian — POC Visual Story “Dónde vivís”

## Objetivo y alcance

Esta POC agrega una única experiencia visual no interactiva al flujo de misiones existente. Su objetivo es validar el disparo como misión y la calidad visual de un recorrido de 13,6 segundos:

```text
Tierra → América del Sur → Argentina → Neuquén → Plottier → hogar
```

Quedan fuera de alcance templates genéricos, generación automática, IA, múltiples temáticas, geolocalización real y cambios de arquitectura.

## Integración

- Skill remota: `visual_story.location.home`.
- Categoría de telemetría: `visual_story`.
- Nivel: `location`.
- Variante única: `where_you_live_plottier`.
- Usa el selector, la rotación, el intervalo/test interval, el bloqueo, RemoteConfig y la cola local-first existentes.
- Admin permite dejar activa solamente **Visual Story → Microhistoria → Dónde vivís**.
- Durante la reproducción no hay respuesta ni interacción de misión.
- La salida aparece al finalizar. La salida admin de emergencia se conserva.
- La casa usa una ubicación visual aproximada y conceptual; no se guarda ni representa un domicilio real.

La experiencia usa cinco PNG aprobados y embebidos como recursos WPF offline: Tierra/América del Sur, Argentina, Neuquén, llegada a Plottier y hogar. El montaje mantiene zoom y pan lentos en cada asset, con solapes de aproximadamente 0,7 segundos entre escenas; no usa mapas vectoriales ni genera imágenes nuevas.

La narración se distribuye como WAV embebido. No hay TTS en tiempo de ejecución. Al validar esta iteración, el sistema sólo reportó voces `es-ES`; no había `es-AR` ni una voz latinoamericana instalada. El WAV existente queda como fallback temporal y debe reemplazarse cuando exista una voz local rioplatense/latinoamericana adecuada o se apruebe un proveedor futuro.

## Audio durante la interrupción

La ausencia de audio en la interrupción real tenía una causa concreta: `MediaInterruptionSession` silenciaba el endpoint global de Windows antes de crear la Visual Story. El preview aislado no crea esa sesión, por eso sí sonaba.

La corrección conserva la pausa multimedia externa y el mute global para las misiones normales. Sólo cuando la misión es `visual_story` se registra `SystemAudioMuteSkipped` y se preserva el audio propio de la narración.

## Telemetría

Además de `MissionStarted` y los eventos de bloqueo existentes, se registran:

- `VisualStoryStarted` al abrir la experiencia;
- `VisualStoryCompleted` cuando termina la línea de tiempo visual.

Ambos incluyen los identificadores de misión/categoría/nivel/skill/variante y la duración prevista. No incluyen ubicación privada.

Para diagnóstico DEV se suman `VisualStoryAudioLoading`, `VisualStoryAudioStarted`, `VisualStoryAudioCompleted`, `VisualStoryAudioStopped` y `VisualStoryAudioFailed`. Incluyen el recurso, duración, locale de la narración, salida multimedia por defecto y el estado de mute del sistema.

## Preview y capturas

Después de compilar:

```powershell
.\scripts\preview-visual-story.ps1
```

El preview reproduce automáticamente narración y animación. **REINICIAR** vuelve al inicio y la línea de tiempo permite pausar e inspeccionar cualquier instante.

Para regenerar los cinco fotogramas representativos:

```powershell
.\scripts\preview-visual-story.ps1 -Capture
```

Se escriben en `docs/assets/visual-story-poc/` y corresponden a Tierra/América del Sur, Argentina, Neuquén, llegada a Plottier y hogar.

Para reproducir específicamente el camino que antes fallaba —sesión de interrupción con `MuteSystemAudioDuringMission=true`— sin servidor ni timer:

```powershell
.\dist\Guardian.exe --visual-story-interruption-preview
```

Ese comando usa un directorio temporal aislado cuando no se pasa `--home`.

## Validación en PC TEST de STG

No ejecutar estos pasos sobre PROD ni sobre el dispositivo productivo final.

1. Desde esta feature branch, ejecutar:

   ```powershell
   .\scripts\build.ps1
   .\scripts\self-test.ps1
   & .\.venv\Scripts\python.exe -m pytest server\tests
   ```

2. Con `.env.stg` configurado con secretos exclusivamente STG, actualizar sólo STG:

   ```powershell
   .\scripts\update-stg.ps1
   ```

3. Abrir `http://localhost:8081/admin/` y confirmar el indicador **STG**.
4. Iniciar el cliente aislado:

   ```powershell
   .\scripts\run-stg-client.ps1
   ```

5. En **Guardian Admin — STG**, abrir la configuración del dispositivo `Guardian-STG-TEST`/PC TEST.
6. Desmarcar todas las habilidades de Matemática y Comprensión.
7. Marcar solamente **Visual Story → Microhistoria → Dónde vivís**, elegir el intervalo de prueba y guardar.
8. Esperar a que RemoteConfig llegue al cliente o usar **Probar misión ahora** para la primera validación.
9. Confirmar que la ventana bloquea, reproduce narración y animación, no ofrece respuesta y no muestra **SALIR** antes del final. Confirmar específicamente que el audio se escucha aun con el comportamiento normal de pausa multimedia configurado.
10. Confirmar que, tras aproximadamente 13,6 segundos, aparece **SALIR** y desbloquea Guardian.
11. En Activity STG, verificar `MissionStarted`, `VisualStoryStarted`, `VisualStoryAudioStarted`, `VisualStoryAudioCompleted`, `VisualStoryCompleted`, `DeviceLocked` y `DeviceUnlocked`. Ante un problema de sonido, comprobar `SystemAudioMuteSkipped` y `VisualStoryAudioFailed`.
12. Dejar correr al menos un intervalo completo para confirmar el disparo por timer, no sólo el comando manual.

Esta POC no requiere migración ni endpoint nuevo y no modifica releases/updater.
