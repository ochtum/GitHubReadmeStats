namespace GitHubReadMeStats.Cli;

internal sealed record CliOptions(
    string GitHubToken,
    string OutputPath,
    string TopLanguagesReadmeWidth,
    string PublicRepoTotalsReadmeWidth,
    string[] ExcludedLanguages,
    int Top,
    bool IncludeForks,
    bool IncludeArchived,
    string? UpdateReadmePath,
    string TopLanguagesSectionStartMarker,
    string TopLanguagesSectionEndMarker,
    string StatsSectionStartMarker,
    string StatsSectionEndMarker,
    string OwnPinsSectionStartMarker,
    string OwnPinsSectionEndMarker,
    string ExternalPinsSectionStartMarker,
    string ExternalPinsSectionEndMarker,
    int PinsColumnsForReadme,
    string? CardsConfigPath,
    string CardsOutputDir);

internal sealed record CliParseResult(CliOptions? Options, string? Error, bool ShowHelp, bool ShowVersion);
