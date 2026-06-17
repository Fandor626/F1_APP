using System.Globalization;
using F1App.Api.Clients;
using F1App.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace F1App.Api.Services;

public class WinProbabilityService(IErgastClient ergastClient, IMemoryCache cache, StandingsService standingsService)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);
    // Short TTL for pre-qualifying cache: qualifying can complete within this window,
    // so we don't want to serve stale empty results for 6 hours.
    private static readonly TimeSpan EmptyCacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<WinProbabilityEntry>> GetWinProbabilitiesAsync(int round, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKeys.WinProbability(round), out IReadOnlyList<WinProbabilityEntry>? cached) && cached is not null)
        {
            return cached;
        }

        var qualifyingResults = await ergastClient.GetQualifyingResultsAsync(round, cancellationToken);

        if (qualifyingResults.Count == 0)
        {
            cache.Set(CacheKeys.WinProbability(round), (IReadOnlyList<WinProbabilityEntry>)[], EmptyCacheTtl);
            return [];
        }

        var standings = await standingsService.GetCurrentDriverStandingsAsync(cancellationToken);
        var pointsByDriverId = standings.ToDictionary(
            s => s.DriverId,
            s => (double)s.Points,
            StringComparer.OrdinalIgnoreCase);

        var maxPoints = pointsByDriverId.Count > 0 ? pointsByDriverId.Values.Max() : 1.0;
        if (maxPoints <= 0) maxPoints = 1.0;

        var rawScores = qualifyingResults
            .Select(r =>
            {
                if (!int.TryParse(r.Position, NumberStyles.Integer, CultureInfo.InvariantCulture, out var gridPos) || gridPos <= 0)
                    return (Result: r, GridPos: 0, Score: 0.0, Valid: false);

                var baseWeight = 1.0 / Math.Pow(gridPos, 1.5);
                var driverPoints = pointsByDriverId.TryGetValue(r.Driver.DriverId, out var pts) ? pts : 0.0;
                var champMultiplier = 1.0 + 0.3 * (driverPoints / maxPoints);
                return (Result: r, GridPos: gridPos, Score: baseWeight * champMultiplier, Valid: true);
            })
            .Where(x => x.Valid)
            .ToList();

        var totalScore = rawScores.Sum(x => x.Score);
        if (totalScore <= 0) totalScore = 1.0;

        var entries = rawScores
            .Select(x => new WinProbabilityEntry(
                $"{x.Result.Driver.GivenName} {x.Result.Driver.FamilyName}",
                x.Result.Constructor.Name,
                x.GridPos,
                Math.Round(x.Score / totalScore * 100.0, 1)))
            .OrderBy(e => e.GridPosition)
            .ToList();

        cache.Set(CacheKeys.WinProbability(round), (IReadOnlyList<WinProbabilityEntry>)entries, CacheTtl);
        return entries;
    }
}
