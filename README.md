# GitHubReadMeStats (.NET 10 / C#)

GitHub GraphQL API を使ってリポジトリ言語情報を集計し、README に貼れる SVG を生成する CLI です。  
`repo-spector` と Zenn 記事のフローを参考に、GitHub Actions で自動更新できる構成にしています。

## Features

- C# / .NET 10 (`net10.0`) 実装
- GitHub GraphQL API から `viewer.repositories` をページング取得
- 言語サイズを集計して `output/top-languages.svg` を出力
- `stats.svg` と `pins/*.svg` を生成して `api/pin` 相当のカードを自前運用可能
- 任意で README セクションをCLIから自動更新
- GitHub Actions で定期更新してコミット可能

## Requirements

- .NET SDK 10
- GitHub Personal Access Token (`GH_TOKEN`)

推奨スコープ:

- `repo` (private repository を含める場合)
- public のみなら最小権限で利用

## Usage

### 1. ローカル実行

```bash
export GH_TOKEN=ghp_xxxxxxxxxxxxxxxxxxxx
dotnet run --project src/GitHubReadMeStats.Cli/GitHubReadMeStats.Cli.csproj -- \
  --output output/top-languages.svg \
  --exclude-languages "html,css,dockerfile" \
  --top 6 \
  --cards-config cards-config.json \
  --cards-output-dir output \
  --update-readme README.md \
  --image-path output/top-languages.svg
```

### 2. 主なオプション

- `--github-token` / `GH_TOKEN` / `GITHUB_TOKEN`: GitHub Token
- `--output`: SVG出力先 (default: `output/top-languages.svg`)
- `--exclude-languages` (`-x`): 除外言語CSV
- `--top`: 表示する上位言語数 (`1..20`)
- `--include-forks`: fork リポジトリも集計
- `--include-archived`: archived リポジトリも集計
- `--update-readme`: README更新対象パス
- `--image-path`: READMEに埋め込む画像パス
- `--cards-config`: `stats` / `pin` カード生成対象を定義したJSON
- `--cards-output-dir`: `stats.svg` と `pins/*.svg` の出力先

### 3. cards-config.json 例

```json
{
  "username": "ochtum",
  "repositories": [
    "ochtum/CaptureScreenMCP",
    "ochtum/SlackEmojiBookmaker",
    "microsoft/vscode-generator-code",
    "tldraw/tldraw"
  ]
}
```
### 4. GitHub Actions で定期更新

`.github/workflows/update-readme-stats.yml` を用意しています。

設定する Secrets:

- `GH_TOKEN`: GitHub Personal Access Token
- `EXCLUDED_LANGUAGES`: 除外言語CSV (例: `html,css,dockerfile`)

## Output Example

`output/top-languages.svg` を README から参照します。

<!-- github-readme-stats:start -->
## GitHub Readme Stats

![Top Languages](output/top-languages.svg)

| Rank | Language | Size | Share |
| ---: | :-- | ---: | ---: |
| 1 | Python | 133.33 KB | 42.18% |
| 2 | JavaScript | 65.7 KB | 20.78% |
| 3 | HTML | 45.77 KB | 14.48% |
| 4 | CSS | 34.08 KB | 10.78% |
| 5 | C# | 19.91 KB | 6.30% |
| 6 | TypeScript | 6.39 KB | 2.02% |

_Updated: 2026-03-05 15:30 UTC_  
_Repositories: 20 / 115 (included/scanned)_
<!-- github-readme-stats:end -->

