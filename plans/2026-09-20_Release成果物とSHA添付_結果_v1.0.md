# QuickDock Release成果物とSHA-256 結果

## 実施内容
- タグpushで動くRelease workflowを追加し、既存タグを指定できる手動起動にも対応。
- GitHub ActionsでタグのQuickDock 1.0.0を発行し、`QuickDock.exe`と`SHA256SUMS.txt`をReleaseへ添付。
- `scripts/build.bat`はPATH上の.NET SDKを優先するよう修正。SDK setup後もマシン共通の古いdotnetを誤選択しないようにした。
- CIとReleaseの実行はどちらも成功。

## 検証
- Release: https://github.com/maktak-105/QuickDock/releases/tag/v1.0.0
- Actions Release run: https://github.com/maktak-105/QuickDock/actions/runs/35499432865
- `SHA256SUMS.txt`内の値 `1cb709384dbd19ae4ebc1d9354a3c1382442b4441af099379ef430e97658d03c` が、GitHub APIのEXE asset SHA-256 digestと一致。
- 配布EXEサイズ: 116,291,726 bytes。
- タグ`v1.0.0`の`MainForm.cs`に起動時実行メニューとHKCU Run登録処理が含まれている。

## 注意
- 起動中の`dist\\QuickDock.exe`はRelease成果物へ自動置換されない。新しいRelease版を使うにはQuickDockを終了して、ReleaseからEXEを取得する。
