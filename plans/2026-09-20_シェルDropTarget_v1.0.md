# 2026-09-20 シェル DropTarget 計画 v1.0

自前 AllowDrop / IDropTarget をやめる。
`%APPDATA%\QuickDock\pins` のフォルダが持つシェル `IDropTarget` を `RegisterDragDrop` する。
Explorer / デスクトップはフォルダへ落とすのと同じ経路。
UI はそのフォルダの中身を表示する。
