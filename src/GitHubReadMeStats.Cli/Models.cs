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

internal sealed class UserStarsLookupData
{
    [JsonPropertyName("user")]
    public UserStarsNode? User { get; init; }
}

internal sealed class UserStarsNode
{
    [JsonPropertyName("repositories")]
    public RepositoryConnection? Repositories { get; init; }
}

internal sealed class UserNode
{
    [JsonPropertyName("login")]
    public string? Login { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("followers")]
    public TotalCountNode? Followers { get; init; }

    [JsonPropertyName("repositories")]
    public TotalCountNode? Repositories { get; init; }

    [JsonPropertyName("privateRepositories")]
    public TotalCountNode? PrivateRepositories { get; init; }

    [JsonPropertyName("forkRepositories")]
    public TotalCountNode? ForkRepositories { get; init; }

    [JsonPropertyName("pullRequests")]
    public TotalCountNode? PullRequests { get; init; }

    [JsonPropertyName("openIssues")]
    public TotalCountNode? OpenIssues { get; init; }

    [JsonPropertyName("closedIssues")]
    public TotalCountNode? ClosedIssues { get; init; }

    [JsonPropertyName("repositoriesContributedTo")]
    public TotalCountNode? RepositoriesContributedTo { get; init; }

    [JsonPropertyName("reviews")]
    public ReviewContributionsNode? Reviews { get; init; }

    [JsonPropertyName("commits")]
    public CommitContributionsNode? Commits { get; init; }

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
    [JsonPropertyName("totalCommitContributions")]
    public int TotalCommitContributions { get; init; }

    [JsonPropertyName("totalIssueContributions")]
    public int TotalIssueContributions { get; init; }

    [JsonPropertyName("totalPullRequestContributions")]
    public int TotalPullRequestContributions { get; init; }

    [JsonPropertyName("totalPullRequestReviewContributions")]
    public int TotalPullRequestReviewContributions { get; init; }

    [JsonPropertyName("totalRepositoriesWithContributedCommits")]
    public int TotalRepositoriesWithContributedCommits { get; init; }

    [JsonPropertyName("contributionCalendar")]
    public ContributionCalendarNode? ContributionCalendar { get; init; }
}

internal sealed class ReviewContributionsNode
{
    [JsonPropertyName("totalPullRequestReviewContributions")]
    public int TotalPullRequestReviewContributions { get; init; }
}

internal sealed class CommitContributionsNode
{
    [JsonPropertyName("totalCommitContributions")]
    public int TotalCommitContributions { get; init; }
}

internal sealed class ContributionCalendarNode
{
    [JsonPropertyName("totalContributions")]
    public int TotalContributions { get; init; }

    [JsonPropertyName("weeks")]
    public List<ContributionWeekNode>? Weeks { get; init; }
}

internal sealed class ContributionWeekNode
{
    [JsonPropertyName("contributionDays")]
    public List<ContributionDayNode>? ContributionDays { get; init; }
}

internal sealed class ContributionDayNode
{
    [JsonPropertyName("date")]
    public string? Date { get; init; }

    [JsonPropertyName("contributionCount")]
    public int ContributionCount { get; init; }
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

    [JsonPropertyName("watchers")]
    public TotalCountNode? Watchers { get; init; }

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
    string? Location,
    int Followers,
    int PublicRepositories,
    int PrivateRepositories,
    int ForkedRepositories,
    int TotalStarsEarned,
    int TotalCommitsLastYear,
    int TotalPullRequestsLastYear,
    int TotalIssuesLastYear,
    int TotalReviewsLastYear,
    int ContributedToRepositoriesLastYear,
    int ContributionsThisYear,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ContributionDaySummary> ContributionDays);

internal sealed record ContributionDaySummary(DateOnly Date, int ContributionCount);

internal sealed record PinRepository(
    string Owner,
    string Name,
    string? LanguageColorOverride,
    string? LanguageIconOverride,
    string? Icon);

internal sealed record PinCardData(
    string Owner,
    string Name,
    string Url,
    string Description,
    int Stars,
    int Forks,
    string PrimaryLanguage,
    string PrimaryLanguageColor,
    string? LanguageIconHref,
    bool IsPrivate,
    bool IsArchived,
    RepositoryTrafficTotals? TrafficTotals,
    string? RepositoryIconHref);

internal sealed record CardsConfig(
    string Username,
    IReadOnlyList<PinRepository> Repositories,
    IReadOnlyDictionary<string, string> LanguageColorOverrides,
    IReadOnlyDictionary<string, string> LanguageIconOverrides,
    string? MainColor,
    string? Theme,
    string? DisplayTimeZone,
    string? DisplayTimeZoneLabel);

internal sealed record TimeDisplaySettings(TimeZoneInfo TimeZone, string Label);

internal sealed record AggregationResult(
    IReadOnlyList<AggregatedLanguage> Languages,
    long TotalBytes,
    int ScannedRepositoryCount,
    int IncludedRepositoryCount);

internal sealed class RestTrafficResponse
{
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("uniques")]
    public int Uniques { get; init; }

    [JsonPropertyName("clones")]
    public List<RestTrafficPoint>? Clones { get; init; }

    [JsonPropertyName("views")]
    public List<RestTrafficPoint>? Views { get; init; }
}

internal sealed class RestTrafficPoint
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("uniques")]
    public int Uniques { get; init; }
}

internal sealed record TrafficDayPoint(DateOnly Date, int Count, int Uniques);

internal sealed record RepositoryTrafficSnapshot(
    int CloneCount,
    int UniqueCloners,
    int ViewCount,
    int UniqueVisitors,
    IReadOnlyList<TrafficDayPoint> CloneDays,
    IReadOnlyList<TrafficDayPoint> ViewDays);

internal sealed record RepositoryTrafficTotals(
    long CloneCountTotal,
    long UniqueClonersTotal,
    long ViewCountTotal,
    long UniqueVisitorsTotal,
    DateOnly SinceDate,
    DateOnly LastRecordedDate,
    bool UpdatedThisRun);

internal sealed record PublicRepositoriesTotalsCardData(
    string ViewerLogin,
    int PublicRepositoryCount,
    long TotalForks,
    long TotalWatchers,
    long TotalStarred,
    long TotalCloneCount,
    long TotalUniqueCloners,
    long TotalViewCount,
    long TotalUniqueVisitors,
    int TrafficAvailableRepositoryCount,
    int TrafficUnavailableRepositoryCount,
    DateOnly? TrafficSinceDate,
    DateOnly? TrafficLastRecordedDate);

internal sealed class TrafficHistoryState
{
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    [JsonPropertyName("repositories")]
    public Dictionary<string, TrafficHistoryRepositoryState> Repositories { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class TrafficHistoryRepositoryState
{
    [JsonPropertyName("cloneDays")]
    public Dictionary<string, TrafficHistoryDayValue> CloneDays { get; init; } = new(StringComparer.Ordinal);

    [JsonPropertyName("viewDays")]
    public Dictionary<string, TrafficHistoryDayValue> ViewDays { get; init; } = new(StringComparer.Ordinal);
}

internal sealed class TrafficHistoryDayValue
{
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("uniques")]
    public int Uniques { get; init; }
}
