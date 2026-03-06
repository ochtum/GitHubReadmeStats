using System.Text;

namespace GitHubReadMeStats.Cli;

internal static class ReadmeUpdater
{
    public static async Task<ReadmeUpdateResult> UpdateAsync(
        string readmePath,
        ReadmeSectionMarkers markers,
        ReadmeImagePaths imagePaths,
        IReadOnlyList<ReadmePinEntry> pins,
        int pinsColumns,
        CancellationToken cancellationToken = default)
    {
        string original = File.Exists(readmePath)
            ? await File.ReadAllTextAsync(readmePath, cancellationToken)
            : string.Empty;

        string current = original;
        bool updatedAnySection = false;
        var updatedSections = new List<string>();
        var skippedSections = new List<string>();

        List<ReadmePinEntry> ownPins = pins.Where(x => x.IsOwnedByProfile).ToList();
        List<ReadmePinEntry> externalPins = pins.Where(x => !x.IsOwnedByProfile).ToList();

        var sections = new[]
        {
            new ReadmeSectionUpdate(
                "top-languages",
                markers.TopLanguagesStart,
                markers.TopLanguagesEnd,
                BuildTopLanguagesSectionBody(imagePaths)),
            new ReadmeSectionUpdate(
                "stats",
                markers.StatsStart,
                markers.StatsEnd,
                BuildStatsSectionBody(imagePaths)),
            new ReadmeSectionUpdate(
                "pins-own",
                markers.OwnPinsStart,
                markers.OwnPinsEnd,
                BuildPinsSectionBody(ownPins, pinsColumns)),
            new ReadmeSectionUpdate(
                "pins-external",
                markers.ExternalPinsStart,
                markers.ExternalPinsEnd,
                BuildPinsSectionBody(externalPins, pinsColumns)),
        };

        foreach (ReadmeSectionUpdate section in sections)
        {
            SectionReplaceResult replaceResult = ReplaceMarkedSection(
                current,
                section.StartMarker,
                section.EndMarker,
                section.Body);

            if (!replaceResult.Found)
            {
                skippedSections.Add($"{section.Name}: {replaceResult.Message}");
                continue;
            }

            current = replaceResult.Markdown;
            if (replaceResult.Changed)
            {
                updatedAnySection = true;
                updatedSections.Add(section.Name);
            }
        }

        if (!updatedAnySection)
        {
            string skipSummary = BuildSummary(updatedSections, skippedSections);
            return new ReadmeUpdateResult(false, skipSummary);
        }

        await File.WriteAllTextAsync(readmePath, current, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
        string updateSummary = BuildSummary(updatedSections, skippedSections);
        return new ReadmeUpdateResult(true, updateSummary);
    }

    private static string BuildTopLanguagesSectionBody(ReadmeImagePaths imagePaths)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<div align=\"center\">");
        sb.AppendLine($"  <img width=\"100%\" src=\"{imagePaths.TopLanguages}\" alt=\"top-languages\" />");
        sb.AppendLine("</div>");
        return sb.ToString().TrimEnd();
    }

    private static string BuildStatsSectionBody(ReadmeImagePaths imagePaths)
    {
        bool hasAnyStatsImage =
            !string.IsNullOrWhiteSpace(imagePaths.GitHubStats) ||
            !string.IsNullOrWhiteSpace(imagePaths.Stats) ||
            !string.IsNullOrWhiteSpace(imagePaths.PublicRepoTotals);

        if (!hasAnyStatsImage)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("<div align=\"center\">");

        bool hasTopRow =
            !string.IsNullOrWhiteSpace(imagePaths.GitHubStats) ||
            !string.IsNullOrWhiteSpace(imagePaths.Stats);

        if (!string.IsNullOrWhiteSpace(imagePaths.GitHubStats) && !string.IsNullOrWhiteSpace(imagePaths.Stats))
        {
            sb.AppendLine($"  <img width=\"49%\" src=\"{imagePaths.GitHubStats}\" alt=\"github-stats\" />");
            sb.AppendLine($"  <img width=\"49%\" src=\"{imagePaths.Stats}\" alt=\"stats\" />");
        }
        else if (!string.IsNullOrWhiteSpace(imagePaths.GitHubStats))
        {
            sb.AppendLine($"  <img width=\"100%\" src=\"{imagePaths.GitHubStats}\" alt=\"github-stats\" />");
        }
        else if (!string.IsNullOrWhiteSpace(imagePaths.Stats))
        {
            sb.AppendLine($"  <img width=\"100%\" src=\"{imagePaths.Stats}\" alt=\"stats\" />");
        }

        if (!string.IsNullOrWhiteSpace(imagePaths.PublicRepoTotals))
        {
            if (hasTopRow)
            {
                sb.AppendLine("  <br />");
            }

            sb.AppendLine($"  <img width=\"100%\" src=\"{imagePaths.PublicRepoTotals}\" alt=\"public-repo-totals\" />");
        }

        sb.AppendLine("</div>");

        return sb.ToString().TrimEnd();
    }

    private static string BuildPinsSectionBody(IReadOnlyList<ReadmePinEntry> pins, int pinsColumns)
    {
        if (pins.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        int columns = Math.Clamp(pinsColumns, 1, 2);
        string width = columns == 1 ? "100%" : "49%";

        for (int i = 0; i < pins.Count; i += columns)
        {
            sb.AppendLine("<p align=\"center\">");
            for (int j = i; j < Math.Min(i + columns, pins.Count); j++)
            {
                ReadmePinEntry pin = pins[j];
                sb.AppendLine($"  <a href=\"{pin.RepositoryUrl}\"><img width=\"{width}\" src=\"{pin.ImagePath}\" alt=\"{pin.Name}\" /></a>");
            }

            sb.AppendLine("</p>");
        }

        return sb.ToString().TrimEnd();
    }

    private static SectionReplaceResult ReplaceMarkedSection(
        string markdown,
        string startMarker,
        string endMarker,
        string body)
    {
        int startIndex = markdown.IndexOf(startMarker, StringComparison.Ordinal);
        if (startIndex < 0)
        {
            return new SectionReplaceResult(
                markdown,
                Found: false,
                Changed: false,
                Message: $"start marker not found ({startMarker})");
        }

        int endIndex = markdown.IndexOf(endMarker, startIndex + startMarker.Length, StringComparison.Ordinal);
        if (endIndex < 0 || endIndex <= startIndex)
        {
            return new SectionReplaceResult(
                markdown,
                Found: false,
                Changed: false,
                Message: $"end marker not found ({endMarker})");
        }

        string replacement = BuildMarkedSection(startMarker, endMarker, body);
        string updated =
            markdown[..startIndex] +
            replacement +
            markdown[(endIndex + endMarker.Length)..];

        bool changed = !string.Equals(markdown, updated, StringComparison.Ordinal);
        return new SectionReplaceResult(updated, Found: true, changed, Message: changed ? "updated" : "no changes");
    }

    private static string BuildMarkedSection(string startMarker, string endMarker, string body)
    {
        var sb = new StringBuilder();
        sb.AppendLine(startMarker);
        if (!string.IsNullOrWhiteSpace(body))
        {
            sb.AppendLine(body.TrimEnd());
        }

        sb.Append(endMarker);
        return sb.ToString();
    }

    private static string BuildSummary(IReadOnlyList<string> updatedSections, IReadOnlyList<string> skippedSections)
    {
        string updated = updatedSections.Count == 0
            ? "updated sections: none"
            : $"updated sections: {string.Join(", ", updatedSections)}";

        if (skippedSections.Count == 0)
        {
            return updated;
        }

        return $"{updated}; skipped: {string.Join(" | ", skippedSections)}";
    }

    private sealed record ReadmeSectionUpdate(string Name, string StartMarker, string EndMarker, string Body);
    private sealed record SectionReplaceResult(string Markdown, bool Found, bool Changed, string Message);
}
