# Development environment

## Tools

- Windows 10/11 x64
- .NET Framework 4.x `csc.exe` (default `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`)
- Git
- PowerShell for the automated test

Python, MinGW, and WebView2 are not required for the product binary.

## Build

```powershell
cd C:\Users\makta\source\QuickDock
scripts\build.bat
```

Output: `dist\QuickDock.exe`

## Run

```powershell
dist\QuickDock.exe
```

Exit from the tray or the container context menu. If the process is elevated, Explorer (medium IL) drag-drop can show the no-drop cursor. Launch the exe from Explorer in that case.

## Tests

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File proto\selftest.ps1
```

## Troubleshooting

- No-drop cursor: do not resize during OLE drag; check integrity level. Look for `OLE DragEnter` in `%APPDATA%\QuickDock\drop.log`.
- Soft icons: 48px extra-large image list (`SHGetImageList`).
