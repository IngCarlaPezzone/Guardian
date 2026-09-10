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

La experiencia está dibujada y animada como vectores WPF offline. La narración española se distribuye como WAV embebido para que no dependa de las voces instaladas en cada PC.

## Telemetría

Además de `MissionStarted` y los eventos de bloqueo existentes, se registran:

- `VisualStoryStarted` al abrir la experiencia;
- `VisualStoryCompleted` cuando termina la línea de tiempo visual.

Ambos incluyen los identificadores de misión/categoría/nivel/skill/variante y la duración prevista. No incluyen ubicación privada.

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
9. Confirmar que la ventana bloquea, reproduce narración y animación, no ofrece respuesta y no muestra **SALIR** antes del final.
10. Confirmar que, tras aproximadamente 13,6 segundos, aparece **SALIR** y desbloquea Guardian.
11. En Activity STG, verificar `MissionStarted`, `VisualStoryStarted`, `VisualStoryCompleted`, `DeviceLocked` y `DeviceUnlocked`.
12. Dejar correr al menos un intervalo completo para confirmar el disparo por timer, no sólo el comando manual.

Esta POC no requiere migración ni endpoint nuevo y no modifica releases/updater.
