using System.Globalization;
using System.Security;
using System.Text;

namespace GitHubReadMeStats.Cli;

internal static class PublicRepositoriesTotalsCardRenderer
{
    public static string Render(
        PublicRepositoriesTotalsCardData totals,
        DateTimeOffset generatedAtUtc,
        TimeDisplaySettings timeDisplay)
    {
        const int width = 640;
        const int height = 260;
        const int cardX = 24;
        const int cardWidth = width - (cardX * 2);

        DateTimeOffset generatedAtLocal = TimeZoneInfo.ConvertTime(generatedAtUtc, timeDisplay.TimeZone);

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"Public repository totals for {EscapeXml(totals.ViewerLogin)}\">");
        sb.AppendLine("  <defs>");
        sb.AppendLine("    <linearGradient id=\"totals-bg\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"1\">");
        sb.AppendLine("      <stop offset=\"0%\" stop-color=\"#0D1B33\" />");
        sb.AppendLine("      <stop offset=\"100%\" stop-color=\"#0B132A\" />");
        sb.AppendLine("    </linearGradient>");
        sb.AppendLine("    <style>");
        sb.AppendLine("      .title { font: 700 24px 'Segoe UI', Arial, sans-serif; fill: #F8FAFC; }");
        sb.AppendLine("      .sub { font: 500 14px 'Segoe UI', Arial, sans-serif; fill: #BFDBFE; }");
        sb.AppendLine("      .section { font: 700 12px 'Segoe UI', Arial, sans-serif; fill: #93C5FD; }");
        sb.AppendLine("      .metric-label { font: 600 10px 'Segoe UI', Arial, sans-serif; fill: #93C5FD; }");
        sb.AppendLine("      .metric-value { font: 700 20px 'Segoe UI', Arial, sans-serif; fill: #E2E8F0; font-variant-numeric: tabular-nums; }");
        sb.AppendLine("      .summary-label { font: 600 11px 'Segoe UI', Arial, sans-serif; fill: #7DD3FC; }");
        sb.AppendLine("      .summary-value { font: 700 23px 'Segoe UI', Arial, sans-serif; fill: #F8FAFC; font-variant-numeric: tabular-nums; }");
        sb.AppendLine("      .hint { font: 500 10px 'Segoe UI', Arial, sans-serif; fill: #64748B; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("  </defs>");

        sb.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" rx=\"18\" fill=\"url(#totals-bg)\" />");
        sb.AppendLine($"  <rect x=\"1\" y=\"1\" width=\"{width - 2}\" height=\"{height - 2}\" rx=\"17\" fill=\"none\" stroke=\"#1E3A8A\" />");

        sb.AppendLine($"  <text x=\"{cardX}\" y=\"42\" class=\"title\">Public Repository Totals</text>");
        sb.AppendLine($"  <text x=\"{cardX}\" y=\"66\" class=\"sub\">@{EscapeXml(totals.ViewerLogin)} | {totals.PublicRepositoryCount.ToString("N0", CultureInfo.InvariantCulture)} public repos</text>");

        const int trafficPanelY = 82;
        const int trafficPanelHeight = 80;
        sb.AppendLine($"  <rect x=\"{cardX}\" y=\"{trafficPanelY}\" width=\"{cardWidth}\" height=\"{trafficPanelHeight}\" rx=\"10\" fill=\"#0A1E3A\" fill-opacity=\"0.6\" stroke=\"#1D4ED8\" stroke-opacity=\"0.65\" />");
        sb.AppendLine($"  <text x=\"{cardX + 12}\" y=\"{trafficPanelY + 17}\" class=\"section\">Traffic</text>");

        AppendMetricColumns(
            sb,
            cardX,
            trafficPanelY + 24,
            cardWidth,
            new[]
            {
                ("Git Clones", totals.TotalCloneCount),
                ("Unique Cloners", totals.TotalUniqueCloners),
                ("Total Views", totals.TotalViewCount),
                ("Unique Visitors", totals.TotalUniqueVisitors),
            });

        if (totals.TrafficAvailableRepositoryCount > 0 &&
            totals.TrafficSinceDate is not null &&
            totals.TrafficLastRecordedDate is not null)
        {
            sb.AppendLine(
                $"  <text x=\"{cardX + 12}\" y=\"{trafficPanelY + trafficPanelHeight - 6}\" class=\"hint\">" +
                $"Traffic covers {totals.TrafficAvailableRepositoryCount}/{totals.PublicRepositoryCount} repos since {totals.TrafficSinceDate:yyyy-MM-dd} (last {totals.TrafficLastRecordedDate:yyyy-MM-dd})" +
                "</text>");
        }
        else
        {
            sb.AppendLine($"  <text x=\"{cardX + 12}\" y=\"{trafficPanelY + trafficPanelHeight - 6}\" class=\"hint\">Traffic unavailable (GH_TOKEN requires repository Administration: Read).</text>");
        }

        const int summaryPanelY = 170;
        const int summaryPanelHeight = 62;
        sb.AppendLine($"  <rect x=\"{cardX}\" y=\"{summaryPanelY}\" width=\"{cardWidth}\" height=\"{summaryPanelHeight}\" rx=\"10\" fill=\"#0E223E\" fill-opacity=\"0.68\" stroke=\"#1E40AF\" stroke-opacity=\"0.65\" />");
        sb.AppendLine($"  <text x=\"{cardX + 12}\" y=\"{summaryPanelY + 18}\" class=\"section\">Repository Signals</text>");

        AppendSummaryMetrics(
            sb,
            cardX,
            summaryPanelY + 22,
            cardWidth,
            new[]
            {
                ("Fork", totals.TotalForks),
                ("Watch", totals.TotalWatchers),
                ("Starred", totals.TotalStarred),
            });

        sb.AppendLine($"  <text x=\"{cardX}\" y=\"248\" class=\"hint\">Updated {generatedAtLocal:yyyy-MM-dd HH:mm} {EscapeXml(timeDisplay.Label)}</text>");
        sb.AppendLine("</svg>");

        return sb.ToString();
    }

    private static void AppendMetricColumns(
        StringBuilder sb,
        int x,
        int y,
        int width,
        IReadOnlyList<(string Label, long Value)> metrics)
    {
        double columnWidth = width / (double)Math.Max(1, metrics.Count);
        for (int i = 0; i < metrics.Count; i++)
        {
            double centerX = x + (columnWidth * i) + (columnWidth / 2d);
            if (i > 0)
            {
                double separatorX = x + (columnWidth * i);
                sb.AppendLine($"  <line x1=\"{Format(separatorX)}\" y1=\"{y - 3}\" x2=\"{Format(separatorX)}\" y2=\"{y + 46}\" stroke=\"#1D4ED8\" stroke-opacity=\"0.45\" />");
            }

            sb.AppendLine($"  <text x=\"{Format(centerX)}\" y=\"{y + 10}\" class=\"metric-label\" text-anchor=\"middle\">{EscapeXml(metrics[i].Label)}</text>");
            sb.AppendLine($"  <text x=\"{Format(centerX)}\" y=\"{y + 34}\" class=\"metric-value\" text-anchor=\"middle\">{FormatMetric(metrics[i].Value)}</text>");
        }
    }

    private static void AppendSummaryMetrics(
        StringBuilder sb,
        int x,
        int y,
        int width,
        IReadOnlyList<(string Label, long Value)> metrics)
    {
        double columnWidth = width / (double)Math.Max(1, metrics.Count);
        for (int i = 0; i < metrics.Count; i++)
        {
            double centerX = x + (columnWidth * i) + (columnWidth / 2d);
            if (i > 0)
            {
                double separatorX = x + (columnWidth * i);
                sb.AppendLine($"  <line x1=\"{Format(separatorX)}\" y1=\"{y - 5}\" x2=\"{Format(separatorX)}\" y2=\"{y + 32}\" stroke=\"#1E40AF\" stroke-opacity=\"0.45\" />");
            }

            sb.AppendLine($"  <text x=\"{Format(centerX)}\" y=\"{y + 12}\" class=\"summary-label\" text-anchor=\"middle\">{EscapeXml(metrics[i].Label)}</text>");
            sb.AppendLine($"  <text x=\"{Format(centerX)}\" y=\"{y + 36}\" class=\"summary-value\" text-anchor=\"middle\">{FormatMetric(metrics[i].Value)}</text>");
        }
    }

    private static string FormatMetric(long value)
    {
        return Math.Max(0, value).ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string Format(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
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
