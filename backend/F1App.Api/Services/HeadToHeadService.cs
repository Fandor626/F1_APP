using System.Globalization;
using F1App.Api.Clients;
using F1App.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace F1App.Api.Services;

public class HeadToHeadService(IErgastClient ergastClient, IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    public async Task<IReadOnlyList<DriverOption>> GetAllDriversAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKeys.AllDrivers, out IReadOnlyList<DriverOption>? cached) && cached is not null)
            return cached;

        var drivers = await ergastClient.GetAllDriversAsync(cancellationToken);
        var options = drivers
            .Select(d => new DriverOption(d.DriverId, $"{d.GivenName} {d.FamilyName}"))
            .OrderBy(d => d.FullName)
            .ToList();

        cache.Set(CacheKeys.AllDrivers, (IReadOnlyList<DriverOption>)options, CacheTtl);
        return options;
    }

    public async Task<HeadToHeadComparison?> CompareAsync(
        string driverIdA, string driverIdB, int? season, string? circuitId, CancellationToken cancellationToken)
    {
        var statsA = await GetDriverStatsAsync(driverIdA, season, circuitId, cancellationToken);
        var statsB = await GetDriverStatsAsync(driverIdB, season, circuitId, cancellationToken);

        return statsA is null || statsB is null ? null : new HeadToHeadComparison(statsA, statsB);
    }

    // Cached per (driverId, season, circuitId) rather than per driver-pair —
    // a session comparing one driver against several others reuses that
    // driver's cached stats across every comparison instead of caching every
    // N^2 pair combination separately.
    private async Task<HeadToHeadDriverStats?> GetDriverStatsAsync(
        string driverId, int? season, string? circuitId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.DriverStatsForComparison(driverId, season, circuitId);
        if (cache.TryGetValue(cacheKey, out HeadToHeadDriverStats? cached))
            return cached;

        var driverInfo = await ergastClient.GetDriverInfoAsync(driverId, cancellationToken);
        if (driverInfo is null)
        {
            cache.Set(cacheKey, (HeadToHeadDriverStats?)null, CacheTtl);
            return null;
        }

        var races = await ergastClient.GetFilteredDriverResultsAsync(driverId, season, circuitId, cancellationToken);
        var qualifying = await ergastClient.GetDriverQualifyingHistoryAsync(driverId, season, circuitId, cancellationToken);

        var wins = 0;
        var dnf = 0;
        var points = 0;
        var fastestLaps = 0;
        var finishPositions = new List<int>();

        foreach (var race in races)
        {
            var result = race.Results.FirstOrDefault();
            if (result is null) continue;

            if (result.Position == "1") wins++;
            if (result.Status is not null && result.Status != "Finished" && !result.Status.StartsWith('+')) dnf++;
            if (result.FastestLap?.Rank == "1") fastestLaps++;
            points += (int)decimal.Parse(result.Points ?? "0", CultureInfo.InvariantCulture);
            if (int.TryParse(result.Position, out var pos)) finishPositions.Add(pos);
        }

        var qualiPositions = qualifying
            .Select(r => r.QualifyingResults.FirstOrDefault())
            .Where(r => r is not null)
            .Select(r => int.Parse(r!.Position, CultureInfo.InvariantCulture))
            .ToList();

        var stats = new HeadToHeadDriverStats(
            driverId,
            $"{driverInfo.GivenName} {driverInfo.FamilyName}",
            qualiPositions.Count == 0 ? null : qualiPositions.Average(),
            finishPositions.Count == 0 ? null : finishPositions.Average(),
            dnf,
            points,
            fastestLaps,
            wins,
            races.Count);

        cache.Set(cacheKey, stats, CacheTtl);
        return stats;
    }
}
