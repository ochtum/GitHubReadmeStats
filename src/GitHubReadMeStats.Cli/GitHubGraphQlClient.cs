using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GitHubReadMeStats.Cli;

internal sealed class GitHubGraphQlClient
{
    private static readonly Uri GraphQlEndpoint = new("https://api.github.com/graphql", UriKind.Absolute);
    private static readonly Uri RestApiEndpoint = new("https://api.github.com", UriKind.Absolute);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly string _token;

    public GitHubGraphQlClient(HttpClient httpClient, string token)
    {
        _httpClient = httpClient;
        _token = token;
    }

    public async Task<ViewerRepositoriesResult> FetchOwnedRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        const string query = """
query($first: Int!, $after: String) {
  viewer {
    login
    repositories(
      first: $first
      after: $after
      ownerAffiliations: OWNER
      orderBy: { field: UPDATED_AT, direction: DESC }
    ) {
      pageInfo {
        hasNextPage
        endCursor
      }
      nodes {
        nameWithOwner
        isPrivate
        isFork
        isArchived
        languages(first: 20, orderBy: { field: SIZE, direction: DESC }) {
          edges {
            size
            node {
              name
              color
            }
          }
        }
      }
    }
  }
}
""";

        var repositories = new List<RepositoryNode>();
        string? afterCursor = null;
        string viewerLogin = string.Empty;

        while (true)
        {
            GraphQlResponse<ViewerData> response = await ExecuteGraphQlAsync<ViewerData>(
                query,
                new
                {
                    first = 50,
                    after = afterCursor,
                },
                cancellationToken);

            ViewerNode viewer = response.Data?.Viewer
                ?? throw new InvalidOperationException("GitHub GraphQL response does not contain viewer.");

            viewerLogin = string.IsNullOrWhiteSpace(viewer.Login) ? viewerLogin : viewer.Login!;

            RepositoryConnection repositoryConnection = viewer.Repositories
                ?? throw new InvalidOperationException("GitHub GraphQL response does not contain repositories.");

            if (repositoryConnection.Nodes is { Count: > 0 })
            {
                foreach (RepositoryNode? node in repositoryConnection.Nodes)
                {
                    if (node is not null)
                    {
                        repositories.Add(node);
                    }
                }
            }

            bool hasNextPage = repositoryConnection.PageInfo?.HasNextPage == true;
            string? endCursor = repositoryConnection.PageInfo?.EndCursor;

            if (!hasNextPage || string.IsNullOrWhiteSpace(endCursor))
            {
                break;
            }

            afterCursor = endCursor;
        }

        if (string.IsNullOrWhiteSpace(viewerLogin))
        {
            throw new InvalidOperationException("Unable to resolve viewer login from GitHub GraphQL response.");
        }

        return new ViewerRepositoriesResult(viewerLogin, repositories);
    }

    public async Task<UserSummary> FetchUserSummaryAsync(string username, CancellationToken cancellationToken = default)
    {
        const string query = """
query($login: String!, $from: DateTime!, $to: DateTime!) {
  user(login: $login) {
    login
    name
    location
    createdAt
    followers {
      totalCount
    }
    repositories(ownerAffiliations: OWNER, isFork: false, privacy: PUBLIC) {
      totalCount
    }
    privateRepositories: repositories(ownerAffiliations: OWNER, isFork: false, privacy: PRIVATE) {
      totalCount
    }
    contributionsCollection(from: $from, to: $to) {
      totalCommitContributions
      totalIssueContributions
      totalPullRequestContributions
      totalRepositoriesWithContributedCommits
      contributionCalendar {
        totalContributions
        weeks {
          contributionDays {
            date
            contributionCount
          }
        }
      }
    }
  }
}
""";

        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        DateTimeOffset from = nowUtc.AddYears(-1);
        DateTimeOffset to = nowUtc;

        GraphQlResponse<UserLookupData> response = await ExecuteGraphQlAsync<UserLookupData>(
            query,
            new
            {
                login = username,
                from,
                to,
            },
            cancellationToken);

        UserNode user = response.Data?.User
            ?? throw new InvalidOperationException($"User not found: {username}");

        int totalStarsEarned = await FetchTotalStarsEarnedAsync(user.Login ?? username, cancellationToken);
        int totalCommitsLastYear = Math.Max(0, user.ContributionsCollection?.TotalCommitContributions ?? 0);
        int totalPullRequestsLastYear = Math.Max(0, user.ContributionsCollection?.TotalPullRequestContributions ?? 0);
        int totalIssuesLastYear = Math.Max(0, user.ContributionsCollection?.TotalIssueContributions ?? 0);
        int contributedToRepositoriesLastYear = Math.Max(0, user.ContributionsCollection?.TotalRepositoriesWithContributedCommits ?? 0);

        List<ContributionDaySummary> contributionDays = ExtractContributionDays(user);
        int contributionsThisYear = contributionDays
            .Where(x => x.Date.Year == nowUtc.Year)
            .Sum(x => x.ContributionCount);

        return new UserSummary(
            user.Login ?? username,
            string.IsNullOrWhiteSpace(user.Name) ? (user.Login ?? username) : user.Name!,
            user.Location,
            user.Followers?.TotalCount ?? 0,
            user.Repositories?.TotalCount ?? 0,
            user.PrivateRepositories?.TotalCount ?? 0,
            totalStarsEarned,
            totalCommitsLastYear,
            totalPullRequestsLastYear,
            totalIssuesLastYear,
            contributedToRepositoriesLastYear,
            contributionsThisYear,
            user.CreatedAt,
            contributionDays);
    }

    private async Task<int> FetchTotalStarsEarnedAsync(string username, CancellationToken cancellationToken)
    {
        const string query = """
query($login: String!, $first: Int!, $after: String) {
  user(login: $login) {
    repositories(
      first: $first
      after: $after
      ownerAffiliations: OWNER
      isFork: false
      orderBy: { field: UPDATED_AT, direction: DESC }
    ) {
      pageInfo {
        hasNextPage
        endCursor
      }
      nodes {
        stargazerCount
      }
    }
  }
}
""";

        int totalStars = 0;
        string? afterCursor = null;

        while (true)
        {
            GraphQlResponse<UserStarsLookupData> response = await ExecuteGraphQlAsync<UserStarsLookupData>(
                query,
                new
                {
                    login = username,
                    first = 100,
                    after = afterCursor,
                },
                cancellationToken);

            RepositoryConnection repositories = response.Data?.User?.Repositories
                ?? throw new InvalidOperationException($"Unable to fetch repositories for user: {username}");

            if (repositories.Nodes is { Count: > 0 })
            {
                foreach (RepositoryNode? node in repositories.Nodes)
                {
                    totalStars += Math.Max(0, node?.StargazerCount ?? 0);
                }
            }

            bool hasNextPage = repositories.PageInfo?.HasNextPage == true;
            string? endCursor = repositories.PageInfo?.EndCursor;
            if (!hasNextPage || string.IsNullOrWhiteSpace(endCursor))
            {
                break;
            }

            afterCursor = endCursor;
        }

        return totalStars;
    }

    public async Task<PinCardData> FetchRepositoryCardDataAsync(string owner, string name, CancellationToken cancellationToken = default)
    {
        const string query = """
query($owner: String!, $name: String!) {
  repository(owner: $owner, name: $name) {
    name
    url
    description
    isPrivate
    isArchived
    stargazerCount
    forkCount
    primaryLanguage {
      name
      color
    }
  }
}
""";

        GraphQlResponse<RepositoryLookupData> response = await ExecuteGraphQlAsync<RepositoryLookupData>(
            query,
            new
            {
                owner,
                name,
            },
            cancellationToken);

        RepositoryNode repository = response.Data?.Repository
            ?? throw new InvalidOperationException($"Repository not found or inaccessible: {owner}/{name}");

        string language = repository.PrimaryLanguage?.Name ?? "Unknown";
        string languageColor = NormalizeColor(repository.PrimaryLanguage?.Color);

        return new PinCardData(
            owner,
            repository.Name ?? name,
            repository.Url ?? $"https://github.com/{owner}/{name}",
            repository.Description ?? "No description provided",
            repository.StargazerCount,
            repository.ForkCount,
            language,
            languageColor,
            null,
            repository.IsPrivate,
            repository.IsArchived,
            null,
            null);
    }

    public async Task<RepositoryTrafficSnapshot?> TryFetchRepositoryTrafficAsync(string owner, string name, CancellationToken cancellationToken = default)
    {
        string encodedOwner = Uri.EscapeDataString(owner);
        string encodedName = Uri.EscapeDataString(name);

        RestTrafficResponse? cloneResponse = await ExecuteRestAsync<RestTrafficResponse>(
            $"/repos/{encodedOwner}/{encodedName}/traffic/clones",
            allowInaccessibleRepository: true,
            cancellationToken);

        if (cloneResponse is null)
        {
            return null;
        }

        RestTrafficResponse? viewResponse = await ExecuteRestAsync<RestTrafficResponse>(
            $"/repos/{encodedOwner}/{encodedName}/traffic/views",
            allowInaccessibleRepository: true,
            cancellationToken);

        if (viewResponse is null)
        {
            return null;
        }

        List<TrafficDayPoint> cloneDays = (cloneResponse.Clones ?? new List<RestTrafficPoint>())
            .Select(MapTrafficDayPoint)
            .OrderBy(x => x.Date)
            .ToList();

        List<TrafficDayPoint> viewDays = (viewResponse.Views ?? new List<RestTrafficPoint>())
            .Select(MapTrafficDayPoint)
            .OrderBy(x => x.Date)
            .ToList();

        return new RepositoryTrafficSnapshot(
            cloneResponse.Count,
            cloneResponse.Uniques,
            viewResponse.Count,
            viewResponse.Uniques,
            cloneDays,
            viewDays);
    }

    private async Task<GraphQlResponse<TData>> ExecuteGraphQlAsync<TData>(string query, object variables, CancellationToken cancellationToken)
    {
        var payload = new
        {
            query,
            variables,
        };

        string responseText = await ExecuteRawAsync(payload, cancellationToken);

        GraphQlResponse<TData>? response = JsonSerializer.Deserialize<GraphQlResponse<TData>>(responseText, JsonOptions);
        if (response is null)
        {
            throw new InvalidOperationException("GitHub GraphQL returned an empty response.");
        }

        if (response.Errors is { Count: > 0 })
        {
            string errors = string.Join(" | ", response.Errors.Select(x => x.Message));
            throw new InvalidOperationException($"GitHub GraphQL errors: {errors}");
        }

        return response;
    }

    private async Task<string> ExecuteRawAsync(object payload, CancellationToken cancellationToken)
    {
        string jsonBody = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, GraphQlEndpoint)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", _token);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("GitHubReadMeStats", CliParser.ApplicationVersion));

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string snippet = body.Length > 500 ? body[..500] + "..." : body;
            throw new InvalidOperationException(
                $"GitHub GraphQL request failed: {(int)response.StatusCode} {response.ReasonPhrase}. {snippet}");
        }

        return body;
    }

    private async Task<T?> ExecuteRestAsync<T>(string relativePath, bool allowInaccessibleRepository, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(RestApiEndpoint, relativePath));
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", _token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("GitHubReadMeStats", CliParser.ApplicationVersion));

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if (allowInaccessibleRepository &&
                response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Unauthorized)
            {
                return default;
            }

            string snippet = body.Length > 500 ? body[..500] + "..." : body;
            throw new InvalidOperationException(
                $"GitHub REST request failed: {(int)response.StatusCode} {response.ReasonPhrase}. {snippet}");
        }

        T? payload = JsonSerializer.Deserialize<T>(body, JsonOptions);
        if (payload is null)
        {
            throw new InvalidOperationException("GitHub REST returned an empty response.");
        }

        return payload;
    }

    private static TrafficDayPoint MapTrafficDayPoint(RestTrafficPoint point)
    {
        DateOnly date = DateOnly.FromDateTime(point.Timestamp.UtcDateTime);
        return new TrafficDayPoint(
            date,
            Math.Max(0, point.Count),
            Math.Max(0, point.Uniques));
    }

    private static List<ContributionDaySummary> ExtractContributionDays(UserNode user)
    {
        var result = new List<ContributionDaySummary>();
        List<ContributionWeekNode>? weeks = user.ContributionsCollection?.ContributionCalendar?.Weeks;
        if (weeks is null || weeks.Count == 0)
        {
            return result;
        }

        foreach (ContributionWeekNode week in weeks)
        {
            if (week.ContributionDays is null || week.ContributionDays.Count == 0)
            {
                continue;
            }

            foreach (ContributionDayNode day in week.ContributionDays)
            {
                if (day is null || string.IsNullOrWhiteSpace(day.Date))
                {
                    continue;
                }

                if (!DateOnly.TryParse(day.Date, out DateOnly parsedDate))
                {
                    continue;
                }

                result.Add(new ContributionDaySummary(parsedDate, Math.Max(0, day.ContributionCount)));
            }
        }

        return result
            .OrderBy(x => x.Date)
            .ToList();
    }

    private static string NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return "#94A3B8";
        }

        string trimmed = color.Trim();
        return trimmed.StartsWith('#') ? trimmed : $"#{trimmed}";
    }
}
