using System.Globalization;
using System.Security;
using System.Text;

namespace GitHubReadMeStats.Cli;

internal static class PinCardRenderer
{
    public static string Render(PinCardData repository)
    {
        const int width = 495;
        const int height = 200;

        string title = repository.Name;
        string description = NormalizeDescription(repository.Description);

        string[] lines = WrapDescription(description, maxColumnsPerLine: 46, maxLines: 3);

        var sb = new StringBuilder();

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"Repository card for {EscapeXml(repository.Owner + "/" + repository.Name)}\">");
        sb.AppendLine("  <defs>");
        sb.AppendLine("    <linearGradient id=\"bg\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"1\">");
        sb.AppendLine("      <stop offset=\"0%\" stop-color=\"#111827\" />");
        sb.AppendLine("      <stop offset=\"100%\" stop-color=\"#0B1120\" />");
        sb.AppendLine("    </linearGradient>");
        sb.AppendLine("    <style>");
        sb.AppendLine("      .title { font: 700 24px 'Segoe UI', Arial, sans-serif; fill: #60A5FA; }");
        sb.AppendLine("      .desc { font: 500 14px 'Segoe UI', Arial, sans-serif; fill: #22D3EE; }");
        sb.AppendLine("      .meta { font: 600 13px 'Segoe UI', Arial, sans-serif; fill: #E2E8F0; }");
        sb.AppendLine("      .sub { font: 500 12px 'Segoe UI', Arial, sans-serif; fill: #94A3B8; }");
        sb.AppendLine("      .icon-frame { fill: #0F1E3D; stroke: #3B82F6; stroke-width: 1.4; }");
        sb.AppendLine("      .icon-shape { fill: none; stroke: #7DD3FC; stroke-width: 1.7; stroke-linecap: round; stroke-linejoin: round; }");
        sb.AppendLine("      .icon-dot { fill: #22D3EE; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("  </defs>");

        sb.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" rx=\"8\" fill=\"url(#bg)\" />");
        sb.AppendLine($"  <rect x=\"1\" y=\"1\" width=\"{width - 2}\" height=\"{height - 2}\" rx=\"7\" fill=\"none\" stroke=\"#334155\" />");
        sb.AppendLine("  <g transform=\"translate(24 18)\">");
        sb.AppendLine("    <rect x=\"0\" y=\"0\" width=\"28\" height=\"28\" rx=\"7\" class=\"icon-frame\" />");
        sb.AppendLine("    <path d=\"M6 10h6l2.2 3H22v9H6z\" class=\"icon-shape\" />");
        sb.AppendLine("    <path d=\"M6 13h16\" class=\"icon-shape\" />");
        sb.AppendLine("    <circle cx=\"20\" cy=\"8\" r=\"2\" class=\"icon-dot\" />");
        sb.AppendLine("  </g>");

        sb.AppendLine("  <text x=\"62\" y=\"40\" class=\"title\">" + EscapeXml(title) + "</text>");

        for (int i = 0; i < lines.Length; i++)
        {
            sb.AppendLine($"  <text x=\"24\" y=\"{72 + (i * 18)}\" class=\"desc\">{EscapeXml(lines[i])}</text>");
        }

        string language = string.IsNullOrWhiteSpace(repository.PrimaryLanguage) ? "Unknown" : repository.PrimaryLanguage;
        string languageColor = string.IsNullOrWhiteSpace(repository.PrimaryLanguageColor) ? "#94A3B8" : repository.PrimaryLanguageColor;

        sb.AppendLine($"  <circle cx=\"28\" cy=\"158\" r=\"6\" fill=\"{EscapeXml(languageColor)}\" />");
        sb.AppendLine($"  <text x=\"42\" y=\"163\" class=\"meta\">{EscapeXml(language)}</text>");
        sb.AppendLine($"  <text x=\"230\" y=\"163\" class=\"meta\">★ {repository.Stars.ToString("N0", CultureInfo.InvariantCulture)}</text>");
        sb.AppendLine($"  <text x=\"310\" y=\"163\" class=\"meta\">⑂ {repository.Forks.ToString("N0", CultureInfo.InvariantCulture)}</text>");

        string badge = repository.IsPrivate ? "private" : repository.IsArchived ? "archived" : "public";
        sb.AppendLine($"  <text x=\"24\" y=\"186\" class=\"sub\">{EscapeXml(repository.Owner)}/{EscapeXml(repository.Name)} • {badge}</text>");

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
}
