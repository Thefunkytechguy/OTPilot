# OTPilot — Intune install script
# Copies OTPilot.exe to Program Files and creates a Start Menu shortcut.

$appName    = "OTPilot"
$installDir = "$env:ProgramFiles\$appName"
$exeName    = "OTPilot.exe"
$exeSource  = Join-Path $PSScriptRoot $exeName

# Create install directory
if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir | Out-Null
}

# Copy executable
Copy-Item -Path $exeSource -Destination "$installDir\$exeName" -Force

# Start Menu shortcut (all users)
$shortcutPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\$appName.lnk"
$shell    = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath       = "$installDir\$exeName"
$shortcut.WorkingDirectory = $installDir
$shortcut.Description      = "OTPilot TOTP Authenticator"
$shortcut.Save()

exit 0
