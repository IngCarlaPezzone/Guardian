GUARDIAN - INSTALACION FACIL

OPCION RECOMENDADA

1. Descomprimir GuardianInstaller.zip.
2. Entrar a la carpeta GuardianInstaller.
3. Hacer doble click en Guardian.exe.
4. Si es una primera instalacion, Guardian abre una ventana para indicar:
   - URL del Guardian Server, por ejemplo http://NOMBRE-SERVIDOR:8080
   - bootstrap token provisto por el administrador
5. Dejar marcada la opcion de iniciar automaticamente con Windows si se desea autoarranque.
6. Windows puede mostrar una advertencia porque es una app local. Elegir "Mas informacion" y luego "Ejecutar de todas formas" si aparece.
7. Guardian queda configurado para el usuario actual de Windows.
8. Buscar el icono de Guardian en la bandeja, cerca del reloj. Si no se ve, abrir la flechita de iconos ocultos.

El bootstrap token no viene dentro del ZIP. Se ingresa durante la instalacion y Guardian lo borra de config.json despues de registrar correctamente el dispositivo.

Si ya existe %LOCALAPPDATA%\Guardian\config.json, el instalador lo conserva. No cambia DeviceId, DeviceToken ni GuardianServerUrl.

VERSION FINAL

El instalador deja Guardian configurado para uso real:

- La mision aparece cada 15 minutos.
- El intervalo de prueba de 60 segundos queda disponible, pero apagado.

Para volver temporalmente a prueba rapida, abrir:

%LOCALAPPDATA%\Guardian\config.json

y cambiar:

"UseTestInterval": false

por:

"UseTestInterval": true

Luego cerrar y volver a abrir Guardian desde la bandeja o reiniciar sesion.

Credenciales admin iniciales:

Usuario: admin
Contrasena: guardian

Desde el icono de bandeja se puede abrir el panel admin, pausar Guardian, activarlo, probar una mision o salir.

Para quitar Guardian, cerrar Guardian desde la bandeja y borrar la carpeta descomprimida. Para quitar autoarranque, ejecutar desde esa carpeta:

Guardian.exe --uninstall-startup

Por defecto se conserva %LOCALAPPDATA%\Guardian\config.json y events.jsonl para no perder configuracion/historial por accidente.

Para una instalacion limpia en una PC que tenia una version vieja, borrar manualmente %LOCALAPPDATA%\Guardian despues de cerrar Guardian. Eso elimina config.json, identidad y eventos locales, y la proxima ejecucion registrara la PC como dispositivo nuevo.
