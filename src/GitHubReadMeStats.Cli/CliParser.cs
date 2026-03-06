namespace GitHubReadMeStats.Cli;

internal static class CliParser
{
    public const string ApplicationName = "github-readme-stats";
    public const string ApplicationVersion = "0.1.0";

    private const string DefaultOutputPath = "output";
    private const string DefaultLanguageCardFileName = "top-languages.svg";
    private const int DefaultTopCount = 6;
    private const string DefaultTopLanguagesStartMarker = "<!-- github-readme-stats:start -->";
    private const string DefaultTopLanguagesEndMarker = "<!-- github-readme-stats:end -->";
    private const string DefaultStatsStartMarker = "<!-- github-readme-stats:stats:start -->";
    private const string DefaultStatsEndMarker = "<!-- github-readme-stats:stats:end -->";
    private const string DefaultOwnPinsStartMarker = "<!-- github-readme-stats:pins-own:start -->";
    private const string DefaultOwnPinsEndMarker = "<!-- github-readme-stats:pins-own:end -->";
    private const string DefaultExternalPinsStartMarker = "<!-- github-readme-stats:pins-external:start -->";
    private const string DefaultExternalPinsEndMarker = "<!-- github-readme-stats:pins-external:end -->";

    public static CliParseResult Parse(string[] args)
    {
        string? token = null;
        string outputPath = DefaultOutputPath;
        string excludedLanguages = Environment.GetEnvironmentVariable("EXCLUDED_LANGUAGES") ?? string.Empty;
        int top = DefaultTopCount;
        bool includeForks = false;
        bool includeArchived = false;
        string? updateReadmePath = null;
        string topLanguagesStartMarker = DefaultTopLanguagesStartMarker;
        string topLanguagesEndMarker = DefaultTopLanguagesEndMarker;
        string statsStartMarker = DefaultStatsStartMarker;
        string statsEndMarker = DefaultStatsEndMarker;
        string ownPinsStartMarker = DefaultOwnPinsStartMarker;
        string ownPinsEndMarker = DefaultOwnPinsEndMarker;
        string externalPinsStartMarker = DefaultExternalPinsStartMarker;
        string externalPinsEndMarker = DefaultExternalPinsEndMarker;
        int pinsColumnsForReadme = 2;
        string? cardsConfigPath = null;
        string? cardsOutputDir = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg is "-h" or "--help")
            {
                return new CliParseResult(null, null, ShowHelp: true, ShowVersion: false);
            }

            if (arg is "-v" or "--version")
            {
                return new CliParseResult(null, null, ShowHelp: false, ShowVersion: true);
            }

            if (TryParseInlineValue(arg, "--github-token", out string tokenValue))
            {
                token = tokenValue;
                continue;
            }

            if (TryParseInlineValue(arg, "--output", out string outputValue))
            {
                outputPath = outputValue;
                continue;
            }

            if (TryParseInlineValue(arg, "--exclude-languages", out string excludeValue))
            {
                excludedLanguages = excludeValue;
                continue;
            }

            if (TryParseInlineValue(arg, "--top", out string topValue))
            {
                if (!int.TryParse(topValue, out top))
                {
                    return new CliParseResult(null, $"--top expects integer value, but got: {topValue}", false, false);
                }

                continue;
            }

            if (TryParseInlineValue(arg, "--update-readme", out string updateReadmeValue))
            {
                updateReadmePath = updateReadmeValue;
                continue;
            }

            if (TryParseInlineValue(arg, "--pins-columns", out string pinsColumnsValue))
            {
                if (!int.TryParse(pinsColumnsValue, out pinsColumnsForReadme))
                {
                    return new CliParseResult(null, $"--pins-columns expects integer value, but got: {pinsColumnsValue}", false, false);
                }

                continue;
            }

            if (TryParseInlineValue(arg, "--top-languages-start-marker", out string topLanguagesStartMarkerValue))
            {
                topLanguagesStartMarker = topLanguagesStartMarkerValue;
                continue;
            }

            if (TryParseInlineValue(arg, "--top-languages-end-marker", out string topLanguagesEndMarkerValue))
            {
                topLanguagesEndMarker = topLanguagesEndMarkerValue;
                continue;
            }

            if (TryParseInlineValue(arg, "--stats-start-marker", out string statsStartMarkerValue))
            {
                statsStartMarker = statsStartMarkerValue;
                continue;
            }

            if (TryParseInlineValue(arg, "--stats-end-marker", out string statsEndMarkerValue))
            {
                statsEndMarker = statsEndMarkerValue;
                continue;
            }

            if (TryParseInlineValue(arg, "--pins-own-start-marker", out string ownPinsStartMarkerValue))
            {
                ownPinsStartMarker = ownPinsStartMarkerValue;
                continue;
            }

            if (TryParseInlineValue(arg, "--pins-own-end-marker", out string ownPinsEndMarkerValue))
            {
                ownPinsEndMarker = ownPinsEndMarkerValue;
                continue;
            }

            if (TryParseInlineValue(arg, "--pins-external-start-marker", out string externalPinsStartMarkerValue))
            {
                externalPinsStartMarker = externalPinsStartMarkerValue;
                continue;
            }

            if (TryParseInlineValue(arg, "--pins-external-end-marker", out string externalPinsEndMarkerValue))
            {
                externalPinsEndMarker = externalPinsEndMarkerValue;
                continue;
            }

            if (TryParseInlineValue(arg, "--cards-config", out string cardsConfigValue))
            {
                cardsConfigPath = cardsConfigValue;
                continue;
            }

            if (TryParseInlineValue(arg, "--cards-output-dir", out string cardsOutputValue))
            {
                cardsOutputDir = cardsOutputValue;
                continue;
            }

            switch (arg)
            {
                case "-t":
                case "--github-token":
                    token = ReadNextValue(args, ref i);
                    if (token is null)
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    break;

                case "-o":
                case "--output":
                {
                    string? value = ReadNextValue(args, ref i);
                    if (value is null)
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    outputPath = value;
                    break;
                }

                case "-x":
                case "--exclude-languages":
                case "--exclude-lang":
                {
                    string? value = ReadNextValue(args, ref i);
                    if (value is null)
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    excludedLanguages = value;
                    break;
                }

                case "--top":
                {
                    string? topString = ReadNextValue(args, ref i);
                    if (!int.TryParse(topString, out top))
                    {
                        return new CliParseResult(null, $"--top expects integer value, but got: {topString}", false, false);
                    }

                    break;
                }

                case "--include-forks":
                    includeForks = true;
                    break;

                case "--include-archived":
                    includeArchived = true;
                    break;

                case "--update-readme":
                    updateReadmePath = ReadNextValue(args, ref i);
                    if (updateReadmePath is null)
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    break;

                case "--pins-columns":
                {
                    string? pinsColumnsString = ReadNextValue(args, ref i);
                    if (!int.TryParse(pinsColumnsString, out pinsColumnsForReadme))
                    {
                        return new CliParseResult(null, $"--pins-columns expects integer value, but got: {pinsColumnsString}", false, false);
                    }

                    break;
                }

                case "--top-languages-start-marker":
                    topLanguagesStartMarker = ReadNextValue(args, ref i) ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(topLanguagesStartMarker))
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    break;

                case "--top-languages-end-marker":
                    topLanguagesEndMarker = ReadNextValue(args, ref i) ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(topLanguagesEndMarker))
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    break;

                case "--stats-start-marker":
                    statsStartMarker = ReadNextValue(args, ref i) ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(statsStartMarker))
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    break;

                case "--stats-end-marker":
                    statsEndMarker = ReadNextValue(args, ref i) ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(statsEndMarker))
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    break;

                case "--pins-own-start-marker":
                    ownPinsStartMarker = ReadNextValue(args, ref i) ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(ownPinsStartMarker))
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    break;

                case "--pins-own-end-marker":
                    ownPinsEndMarker = ReadNextValue(args, ref i) ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(ownPinsEndMarker))
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    break;

                case "--pins-external-start-marker":
                    externalPinsStartMarker = ReadNextValue(args, ref i) ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(externalPinsStartMarker))
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    break;

                case "--pins-external-end-marker":
                    externalPinsEndMarker = ReadNextValue(args, ref i) ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(externalPinsEndMarker))
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    break;

                case "--cards-config":
                    cardsConfigPath = ReadNextValue(args, ref i);
                    if (cardsConfigPath is null)
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    break;

                case "--cards-output-dir":
                    cardsOutputDir = ReadNextValue(args, ref i) ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(cardsOutputDir))
                    {
                        return new CliParseResult(null, $"{arg} requires a value.", false, false);
                    }

                    break;

                default:
                    return new CliParseResult(null, $"Unknown option: {arg}", false, false);
            }
        }

        if (top < 1 || top > 20)
        {
            return new CliParseResult(null, "--top must be between 1 and 20.", false, false);
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return new CliParseResult(null, "--output requires a non-empty value.", false, false);
        }

        string resolvedOutputPath = ResolveOutputPath(outputPath);

        if (string.IsNullOrWhiteSpace(topLanguagesStartMarker) || string.IsNullOrWhiteSpace(topLanguagesEndMarker))
        {
            return new CliParseResult(null, "--top-languages-start-marker and --top-languages-end-marker require non-empty values.", false, false);
        }

        if (string.IsNullOrWhiteSpace(statsStartMarker) || string.IsNullOrWhiteSpace(statsEndMarker))
        {
            return new CliParseResult(null, "--stats-start-marker and --stats-end-marker require non-empty values.", false, false);
        }

        if (string.IsNullOrWhiteSpace(ownPinsStartMarker) || string.IsNullOrWhiteSpace(ownPinsEndMarker))
        {
            return new CliParseResult(null, "--pins-own-start-marker and --pins-own-end-marker require non-empty values.", false, false);
        }

        if (string.IsNullOrWhiteSpace(externalPinsStartMarker) || string.IsNullOrWhiteSpace(externalPinsEndMarker))
        {
            return new CliParseResult(null, "--pins-external-start-marker and --pins-external-end-marker require non-empty values.", false, false);
        }

        if (pinsColumnsForReadme is < 1 or > 2)
        {
            return new CliParseResult(null, "--pins-columns must be 1 or 2.", false, false);
        }

        if (string.IsNullOrWhiteSpace(updateReadmePath))
        {
            updateReadmePath = null;
        }

        if (string.IsNullOrWhiteSpace(cardsConfigPath))
        {
            cardsConfigPath = null;
        }

        if (cardsOutputDir is not null && string.IsNullOrWhiteSpace(cardsOutputDir))
        {
            return new CliParseResult(null, "--cards-output-dir requires a non-empty value.", false, false);
        }

        string resolvedCardsOutputDir = string.IsNullOrWhiteSpace(cardsOutputDir)
            ? ResolveCardsOutputDir(resolvedOutputPath)
            : cardsOutputDir!;

        token = token?.Trim();
        token ??= Environment.GetEnvironmentVariable("GH_TOKEN")?.Trim();
        token ??= Environment.GetEnvironmentVariable("GITHUB_TOKEN")?.Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            return new CliParseResult(null, "GitHub token is required. Pass --github-token or set GH_TOKEN/GITHUB_TOKEN.", false, false);
        }

        string resolvedToken = token;
        string[] excludedLanguageList = SplitCsv(excludedLanguages);

        var options = new CliOptions(
            resolvedToken,
            resolvedOutputPath,
            excludedLanguageList,
            top,
            includeForks,
            includeArchived,
            updateReadmePath,
            topLanguagesStartMarker,
            topLanguagesEndMarker,
            statsStartMarker,
            statsEndMarker,
            ownPinsStartMarker,
            ownPinsEndMarker,
            externalPinsStartMarker,
            externalPinsEndMarker,
            pinsColumnsForReadme,
            cardsConfigPath,
            resolvedCardsOutputDir);

        return new CliParseResult(options, null, false, false);
    }

    public static string GetHelpText()
    {
        return
            "GitHubReadMeStats CLI (.NET 10)\n" +
            "\n" +
            "Usage:\n" +
            "  dotnet run --project src/GitHubReadMeStats.Cli -- [options]\n" +
            "\n" +
            "Options:\n" +
            "  -h, --help                      Show help\n" +
            "  -v, --version                   Show version\n" +
            "  -t, --github-token <token>      GitHub PAT (or GH_TOKEN / GITHUB_TOKEN env)\n" +
            "  -o, --output <path>             Language card output path or directory (default: output -> output/top-languages.svg)\n" +
            "  -x, --exclude-languages <csv>   Exclude language names (default: EXCLUDED_LANGUAGES env)\n" +
            "      --top <n>                   Number of languages to render (1-20, default: 6)\n" +
            "      --include-forks             Include fork repositories\n" +
            "      --include-archived          Include archived repositories\n" +
            "      --update-readme <path>      Update markdown section in README\n" +
            "      --pins-columns <1|2>        Pin card columns in README markdown (default: 2)\n" +
            "      --top-languages-start-marker <marker> top-languages section start marker\n" +
            "      --top-languages-end-marker <marker> top-languages section end marker\n" +
            "      --stats-start-marker <marker> stats section start marker\n" +
            "      --stats-end-marker <marker> stats section end marker\n" +
            "      --pins-own-start-marker <marker> own pins section start marker\n" +
            "      --pins-own-end-marker <marker> own pins section end marker\n" +
            "      --pins-external-start-marker <marker> external pins section start marker\n" +
            "      --pins-external-end-marker <marker> external pins section end marker\n" +
            "      --cards-config <path>       JSON config for stats/pin card generation\n" +
            "      --cards-output-dir <path>   (Compatibility) Override cards output directory\n";
    }

    private static bool TryParseInlineValue(string arg, string optionName, out string value)
    {
        string prefix = optionName + "=";
        if (arg.StartsWith(prefix, StringComparison.Ordinal))
        {
            value = arg[prefix.Length..];
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string ResolveCardsOutputDir(string outputPath)
    {
        string? parentDirectory = Path.GetDirectoryName(outputPath);
        return string.IsNullOrWhiteSpace(parentDirectory) ? "." : parentDirectory;
    }

    private static string ResolveOutputPath(string outputPath)
    {
        string trimmed = outputPath.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return trimmed;
        }

        bool looksLikeDirectory =
            trimmed.EndsWith(Path.DirectorySeparatorChar) ||
            trimmed.EndsWith(Path.AltDirectorySeparatorChar) ||
            !Path.HasExtension(trimmed);

        if (looksLikeDirectory)
        {
            return Path.Combine(trimmed, DefaultLanguageCardFileName);
        }

        return trimmed;
    }

    private static string? ReadNextValue(string[] args, ref int index)
    {
        int next = index + 1;
        if (next >= args.Length)
        {
            return null;
        }

        string value = args[next];
        if (value.StartsWith('-'))
        {
            return null;
        }

        index = next;
        return value;
    }

    private static string[] SplitCsv(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
