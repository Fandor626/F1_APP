using System.Globalization;
using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace F1App.Api.Services;

public class RaceScheduleService(IErgastClient ergastClient, IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public async Task<IReadOnlyList<RaceWeekendSummary>> GetCurrentSeasonScheduleAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKeys.CurrentSeasonRaceSchedule, out IReadOnlyList<RaceWeekendSummary>? cached) && cached is not null)
        {
            return cached;
        }

        var raceTable = await ergastClient.GetCurrentSeasonScheduleAsync(cancellationToken);

        var schedule = raceTable.Races
            .Select(ToSummary)
            .OrderBy(race => race.RaceStart)
            .ToList();

        cache.Set(CacheKeys.CurrentSeasonRaceSchedule, (IReadOnlyList<RaceWeekendSummary>)schedule, CacheTtl);

        return schedule;
    }

    private static RaceWeekendSummary ToSummary(ErgastRaceDto race) =>
        new(
            int.Parse(race.Season, CultureInfo.InvariantCulture),
            int.Parse(race.Round, CultureInfo.InvariantCulture),
            race.RaceName,
            race.Circuit.CircuitName,
            race.Circuit.Location.Locality,
            race.Circuit.Location.Country,
            CombineDateAndTime(race.Date, race.Time));

    private static DateTimeOffset CombineDateAndTime(string date, string? time) =>
        DateTimeOffset.Parse(
            $"{date}T{time ?? "00:00:00Z"}",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal);
}
