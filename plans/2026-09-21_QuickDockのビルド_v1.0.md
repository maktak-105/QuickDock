# QuickDock のビルド

## ユーザー指示（原文・2026-09-21）
- 「QuickDockをビルドして」

## 解釈
- QuickDock（.NET 10 のプロジェクト）を、プロジェクトの標準の手順（`scripts\build.bat`）でビルドし、成功か失敗かを報告する。
- ビルドだけを行う。ソースの変更、コミット、push、リリース、実行ファイルを起動しての画面操作は、指示が無いので行わない（ユーザーが手元で確認する方針）。

## 調査項目
1. ビルド手順と必要な環境（`README.md`、`docs/environment.md`、`scripts/build.bat`）と、この PC の .NET SDK。
2. 起動中の QuickDock が、出力先のファイルをロックしていないか（起動中のものは終了させない）。
3. ビルドの実行、出力（`dist/` の実行ファイル）の確認、テストがあれば実行する。

## 確認した事実
- ビルド手順: `scripts\build.bat`（中身は `dotnet publish src\app\QuickDock.csproj -c Release -o dist`）。出力は `dist\QuickDock.exe`。環境は .NET 10 SDK のみ（Python・MinGW・WebView2 は不要）。
- この PC の .NET SDK: 10.0.201（`global.json` は無い）。
- ソースの状態: `main` は `origin/main` と一致（`34846a5`「Document QuickDock 1.0.0 release verification」）。未コミットの変更なし（この計画書を除く）。
- ビルド前: 起動中の QuickDock は 0 件（ロックの心配なし。終了させたプロセスも無い）。`dist` は `.gitkeep` のみ。

## 結果（2026-09-21 実施）
- **ビルド成功**: `scripts\build.bat` の終了コード 0、所要 39.6 秒（パッケージの復元 24.6 秒を含む）。「built dist\QuickDock.exe」を確認。
- 出力: `dist\QuickDock.exe`（116,300,242 バイト）1 ファイル。x64（machine 0x8664）、Windows GUI（subsystem 2）。ファイルバージョン 1.0.0.0、製品バージョン 1.0.0+34846a5…（ビルドしたコミットの識別子と一致）。`HISTORY.md` の最新版 1.0.0 と一致。
- 警告 1 件（エラーは 0 件）: `WFO0003`（高 DPI 設定を `app.manifest` から削除し、`Application.SetHighDpiMode` API か `ApplicationHighDpiMode` プロジェクト プロパティで設定するよう、WinForms のアナライザーが案内するもの）。ビルドの成否には影響しない。修正は指示が無いので、していない。
- ビルド後の Git: 追跡ファイルの変更 0 件。ビルドの中間出力（`build\intermediate`、`dist`）は無視されており、新たな未追跡ファイルも無い（この計画書を除く）。

## 実施していないこと
- `tests\selftest.ps1`（対話式のスモークテスト。実行ファイルを起動して画面・ドラッグ＆ドロップを操作する）と、実行ファイルの起動。画面での確認はユーザーが行う方針のため。
- ソースの変更、コミット、push、リリース。この計画書は未コミット（指示が無いため）。
