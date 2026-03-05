namespace GitHubReadMeStats.Cli;

internal sealed record CliOptions(
    string GitHubToken,
    string OutputPath,
    string[] ExcludedLanguages,
    int Top,
    bool IncludeForks,
    bool IncludeArchived,
    string? UpdateReadmePath,
    string ReadmeSectionStartMarker,
    string ReadmeSectionEndMarker,
    string? ImagePathForReadme,
    string? CardsConfigPath,
    string CardsOutputDir);

internal sealed record CliParseResult(CliOptions? Options, string? Error, bool ShowHelp, bool ShowVersion);
