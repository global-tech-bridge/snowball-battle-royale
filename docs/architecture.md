# アーキテクチャ

## 1. 基本方針

| 方針 | 理由 |
| --- | --- |
| ゲームルールは Pure C# に置く | Unity の実行環境なしで EditMode テストできる。AI 駆動開発でのフィードバックループが速い |
| MonoBehaviour は薄く保つ | 入力・描画・シーンなど「Unity にしかできないこと」だけを担当させる |
| 設定値は ScriptableObject (`*SO`) に外出し | バランス調整をコード変更なしで回せる |
| asmdef でアセンブリを分割 | 依存方向を強制し、差分コンパイルを速くする |

典型的な分割例：

- `PlayerStats`（Pure C#）が HP / スタミナ / 悴みの**計算**を持つ
- `PlayerStatsSO`（ScriptableObject）が**係数**を持つ
- `PlayerMovement`（MonoBehaviour）が入力と `CharacterController` の**接続**だけを行う

## 2. アセンブリ構成

```
SB.Domain   ← 依存なし（純粋なルール・データ・SBLog）
   ↑
SB.Core     ← アプリ基盤（Bootstrap / シーンロード / サービス）
   ↑
   ├── SB.World   地形・バイオーム・掘削・天候
   ├── SB.Items   アイテム・インベントリ
   ├── SB.Audio   AudioManager・SFX
   ├── SB.Player  プレイヤー操作（+ Unity.InputSystem）
   ├── SB.AI      Bot FSM・知覚・BotManager
   └── SB.UI      HUD・メニュー（+ Localization / TextMeshPro）

SB.Editor          Editor 専用ツール（プロジェクト設定・パッケージ導入）
SB.Tests.EditMode  EditMode テスト（全アセンブリを参照）
SB.Tests.PlayMode  PlayMode テスト
```

**依存は上から下への一方向のみ。** `SB.Domain` は他のどのアセンブリも参照しない。
`SB.Player` が `SB.AI` を参照することはない（共通処理が必要になったら `SB.Domain` / `SB.Core` に降ろす）。

> `SB.Core` は Issue #10 のリスト（Domain / Player / AI / World / Items / UI / Audio / Tests）に対する追加。
> `Bootstrap` のようなアプリ基盤を `SB.Domain` に置くと Domain が純粋でなくなるため分離した。

## 3. 起動シーケンス

```
00_Boot  (Bootstrap)
   └─ BootSequence.Run()
        └─ 01_Persistent を Additive ロード（Systems / Camera / Light）
```

- `Bootstrap`（MonoBehaviour）は `DontDestroyOnLoad` と `targetFrameRate` を設定するだけ
- 判断ロジックはすべて `BootSequence`（Pure C#）にあり、`ISceneLoader` 経由でシーンを操作する
- したがって「Build Settings に無いシーンを指定した」などの異常系も EditMode テストで検証できる

## 4. ロギング

`SnowballBattleRoyale.Domain.SBLog` を使う（`UnityEngine.Debug` を直接呼ばない）。

```csharp
SBLog.Info("Loaded 01_Persistent", this, category: "Boot");
SBLog.Warn("Battery low", this, category: "Items");
SBLog.Error("Scene missing", this, category: "Boot");
```

- 出力形式は `[SB] [Category] message`
- `Info` / `Warn` / `Error` すべてに `[Conditional]` が付いており、**リリースビルドでは呼び出しごと消える**
  （引数の文字列連結コストも発生しない）
- 有効な条件は `UNITY_EDITOR` / `DEVELOPMENT_BUILD` / `SB_LOG`
  リリースビルドでもログが欲しい場合は Player Settings の Scripting Define Symbols に `SB_LOG` を追加する
- `SBLog.Sink` を差し替えると出力先を変更できる（テストではこれを使って出力を検証している）

## 5. テスト方針

| 種別 | 対象 | 場所 |
| --- | --- | --- |
| EditMode | Pure C# のルール・計算（`PlayerStats` / `ColdSystem` / `WeatherSystem` / `BootSequence` …） | `Assets/_Project/Tests/EditMode` |
| PlayMode | MonoBehaviour の統合・シーン挙動 | `Assets/_Project/Tests/PlayMode` |

新しいゲームルールを追加するときは、**まず EditMode テストを書く**（TDD）。
実行は Test Runner か `./Tools/run-tests.sh`。

## 6. 設定アセット（ScriptableObject）

`Assets/_Project/Configs/` 配下に置く。命名は `<対象>SO`（例: `PlayerStatsSO`, `ColdSystemConfigSO`）。
ランタイムコードは SO を**読み取り専用**として扱う（実行時に書き換えるとエディタ上の値が汚れるため）。

## 7. ネットワーク（Phase 12 / ストレッチ）

現状はシングルプレイ前提。オンライン化時は以下を想定する。

- 判定は Server Authoritative（雪玉ヒット・ダメージ・アイテム取得はサーバーで決定）
- 地形変形（掘削・雪壁）は「操作イベント」を同期し、各クライアントで再生する（ハイトマップ全体は送らない）
- Bot はサーバー側でのみシミュレートし、結果だけを配信する
- Pure C# のルールクラスはサーバー・クライアント双方で再利用できる（この分離が Phase 12 の前提になる）

## 8. パフォーマンス方針（Phase 11）

| 項目 | 目標 |
| --- | --- |
| フレームレート | ミッドレンジ PC で 60 FPS / ローエンドで 30 FPS |
| メモリ | 2GB 以下 |
| Bot | 99 体同時稼働 |

- Bot は `BotManager` が Tick を分散し、距離に応じて LOD（遠方は簡略化・低頻度更新）
- 雪玉は ObjectPool で使い回し、`Instantiate` / `Destroy` を毎フレーム走らせない
- 樹木・岩は LOD + GPU インスタンシング、マテリアルを統一して Draw Call を抑える
- `SBLog` がリリースで消えるため、ログ由来の GC Alloc は製品ビルドに残らない
