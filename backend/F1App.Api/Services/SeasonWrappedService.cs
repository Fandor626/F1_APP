using System.Globalization;
using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace F1App.Api.Services;

public class SeasonWrappedService(IErgastClient ergastClient, IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    // Unlike every other cached method in this codebase, a `null` result here
    // (season still in progress) is itself a meaningful, cacheable answer — not
    // a cache miss — so this method uses a bare TryGetValue instead of the
    // `&& cached is not null` guard used by StandingsService. Without this,
    // every single request during an in-progress season would re-walk the
    // "is the final round done yet" check against Ergast.
    public async Task<SeasonWrapped?> GetSeasonWrappedAsync(IReadOnlyList<RaceWeekendSummary> schedule, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKeys.SeasonWrapped, out SeasonWrapped? cached))
            return cached;

        var wrapped = await ComputeAsync(schedule, cancellationToken);
        cache.Set(CacheKeys.SeasonWrapped, wrapped, CacheTtl);
        return wrapped;
    }

    private async Task<SeasonWrapped?> ComputeAsync(IReadOnlyList<RaceWeekendSummary> schedule, CancellationToken cancellationToken)
    {
        if (schedule.Count == 0) return null;

        var finalRound = schedule.Max(r => r.Round);
        var finalRoundResult = await ergastClient.GetRaceResultsByRoundAsync(finalRound, cancellationToken);
        if (finalRoundResult is null) return null; // season still in progress — cheap short-circuit, no further calls

        var roundResults = new List<(int Round, string RaceName, ErgastRaceResultRaceDto Result)>();
        foreach (var race in schedule.OrderBy(r => r.Round))
        {
            var result = race.Round == finalRound
                ? finalRoundResult
                : await ergastClient.GetRaceResultsByRoundAsync(race.Round, cancellationToken);
            if (result is not null)
                roundResults.Add((race.Round, race.RaceName, result));
        }

        var mostDramaticRace = FindMostDramaticRace(roundResults);
        var mostDnfs = FindMostDnfs(roundResults);
        var biggestComeback = FindBiggestPointsComeback(roundResults);
        var mostGained = FindMostPositionsGainedInARace(roundResults);
        var mostImprovedConstructor = await FindMostImprovedConstructorAsync(schedule.Count, cancellationToken);

        if (mostDramaticRace is null || mostDnfs is null || biggestComeback is null || mostGained is null || mostImprovedConstructor is null)
            return null; // insufficient data to build a meaningful wrap (e.g. a 0-round or all-DNS season)

        return new SeasonWrapped(mostDramaticRace, mostDnfs, biggestComeback, mostGained, mostImprovedConstructor);
    }

    // internal (not private), matching RaceDataOrchestrator's established
    // convention for directly unit-testing pure computation helpers.
    internal static DramaticRaceAward? FindMostDramaticRace(List<(int Round, string RaceName, ErgastRaceResultRaceDto Result)> roundResults)
    {
        (int Round, string RaceName, int Swing)? best = null;
        foreach (var (round, raceName, result) in roundResults)
        {
            var swing = 0;
            foreach (var r in result.Results)
            {
                if (int.TryParse(r.Grid, out var grid) && int.TryParse(r.Position, out var pos))
                    swing += Math.Abs(grid - pos);
            }
            if (best is null || swing > best.Value.Swing)
                best = (round, raceName, swing);
        }
        return best is null ? null : new DramaticRaceAward(best.Value.RaceName, best.Value.Round, best.Value.Swing);
    }

    internal static DriverStatAward? FindMostDnfs(List<(int Round, string RaceName, ErgastRaceResultRaceDto Result)> roundResults)
    {
        var dnfCounts = new Dictionary<string, (string Name, string Constructor, int Count)>();
        foreach (var (_, _, result) in roundResults)
        {
            foreach (var r in result.Results)
            {
                var isDnf = r.Status is not null && r.Status != "Finished" && !r.Status.StartsWith('+');
                if (!isDnf) continue;

                (string Name, string Constructor, int Count) existing = dnfCounts.GetValueOrDefault(r.Driver.DriverId, (r.Driver.FamilyName, r.Constructor.Name, 0));
                dnfCounts[r.Driver.DriverId] = (existing.Name, existing.Constructor, existing.Count + 1);
            }
        }
        if (dnfCounts.Count == 0) return null;
        var top = dnfCounts.OrderByDescending(kv => kv.Value.Count).First();
        return new DriverStatAward(top.Key, top.Value.Name, top.Value.Constructor, top.Value.Count);
    }

    internal static DriverStatAward? FindBiggestPointsComeback(List<(int Round, string RaceName, ErgastRaceResultRaceDto Result)> roundResults)
    {
        var cumulative = new Dictionary<string, decimal>();
        var driverMeta = new Dictionary<string, (string Name, string Constructor)>();
        var gapHistoryByDriver = new Dictionary<string, List<decimal>>();

        foreach (var (_, _, result) in roundResults.OrderBy(r => r.Round))
        {
            foreach (var r in result.Results)
            {
                var pts = decimal.Parse(r.Points ?? "0", CultureInfo.InvariantCulture);
                cumulative[r.Driver.DriverId] = cumulative.GetValueOrDefault(r.Driver.DriverId) + pts;
                driverMeta[r.Driver.DriverId] = (r.Driver.FamilyName, r.Constructor.Name);
            }

            var leaderPoints = cumulative.Count == 0 ? 0 : cumulative.Values.Max();
            foreach (var driverId in cumulative.Keys)
            {
                gapHistoryByDriver.TryAdd(driverId, []);
                gapHistoryByDriver[driverId].Add(leaderPoints - cumulative[driverId]);
            }
        }

        string? bestDriverId = null;
        var bestComeback = 0m;
        foreach (var (driverId, gaps) in gapHistoryByDriver)
        {
            if (gaps.Count == 0) continue;
            var comeback = gaps.Max() - gaps[^1];
            if (comeback > bestComeback)
            {
                bestComeback = comeback;
                bestDriverId = driverId;
            }
        }

        if (bestDriverId is null) return null;
        var meta = driverMeta[bestDriverId];
        return new DriverStatAward(bestDriverId, meta.Name, meta.Constructor, (int)bestComeback);
    }

    internal static DriverRaceAward? FindMostPositionsGainedInARace(List<(int Round, string RaceName, ErgastRaceResultRaceDto Result)> roundResults)
    {
        (string DriverId, string Name, string Constructor, string RaceName, int Gained)? best = null;
        foreach (var (_, raceName, result) in roundResults)
        {
            foreach (var r in result.Results)
            {
                if (!int.TryParse(r.Grid, out var grid) || !int.TryParse(r.Position, out var pos)) continue;
                var gained = grid - pos;
                if (best is null || gained > best.Value.Gained)
                    best = (r.Driver.DriverId, r.Driver.FamilyName, r.Constructor.Name, raceName, gained);
            }
        }
        return best is null
            ? null
            : new DriverRaceAward(best.Value.DriverId, best.Value.Name, best.Value.Constructor, best.Value.RaceName, best.Value.Gained);
    }

    internal async Task<ConstructorImprovementAward?> FindMostImprovedConstructorAsync(int totalRounds, CancellationToken cancellationToken)
    {
        var checkpointRound = Math.Min(5, totalRounds);
        var checkpointStandings = await ergastClient.GetConstructorStandingsByRoundAsync(checkpointRound, cancellationToken);
        var finalStandings = await ergastClient.GetCurrentConstructorStandingsAsync(cancellationToken);

        var checkpointPositions = checkpointStandings.ToDictionary(
            s => s.Constructor.Name, s => int.Parse(s.Position, CultureInfo.InvariantCulture));

        ConstructorImprovementAward? best = null;
        foreach (var final in finalStandings)
        {
            if (!checkpointPositions.TryGetValue(final.Constructor.Name, out var checkpointPos)) continue;

            var finalPos = int.Parse(final.Position, CultureInfo.InvariantCulture);
            var improved = checkpointPos - finalPos;
            if (best is null || improved > best.PositionsImproved)
                best = new ConstructorImprovementAward(final.Constructor.Name, checkpointPos, finalPos, improved);
        }
        return best;
    }
}
