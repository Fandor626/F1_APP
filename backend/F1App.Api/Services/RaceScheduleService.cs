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
        var races = await GetCachedRacesAsync(cancellationToken);

        return races
            .Select(ToSummary)
            .OrderBy(race => race.RaceStart)
            .ToList();
    }

    public async Task<RaceWeekendDetail?> GetRaceDetailAsync(int round, CancellationToken cancellationToken)
    {
        var races = await GetCachedRacesAsync(cancellationToken);
        var race = races.FirstOrDefault(r => int.Parse(r.Round, CultureInfo.InvariantCulture) == round);

        return race is null ? null : ToDetail(race);
    }

    private async Task<IReadOnlyList<ErgastRaceDto>> GetCachedRacesAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKeys.CurrentSeasonRaceSchedule, out IReadOnlyList<ErgastRaceDto>? cached) && cached is not null)
        {
            return cached;
        }

        var raceTable = await ergastClient.GetCurrentSeasonScheduleAsync(cancellationToken);
        cache.Set(CacheKeys.CurrentSeasonRaceSchedule, raceTable.Races, CacheTtl);

        return raceTable.Races;
    }

    private static RaceWeekendSummary ToSummary(ErgastRaceDto race)
    {
        var raceStart = CombineDateAndTime(race.Date, race.Time);
        var weekendStart = race.FirstPractice is null
            ? raceStart
            : CombineDateAndTime(race.FirstPractice.Date, race.FirstPractice.Time);

        return new(
            int.Parse(race.Season, CultureInfo.InvariantCulture),
            int.Parse(race.Round, CultureInfo.InvariantCulture),
            race.RaceName,
            race.Circuit.CircuitName,
            race.Circuit.Location.Locality,
            race.Circuit.Location.Country,
            weekendStart,
            raceStart);
    }

    private static RaceWeekendDetail ToDetail(ErgastRaceDto race) =>
        new(
            int.Parse(race.Season, CultureInfo.InvariantCulture),
            int.Parse(race.Round, CultureInfo.InvariantCulture),
            race.RaceName,
            race.Circuit.CircuitName,
            race.Circuit.Location.Country,
            BuildSessions(race));

    // Collecting whichever sessions Ergast actually published for this race and
    // sorting chronologically — rather than branching on weekend type — gives the
    // right order for both shapes for free: standard weekends naturally produce
    // FP1/FP2/FP3/Qualifying/Race, sprint weekends naturally produce
    // FP1/Sprint Qualifying/Sprint/Qualifying/Race, because that's the order
    // they're actually run in.
    private static IReadOnlyList<Session> BuildSessions(ErgastRaceDto race)
    {
        var sessions = new List<Session>();

        void AddIfPresent(string name, ErgastSessionDto? session)
        {
            if (session is not null)
            {
                sessions.Add(new Session(name, CombineDateAndTime(session.Date, session.Time)));
            }
        }

        AddIfPresent("FP1", race.FirstPractice);
        AddIfPresent("FP2", race.SecondPractice);
        AddIfPresent("FP3", race.ThirdPractice);
        AddIfPresent("Sprint Qualifying", race.SprintQualifying);
        AddIfPresent("Sprint", race.Sprint);
        AddIfPresent("Qualifying", race.Qualifying);
        sessions.Add(new Session("Race", CombineDateAndTime(race.Date, race.Time)));

        return sessions.OrderBy(s => s.Start).ToList();
    }

    private static DateTimeOffset CombineDateAndTime(string date, string? time) =>
        DateTimeOffset.Parse(
            $"{date}T{time ?? "00:00:00Z"}",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal);
}
