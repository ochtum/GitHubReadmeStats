# README.md Auto-Update Example

This file is a practical example for automatically updating your profile `README.md` with `GitHubReadmeStats`.  
Replace `<PATH_TO_GITHUB_README_STATS>` and `<PATH_TO_PROFILE_REPO>` with paths in your own environment.

## Full CLI Example (Run Locally)

```powershell
$statsRepoPath = "<PATH_TO_GITHUB_README_STATS>"
$profileRepoPath = "<PATH_TO_PROFILE_REPO>"

dotnet run --project "$statsRepoPath\src\GitHubReadMeStats.Cli\GitHubReadMeStats.Cli.csproj" --configuration Release -- `
  --github-token $env:GH_TOKEN `
  --output "$profileRepoPath\output" `
  --exclude-languages "ShaderLab,HLSL" `
  --top 6 `
  --update-readme "$profileRepoPath\README.md" `
  --pins-columns 2 `
  --top-languages-start-marker "<!-- github-readme-stats:start -->" `
  --top-languages-end-marker "<!-- github-readme-stats:end -->" `
  --stats-start-marker "<!-- github-readme-stats:stats:start -->" `
  --stats-end-marker "<!-- github-readme-stats:stats:end -->" `
  --pins-own-start-marker "<!-- github-readme-stats:pins-own:start -->" `
  --pins-own-end-marker "<!-- github-readme-stats:pins-own:end -->" `
  --pins-external-start-marker "<!-- github-readme-stats:pins-external:start -->" `
  --pins-external-end-marker "<!-- github-readme-stats:pins-external:end -->" `
  --cards-config "$profileRepoPath\cards-config.json" `
  --cards-output-dir "$profileRepoPath\output"
```

## `.github/workflows/update-profile-readme-stats.yml` Example

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
          # Required only if the tool repository is private
          # token: ${{ secrets.GH_TOKEN }}

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Generate cards and update README
        env:
          GH_TOKEN: ${{ secrets.GH_TOKEN }}
          EXCLUDED_LANGUAGES: ${{ secrets.EXCLUDED_LANGUAGES }}
        run: |
          mkdir -p output output/pins
          dotnet run --project tools/github-readme-stats/src/GitHubReadMeStats.Cli/GitHubReadMeStats.Cli.csproj --configuration Release -- \
            --output output \
            --exclude-languages "${EXCLUDED_LANGUAGES}" \
            --top 6 \
            --update-readme README.md \
            --pins-columns 2 \
            --top-languages-start-marker "<!-- github-readme-stats:start -->" \
            --top-languages-end-marker "<!-- github-readme-stats:end -->" \
            --stats-start-marker "<!-- github-readme-stats:stats:start -->" \
            --stats-end-marker "<!-- github-readme-stats:stats:end -->" \
            --pins-own-start-marker "<!-- github-readme-stats:pins-own:start -->" \
            --pins-own-end-marker "<!-- github-readme-stats:pins-own:end -->" \
            --pins-external-start-marker "<!-- github-readme-stats:pins-external:start -->" \
            --pins-external-end-marker "<!-- github-readme-stats:pins-external:end -->" \
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

![Section divider](./assets/dividers/divider-blue-solid-bold.svg)

## README.md Example (Before SVG Output)

Place markers in `README.md` first.  
Sections without markers are not updated automatically.

```markdown
## 🛠 Skills

![](https://skillicons.dev/icons?i=cs,php,js,ts,html,css)

<!-- github-readme-stats:start -->
<!-- github-readme-stats:end -->

## 📈 GitHub Stats

<!-- github-readme-stats:stats:start -->
<!-- github-readme-stats:stats:end -->

## ✨ My Projects

<!-- github-readme-stats:pins-own:start -->
<!-- github-readme-stats:pins-own:end -->

## 🌟 Open Source Contributions

<!-- github-readme-stats:pins-external:start -->
<!-- github-readme-stats:pins-external:end -->
```

## README.md Example (After SVG Output)

```markdown
## 🛠 Skills

![](https://skillicons.dev/icons?i=cs,php,js,ts,html,css)

<!-- github-readme-stats:start -->
<div align="center">
  <img width="100%" src="./output/top-languages.svg" alt="top-languages" />
</div>
<!-- github-readme-stats:end -->

## 📈 GitHub Stats

<!-- github-readme-stats:stats:start -->
<div align="center">
  <img width="49%" src="./output/github-stats.svg" alt="github-stats" />
  <img width="49%" src="./output/stats.svg" alt="stats" />
  <br />
  <img width="100%" src="./output/public-repo-totals.svg" alt="public-repo-totals" />
</div>
<!-- github-readme-stats:stats:end -->

## ✨ My Projects

<!-- github-readme-stats:pins-own:start -->
<p align="center">
  <a href="https://github.com/ochtum/GitHubReadmeStats"><img width="49%" src="./output/pins/ochtum-GitHubReadmeStats.svg" alt="GitHubReadmeStats" /></a>
  <a href="https://github.com/ochtum/CaptureScreenMCP"><img width="49%" src="./output/pins/ochtum-CaptureScreenMCP.svg" alt="CaptureScreenMCP" /></a>
</p>
<!-- github-readme-stats:pins-own:end -->

## 🌟 Open Source Contributions

<!-- github-readme-stats:pins-external:start -->
<p align="center">
  <a href="https://github.com/microsoft/vscode-generator-code"><img width="49%" src="./output/pins/microsoft-vscode-generator-code.svg" alt="vscode-generator-code" /></a>
  <a href="https://github.com/tldraw/tldraw"><img width="49%" src="./output/pins/tldraw-tldraw.svg" alt="tldraw" /></a>
</p>
<!-- github-readme-stats:pins-external:end -->
```
