# Unity MCP セットアップ（AI エージェント連携）

Unity Editor を MCP サーバーとして公開し、Claude Code などの AI エージェントから
シーン・Console・アセット・Editor 操作にアクセスできるようにする。

本プロジェクトは Unity 公式の **Unity MCP**（`com.unity.ai.assistant` パッケージに同梱）を使う。
コミュニティ製の別 MCP サーバー（uv / Node.js ベース）は導入しない。

## 前提

| 項目 | 値 |
| --- | --- |
| Unity | `6000.3.4f1`（Unity 6.0 以降が必要） |
| パッケージ | `com.unity.ai.assistant` `2.19.0-pre.2`（`Packages/manifest.json` に記載済み） |
| クライアント | Claude Code |

パッケージはすでに `manifest.json` に入っているため、プロジェクトを開けば自動で解決される。
手動で入れ直したい場合は Unity Editor のメニューから
`Tools > Snowball > Install Unity MCP (AI Assistant)` を実行する。

## セットアップ手順

### 1. Unity Editor でプロジェクトを開く

Unity Hub から本プロジェクトを開く。パッケージのインポート完了まで待つ。

### 2. Unity Bridge が起動しているか確認する

`Edit > Project Settings > AI > Unity MCP` を開き、**Unity Bridge** が緑色の
`Running` になっていることを確認する。
Editor 起動時に自動で走るが、`Stopped` の場合は `Start` を押す。

### 3. Claude Code を登録する

同じ設定画面の **Integrations** セクションを展開し、`Claude Code` を選んで
`Configure` を押す。MCP サーバー設定が自動で書き込まれる。

自動設定がうまくいかない場合は、リレーバイナリを直接 MCP サーバーとして登録する：

```bash
claude mcp add unity -- ~/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64 --mcp
```

プラットフォーム別のリレーバイナリのパス：

| OS | パス |
| --- | --- |
| macOS (Apple Silicon) | `~/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64` |
| macOS (Intel) | `~/.unity/relay/relay_mac_x64.app/Contents/MacOS/relay_mac_x64` |
| Windows | `%USERPROFILE%\.unity\relay\relay_win_x64.exe` |
| Linux | `~/.unity/relay/relay_linux_x64` |

> リレーバイナリは Unity Editor が GUI で起動し、AI Assistant パッケージを初期化した時点で
> `~/.unity/relay/` に展開される。バッチモード起動だけでは作られない。

### 4. 接続を承認する

エージェントが初回接続すると、Unity 側に pending connection の通知が出る。
`Edit > Project Settings > AI > Unity MCP` で `Accept` を押す。

### 5. 動作確認

Claude Code に以下を投げて、Unity Console の内容が返ってくれば成功。

```
Unity の Console メッセージを読んで、警告とエラーを要約して
```

## 接続確認コマンド

```bash
claude mcp list
```

`unity` が `Connected` になっていれば利用可能。

## トラブルシューティング

| 症状 | 対処 |
| --- | --- |
| Bridge が `Stopped` のまま | Editor を再起動する。`Project Settings > AI > Unity MCP` で `Start` を押す |
| `claude mcp list` に出ない | Claude Code を再起動する（MCP 設定は起動時に読み込まれる） |
| リレーバイナリが無い | Unity Editor を **GUI で** 一度起動する（バッチモードでは展開されない） |
| 接続が拒否される | `Project Settings > AI > Unity MCP` の pending connection を Accept する |
| エージェントの編集が反映されない | Unity Editor にフォーカスを戻してアセットを再インポートさせる |

## 注意

- MCP 経由の操作は Editor を直接触るのと同じ権限を持つ。シーンやアセットが書き換わるため、
  作業前にコミット済みの状態にしておくこと。
- Play Mode 中の操作は状態が保存されない。シーン編集は Edit Mode で行う。
