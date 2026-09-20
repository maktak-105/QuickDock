# QuickDock テンプレート準拠監査 結果

## 実施内容
- .NET Framework `csc.exe`を製品ビルドに使うという古い環境説明を、.NET 10 SDKの手順へ修正。
- 実製品スモークテストとヘルパーを`tests/`へ移動し、README・仕様・環境文書の参照を同期。
- 未使用C++ソースを`proto/legacy_cpp/`へ移し、旧C# compiler / HTML bundle scriptsと失敗固定のテストスタブを削除。
- `Directory.Build.props`で.NETの中間生成物を`build/intermediate/`へ集約。既存の`src/app/bin`・`obj`キャッシュは削除せず同配下へ退避。
- CIをファイル有無チェックから.NET 10の実発行へ変更。
- 英日READMEに版数を記載し、配布履歴の日本語版から未公開開発履歴を除去して英語版と同期。

## 検証
- `C:\Program Files\dotnet\dotnet.exe --version` は `10.0.401`。
- `dotnet publish src/app/QuickDock.csproj --configuration Release --output <temp> --nologo` 成功し、単体exeを一時ディレクトリへ出力（distおよび起動中プロセスは変更なし）。
- `dotnet build src/app/QuickDock.csproj --configuration Release --nologo` 成功（0エラー）。既存の高DPI manifest警告 `WFO0003` あり。
- 中間出力先が `build/intermediate/` 配下であることをMSBuildプロパティで確認。
- `git diff --check` 成功。実ドキュメント・テスト・ビルド参照を確認。
- GitHub Actionsの実行結果は未確認（pushしていない）。
