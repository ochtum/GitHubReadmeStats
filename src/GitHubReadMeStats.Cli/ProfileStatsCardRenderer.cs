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
        const int chartX = 224;
        const int chartY = 32;
        const int chartWidth = 245;
        const int chartHeight = 132;

        DateOnly today = DateOnly.FromDateTime(generatedAtUtc.UtcDateTime.Date);
        IReadOnlyList<int> series = BuildDailySeries(summary.ContributionDays, days: 56, today);
        int axisMax = BuildAxisMax(series.Max());

        string areaPath = BuildAreaPath(series, chartX, chartY, chartWidth, chartHeight, axisMax);
        string linePath = BuildLinePath(series, chartX, chartY, chartWidth, chartHeight, axisMax);
        IReadOnlyList<(int Index, DateOnly Date)> xTicks = BuildXAxisTicks(series.Count, today.AddDays(-(series.Count - 1)));

        int joinedYears = CalculateJoinedYears(summary.CreatedAt.UtcDateTime.Date, generatedAtUtc.UtcDateTime.Date);

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"GitHub profile stats with contribution trend\">");
        sb.AppendLine("  <defs>");
        sb.AppendLine("    <linearGradient id=\"bg\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"1\">");
        sb.AppendLine("      <stop offset=\"0%\" stop-color=\"#090E2C\" />");
        sb.AppendLine("      <stop offset=\"100%\" stop-color=\"#041738\" />");
        sb.AppendLine("    </linearGradient>");
        sb.AppendLine("    <linearGradient id=\"area\" x1=\"0\" x2=\"0\" y1=\"0\" y2=\"1\">");
        sb.AppendLine("      <stop offset=\"0%\" stop-color=\"#16F2D1\" stop-opacity=\"0.95\" />");
        sb.AppendLine("      <stop offset=\"100%\" stop-color=\"#0EA5E9\" stop-opacity=\"0.25\" />");
        sb.AppendLine("    </linearGradient>");
        sb.AppendLine("    <style>");
        sb.AppendLine("      .title { font: 700 26px 'Segoe UI', Arial, sans-serif; fill: #F43F98; }");
        sb.AppendLine("      .login { font: 700 12px 'Segoe UI', Arial, sans-serif; fill: #22D3EE; }");
        sb.AppendLine("      .label { font: 600 11px 'Segoe UI', Arial, sans-serif; fill: #7DD3FC; }");
        sb.AppendLine("      .value { font: 700 11px 'Segoe UI', Arial, sans-serif; fill: #E2E8F0; }");
        sb.AppendLine("      .axis { font: 600 9px 'Segoe UI', Arial, sans-serif; fill: #38BDF8; }");
        sb.AppendLine("      .xaxis { font: 600 8px 'Segoe UI', Arial, sans-serif; fill: #22D3EE; }");
        sb.AppendLine("      .meta { font: 600 10px 'Segoe UI', Arial, sans-serif; fill: #64748B; }");
        sb.AppendLine("      .chart-title { font: 700 8px 'Segoe UI', Arial, sans-serif; fill: #06B6D4; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("  </defs>");

        sb.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" rx=\"8\" fill=\"url(#bg)\" />");
        sb.AppendLine($"  <rect x=\"1\" y=\"1\" width=\"{width - 2}\" height=\"{height - 2}\" rx=\"7\" fill=\"none\" stroke=\"#1E3A8A\" />");

        sb.AppendLine($"  <text x=\"18\" y=\"26\" class=\"title\">{EscapeXml(summary.Login)} ({EscapeXml(summary.DisplayName)})</text>");
        sb.AppendLine($"  <text x=\"18\" y=\"45\" class=\"login\">@{EscapeXml(summary.Login)}</text>");

        int metricY = 69;
        AppendMetricRow(sb, metricY, "#FACC15", $"{summary.ContributionsThisYear.ToString("N0", CultureInfo.InvariantCulture)} Contributions in {generatedAtUtc.Year}");
        AppendMetricRow(sb, metricY + 18, "#22D3EE", $"{summary.PublicRepositories.ToString("N0", CultureInfo.InvariantCulture)} Public Repositories");
        AppendMetricRow(sb, metricY + 36, "#84CC16", $"Joined GitHub {joinedYears} years ago");
        AppendMetricRow(sb, metricY + 54, "#A78BFA", $"{summary.Followers.ToString("N0", CultureInfo.InvariantCulture)} Followers");
        if (!string.IsNullOrWhiteSpace(summary.Location))
        {
            AppendMetricRow(sb, metricY + 72, "#F472B6", EscapeXml(summary.Location!));
        }

        sb.AppendLine($"  <text x=\"{chartX + chartWidth - 96}\" y=\"16\" class=\"chart-title\">contributions in the last 8 weeks</text>");
        sb.AppendLine($"  <rect x=\"{chartX}\" y=\"{chartY}\" width=\"{chartWidth}\" height=\"{chartHeight}\" rx=\"4\" fill=\"#020817\" fill-opacity=\"0.25\" />");

        for (int i = 0; i <= 4; i++)
        {
            double ratio = i / 4.0;
            double y = chartY + (chartHeight * ratio);
            int value = (int)Math.Round(axisMax * (1.0 - ratio), MidpointRounding.AwayFromZero);
            sb.AppendLine($"  <line x1=\"{chartX}\" y1=\"{FormatNumber(y)}\" x2=\"{chartX + chartWidth}\" y2=\"{FormatNumber(y)}\" stroke=\"#1E3A8A\" stroke-opacity=\"0.55\" />");
            sb.AppendLine($"  <text x=\"{chartX + chartWidth + 6}\" y=\"{FormatNumber(y + 3)}\" class=\"axis\">{value}</text>");
        }

        sb.AppendLine($"  <path d=\"{areaPath}\" fill=\"url(#area)\" stroke=\"none\" />");
        sb.AppendLine($"  <path d=\"{linePath}\" fill=\"none\" stroke=\"#06B6D4\" stroke-width=\"2\" />");

        foreach ((int index, DateOnly date) in xTicks)
        {
            double x = chartX + (chartWidth * index / (double)Math.Max(1, series.Count - 1));
            sb.AppendLine($"  <text x=\"{FormatNumber(x)}\" y=\"{chartY + chartHeight + 13}\" class=\"xaxis\" text-anchor=\"middle\">{date:MM/dd}</text>");
        }

        sb.AppendLine($"  <text x=\"18\" y=\"198\" class=\"meta\">Updated {generatedAtUtc:yyyy-MM-dd HH:mm} UTC</text>");
        sb.AppendLine("</svg>");

        return sb.ToString();
    }

    private static IReadOnlyList<int> BuildDailySeries(IReadOnlyList<ContributionDaySummary> source, int days, DateOnly today)
    {
        var map = source
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => g.Last().ContributionCount);

        var result = new List<int>(days);
        DateOnly start = today.AddDays(-(days - 1));
        for (int i = 0; i < days; i++)
        {
            DateOnly date = start.AddDays(i);
            if (!map.TryGetValue(date, out int count))
            {
                count = 0;
            }

            result.Add(Math.Max(0, count));
        }

        return result;
    }

    private static int BuildAxisMax(int observedMax)
    {
        int max = Math.Max(observedMax, 10);
        int step = Math.Max(5, (int)Math.Ceiling(max / 20.0) * 5);
        return step * 4;
    }

    private static string BuildAreaPath(IReadOnlyList<int> points, int chartX, int chartY, int chartWidth, int chartHeight, int axisMax)
    {
        double bottom = chartY + chartHeight;
        var sb = new StringBuilder();
        sb.Append($"M {chartX} {FormatNumber(bottom)} ");

        for (int i = 0; i < points.Count; i++)
        {
            double x = chartX + (chartWidth * i / (double)Math.Max(1, points.Count - 1));
            double y = ToChartY(points[i], chartY, chartHeight, axisMax);
            sb.Append($"L {FormatNumber(x)} {FormatNumber(y)} ");
        }

        sb.Append($"L {chartX + chartWidth} {FormatNumber(bottom)} Z");
        return sb.ToString();
    }

    private static string BuildLinePath(IReadOnlyList<int> points, int chartX, int chartY, int chartWidth, int chartHeight, int axisMax)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < points.Count; i++)
        {
            double x = chartX + (chartWidth * i / (double)Math.Max(1, points.Count - 1));
            double y = ToChartY(points[i], chartY, chartHeight, axisMax);
            string command = i == 0 ? "M" : "L";
            sb.Append($"{command} {FormatNumber(x)} {FormatNumber(y)} ");
        }

        return sb.ToString().TrimEnd();
    }

    private static double ToChartY(int value, int chartY, int chartHeight, int axisMax)
    {
        double ratio = axisMax <= 0 ? 0 : Math.Clamp(value / (double)axisMax, 0, 1);
        return chartY + chartHeight - (chartHeight * ratio);
    }

    private static IReadOnlyList<(int Index, DateOnly Date)> BuildXAxisTicks(int count, DateOnly startDate)
    {
        var result = new List<(int Index, DateOnly Date)>();
        const int ticks = 6;

        for (int i = 0; i < ticks; i++)
        {
            int index = (int)Math.Round((count - 1) * (i / (double)(ticks - 1)), MidpointRounding.AwayFromZero);
            if (result.Any(x => x.Index == index))
            {
                continue;
            }

            result.Add((index, startDate.AddDays(index)));
        }

        return result;
    }

    private static int CalculateJoinedYears(DateTime joinedDate, DateTime referenceDate)
    {
        int years = referenceDate.Year - joinedDate.Year;
        if (referenceDate.Date < joinedDate.Date.AddYears(years))
        {
            years--;
        }

        return Math.Max(0, years);
    }

    private static void AppendMetricRow(StringBuilder sb, int y, string dotColor, string text)
    {
        sb.AppendLine($"  <circle cx=\"20\" cy=\"{y - 3}\" r=\"3\" fill=\"{dotColor}\" />");
        sb.AppendLine($"  <text x=\"28\" y=\"{y}\" class=\"value\">{EscapeXml(text)}</text>");
    }

    private static string EscapeXml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return SecurityElement.Escape(value) ?? string.Empty;
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
