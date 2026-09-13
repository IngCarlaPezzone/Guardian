param(
  [switch]$Capture,
  [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "dist\Guardian.exe"
if (!(Test-Path $exe)) {
  & (Join-Path $PSScriptRoot "build.ps1") -VersionOverride "0.4.11-staging-guardian-visual-language.1"
}

if ($Capture) {
  $output = if ($OutputDirectory) { $OutputDirectory } else { Join-Path $root "docs\assets\visual-story-poc" }
  $process = Start-Process -FilePath $exe -ArgumentList @("--visual-story-capture", $output) -WorkingDirectory $root -PassThru -Wait
  if ($process.ExitCode -ne 0) { throw "No se pudieron generar las capturas de Visual Story." }
  Write-Host "Capturas generadas en: $output"
  return
}

Start-Process -FilePath $exe -ArgumentList "--visual-story-preview" -WorkingDirectory $root | Out-Null
Write-Host "Preview iniciado. Usá REINICIAR o la línea de tiempo para inspeccionar la story."
