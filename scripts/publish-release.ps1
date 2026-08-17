param(
  [string]$Description = ""
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$version = (Get-Content -Raw -Path (Join-Path $root "VERSION")).Trim()
if ($version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$') {
  throw "VERSION debe usar SemVer MAJOR.MINOR.PATCH"
}

& (Join-Path $PSScriptRoot "self-test.ps1")

$releaseDir = Join-Path $root "release"
$packageDir = Join-Path $releaseDir "Guardian-$version"
$zipPath = Join-Path $releaseDir "Guardian-$version.zip"

& (Join-Path $PSScriptRoot "build.ps1")

if (Test-Path $packageDir) { Remove-Item -Recurse -Force -LiteralPath $packageDir }
New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
Copy-Item -Force (Join-Path $root "dist\Guardian.exe") (Join-Path $packageDir "Guardian.exe")
Copy-Item -Force (Join-Path $root "dist\Guardian.exe.config") (Join-Path $packageDir "Guardian.exe.config")
Copy-Item -Force (Join-Path $root "dist\GuardianUpdater.exe") (Join-Path $packageDir "GuardianUpdater.exe")
if (Test-Path $zipPath) { Remove-Item -Force -LiteralPath $zipPath }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($packageDir, $zipPath)

$hash = (Get-FileHash -Algorithm SHA256 -Path $zipPath).Hash.ToLowerInvariant()
Set-Content -Encoding ASCII -Path "$zipPath.sha256" -Value $hash

$serverReleaseDir = Join-Path $root "releases"
New-Item -ItemType Directory -Force -Path $serverReleaseDir | Out-Null
Copy-Item -Force $zipPath (Join-Path $serverReleaseDir (Split-Path -Leaf $zipPath))

$envPath = Join-Path $root ".env"
if (Test-Path $envPath) {
  docker compose --env-file $envPath -f (Join-Path $root "deploy\docker-compose.yml") exec -T guardian-app python -m server.app.register_release --version $version --file "/data/guardian/releases/$(Split-Path -Leaf $zipPath)" --notes $Description
} else {
  Write-Host "Release generado localmente. Para registrarlo en servidor, crea .env y levanta Guardian Server."
}

Write-Host "Release publicado manualmente: $zipPath"
Write-Host "SHA-256: $hash"
