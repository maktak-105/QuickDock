# QuickDock Release成果物とSHA-256

## 目的
- GitHub Actionsでタグのソースをビルドし、EXEとそのSHA-256をGitHub Releaseに添付する。
- 既存の`v1.0.0`タグにもworkflow_dispatchから同じ手順を実行できるようにする。
- READMEに取得・照合手順を記載する。

## 検証
- 既存タグ`v1.0.0`を指定してGitHub Actionsを手動起動する。
- ReleaseにEXEと`SHA256SUMS.txt`が存在し、ハッシュが一致することを確認する。
