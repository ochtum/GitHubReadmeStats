using System.Globalization;
using System.Text;

namespace GitHubReadMeStats.Cli;

internal static class ReadmeUpdater
{
    public static async Task UpdateAsync(
        string readmePath,
        string startMarker,
        string endMarker,
        string imagePath,
        AggregationResult aggregation,
        int topCount,
        DateTimeOffset generatedAtUtc,
        TimeDisplaySettings timeDisplay,
        CancellationToken cancellationToken = default)
    {
        string original = File.Exists(readmePath)
            ? await File.ReadAllTextAsync(readmePath, cancellationToken)
            : string.Empty;

        string section = BuildSection(startMarker, endMarker, imagePath, aggregation, topCount, generatedAtUtc, timeDisplay);

        string updated;
        int startIndex = original.IndexOf(startMarker, StringComparison.Ordinal);
        int endIndex = original.IndexOf(endMarker, StringComparison.Ordinal);

        if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
        {
            int afterEnd = endIndex + endMarker.Length;
            string before = original[..startIndex].TrimEnd();
            string after = original[afterEnd..].TrimStart();

            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(before))
            {
                builder.AppendLine(before);
                builder.AppendLine();
            }

            builder.AppendLine(section.TrimEnd());

            if (!string.IsNullOrWhiteSpace(after))
            {
                builder.AppendLine();
                builder.AppendLine(after);
            }

            updated = builder.ToString();
        }
        else
        {
            var builder = new StringBuilder(original.TrimEnd());
            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine(section.TrimEnd());
            updated = builder.ToString();
        }

        await File.WriteAllTextAsync(readmePath, updated + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
    }

    private static string BuildSection(
        string startMarker,
        string endMarker,
        string imagePath,
        AggregationResult aggregation,
        int topCount,
        DateTimeOffset generatedAtUtc,
        TimeDisplaySettings timeDisplay)
    {
        IReadOnlyList<AggregatedLanguage> topLanguages = aggregation.Languages.Take(topCount).ToList();
        DateTimeOffset generatedAtLocal = TimeZoneInfo.ConvertTime(generatedAtUtc, timeDisplay.TimeZone);

        var sb = new StringBuilder();
        sb.AppendLine(startMarker);
        sb.AppendLine("## GitHub Readme Stats");
        sb.AppendLine();
        sb.AppendLine($"![Top Languages]({imagePath})");
        sb.AppendLine();
        sb.AppendLine("| Rank | Language | Size | Share |");
        sb.AppendLine("| ---: | :-- | ---: | ---: |");

        for (int i = 0; i < topLanguages.Count; i++)
        {
            AggregatedLanguage language = topLanguages[i];
            sb.AppendLine($"| {i + 1} | {language.Name} | {SvgRenderer.ToHumanReadableBytes(language.Size)} | {language.Percent.ToString("0.00", CultureInfo.InvariantCulture)}% |");
        }

        sb.AppendLine();
        sb.AppendLine($"_Updated: {generatedAtLocal:yyyy-MM-dd HH:mm} {timeDisplay.Label}_  ");
        sb.AppendLine($"_Repositories: {aggregation.IncludedRepositoryCount} / {aggregation.ScannedRepositoryCount} (included/scanned)_");
        sb.AppendLine(endMarker);

        return sb.ToString();
    }
}
