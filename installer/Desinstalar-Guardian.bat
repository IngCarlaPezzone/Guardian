@echo off
setlocal

set "APP_DIR=%LOCALAPPDATA%\Guardian"

taskkill /IM Guardian.exe /F >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v Guardian /f >nul 2>nul

if exist "%APP_DIR%\Guardian.exe" del /F /Q "%APP_DIR%\Guardian.exe" >nul 2>nul
if exist "%APP_DIR%\GuardianUpdater.exe" del /F /Q "%APP_DIR%\GuardianUpdater.exe" >nul 2>nul
if exist "%APP_DIR%\Guardian.exe.config" del /F /Q "%APP_DIR%\Guardian.exe.config" >nul 2>nul
if exist "%APP_DIR%\intentional-exit.flag" del /F /Q "%APP_DIR%\intentional-exit.flag" >nul 2>nul

set "DELETE_DATA=%GUARDIAN_UNINSTALL_DELETE_DATA%"
if not defined DELETE_DATA (
  echo.
  echo Queres borrar tambien configuracion, identidad y eventos locales?
  echo.
  echo Elegi N para una actualizacion o desinstalacion normal.
  echo Elegi S solo para instalacion limpia o para registrar esta PC como dispositivo nuevo.
  echo.
  set /p "DELETE_DATA=Borrar datos locales de Guardian? (S/N): "
)

if /I "%DELETE_DATA%"=="S" set "DELETE_DATA=1"
if "%DELETE_DATA%"=="1" (
  if exist "%APP_DIR%" rmdir /S /Q "%APP_DIR%" >nul 2>nul
  echo Guardian desinstalado para este usuario de Windows.
  echo.
  echo Se eliminaron binarios, autoarranque, config.json, identidad y eventos locales.
  echo La proxima instalacion registrara esta PC como un dispositivo nuevo.
  echo.
  if not "%GUARDIAN_INSTALL_NO_PAUSE%"=="1" pause
  exit /b 0
)

echo Guardian desinstalado para este usuario de Windows.
echo.
echo Se conservaron config.json y events.jsonl si existian, para no borrar historial/configuracion por accidente.
echo Carpeta de datos: %APP_DIR%
echo.
if not "%GUARDIAN_INSTALL_NO_PAUSE%"=="1" pause
