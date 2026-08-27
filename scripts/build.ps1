param([string]$VersionOverride = "")

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root "src\Guardian\Guardian.cs"
$missionSrc = Join-Path $root "src\Guardian\MissionSystem.cs"
$installPathsSrc = Join-Path $root "src\Guardian\GuardianInstallPaths.cs"
$updaterSrc = Join-Path $root "updater\src\GuardianUpdater.cs"
$outDir = Join-Path $root "dist"
$objDir = Join-Path $root "obj"
$out = Join-Path $outDir "Guardian.exe"
$updaterOut = Join-Path $outDir "GuardianUpdater.exe"
$version = if ($VersionOverride) { $VersionOverride } else { (Get-Content -Raw -Path (Join-Path $root "VERSION")).Trim() }
if ($version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z][0-9A-Za-z.-]*)?$') {
  throw "La versión debe usar SemVer, con sufijo prerelease opcional."
}
$guardianVersionSrc = Join-Path $objDir "GuardianVersionInfo.g.cs"
$updaterVersionSrc = Join-Path $objDir "UpdaterVersionInfo.g.cs"
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$wpf = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\WPF"

if (!(Test-Path $csc)) {
  throw "No se encontro csc.exe en $csc"
}

foreach ($dll in @("PresentationCore.dll", "PresentationFramework.dll", "WindowsBase.dll")) {
  if (!(Test-Path (Join-Path $wpf $dll))) {
    throw "No se encontro $dll en $wpf"
  }
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
New-Item -ItemType Directory -Force -Path $objDir | Out-Null

Set-Content -Encoding UTF8 -Path $guardianVersionSrc -Value "namespace Guardian { public static class VersionInfo { public const string Version = `"$version`"; } }"
Set-Content -Encoding UTF8 -Path $updaterVersionSrc -Value "namespace GuardianUpdater { public static class VersionInfo { public const string Version = `"$version`"; } }"

& $csc `
  /nologo `
  /target:winexe `
  /out:$out `
  /reference:"$(Join-Path $wpf 'PresentationCore.dll')" `
  /reference:"$(Join-Path $wpf 'PresentationFramework.dll')" `
  /reference:"$(Join-Path $wpf 'WindowsBase.dll')" `
  /reference:System.Xaml.dll `
  /reference:System.Windows.Forms.dll `
  /reference:System.Drawing.dll `
  /reference:System.Web.Extensions.dll `
  $src `
  $missionSrc `
  $installPathsSrc `
  $guardianVersionSrc

if ($LASTEXITCODE -ne 0) {
  throw "Build fallo con codigo $LASTEXITCODE"
}

Copy-Item -Force (Join-Path $root "src\Guardian\App.config") "$out.config"
$assetOutputDir = Join-Path $outDir "Assets\Icons"
New-Item -ItemType Directory -Force -Path $assetOutputDir | Out-Null
Copy-Item -Force (Join-Path $root "src\Guardian\Assets\Icons\help-lightbulb.png") (Join-Path $assetOutputDir "help-lightbulb.png")
& $csc `
  /nologo `
  /target:winexe `
  /out:$updaterOut `
  /reference:System.Web.Extensions.dll `
  /reference:System.IO.Compression.dll `
  /reference:System.IO.Compression.FileSystem.dll `
  $updaterSrc `
  $installPathsSrc `
  $updaterVersionSrc

if ($LASTEXITCODE -ne 0) {
  throw "Build de GuardianUpdater fallo con codigo $LASTEXITCODE"
}

if (!(Test-Path $out)) {
  throw "Build no genero $out"
}
if (!(Test-Path $updaterOut)) {
  throw "Build no genero $updaterOut"
}

Write-Host "Build OK: $out"
Write-Host "Build OK: $updaterOut"
