# Changelog

Public version is **1.1.0**. Entries 1.0.1–3.2.2 below are unreleased development notes (kept).

## 1.1.0 — 2026-09-23

- Hide from the taskbar and Alt+Tab (WS_EX_TOOLWINDOW)
- Left-click the tray icon to bring the dock to the front

## 1.0.0 — 2026-09-20 (first public)

- Borderless floating dock; Explorer drop; hover expand; reorder; run as admin
- Option to run QuickDock when the current user signs in to Windows
- Explorer cannot drop onto an elevated instance (UIPI)

## 3.2.2 — 2026-09-20 (unreleased)

- Icon context menu: Run as administrator / Delete
- Name tooltip shows immediately after a drop

## 3.2.1 — 2026-09-20

- App/tray icon is the glass-shelf dock art

## 3.2.0 — 2026-09-20

- Borderless floating dock; 6 icons per row then wrap; hover names; drag to reorder

## 3.1.1 — 2026-09-20

- Docs: Explorer cannot drop onto an elevated QuickDock (UIPI). Not a WPF vs WinForms issue.

## 2.0.0 — 2026-09-20

- Rebuilt on WPF / .NET 10 for Explorer FileDrop
- `dotnet publish` writes a self-contained `dist\QuickDock.exe` (win-x64)

## 1.0.2 — 2026-09-20

- Drop uses WinForms `AllowDrop` instead of a custom COM `IDropTarget`

## 1.0.1 — 2026-09-20

- Do not resize the window while a drop is in progress
- OLE effect is COPY only; `RegisterDragDrop` without `DragAcceptFiles`
- Prefer WinForms `DataObject` for dropped paths

## 1.0.0 — 2026-09-20

- Drop shortcuts into the cyan container; hover expands left; click launches
- Drag the container to move it; persist position and always-on-top in `%APPDATA%\QuickDock`
- Confirm before deleting an icon
- Settings menu (container / tray): always on top, Exit
- 48px icons via SHIL_EXTRALARGE
- OLE `RegisterDragDrop` for Explorer (no-drop cursor)
- `scripts\build.bat` writes `dist\QuickDock.exe`
- `tests\selftest.ps1` checks register / expand / launch

## 0.1.0 — 2026-09-19

- Created the Quick app template folder layout.
