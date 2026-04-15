# PortKiller for Windows

<p align="center">
  <img src="platforms/windows/PortKiller/Assets/AppIcon.svg" alt="PortKiller" width="160" height="160">
</p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License: MIT"></a>
  <a href="https://www.microsoft.com/windows"><img src="https://img.shields.io/badge/Windows-10%2B-0078D6" alt="Windows 10+"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/9.0"><img src="https://img.shields.io/badge/.NET-9.0-512BD4" alt=".NET 9"></a>
  <img src="https://img.shields.io/badge/version-4.1-444444" alt="Version 4.1">
</p>

<p align="center">
  <strong>Native WPF app</strong> to inspect listening TCP ports, kill processes, and manage Cloudflare Tunnels — with a system tray workflow, configurable refresh, and a modern UI with vector artwork in the sidebar.<br><br>
  Published by <a href="https://arpcodes.com">arpcodes.com</a>.
</p>

---

## Highlights (this fork)

| Area | What you get |
|------|----------------|
| **Installer** | Self-contained **Inno Setup** package — no separate .NET install on target PCs; Start Menu entry, optional desktop shortcut, clean uninstall. |
| **Icons** | **Window, taskbar, tray, and installer** use the same visual identity (`Assets/app.ico` generated from `Assets/AppIcon.svg`). |
| **UI** | **SharpVectors** renders **SVG** in-app (sidebar branding); tray menu uses custom styling for a clean dark menu without odd white gaps. |
| **Settings** | **Tray behavior** (e.g. close to tray / start minimized), **notifications**, and **auto-refresh interval** from the sidebar **Settings** panel. |
| **Elevation** | Designed to run **as Administrator** when listing or ending processes (UAC as needed). |

---

## Features

### Ports & processes

- Discovers **listening TCP ports** and maps them to processes.
- **Kill** with confirmation — graceful stop where possible, force when needed.
- **Search and filter** by port or process name.
- **Favorites** and **watched ports** with notifications for changes.
- **Categories** (e.g. web, database, dev tools) for quicker scanning.
- **Auto-refresh** on an interval you set in **Settings**.

### Cloudflare Tunnels

- View and work with **active Cloudflare Tunnel** connections from the app.

### System tray

- **Left-click** / **right-click** the tray icon for quick actions and the full menu.
- Optional **close to tray** and related options in **Settings**.

---

## Requirements

| | |
|--|--|
| **OS** | Windows **10** (1809+) or **11**, **64-bit (x64)** |
| **Setup `.exe`** | No extra runtime — **self-contained** .NET payload inside the installer. |
| **Portable ZIP** | Requires **[.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)** on the machine. |
| **Privileges** | **Administrator** / UAC for listing and killing processes. |

---

## Installation

### Version 4.1 — Windows (recommended)

**Download and run the installer** (self-contained — no separate .NET install on the PC):

**[publish/installer/PortKiller-4.1.0-Setup-x64.exe](publish/installer/PortKiller-4.1.0-Setup-x64.exe)**

1. Open or download that file from the repository.
2. Run it. If SmartScreen appears for an unsigned build, choose **More info** → **Run anyway**.
3. Approve **UAC** when prompted.
4. Start **PortKiller** from the **Start Menu** or tray. The installer does not auto-launch the app (open it yourself — the app may prompt for elevation).

### Option B — Portable ZIP

1. Download **`PortKiller-v*-windows-x64.zip`** from Releases.
2. Extract and run **`PortKiller.exe`**.
3. Install **.NET 9 Desktop Runtime** if Windows reports it is missing.

### Option C — Run from source

```powershell
cd platforms\windows\PortKiller
dotnet restore
dotnet run
```

Requires **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** on Windows.

---

## Build the Windows installer locally

From the **repository root**:

**Prerequisites:** [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0), [Inno Setup 6](https://jrsoftware.org/isdl.php) (e.g. `winget install JRSoftware.InnoSetup`).

```powershell
.\platforms\windows\scripts\build-installer.ps1
```

**Output (single file to ship):**

```text
publish/installer/PortKiller-<version>-Setup-x64.exe
```

`<version>` comes from `platforms/windows/PortKiller/PortKiller.csproj` (currently **4.1.0**).

**Smaller installer** (machines must already have .NET 9):

```powershell
.\platforms\windows\scripts\build-installer.ps1 -FrameworkDependent
```

**Regenerate `app.ico` and installer wizard images** after editing `Assets/AppIcon.svg`:

```powershell
dotnet run --project tools/IconGen -- platforms/windows/PortKiller/Assets/AppIcon.svg platforms/windows/PortKiller/Assets/app.ico platforms/windows/installer/wizard-large.png platforms/windows/installer/wizard-small.png
```

More detail: **[platforms/windows/README.md](platforms/windows/README.md)**.

---

## Usage (quick)

| Goal | How |
|------|-----|
| Refresh list | Automatic on the interval in **Settings**; manual refresh from the UI. |
| Kill a process | Use the row action / confirm when prompted. |
| Search | Top search box — port or process name. |
| Tray | Tray icon — context menu and quick actions. |
| Preferences | Sidebar **Settings** — version info, tray options, notifications, refresh interval. |

---

## Tech stack

| | |
|--|--|
| **Runtime** | .NET **9** (`net9.0-windows`), WPF |
| **UI** | MVVM ([CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)), **SharpVectors.Wpf** for SVG |
| **Tray** | [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) |
| **Installer** | [Inno Setup 6](https://jrsoftware.org/isinfo.php) — `platforms/windows/installer/PortKiller.iss` |

---

## Troubleshooting

| Issue | Try |
|-------|-----|
| Access denied when killing | Run PortKiller **as Administrator**. |
| SmartScreen blocks installer | **More info** → **Run anyway**, or sign the installer in your release pipeline. |
| Missing .NET (ZIP only) | Install [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0). |
| Installer build fails | Close any open `PortKiller-*-Setup-x64.exe`, then rebuild. |

---

## Contributing

See **[CONTRIBUTING.md](CONTRIBUTING.md)** for development setup.

---

## License

**MIT** — see **[LICENSE](LICENSE)**. Copyright (c) 2026 **arpcodes.com**.

This Windows line builds on ideas from the broader [port-killer](https://github.com/productdevbook/port-killer) ecosystem.
