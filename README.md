# QuickDock

A borderless floating dock for Windows (WinForms / .NET 10). Drop shortcuts onto it, hover to expand, click to launch.

Version: **1.1.0**

## Using the binary

Download `QuickDock.exe` and `SHA256SUMS.txt` from [GitHub Releases](https://github.com/maktak-105/QuickDock/releases). Verify the download with `Get-FileHash .\QuickDock.exe -Algorithm SHA256` and compare it with `SHA256SUMS.txt`.

To build from source, install the .NET 10 SDK; see [the development environment guide](docs/environment.md).

```powershell
.\scripts\build.bat
```

Then **double-click** `dist\QuickDock.exe` in Explorer (normal user). Do not leave it running from an elevated terminal.

- Drop `.lnk` / `.exe` onto the dock (`%APPDATA%\QuickDock\pins`)
- Hover to expand (max 6 icons per row, then wrap down)
- Hover an icon for its name
- Left-click launches; right-click: **Run as administrator** / **Delete**
- Drag icons to reorder
- Drag the dock handle to move it
- Handle or tray right-click: **Run at Windows startup**, **Always on top**, Exit

If Explorer shows the “no drop” cursor, QuickDock is almost certainly running **as Administrator**. Quit that instance and start the exe from Explorer. See `docs/spec_jp.md` section 7.

## Tests

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests\selftest.ps1
```

## License

MIT. Copyright (c) 2026 maktak-105 (GitHub: https://github.com/maktak-105)
