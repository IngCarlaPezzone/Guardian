$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "dist\Guardian.exe"

if (!(Test-Path $exe)) {
  throw "No se encontro $exe"
}

& $exe --uninstall-startup
