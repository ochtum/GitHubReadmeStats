using System.Text.Json.Serialization;

namespace GitHubReadMeStats.Cli;

internal sealed class GraphQlResponse<TData>
{
    [JsonPropertyName("data")]
    public TData? Data { get; init; }

    [JsonPropertyName("errors")]
    public List<GraphQlError>? Errors { get; init; }
}

internal sealed class GraphQlError
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

internal sealed class ViewerData
{
    [JsonPropertyName("viewer")]
    public ViewerNode? Viewer { get; init; }
}

internal sealed class ViewerNode
{
    [JsonPropertyName("login")]
    public string? Login { get; init; }

    [JsonPropertyName("repositories")]
    public RepositoryConnection? Repositories { get; init; }
}

internal sealed class UserLookupData
{
    [JsonPropertyName("user")]
    public UserNode? User { get; init; }
}

internal sealed class UserNode
{
    [JsonPropertyName("login")]
    public string? Login { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("followers")]
    public TotalCountNode? Followers { get; init; }

    [JsonPropertyName("repositories")]
    public TotalCountNode? Repositories { get; init; }

    [JsonPropertyName("contributionsCollection")]
    public ContributionsCollectionNode? ContributionsCollection { get; init; }
}

internal sealed class TotalCountNode
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }
}

internal sealed class ContributionsCollectionNode
{
    [JsonPropertyName("contributionCalendar")]
    public ContributionCalendarNode? ContributionCalendar { get; init; }
}

internal sealed class ContributionCalendarNode
{
    [JsonPropertyName("totalContributions")]
    public int TotalContributions { get; init; }
}

internal sealed class RepositoryConnection
{
    [JsonPropertyName("pageInfo")]
    public PageInfo? PageInfo { get; init; }

    [JsonPropertyName("nodes")]
    public List<RepositoryNode?>? Nodes { get; init; }
}

internal sealed class PageInfo
{
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; init; }

    [JsonPropertyName("endCursor")]
    public string? EndCursor { get; init; }
}

internal sealed class RepositoryNode
{
    [JsonPropertyName("nameWithOwner")]
    public string NameWithOwner { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("isPrivate")]
    public bool IsPrivate { get; init; }

    [JsonPropertyName("isFork")]
    public bool IsFork { get; init; }

    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; init; }

    [JsonPropertyName("stargazerCount")]
    public int StargazerCount { get; init; }

    [JsonPropertyName("forkCount")]
    public int ForkCount { get; init; }

    [JsonPropertyName("primaryLanguage")]
    public LanguageNode? PrimaryLanguage { get; init; }

    [JsonPropertyName("languages")]
    public LanguagesConnection? Languages { get; init; }
}

internal sealed class RepositoryLookupData
{
    [JsonPropertyName("repository")]
    public RepositoryNode? Repository { get; init; }
}

internal sealed class LanguagesConnection
{
    [JsonPropertyName("edges")]
    public List<LanguageEdge?>? Edges { get; init; }
}

internal sealed class LanguageEdge
{
    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("node")]
    public LanguageNode? Node { get; init; }
}

internal sealed class LanguageNode
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("color")]
    public string? Color { get; init; }
}

internal sealed record ViewerRepositoriesResult(string ViewerLogin, IReadOnlyList<RepositoryNode> Repositories);
internal sealed record AggregatedLanguage(string Name, long Size, string Color, double Percent);
internal sealed record UserSummary(
    string Login,
    string DisplayName,
    int Followers,
    int PublicRepositories,
    int ContributionsThisYear,
    DateTimeOffset CreatedAt);

internal sealed record PinRepository(string Owner, string Name);

internal sealed record PinCardData(
    string Owner,
    string Name,
    string Url,
    string Description,
    int Stars,
    int Forks,
    string PrimaryLanguage,
    string PrimaryLanguageColor,
    bool IsPrivate,
    bool IsArchived);

internal sealed record CardsConfig(string Username, IReadOnlyList<PinRepository> Repositories);

internal sealed record AggregationResult(
    IReadOnlyList<AggregatedLanguage> Languages,
    long TotalBytes,
    int ScannedRepositoryCount,
    int IncludedRepositoryCount);
