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

        string[] lines = WrapDescription(description, maxCharsPerLine: 44, maxLines: 2);

        var sb = new StringBuilder();

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"Repository card for {EscapeXml(repository.Owner + "/" + repository.Name)}\">");
        sb.AppendLine("  <defs>");
        sb.AppendLine("    <linearGradient id=\"bg\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"1\">");
        sb.AppendLine("      <stop offset=\"0%\" stop-color=\"#111827\" />");
        sb.AppendLine("      <stop offset=\"100%\" stop-color=\"#0B1120\" />");
        sb.AppendLine("    </linearGradient>");
        sb.AppendLine("    <style>");
        sb.AppendLine("      .title { font: 700 28px 'Segoe UI', Arial, sans-serif; fill: #60A5FA; }");
        sb.AppendLine("      .desc { font: 500 16px 'Segoe UI', Arial, sans-serif; fill: #22D3EE; }");
        sb.AppendLine("      .meta { font: 600 14px 'Segoe UI', Arial, sans-serif; fill: #E2E8F0; }");
        sb.AppendLine("      .sub { font: 500 12px 'Segoe UI', Arial, sans-serif; fill: #94A3B8; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("  </defs>");

        sb.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" rx=\"8\" fill=\"url(#bg)\" />");
        sb.AppendLine($"  <rect x=\"1\" y=\"1\" width=\"{width - 2}\" height=\"{height - 2}\" rx=\"7\" fill=\"none\" stroke=\"#334155\" />");

        sb.AppendLine("  <text x=\"24\" y=\"40\" class=\"title\">▣ " + EscapeXml(title) + "</text>");

        for (int i = 0; i < lines.Length; i++)
        {
            sb.AppendLine($"  <text x=\"24\" y=\"{72 + (i * 22)}\" class=\"desc\">{EscapeXml(lines[i])}</text>");
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

    private static string[] WrapDescription(string text, int maxCharsPerLine, int maxLines)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = new StringBuilder();

        foreach (string word in words)
        {
            if (current.Length == 0)
            {
                current.Append(word);
                continue;
            }

            if (current.Length + 1 + word.Length <= maxCharsPerLine)
            {
                current.Append(' ').Append(word);
                continue;
            }

            lines.Add(current.ToString());
            current.Clear();
            current.Append(word);

            if (lines.Count == maxLines)
            {
                break;
            }
        }

        if (lines.Count < maxLines && current.Length > 0)
        {
            lines.Add(current.ToString());
        }

        if (lines.Count == 0)
        {
            lines.Add("No description provided");
        }

        if (lines.Count > maxLines)
        {
            lines = lines.Take(maxLines).ToList();
        }

        if (lines.Count == maxLines)
        {
            string last = lines[^1];
            if (text.Length > string.Join(' ', lines).Length && !last.EndsWith("...", StringComparison.Ordinal))
            {
                lines[^1] = TrimToLength(last, maxCharsPerLine - 3) + "...";
            }
        }

        return lines.ToArray();
    }

    private static string TrimToLength(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength].TrimEnd();
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
