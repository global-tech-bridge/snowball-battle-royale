# プロジェクト構成・命名規約

## フォルダ構成

```
snowball-battle-royale/
├── Assets/
│   └── _Project/              ← 自作アセットはすべてこの下（サードパーティと混ぜない）
│       ├── Art/               モデル / テクスチャ / マテリアル
│       ├── Audio/             SFX / BGM / AudioMixer
│       ├── Configs/           ScriptableObject 設定アセット
│       │   ├── Input/         SBInputActions.inputactions
│       │   └── Rendering/     URP アセット
│       ├── Prefabs/           プレハブ
│       ├── Scenes/            00_Boot / 01_Persistent / Sandbox_*
│       ├── Scripts/           C#（asmdef 単位のサブフォルダ）
│       │   ├── Domain/        SB.Domain
│       │   ├── Core/          SB.Core
│       │   ├── Player/        SB.Player
│       │   ├── AI/            SB.AI
│       │   ├── World/         SB.World
│       │   ├── Items/         SB.Items
│       │   ├── UI/            SB.UI
│       │   ├── Audio/         SB.Audio
│       │   └── Editor/        SB.Editor（Editor 専用）
│       ├── Shaders/           シェーダー
│       ├── Tests/
│       │   ├── EditMode/      SB.Tests.EditMode
│       │   └── PlayMode/      SB.Tests.PlayMode
│       └── UI/                UXML / USS / UI アセット
├── Packages/                  manifest.json（パッケージ定義）
├── ProjectSettings/           プロジェクト設定（コミット対象）
├── Tools/                     開発スクリプト（run-tests.sh など）
└── docs/                      設計ドキュメント
```

サードパーティのアセットは `Assets/` 直下（`_Project` の外）に置く。
`_Project` プレフィックスの `_` は、Project ウィンドウで常に先頭に並ぶようにするため。

## シーン命名

| 名前 | 用途 |
| --- | --- |
| `00_Boot` | 起動シーン。`Bootstrap` だけを置く |
| `01_Persistent` | 常駐システム・カメラ・ライト |
| `Sandbox_<機能>` | 機能単位の検証シーン（例: `Sandbox_Player`） |

## C# 命名規約

`.editorconfig` で強制している。主なもの：

| 対象 | 規約 | 例 |
| --- | --- | --- |
| クラス / メソッド / プロパティ | PascalCase | `PlayerMovement`, `ApplyDamage` |
| private フィールド | `_camelCase` | `_moveSpeed` |
| 定数 | PascalCase | `MaxSlots` |
| ローカル変数 / 引数 | camelCase | `sceneName` |
| インターフェース | `I` + PascalCase | `IDamageable` |
| ScriptableObject | 末尾 `SO` | `PlayerStatsSO` |
| asmdef | `SB.<領域>` | `SB.Player` |
| 名前空間 | `SnowballBattleRoyale.<領域>` | `SnowballBattleRoyale.Player` |

インデントは 4 スペース、改行は LF、ブレースは改行してから開く（Allman）。

## Git 運用

- `main`: リリース可能な状態
- `develop`: 開発の統合先
- `feature/phase-<N>-<内容>`: Issue 単位の作業ブランチ

バイナリ（`.png` `.fbx` `.wav` など）は Git LFS で管理する（`.gitattributes` 参照）。
`.meta` ファイルは**必ずコミットする**（欠けると参照が壊れる）。

`Library/` `Temp/` `Logs/` `UserSettings/` はコミットしない（`.gitignore` 参照）。
