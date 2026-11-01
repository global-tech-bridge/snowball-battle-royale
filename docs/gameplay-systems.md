# ゲームプレイシステム

各 Phase で実装するシステムの仕様。Issue 本文を一次情報として起こしている。
未実装のセクションは着手時に追記する。

## Section 1: プレイヤー（Phase 2 / Issue #11）

### 1.1 構成

| クラス | 種別 | 責務 |
| --- | --- | --- |
| `PlayerStats` | Pure C# (`SB.Domain`) | HP / スタミナ / 悴みの計算 |
| `IPlayerStatsConfig` | interface (`SB.Domain`) | 調整値の供給元 |
| `MovementModeResolver` | Pure C# (`SB.Domain`) | 入力 → 姿勢（MovementMode）の決定 |
| `PlayerLookState` | Pure C# (`SB.Domain`) | 視点角度の累積・クランプ |
| `PlayerStatsSO` | ScriptableObject (`SB.Player`) | 調整値アセット |
| `PlayerLookSettingsSO` | ScriptableObject (`SB.Player`) | マウス感度・ピッチ制限 |
| `PlayerInputReader` | MonoBehaviour (`SB.Player`) | Input System との唯一の接点 |
| `PlayerMovement` | MonoBehaviour (`SB.Player`) | CharacterController の駆動 |
| `PlayerLook` | MonoBehaviour (`SB.Player`) | ヨー（体）/ ピッチ（カメラ）の適用 |
| `DebugStatusHud` | MonoBehaviour (`SB.UI`) | 仮 UI（Phase 10 で置き換え） |

ルール（計算）と Unity（描画・入力）を分離しているため、速度・スタミナ・視点の仕様は
すべて EditMode テストで検証できる。

### 1.2 移動速度

| 姿勢 | 速度 | 入力 |
| --- | --- | --- |
| 歩行 (Walk) | 8 m/s | WASD |
| ダッシュ (Sprint) | 14 m/s | WASD + Shift 長押し |
| しゃがみ (Crouch) | 4 m/s | C（トグル） |
| 伏せ (Prone) | 2 m/s | Z（トグル） |

**姿勢の優先度: Prone > Crouch > Sprint > Walk。**
しゃがみ・伏せ中はダッシュできない。しゃがみと伏せは相互排他で、一方に入るともう一方は解除される。

ダッシュは「Shift を押している」かつ「実際に移動入力がある」かつ「スタミナがある」場合のみ成立する。
立ち止まったまま Shift を押しても Walk 扱いになり、スタミナは減らない。

### 1.3 スタミナ

| 項目 | 値 |
| --- | --- |
| 最大 | 100 |
| ダッシュ時の消費 | -10 / 秒 |
| 非ダッシュ時の回復 | +5 / 秒 |
| 悴み 75% 以上での消費 | **× 2**（-20 / 秒） |
| 枯渇後の再ダッシュ可能ライン | 15 |

- 悴みペナルティは**消費のみ**に掛かり、回復速度には影響しない
- スタミナが 0 になると「消耗（Exhausted）」状態になり、15 まで回復するまでダッシュできない。
  このヒステリシスが無いと 0 付近でダッシュが点滅するため入れている（Issue には明記されていない追加仕様）
- 悴み（Cold）の**上昇・下降ロジックは Phase 5 の担当**。Phase 2 では値を保持してペナルティに使うだけ

### 1.4 視点

| 項目 | 値 |
| --- | --- |
| マウス感度 | 0.12 度 / delta（`PlayerLookSettingsSO` で 0.01〜2.0） |
| ゲームパッド感度 | 4.0 度 / 秒（別係数） |
| ピッチ制限 | -85° 〜 +85° |
| Y 軸反転 | 設定で切替可能 |

- ヨーは Player 本体、ピッチは CameraPivot に適用する
- 感度は毎フレーム SO から読むため、Play Mode 中に値を変えても即反映される
- マウスとゲームパッドは delta のスケールが違うため、感度係数を分けている
  （ゲームパッドのみ `Time.deltaTime` を掛ける）

### 1.5 姿勢による当たり判定・視点高さ

| 姿勢 | CharacterController 高さ | 視点高さ |
| --- | --- | --- |
| 立ち | 1.8 m | 1.6 m |
| しゃがみ | 1.1 m | 0.95 m |
| 伏せ | 0.6 m | 0.45 m |

遷移は 0.15 秒で `SmoothDamp` 補間する。高さ変更時は `center` も追従させ、
カプセルの足元が同じ地面に接したままになるようにしている。

### 1.6 テストシーン

`Assets/_Project/Scenes/Sandbox_Player.unity`

- 100m × 100m の地面
- 速度確認用の距離マーカー（+Z 方向に 10m 刻みで 4 本）
- 壁 / 箱 / しゃがみ用の低いバー / 25° の傾斜
- Player プレハブ配置済み、仮 HUD 表示あり
