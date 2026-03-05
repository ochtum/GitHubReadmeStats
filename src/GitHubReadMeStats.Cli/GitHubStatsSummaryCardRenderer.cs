using System.Globalization;
using System.Security;
using System.Text;

namespace GitHubReadMeStats.Cli;

internal static class GitHubStatsSummaryCardRenderer
{
    public static string Render(UserSummary summary)
    {
        const int width = 495;
        const int height = 210;
        const double gaugeCenterX = 414;
        const double gaugeCenterY = 103;
        const double gaugeRadius = 34;

        Grade grade = CalculateGrade(summary);
        string title = $"{summary.Login}'s GitHub Stats";

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"GitHub stats summary for {EscapeXml(summary.Login)}\">");
        sb.AppendLine("  <defs>");
        sb.AppendLine("    <linearGradient id=\"stats-bg\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"1\">");
        sb.AppendLine("      <stop offset=\"0%\" stop-color=\"#0D1233\" />");
        sb.AppendLine("      <stop offset=\"100%\" stop-color=\"#100B2F\" />");
        sb.AppendLine("    </linearGradient>");
        sb.AppendLine("    <style>");
        sb.AppendLine("      .title { font: 700 28px 'Segoe UI', Arial, sans-serif; fill: #F43F93; }");
        sb.AppendLine("      .label { font: 700 12px 'Segoe UI', Arial, sans-serif; fill: #67E8F9; }");
        sb.AppendLine("      .value { font: 700 22px 'Segoe UI', Arial, sans-serif; fill: #A7F3D0; }");
        sb.AppendLine("      .icon { font: 700 13px 'Segoe UI Symbol', 'Segoe UI', Arial, sans-serif; fill: #FACC15; }");
        sb.AppendLine("      .grade { font: 700 46px 'Segoe UI', Arial, sans-serif; fill: #67E8F9; }");
        sb.AppendLine("      .score { font: 600 11px 'Segoe UI', Arial, sans-serif; fill: #94A3B8; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("  </defs>");

        sb.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" rx=\"8\" fill=\"url(#stats-bg)\" />");
        sb.AppendLine($"  <rect x=\"1\" y=\"1\" width=\"{width - 2}\" height=\"{height - 2}\" rx=\"7\" fill=\"none\" stroke=\"#334155\" />");
        sb.AppendLine($"  <text x=\"24\" y=\"36\" class=\"title\">{EscapeXml(title)}</text>");

        AppendMetricRow(sb, 24, 63, "☆", "Total Stars Earned:", summary.TotalStarsEarned);
        AppendMetricRow(sb, 24, 91, "◔", "Total Commits (last year):", summary.TotalCommitsLastYear);
        AppendMetricRow(sb, 24, 119, "⑂", "Total PRs:", summary.TotalPullRequestsLastYear);
        AppendMetricRow(sb, 24, 147, "◍", "Total Issues:", summary.TotalIssuesLastYear);
        AppendMetricRow(sb, 24, 175, "▣", "Contributed to (last year):", summary.ContributedToRepositoriesLastYear);

        sb.AppendLine($"  <circle cx=\"{Format(gaugeCenterX)}\" cy=\"{Format(gaugeCenterY)}\" r=\"{Format(gaugeRadius)}\" fill=\"none\" stroke=\"#312E81\" stroke-width=\"7\" />");
        if (grade.Score > 0)
        {
            string gaugePath = BuildArcPath(gaugeCenterX, gaugeCenterY, gaugeRadius, grade.Score / 100d);
            sb.AppendLine($"  <path d=\"{gaugePath}\" fill=\"none\" stroke=\"#EC4899\" stroke-width=\"7\" stroke-linecap=\"round\" />");
        }

        sb.AppendLine($"  <text x=\"{Format(gaugeCenterX)}\" y=\"{Format(gaugeCenterY + 13)}\" class=\"grade\" text-anchor=\"middle\">{grade.Letter}</text>");
        sb.AppendLine($"  <text x=\"{Format(gaugeCenterX)}\" y=\"{Format(gaugeCenterY + 33)}\" class=\"score\" text-anchor=\"middle\">score {grade.Score}</text>");
        sb.AppendLine("</svg>");

        return sb.ToString();
    }

    private static void AppendMetricRow(StringBuilder sb, int x, int y, string icon, string label, int value)
    {
        sb.AppendLine($"  <text x=\"{x}\" y=\"{y}\" class=\"icon\">{EscapeXml(icon)}</text>");
        sb.AppendLine($"  <text x=\"{x + 18}\" y=\"{y}\" class=\"label\">{EscapeXml(label)}</text>");
        sb.AppendLine($"  <text x=\"284\" y=\"{y}\" class=\"value\" text-anchor=\"end\">{value.ToString("N0", CultureInfo.InvariantCulture)}</text>");
    }

    private static Grade CalculateGrade(UserSummary summary)
    {
        static double Log10Score(int value) => Math.Log10(Math.Max(value, 0) + 1d);

        double raw =
            (25d * Log10Score(summary.TotalStarsEarned)) +
            (35d * Log10Score(summary.TotalCommitsLastYear)) +
            (20d * Log10Score(summary.TotalPullRequestsLastYear)) +
            (15d * Log10Score(summary.TotalIssuesLastYear)) +
            (5d * Log10Score(summary.ContributedToRepositoriesLastYear));

        int score = (int)Math.Round(Math.Clamp(raw / 2.6d, 0d, 100d), MidpointRounding.AwayFromZero);
        string letter = score switch
        {
            >= 90 => "S",
            >= 80 => "A",
            >= 70 => "B",
            >= 60 => "C",
            >= 50 => "D",
            >= 40 => "E",
            _ => "F",
        };

        return new Grade(letter, score);
    }

    private static string BuildArcPath(double cx, double cy, double radius, double ratio)
    {
        if (ratio >= 0.999d)
        {
            double startX = cx;
            double startY = cy - radius;
            double endY = startY + 0.01d;
            return string.Create(CultureInfo.InvariantCulture,
                $"M {Format(startX)} {Format(startY)} A {Format(radius)} {Format(radius)} 0 1 1 {Format(startX)} {Format(endY)}");
        }

        double startAngle = -90d;
        double endAngle = startAngle + (360d * ratio);
        double startRad = DegreesToRadians(startAngle);
        double endRad = DegreesToRadians(endAngle);

        double x1 = cx + (radius * Math.Cos(startRad));
        double y1 = cy + (radius * Math.Sin(startRad));
        double x2 = cx + (radius * Math.Cos(endRad));
        double y2 = cy + (radius * Math.Sin(endRad));
        int largeArcFlag = ratio > 0.5d ? 1 : 0;

        return string.Create(CultureInfo.InvariantCulture,
            $"M {Format(x1)} {Format(y1)} A {Format(radius)} {Format(radius)} 0 {largeArcFlag} 1 {Format(x2)} {Format(y2)}");
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180d);
    }

    private static string EscapeXml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return SecurityElement.Escape(value) ?? string.Empty;
    }

    private static string Format(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private readonly record struct Grade(string Letter, int Score);
}
