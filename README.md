# Snowball Battle Royale

雪原を舞台にした 100 人バトルロイヤル FPS（Unity 6 / URP）。
プレイヤーは雪玉を投げ、雪を掘り、雪壁を築きながら、寒さによる「悴み（Cold）」と戦って最後の 1 人を目指す。

## Quick Start

### 必要環境

| 項目 | バージョン |
| --- | --- |
| Unity | `6000.3.4f1`（`ProjectSettings/ProjectVersion.txt` と一致させること） |
| Git LFS | 必須（バイナリアセットは LFS 管理） |
| IDE | Rider / Visual Studio / VS Code（`.editorconfig` 準拠） |

### セットアップ手順

1. Git LFS を初期化してクローンする

   ```bash
   git lfs install
   git clone https://github.com/global-tech-bridge/snowball-battle-royale.git
   cd snowball-battle-royale
   ```

2. Unity Hub に Unity `6000.3.4f1` をインストールする（Hub の「インストール」→「エディターをインストール」）。
3. Unity Hub の「プロジェクトを追加」から、クローンしたフォルダを選択して開く。
   初回起動時にパッケージ解決とインポートが走るため数分かかる。
4. `Assets/_Project/Scenes/00_Boot.unity` を開き、Play を押す。
   `Bootstrap` が Persistent シーンをアディティブロードし、Console に起動ログが出れば成功。

### テスト実行

Unity Editor 上： `Window > General > Test Runner` → `EditMode` タブ → `Run All`

CI / コマンドラインから：

```bash
./Tools/run-tests.sh
```

## プロジェクト構成

```
Assets/_Project/
├── Art/        モデル・テクスチャ・マテリアル
├── Audio/      SFX・BGM
├── Configs/    ScriptableObject 設定アセット
├── Prefabs/    プレハブ
├── Scenes/     シーン（00_Boot / 01_Persistent / Sandbox_*）
├── Scripts/    C# スクリプト（asmdef 単位で分割）
├── Shaders/    シェーダー
├── Tests/      EditMode / PlayMode テスト
└── UI/         UI Toolkit / uGUI アセット
```

詳細は [docs/project-structure.md](docs/project-structure.md) を参照。

## ドキュメント

| ドキュメント | 内容 |
| --- | --- |
| [docs/roadmap.md](docs/roadmap.md) | Phase 1〜12 の開発ロードマップ |
| [docs/architecture.md](docs/architecture.md) | アセンブリ構成・依存方向・設計方針 |
| [docs/project-structure.md](docs/project-structure.md) | フォルダ規約・命名規則 |
| [docs/unity-mcp.md](docs/unity-mcp.md) | Unity MCP（AI エージェント連携）セットアップ |

## 開発フロー

- ブランチ： `develop` から `feature/<phase>-<内容>` を切る
- Issue 単位で PR を作成し、`Closes #<issue番号>` を記載する
- テスト（EditMode）が緑であることを PR の前提とする

## ライセンス

社内プロジェクト（非公開）。
