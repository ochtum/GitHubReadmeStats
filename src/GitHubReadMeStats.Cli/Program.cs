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
            CardColorTheme? colorTheme = ResolveCardColorTheme(cardsConfig);

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
            string svg = SvgRenderer.Render(repositoriesResult.ViewerLogin, aggregation, options.Top, generatedAtUtc, timeDisplay, colorTheme);

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
                    repositoriesResult,
                    cardsConfig!,
                    timeDisplay,
                    colorTheme);
            }

            if (!string.IsNullOrWhiteSpace(options.UpdateReadmePath))
            {
                string readmePath = options.UpdateReadmePath!;
                string profileOwner = cardsConfig?.Username ?? repositoriesResult.ViewerLogin;
                ReadmeImagePaths readmeImagePaths = ResolveReadmeImagePaths(options, readmePath);
                ReadmeSectionMarkers readmeSectionMarkers = ResolveReadmeSectionMarkers(options);
                IReadOnlyList<ReadmePinEntry> readmePins = ResolveReadmePinEntries(options, readmePath, cardsConfig, profileOwner);

                ReadmeUpdateResult updateResult = await ReadmeUpdater.UpdateAsync(
                    readmePath,
                    readmeSectionMarkers,
                    readmeImagePaths,
                    readmePins,
                    options.PinsColumnsForReadme);

                if (updateResult.Updated)
                {
                    Console.WriteLine($"Updated README section: {readmePath}");
                }
                else
                {
                    Console.WriteLine($"Skipped README update: {updateResult.Message}");
                }
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
        ViewerRepositoriesResult repositoriesResult,
        CardsConfig config,
        TimeDisplaySettings timeDisplay,
        CardColorTheme? colorTheme)
    {
        string configPath = options.CardsConfigPath!;
        string configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath)) ?? Directory.GetCurrentDirectory();

        UserSummary summary = await graphqlClient.FetchUserSummaryAsync(config.Username);
        string githubStatsSvg = GitHubStatsSummaryCardRenderer.Render(summary, generatedAtUtc, timeDisplay, colorTheme);
        string githubStatsPath = Path.Combine(options.CardsOutputDir, "github-stats.svg");
        EnsureParentDirectory(githubStatsPath);
        await File.WriteAllTextAsync(githubStatsPath, githubStatsSvg + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"Generated: {githubStatsPath}");

        string statsSvg = ProfileStatsCardRenderer.Render(summary, generatedAtUtc, timeDisplay, colorTheme);
        string statsPath = Path.Combine(options.CardsOutputDir, "stats.svg");
        EnsureParentDirectory(statsPath);
        await File.WriteAllTextAsync(statsPath, statsSvg + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"Generated: {statsPath}");

        string pinOutputDir = Path.Combine(options.CardsOutputDir, "pins");
        Directory.CreateDirectory(pinOutputDir);

        string trafficHistoryPath = Path.Combine(options.CardsOutputDir, "traffic-history.json");
        TrafficHistoryStore trafficHistory = TrafficHistoryStore.Load(trafficHistoryPath);
        string[] configuredTrafficRepositoryKeys = config.Repositories
            .Select(repo => $"{repo.Owner}/{repo.Name}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        trafficHistory.KeepOnlyRepositories(configuredTrafficRepositoryKeys);

        var trafficTotalsCache = new Dictionary<string, RepositoryTrafficTotals?>(StringComparer.OrdinalIgnoreCase);
        var unavailableTrafficKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        async Task<RepositoryTrafficTotals?> GetTrafficTotalsAsync(string owner, string name)
        {
            string key = $"{owner}/{name}";
            if (trafficTotalsCache.TryGetValue(key, out RepositoryTrafficTotals? cachedTotals))
            {
                return cachedTotals;
            }

            RepositoryTrafficSnapshot? trafficSnapshot = null;
            try
            {
                trafficSnapshot = await graphqlClient.TryFetchRepositoryTrafficAsync(owner, name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to fetch traffic for {owner}/{name}: {ex.Message}");
            }

            RepositoryTrafficTotals? trafficTotals = trafficHistory.MergeAndGetTotals(owner, name, trafficSnapshot);
            if (trafficSnapshot is null && trafficTotals is null && unavailableTrafficKeys.Add(key))
            {
                Console.WriteLine(
                    $"Info: traffic unavailable for {owner}/{name}. " +
                    "Check GH_TOKEN repository access and 'Administration: Read' permission.");
            }

            trafficTotalsCache[key] = trafficTotals;
            return trafficTotals;
        }

        List<RepositoryNode> publicRepositories = repositoriesResult.Repositories
            .Where(repo => !repo.IsPrivate && !repo.IsFork)
            .ToList();

        long totalForks = 0;
        long totalWatchers = 0;
        long totalStarred = 0;
        long totalCloneCount = 0;
        long totalUniqueCloners = 0;
        long totalViewCount = 0;
        long totalUniqueVisitors = 0;

        int trafficTargetRepositoryCount = 0;
        int trafficAvailableRepositoryCount = 0;
        DateOnly? trafficSinceDate = null;
        DateOnly? trafficLastRecordedDate = null;

        foreach (RepositoryNode repository in publicRepositories)
        {
            totalForks += Math.Max(0, repository.ForkCount);
            totalWatchers += Math.Max(0, repository.Watchers?.TotalCount ?? 0);
            totalStarred += Math.Max(0, repository.StargazerCount);

            if (!TrySplitNameWithOwner(repository.NameWithOwner, out string owner, out string name))
            {
                continue;
            }

            trafficTargetRepositoryCount++;
            RepositoryTrafficTotals? trafficTotals = await GetTrafficTotalsAsync(owner, name);
            if (trafficTotals is null)
            {
                continue;
            }

            trafficAvailableRepositoryCount++;
            totalCloneCount += Math.Max(0, trafficTotals.CloneCountTotal);
            totalUniqueCloners += Math.Max(0, trafficTotals.UniqueClonersTotal);
            totalViewCount += Math.Max(0, trafficTotals.ViewCountTotal);
            totalUniqueVisitors += Math.Max(0, trafficTotals.UniqueVisitorsTotal);

            trafficSinceDate = trafficSinceDate is null || trafficTotals.SinceDate < trafficSinceDate.Value
                ? trafficTotals.SinceDate
                : trafficSinceDate;

            trafficLastRecordedDate = trafficLastRecordedDate is null || trafficTotals.LastRecordedDate > trafficLastRecordedDate.Value
                ? trafficTotals.LastRecordedDate
                : trafficLastRecordedDate;
        }

        int trafficUnavailableRepositoryCount = Math.Max(0, trafficTargetRepositoryCount - trafficAvailableRepositoryCount);
        var totalsCardData = new PublicRepositoriesTotalsCardData(
            repositoriesResult.ViewerLogin,
            publicRepositories.Count,
            totalForks,
            totalWatchers,
            totalStarred,
            totalCloneCount,
            totalUniqueCloners,
            totalViewCount,
            totalUniqueVisitors,
            trafficAvailableRepositoryCount,
            trafficUnavailableRepositoryCount,
            trafficSinceDate,
            trafficLastRecordedDate);

        string totalsSvg = PublicRepositoriesTotalsCardRenderer.Render(totalsCardData, generatedAtUtc, timeDisplay, colorTheme);
        string totalsPath = Path.Combine(options.CardsOutputDir, "public-repo-totals.svg");
        EnsureParentDirectory(totalsPath);
        await File.WriteAllTextAsync(totalsPath, totalsSvg + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"Generated: {totalsPath}");

        int generatedPinCount = 0;
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

            RepositoryTrafficTotals? trafficTotals = await GetTrafficTotalsAsync(repo.Owner, repo.Name);

            PinCardData data = baseData with
            {
                TrafficTotals = trafficTotals,
            };

            string pinSvg = PinCardRenderer.Render(data, colorTheme);
            string pinPath = Path.Combine(pinOutputDir, $"{SanitizePathSegment(repo.Owner)}-{SanitizePathSegment(repo.Name)}.svg");
            await File.WriteAllTextAsync(pinPath, pinSvg + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            generatedPinCount++;
        }

        trafficHistory.KeepOnlyRepositories(configuredTrafficRepositoryKeys);
        await trafficHistory.SaveAsync(trafficHistoryPath);
        Console.WriteLine($"Updated traffic history: {trafficHistoryPath}");
        if (unavailableTrafficKeys.Count > 0)
        {
            Console.WriteLine($"Traffic unavailable repositories: {unavailableTrafficKeys.Count}");
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

    private static CardColorTheme? ResolveCardColorTheme(CardsConfig? config)
    {
        if (config is null)
        {
            return null;
        }

        bool hasMainColor = !string.IsNullOrWhiteSpace(config.MainColor);
        bool hasTheme = !string.IsNullOrWhiteSpace(config.Theme);

        if (hasTheme || !hasMainColor)
        {
            string configuredTheme = hasTheme
                ? config.Theme!
                : CardColorThemeFactory.DefaultThemeName;
            if (!CardColorThemeFactory.TryResolveTheme(configuredTheme, out _, out string? resolvedThemeColor, out bool useClassicFallbackPalette))
            {
                throw new InvalidOperationException(
                    $"Invalid cards-config theme '{configuredTheme}'. Supported themes: {string.Join(", ", CardColorThemeFactory.BuiltInThemeNames)}.");
            }

            if (useClassicFallbackPalette)
            {
                return null;
            }

            return CardColorThemeFactory.Create(resolvedThemeColor!);
        }

        string resolvedMainColor = config.MainColor!;

        try
        {
            return CardColorThemeFactory.Create(resolvedMainColor);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                $"Invalid cards-config mainColor '{config.MainColor}'. Use hex (#RGB/#RRGGBB/#RRGGBBAA) or oklch(...).",
                ex);
        }
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

    private static bool TrySplitNameWithOwner(string value, out string owner, out string name)
    {
        owner = string.Empty;
        name = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string[] parts = value
            .Split('/', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2 ||
            string.IsNullOrWhiteSpace(parts[0]) ||
            string.IsNullOrWhiteSpace(parts[1]))
        {
            return false;
        }

        owner = parts[0];
        name = parts[1];
        return true;
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

    private static ReadmeImagePaths ResolveReadmeImagePaths(CliOptions options, string readmePath)
    {
        string topLanguagesPath = ResolveRequiredReadmeImagePath(options.TopLanguagesImagePathForReadme, readmePath, options.OutputPath);
        string? statsPath = ResolveOptionalReadmeImagePath(
            options.StatsImagePathForReadme,
            readmePath,
            Path.Combine(options.CardsOutputDir, "stats.svg"));
        string? publicRepoTotalsPath = ResolveOptionalReadmeImagePath(
            options.PublicRepoTotalsImagePathForReadme,
            readmePath,
            Path.Combine(options.CardsOutputDir, "public-repo-totals.svg"));
        string? githubStatsPath = ResolveOptionalReadmeImagePath(
            options.GitHubStatsImagePathForReadme,
            readmePath,
            Path.Combine(options.CardsOutputDir, "github-stats.svg"));

        return new ReadmeImagePaths(
            topLanguagesPath,
            statsPath,
            publicRepoTotalsPath,
            githubStatsPath);
    }

    private static ReadmeSectionMarkers ResolveReadmeSectionMarkers(CliOptions options)
    {
        return new ReadmeSectionMarkers(
            options.TopLanguagesSectionStartMarker,
            options.TopLanguagesSectionEndMarker,
            options.StatsSectionStartMarker,
            options.StatsSectionEndMarker,
            options.OwnPinsSectionStartMarker,
            options.OwnPinsSectionEndMarker,
            options.ExternalPinsSectionStartMarker,
            options.ExternalPinsSectionEndMarker);
    }

    private static IReadOnlyList<ReadmePinEntry> ResolveReadmePinEntries(
        CliOptions options,
        string readmePath,
        CardsConfig? cardsConfig,
        string profileOwner)
    {
        if (cardsConfig is null || cardsConfig.Repositories.Count == 0)
        {
            return Array.Empty<ReadmePinEntry>();
        }

        string readmeDirectory = Path.GetDirectoryName(readmePath) ?? ".";
        string pinDirectory = Path.Combine(options.CardsOutputDir, "pins");

        var entries = new List<ReadmePinEntry>();
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (PinRepository repo in cardsConfig.Repositories)
        {
            string key = $"{repo.Owner}/{repo.Name}";
            if (!dedupe.Add(key))
            {
                continue;
            }

            string pinFileName = $"{SanitizePathSegment(repo.Owner)}-{SanitizePathSegment(repo.Name)}.svg";
            string pinPath = Path.Combine(pinDirectory, pinFileName);
            if (!File.Exists(pinPath))
            {
                continue;
            }

            string imagePath = Path.GetRelativePath(readmeDirectory, pinPath).Replace('\\', '/');
            string repositoryUrl = $"https://github.com/{repo.Owner}/{repo.Name}";
            bool isOwnedByProfile = string.Equals(repo.Owner, profileOwner, StringComparison.OrdinalIgnoreCase);

            entries.Add(new ReadmePinEntry(repo.Owner, repo.Name, repositoryUrl, imagePath, isOwnedByProfile));
        }

        return entries;
    }

    private static string ResolveRequiredReadmeImagePath(string? configuredImagePath, string readmePath, string defaultImagePath)
    {
        if (!string.IsNullOrWhiteSpace(configuredImagePath))
        {
            return configuredImagePath!.Replace('\\', '/');
        }

        string readmeDirectory = Path.GetDirectoryName(readmePath) ?? ".";
        string relativePath = Path.GetRelativePath(readmeDirectory, defaultImagePath);
        return relativePath.Replace('\\', '/');
    }

    private static string? ResolveOptionalReadmeImagePath(string? configuredImagePath, string readmePath, string defaultImagePath)
    {
        if (!string.IsNullOrWhiteSpace(configuredImagePath))
        {
            return configuredImagePath!.Replace('\\', '/');
        }

        if (!File.Exists(defaultImagePath))
        {
            return null;
        }

        string readmeDirectory = Path.GetDirectoryName(readmePath) ?? ".";
        string relativePath = Path.GetRelativePath(readmeDirectory, defaultImagePath);
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
