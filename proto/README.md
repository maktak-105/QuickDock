# proto/

製品は `src\app\QuickDock.cs` → `dist\QuickDock.exe`。

ここには自動テストだけ置く。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File proto\selftest.ps1
```

- `selftest.ps1` … 収納サイズ、ヒットテスト、D&D 登録、左展開、クリック起動
- `DropTest.exe` … selftest が使う OLE ドロップ擬似（ビルドは selftest 内）
