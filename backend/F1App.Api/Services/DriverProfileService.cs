using System.Globalization;
using F1App.Api.Clients;
using F1App.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace F1App.Api.Services;

public class DriverProfileService(IErgastClient ergastClient, IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    public async Task<DriverProfile?> GetDriverProfileAsync(string driverId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.DriverProfile(driverId);
        if (cache.TryGetValue(cacheKey, out DriverProfile? cached))
            return cached;

        var profile = await ComputeAsync(driverId, cancellationToken);
        cache.Set(cacheKey, profile, CacheTtl);
        return profile;
    }

    private async Task<DriverProfile?> ComputeAsync(string driverId, CancellationToken cancellationToken)
    {
        var driverInfo = await ergastClient.GetDriverInfoAsync(driverId, cancellationToken);
        if (driverInfo is null) return null;

        var races = await ergastClient.GetAllDriverResultsAsync(driverId, cancellationToken);
        var ordered = races
            .OrderBy(r => int.Parse(r.Season, CultureInfo.InvariantCulture))
            .ThenBy(r => int.Parse(r.Round, CultureInfo.InvariantCulture))
            .ToList();

        var wins = 0;
        var podiums = 0;
        var poles = 0;
        var fastestLaps = 0;
        var cumulative = 0m;
        var careerPoints = new List<DriverCareerPoint>();
        var constructorsBySeason = new SortedDictionary<int, List<string>>();

        foreach (var race in ordered)
        {
            var result = race.Results.FirstOrDefault();
            if (result is null) continue;

            if (result.Position == "1") wins++;
            if (result.Position is "1" or "2" or "3") podiums++;
            if (result.Grid == "1") poles++;
            if (result.FastestLap?.Rank == "1") fastestLaps++;

            var season = int.Parse(race.Season, CultureInfo.InvariantCulture);
            constructorsBySeason.TryAdd(season, []);
            if (!constructorsBySeason[season].Contains(result.Constructor.Name))
                constructorsBySeason[season].Add(result.Constructor.Name);

            var pointsThisRound = (int)decimal.Parse(result.Points ?? "0", CultureInfo.InvariantCulture);
            cumulative += pointsThisRound;
            careerPoints.Add(new DriverCareerPoint(
                season, int.Parse(race.Round, CultureInfo.InvariantCulture), race.RaceName, pointsThisRound, cumulative));
        }

        var titles = 0;
        foreach (var season in constructorsBySeason.Keys)
        {
            var champion = await ergastClient.GetSeasonChampionAsync(season, cancellationToken);
            if (champion?.Driver.DriverId == driverId) titles++;
        }

        var totals = new DriverCareerTotals(ordered.Count, wins, podiums, poles, fastestLaps, titles);
        var constructorHistory = constructorsBySeason
            .Select(kv => new ConstructorHistoryEntry(kv.Key, kv.Value))
            .ToList();

        return new DriverProfile(
            driverId,
            $"{driverInfo.GivenName} {driverInfo.FamilyName}",
            driverInfo.Nationality ?? string.Empty,
            totals,
            constructorHistory,
            careerPoints);
    }
}
