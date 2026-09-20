# proto/

製品本体は `src\app\Program.cs` / `MainForm.cs`（C# / WinForms / .NET 10）です。
`scripts\build.bat` で `dist\QuickDock.exe` を発行します。

このフォルダーには旧実装（`legacy_cpp/`）や動作調査用のプロトタイプを置きます。製品のビルドや配布には含めません。
現行製品のドロップ・展開・起動スモークテストと、そのヘルパーは `tests/` にあります。
