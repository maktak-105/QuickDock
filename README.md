# QuickDock

A borderless floating dock for Windows (WinForms / .NET 10). Drop shortcuts onto it, hover to expand, click to launch.

## Using the binary

Build, then **double-click** `dist\QuickDock.exe` in Explorer (normal user). Do not leave it running from an elevated terminal.

```powershell
cd C:\Users\makta\source\QuickDock
scripts\build.bat
```

- Drop `.lnk` / `.exe` onto the dock (`%APPDATA%\QuickDock\pins`)
- Hover to expand (max 6 icons per row, then wrap down)
- Hover an icon for its name
- Left-click launches; right-click: **Run as administrator** / **Delete**
- Drag icons to reorder
- Drag the dock handle to move it
- Handle or tray right-click: **Always on top**, Exit

If Explorer shows the “no drop” cursor, QuickDock is almost certainly running **as Administrator**. Quit that instance and start the exe from Explorer. See `docs/spec_jp.md` section 7.

## Tests

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File proto\selftest.ps1
```

## License

MIT. Copyright (c) 2026 maktak-105 (GitHub: https://github.com/maktak-105)
