using System.Text;
using System.Text.RegularExpressions;

namespace GitHubReadMeStats.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        CliParseResult parseResult = CliParser.Parse(args);

        if (parseResult.ShowHelp)
        {
            Console.WriteLine(CliParser.GetHelpText());
            return 0;
        }

        if (parseResult.ShowVersion)
        {
            Console.WriteLine($"{CliParser.ApplicationName} {CliParser.ApplicationVersion}");
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(parseResult.Error) || parseResult.Options is null)
        {
            Console.Error.WriteLine($"Error: {parseResult.Error}");
            Console.Error.WriteLine();
            Console.Error.WriteLine(CliParser.GetHelpText());
            return 1;
        }

        CliOptions options = parseResult.Options;

        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60),
            };

            var graphqlClient = new GitHubGraphQlClient(httpClient, options.GitHubToken);
            ViewerRepositoriesResult repositoriesResult = await graphqlClient.FetchOwnedRepositoriesAsync();

            CardsConfig? cardsConfig = null;
            if (!string.IsNullOrWhiteSpace(options.CardsConfigPath))
            {
                cardsConfig = CardsConfigLoader.Load(options.CardsConfigPath!, repositoriesResult.ViewerLogin);
            }
            TimeDisplaySettings timeDisplay = ResolveTimeDisplaySettings(cardsConfig);

            AggregationResult aggregation = LanguageAggregator.Aggregate(repositoriesResult.Repositories, options);
            if (cardsConfig is not null)
            {
                aggregation = ApplyLanguageColorOverrides(aggregation, cardsConfig.LanguageColorOverrides);
            }

            if (aggregation.Languages.Count == 0)
            {
                Console.Error.WriteLine("No language data was collected. Check repository visibility, token scopes, and filters.");
                return 1;
            }

            DateTimeOffset generatedAtUtc = DateTimeOffset.UtcNow;
            string svg = SvgRenderer.Render(repositoriesResult.ViewerLogin, aggregation, options.Top, generatedAtUtc, timeDisplay);

            EnsureParentDirectory(options.OutputPath);
            await File.WriteAllTextAsync(options.OutputPath, svg + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            Console.WriteLine($"Generated: {options.OutputPath}");
            Console.WriteLine($"Viewer: @{repositoriesResult.ViewerLogin}");
            Console.WriteLine($"Repositories (scanned/included): {aggregation.ScannedRepositoryCount}/{aggregation.IncludedRepositoryCount}");
            Console.WriteLine($"Languages: {aggregation.Languages.Count}");
            Console.WriteLine($"Total size: {SvgRenderer.ToHumanReadableBytes(aggregation.TotalBytes)}");

            if (!string.IsNullOrWhiteSpace(options.CardsConfigPath))
            {
                await GenerateAdditionalCardsAsync(
                    graphqlClient,
                    options,
                    generatedAtUtc,
                    cardsConfig!,
                    timeDisplay);
            }

            if (!string.IsNullOrWhiteSpace(options.UpdateReadmePath))
            {
                string readmePath = options.UpdateReadmePath!;
                string imagePathForReadme = ResolveReadmeImagePath(options, readmePath);

                await ReadmeUpdater.UpdateAsync(
                    readmePath,
                    options.ReadmeSectionStartMarker,
                    options.ReadmeSectionEndMarker,
                    imagePathForReadme,
                    aggregation,
                    options.Top,
                    generatedAtUtc,
                    timeDisplay);

                Console.WriteLine($"Updated README section: {readmePath}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task GenerateAdditionalCardsAsync(
        GitHubGraphQlClient graphqlClient,
        CliOptions options,
        DateTimeOffset generatedAtUtc,
        CardsConfig config,
        TimeDisplaySettings timeDisplay)
    {
        string configPath = options.CardsConfigPath!;
        string configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath)) ?? Directory.GetCurrentDirectory();

        UserSummary summary = await graphqlClient.FetchUserSummaryAsync(config.Username);
        string githubStatsSvg = GitHubStatsSummaryCardRenderer.Render(summary);
        string githubStatsPath = Path.Combine(options.CardsOutputDir, "github-stats.svg");
        EnsureParentDirectory(githubStatsPath);
        await File.WriteAllTextAsync(githubStatsPath, githubStatsSvg + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"Generated: {githubStatsPath}");

        string statsSvg = ProfileStatsCardRenderer.Render(summary, generatedAtUtc, timeDisplay);
        string statsPath = Path.Combine(options.CardsOutputDir, "stats.svg");
        EnsureParentDirectory(statsPath);
        await File.WriteAllTextAsync(statsPath, statsSvg + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"Generated: {statsPath}");

        string pinOutputDir = Path.Combine(options.CardsOutputDir, "pins");
        Directory.CreateDirectory(pinOutputDir);

        string trafficHistoryPath = Path.Combine(options.CardsOutputDir, "traffic-history.json");
        TrafficHistoryStore trafficHistory = TrafficHistoryStore.Load(trafficHistoryPath);

        int generatedPinCount = 0;
        int unavailableTrafficCount = 0;
        foreach (PinRepository repo in config.Repositories)
        {
            PinCardData baseData = await graphqlClient.FetchRepositoryCardDataAsync(repo.Owner, repo.Name);

            string? colorOverride = ResolveLanguageColorOverride(repo, config, baseData.PrimaryLanguage);
            if (!string.IsNullOrWhiteSpace(colorOverride))
            {
                baseData = baseData with
                {
                    PrimaryLanguageColor = colorOverride,
                };
            }

            string? languageIconHref = ResolveLanguageIconOverride(repo, config, baseData.PrimaryLanguage, configDirectory);
            if (!string.IsNullOrWhiteSpace(languageIconHref))
            {
                baseData = baseData with
                {
                    LanguageIconHref = languageIconHref,
                };
            }

            string? repositoryIconHref = TryResolveIconHref(repo.Icon, configDirectory);
            if (!string.IsNullOrWhiteSpace(repositoryIconHref))
            {
                baseData = baseData with
                {
                    RepositoryIconHref = repositoryIconHref,
                };
            }

            RepositoryTrafficSnapshot? trafficSnapshot = null;
            try
            {
                trafficSnapshot = await graphqlClient.TryFetchRepositoryTrafficAsync(repo.Owner, repo.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to fetch traffic for {repo.Owner}/{repo.Name}: {ex.Message}");
            }

            RepositoryTrafficTotals? trafficTotals = trafficHistory.MergeAndGetTotals(repo.Owner, repo.Name, trafficSnapshot);
            if (trafficSnapshot is null && trafficTotals is null)
            {
                unavailableTrafficCount++;
                Console.WriteLine(
                    $"Info: traffic unavailable for {repo.Owner}/{repo.Name}. " +
                    "Check GH_TOKEN repository access and 'Administration: Read' permission.");
            }

            PinCardData data = baseData with
            {
                TrafficTotals = trafficTotals,
            };

            string pinSvg = PinCardRenderer.Render(data);
            string pinPath = Path.Combine(pinOutputDir, $"{SanitizePathSegment(repo.Owner)}-{SanitizePathSegment(repo.Name)}.svg");
            await File.WriteAllTextAsync(pinPath, pinSvg + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            generatedPinCount++;
        }

        await trafficHistory.SaveAsync(trafficHistoryPath);
        Console.WriteLine($"Updated traffic history: {trafficHistoryPath}");
        if (unavailableTrafficCount > 0)
        {
            Console.WriteLine($"Traffic unavailable repositories: {unavailableTrafficCount}");
        }

        Console.WriteLine($"Generated pin cards: {generatedPinCount}");
    }

    private static TimeDisplaySettings ResolveTimeDisplaySettings(CardsConfig? config)
    {
        if (config is null || string.IsNullOrWhiteSpace(config.DisplayTimeZone))
        {
            return new TimeDisplaySettings(TimeZoneInfo.Utc, "UTC");
        }

        string configuredTimeZone = config.DisplayTimeZone.Trim();
        if (!TryResolveTimeZoneInfo(configuredTimeZone, out TimeZoneInfo timeZone))
        {
            Console.WriteLine(
                $"Warning: displayTimeZone '{configuredTimeZone}' is not supported on this runner. " +
                "Falling back to UTC.");
            return new TimeDisplaySettings(TimeZoneInfo.Utc, "UTC");
        }

        string label = string.IsNullOrWhiteSpace(config.DisplayTimeZoneLabel)
            ? BuildDefaultTimeZoneLabel(timeZone)
            : config.DisplayTimeZoneLabel.Trim();

        return new TimeDisplaySettings(timeZone, label);
    }

    private static bool TryResolveTimeZoneInfo(string value, out TimeZoneInfo timeZone)
    {
        timeZone = TimeZoneInfo.Utc;
        string trimmed = value.Trim();
        if (trimmed.Equals("UTC", StringComparison.OrdinalIgnoreCase))
        {
            timeZone = TimeZoneInfo.Utc;
            return true;
        }

        if (trimmed.StartsWith("UTC", StringComparison.OrdinalIgnoreCase))
        {
            string offsetText = trimmed[3..].Trim();
            if (string.IsNullOrWhiteSpace(offsetText))
            {
                timeZone = TimeZoneInfo.Utc;
                return true;
            }

            if (TryParseUtcOffset(offsetText, out TimeSpan offset))
            {
                string id = "UTC" + FormatUtcOffset(offset);
                timeZone = TimeZoneInfo.CreateCustomTimeZone(id, offset, id, id);
                return true;
            }
        }

        if (TryParseUtcOffset(trimmed, out TimeSpan directOffset))
        {
            string id = "UTC" + FormatUtcOffset(directOffset);
            timeZone = TimeZoneInfo.CreateCustomTimeZone(id, directOffset, id, id);
            return true;
        }

        if (trimmed.Equals("JST", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = "Asia/Tokyo";
        }

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(trimmed);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseUtcOffset(string value, out TimeSpan offset)
    {
        offset = TimeSpan.Zero;
        string trimmed = value.Trim();
        Match match = Regex.Match(trimmed, "^([+-])(\\d{1,2})(?::?(\\d{2}))?$");
        if (!match.Success)
        {
            return false;
        }

        int hours = int.Parse(match.Groups[2].Value);
        int minutes = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
        if (hours > 14 || minutes > 59)
        {
            return false;
        }

        offset = new TimeSpan(hours, minutes, 0);
        if (match.Groups[1].Value == "-")
        {
            offset = -offset;
        }

        return true;
    }

    private static string BuildDefaultTimeZoneLabel(TimeZoneInfo timeZone)
    {
        if (timeZone.Id.Equals("UTC", StringComparison.OrdinalIgnoreCase))
        {
            return "UTC";
        }

        if (timeZone.Id.Equals("Asia/Tokyo", StringComparison.OrdinalIgnoreCase) ||
            timeZone.StandardName.Contains("Tokyo", StringComparison.OrdinalIgnoreCase))
        {
            return "JST";
        }

        if (timeZone.Id.StartsWith("UTC", StringComparison.OrdinalIgnoreCase))
        {
            return timeZone.Id;
        }

        return timeZone.Id;
    }

    private static string FormatUtcOffset(TimeSpan offset)
    {
        string sign = offset < TimeSpan.Zero ? "-" : "+";
        TimeSpan absolute = offset.Duration();
        int hours = absolute.Hours + (absolute.Days * 24);
        return $"{sign}{hours:00}:{absolute.Minutes:00}";
    }

    private static AggregationResult ApplyLanguageColorOverrides(
        AggregationResult aggregation,
        IReadOnlyDictionary<string, string> colorOverrides)
    {
        if (colorOverrides.Count == 0)
        {
            return aggregation;
        }

        bool changed = false;
        var languages = new List<AggregatedLanguage>(aggregation.Languages.Count);
        foreach (AggregatedLanguage language in aggregation.Languages)
        {
            if (colorOverrides.TryGetValue(language.Name, out string? overrideColor))
            {
                string? normalized = NormalizeCssColor(overrideColor);
                if (!string.IsNullOrWhiteSpace(normalized) &&
                    !string.Equals(normalized, language.Color, StringComparison.OrdinalIgnoreCase))
                {
                    languages.Add(language with { Color = normalized });
                    changed = true;
                    continue;
                }
            }

            languages.Add(language);
        }

        return changed
            ? aggregation with { Languages = languages }
            : aggregation;
    }

    private static void EnsureParentDirectory(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string SanitizePathSegment(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            if (invalidCharacters.Contains(c) || c == '/' || c == '\\')
            {
                sanitized.Append('_');
                continue;
            }

            sanitized.Append(c);
        }

        return sanitized.ToString();
    }

    private static string ResolveReadmeImagePath(CliOptions options, string readmePath)
    {
        if (!string.IsNullOrWhiteSpace(options.ImagePathForReadme))
        {
            return options.ImagePathForReadme!.Replace('\\', '/');
        }

        string readmeDirectory = Path.GetDirectoryName(readmePath) ?? ".";
        string relativePath = Path.GetRelativePath(readmeDirectory, options.OutputPath);
        return relativePath.Replace('\\', '/');
    }

    private static string? ResolveLanguageColorOverride(PinRepository repo, CardsConfig config, string primaryLanguage)
    {
        if (config.LanguageColorOverrides.TryGetValue(primaryLanguage, out string? fromMap))
        {
            string? normalizedFromMap = NormalizeCssColor(fromMap);
            if (!string.IsNullOrWhiteSpace(normalizedFromMap))
            {
                return normalizedFromMap;
            }
        }

        return NormalizeCssColor(repo.LanguageColorOverride);
    }

    private static string? ResolveLanguageIconOverride(
        PinRepository repo,
        CardsConfig config,
        string primaryLanguage,
        string configDirectory)
    {
        if (config.LanguageIconOverrides.TryGetValue(primaryLanguage, out string? fromMap))
        {
            string? resolvedFromMap = TryResolveIconHref(fromMap, configDirectory);
            if (!string.IsNullOrWhiteSpace(resolvedFromMap))
            {
                return resolvedFromMap;
            }
        }

        return TryResolveIconHref(repo.LanguageIconOverride, configDirectory);
    }

    private static string? NormalizeCssColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        if (Regex.IsMatch(trimmed, "^#[0-9a-fA-F]{3}([0-9a-fA-F]{1,5})?$"))
        {
            return trimmed;
        }

        if (Regex.IsMatch(trimmed, "^[a-zA-Z]{1,20}$"))
        {
            return trimmed.ToLowerInvariant();
        }

        if (IsSafeColorFunction(trimmed, "rgb") ||
            IsSafeColorFunction(trimmed, "rgba") ||
            IsSafeColorFunction(trimmed, "hsl") ||
            IsSafeColorFunction(trimmed, "hsla") ||
            IsSafeColorFunction(trimmed, "oklch") ||
            IsSafeColorFunction(trimmed, "oklab"))
        {
            return trimmed;
        }

        return null;
    }

    private static bool IsSafeColorFunction(string value, string functionName)
    {
        string prefix = functionName + "(";
        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !value.EndsWith(')'))
        {
            return false;
        }

        if (value.Length > 100)
        {
            return false;
        }

        string args = value[prefix.Length..^1];
        foreach (char c in args)
        {
            bool allowed = char.IsDigit(c) ||
                           char.IsWhiteSpace(c) ||
                           c is '.' or ',' or '%' or '/' or '+' or '-';

            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }

    private static string? TryResolveIconHref(string? iconValue, string configDirectory)
    {
        if (string.IsNullOrWhiteSpace(iconValue))
        {
            return null;
        }

        string trimmed = iconValue.Trim();
        if (trimmed.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
        {
            return trimmed;
        }

        string resolvedPath = Path.IsPathRooted(trimmed)
            ? trimmed
            : Path.GetFullPath(Path.Combine(configDirectory, trimmed));

        if (!File.Exists(resolvedPath))
        {
            return null;
        }

        string extension = Path.GetExtension(resolvedPath).ToLowerInvariant();
        string? mime = extension switch
        {
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => null,
        };

        if (mime is null)
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(resolvedPath);
        string base64 = Convert.ToBase64String(bytes);
        return $"data:{mime};base64,{base64}";
    }
}
