# QuickDock specification

## 1. Overview

- **Name**: QuickDock
- **Purpose**: Borderless floating dock. Drop shortcuts; hover to expand; click to launch.
- **OS**: Windows 10 / 11 (64-bit)
- **Implementation**: C# / WinForms / .NET 10 (`dotnet publish`, win-x64 self-contained)
- **Distribution**: `dist\QuickDock.exe`. GitHub Release ZIP not published yet.
- **Version**: 1.0.0
- **Version**: 1.1.0
- **Icon**: `assets/QuickDock-icon.png`; EXE uses `src/app/QuickDock.ico`

## 2. UI

Borderless ~64×64 dock tile when collapsed. Expanded: up to 6 icons per row, then wrap down. No WebView2.

## 3. Behavior

| Action | Result |
|--------|--------|
| Drop `.lnk` / `.exe` | Copy into `%APPDATA%\QuickDock\pins` |
| Hover | Expand |
| Hover a stored icon | Name tooltip (also right after a drop) |
| Left-click icon | Launch |
| Drag icons | Reorder (`order.txt`) |
| Right-click icon | **Run as administrator**, **Delete** (delete confirms) |
| Drag the dock handle | Move; save position |
| Right-click handle / tray | **Run at Windows startup**, **Always on top**, Exit |

The startup option is stored per user in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` and launches the current executable.

## 4. Data

`%APPDATA%\QuickDock\`

| File | Content |
|------|---------|
| `pins\` | Stored shortcuts / exes |
| `order.txt` | Icon order (file names) |
| `items.txt` | Legacy; migrated into `pins\` |
| `settings.txt` | `x=` `y=` `topmost=` |
| `drop.log` | Drop diagnostics |

## 5. Build and test

```powershell
scripts\build.bat
powershell -NoProfile -ExecutionPolicy Bypass -File tests\selftest.ps1
```

## 6. Elevation and drop

Explorer cannot drop onto an elevated QuickDock (UIPI). Launch `dist\QuickDock.exe` by double-clicking it in Explorer.

## 7. Out of scope for v1

- Plugins, weather, meters
- Multiple docks
- macOS-style magnification
- GitHub Releases ZIP
