# 開発ロードマップ

Snowball Battle Royale の開発フェーズ一覧。各フェーズは GitHub Issue と 1:1 で対応する。

| Phase | Issue | 内容 | 状態 |
| --- | --- | --- | --- |
| 1 | [#10](https://github.com/global-tech-bridge/snowball-battle-royale/issues/10) | プロジェクトセットアップ（Unity 初期化・asmdef・ロガー） | 進行中 |
| 2 | [#11](https://github.com/global-tech-bridge/snowball-battle-royale/issues/11) | プレイヤーコントローラー（FPS 移動・視点・スタミナ） | 未着手 |
| 3 | [#12](https://github.com/global-tech-bridge/snowball-battle-royale/issues/12) | ワールド・地形（バイオーム・小屋・掘削システム） | 未着手 |
| 4 | [#13](https://github.com/global-tech-bridge/snowball-battle-royale/issues/13) | 戦闘・雪玉（投擲・当たり判定・雪壁構築） | 未着手 |
| 5 | [#14](https://github.com/global-tech-bridge/snowball-battle-royale/issues/14) | 悴み（Cold）システム | 未着手 |
| 6 | [#15](https://github.com/global-tech-bridge/snowball-battle-royale/issues/15) | インベントリ・アイテム（8 スロット・9 種） | 未着手 |
| 7 | [#16](https://github.com/global-tech-bridge/snowball-battle-royale/issues/16) | AI Bot（99 体 FSM・戦闘・建築・16 ポーズ） | 未着手 |
| 8 | [#17](https://github.com/global-tech-bridge/snowball-battle-royale/issues/17) | 天候・フェーズ（時間進行・気温・吹雪） | 未着手 |
| 9 | [#18](https://github.com/global-tech-bridge/snowball-battle-royale/issues/18) | オーディオ（SFX・BGM・足音テンポ管理） | 未着手 |
| 10 | [#19](https://github.com/global-tech-bridge/snowball-battle-royale/issues/19) | UI・HUD（ステータスバー・ミニマップ・多言語） | 未着手 |
| 11 | [#20](https://github.com/global-tech-bridge/snowball-battle-royale/issues/20) | ポリッシュ・最適化（パフォーマンス・QA・バランス） | 未着手 |
| 12 | [#21](https://github.com/global-tech-bridge/snowball-battle-royale/issues/21) | （ストレッチ）オンラインマルチプレイ | 未着手 |

## Phase 1: プロジェクトセットアップ

Unity プロジェクトを初期化し、AI 駆動開発の土台を整える。

- Unity `6000.3.4f1` / URP / Input System / Test Framework / Localization / TextMeshPro
- `Assets/_Project/` フォルダ構成と asmdef によるアセンブリ分割
- `SBLog` ロガー（`Conditional` 属性でリリースビルドから除外）
- `00_Boot` シーン + `Bootstrap`（Persistent シーンのアディティブロード）
- EditMode テスト基盤
- Input Actions（Move / Look / Throw / Dig / Build / Interact / SlotSelect）

**受入基準**: ビルドが通る／既存テストが緑／README の Quick Start でセットアップできる。

## Phase 2: プレイヤーコントローラー

`PlayerStats`（Pure C#）+ `PlayerStatsSO` + `PlayerMovement` + `PlayerLook`。
移動速度は 歩 8 / ダッシュ 14 / しゃがみ 4 / 伏せ 2 m/s。スタミナはダッシュ -10/s、回復 +5/s、
悴み 75% 以上で消費 2 倍。

## Phase 3: ワールド・地形

700m × 700m のマップ。Mountain / Forest / Plain / DeepSnow / FrozenLake のバイオーム、
樹木 約 1100 本・岩 約 250 個・小屋（集落 4 + 散在 8）・焚き火 20 箇所。
`DigSystem` によるハイトマップ動的変形（塹壕構築・掘削深度による色変化）。

## Phase 4: 戦闘・雪玉

`Snowball` プレハブ + ObjectPool、`SnowballThrower`（Player / Bot 共用）。
物理は重力 9.8 / 初速 50 m/s / 寿命 3 秒。`IDamageable` 経由でダメージ適用
（ボディアーマー時 -50%）。Q キーで雪壁構築（雪玉 3 個消費、3×1.5×0.8m、OBB 判定）。

## Phase 5: 悴み（Cold）システム

`ColdSystem`（Pure C#）+ `ColdSystemConfigSO`。
上昇要因は 気温（sqrt 曲線）/ 雪玉所持 / 停止 / 深雪 / 掘削。
回復要因は 焚き火近傍（< 8m）/ 防寒着（×0.5）/ 長靴（×0.8）。
デバフ段階は 25%（速度低下）/ 75%（スタミナ 2 倍消費）/ 85%（継続ダメージ）。

## Phase 6: インベントリ・アイテム

8 スロット（雪玉はスタック 10）。アイテム 9 種：Snowball / Flashlight / Lighter / Wood /
BodyArmor / ColdArmor / Boots / Shovel / SnowballMaker。
E キーで取得、F キーで使用・装備トグル。

## Phase 7: AI Bot

最大 99 体。FSM は Wander / SeekWarmth / FightBot / AttackPlayer / Flee / Down。
優先度は Flee > SeekWarmth > FightBot > AttackPlayer > Wander。
Bot 自身も雪壁構築・塹壕掘削を行い、16 ポーズで状態を表現する。
`BotManager` による Tick 分散・距離 LOD で 60 FPS を維持。

## Phase 8: 天候・フェーズ

気温推移 -2°C → -7 → -17 → -31°C。フェーズは Early (0-33%) / Mid (33-66%) / Late (66-100%)。
雪パーティクル密度・Fog・Skybox が連動し、焚き火は時間とともに消えていく。

## Phase 9: オーディオ

`AudioManager` + `SfxLibrarySO` + AudioMixer（Master / SFX{Player,Bot,Environment,UI} / BGM）。
足音は移動状態でテンポ変化。3D 音響は最大可聴 50m。BGM はフェーズ別 3 トラック + ロビー。

## Phase 10: UI・HUD

UI Toolkit ベース。HP / スタミナ / 悴み度バー、ミニマップ（160×160）、インベントリバー、
残り時間・人数・気温・フェーズ表示、キルフィード、設定画面。
Unity Localization で JP / EN 切り替え。

## Phase 11: ポリッシュ・最適化

目標は ミッドレンジ PC で 60 FPS / ローエンド 30 FPS / メモリ 2GB 以下。
LOD・Culling・圧縮・Draw Call 削減・Bot 負荷分散を行い、5 分プレイテストを 10 回以上実施。

## Phase 12（ストレッチ）: オンラインマルチプレイ

MVP（Phase 1-11）完了後に着手。Netcode for GameObjects または Mirror を統合し、
Server Authoritative な雪玉判定・地形同期・マッチメイキング・チームモードを実装する。
