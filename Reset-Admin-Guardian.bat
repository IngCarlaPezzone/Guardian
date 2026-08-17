@echo off
setlocal

set "ROOT=%~dp0"

"%ROOT%dist\Guardian.exe" --reset-admin

echo.
echo Si Guardian ya estaba abierto, cerralo y volvelo a abrir para que tome la credencial restaurada.
echo Usuario: admin
echo Contrasena: guardian
echo.
pause
