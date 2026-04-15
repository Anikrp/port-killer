# PortKiller — Windows (developer notes)

End-user documentation (features, install, troubleshooting) is in the **[repository root README](../../README.md)**.

---

## Installer output paths (repo root)

| Artifact | Path |
|----------|------|
| **Setup EXE** | `publish/installer/PortKiller-<version>-Setup-x64.exe` |
| **Staging** (Inno input) | `publish/win-x64-installer/` |

`<version>` matches `<Version>` in `PortKiller/PortKiller.csproj`.

---

## Build

From **repository root**:

```powershell
.\platforms\windows\scripts\build-installer.ps1
```

**Framework-dependent** (smaller; needs .NET 9 on target):

```powershell
.\platforms\windows\scripts\build-installer.ps1 -FrameworkDependent
```

---

## Icons

- **Source artwork:** `PortKiller/Assets/AppIcon.svg`
- **App / tray / shortcuts:** `PortKiller/Assets/app.ico` (generated)
- **Inno wizard:** `installer/wizard-large.png`, `installer/wizard-small.png` (generated)

Regenerate after SVG changes:

```powershell
dotnet run --project tools/IconGen -- platforms/windows/PortKiller/Assets/AppIcon.svg platforms/windows/PortKiller/Assets/app.ico platforms/windows/installer/wizard-large.png platforms/windows/installer/wizard-small.png
```

Generated files next to `PortKiller.iss` are listed in **`.gitignore`**.

---

## Layout

```
platforms/windows/
├── PortKiller/           # WPF project
├── installer/
│   └── PortKiller.iss
└── scripts/
    └── build-installer.ps1
```

---

## License

[MIT](../../LICENSE) — Copyright (c) 2026 arpcode.com.
