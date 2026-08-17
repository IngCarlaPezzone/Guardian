param(
  [switch]$IncludeExperimentalExe
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$releaseDir = Join-Path $root "release"
$packageDir = Join-Path $releaseDir "GuardianInstaller"
$zipPath = Join-Path $releaseDir "GuardianInstaller.zip"
$setupOut = Join-Path $releaseDir "GuardianSetup.exe"
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

& (Join-Path $PSScriptRoot "build.ps1")

if (Test-Path $packageDir) {
  Remove-Item -Recurse -Force -LiteralPath $packageDir
}
New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
Copy-Item -Force (Join-Path $root "dist\Guardian.exe") (Join-Path $packageDir "Guardian.exe")
Copy-Item -Force (Join-Path $root "dist\Guardian.exe.config") (Join-Path $packageDir "Guardian.exe.config")
Copy-Item -Force (Join-Path $root "dist\GuardianUpdater.exe") (Join-Path $packageDir "GuardianUpdater.exe")
Copy-Item -Force (Join-Path $root "installer\README-INSTALACION.txt") (Join-Path $packageDir "README-INSTALACION.txt")

if (Test-Path $zipPath) {
  Remove-Item -Force -LiteralPath $zipPath
}
if (Test-Path $setupOut) {
  Remove-Item -Force -LiteralPath $setupOut
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($packageDir, $zipPath)

if ($IncludeExperimentalExe) {
  & $csc `
    /nologo `
    /target:winexe `
    /out:$setupOut `
    /reference:System.Windows.Forms.dll `
    /resource:"$(Join-Path $root 'dist\Guardian.exe')",Guardian.exe `
    /resource:"$(Join-Path $root 'dist\Guardian.exe.config')",Guardian.exe.config `
    /resource:"$(Join-Path $root 'dist\GuardianUpdater.exe')",GuardianUpdater.exe `
    (Join-Path $root "installer\GuardianSetup.cs")

  if ($LASTEXITCODE -ne 0) {
    throw "Build de GuardianSetup fallo con codigo $LASTEXITCODE"
  }
}

Write-Host "Paquete generado:"
Write-Host "  $packageDir"
Write-Host "  $zipPath"
if ($IncludeExperimentalExe) {
  Write-Host "  $setupOut"
  Write-Host "Nota: GuardianSetup.exe es experimental y puede ser bloqueado por antivirus si no esta firmado."
}
