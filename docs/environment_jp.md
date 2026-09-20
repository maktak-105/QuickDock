# 開発環境

## ツール

- Windows 10/11 x64
- .NET 10 SDK（`dotnet publish`で製品をビルド）
- 操作付きスモークテスト用のWindows PowerShell 5.1またはPowerShell 7
- .NET Framework 4.x の `csc.exe` はスモークテスト用OLEヘルパーのコンパイルにのみ使用
- Git

製品本体のビルド・実行に Python / MinGW / WebView2 / .NET Frameworkコンパイラーは不要。

## ビルド

リポジトリルートで実行します。

```powershell
.\scripts\build.bat
```

成果物: `dist\QuickDock.exe`

## 実行

```powershell
dist\QuickDock.exe
```

終了はトレイまたはコンテナ右クリックの「終了」。管理者権限で起動すると、通常の Explorer からのドロップが進入禁止になることがある。そのときは exe をエクスプローラーから直接起動する。

## テスト

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests\selftest.ps1
```

## トラブル

- ドロップで進入禁止: ドロップ中に窓サイズを変えていないか、プロセスの整合性レベル（管理者）を確認。`%APPDATA%\QuickDock\drop.log` に `OLE DragEnter` が出るか見る。
- アイコンが粗い: 48px のシステムイメージリスト（`SHGetImageList` SHIL_EXTRALARGE）を使用。
