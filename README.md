<p align="left">
  <a href="README_en.md"><img src="https://img.shields.io/badge/English Mode-blue.svg" alt="English"></a>
  <a href="README.md"><img src="https://img.shields.io/badge/日本語 モード-red.svg" alt="日本語"></a>
</p>

# GitHubReadMeStats (.NET 10 / C#)

GitHub GraphQL API を使って、プロフィール README 向けの SVG を自前生成する CLI です。  
`top-languages.svg` に加えて、`github-stats.svg`、`stats.svg`、`public-repo-totals.svg`、`pins/*.svg` も出力できます。

## このリポジトリの役割

このリポジトリは「README用SVG生成ツール」です。  
実運用では、各ユーザーのプロフィールリポジトリ（`<user>/<user>`）側の GitHub Actions から本ツールを呼び出す構成を推奨します。

### できること一覧

- 所有リポジトリの言語使用割合を集計し、`top-languages.svg` を生成
- Stars/Commits/PRs/Issues/Rankを集計し、`github-stats.svg`を生成
- Contributions推移 + Public/Private/Forked repo数を集計し、`stats.svg`を生成
- Public repoのTraffic/Fork/Watch/Starを集計し、`public-repo-totals.svg`を生成
- 指定リポジトリの個別カード、`pins/<owner>-<repo>.svg`を生成
- `cards-config.json` で言語色・言語アイコン・リポジトリアイコンを設定できます。
- `cards-config.json` の `repositories` で指定したリポジトリのTraffic日次データを `output/traffic-history.json` に蓄積し、累積表示をサポートします。
- README の指定セクションを自動更新可能（`--update-readme`）

### Tech Stack

- C# / .NET 10 (`net10.0`)
- GitHub GraphQL API + GitHub REST API（Traffic）

### 出力される SVG の説明

- `top-languages.svg`: ユーザーが所有するリポジトリを対象に、言語使用割合を上位 N 件で可視化するカードです。
- `github-stats.svg`: Stars / Commits(last year) / PRs / Issues / Contributed repositories とランクを表示する総合サマリーカードです。
- `stats.svg`: Contributions 推移グラフと、Public/Private/Forked リポジトリ数などのプロフィール統計を表示するカードです。
- `public-repo-totals.svg`: Publicリポジトリを対象に、Traffic 合計（Git Clones / Unique Cloners / Total Views / Unique Visitors）と Fork/Watch/Starred 合計を表示するカードです。
- `pins/*.svg`: `cards-config.json` の `repositories` で指定した各リポジトリの個別カードです。説明文、言語、スター/フォーク、Traffic（取得可能な場合）を表示します。

### SVGサンプル

テーマ別サンプル一覧: [theme-sample.md](./theme-sample.md)

以下はこのリポジトリに保存している `indigo-night` テーマの出力例です。

#### `top-languages.svg`

![top-languages sample](./theme-sample/indigo-night/top-languages.svg)

#### `github-stats.svg` / `stats.svg`

<div align="center">
  <img width="49%" src="./theme-sample/indigo-night/github-stats.svg" alt="github-stats sample" />
  <img width="49%" src="./theme-sample/indigo-night/stats.svg" alt="stats sample" />
</div>

#### `public-repo-totals.svg`

![public-repo-totals sample](./theme-sample/indigo-night/public-repo-totals.svg)

#### `pins/*.svg`

<div align="center">
  <img width="70%" src="./theme-sample/indigo-night/pins/ochtum-GitHubReadmeStats.svg" alt="pin sample" />
</div>

### Requirements

- .NET SDK 10
- GitHub Personal Access Token (`GH_TOKEN` または `GITHUB_TOKEN` 環境変数)

### Token 権限の目安

- Classic PAT: `repo` を付与すると private repo を含めて扱いやすいです。
- Fine-grained PAT: 実行対象ユーザーの必要なリポジトリに `Contents: Read` を付与してください。
- リポジトリ card に traffic 指標を表示する場合、Fine-grained PAT では `Administration: Read` も必要です（GitHub Traffic API 要件）。
- traffic 指標を表示したいリポジトリは、Fine-grained PAT の `Repository access` に含めてください（`cards-config.json` の対象 repo すべて）。
- private repo を集計/カード化する場合は、その private repo への参照権限が必要です。

![Section divider](./assets/dividers/divider-blue-solid-bold.svg)

## 事前準備

プロフィールリポジトリで動かす前に、次を準備してください。

1. GitHub PAT を作成

- GitHub 右上アイコン -> `Settings` -> `Developer settings` -> `Personal access tokens` を開く
- `Tokens (classic)` なら `Generate new token (classic)` で発行し、`repo` スコープを付与
- `Fine-grained tokens` を使う場合は、対象リポジトリに `Contents: Read` を付与
- traffic 指標を使う場合は、同じく `Administration: Read` も付与
- 発行後、表示されるトークン文字列を控える（再表示できません）

2. プロフィールリポジトリ (`<user>/<user>`) に Secrets を設定

- `Settings` -> `Secrets and variables` -> `Actions` -> `New repository secret`
- `GH_TOKEN`: 1で発行した PAT を設定
- `EXCLUDED_LANGUAGES` (任意): 除外したい言語を CSV で設定  
  例: `html,css,dockerfile,jupyter notebook`

3. private ツールリポジトリを checkout する場合の追加要件

- workflow の `actions/checkout` で `repository: ochtum/GitHubReadmeStats` を指定するなら、`GH_TOKEN` がそのリポジトリを読める必要があります
- workflow 側で `token: ${{ secrets.GH_TOKEN }}` を指定してください（READMEのサンプルにコメント付きで記載）

![Section divider](./assets/dividers/divider-blue-solid-bold.svg)

## Quick Start (ローカルで実行する場合)

### 1. 本リポジトリをクローン

- Linux / WSL (bash):

```bash
git clone https://github.com/ochtum/GitHubReadmeStats.git
cd GitHubReadmeStats
```

- Windows (PowerShell):

```powershell
git clone https://github.com/ochtum/GitHubReadmeStats.git
Set-Location GitHubReadmeStats
```

### 2. .NET SDK 10 をインストール

- `dotnet` コマンドの実行には .NET SDK が必要です。
- Windows:
  - `winget install --id Microsoft.DotNet.SDK.10 --exact`
  - または公式インストーラー: https://dotnet.microsoft.com/download/dotnet/10.0
- Linux / WSL:
  - 公式手順: https://learn.microsoft.com/dotnet/core/install/linux
- インストール確認: `dotnet --version` を実行し、`10.x` が表示されることを確認

### Linux / WSL (bash)

```bash
export GH_TOKEN=ghp_xxxxxxxxxxxxxxxxxxxx

dotnet run --project src/GitHubReadMeStats.Cli/GitHubReadMeStats.Cli.csproj -- \
  --output output \
  --exclude-languages "html,css,dockerfile" \
  --top 6 \
  --cards-config cards-config.json
```

### Windows (PowerShell)

```powershell
$env:GH_TOKEN="ghp_xxxxxxxxxxxxxxxxxxxx"

dotnet run --project src/GitHubReadMeStats.Cli/GitHubReadMeStats.Cli.csproj -- `
  --output output `
  --exclude-languages "html,css,dockerfile" `
  --top 6 `
  --cards-config cards-config.json
```

### cards-config.json

`--cards-config` を指定すると `github-stats.svg`、`stats.svg`、`public-repo-totals.svg`、`pins/*.svg` が生成されます。

```json
{
  "username": "ochtum",
  "theme": "indigo-night",
  "displayTimeZone": "Asia/Tokyo",
  "displayTimeZoneLabel": "JST",
  "languageColors": {
    "JavaScript": "#f1e05a",
    "TypeScript": "oklch(0.72 0.16 248)"
  },
  "languageIcons": {
    "JavaScript": "./assets/icons/javascript.svg",
    "TypeScript": "./assets/icons/typescript.svg"
  },
  "repositories": [
    "ochtum/CaptureScreenMCP",
    {
      "owner": "ochtum",
      "name": "SlackEmojiBookmaker",
      "languageColor": "#ffd54f",
      "languageIcon": "./assets/icons/js-alt.png",
      "icon": "./assets/icons/slack-emoji.png"
    },
    "microsoft/vscode-generator-code",
    {
      "owner": "tldraw",
      "name": "tldraw",
      "icon": "./assets/icons/tldraw.svg"
    }
  ]
}
```

補足:

- `languageColors`: 言語名単位の色上書き（`PrimaryLanguage` に一致したとき適用）
- `repositories[].languageColor`: そのリポジトリだけの色上書き（`languageColors` がない場合に適用）
- `languageIcons`: 言語名単位のアイコン上書き（`PrimaryLanguage` に一致したとき適用）
- `repositories[].languageIcon`: そのリポジトリだけの言語アイコン上書き（`languageIcons` がない場合に適用）
- `theme`: 全カード共通のプリセットテーマ名。`indigo-night` (既定), `cobalt`, `ocean`, `teal`, `emerald`, `amber`, `coral`, `violet`, `graphite`, `sakura`, `rose-petal`, `lavender-mist`, `peach-cream`, `mint-bloom`, `neon-night` をサポート。`neon-night` は `mainColor` 導入前デザインを再現
- `mainColor`: 全カード共通のメインカラー（hex / `oklch(...)`）。`theme` が同時指定されている場合は `theme` を優先。背景・罫線・文字・アイコン/ラベル色を自動調整（言語カラーは対象外）
- `repositories[].icon`: アイコン指定（`cards-config.json` からの相対パス、絶対パス、`https://...`、`data:image/...` をサポート）
- `displayTimeZone`: `updated` 表示時刻のタイムゾーン（未指定時は `UTC`）
- `displayTimeZoneLabel`: 表示ラベル（例: `JST`。未指定時は `UTC` / `UTC+09:00` / `Asia/Tokyo` などを自動決定）

### CLI Options

- `--github-token`, `-t`: GitHub token
- `--output`, `-o`: 言語カード SVG の出力先ファイルまたは出力先ディレクトリ (default: `output` -> `output/top-languages.svg`)。`--cards-config` 使用時、stats/pin/public もこの親ディレクトリ配下に出力
- `--exclude-languages`, `-x`: 除外言語 CSV
- `--top`: 表示する上位言語数 (`1..20`)
- `--include-forks`: fork を集計対象に含める
- `--include-archived`: archived を集計対象に含める
- `--update-readme`: README 更新対象パス
- `--pins-columns`: README の pins 表示列数 (`1` or `2`, default: `2`)
- `--top-languages-start-marker`: Top Languages セクション開始マーカー
- `--top-languages-end-marker`: Top Languages セクション終了マーカー
- `--stats-start-marker`: GitHub Stats セクション開始マーカー
- `--stats-end-marker`: GitHub Stats セクション終了マーカー
- `--pins-own-start-marker`: 自分IDリポジトリ pins セクション開始マーカー
- `--pins-own-end-marker`: 自分IDリポジトリ pins セクション終了マーカー
- `--pins-external-start-marker`: 外部リポジトリ pins セクション開始マーカー
- `--pins-external-end-marker`: 外部リポジトリ pins セクション終了マーカー
- `--cards-config`: stats/pin カード生成設定 JSON
- `--cards-output-dir`: 互換用の上書きオプション。未指定時は `--output` の親ディレクトリを使用

`--update-readme` は各セクションの start/end マーカーが README に存在する場合のみ更新します。  
マーカーが見つからないセクションは追記せず、そのセクションのみスキップします。
画像パスは自動解決され、`--output` / `--cards-output-dir` 配下の既定ファイル名（`top-languages.svg`, `stats.svg`, `public-repo-totals.svg`, `github-stats.svg`, `pins/*.svg`）を使用します。

![Section divider](./assets/dividers/divider-blue-solid-bold.svg)

## プロフィールリポジトリでの導入手順

対象は `<user>/<user>` プロフィールリポジトリです。

1. `.github/workflows/update-profile-readme-stats.yml` を作成

```yaml
name: update-profile-readme-stats

on:
  schedule:
    - cron: "0 0 * * 1"
  workflow_dispatch:

permissions:
  contents: write

jobs:
  update:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout profile repository
        uses: actions/checkout@v4

      - name: Checkout stats generator repository
        uses: actions/checkout@v4
        with:
          repository: ochtum/GitHubReadmeStats
          ref: main
          path: tools/github-readme-stats
          # ツールrepoをprivate運用する場合のみ必要
          # token: ${{ secrets.GH_TOKEN }}

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Generate cards
        env:
          GH_TOKEN: ${{ secrets.GH_TOKEN }}
          EXCLUDED_LANGUAGES: ${{ secrets.EXCLUDED_LANGUAGES }}
        run: |
          mkdir -p output output/pins
          dotnet run --project tools/github-readme-stats/src/GitHubReadMeStats.Cli/GitHubReadMeStats.Cli.csproj --configuration Release -- \
            --output output \
            --exclude-languages "${EXCLUDED_LANGUAGES}" \
            --top 6 \
            --cards-config cards-config.json

      - name: Commit and push if changed
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git add README.md cards-config.json output/top-languages.svg output/github-stats.svg output/stats.svg output/public-repo-totals.svg output/traffic-history.json output/pins/*.svg
          if git diff --cached --quiet; then
            echo "No changes to commit"
            exit 0
          fi
          git commit -m "chore: update profile readme stats"
          git push
```

2. リポジトリ直下に `cards-config.json` を作成

```json
{
  "username": "your-github-id",
  "theme": "indigo-night",
  "repositories": [
    "your-github-id/your-repo-1",
    "your-github-id/your-repo-2",
    "microsoft/vscode-generator-code"
  ]
}
```

3. 事前準備セクションに従って Secrets を設定

- `GH_TOKEN`: GraphQL 取得用の PAT
- `EXCLUDED_LANGUAGES`: 任意 (例: `html,css,dockerfile`)

4. `Actions -> update-profile-readme-stats -> Run workflow` を実行

![Section divider](./assets/dividers/divider-blue-solid-bold.svg)

## README.md への埋め込みサンプル

`<user>/<user>` の `README.md` に、次のように記載すると生成済みカードを表示できます。

```md
## Weekly Update

<p align="center">
  <a href="https://github.com/ochtum/GitHubReadmeStats">
    <img src="./output/top-languages.svg" alt="Top Languages" height="250" />
  </a>
</p>

## GitHub Stats

![GitHub stats summary](./output/github-stats.svg)

![GitHub stats](./output/stats.svg)

![Public repository totals](./output/public-repo-totals.svg)

## My Projects

<a href="https://github.com/your-github-id/your-repo-1">
  <img align="center" src="./output/pins/your-github-id-your-repo-1.svg" />
</a>
<a href="https://github.com/microsoft/vscode-generator-code">
  <img align="center" src="./output/pins/microsoft-vscode-generator-code.svg" />
</a>
```

`pins` のファイル名は `owner-repo.svg` 形式です。  
例: `microsoft/vscode-generator-code` -> `./output/pins/microsoft-vscode-generator-code.svg`

Traffic累積を維持するには、workflow の commit 対象に `output/traffic-history.json` を含めてください。

![Section divider](./assets/dividers/divider-blue-solid-bold.svg)

## 制約

- `top-languages` 集計対象は、実行トークンの `viewer` が所有するリポジトリです。
- `pins` は `cards-config.json` で指定した `owner/repo` を個別取得します。
- アクセス権のない private repo は取得できません。
- Traffic API は直近 14 日の日次データしか取得できません。`output/traffic-history.json`（`cards-config.json` の `repositories` 指定分のみ保持）に日次を積み上げることで、カードには「収集開始日以降」の累積を表示します。
- `Unique cloners/visitors` の全期間ユニーク人数を厳密に復元するAPIはないため、累積表示は「日次 uniques の合算」です。

![Section divider](./assets/dividers/divider-blue-solid-bold.svg)

## 定期実行したい場合

- 導入手順の workflow サンプルには、すでに `schedule` が含まれています。
- 実行時刻を変えたい場合は、`cron` の値だけ変更してください（UTC基準）。
- 例: 毎週月曜 09:00 JST に実行したい場合は `0 0 * * 1`（UTC）です。
- `schedule` 実行はデフォルトブランチの最新コミットに対して実行されます。
- `schedule` の最短間隔は 5 分です。
- GitHub 側の高負荷時は、`schedule` 実行が遅延することがあります。
- public リポジトリでは、60 日間リポジトリ活動がないと schedule が自動無効化される場合があります。無効化された場合は `Actions` 画面から再有効化してください。

```yaml
on:
  schedule:
    - cron: "0 0 * * 1" # ここを変更する
  workflow_dispatch:
```

よく使う例:

- 毎日 09:00 JST: `0 0 * * *`
- 毎週 月曜 09:00 JST: `0 0 * * 1`
- 毎月 1日 09:00 JST: `0 0 1 * *`

![Section divider](./assets/dividers/divider-blue-solid-bold.svg)

## 参考

- https://zenn.dev/chot/articles/30b08c452795eb
- https://github.com/4okimi7uki/repo-spector

![Section divider](./assets/dividers/divider-blue-solid-bold.svg)

## ライセンス

- このプロジェクトは [MIT License](./LICENSE) の下で提供されています。
- 参考元・再利用コードに関する第三者ライセンス表記は [THIRD_PARTY_NOTICES.md](./THIRD_PARTY_NOTICES.md) を参照してください。
