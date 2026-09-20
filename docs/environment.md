# Development environment

## Tools

- Windows 10/11 x64
- .NET 10 SDK (used by `dotnet publish`)
- Windows PowerShell 5.1 or PowerShell 7 for the interactive smoke test
- .NET Framework 4.x `csc.exe` is used only to compile the smoke-test OLE helper
- Git

Python, MinGW, WebView2, and the .NET Framework compiler are not required to build or run the product.

## Build

From the repository root, run:

```powershell
.\scripts\build.bat
```

Output: `dist\QuickDock.exe`

## Run

```powershell
dist\QuickDock.exe
```

Exit from the tray or the container context menu. If the process is elevated, Explorer (medium IL) drag-drop can show the no-drop cursor. Launch the exe from Explorer in that case.

## Tests

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests\selftest.ps1
```

## Troubleshooting

- No-drop cursor: do not resize during OLE drag; check integrity level. Look for `OLE DragEnter` in `%APPDATA%\QuickDock\drop.log`.
- Soft icons: 48px extra-large image list (`SHGetImageList`).
