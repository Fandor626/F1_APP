using System.Globalization;
using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace F1App.Api.Services;

public class CircuitProfileService(IErgastClient ergastClient, IMemoryCache cache)
{
    // Historical results tier — 7 days, matching RaceScheduleService's
    // ResultsCacheTtl precedent (this data only changes once per race).
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    public async Task<CircuitProfile?> GetCircuitProfileAsync(string circuitId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.CircuitProfile(circuitId);
        if (cache.TryGetValue(cacheKey, out CircuitProfile? cached))
            return cached;

        var profile = await ComputeAsync(circuitId, cancellationToken);
        cache.Set(cacheKey, profile, CacheTtl);
        return profile;
    }

    private async Task<CircuitProfile?> ComputeAsync(string circuitId, CancellationToken cancellationToken)
    {
        var circuitInfo = await ergastClient.GetCircuitInfoAsync(circuitId, cancellationToken);
        if (circuitInfo is null) return null;

        var races = await ergastClient.GetAllCircuitResultsAsync(circuitId, cancellationToken);
        var stats = CircuitStaticFacts.ByCircuitId.GetValueOrDefault(circuitId);

        if (races.Count == 0)
        {
            return new CircuitProfile(
                circuitId, circuitInfo.CircuitName, circuitInfo.Location.Locality,
                circuitInfo.Location.Country, 0, null, null, [], stats);
        }

        var firstSeason = races.Min(r => int.Parse(r.Season, CultureInfo.InvariantCulture));

        var winners = races
            .Select(r => (Race: r, Winner: r.Results.FirstOrDefault(x => x.Position == "1")))
            .Where(x => x.Winner is not null)
            .Select(x => new CircuitWinner(
                int.Parse(x.Race.Season, CultureInfo.InvariantCulture),
                $"{x.Winner!.Driver.GivenName} {x.Winner.Driver.FamilyName}",
                x.Winner.Constructor.Name))
            .OrderByDescending(w => w.Season)
            .ToList();

        var lapRecord = FindLapRecord(races);
        var recentLapRecord = FindRecentLapRecord(races);

        return new CircuitProfile(
            circuitId, circuitInfo.CircuitName, circuitInfo.Location.Locality,
            circuitInfo.Location.Country, firstSeason, lapRecord, recentLapRecord, winners, stats);
    }

    internal static LapRecord? FindLapRecord(IReadOnlyList<ErgastRaceResultRaceDto> races)
    {
        LapRecord? best = null;
        var bestSeconds = double.MaxValue;

        foreach (var race in races)
        {
            var season = int.Parse(race.Season, CultureInfo.InvariantCulture);
            foreach (var result in race.Results)
            {
                if (result.FastestLap?.Rank != "1" || result.FastestLap.Time?.Time is not { } timeText)
                    continue;

                var seconds = ParseLapTimeSeconds(timeText);
                if (seconds < bestSeconds)
                {
                    bestSeconds = seconds;
                    best = new LapRecord(result.Driver.DriverId, $"{result.Driver.GivenName} {result.Driver.FamilyName}", result.Constructor.Name, timeText, season);
                }
            }
        }
        return best;
    }

    // FR-4/FR-14's "current or most recently completed year" fastest lap —
    // the most recent season this circuit has raced in (not necessarily the
    // live current season, if this circuit hasn't run yet this year), scoped
    // to just that one season's race(s) at this circuit. Reuses the same
    // races list FindLapRecord scans — no extra Ergast call.
    internal static LapRecord? FindRecentLapRecord(IReadOnlyList<ErgastRaceResultRaceDto> races)
    {
        var mostRecentSeason = races.Max(r => int.Parse(r.Season, CultureInfo.InvariantCulture));
        var recentRaces = races.Where(r => int.Parse(r.Season, CultureInfo.InvariantCulture) == mostRecentSeason).ToList();
        return FindLapRecord(recentRaces);
    }

    // Ergast lap times are "m:ss.fff" (no hour component — no F1 lap has ever
    // taken 60+ minutes).
    internal static double ParseLapTimeSeconds(string time)
    {
        var parts = time.Split(':');
        return double.Parse(parts[0], CultureInfo.InvariantCulture) * 60
            + double.Parse(parts[1], CultureInfo.InvariantCulture);
    }
}
