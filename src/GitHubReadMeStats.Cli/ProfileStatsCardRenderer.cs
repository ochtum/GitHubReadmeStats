using System.Globalization;
using System.Security;
using System.Text;

namespace GitHubReadMeStats.Cli;

internal static class ProfileStatsCardRenderer
{
    public static string Render(UserSummary summary, DateTimeOffset generatedAtUtc)
    {
        const int width = 495;
        const int height = 210;

        var sb = new StringBuilder();

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"GitHub profile stats\">");
        sb.AppendLine("  <defs>");
        sb.AppendLine("    <linearGradient id=\"bg\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"1\">");
        sb.AppendLine("      <stop offset=\"0%\" stop-color=\"#0F172A\" />");
        sb.AppendLine("      <stop offset=\"100%\" stop-color=\"#111827\" />");
        sb.AppendLine("    </linearGradient>");
        sb.AppendLine("    <style>");
        sb.AppendLine("      .title { font: 700 22px 'Segoe UI', Arial, sans-serif; fill: #F8FAFC; }");
        sb.AppendLine("      .login { font: 500 14px 'Segoe UI', Arial, sans-serif; fill: #93C5FD; }");
        sb.AppendLine("      .item { font: 600 14px 'Segoe UI', Arial, sans-serif; fill: #E2E8F0; }");
        sb.AppendLine("      .meta { font: 500 12px 'Segoe UI', Arial, sans-serif; fill: #94A3B8; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("  </defs>");

        sb.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" rx=\"18\" fill=\"url(#bg)\" />");
        sb.AppendLine($"  <rect x=\"1\" y=\"1\" width=\"{width - 2}\" height=\"{height - 2}\" rx=\"17\" fill=\"none\" stroke=\"#1E293B\" />");

        sb.AppendLine($"  <text x=\"24\" y=\"40\" class=\"title\">{EscapeXml(summary.DisplayName)}</text>");
        sb.AppendLine($"  <text x=\"24\" y=\"60\" class=\"login\">@{EscapeXml(summary.Login)}</text>");

        sb.AppendLine($"  <text x=\"24\" y=\"95\" class=\"item\">• {summary.ContributionsThisYear.ToString("N0", CultureInfo.InvariantCulture)} Contributions this year</text>");
        sb.AppendLine($"  <text x=\"24\" y=\"120\" class=\"item\">• {summary.PublicRepositories.ToString("N0", CultureInfo.InvariantCulture)} Public repositories</text>");
        sb.AppendLine($"  <text x=\"24\" y=\"145\" class=\"item\">• {summary.Followers.ToString("N0", CultureInfo.InvariantCulture)} Followers</text>");
        sb.AppendLine($"  <text x=\"24\" y=\"170\" class=\"item\">• Joined GitHub {summary.CreatedAt:yyyy-MM-dd}</text>");

        sb.AppendLine($"  <text x=\"24\" y=\"194\" class=\"meta\">Updated {generatedAtUtc:yyyy-MM-dd HH:mm} UTC</text>");
        sb.AppendLine("</svg>");

        return sb.ToString();
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
