# OTPilot — Build + package for Intune deployment
# Prerequisites:
#   - .NET 8 SDK
#   - IntuneWinAppUtil.exe in PATH or same folder as this script
#     Download: https://github.com/microsoft/Microsoft-Win32-Content-Prep-Tool/releases

$ErrorActionPreference = "Stop"

$repoRoot    = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $repoRoot "src\OTPilot\OTPilot.csproj"
$publishDir  = Join-Path $repoRoot "src\OTPilot\bin\Release\net8.0-windows\publish\win-x64"
$packageDir  = Join-Path $PSScriptRoot "package"
$outputDir   = Join-Path $PSScriptRoot "output"

Write-Host "==> Building OTPilot (Release, self-contained)..." -ForegroundColor Cyan
dotnet publish $projectPath --configuration Release --runtime win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true --output $publishDir

# Assemble the package folder (exe + install scripts)
if (Test-Path $packageDir) { Remove-Item $packageDir -Recurse -Force }
New-Item -ItemType Directory -Path $packageDir | Out-Null

Copy-Item "$publishDir\OTPilot.exe"   "$packageDir\OTPilot.exe"
Copy-Item "$PSScriptRoot\install.ps1"   "$packageDir\install.ps1"
Copy-Item "$PSScriptRoot\uninstall.ps1" "$packageDir\uninstall.ps1"

# Locate IntuneWinAppUtil
$intuneUtil = Get-Command "IntuneWinAppUtil.exe" -ErrorAction SilentlyContinue
if (-not $intuneUtil) {
    $intuneUtil = Join-Path $PSScriptRoot "IntuneWinAppUtil.exe"
    if (-not (Test-Path $intuneUtil)) {
        Write-Error "IntuneWinAppUtil.exe not found. Download it from https://github.com/microsoft/Microsoft-Win32-Content-Prep-Tool/releases and place it next to this script."
    }
} else {
    $intuneUtil = $intuneUtil.Source
}

if (-not (Test-Path $outputDir)) { New-Item -ItemType Directory -Path $outputDir | Out-Null }

Write-Host "==> Wrapping with IntuneWinAppUtil..." -ForegroundColor Cyan
& $intuneUtil -c $packageDir -s "OTPilot.exe" -o $outputDir -q

Write-Host ""
Write-Host "Done! Upload this file to Intune:" -ForegroundColor Green
Write-Host "  $outputDir\OTPilot.intunewin" -ForegroundColor Yellow
Write-Host ""
Write-Host "Intune app settings:" -ForegroundColor Cyan
Write-Host "  Install cmd   : powershell.exe -ExecutionPolicy Bypass -File install.ps1"
Write-Host "  Uninstall cmd : powershell.exe -ExecutionPolicy Bypass -File uninstall.ps1"
Write-Host "  Detection rule: File exists  %ProgramFiles%\OTPilot\OTPilot.exe"
