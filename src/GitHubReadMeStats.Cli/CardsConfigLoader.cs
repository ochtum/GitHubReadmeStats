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

        return new CardsConfig(username, repositories);
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

            if (string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(name) && name!.Contains('/'))
            {
                return ParseOwnerAndName(name);
            }

            if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(name))
            {
                return new PinRepository(owner.Trim(), name.Trim());
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

        return new PinRepository(segments[0], segments[1]);
    }
}
