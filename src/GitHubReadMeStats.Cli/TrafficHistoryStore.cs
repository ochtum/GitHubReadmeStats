using System.Text;
using System.Text.Json;

namespace GitHubReadMeStats.Cli;

internal sealed class TrafficHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly TrafficHistoryState _state;

    private TrafficHistoryStore(TrafficHistoryState state)
    {
        _state = state;
    }

    public static TrafficHistoryStore Load(string path)
    {
        if (!File.Exists(path))
        {
            return new TrafficHistoryStore(new TrafficHistoryState());
        }

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new TrafficHistoryStore(new TrafficHistoryState());
        }

        try
        {
            TrafficHistoryState? state = JsonSerializer.Deserialize<TrafficHistoryState>(json, JsonOptions);
            return new TrafficHistoryStore(state ?? new TrafficHistoryState());
        }
        catch (JsonException)
        {
            // If state is corrupted, recreate it and continue generation.
            return new TrafficHistoryStore(new TrafficHistoryState());
        }
    }

    public RepositoryTrafficTotals? MergeAndGetTotals(string owner, string name, RepositoryTrafficSnapshot? snapshot)
    {
        string repositoryKey = $"{owner}/{name}";
        if (!_state.Repositories.TryGetValue(repositoryKey, out TrafficHistoryRepositoryState? repositoryState))
        {
            if (snapshot is null)
            {
                return null;
            }

            repositoryState = new TrafficHistoryRepositoryState();
            _state.Repositories[repositoryKey] = repositoryState;
        }

        bool updated = false;
        if (snapshot is not null)
        {
            MergeDailySeries(repositoryState.CloneDays, snapshot.CloneDays);
            MergeDailySeries(repositoryState.ViewDays, snapshot.ViewDays);
            updated = true;
        }

        if (repositoryState.CloneDays.Count == 0 && repositoryState.ViewDays.Count == 0)
        {
            return null;
        }

        long cloneCountTotal = repositoryState.CloneDays.Values.Sum(x => Math.Max(0, x.Count));
        long uniqueClonersTotal = repositoryState.CloneDays.Values.Sum(x => Math.Max(0, x.Uniques));
        long viewCountTotal = repositoryState.ViewDays.Values.Sum(x => Math.Max(0, x.Count));
        long uniqueVisitorsTotal = repositoryState.ViewDays.Values.Sum(x => Math.Max(0, x.Uniques));

        DateOnly sinceDate = GetBoundaryDate(repositoryState, pickFirst: true);
        DateOnly lastRecordedDate = GetBoundaryDate(repositoryState, pickFirst: false);

        return new RepositoryTrafficTotals(
            cloneCountTotal,
            uniqueClonersTotal,
            viewCountTotal,
            uniqueVisitorsTotal,
            sinceDate,
            lastRecordedDate,
            updated);
    }

    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(_state, JsonOptions);
        await File.WriteAllTextAsync(path, json + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
    }

    private static void MergeDailySeries(Dictionary<string, TrafficHistoryDayValue> destination, IReadOnlyList<TrafficDayPoint> source)
    {
        foreach (TrafficDayPoint point in source)
        {
            string dateKey = point.Date.ToString("yyyy-MM-dd");
            destination[dateKey] = new TrafficHistoryDayValue
            {
                Count = Math.Max(0, point.Count),
                Uniques = Math.Max(0, point.Uniques),
            };
        }
    }

    private static DateOnly GetBoundaryDate(TrafficHistoryRepositoryState state, bool pickFirst)
    {
        IEnumerable<DateOnly> dates = state.CloneDays.Keys
            .Concat(state.ViewDays.Keys)
            .Select(ParseDateKey)
            .Where(x => x is not null)
            .Select(x => x!.Value);

        DateOnly? boundary = pickFirst
            ? dates.DefaultIfEmpty(DateOnly.MinValue).Min()
            : dates.DefaultIfEmpty(DateOnly.MinValue).Max();

        return boundary ?? DateOnly.MinValue;
    }

    private static DateOnly? ParseDateKey(string value)
    {
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", out DateOnly date))
        {
            return date;
        }

        return null;
    }
}
