# GitHubReadMeStats (.NET 10 / C#)

GitHub GraphQL API を使って、プロフィール README 向けの SVG を自前生成する CLI です。  
`top-languages.svg` に加えて、`stats.svg` と `pins/*.svg` も出力できます。

## このリポジトリの役割

このリポジトリは「生成ツール」です。  
実運用では、各ユーザーのプロフィールリポジトリ（`<user>/<user>`）側の GitHub Actions から本ツールを呼び出す構成を推奨します。

## Features

- C# / .NET 10 (`net10.0`)
- GitHub GraphQL API から `viewer.repositories(ownerAffiliations: OWNER)` をページング取得
- 言語集計カード: `top-languages.svg`
- プロフィールサマリーカード: `stats.svg`
- リポジトリカード: `pins/<owner>-<repo>.svg`
- `cards-config.json` で言語色の上書き (`HEX`, `OKLCH` など) と、リポジトリごとのアイコン指定が可能
- リポジトリカードに traffic 指標 (`Git Clones`, `Unique cloners`, `Total views`, `Unique visitors`) を表示
- README セクション自動更新 (`--update-readme`)

## Requirements

- .NET SDK 10
- GitHub Personal Access Token (`GH_TOKEN` または `GITHUB_TOKEN` 環境変数)

## Token 権限の目安

- Classic PAT: `repo` を付与すると private repo を含めて扱いやすいです。
- Fine-grained PAT: 実行対象ユーザーの必要なリポジトリに `Contents: Read` を付与してください。
- リポジトリ card に traffic 指標を表示する場合、Fine-grained PAT では `Administration: Read` も必要です（GitHub Traffic API 要件）。
- traffic 指標を表示したいリポジトリは、Fine-grained PAT の `Repository access` に含めてください（`cards-config.json` の対象 repo すべて）。
- private repo を集計/カード化する場合は、その private repo への参照権限が必要です。

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

## Quick Start (local)

```bash
export GH_TOKEN=ghp_xxxxxxxxxxxxxxxxxxxx

dotnet run --project src/GitHubReadMeStats.Cli/GitHubReadMeStats.Cli.csproj -- \
  --output output/top-languages.svg \
  --exclude-languages "html,css,dockerfile" \
  --top 6 \
  --cards-config cards-config.json \
  --cards-output-dir output
```

## cards-config.json

`--cards-config` を指定すると `stats.svg` と `pins/*.svg` が生成されます。

```json
{
  "username": "ochtum",
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
- `repositories[].icon`: アイコン指定（`cards-config.json` からの相対パス、絶対パス、`https://...`、`data:image/...` をサポート）
- `displayTimeZone`: `updated` 表示時刻のタイムゾーン（未指定時は `UTC`）
- `displayTimeZoneLabel`: 表示ラベル（例: `JST`。未指定時は `UTC` / `UTC+09:00` / `Asia/Tokyo` などを自動決定）

## CLI Options

- `--github-token`, `-t`: GitHub token
- `--output`, `-o`: 言語カード SVG の出力先 (default: `output/top-languages.svg`)
- `--exclude-languages`, `-x`: 除外言語 CSV
- `--top`: 表示する上位言語数 (`1..20`)
- `--include-forks`: fork を集計対象に含める
- `--include-archived`: archived を集計対象に含める
- `--update-readme`: README 更新対象パス
- `--image-path`: README に埋め込む画像パス
- `--start-marker`: README セクション開始マーカー
- `--end-marker`: README セクション終了マーカー
- `--cards-config`: stats/pin カード生成設定 JSON
- `--cards-output-dir`: stats/pin の出力先ディレクトリ (default: `output`)

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
            --output output/top-languages.svg \
            --exclude-languages "${EXCLUDED_LANGUAGES}" \
            --top 6 \
            --cards-config cards-config.json \
            --cards-output-dir output

      - name: Commit and push if changed
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git add README.md cards-config.json output/top-languages.svg output/stats.svg output/traffic-history.json output/pins/*.svg
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
![GitHub stats](./output/stats.svg)

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

## このリポジトリの Actions について

このリポジトリには自己更新用の workflow (`.github/workflows/update-readme-stats.yml`) がありますが、利用者に必須ではありません。  
公開ツールとして使うだけなら、利用者側プロフィールリポジトリの workflow だけで運用できます。

## 制約

- `top-languages` 集計対象は、実行トークンの `viewer` が所有するリポジトリです。
- `pins` は `cards-config.json` で指定した `owner/repo` を個別取得します。
- アクセス権のない private repo は取得できません。
- Traffic API は直近 14 日の日次データしか取得できません。`output/traffic-history.json` に日次を積み上げることで、カードには「収集開始日以降」の累積を表示します。
- `Unique cloners/visitors` の全期間ユニーク人数を厳密に復元するAPIはないため、累積表示は「日次 uniques の合算」です。

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

## 参考

- https://zenn.dev/chot/articles/30b08c452795eb
- https://github.com/4okimi7uki/repo-spector

