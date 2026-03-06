using System.Globalization;

namespace GitHubReadMeStats.Cli;

internal sealed record StatusBadgePalette(string Fill, string Stroke, string Text);

internal sealed record CardColorTheme(
    string MainColorCss,
    string BackgroundStart,
    string BackgroundEnd,
    string Border,
    string TitleText,
    string PrimaryText,
    string SecondaryText,
    string TertiaryText,
    string MutedText,
    string Accent,
    string AccentStrong,
    string AccentSoft,
    string PanelFill,
    string PanelFillAlt,
    string PanelStroke,
    string PanelStrokeSoft,
    string ChartGrid,
    string ChartLine,
    string TrackFill,
    string IconFrameFill,
    string IconImageBackground,
    string IconStroke,
    string IconDot,
    IReadOnlyList<string> MetricDotPalette,
    StatusBadgePalette PublicBadge,
    StatusBadgePalette PrivateBadge,
    StatusBadgePalette ArchivedBadge);

internal static class CardColorThemeFactory
{
    public const string DefaultMainColor = "#0C1830";
    public const string DefaultThemeName = "indigo-night";
    private const string NeonNightThemeName = "neon-night";

    private static readonly string[] SupportedThemeNames =
    [
        DefaultThemeName,
        "cobalt",
        "ocean",
        "teal",
        "emerald",
        "amber",
        "coral",
        "violet",
        "graphite",
        "sakura",
        "rose-petal",
        "lavender-mist",
        "peach-cream",
        "mint-bloom",
        NeonNightThemeName,
    ];

    private static readonly IReadOnlyDictionary<string, string> ThemeMainColors =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [DefaultThemeName] = DefaultMainColor,
            ["cobalt"] = "#1D4ED8",
            ["ocean"] = "#0369A1",
            ["teal"] = "#0F766E",
            ["emerald"] = "#166534",
            ["amber"] = "#92400E",
            ["coral"] = "#BE123C",
            ["violet"] = "#6D28D9",
            ["graphite"] = "#1F2937",
            ["sakura"] = "#F472B6",
            ["rose-petal"] = "#9F1239",
            ["lavender-mist"] = "#A855F7",
            ["peach-cream"] = "#EA580C",
            ["mint-bloom"] = "#10B981",
        };

    private static readonly IReadOnlyDictionary<string, string> ThemeAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = DefaultThemeName,
            ["classic"] = NeonNightThemeName,
        };

    private static readonly Rgb LightTextRgb = new(248, 250, 252);
    private static readonly Rgb DarkTextRgb = new(15, 23, 42);

    public static IReadOnlyList<string> BuiltInThemeNames => SupportedThemeNames;

    public static bool TryResolveTheme(
        string? themeName,
        out string normalizedThemeName,
        out string? mainColor,
        out bool useClassicFallbackPalette)
    {
        normalizedThemeName = string.Empty;
        mainColor = null;
        useClassicFallbackPalette = false;

        if (string.IsNullOrWhiteSpace(themeName))
        {
            return false;
        }

        string candidate = themeName.Trim();
        if (ThemeAliases.TryGetValue(candidate, out string? alias) && !string.IsNullOrWhiteSpace(alias))
        {
            candidate = alias;
        }

        if (!TryNormalizeThemeName(candidate, out normalizedThemeName))
        {
            return false;
        }

        if (normalizedThemeName.Equals(NeonNightThemeName, StringComparison.OrdinalIgnoreCase))
        {
            useClassicFallbackPalette = true;
            return true;
        }

        mainColor = ThemeMainColors[normalizedThemeName];
        return true;
    }

    public static CardColorTheme Create(string mainColor)
    {
        if (!TryNormalizeAndParseMainColor(mainColor, out string normalizedMainColor, out Rgb baseRgb))
        {
            throw new InvalidOperationException("Invalid cards-config mainColor. Use hex (#RGB/#RRGGBB/#RRGGBBAA) or oklch(...).");
        }

        Rgb backgroundEnd = Mix(baseRgb, DarkTextRgb, 0.22d);
        Rgb backgroundMid = Mix(baseRgb, backgroundEnd, 0.5d);

        bool useLightText = GetContrastRatio(backgroundMid, LightTextRgb) >= GetContrastRatio(backgroundMid, DarkTextRgb);
        Rgb primaryTextRgb = useLightText ? LightTextRgb : DarkTextRgb;

        Rgb secondaryText = Mix(primaryTextRgb, backgroundMid, 0.28d);
        Rgb tertiaryText = Mix(primaryTextRgb, backgroundMid, 0.42d);
        Rgb mutedText = Mix(primaryTextRgb, backgroundMid, 0.58d);

        Rgb accentSeed = EnsureContrast(baseRgb, backgroundMid, 2.2d);
        Rgb accent = EnsureContrast(ShiftLightness(accentSeed, useLightText ? 0.16d : -0.18d), backgroundMid, 2.8d);
        Rgb accentStrong = EnsureContrast(
            ShiftSaturation(ShiftLightness(accentSeed, useLightText ? 0.26d : -0.28d), 0.10d),
            backgroundMid,
            3.2d);
        Rgb accentSoft = EnsureContrast(
            ShiftSaturation(ShiftLightness(accentSeed, useLightText ? 0.08d : -0.10d), -0.12d),
            backgroundMid,
            2.2d);

        Rgb panelFill = Mix(backgroundMid, primaryTextRgb, useLightText ? 0.12d : 0.08d);
        Rgb panelFillAlt = Mix(backgroundMid, primaryTextRgb, useLightText ? 0.18d : 0.13d);
        Rgb panelStroke = Mix(accentStrong, backgroundMid, 0.18d);
        Rgb panelStrokeSoft = Mix(panelStroke, backgroundMid, 0.42d);
        Rgb chartGrid = Mix(panelStroke, backgroundMid, 0.62d);
        Rgb trackFill = Mix(backgroundMid, primaryTextRgb, useLightText ? 0.24d : 0.15d);

        IReadOnlyList<string> metricDots = BuildMetricDotPalette(accentStrong, backgroundMid);

        StatusBadgePalette publicBadge = BuildStatusBadge(accentStrong, backgroundMid);
        StatusBadgePalette privateBadge = BuildStatusBadge(ShiftHue(accentStrong, 34d), backgroundMid);
        StatusBadgePalette archivedBadge = BuildStatusBadge(
            ShiftSaturation(ShiftHue(accentStrong, -110d), -0.60d),
            backgroundMid);

        return new CardColorTheme(
            normalizedMainColor,
            normalizedMainColor,
            ToHex(backgroundEnd),
            ToHex(Mix(panelStroke, backgroundMid, 0.30d)),
            ToHex(accentStrong),
            ToHex(primaryTextRgb),
            ToHex(secondaryText),
            ToHex(tertiaryText),
            ToHex(mutedText),
            ToHex(accent),
            ToHex(accentStrong),
            ToHex(accentSoft),
            ToHex(panelFill),
            ToHex(panelFillAlt),
            ToHex(panelStroke),
            ToHex(panelStrokeSoft),
            ToHex(chartGrid),
            ToHex(accentStrong),
            ToHex(trackFill),
            ToHex(panelFillAlt),
            ToHex(Mix(panelFillAlt, backgroundMid, 0.40d)),
            ToHex(accentSoft),
            ToHex(accentStrong),
            metricDots,
            publicBadge,
            privateBadge,
            archivedBadge);
    }

    private static bool TryNormalizeThemeName(string value, out string normalizedThemeName)
    {
        foreach (string supportedTheme in SupportedThemeNames)
        {
            if (supportedTheme.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                normalizedThemeName = supportedTheme;
                return true;
            }
        }

        normalizedThemeName = string.Empty;
        return false;
    }

    private static StatusBadgePalette BuildStatusBadge(Rgb seed, Rgb background)
    {
        Rgb fill = Mix(seed, background, 0.45d);
        Rgb stroke = EnsureContrast(seed, fill, 2.4d);
        Rgb text = GetPreferredTextColor(fill);
        return new StatusBadgePalette(ToHex(fill), ToHex(stroke), ToHex(text));
    }

    private static IReadOnlyList<string> BuildMetricDotPalette(Rgb seed, Rgb background)
    {
        double[] hueShifts = [0d, 22d, -22d, 46d, -46d, 72d];
        var colors = new List<string>(hueShifts.Length);
        foreach (double shift in hueShifts)
        {
            Rgb shifted = ShiftHue(seed, shift);
            Rgb contrasted = EnsureContrast(shifted, background, 1.8d);
            colors.Add(ToHex(contrasted));
        }

        return colors;
    }

    private static bool TryNormalizeAndParseMainColor(string? value, out string normalized, out Rgb rgb)
    {
        normalized = string.Empty;
        rgb = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (TryParseHexColor(trimmed, out rgb))
        {
            normalized = ToHex(rgb);
            return true;
        }

        if (TryParseOklch(trimmed, out Oklch oklch))
        {
            normalized = FormatOklch(oklch);
            rgb = OklchToRgb(oklch);
            return true;
        }

        return false;
    }

    private static string FormatOklch(Oklch color)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"oklch({color.L:0.###} {color.C:0.###} {color.H:0.###})");
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

    private static bool TryParseOklch(string color, out Oklch result)
    {
        result = default;

        if (!color.StartsWith("oklch(", StringComparison.OrdinalIgnoreCase) || !color.EndsWith(')'))
        {
            return false;
        }

        string inner = color[6..^1].Trim();
        if (string.IsNullOrWhiteSpace(inner) || inner.Length > 96)
        {
            return false;
        }

        foreach (char ch in inner)
        {
            bool allowed = char.IsDigit(ch) ||
                           char.IsWhiteSpace(ch) ||
                           ch is '.' or ',' or '%' or '/' or '+' or '-' ||
                           char.IsLetter(ch);
            if (!allowed)
            {
                return false;
            }
        }

        string[] alphaSplit = inner.Split('/', 2, StringSplitOptions.TrimEntries);
        string valuePart = alphaSplit[0].Replace(',', ' ');
        string[] tokens = valuePart.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length < 3)
        {
            return false;
        }

        if (!TryParseLightness(tokens[0], out double l) ||
            !TryParseChroma(tokens[1], out double c) ||
            !TryParseAngle(tokens[2], out double h))
        {
            return false;
        }

        if (alphaSplit.Length == 2 && !TryParseAlpha(alphaSplit[1], out _))
        {
            return false;
        }

        result = new Oklch(l, c, h);
        return true;
    }

    private static bool TryParseLightness(string token, out double value)
    {
        value = 0d;
        string trimmed = token.Trim();

        bool isPercent = trimmed.EndsWith('%');
        string numeric = isPercent ? trimmed[..^1] : trimmed;
        if (!double.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return false;
        }

        if (isPercent || parsed > 1d)
        {
            parsed /= 100d;
        }

        value = Math.Clamp(parsed, 0d, 1d);
        return true;
    }

    private static bool TryParseChroma(string token, out double value)
    {
        value = 0d;
        string trimmed = token.Trim();

        bool isPercent = trimmed.EndsWith('%');
        string numeric = isPercent ? trimmed[..^1] : trimmed;
        if (!double.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return false;
        }

        if (isPercent)
        {
            parsed /= 100d;
        }

        value = Math.Clamp(parsed, 0d, 1d);
        return true;
    }

    private static bool TryParseAngle(string token, out double degrees)
    {
        degrees = 0d;
        string trimmed = token.Trim().ToLowerInvariant();

        double factor = 1d;
        string numeric = trimmed;
        if (trimmed.EndsWith("deg", StringComparison.Ordinal))
        {
            numeric = trimmed[..^3];
            factor = 1d;
        }
        else if (trimmed.EndsWith("grad", StringComparison.Ordinal))
        {
            numeric = trimmed[..^4];
            factor = 0.9d;
        }
        else if (trimmed.EndsWith("rad", StringComparison.Ordinal))
        {
            numeric = trimmed[..^3];
            factor = 180d / Math.PI;
        }
        else if (trimmed.EndsWith("turn", StringComparison.Ordinal))
        {
            numeric = trimmed[..^4];
            factor = 360d;
        }

        if (!double.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return false;
        }

        degrees = parsed * factor;
        degrees %= 360d;
        if (degrees < 0d)
        {
            degrees += 360d;
        }

        return true;
    }

    private static bool TryParseAlpha(string token, out double value)
    {
        value = 1d;
        string trimmed = token.Trim();
        bool isPercent = trimmed.EndsWith('%');
        string numeric = isPercent ? trimmed[..^1] : trimmed;
        if (!double.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return false;
        }

        if (isPercent)
        {
            parsed /= 100d;
        }

        value = Math.Clamp(parsed, 0d, 1d);
        return true;
    }

    private static Rgb OklchToRgb(Oklch color)
    {
        double hRad = color.H * (Math.PI / 180d);
        double a = color.C * Math.Cos(hRad);
        double b = color.C * Math.Sin(hRad);

        double lPrime = color.L + (0.3963377774d * a) + (0.2158037573d * b);
        double mPrime = color.L - (0.1055613458d * a) - (0.0638541728d * b);
        double sPrime = color.L - (0.0894841775d * a) - (1.2914855480d * b);

        double l = lPrime * lPrime * lPrime;
        double m = mPrime * mPrime * mPrime;
        double s = sPrime * sPrime * sPrime;

        double rLinear = (+4.0767416621d * l) + (-3.3077115913d * m) + (0.2309699292d * s);
        double gLinear = (-1.2684380046d * l) + (2.6097574011d * m) + (-0.3413193965d * s);
        double bLinear = (-0.0041960863d * l) + (-0.7034186147d * m) + (1.7076147010d * s);

        byte r = LinearToSrgbByte(rLinear);
        byte g = LinearToSrgbByte(gLinear);
        byte bChannel = LinearToSrgbByte(bLinear);
        return new Rgb(r, g, bChannel);
    }

    private static byte LinearToSrgbByte(double value)
    {
        double clamped = Math.Clamp(value, 0d, 1d);
        double srgb = clamped <= 0.0031308d
            ? clamped * 12.92d
            : (1.055d * Math.Pow(clamped, 1d / 2.4d)) - 0.055d;

        return (byte)Math.Round(Math.Clamp(srgb * 255d, 0d, 255d));
    }

    private static Rgb EnsureContrast(Rgb candidate, Rgb background, double minContrast)
    {
        if (GetContrastRatio(candidate, background) >= minContrast)
        {
            return candidate;
        }

        bool shouldLighten = GetRelativeLuminance(background) < 0.5d;
        Hsl hsl = ToHsl(candidate);
        for (int i = 0; i < 28; i++)
        {
            hsl = hsl with
            {
                L = Math.Clamp(hsl.L + (shouldLighten ? 0.03d : -0.03d), 0d, 1d),
            };

            Rgb adjusted = FromHsl(hsl);
            if (GetContrastRatio(adjusted, background) >= minContrast)
            {
                return adjusted;
            }
        }

        return GetPreferredTextColor(background);
    }

    private static Rgb GetPreferredTextColor(Rgb background)
    {
        return GetContrastRatio(background, LightTextRgb) >= GetContrastRatio(background, DarkTextRgb)
            ? LightTextRgb
            : DarkTextRgb;
    }

    private static Rgb ShiftLightness(Rgb rgb, double delta)
    {
        Hsl hsl = ToHsl(rgb);
        hsl = hsl with { L = Math.Clamp(hsl.L + delta, 0d, 1d) };
        return FromHsl(hsl);
    }

    private static Rgb ShiftSaturation(Rgb rgb, double delta)
    {
        Hsl hsl = ToHsl(rgb);
        hsl = hsl with { S = Math.Clamp(hsl.S + delta, 0d, 1d) };
        return FromHsl(hsl);
    }

    private static Rgb ShiftHue(Rgb rgb, double degrees)
    {
        Hsl hsl = ToHsl(rgb);
        double hue = (hsl.H + degrees) % 360d;
        if (hue < 0d)
        {
            hue += 360d;
        }

        hsl = hsl with { H = hue };
        return FromHsl(hsl);
    }

    private static Hsl ToHsl(Rgb rgb)
    {
        double r = rgb.R / 255d;
        double g = rgb.G / 255d;
        double b = rgb.B / 255d;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double hue;
        if (delta == 0d)
        {
            hue = 0d;
        }
        else if (max == r)
        {
            hue = 60d * (((g - b) / delta) % 6d);
        }
        else if (max == g)
        {
            hue = 60d * (((b - r) / delta) + 2d);
        }
        else
        {
            hue = 60d * (((r - g) / delta) + 4d);
        }

        if (hue < 0d)
        {
            hue += 360d;
        }

        double lightness = (max + min) / 2d;
        double saturation = delta == 0d
            ? 0d
            : delta / (1d - Math.Abs((2d * lightness) - 1d));

        return new Hsl(hue, saturation, lightness);
    }

    private static Rgb FromHsl(Hsl hsl)
    {
        double h = hsl.H;
        double s = hsl.S;
        double l = hsl.L;

        double c = (1d - Math.Abs((2d * l) - 1d)) * s;
        double x = c * (1d - Math.Abs(((h / 60d) % 2d) - 1d));
        double m = l - (c / 2d);

        (double r1, double g1, double b1) = h switch
        {
            < 60d => (c, x, 0d),
            < 120d => (x, c, 0d),
            < 180d => (0d, c, x),
            < 240d => (0d, x, c),
            < 300d => (x, 0d, c),
            _ => (c, 0d, x),
        };

        byte r = (byte)Math.Round(Math.Clamp((r1 + m) * 255d, 0d, 255d));
        byte g = (byte)Math.Round(Math.Clamp((g1 + m) * 255d, 0d, 255d));
        byte b = (byte)Math.Round(Math.Clamp((b1 + m) * 255d, 0d, 255d));
        return new Rgb(r, g, b);
    }

    private static Rgb Mix(Rgb from, Rgb to, double amount)
    {
        double t = Math.Clamp(amount, 0d, 1d);
        byte r = (byte)Math.Round(from.R + ((to.R - from.R) * t));
        byte g = (byte)Math.Round(from.G + ((to.G - from.G) * t));
        byte b = (byte)Math.Round(from.B + ((to.B - from.B) * t));
        return new Rgb(r, g, b);
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
        return string.Create(CultureInfo.InvariantCulture, $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}");
    }

    private readonly record struct Rgb(byte R, byte G, byte B);

    private readonly record struct Hsl(double H, double S, double L);

    private readonly record struct Oklch(double L, double C, double H);
}
