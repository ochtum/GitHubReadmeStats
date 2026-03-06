using System.Globalization;
using System.Security;
using System.Text;

namespace GitHubReadMeStats.Cli;

internal static class SvgRenderer
{
    private const string TrackColor = "#1F2937";
    private const string LightTextColor = "#F8FAFC";
    private const string DarkTextColor = "#0F172A";

    public static string Render(
        string viewerLogin,
        AggregationResult aggregation,
        int topCount,
        DateTimeOffset generatedAtUtc,
        TimeDisplaySettings timeDisplay,
        CardColorTheme? colorTheme = null)
    {
        IReadOnlyList<AggregatedLanguage> topLanguages = aggregation.Languages.Take(topCount).ToList();
        DateTimeOffset generatedAtLocal = TimeZoneInfo.ConvertTime(generatedAtUtc, timeDisplay.TimeZone);

        const int canvasWidth = 640;
        const int horizontalPadding = 24;
        const int titleY = 42;
        const int subtitleY = 66;
        const int barHeight = 20;
        const int rowGap = 8;
        const int listStartY = 82;
        const int footerGap = 14;
        const int footerBaselineOffset = 26;
        const int bottomPadding = 14;

        int rowHeight = barHeight + rowGap;
        int listHeight = topLanguages.Count == 0
            ? 0
            : (topLanguages.Count * rowHeight) - rowGap;
        int footerY = listStartY + listHeight + footerGap + footerBaselineOffset;
        int height = footerY + bottomPadding;

        int chartX = horizontalPadding;
        int chartWidth = canvasWidth - (horizontalPadding * 2);

        string backgroundStart = colorTheme?.BackgroundStart ?? "#0F172A";
        string backgroundEnd = colorTheme?.BackgroundEnd ?? "#111827";
        string borderColor = colorTheme?.Border ?? "#1E293B";
        string titleColor = colorTheme?.TitleText ?? "#F8FAFC";
        string subColor = colorTheme?.SecondaryText ?? "#CBD5E1";
        string mutedColor = colorTheme?.MutedText ?? "#94A3B8";
        string trackColor = colorTheme?.TrackFill ?? TrackColor;

        var sb = new StringBuilder();

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{canvasWidth}\" height=\"{height}\" viewBox=\"0 0 {canvasWidth} {height}\" role=\"img\" aria-label=\"GitHub readme language stats\">");
        sb.AppendLine("  <defs>");
        sb.AppendLine("    <linearGradient id=\"card-bg\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"1\">");
        sb.AppendLine($"      <stop offset=\"0%\" stop-color=\"{EscapeXml(backgroundStart)}\" />");
        sb.AppendLine($"      <stop offset=\"100%\" stop-color=\"{EscapeXml(backgroundEnd)}\" />");
        sb.AppendLine("    </linearGradient>");
        sb.AppendLine("    <style>");
        sb.AppendLine($"      .title {{ font: 700 24px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(titleColor)}; }}");
        sb.AppendLine($"      .sub {{ font: 500 14px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(subColor)}; }}");
        sb.AppendLine("      .label { font: 600 13px 'Segoe UI', Arial, sans-serif; }");
        sb.AppendLine("      .value { font: 600 12px 'Segoe UI', Arial, sans-serif; }");
        sb.AppendLine($"      .muted {{ font: 500 12px 'Segoe UI', Arial, sans-serif; fill: {EscapeXml(mutedColor)}; }}");
        sb.AppendLine("    </style>");
        sb.AppendLine("  </defs>");

        sb.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{canvasWidth}\" height=\"{height}\" rx=\"18\" fill=\"url(#card-bg)\" />");
        sb.AppendLine($"  <rect x=\"1\" y=\"1\" width=\"{canvasWidth - 2}\" height=\"{height - 2}\" rx=\"17\" fill=\"none\" stroke=\"{EscapeXml(borderColor)}\" />");

        sb.AppendLine($"  <text x=\"{horizontalPadding}\" y=\"{titleY}\" class=\"title\">Most Used Languages</text>");
        sb.AppendLine($"  <text x=\"{horizontalPadding}\" y=\"{subtitleY}\" class=\"sub\">@{EscapeXml(viewerLogin)} | {aggregation.IncludedRepositoryCount} repos | updated {generatedAtLocal:yyyy-MM-dd HH:mm} {EscapeXml(timeDisplay.Label)}</text>");

        for (int i = 0; i < topLanguages.Count; i++)
        {
            AggregatedLanguage language = topLanguages[i];
            int barY = listStartY + (i * rowHeight);
            int textY = barY + 14;

            double barWidth = chartWidth * (language.Percent / 100d);
            if (barWidth > 0 && barWidth < 4)
            {
                barWidth = 4;
            }

            sb.AppendLine($"  <rect x=\"{chartX}\" y=\"{barY}\" width=\"{chartWidth}\" height=\"{barHeight}\" rx=\"10\" fill=\"{trackColor}\" />");
            if (barWidth > 0)
            {
                sb.AppendLine($"  <rect x=\"{chartX}\" y=\"{barY}\" width=\"{barWidth.ToString("0.##", CultureInfo.InvariantCulture)}\" height=\"{barHeight}\" rx=\"10\" fill=\"{EscapeXml(language.Color)}\" />");
            }

            string labelBackgroundColor = barWidth >= 12 ? language.Color : trackColor;
            string valueBackgroundColor = barWidth >= (chartWidth - 8) ? language.Color : trackColor;
            string labelColor = GetAccessibleTextColor(labelBackgroundColor);
            string valueColor = GetAccessibleTextColor(valueBackgroundColor);

            sb.AppendLine($"  <text x=\"{chartX + 10}\" y=\"{textY + 1}\" class=\"label\" fill=\"{labelColor}\">{EscapeXml(language.Name)}</text>");
            sb.AppendLine($"  <text x=\"{chartX + chartWidth - 8}\" y=\"{textY + 1}\" class=\"value\" fill=\"{valueColor}\" text-anchor=\"end\">{language.Percent.ToString("0.00", CultureInfo.InvariantCulture)}%</text>");
        }

        sb.AppendLine($"  <text x=\"{horizontalPadding}\" y=\"{footerY}\" class=\"muted\">Generated by GitHubReadMeStats</text>");
        sb.AppendLine("</svg>");

        return sb.ToString();
    }

    public static string ToHumanReadableBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        string[] units = new[] { "KB", "MB", "GB", "TB" };
        double value = bytes;
        int unitIndex = -1;

        do
        {
            value /= 1024;
            unitIndex++;
        }
        while (value >= 1024 && unitIndex < units.Length - 1);

        return $"{value.ToString("0.##", CultureInfo.InvariantCulture)} {units[unitIndex]}";
    }

    private static string EscapeXml(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return SecurityElement.Escape(value) ?? string.Empty;
    }

    private static string GetAccessibleTextColor(string backgroundColor)
    {
        if (!TryParseColor(backgroundColor, out Rgb background))
        {
            return LightTextColor;
        }

        Rgb light = ParseHexColorOrThrow(LightTextColor);
        Rgb dark = ParseHexColorOrThrow(DarkTextColor);

        double lightContrast = GetContrastRatio(background, light);
        double darkContrast = GetContrastRatio(background, dark);
        return darkContrast >= lightContrast ? DarkTextColor : LightTextColor;
    }

    private static double GetContrastRatio(Rgb a, Rgb b)
    {
        double l1 = GetRelativeLuminance(a);
        double l2 = GetRelativeLuminance(b);
        double max = Math.Max(l1, l2);
        double min = Math.Min(l1, l2);
        return (max + 0.05) / (min + 0.05);
    }

    private static double GetRelativeLuminance(Rgb rgb)
    {
        static double ToLinear(double channel)
        {
            return channel <= 0.04045
                ? channel / 12.92
                : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

        double r = ToLinear(rgb.R / 255d);
        double g = ToLinear(rgb.G / 255d);
        double b = ToLinear(rgb.B / 255d);
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
    }

    private static bool TryParseColor(string color, out Rgb rgb)
    {
        string trimmed = color.Trim();
        if (TryParseHexColor(trimmed, out rgb))
        {
            return true;
        }

        if (TryParseRgbFunction(trimmed, out rgb))
        {
            return true;
        }

        if (TryParseHslFunction(trimmed, out rgb))
        {
            return true;
        }

        if (TryParseOklFunctionLightness(trimmed, out rgb))
        {
            return true;
        }

        rgb = default;
        return false;
    }

    private static bool TryParseHexColor(string color, out Rgb rgb)
    {
        rgb = default;
        if (!color.StartsWith('#'))
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

    private static bool TryParseRgbFunction(string color, out Rgb rgb)
    {
        rgb = default;
        if (!color.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        int start = color.IndexOf('(');
        int end = color.LastIndexOf(')');
        if (start < 0 || end <= start)
        {
            return false;
        }

        string args = color[(start + 1)..end];
        string[] components = args
            .Replace("/", " ", StringComparison.Ordinal)
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (components.Length < 3)
        {
            return false;
        }

        if (!TryParseRgbChannel(components[0], out byte r) ||
            !TryParseRgbChannel(components[1], out byte g) ||
            !TryParseRgbChannel(components[2], out byte b))
        {
            return false;
        }

        rgb = new Rgb(r, g, b);
        return true;
    }

    private static bool TryParseRgbChannel(string value, out byte channel)
    {
        channel = 0;
        string trimmed = value.Trim();
        if (trimmed.EndsWith('%'))
        {
            string number = trimmed[..^1];
            if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double percent))
            {
                return false;
            }

            percent = Math.Clamp(percent, 0, 100);
            channel = (byte)Math.Round((percent / 100d) * 255d);
            return true;
        }

        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double raw))
        {
            return false;
        }

        raw = Math.Clamp(raw, 0, 255);
        channel = (byte)Math.Round(raw);
        return true;
    }

    private static bool TryParseHslFunction(string color, out Rgb rgb)
    {
        rgb = default;
        if (!color.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        int start = color.IndexOf('(');
        int end = color.LastIndexOf(')');
        if (start < 0 || end <= start)
        {
            return false;
        }

        string args = color[(start + 1)..end];
        string[] components = args
            .Replace("/", " ", StringComparison.Ordinal)
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (components.Length < 3)
        {
            return false;
        }

        if (!double.TryParse(components[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double hue))
        {
            return false;
        }

        if (!TryParsePercentChannel(components[1], out double saturation) ||
            !TryParsePercentChannel(components[2], out double lightness))
        {
            return false;
        }

        rgb = HslToRgb(hue, saturation, lightness);
        return true;
    }

    private static bool TryParsePercentChannel(string value, out double normalized)
    {
        normalized = 0;
        string trimmed = value.Trim();
        bool isPercent = trimmed.EndsWith('%');
        string number = isPercent ? trimmed[..^1] : trimmed;

        if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return false;
        }

        normalized = isPercent ? parsed / 100d : parsed;
        normalized = Math.Clamp(normalized, 0d, 1d);
        return true;
    }

    private static Rgb HslToRgb(double hue, double saturation, double lightness)
    {
        hue %= 360d;
        if (hue < 0)
        {
            hue += 360d;
        }

        double c = (1 - Math.Abs((2 * lightness) - 1)) * saturation;
        double x = c * (1 - Math.Abs(((hue / 60d) % 2) - 1));
        double m = lightness - (c / 2);

        (double r1, double g1, double b1) = hue switch
        {
            < 60 => (c, x, 0d),
            < 120 => (x, c, 0d),
            < 180 => (0d, c, x),
            < 240 => (0d, x, c),
            < 300 => (x, 0d, c),
            _ => (c, 0d, x),
        };

        byte r = (byte)Math.Round(Math.Clamp((r1 + m) * 255d, 0, 255));
        byte g = (byte)Math.Round(Math.Clamp((g1 + m) * 255d, 0, 255));
        byte b = (byte)Math.Round(Math.Clamp((b1 + m) * 255d, 0, 255));
        return new Rgb(r, g, b);
    }

    private static bool TryParseOklFunctionLightness(string color, out Rgb rgb)
    {
        rgb = default;
        if (!color.StartsWith("oklch(", StringComparison.OrdinalIgnoreCase) &&
            !color.StartsWith("oklab(", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        int start = color.IndexOf('(');
        int end = color.LastIndexOf(')');
        if (start < 0 || end <= start)
        {
            return false;
        }

        string args = color[(start + 1)..end];
        string[] components = args
            .Replace("/", " ", StringComparison.Ordinal)
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (components.Length == 0)
        {
            return false;
        }

        if (!TryParsePercentChannel(components[0], out double lightness))
        {
            return false;
        }

        // Use Oklab/OKLCH L (perceptual lightness) to derive a grayscale approximation
        // for contrast decisions when exact conversion isn't needed.
        byte channel = (byte)Math.Round(Math.Clamp(Math.Pow(lightness, 1d / 2.2d) * 255d, 0, 255));
        rgb = new Rgb(channel, channel, channel);
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

    private readonly record struct Rgb(byte R, byte G, byte B);
}
