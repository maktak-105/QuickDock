# QuickDock

Windows 用の枠なしフローティングドック。ショートカットをドロップして格納し、ホバーで展開、クリックで起動する。

バージョン: **1.1.0**（C# / WinForms / .NET 10）

## 使い方

配布版の `QuickDock.exe` と `SHA256SUMS.txt` は[GitHub Releases](https://github.com/maktak-105/QuickDock/releases)から取得できます。ハッシュは `Get-FileHash .\QuickDock.exe -Algorithm SHA256` で計算し、`SHA256SUMS.txt` と照合します。

ソースからビルドするには .NET 10 SDK が必要です（[開発環境ガイド](docs/environment_jp.md)）。ビルドした `dist\QuickDock.exe` は **エクスプローラーからダブルクリック**（通常権限）で起動します。管理者のターミナルから起動したままだと、エクスプローラーからドロップできません。

```powershell
.\scripts\build.bat
```

- `.lnk` / `.exe` をドックへドロップして格納（`%APPDATA%\QuickDock\pins`）
- カーソルを乗せると展開。アイコンは横 6 個まで、それ以上は下の段
- アイコンに乗せると名前を表示
- 左クリックで起動。右クリックで **管理者として実行** / **削除**
- アイコン同士のドラッグで並べ替え
- ドック本体をドラッグして移動
- 本体またはトレイの右クリック: **Windows起動時に実行** / **常に前面** / 終了

詳細は `docs/spec_jp.md`。管理者権限とドロップは同ファイル 7 節。

## テスト

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests\selftest.ps1
```

## ライセンス

MIT。Copyright (c) 2026 maktak-105 (GitHub: https://github.com/maktak-105)
