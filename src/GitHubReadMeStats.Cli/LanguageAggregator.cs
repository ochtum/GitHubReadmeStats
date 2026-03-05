namespace GitHubReadMeStats.Cli;

internal static class LanguageAggregator
{
    public static AggregationResult Aggregate(IReadOnlyList<RepositoryNode> repositories, CliOptions options)
    {
        var aggregateMap = new Dictionary<string, MutableLanguageCounter>(StringComparer.OrdinalIgnoreCase);
        var excluded = new HashSet<string>(options.ExcludedLanguages, StringComparer.OrdinalIgnoreCase);

        int scannedRepositories = 0;
        int includedRepositories = 0;

        foreach (RepositoryNode repo in repositories)
        {
            scannedRepositories++;

            if (!options.IncludeForks && repo.IsFork)
            {
                continue;
            }

            if (!options.IncludeArchived && repo.IsArchived)
            {
                continue;
            }

            includedRepositories++;

            IEnumerable<LanguageEdge?> edges = repo.Languages?.Edges?.AsEnumerable() ?? Array.Empty<LanguageEdge?>();
            foreach (LanguageEdge? edge in edges)
            {
                if (edge?.Node is null)
                {
                    continue;
                }

                string name = edge.Node.Name?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name) || excluded.Contains(name))
                {
                    continue;
                }

                long size = Math.Max(edge.Size, 0);
                string color = NormalizeColor(edge.Node.Color, name);

                if (aggregateMap.TryGetValue(name, out MutableLanguageCounter? existing))
                {
                    existing.Size += size;
                    if (!existing.HasGraphQlColor && !string.IsNullOrWhiteSpace(edge.Node.Color))
                    {
                        existing.Color = color;
                        existing.HasGraphQlColor = true;
                    }
                }
                else
                {
                    aggregateMap[name] = new MutableLanguageCounter
                    {
                        Name = name,
                        Size = size,
                        Color = color,
                        HasGraphQlColor = !string.IsNullOrWhiteSpace(edge.Node.Color),
                    };
                }
            }
        }

        long totalBytes = aggregateMap.Values.Sum(x => x.Size);

        List<AggregatedLanguage> languages = aggregateMap.Values
            .OrderByDescending(x => x.Size)
            .Select(x => new AggregatedLanguage(
                x.Name,
                x.Size,
                x.Color,
                totalBytes == 0 ? 0 : Math.Round((double)x.Size / totalBytes * 100.0, 2)))
            .ToList();

        return new AggregationResult(languages, totalBytes, scannedRepositories, includedRepositories);
    }

    private static string NormalizeColor(string? color, string languageName)
    {
        if (!string.IsNullOrWhiteSpace(color))
        {
            string trimmed = color.Trim();
            return trimmed.StartsWith('#') ? trimmed : $"#{trimmed}";
        }

        return GenerateDeterministicColor(languageName);
    }

    private static string GenerateDeterministicColor(string text)
    {
        const uint fnvOffset = 2166136261;
        const uint fnvPrime = 16777619;

        uint hash = fnvOffset;

        foreach (char c in text.ToLowerInvariant())
        {
            hash ^= c;
            hash *= fnvPrime;
        }

        byte r = (byte)(70 + (hash & 0x7F));
        byte g = (byte)(70 + ((hash >> 8) & 0x7F));
        byte b = (byte)(70 + ((hash >> 16) & 0x7F));

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private sealed class MutableLanguageCounter
    {
        public string Name { get; init; } = string.Empty;

        public long Size { get; set; }

        public string Color { get; set; } = "#4A4A4A";

        public bool HasGraphQlColor { get; set; }
    }
}
