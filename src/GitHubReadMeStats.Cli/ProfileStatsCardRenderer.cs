using System.Globalization;
using System.Security;
using System.Text;

namespace GitHubReadMeStats.Cli;

internal static class ProfileStatsCardRenderer
{
    public static string Render(
        UserSummary summary,
        DateTimeOffset generatedAtUtc,
        TimeDisplaySettings timeDisplay,
        CardColorTheme? colorTheme = null)
    {
        const int width = 495;
        const int height = 210;
        const int chartX = 224;
        const int chartY = 26;
        const int chartWidth = 245;
        const int chartHeight = 132;

        DateTimeOffset generatedAtLocal = TimeZoneInfo.ConvertTime(generatedAtUtc, timeDisplay.TimeZone);
        DateOnly today = DateOnly.FromDateTime(generatedAtLocal.Date);
        IReadOnlyList<MonthlyContributionPoint> monthlySeries = BuildMonthlySeries(summary.ContributionDays, months: 13, today);
        int axisMax = BuildAxisMax(monthlySeries.Max(x => x.Count));

        List<(double X, double Y)> chartPoints = BuildChartPoints(monthlySeries, chartX, chartY, chartWidth, chartHeight, axisMax);
        string linePath = BuildSmoothLinePath(chartPoints);
        string areaPath = BuildSmoothAreaPath(chartPoints, chartY + chartHeight);

        int joinedYears = CalculateJoinedYears(summary.CreatedAt.UtcDateTime.Date, generatedAtLocal.Date);

        string backgroundStart = colorTheme?.BackgroundStart ?? "#090E2C";
        string backgroundEnd = colorTheme?.BackgroundEnd ?? "#041738";
        string borderColor = colorTheme?.Border ?? "#1E3A8A";
        string areaColor = colorTheme?.Accent ?? "#16F2D1";
        string titleColor = colorTheme?.TitleText ?? "#F43F98";
        string loginColor = colorTheme?.SecondaryText ?? "#22D3EE";
        string valueColor = colorTheme?.PrimaryText ?? "#7DD3FC";
        string axisColor = colorTheme?.SecondaryText ?? "#38BDF8";
        string xAxisColor = colorTheme?.TertiaryText ?? "#22D3EE";
        string metaColor = colorTheme?.MutedText ?? "#64748B";
        string chartTitleColor = colorTheme?.AccentStrong ?? "#06B6D4";
        string chartPanelFill = colorTheme?.PanelFill ?? "#020817";
        string chartPanelOpacity = colorTheme is null ? "0.25" : "0.34";
        string gridColor = colorTheme?.ChartGrid ?? "#1E3A8A";
        string lineColor = colorTheme?.ChartLine ?? "#06B6D4";
        string[] metricDotColors = ResolveMetricDotColors(colorTheme);

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"GitHub profile stats with yearly contribution trend\">");
        sb.AppendLine("  <defs>");
        sb.AppendLine("    <linearGradient id=\"bg\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"1\">");
        sb.AppendLine($"      <stop offset=\"0%\" stop-color=\"{EscapeXml(backgroundStart)}\" />");
        sb.AppendLine($"      <stop offset=\"100%\" stop-color=\"{EscapeXml(backgroundEnd)}\" />");
        sb.AppendLine("    </linearGradient>");
        sb.AppendLine("    <linearGradient id=\"area\" x1=\"0\" x2=\"0\" y1=\"0\" y2=\"1\">");
        sb.AppendLine($"      <stop offset=\"0%\" stop-color=\"{EscapeXml(areaColor)}\" stop-opacity=\"0.95\" />");
        sb.AppendLine($"      <stop offset=\"100%\" stop-color=\"{EscapeXml(areaColor)}\" stop-opacity=\"0.05\" />");
        sb.AppendLine("    </linearGradient>");
        sb.AppendLine("    <style>");
        sb.AppendLine($"      .title {{ font: 700 21px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(titleColor)}; }}");
        sb.AppendLine($"      .login {{ font: 700 12px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(loginColor)}; }}");
        sb.AppendLine($"      .value {{ font: 700 12px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(valueColor)}; }}");
        sb.AppendLine($"      .axis {{ font: 600 9px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(axisColor)}; }}");
        sb.AppendLine($"      .xaxis {{ font: 600 8px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(xAxisColor)}; }}");
        sb.AppendLine($"      .meta {{ font: 600 10px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(metaColor)}; }}");
        sb.AppendLine($"      .chart-title {{ font: 700 8px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(chartTitleColor)}; }}");
        sb.AppendLine("    </style>");
        sb.AppendLine("  </defs>");

        sb.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" rx=\"8\" fill=\"url(#bg)\" />");
        sb.AppendLine($"  <rect x=\"1\" y=\"1\" width=\"{width - 2}\" height=\"{height - 2}\" rx=\"7\" fill=\"none\" stroke=\"{EscapeXml(borderColor)}\" />");

        sb.AppendLine($"  <text x=\"18\" y=\"26\" class=\"title\">{EscapeXml(summary.Login)} ({EscapeXml(summary.DisplayName)})</text>");
        sb.AppendLine($"  <text x=\"18\" y=\"45\" class=\"login\">@{EscapeXml(summary.Login)}</text>");

        int metricY = 69;
        AppendMetricRow(sb, metricY, metricDotColors[0], $"{summary.ContributionsThisYear.ToString("N0", CultureInfo.InvariantCulture)} Contributions in {generatedAtLocal.Year}");
        AppendMetricRow(sb, metricY + 16, metricDotColors[1], $"{summary.PublicRepositories.ToString("N0", CultureInfo.InvariantCulture)} Public Repositories");
        AppendMetricRow(sb, metricY + 32, metricDotColors[2], $"{summary.PrivateRepositories.ToString("N0", CultureInfo.InvariantCulture)} Private Repositories");
        AppendMetricRow(sb, metricY + 48, metricDotColors[3], $"{summary.ForkedRepositories.ToString("N0", CultureInfo.InvariantCulture)} Forked Repositories");
        AppendMetricRow(sb, metricY + 64, metricDotColors[4], $"Joined GitHub {joinedYears} years ago");
        if (!string.IsNullOrWhiteSpace(summary.Location))
        {
            AppendMetricRow(sb, metricY + 80, metricDotColors[5], summary.Location!);
        }

        sb.AppendLine($"  <text x=\"{chartX + chartWidth - 104}\" y=\"14\" class=\"chart-title\">contributions in the last year</text>");
        sb.AppendLine($"  <rect x=\"{chartX}\" y=\"{chartY}\" width=\"{chartWidth}\" height=\"{chartHeight}\" rx=\"4\" fill=\"{EscapeXml(chartPanelFill)}\" fill-opacity=\"{chartPanelOpacity}\" />");

        for (int value = 0; value <= axisMax; value += 20)
        {
            double ratio = axisMax == 0 ? 0 : value / (double)axisMax;
            double y = chartY + chartHeight - (chartHeight * ratio);
            sb.AppendLine($"  <line x1=\"{chartX}\" y1=\"{FormatNumber(y)}\" x2=\"{chartX + chartWidth}\" y2=\"{FormatNumber(y)}\" stroke=\"{EscapeXml(gridColor)}\" stroke-opacity=\"0.55\" />");
            sb.AppendLine($"  <text x=\"{chartX + chartWidth + 6}\" y=\"{FormatNumber(y + 3)}\" class=\"axis\">{value}</text>");
        }

        sb.AppendLine($"  <path d=\"{areaPath}\" fill=\"url(#area)\" stroke=\"none\" />");
        sb.AppendLine($"  <path d=\"{linePath}\" fill=\"none\" stroke=\"{EscapeXml(lineColor)}\" stroke-width=\"2\" />");

        for (int i = 0; i < monthlySeries.Count; i++)
        {
            if (i % 2 != 0 && i != monthlySeries.Count - 1)
            {
                continue;
            }

            MonthlyContributionPoint point = monthlySeries[i];
            double x = chartX + (chartWidth * i / (double)Math.Max(1, monthlySeries.Count - 1));
            sb.AppendLine($"  <text x=\"{FormatNumber(x)}\" y=\"{chartY + chartHeight + 13}\" class=\"xaxis\" text-anchor=\"middle\">{point.Month:yy/MM}</text>");
        }

        sb.AppendLine($"  <text x=\"18\" y=\"198\" class=\"meta\">Updated {generatedAtLocal:yyyy-MM-dd HH:mm} {EscapeXml(timeDisplay.Label)}</text>");
        sb.AppendLine("</svg>");

        return sb.ToString();
    }

    private static IReadOnlyList<MonthlyContributionPoint> BuildMonthlySeries(IReadOnlyList<ContributionDaySummary> source, int months, DateOnly today)
    {
        DateOnly currentMonthStart = new(today.Year, today.Month, 1);
        DateOnly firstMonthStart = currentMonthStart.AddMonths(-(months - 1));

        var result = new List<MonthlyContributionPoint>(months);
        for (int i = 0; i < months; i++)
        {
            DateOnly monthStart = firstMonthStart.AddMonths(i);
            DateOnly monthEnd = monthStart.AddMonths(1).AddDays(-1);
            int count = source
                .Where(x => x.Date >= monthStart && x.Date <= monthEnd)
                .Sum(x => x.ContributionCount);

            result.Add(new MonthlyContributionPoint(monthStart, Math.Max(0, count)));
        }

        return result;
    }

    private static int BuildAxisMax(int observedMax)
    {
        int rounded = (int)Math.Ceiling(Math.Max(observedMax, 20) / 20.0) * 20;
        return Math.Max(100, rounded);
    }

    private static List<(double X, double Y)> BuildChartPoints(
        IReadOnlyList<MonthlyContributionPoint> monthlySeries,
        int chartX,
        int chartY,
        int chartWidth,
        int chartHeight,
        int axisMax)
    {
        var points = new List<(double X, double Y)>(monthlySeries.Count);
        for (int i = 0; i < monthlySeries.Count; i++)
        {
            double x = chartX + (chartWidth * i / (double)Math.Max(1, monthlySeries.Count - 1));
            double y = ToChartY(monthlySeries[i].Count, chartY, chartHeight, axisMax);
            points.Add((x, y));
        }

        return points;
    }

    private static string BuildSmoothLinePath(IReadOnlyList<(double X, double Y)> points)
    {
        if (points.Count == 0)
        {
            return string.Empty;
        }

        if (points.Count == 1)
        {
            return $"M {FormatNumber(points[0].X)} {FormatNumber(points[0].Y)}";
        }

        var sb = new StringBuilder();
        sb.Append($"M {FormatNumber(points[0].X)} {FormatNumber(points[0].Y)} ");

        for (int i = 0; i < points.Count - 1; i++)
        {
            (double X, double Y) p0 = i == 0 ? points[i] : points[i - 1];
            (double X, double Y) p1 = points[i];
            (double X, double Y) p2 = points[i + 1];
            (double X, double Y) p3 = i + 2 < points.Count ? points[i + 2] : p2;

            double c1x = p1.X + (p2.X - p0.X) / 6.0;
            double c1y = p1.Y + (p2.Y - p0.Y) / 6.0;
            double c2x = p2.X - (p3.X - p1.X) / 6.0;
            double c2y = p2.Y - (p3.Y - p1.Y) / 6.0;

            sb.Append($"C {FormatNumber(c1x)} {FormatNumber(c1y)} {FormatNumber(c2x)} {FormatNumber(c2y)} {FormatNumber(p2.X)} {FormatNumber(p2.Y)} ");
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildSmoothAreaPath(IReadOnlyList<(double X, double Y)> points, double baselineY)
    {
        if (points.Count == 0)
        {
            return string.Empty;
        }

        if (points.Count == 1)
        {
            return $"M {FormatNumber(points[0].X)} {FormatNumber(baselineY)} L {FormatNumber(points[0].X)} {FormatNumber(points[0].Y)} L {FormatNumber(points[0].X)} {FormatNumber(baselineY)} Z";
        }

        var sb = new StringBuilder();
        sb.Append($"M {FormatNumber(points[0].X)} {FormatNumber(baselineY)} ");
        sb.Append($"L {FormatNumber(points[0].X)} {FormatNumber(points[0].Y)} ");

        for (int i = 0; i < points.Count - 1; i++)
        {
            (double X, double Y) p0 = i == 0 ? points[i] : points[i - 1];
            (double X, double Y) p1 = points[i];
            (double X, double Y) p2 = points[i + 1];
            (double X, double Y) p3 = i + 2 < points.Count ? points[i + 2] : p2;

            double c1x = p1.X + (p2.X - p0.X) / 6.0;
            double c1y = p1.Y + (p2.Y - p0.Y) / 6.0;
            double c2x = p2.X - (p3.X - p1.X) / 6.0;
            double c2y = p2.Y - (p3.Y - p1.Y) / 6.0;

            sb.Append($"C {FormatNumber(c1x)} {FormatNumber(c1y)} {FormatNumber(c2x)} {FormatNumber(c2y)} {FormatNumber(p2.X)} {FormatNumber(p2.Y)} ");
        }

        sb.Append($"L {FormatNumber(points[^1].X)} {FormatNumber(baselineY)} Z");
        return sb.ToString();
    }

    private static double ToChartY(int value, int chartY, int chartHeight, int axisMax)
    {
        double ratio = axisMax <= 0 ? 0 : Math.Clamp(value / (double)axisMax, 0, 1);
        return chartY + chartHeight - (chartHeight * ratio);
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

    private static string[] ResolveMetricDotColors(CardColorTheme? colorTheme)
    {
        string[] defaults = ["#FACC15", "#22D3EE", "#38BDF8", "#A78BFA", "#84CC16", "#F472B6"];
        if (colorTheme is null || colorTheme.MetricDotPalette.Count == 0)
        {
            return defaults;
        }

        var result = new string[defaults.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = colorTheme.MetricDotPalette[i % colorTheme.MetricDotPalette.Count];
        }

        return result;
    }

    private sealed record MonthlyContributionPoint(DateOnly Month, int Count);
}
