# OTPilot — Intune uninstall script

$appName    = "OTPilot"
$installDir = "$env:ProgramFiles\$appName"
$shortcut   = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\$appName.lnk"

# Kill any running instance first
Get-Process -Name "OTPilot" -ErrorAction SilentlyContinue | Stop-Process -Force

# Remove install directory
if (Test-Path $installDir) {
    Remove-Item -Path $installDir -Recurse -Force
}

# Remove Start Menu shortcut
if (Test-Path $shortcut) {
    Remove-Item -Path $shortcut -Force
}

exit 0
