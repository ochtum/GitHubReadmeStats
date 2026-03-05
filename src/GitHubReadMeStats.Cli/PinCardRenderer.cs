using System.Globalization;
using System.Security;
using System.Text;

namespace GitHubReadMeStats.Cli;

internal static class PinCardRenderer
{
    public static string Render(PinCardData repository)
    {
        const int width = 495;
        const int height = 228;
        bool hasTrafficTotals = repository.TrafficTotals is not null;

        string title = repository.Name;
        string description = NormalizeDescription(repository.Description);

        int descriptionColumns = hasTrafficTotals ? 42 : 46;
        string[] lines = WrapDescription(description, maxColumnsPerLine: descriptionColumns, maxLines: 3);

        var sb = new StringBuilder();

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"Repository card for {EscapeXml(repository.Owner + "/" + repository.Name)}\">");
        sb.AppendLine("  <defs>");
        sb.AppendLine("    <linearGradient id=\"bg\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"1\">");
        sb.AppendLine("      <stop offset=\"0%\" stop-color=\"#111827\" />");
        sb.AppendLine("      <stop offset=\"100%\" stop-color=\"#0B1120\" />");
        sb.AppendLine("    </linearGradient>");
        sb.AppendLine("    <style>");
        sb.AppendLine("      .title { font: 700 24px 'Segoe UI', Arial, sans-serif; fill: #60A5FA; }");
        sb.AppendLine("      .desc { font: 500 13px 'Segoe UI', Arial, sans-serif; fill: #22D3EE; }");
        sb.AppendLine("      .traffic-label { font: 600 10px 'Segoe UI', Arial, sans-serif; fill: #93C5FD; }");
        sb.AppendLine("      .traffic-value { font: 700 15px 'Segoe UI', Arial, sans-serif; fill: #E2E8F0; }");
        sb.AppendLine("      .meta { font: 600 12px 'Segoe UI', Arial, sans-serif; fill: #E2E8F0; }");
        sb.AppendLine("      .sub { font: 500 11px 'Segoe UI', Arial, sans-serif; fill: #94A3B8; }");
        sb.AppendLine("      .badge-text { font: 700 9px 'Segoe UI', Arial, sans-serif; text-anchor: middle; }");
        sb.AppendLine("      .hint { font: 500 10px 'Segoe UI', Arial, sans-serif; fill: #64748B; }");
        sb.AppendLine("      .traffic-panel { fill: #0A1E3A; fill-opacity: 0.55; stroke: #1D4ED8; stroke-opacity: 0.65; }");
        sb.AppendLine("      .traffic-sep { stroke: #1D4ED8; stroke-opacity: 0.45; }");
        sb.AppendLine("      .icon-frame { fill: #0F1E3D; stroke: #3B82F6; stroke-width: 1.4; }");
        sb.AppendLine("      .icon-image-bg { fill: #0B1224; }");
        sb.AppendLine("      .icon-shape { fill: none; stroke: #7DD3FC; stroke-width: 1.7; stroke-linecap: round; stroke-linejoin: round; }");
        sb.AppendLine("      .icon-dot { fill: #22D3EE; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("    <clipPath id=\"iconClip\">");
        sb.AppendLine("      <rect x=\"4\" y=\"4\" width=\"20\" height=\"20\" rx=\"4\" />");
        sb.AppendLine("    </clipPath>");
        sb.AppendLine("  </defs>");

        sb.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" rx=\"8\" fill=\"url(#bg)\" />");
        sb.AppendLine($"  <rect x=\"1\" y=\"1\" width=\"{width - 2}\" height=\"{height - 2}\" rx=\"7\" fill=\"none\" stroke=\"#334155\" />");
        sb.AppendLine("  <g transform=\"translate(24 18)\">");
        sb.AppendLine("    <rect x=\"0\" y=\"0\" width=\"28\" height=\"28\" rx=\"7\" class=\"icon-frame\" />");
        if (!string.IsNullOrWhiteSpace(repository.RepositoryIconHref))
        {
            sb.AppendLine("    <rect x=\"4\" y=\"4\" width=\"20\" height=\"20\" rx=\"4\" class=\"icon-image-bg\" />");
            sb.AppendLine($"    <image x=\"4\" y=\"4\" width=\"20\" height=\"20\" clip-path=\"url(#iconClip)\" preserveAspectRatio=\"xMidYMid meet\" href=\"{EscapeXml(repository.RepositoryIconHref)}\" />");
        }
        else
        {
            sb.AppendLine("    <path d=\"M6 10h6l2.2 3H22v9H6z\" class=\"icon-shape\" />");
            sb.AppendLine("    <path d=\"M6 13h16\" class=\"icon-shape\" />");
            sb.AppendLine("    <circle cx=\"20\" cy=\"8\" r=\"2\" class=\"icon-dot\" />");
        }
        sb.AppendLine("  </g>");

        sb.AppendLine("  <text x=\"62\" y=\"40\" class=\"title\">" + EscapeXml(title) + "</text>");

        for (int i = 0; i < lines.Length; i++)
        {
            sb.AppendLine($"  <text x=\"24\" y=\"{70 + (i * 16)}\" class=\"desc\">{EscapeXml(lines[i])}</text>");
        }

        if (repository.TrafficTotals is not null)
        {
            RepositoryTrafficTotals totals = repository.TrafficTotals;
            const int panelX = 270;
            const int panelY = 92;
            const int panelW = 201;
            const int panelH = 88;
            int panelMiddleX = panelX + (panelW / 2);
            int panelMiddleY = panelY + (panelH / 2);

            sb.AppendLine($"  <rect x=\"{panelX}\" y=\"{panelY}\" width=\"{panelW}\" height=\"{panelH}\" rx=\"8\" class=\"traffic-panel\" />");
            sb.AppendLine($"  <line x1=\"{panelMiddleX}\" y1=\"{panelY + 8}\" x2=\"{panelMiddleX}\" y2=\"{panelY + panelH - 8}\" class=\"traffic-sep\" />");
            sb.AppendLine($"  <line x1=\"{panelX + 8}\" y1=\"{panelMiddleY}\" x2=\"{panelX + panelW - 8}\" y2=\"{panelMiddleY}\" class=\"traffic-sep\" />");

            AppendTrafficCell(sb, panelX + 14, panelY + 18, "Git Clones", totals.CloneCountTotal);
            AppendTrafficCell(sb, panelMiddleX + 12, panelY + 18, "Unique Cloners", totals.UniqueClonersTotal);
            AppendTrafficCell(sb, panelX + 14, panelMiddleY + 16, "Total Views", totals.ViewCountTotal);
            AppendTrafficCell(sb, panelMiddleX + 12, panelMiddleY + 16, "Unique Visitors", totals.UniqueVisitorsTotal);

            sb.AppendLine($"  <text x=\"24\" y=\"223\" class=\"hint\">Traffic since {totals.SinceDate:yyyy-MM-dd} (last {totals.LastRecordedDate:yyyy-MM-dd})</text>");
        }

        string language = string.IsNullOrWhiteSpace(repository.PrimaryLanguage) ? "Unknown" : repository.PrimaryLanguage;
        string languageColor = string.IsNullOrWhiteSpace(repository.PrimaryLanguageColor) ? "#94A3B8" : repository.PrimaryLanguageColor;
        int metaY = hasTrafficTotals ? 200 : 166;
        int dotY = hasTrafficTotals ? 196 : 162;
        int subY = hasTrafficTotals ? 214 : 184;
        int starX = hasTrafficTotals ? 144 : 220;
        int forkX = hasTrafficTotals ? 208 : 300;

        if (!string.IsNullOrWhiteSpace(repository.LanguageIconHref))
        {
            sb.AppendLine($"  <clipPath id=\"langIconClip\"><circle cx=\"28\" cy=\"{dotY}\" r=\"5\" /></clipPath>");
            sb.AppendLine($"  <circle cx=\"28\" cy=\"{dotY}\" r=\"5\" fill=\"{EscapeXml(languageColor)}\" />");
            sb.AppendLine($"  <image x=\"23\" y=\"{dotY - 5}\" width=\"10\" height=\"10\" clip-path=\"url(#langIconClip)\" preserveAspectRatio=\"xMidYMid meet\" href=\"{EscapeXml(repository.LanguageIconHref)}\" />");
        }
        else
        {
            sb.AppendLine($"  <circle cx=\"28\" cy=\"{dotY}\" r=\"5\" fill=\"{EscapeXml(languageColor)}\" />");
        }

        sb.AppendLine($"  <text x=\"40\" y=\"{metaY}\" class=\"meta\">{EscapeXml(language)}</text>");
        sb.AppendLine($"  <text x=\"{starX}\" y=\"{metaY}\" class=\"meta\">★ {repository.Stars.ToString("N0", CultureInfo.InvariantCulture)}</text>");
        sb.AppendLine($"  <text x=\"{forkX}\" y=\"{metaY}\" class=\"meta\">⑂ {repository.Forks.ToString("N0", CultureInfo.InvariantCulture)}</text>");

        string repositoryPath = $"{repository.Owner}/{repository.Name}";
        string badgeLabel = repository.IsPrivate ? "PRIVATE" : repository.IsArchived ? "ARCHIVED" : "PUBLIC";
        (string badgeFill, string badgeStroke, string badgeTextFill) = GetStatusBadgeStyle(repository);
        int badgeWidth = 16 + (badgeLabel.Length * 7);
        int badgeX = width - 24 - badgeWidth;
        int badgeY = subY - 11;
        int badgeTextX = badgeX + (badgeWidth / 2);
        int badgeTextY = badgeY + 10;

        string repositoryPathTrimmed = TrimToDisplayWidth(repositoryPath, maxColumns: 56);
        sb.AppendLine($"  <text x=\"24\" y=\"{subY}\" class=\"sub\">{EscapeXml(repositoryPathTrimmed)}</text>");
        sb.AppendLine($"  <rect x=\"{badgeX}\" y=\"{badgeY}\" width=\"{badgeWidth}\" height=\"15\" rx=\"7\" fill=\"{badgeFill}\" stroke=\"{badgeStroke}\" />");
        sb.AppendLine($"  <text x=\"{badgeTextX}\" y=\"{badgeTextY}\" class=\"badge-text\" fill=\"{badgeTextFill}\">{badgeLabel}</text>");

        sb.AppendLine("</svg>");

        return sb.ToString();
    }

    private static string NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return "No description provided";
        }

        return description
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ")
            .Trim();
    }

    private static string[] WrapDescription(string text, int maxColumnsPerLine, int maxLines)
    {
        var lines = new List<string>();
        var current = new StringBuilder(text.Length);
        int currentWidth = 0;
        bool truncated = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            int charWidth = GetVisualWidth(c);

            if (currentWidth + charWidth > maxColumnsPerLine && current.Length > 0)
            {
                lines.Add(current.ToString().TrimEnd());
                if (lines.Count == maxLines)
                {
                    truncated = i < text.Length;
                    break;
                }

                current.Clear();
                currentWidth = 0;
                if (c == ' ')
                {
                    continue;
                }
            }

            if (c == ' ' && current.Length == 0)
            {
                continue;
            }

            current.Append(c);
            currentWidth += charWidth;
        }

        if (lines.Count < maxLines && current.Length > 0)
        {
            lines.Add(current.ToString().TrimEnd());
        }

        if (lines.Count == 0)
        {
            lines.Add("No description provided");
        }

        if (lines.Count > maxLines)
        {
            lines = lines.Take(maxLines).ToList();
        }

        if (lines.Count == maxLines && truncated)
        {
            lines[^1] = TrimToDisplayWidth(lines[^1], maxColumnsPerLine - 3) + "...";
        }

        return lines.ToArray();
    }

    private static string TrimToDisplayWidth(string value, int maxColumns)
    {
        int width = 0;
        var sb = new StringBuilder(value.Length);

        foreach (char c in value)
        {
            int charWidth = GetVisualWidth(c);
            if (width + charWidth > maxColumns)
            {
                break;
            }

            sb.Append(c);
            width += charWidth;
        }

        return sb.ToString().TrimEnd();
    }

    private static int GetVisualWidth(char c)
    {
        if (c is <= '\u001F' or '\u007F')
        {
            return 0;
        }

        return IsEastAsianWide(c) ? 2 : 1;
    }

    private static bool IsEastAsianWide(char c)
    {
        return c == '\u2329' || c == '\u232A' ||
               (c >= '\u1100' && c <= '\u115F') ||
               (c >= '\u2E80' && c <= '\uA4CF' && c != '\u303F') ||
               (c >= '\uAC00' && c <= '\uD7A3') ||
               (c >= '\uF900' && c <= '\uFAFF') ||
               (c >= '\uFE10' && c <= '\uFE19') ||
               (c >= '\uFE30' && c <= '\uFE6F') ||
               (c >= '\uFF00' && c <= '\uFF60') ||
               (c >= '\uFFE0' && c <= '\uFFE6');
    }

    private static string EscapeXml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return SecurityElement.Escape(value) ?? string.Empty;
    }

    private static string FormatMetric(long value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static void AppendTrafficCell(StringBuilder sb, int x, int y, string label, long value)
    {
        sb.AppendLine($"  <text x=\"{x}\" y=\"{y}\" class=\"traffic-label\">{EscapeXml(label)}</text>");
        sb.AppendLine($"  <text x=\"{x}\" y=\"{y + 18}\" class=\"traffic-value\">{FormatMetric(value)}</text>");
    }

    private static (string Fill, string Stroke, string Text) GetStatusBadgeStyle(PinCardData repository)
    {
        if (repository.IsPrivate)
        {
            return ("#78350F", "#F59E0B", "#FEF3C7");
        }

        if (repository.IsArchived)
        {
            return ("#3F3F46", "#A1A1AA", "#F4F4F5");
        }

        return ("#0F766E", "#14B8A6", "#CCFBF1");
    }
}
