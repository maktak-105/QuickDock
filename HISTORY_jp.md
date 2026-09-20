# 変更履歴

## 3.2.2 — 2026-09-20

- アイコン右クリック: 管理者として実行 / 削除
- ドロップ直後でも名前ポップアップを出す

## 3.2.1 — 2026-09-20

- トレイ／アプリのアイコンをガラス棚のドック風に変更（丸ポチではない）

## 3.2.0 — 2026-09-20

- 枠なしフローティングドックに戻した
- アイコンは横 6 個まで、以降は下段。ホバーで名前表示。中で D&D 並べ替え
- Quick 風のドックアイコン（シアン円＋アイコン列）

## 3.1.1 — 2026-09-20

- ドキュメント: 管理者起動だと Explorer からドロップできない（UIPI）。WPF の有無は原因ではない
- 子コントロールも `AllowDrop`。既定の常に前面はオフ

## 3.1.0 — 2026-09-20

- WPF をやめ、公開アプリと同じ「普通の WinForms 窓 + FileDrop + Copy」
- OpenFences と同じく pins フォルダへコピー。追加ボタンあり

## 3.0.0 — 2026-09-20

- 自前ドロップを廃止。ピン用フォルダのシェル `IDropTarget` を `RegisterDragDrop`

## 2.1.0 — 2026-09-20

- 枠なし 64px をやめ、通常の WPF 窓にした
- DragOver は常に Copy（進入禁止カーソル対策）

## 2.0.1 — 2026-09-20

- デスクトップのショートカット向けに Shell IDList / FileNameW / Link ドロップを受け付ける

## 2.0.0 — 2026-09-20

- WinForms をやめ、WPF / .NET 10 に切り替え（Explorer の FileDrop 向け）
- `dotnet publish` で `dist\QuickDock.exe`（win-x64 単体）

## 1.0.2 — 2026-09-20

- 自前 COM `IDropTarget` を廃止。WinForms `AllowDrop` で Explorer から登録

## 1.0.1 — 2026-09-20

- ドロップ中はホバー展開しない（Explorer の登録が切れるため）
- OLE 効果は COPY のみ。`DragAcceptFiles` を外して `RegisterDragDrop` のみ
- パス取得は `DataObject` を優先

## 1.0.0 — 2026-09-20

- コンテナ（シアン丸）にドロップして格納。ホバーで左展開。クリック起動
- コンテナはドラッグで移動。位置と「常に前面」を `%APPDATA%\QuickDock` に保存
- アイコン削除は右クリック後に確認
- 設定メニュー（コンテナ / トレイ）に常に前面と終了
- アイコンは 48px（SHIL_EXTRALARGE）
- Explorer 向け OLE `RegisterDragDrop`（進入禁止カーソル対策）
- `scripts\build.bat` → `dist\QuickDock.exe`
- `proto\selftest.ps1` で登録・展開・起動を自動確認

## 0.1.0 — 2026-09-19

- テンプレートのフォルダ構成を作成した。
