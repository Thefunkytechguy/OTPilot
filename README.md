# OTP Pilot

> A secure, roaming-friendly TOTP authenticator for enterprise hot-desk environments.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-0078D4)](https://github.com/Thefunkytechguy/OTPilot)
[![Version](https://img.shields.io/badge/Version-1.3.0-00A0DF)](https://github.com/Thefunkytechguy/OTPilot/releases)

---

## The Problem

In a PCI-compliant call centre, **mobile phones are not permitted on the floor**. Staff hot-desk across multiple workstations every shift, and existing tools like WinOTP store TOTP secrets **locally on the machine** — meaning users have to re-enrol their accounts on every machine they sit at. Lost or reimaged machines result in permanent loss of accounts, and IT support spends time on repeated re-enrolment requests.

**OTP Pilot fixes this.** Your accounts follow you wherever you log in.

---

## How It Works

OTP Pilot stores your encrypted TOTP vault in **OneDrive for Business**. Because OneDrive is tied to your Entra ID (Azure AD) account, your vault is automatically available on any machine you sign into — no extra setup, no master password, no IT involvement.

```
[Any Workstation] ──► [OneDrive for Business] ──► [AES-256-GCM encrypted vault]
                              │
                    Protected by Entra ID SSO
                    (your Windows login = access)
```

Works without OneDrive too — a local storage mode encrypts the vault using Windows DPAPI, tied to your user account.

---

## Features

- **Roaming vault** — TOTP accounts follow you across all hot-desk machines via OneDrive for Business
- **No master password** — Entra ID and Windows DPAPI act as the authentication gate
- **AES-256-GCM encryption** — every secret is encrypted at rest with authenticated encryption
- **QR code enrolment** — scan directly from your screen (no camera needed)
- **Manual entry** — add accounts by typing the secret key
- **System tray** — lives quietly in the tray; codes are always one click away
- **Single instance** — opening a second window brings the existing one to focus
- **Offline support** — DPAPI-cached key means the app works without a network connection
- **Intune deployment** — silent Win32 app install, no user interaction required
- **Open source** — every line is auditable

---

## Security

| What | How |
|---|---|
| Vault encryption | AES-256-GCM with a unique nonce per write |
| Key generation | `RandomNumberGenerator.Fill()` — cryptographically secure |
| Key storage (cloud) | Raw key in OneDrive, access-gated by Entra ID |
| Key storage (local) | Windows DPAPI `CurrentUser` scope — user and machine bound |
| In-transit protection | TLS 1.2/1.3 enforced by OneDrive / SharePoint endpoints |
| No custom backend | Zero new network attack surface — uses existing Microsoft infrastructure |
| No telemetry | No analytics, no callbacks, no clipboard logging |

All cryptographic operations use **Microsoft's own `System.Security.Cryptography` namespace** — no third-party crypto libraries.

For a full security and architecture writeup, see the [Design Document](https://github.com/Thefunkytechguy/OTPilot).

---

## Requirements

- Windows 10 / 11
- .NET 8 Runtime (included in the Intune package)
- OneDrive for Business (optional — local mode available)
- Azure AD / Entra ID joined device (recommended for roaming)

---

## Installation

### Via Intune (Recommended for Enterprise)

1. Build the package using the provided script:
   ```powershell
   cd Intune
   .\build-intune-package.ps1
   ```
2. Upload `Intune\output\OTPilot.intunewin` to [intune.microsoft.com](https://intune.microsoft.com) as a **Windows app (Win32)**
3. Use the following settings:

   | Setting | Value |
   |---|---|
   | Install command | `powershell.exe -ExecutionPolicy Bypass -File install.ps1` |
   | Uninstall command | `powershell.exe -ExecutionPolicy Bypass -File uninstall.ps1` |
   | Install behaviour | System |
   | Detection rule | File exists: `%ProgramFiles%\OTPilot\OTPilot.exe` |

4. Assign to your device group — Intune handles the rest silently.

### Manual Install

1. Download the latest release from the [Releases](https://github.com/Thefunkytechguy/OTPilot/releases) page
2. Run `install.ps1` as Administrator, or copy the folder to `%ProgramFiles%\OTPilot\`
3. Launch `OTPilot.exe`

---

## First Run

On first launch, a setup wizard will ask where to store your vault:

| Option | Best for |
|---|---|
| **OneDrive for Business** *(recommended)* | Hot-desk users — vault follows you everywhere |
| **Local storage** | Offline / single-machine use |
| **Custom path** | Mapped drives or network shares |

This is a one-time choice per machine. It can be reset by deleting `%LocalAppData%\OTPilot\config.json`.

---

## Adding Accounts

**QR Code (recommended)**
1. Open OTP Pilot and click the **+** button
2. Click **Scan QR Code**
3. Draw a rectangle around the QR code on screen
4. Details are filled in automatically — click **Add**

**Manual Entry**
1. Click the **+** button
2. Enter the account name, issuer, and Base32 secret key
3. Click **Add**

---

## Building from Source

```bash
git clone https://github.com/Thefunkytechguy/OTPilot.git
cd OTPilot/src/OTPilot
dotnet build
dotnet run
```

**Requirements:** .NET 8 SDK, Windows

**Dependencies** (via NuGet):

| Package | Purpose |
|---|---|
| `Otp.NET` | RFC 6238 TOTP code generation |
| `CommunityToolkit.Mvvm` | MVVM framework |
| `Hardcodet.NotifyIcon.Wpf` | System tray support |
| `ZXing.Net` | QR code decoding |
| `System.Drawing.Common` | Screen capture |

---

## vs WinOTP

| | WinOTP | OTP Pilot |
|---|---|---|
| Cross-machine roaming | No | Yes — OneDrive for Business |
| Encryption | DPAPI | AES-256-GCM + DPAPI |
| Hot-desk support | None | Full |
| QR enrolment | Limited | Screen-capture, no camera needed |
| Offline support | Yes | Yes |
| Intune deployment | Manual | Native Win32 package |
| Open source | No | Yes |

---

## License

MIT — free to use, modify, and distribute.
See [LICENSE.txt](LICENSE.txt) for details.

---

## Author

**Eugene Myburgh**
[github.com/Thefunkytechguy](https://github.com/Thefunkytechguy)

---

*Built for a real problem, in a real call centre, for real people.*
