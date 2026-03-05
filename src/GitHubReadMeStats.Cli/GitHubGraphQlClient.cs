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
            var payload = new
            {
                query,
                variables = new
                {
                    first = 50,
                    after = afterCursor,
                },
            };

            string responseText = await ExecuteAsync(payload, cancellationToken);

            GraphQlResponse<ViewerData>? response = JsonSerializer.Deserialize<GraphQlResponse<ViewerData>>(responseText, JsonOptions);
            if (response is null)
            {
                throw new InvalidOperationException("GitHub GraphQL returned an empty response.");
            }

            if (response.Errors is { Count: > 0 })
            {
                string errors = string.Join(" | ", response.Errors.Select(x => x.Message));
                throw new InvalidOperationException($"GitHub GraphQL errors: {errors}");
            }

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

    private async Task<string> ExecuteAsync(object payload, CancellationToken cancellationToken)
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
}
