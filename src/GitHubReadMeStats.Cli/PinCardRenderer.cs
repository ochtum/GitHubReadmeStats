using System.Globalization;
using System.Security;
using System.Text;

namespace GitHubReadMeStats.Cli;

internal static class PinCardRenderer
{
    private const string LightTextColor = "#F8FAFC";
    private const string DarkTextColor = "#0F172A";
    private const double MinimumAaTextContrastRatio = 4.5d;
    private const double MinimumUiContrastRatio = 3.0d;

    public static string Render(PinCardData repository, CardColorTheme? colorTheme = null)
    {
        const int width = 495;
        const int height = 228;
        bool hasTrafficTotals = repository.TrafficTotals is not null;

        string title = repository.Name;
        string description = NormalizeDescription(repository.Description);

        int descriptionColumns = hasTrafficTotals ? 64 : 54;
        string[] lines = WrapDescription(description, maxColumnsPerLine: descriptionColumns, maxLines: 3);

        string backgroundStart = colorTheme?.BackgroundStart ?? "#111827";
        string backgroundEnd = colorTheme?.BackgroundEnd ?? "#0B1120";
        string borderColor = colorTheme?.Border ?? "#334155";
        string titleColor = colorTheme?.TitleText ?? "#60A5FA";
        string descriptionColor = colorTheme?.SecondaryText ?? "#22D3EE";
        string trafficLabelColor = colorTheme?.SecondaryText ?? "#93C5FD";
        string trafficValueColor = colorTheme?.PrimaryText ?? "#E2E8F0";
        string metaColor = colorTheme?.PrimaryText ?? "#E2E8F0";
        string subColor = colorTheme?.TertiaryText ?? "#94A3B8";
        string hintColor = colorTheme?.MutedText ?? "#64748B";
        string trafficPanelFill = colorTheme?.PanelFill ?? "#0A1E3A";
        string trafficPanelStroke = colorTheme?.PanelStroke ?? "#1D4ED8";
        string trafficSeparatorColor = colorTheme?.ChartGrid ?? "#1D4ED8";
        string iconFrameFill = colorTheme?.IconFrameFill ?? "#0F1E3D";
        string iconFrameStroke = colorTheme?.PanelStroke ?? "#3B82F6";
        string iconImageBackground = colorTheme?.IconImageBackground ?? "#0B1224";
        string iconStroke = colorTheme?.IconStroke ?? "#7DD3FC";
        string iconDot = colorTheme?.IconDot ?? "#22D3EE";

        // Keep icon contrast readable against themed backgrounds and icon frame.
        iconFrameFill = EnsureMinContrast(iconFrameFill, MinimumUiContrastRatio, backgroundStart, backgroundEnd);
        iconFrameStroke = EnsureMinContrast(iconFrameStroke, MinimumUiContrastRatio, iconFrameFill);
        iconImageBackground = EnsureMinContrast(iconImageBackground, MinimumUiContrastRatio, iconFrameFill);
        iconStroke = EnsureMinContrast(iconStroke, MinimumAaTextContrastRatio, iconFrameFill);
        iconDot = EnsureMinContrast(iconDot, MinimumAaTextContrastRatio, iconFrameFill);

        var sb = new StringBuilder();

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"Repository card for {EscapeXml(repository.Owner + "/" + repository.Name)}\">");
        sb.AppendLine("  <defs>");
        sb.AppendLine("    <linearGradient id=\"bg\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"1\">");
        sb.AppendLine($"      <stop offset=\"0%\" stop-color=\"{EscapeXml(backgroundStart)}\" />");
        sb.AppendLine($"      <stop offset=\"100%\" stop-color=\"{EscapeXml(backgroundEnd)}\" />");
        sb.AppendLine("    </linearGradient>");
        sb.AppendLine("    <style>");
        sb.AppendLine($"      .title {{ font: 700 24px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(titleColor)}; }}");
        sb.AppendLine($"      .desc {{ font: 500 14px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(descriptionColor)}; }}");
        sb.AppendLine($"      .traffic-label {{ font: 600 9px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(trafficLabelColor)}; }}");
        sb.AppendLine($"      .traffic-value {{ font: 700 17px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(trafficValueColor)}; }}");
        sb.AppendLine($"      .meta {{ font: 600 12px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(metaColor)}; }}");
        sb.AppendLine($"      .sub {{ font: 500 11px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(subColor)}; }}");
        sb.AppendLine("      .badge-text { font: 700 9px 'Segoe UI', Arial, sans-serif; text-anchor: middle; }");
        sb.AppendLine($"      .hint {{ font: 500 10px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(hintColor)}; }}");
        sb.AppendLine($"      .traffic-panel {{ fill: {EscapeXml(trafficPanelFill)}; fill-opacity: 0.55; stroke: {EscapeXml(trafficPanelStroke)}; stroke-opacity: 0.65; }}");
        sb.AppendLine($"      .traffic-sep {{ stroke: {EscapeXml(trafficSeparatorColor)}; stroke-opacity: 0.45; }}");
        sb.AppendLine($"      .icon-frame {{ fill: {EscapeXml(iconFrameFill)}; stroke: {EscapeXml(iconFrameStroke)}; stroke-width: 1.4; }}");
        sb.AppendLine($"      .icon-image-bg {{ fill: {EscapeXml(iconImageBackground)}; }}");
        sb.AppendLine($"      .icon-shape {{ fill: none; stroke: {EscapeXml(iconStroke)}; stroke-width: 1.7; stroke-linecap: round; stroke-linejoin: round; }}");
        sb.AppendLine($"      .icon-dot {{ fill: {EscapeXml(iconDot)}; }}");
        sb.AppendLine("    </style>");
        sb.AppendLine("    <clipPath id=\"iconClip\">");
        sb.AppendLine("      <rect x=\"4\" y=\"4\" width=\"20\" height=\"20\" rx=\"4\" />");
        sb.AppendLine("    </clipPath>");
        sb.AppendLine("  </defs>");

        sb.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" rx=\"8\" fill=\"url(#bg)\" />");
        sb.AppendLine($"  <rect x=\"1\" y=\"1\" width=\"{width - 2}\" height=\"{height - 2}\" rx=\"7\" fill=\"none\" stroke=\"{EscapeXml(borderColor)}\" />");
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
            sb.AppendLine($"  <text x=\"24\" y=\"{70 + (i * 17)}\" class=\"desc\">{EscapeXml(lines[i])}</text>");
        }

        if (repository.TrafficTotals is not null)
        {
            RepositoryTrafficTotals totals = repository.TrafficTotals;
            const int panelX = 24;
            const int panelY = 124;
            const int panelW = 447;
            const int panelH = 58;
            const int metricColumns = 4;
            int columnWidth = panelW / metricColumns;

            sb.AppendLine($"  <rect x=\"{panelX}\" y=\"{panelY}\" width=\"{panelW}\" height=\"{panelH}\" rx=\"8\" class=\"traffic-panel\" />");

            for (int i = 1; i < metricColumns; i++)
            {
                int separatorX = panelX + (columnWidth * i);
                sb.AppendLine($"  <line x1=\"{separatorX}\" y1=\"{panelY + 8}\" x2=\"{separatorX}\" y2=\"{panelY + panelH - 8}\" class=\"traffic-sep\" />");
            }

            AppendTrafficColumn(sb, panelX + (columnWidth * 0) + (columnWidth / 2), panelY + 18, "Git Clones", totals.CloneCountTotal);
            AppendTrafficColumn(sb, panelX + (columnWidth * 1) + (columnWidth / 2), panelY + 18, "Unique Cloners", totals.UniqueClonersTotal);
            AppendTrafficColumn(sb, panelX + (columnWidth * 2) + (columnWidth / 2), panelY + 18, "Total Views", totals.ViewCountTotal);
            AppendTrafficColumn(sb, panelX + (columnWidth * 3) + (columnWidth / 2), panelY + 18, "Unique Visitors", totals.UniqueVisitorsTotal);

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
        (string badgeFill, string badgeStroke, string badgeTextFill) = GetStatusBadgeStyle(
            repository,
            colorTheme,
            backgroundStart,
            backgroundEnd);
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

    private static void AppendTrafficColumn(StringBuilder sb, int centerX, int y, string label, long value)
    {
        sb.AppendLine($"  <text x=\"{centerX}\" y=\"{y}\" class=\"traffic-label\" text-anchor=\"middle\">{EscapeXml(label)}</text>");
        sb.AppendLine($"  <text x=\"{centerX}\" y=\"{y + 22}\" class=\"traffic-value\" text-anchor=\"middle\">{FormatMetric(value)}</text>");
    }

    private static (string Fill, string Stroke, string Text) GetStatusBadgeStyle(
        PinCardData repository,
        CardColorTheme? colorTheme,
        string backgroundStart,
        string backgroundEnd)
    {
        string fill;
        string stroke;
        string text;

        if (colorTheme is not null)
        {
            StatusBadgePalette badgePalette = repository.IsPrivate
                ? colorTheme.PrivateBadge
                : repository.IsArchived
                    ? colorTheme.ArchivedBadge
                    : colorTheme.PublicBadge;

            fill = badgePalette.Fill;
            stroke = badgePalette.Stroke;
            text = badgePalette.Text;
            return EnsureBadgeContrast(fill, stroke, text, backgroundStart, backgroundEnd);
        }

        if (repository.IsPrivate)
        {
            fill = "#78350F";
            stroke = "#F59E0B";
            text = "#FEF3C7";
            return EnsureBadgeContrast(fill, stroke, text, backgroundStart, backgroundEnd);
        }

        if (repository.IsArchived)
        {
            fill = "#3F3F46";
            stroke = "#A1A1AA";
            text = "#F4F4F5";
            return EnsureBadgeContrast(fill, stroke, text, backgroundStart, backgroundEnd);
        }

        fill = "#0F766E";
        stroke = "#14B8A6";
        text = "#CCFBF1";
        return EnsureBadgeContrast(fill, stroke, text, backgroundStart, backgroundEnd);
    }

    private static (string Fill, string Stroke, string Text) EnsureBadgeContrast(
        string fill,
        string stroke,
        string text,
        string backgroundStart,
        string backgroundEnd)
    {
        string adjustedFill = EnsureMinContrast(fill, MinimumUiContrastRatio, backgroundStart, backgroundEnd);
        string adjustedText = EnsureMinContrast(text, MinimumAaTextContrastRatio, adjustedFill);
        string adjustedStroke = EnsureMinContrast(stroke, MinimumUiContrastRatio, adjustedFill);
        return (adjustedFill, adjustedStroke, adjustedText);
    }

    private static string EnsureMinContrast(string preferred, double minimumContrast, params string[] backgrounds)
    {
        List<Rgb> parsedBackgrounds = backgrounds
            .Select(background => TryParseHexColor(background, out Rgb parsed) ? parsed : (Rgb?)null)
            .Where(rgb => rgb.HasValue)
            .Select(rgb => rgb!.Value)
            .ToList();

        if (parsedBackgrounds.Count == 0 || !TryParseHexColor(preferred, out Rgb preferredRgb))
        {
            return preferred;
        }

        double preferredMinContrast = parsedBackgrounds.Min(background => GetContrastRatio(preferredRgb, background));
        if (preferredMinContrast >= minimumContrast)
        {
            return preferred;
        }

        Rgb light = ParseHexColorOrThrow(LightTextColor);
        Rgb dark = ParseHexColorOrThrow(DarkTextColor);
        double lightMinContrast = parsedBackgrounds.Min(background => GetContrastRatio(light, background));
        double darkMinContrast = parsedBackgrounds.Min(background => GetContrastRatio(dark, background));

        return lightMinContrast >= darkMinContrast ? LightTextColor : DarkTextColor;
    }

    private static bool TryParseHexColor(string color, out Rgb rgb)
    {
        rgb = default;
        if (string.IsNullOrWhiteSpace(color) || !color.StartsWith('#'))
        {
            return false;
        }

        string hex = color[1..];
        if (hex.Length is 3 or 4)
        {
            hex = string.Concat(hex.Select(ch => $"{ch}{ch}"));
        }

        if (hex.Length == 8)
        {
            hex = hex[..6];
        }

        if (hex.Length != 6)
        {
            return false;
        }

        if (!byte.TryParse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) ||
            !byte.TryParse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) ||
            !byte.TryParse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
        {
            return false;
        }

        rgb = new Rgb(r, g, b);
        return true;
    }

    private static Rgb ParseHexColorOrThrow(string color)
    {
        if (TryParseHexColor(color, out Rgb rgb))
        {
            return rgb;
        }

        throw new InvalidOperationException($"Invalid hex color: {color}");
    }

    private static double GetContrastRatio(Rgb a, Rgb b)
    {
        double l1 = GetRelativeLuminance(a);
        double l2 = GetRelativeLuminance(b);
        double max = Math.Max(l1, l2);
        double min = Math.Min(l1, l2);
        return (max + 0.05d) / (min + 0.05d);
    }

    private static double GetRelativeLuminance(Rgb rgb)
    {
        static double ToLinear(double channel)
        {
            return channel <= 0.04045d
                ? channel / 12.92d
                : Math.Pow((channel + 0.055d) / 1.055d, 2.4d);
        }

        double r = ToLinear(rgb.R / 255d);
        double g = ToLinear(rgb.G / 255d);
        double b = ToLinear(rgb.B / 255d);
        return (0.2126d * r) + (0.7152d * g) + (0.0722d * b);
    }

    private readonly record struct Rgb(byte R, byte G, byte B);
}
