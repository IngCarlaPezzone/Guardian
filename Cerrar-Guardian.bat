@echo off
setlocal

set "ROOT=%~dp0"

taskkill /IM Guardian.exe /F >nul 2>nul
"%ROOT%dist\Guardian.exe" --unmute-audio

echo Guardian cerrado y audio desmuteado.
pause
