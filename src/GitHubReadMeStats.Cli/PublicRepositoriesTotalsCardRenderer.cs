using System.Globalization;
using System.Security;
using System.Text;

namespace GitHubReadMeStats.Cli;

internal static class PublicRepositoriesTotalsCardRenderer
{
    private const string LightTextColor = "#F8FAFC";
    private const string DarkTextColor = "#0F172A";
    private const double MinimumAaContrastRatio = 4.5d;

    public static string Render(
        PublicRepositoriesTotalsCardData totals,
        DateTimeOffset generatedAtUtc,
        TimeDisplaySettings timeDisplay,
        CardColorTheme? colorTheme = null)
    {
        const int width = 640;
        const int height = 260;
        const int cardX = 24;
        const int cardWidth = width - (cardX * 2);

        DateTimeOffset generatedAtLocal = TimeZoneInfo.ConvertTime(generatedAtUtc, timeDisplay.TimeZone);

        string backgroundStart = colorTheme?.BackgroundStart ?? "#0D1B33";
        string backgroundEnd = colorTheme?.BackgroundEnd ?? "#0B132A";
        string borderColor = colorTheme?.Border ?? "#1E3A8A";
        string titleColor = colorTheme?.PrimaryText ?? "#F8FAFC";
        string subColor = colorTheme?.SecondaryText ?? "#BFDBFE";
        string sectionColor = colorTheme?.AccentSoft ?? "#93C5FD";
        string metricLabelColor = colorTheme?.SecondaryText ?? "#93C5FD";
        string metricValueColor = colorTheme?.PrimaryText ?? "#E2E8F0";
        string summaryLabelColor = colorTheme?.Accent ?? "#7DD3FC";
        string summaryValueColor = colorTheme?.PrimaryText ?? "#F8FAFC";
        string hintColor = colorTheme?.MutedText ?? "#64748B";
        string trafficPanelFill = colorTheme?.PanelFill ?? "#0A1E3A";
        string trafficPanelStroke = colorTheme?.PanelStroke ?? "#1D4ED8";
        string summaryPanelFill = colorTheme?.PanelFillAlt ?? "#0E223E";
        string summaryPanelStroke = colorTheme?.PanelStrokeSoft ?? "#1E40AF";
        string trafficSeparatorColor = colorTheme?.ChartGrid ?? "#1D4ED8";
        string summarySeparatorColor = colorTheme?.ChartGrid ?? "#1E40AF";
        string trafficPanelFillOpacity = colorTheme is null ? "0.6" : "0.62";
        string trafficPanelStrokeOpacity = colorTheme is null ? "0.65" : "0.68";
        string summaryPanelFillOpacity = colorTheme is null ? "0.68" : "0.70";
        string summaryPanelStrokeOpacity = colorTheme is null ? "0.65" : "0.68";

        double trafficPanelAlpha = ParseOpacity(trafficPanelFillOpacity);
        double summaryPanelAlpha = ParseOpacity(summaryPanelFillOpacity);
        string trafficBackgroundStart = BlendColors(backgroundStart, trafficPanelFill, trafficPanelAlpha);
        string trafficBackgroundEnd = BlendColors(backgroundEnd, trafficPanelFill, trafficPanelAlpha);
        string summaryBackgroundStart = BlendColors(backgroundStart, summaryPanelFill, summaryPanelAlpha);
        string summaryBackgroundEnd = BlendColors(backgroundEnd, summaryPanelFill, summaryPanelAlpha);

        titleColor = EnsureAaTextColor(titleColor, backgroundStart, backgroundEnd);
        subColor = EnsureAaTextColor(subColor, backgroundStart, backgroundEnd);
        sectionColor = EnsureAaTextColor(sectionColor, trafficBackgroundStart, trafficBackgroundEnd, summaryBackgroundStart, summaryBackgroundEnd);
        metricLabelColor = EnsureAaTextColor(metricLabelColor, trafficBackgroundStart, trafficBackgroundEnd);
        metricValueColor = EnsureAaTextColor(metricValueColor, trafficBackgroundStart, trafficBackgroundEnd);
        summaryLabelColor = EnsureAaTextColor(summaryLabelColor, summaryBackgroundStart, summaryBackgroundEnd);
        summaryValueColor = EnsureAaTextColor(summaryValueColor, summaryBackgroundStart, summaryBackgroundEnd);
        hintColor = EnsureAaTextColor(hintColor, trafficBackgroundStart, trafficBackgroundEnd, backgroundStart, backgroundEnd);

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"Public repository totals for {EscapeXml(totals.ViewerLogin)}\">");
        sb.AppendLine("  <defs>");
        sb.AppendLine("    <linearGradient id=\"totals-bg\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"1\">");
        sb.AppendLine($"      <stop offset=\"0%\" stop-color=\"{EscapeXml(backgroundStart)}\" />");
        sb.AppendLine($"      <stop offset=\"100%\" stop-color=\"{EscapeXml(backgroundEnd)}\" />");
        sb.AppendLine("    </linearGradient>");
        sb.AppendLine("    <style>");
        sb.AppendLine($"      .title {{ font: 700 24px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(titleColor)}; }}");
        sb.AppendLine($"      .sub {{ font: 500 14px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(subColor)}; }}");
        sb.AppendLine($"      .section {{ font: 700 12px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(sectionColor)}; }}");
        sb.AppendLine($"      .metric-label {{ font: 600 10px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(metricLabelColor)}; }}");
        sb.AppendLine($"      .metric-value {{ font: 700 20px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(metricValueColor)}; font-variant-numeric: tabular-nums; }}");
        sb.AppendLine($"      .summary-label {{ font: 600 11px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(summaryLabelColor)}; }}");
        sb.AppendLine($"      .summary-value {{ font: 700 23px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(summaryValueColor)}; font-variant-numeric: tabular-nums; }}");
        sb.AppendLine($"      .hint {{ font: 500 10px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(hintColor)}; }}");
        sb.AppendLine("    </style>");
        sb.AppendLine("  </defs>");

        sb.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" rx=\"18\" fill=\"url(#totals-bg)\" />");
        sb.AppendLine($"  <rect x=\"1\" y=\"1\" width=\"{width - 2}\" height=\"{height - 2}\" rx=\"17\" fill=\"none\" stroke=\"{EscapeXml(borderColor)}\" />");

        sb.AppendLine($"  <text x=\"{cardX}\" y=\"42\" class=\"title\">Public Repository Totals</text>");
        sb.AppendLine($"  <text x=\"{cardX}\" y=\"66\" class=\"sub\">@{EscapeXml(totals.ViewerLogin)} | {totals.PublicRepositoryCount.ToString("N0", CultureInfo.InvariantCulture)} public repos</text>");

        const int trafficPanelY = 82;
        const int trafficPanelHeight = 80;
        sb.AppendLine($"  <rect x=\"{cardX}\" y=\"{trafficPanelY}\" width=\"{cardWidth}\" height=\"{trafficPanelHeight}\" rx=\"10\" fill=\"{EscapeXml(trafficPanelFill)}\" fill-opacity=\"{trafficPanelFillOpacity}\" stroke=\"{EscapeXml(trafficPanelStroke)}\" stroke-opacity=\"{trafficPanelStrokeOpacity}\" />");
        sb.AppendLine($"  <text x=\"{cardX + 12}\" y=\"{trafficPanelY + 17}\" class=\"section\">Traffic</text>");

        AppendMetricColumns(
            sb,
            cardX,
            trafficPanelY + 24,
            cardWidth,
            trafficSeparatorColor,
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
        sb.AppendLine($"  <rect x=\"{cardX}\" y=\"{summaryPanelY}\" width=\"{cardWidth}\" height=\"{summaryPanelHeight}\" rx=\"10\" fill=\"{EscapeXml(summaryPanelFill)}\" fill-opacity=\"{summaryPanelFillOpacity}\" stroke=\"{EscapeXml(summaryPanelStroke)}\" stroke-opacity=\"{summaryPanelStrokeOpacity}\" />");
        sb.AppendLine($"  <text x=\"{cardX + 12}\" y=\"{summaryPanelY + 18}\" class=\"section\">Repository Signals</text>");

        AppendSummaryMetrics(
            sb,
            cardX,
            summaryPanelY + 22,
            cardWidth,
            summarySeparatorColor,
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
        string separatorColor,
        IReadOnlyList<(string Label, long Value)> metrics)
    {
        double columnWidth = width / (double)Math.Max(1, metrics.Count);
        for (int i = 0; i < metrics.Count; i++)
        {
            double centerX = x + (columnWidth * i) + (columnWidth / 2d);
            if (i > 0)
            {
                double separatorX = x + (columnWidth * i);
                sb.AppendLine($"  <line x1=\"{Format(separatorX)}\" y1=\"{y - 3}\" x2=\"{Format(separatorX)}\" y2=\"{y + 46}\" stroke=\"{EscapeXml(separatorColor)}\" stroke-opacity=\"0.45\" />");
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
        string separatorColor,
        IReadOnlyList<(string Label, long Value)> metrics)
    {
        double columnWidth = width / (double)Math.Max(1, metrics.Count);
        for (int i = 0; i < metrics.Count; i++)
        {
            double centerX = x + (columnWidth * i) + (columnWidth / 2d);
            if (i > 0)
            {
                double separatorX = x + (columnWidth * i);
                sb.AppendLine($"  <line x1=\"{Format(separatorX)}\" y1=\"{y - 5}\" x2=\"{Format(separatorX)}\" y2=\"{y + 32}\" stroke=\"{EscapeXml(separatorColor)}\" stroke-opacity=\"0.45\" />");
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

    private static string EnsureAaTextColor(string preferred, params string[] backgrounds)
    {
        List<Rgb> parsedBackgrounds = backgrounds
            .Select(color => TryParseHexColor(color, out Rgb parsed) ? parsed : (Rgb?)null)
            .Where(rgb => rgb.HasValue)
            .Select(rgb => rgb!.Value)
            .ToList();

        if (parsedBackgrounds.Count == 0 || !TryParseHexColor(preferred, out Rgb preferredRgb))
        {
            return preferred;
        }

        double preferredMinContrast = parsedBackgrounds
            .Min(background => GetContrastRatio(preferredRgb, background));
        if (preferredMinContrast >= MinimumAaContrastRatio)
        {
            return preferred;
        }

        Rgb light = ParseHexColorOrThrow(LightTextColor);
        Rgb dark = ParseHexColorOrThrow(DarkTextColor);
        double lightMinContrast = parsedBackgrounds.Min(background => GetContrastRatio(light, background));
        double darkMinContrast = parsedBackgrounds.Min(background => GetContrastRatio(dark, background));

        if (lightMinContrast >= MinimumAaContrastRatio || darkMinContrast >= MinimumAaContrastRatio)
        {
            return lightMinContrast >= darkMinContrast ? LightTextColor : DarkTextColor;
        }

        return lightMinContrast >= darkMinContrast ? LightTextColor : DarkTextColor;
    }

    private static string BlendColors(string baseColor, string overlayColor, double overlayAlpha)
    {
        if (!TryParseHexColor(baseColor, out Rgb baseRgb) ||
            !TryParseHexColor(overlayColor, out Rgb overlayRgb))
        {
            return overlayColor;
        }

        double alpha = Math.Clamp(overlayAlpha, 0d, 1d);
        byte r = (byte)Math.Round((overlayRgb.R * alpha) + (baseRgb.R * (1d - alpha)));
        byte g = (byte)Math.Round((overlayRgb.G * alpha) + (baseRgb.G * (1d - alpha)));
        byte b = (byte)Math.Round((overlayRgb.B * alpha) + (baseRgb.B * (1d - alpha)));
        return ToHex(new Rgb(r, g, b));
    }

    private static double ParseOpacity(string value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return Math.Clamp(parsed, 0d, 1d);
        }

        return 1d;
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

    private static string ToHex(Rgb rgb)
    {
        return $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}";
    }

    private static string EscapeXml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return SecurityElement.Escape(value) ?? string.Empty;
    }

    private readonly record struct Rgb(byte R, byte G, byte B);
}
