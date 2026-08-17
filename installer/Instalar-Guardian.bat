@echo off
setlocal

set "SOURCE_DIR=%~dp0"
set "APP_DIR=%LOCALAPPDATA%\Guardian"
set "CONFIG_PATH=%APP_DIR%\config.json"
set "EXE_SOURCE=%SOURCE_DIR%Guardian.exe"
set "UPDATER_SOURCE=%SOURCE_DIR%GuardianUpdater.exe"
set "CONFIG_SOURCE=%SOURCE_DIR%Guardian.exe.config"

if not exist "%EXE_SOURCE%" (
  if exist "%SOURCE_DIR%..\dist\Guardian.exe" (
    set "EXE_SOURCE=%SOURCE_DIR%..\dist\Guardian.exe"
    set "UPDATER_SOURCE=%SOURCE_DIR%..\dist\GuardianUpdater.exe"
    set "CONFIG_SOURCE=%SOURCE_DIR%..\dist\Guardian.exe.config"
  )
)

if not exist "%EXE_SOURCE%" (
  echo No se encontro Guardian.exe.
  echo.
  echo Probablemente abriste la carpeta installer del codigo fuente.
  echo Esa carpeta tiene solo plantillas.
  echo.
  echo Para instalar en otra computadora, copia esta carpeta completa:
  echo   release\GuardianInstaller
  echo.
  echo O copia/descomprime este archivo:
  echo   release\GuardianInstaller.zip
  echo.
  echo Esa version incluye Guardian.exe.
  echo.
  if not "%GUARDIAN_INSTALL_NO_PAUSE%"=="1" pause
  exit /b 1
)

taskkill /IM Guardian.exe /F >nul 2>&1
mkdir "%APP_DIR%" >nul 2>nul
copy /Y "%EXE_SOURCE%" "%APP_DIR%\Guardian.exe" >nul 2>nul
if errorlevel 1 (
  echo No se pudo copiar Guardian.exe a %APP_DIR%.
  if not "%GUARDIAN_INSTALL_NO_PAUSE%"=="1" pause
  exit /b 1
)
if exist "%UPDATER_SOURCE%" (
  copy /Y "%UPDATER_SOURCE%" "%APP_DIR%\GuardianUpdater.exe" >nul 2>nul
  if errorlevel 1 (
    echo No se pudo copiar GuardianUpdater.exe a %APP_DIR%.
    if not "%GUARDIAN_INSTALL_NO_PAUSE%"=="1" pause
    exit /b 1
  )
)
if exist "%CONFIG_SOURCE%" (
  copy /Y "%CONFIG_SOURCE%" "%APP_DIR%\Guardian.exe.config" >nul 2>nul
  if errorlevel 1 (
    echo No se pudo copiar Guardian.exe.config a %APP_DIR%.
    if not "%GUARDIAN_INSTALL_NO_PAUSE%"=="1" pause
    exit /b 1
  )
)

if exist "%CONFIG_PATH%" (
  echo Configuracion existente detectada.
  echo Se conserva %CONFIG_PATH%
  echo No se reemplazan DeviceId, DeviceToken ni GuardianServerUrl.
) else (
  echo.
  echo Primera instalacion de Guardian.
  echo Se abrira una ventana para ingresar la URL del servidor y el bootstrap token.
  echo El token no viene dentro del instalador y Guardian lo borrara luego del registro exitoso.
  echo.
  "%APP_DIR%\Guardian.exe" --configure-install
  if errorlevel 1 (
    echo.
    echo Instalacion cancelada o configuracion inicial incompleta.
    if not "%GUARDIAN_INSTALL_NO_PAUSE%"=="1" pause
    exit /b 1
  )
)

set "AUTOSTART_OK=1"
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v Guardian /t REG_SZ /d "\"%APP_DIR%\Guardian.exe\"" /f >nul 2>nul
if errorlevel 1 (
  set "AUTOSTART_OK=0"
  echo Advertencia: no se pudo registrar Guardian para autoarranque.
  echo Podes volver a ejecutar el instalador o iniciar Guardian manualmente desde %APP_DIR%.
)

start "" "%APP_DIR%\Guardian.exe"

echo Guardian instalado correctamente.
echo.
if "%AUTOSTART_OK%"=="1" (
  echo Va a iniciar automaticamente con este usuario de Windows.
) else (
  echo Guardian se inicio ahora, pero el autoarranque no quedo registrado.
)
echo Busca el icono de Guardian en la bandeja, cerca del reloj.
echo.
echo Credenciales admin iniciales:
echo   Usuario: admin
echo   Contrasena: guardian
echo.
echo Version final: la mision aparece cada 15 minutos.
echo Para pruebas rapidas, cambia UseTestInterval a true en config.json.
echo Config: %APP_DIR%\config.json
echo.
echo Cambia esas credenciales antes de usarlo en serio.
echo.
if not "%GUARDIAN_INSTALL_NO_PAUSE%"=="1" pause
exit /b 0
