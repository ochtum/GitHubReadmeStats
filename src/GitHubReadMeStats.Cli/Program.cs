using System.Text;

namespace GitHubReadMeStats.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        CliParseResult parseResult = CliParser.Parse(args);

        if (parseResult.ShowHelp)
        {
            Console.WriteLine(CliParser.GetHelpText());
            return 0;
        }

        if (parseResult.ShowVersion)
        {
            Console.WriteLine($"{CliParser.ApplicationName} {CliParser.ApplicationVersion}");
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(parseResult.Error) || parseResult.Options is null)
        {
            Console.Error.WriteLine($"Error: {parseResult.Error}");
            Console.Error.WriteLine();
            Console.Error.WriteLine(CliParser.GetHelpText());
            return 1;
        }

        CliOptions options = parseResult.Options;

        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60),
            };

            var graphqlClient = new GitHubGraphQlClient(httpClient, options.GitHubToken);
            ViewerRepositoriesResult repositoriesResult = await graphqlClient.FetchOwnedRepositoriesAsync();

            AggregationResult aggregation = LanguageAggregator.Aggregate(repositoriesResult.Repositories, options);
            if (aggregation.Languages.Count == 0)
            {
                Console.Error.WriteLine("No language data was collected. Check repository visibility, token scopes, and filters.");
                return 1;
            }

            DateTimeOffset generatedAtUtc = DateTimeOffset.UtcNow;
            string svg = SvgRenderer.Render(repositoriesResult.ViewerLogin, aggregation, options.Top, generatedAtUtc);

            EnsureParentDirectory(options.OutputPath);
            await File.WriteAllTextAsync(options.OutputPath, svg + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            Console.WriteLine($"Generated: {options.OutputPath}");
            Console.WriteLine($"Viewer: @{repositoriesResult.ViewerLogin}");
            Console.WriteLine($"Repositories (scanned/included): {aggregation.ScannedRepositoryCount}/{aggregation.IncludedRepositoryCount}");
            Console.WriteLine($"Languages: {aggregation.Languages.Count}");
            Console.WriteLine($"Total size: {SvgRenderer.ToHumanReadableBytes(aggregation.TotalBytes)}");

            if (!string.IsNullOrWhiteSpace(options.UpdateReadmePath))
            {
                string readmePath = options.UpdateReadmePath!;
                string imagePathForReadme = ResolveReadmeImagePath(options, readmePath);

                await ReadmeUpdater.UpdateAsync(
                    readmePath,
                    options.ReadmeSectionStartMarker,
                    options.ReadmeSectionEndMarker,
                    imagePathForReadme,
                    aggregation,
                    options.Top,
                    generatedAtUtc);

                Console.WriteLine($"Updated README section: {readmePath}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed: {ex.Message}");
            return 1;
        }
    }

    private static void EnsureParentDirectory(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string ResolveReadmeImagePath(CliOptions options, string readmePath)
    {
        if (!string.IsNullOrWhiteSpace(options.ImagePathForReadme))
        {
            return options.ImagePathForReadme!.Replace('\\', '/');
        }

        string readmeDirectory = Path.GetDirectoryName(readmePath) ?? ".";
        string relativePath = Path.GetRelativePath(readmeDirectory, options.OutputPath);
        return relativePath.Replace('\\', '/');
    }
}
