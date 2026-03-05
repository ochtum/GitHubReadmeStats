using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GitHubReadMeStats.Cli;

internal sealed class GitHubGraphQlClient
{
    private static readonly Uri GraphQlEndpoint = new("https://api.github.com/graphql", UriKind.Absolute);

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
    createdAt
    followers {
      totalCount
    }
    repositories(ownerAffiliations: OWNER, isFork: false, privacy: PUBLIC) {
      totalCount
    }
    contributionsCollection(from: $from, to: $to) {
      contributionCalendar {
        totalContributions
      }
    }
  }
}
""";

        DateTimeOffset from = new(DateTimeOffset.UtcNow.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset to = DateTimeOffset.UtcNow;

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

        return new UserSummary(
            user.Login ?? username,
            string.IsNullOrWhiteSpace(user.Name) ? (user.Login ?? username) : user.Name!,
            user.Followers?.TotalCount ?? 0,
            user.Repositories?.TotalCount ?? 0,
            user.ContributionsCollection?.ContributionCalendar?.TotalContributions ?? 0,
            user.CreatedAt);
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
            repository.IsPrivate,
            repository.IsArchived);
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
