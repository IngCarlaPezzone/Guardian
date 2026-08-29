param(
  [string]$Description = "",
  [string]$Version = "0.1.0-staging-environment",
  [switch]$ReproduceProdVersion
)
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $root ".env.stg"
if (!(Test-Path $envPath)) { throw "Falta .env.stg. Este script nunca usa .env de PROD." }
if ($version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z][0-9A-Za-z.-]*)?$') { throw "Version debe usar SemVer con sufijo prerelease opcional." }
if (!$ReproduceProdVersion -and $version -notmatch '-') { throw "STG requiere una versión con sufijo (por ejemplo 0.1.0-staging-environment). Usa -ReproduceProdVersion sólo para reproducir una versión PROD." }

& (Join-Path $PSScriptRoot "self-test.ps1")
& (Join-Path $PSScriptRoot "build.ps1") -VersionOverride $version

$releaseDir = Join-Path $root "release"
$packageDir = Join-Path $releaseDir "Guardian-$version"
$zipPath = Join-Path $releaseDir "Guardian-$version.zip"
if (Test-Path $packageDir) { Remove-Item -Recurse -Force -LiteralPath $packageDir }
New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
Copy-Item -Force (Join-Path $root "dist\Guardian.exe") (Join-Path $packageDir "Guardian.exe")
Copy-Item -Force (Join-Path $root "dist\Guardian.exe.config") (Join-Path $packageDir "Guardian.exe.config")
Copy-Item -Force (Join-Path $root "dist\GuardianUpdater.exe") (Join-Path $packageDir "GuardianUpdater.exe")
Copy-Item -Recurse -Force (Join-Path $root "dist\Assets") (Join-Path $packageDir "Assets")
if (Test-Path $zipPath) { Remove-Item -Force -LiteralPath $zipPath }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($packageDir, $zipPath)
$hash = (Get-FileHash -Algorithm SHA256 -Path $zipPath).Hash.ToLowerInvariant()
Set-Content -Encoding ASCII -Path "$zipPath.sha256" -Value $hash

$stgReleaseDir = Join-Path $root "releases-stg"
New-Item -ItemType Directory -Force -Path $stgReleaseDir | Out-Null
Copy-Item -Force $zipPath (Join-Path $stgReleaseDir (Split-Path -Leaf $zipPath))
if ($ReproduceProdVersion) {
  docker compose -p guardian-stg --env-file $envPath -f (Join-Path $root "deploy\docker-compose.stg.yml") exec -T guardian-stg-app python -m server.app.register_release --version $version --file "/data/guardian/releases/$(Split-Path -Leaf $zipPath)" --notes $Description --allow-prod-version
} else {
  docker compose -p guardian-stg --env-file $envPath -f (Join-Path $root "deploy\docker-compose.stg.yml") exec -T guardian-stg-app python -m server.app.register_release --version $version --file "/data/guardian/releases/$(Split-Path -Leaf $zipPath)" --notes $Description
}
Write-Host "Release publicada solo en STG: $zipPath"
