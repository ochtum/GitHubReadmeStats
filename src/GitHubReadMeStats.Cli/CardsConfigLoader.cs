using System.Text.Json;

namespace GitHubReadMeStats.Cli;

internal static class CardsConfigLoader
{
    public static CardsConfig Load(string path, string defaultUsername)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Cards config file not found: {path}");
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;

        string username = ReadUsername(root, defaultUsername);
        IReadOnlyList<PinRepository> repositories = ReadRepositories(root);
        IReadOnlyDictionary<string, string> languageColorOverrides = ReadLanguageColorOverrides(root);
        IReadOnlyDictionary<string, string> languageIconOverrides = ReadLanguageIconOverrides(root);
        string? mainColor = ReadMainColor(root);
        string? theme = ReadTheme(root);
        string? displayTimeZone = ReadDisplayTimeZone(root);
        string? displayTimeZoneLabel = ReadDisplayTimeZoneLabel(root);

        return new CardsConfig(
            username,
            repositories,
            languageColorOverrides,
            languageIconOverrides,
            mainColor,
            theme,
            displayTimeZone,
            displayTimeZoneLabel);
    }

    private static string ReadUsername(JsonElement root, string defaultUsername)
    {
        if (root.TryGetProperty("username", out JsonElement usernameElement) &&
            usernameElement.ValueKind == JsonValueKind.String)
        {
            string? configuredUsername = usernameElement.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(configuredUsername))
            {
                return configuredUsername;
            }
        }

        return defaultUsername;
    }

    private static IReadOnlyList<PinRepository> ReadRepositories(JsonElement root)
    {
        if (!root.TryGetProperty("repositories", out JsonElement repositoriesElement) ||
            repositoriesElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<PinRepository>();
        }

        var result = new List<PinRepository>();
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement element in repositoriesElement.EnumerateArray())
        {
            PinRepository? repository = ParseRepository(element);
            if (repository is null)
            {
                continue;
            }

            string key = $"{repository.Owner}/{repository.Name}";
            if (dedupe.Add(key))
            {
                result.Add(repository);
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string> ReadLanguageColorOverrides(JsonElement root)
    {
        if (!root.TryGetProperty("languageColors", out JsonElement colorsElement) ||
            colorsElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty property in colorsElement.EnumerateObject())
        {
            string key = property.Name.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string? value = property.Value.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            result[key] = value;
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string> ReadLanguageIconOverrides(JsonElement root)
    {
        if (!root.TryGetProperty("languageIcons", out JsonElement iconsElement) ||
            iconsElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty property in iconsElement.EnumerateObject())
        {
            string key = property.Name.Trim();
            if (string.IsNullOrWhiteSpace(key) || property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string? value = property.Value.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            result[key] = value;
        }

        return result;
    }

    private static string? ReadDisplayTimeZone(JsonElement root)
    {
        return NormalizeNullable(
            ReadStringProperty(root, "displayTimeZone")
            ?? ReadStringProperty(root, "displayTimezone")
            ?? ReadStringProperty(root, "timeZone")
            ?? ReadStringProperty(root, "timezone"));
    }

    private static string? ReadMainColor(JsonElement root)
    {
        return NormalizeNullable(
            ReadStringProperty(root, "mainColor")
            ?? ReadStringProperty(root, "main_color")
            ?? ReadStringProperty(root, "main-color"));
    }

    private static string? ReadTheme(JsonElement root)
    {
        return NormalizeNullable(
            ReadStringProperty(root, "theme")
            ?? ReadStringProperty(root, "cardTheme")
            ?? ReadStringProperty(root, "card_theme")
            ?? ReadStringProperty(root, "card-theme"));
    }

    private static string? ReadDisplayTimeZoneLabel(JsonElement root)
    {
        return NormalizeNullable(
            ReadStringProperty(root, "displayTimeZoneLabel")
            ?? ReadStringProperty(root, "displayTimezoneLabel")
            ?? ReadStringProperty(root, "timeZoneLabel")
            ?? ReadStringProperty(root, "timezoneLabel"));
    }

    private static PinRepository? ParseRepository(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return ParseOwnerAndName(element.GetString());
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            string? owner = null;
            string? name = null;
            string? languageColorOverride = null;
            string? languageIconOverride = null;
            string? icon = null;

            if (element.TryGetProperty("owner", out JsonElement ownerElement) && ownerElement.ValueKind == JsonValueKind.String)
            {
                owner = ownerElement.GetString();
            }

            if (element.TryGetProperty("name", out JsonElement nameElement) && nameElement.ValueKind == JsonValueKind.String)
            {
                name = nameElement.GetString();
            }
            else if (element.TryGetProperty("repo", out JsonElement repoElement) && repoElement.ValueKind == JsonValueKind.String)
            {
                name = repoElement.GetString();
            }

            languageColorOverride = ReadStringProperty(element, "languageColor")
                ?? ReadStringProperty(element, "language_color")
                ?? ReadStringProperty(element, "language-color");

            languageIconOverride = ReadStringProperty(element, "languageIcon")
                ?? ReadStringProperty(element, "language_icon")
                ?? ReadStringProperty(element, "language-icon")
                ?? ReadStringProperty(element, "languageIconPath")
                ?? ReadStringProperty(element, "language_icon_path")
                ?? ReadStringProperty(element, "language-icon-path");

            icon = ReadStringProperty(element, "icon")
                ?? ReadStringProperty(element, "iconPath")
                ?? ReadStringProperty(element, "icon_path")
                ?? ReadStringProperty(element, "icon-path");

            if (string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(name) && name!.Contains('/'))
            {
                PinRepository? parsed = ParseOwnerAndName(name);
                if (parsed is null)
                {
                    return null;
                }

                return parsed with
                {
                    LanguageColorOverride = NormalizeNullable(languageColorOverride),
                    LanguageIconOverride = NormalizeNullable(languageIconOverride),
                    Icon = NormalizeNullable(icon),
                };
            }

            if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(name))
            {
                return new PinRepository(
                    owner.Trim(),
                    name.Trim(),
                    NormalizeNullable(languageColorOverride),
                    NormalizeNullable(languageIconOverride),
                    NormalizeNullable(icon));
            }
        }

        return null;
    }

    private static PinRepository? ParseOwnerAndName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string[] segments = value
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length != 2)
        {
            return null;
        }

        return new PinRepository(segments[0], segments[1], null, null, null);
    }

    private static string? ReadStringProperty(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement element) || element.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return element.GetString();
    }

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
