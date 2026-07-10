using System.Globalization;
using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace F1App.Api.Services;

public class StandingsService(IErgastClient ergastClient, IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    // DESIGN.md only defines 4 team accent tokens (Red Bull, Ferrari, Mercedes,
    // McLaren) — plotting all ~20 drivers on the trajectory chart would be
    // unreadable and give most lines no distinct color anyway. Mirrors the UX
    // mockup's own restraint (4 legend entries): pick the highest-standing
    // driver from each of the 4 tokenized constructors, so every plotted line
    // gets a real team color and no two lines share one.
    private static readonly string[] TokenizedConstructors =
        ["Red Bull Racing", "Ferrari", "Mercedes", "McLaren"];

    public async Task<IReadOnlyList<DriverStanding>> GetCurrentDriverStandingsAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKeys.CurrentDriverStandings, out IReadOnlyList<DriverStanding>? cached) && cached is not null)
        {
            return cached;
        }

        var standings = await ergastClient.GetCurrentDriverStandingsAsync(cancellationToken);
        var result = standings.Select(ToDriverStanding).ToList();

        cache.Set(CacheKeys.CurrentDriverStandings, (IReadOnlyList<DriverStanding>)result, CacheTtl);

        return result;
    }

    public async Task<IReadOnlyList<ConstructorStanding>> GetCurrentConstructorStandingsAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKeys.CurrentConstructorStandings, out IReadOnlyList<ConstructorStanding>? cached) && cached is not null)
        {
            return cached;
        }

        var standings = await ergastClient.GetCurrentConstructorStandingsAsync(cancellationToken);
        var result = standings.Select(ToConstructorStanding).ToList();

        cache.Set(CacheKeys.CurrentConstructorStandings, (IReadOnlyList<ConstructorStanding>)result, CacheTtl);

        return result;
    }

    public async Task<IReadOnlyList<DriverTrajectory>> GetChampionshipTrajectoryAsync(
        IReadOnlyList<RaceWeekendSummary> schedule, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKeys.ChampionshipTrajectory, out IReadOnlyList<DriverTrajectory>? cached) && cached is not null)
            return cached;

        var currentStandings = await GetCurrentDriverStandingsAsync(cancellationToken);
        var selectedDriverIds = TokenizedConstructors
            .Select(team => currentStandings
                .Where(s => s.ConstructorName == team)
                .OrderBy(s => s.Position)
                .FirstOrDefault())
            .Where(s => s is not null)
            .Select(s => s!.DriverId)
            .ToHashSet();

        var runningTotals = new Dictionary<string, decimal>();
        var pointsByDriver = new Dictionary<string, List<TrajectoryPoint>>();

        foreach (var race in schedule.OrderBy(r => r.Round))
        {
            var raceResult = await ergastClient.GetRaceResultsByRoundAsync(race.Round, cancellationToken);
            if (raceResult is null) break; // round not yet completed — and neither are any after it

            foreach (var result in raceResult.Results.Where(r => selectedDriverIds.Contains(r.Driver.DriverId)))
            {
                var pointsThisRound = decimal.Parse(result.Points ?? "0", CultureInfo.InvariantCulture);
                var cumulative = runningTotals.GetValueOrDefault(result.Driver.DriverId) + pointsThisRound;
                runningTotals[result.Driver.DriverId] = cumulative;

                pointsByDriver.TryAdd(result.Driver.DriverId, []);
                pointsByDriver[result.Driver.DriverId].Add(new TrajectoryPoint(
                    race.Round,
                    race.RaceName,
                    int.TryParse(result.Position, out var pos) ? pos : null,
                    (int)pointsThisRound,
                    cumulative));
            }
        }

        var trajectories = currentStandings
            .Where(s => selectedDriverIds.Contains(s.DriverId))
            .OrderBy(s => s.Position)
            .Select(s => new DriverTrajectory(s.DriverId, s.DriverName, s.ConstructorName, pointsByDriver.GetValueOrDefault(s.DriverId, [])))
            .ToList();

        cache.Set(CacheKeys.ChampionshipTrajectory, (IReadOnlyList<DriverTrajectory>)trajectories, CacheTtl);

        return trajectories;
    }

    private static DriverStanding ToDriverStanding(ErgastDriverStandingDto standing) =>
        new(
            int.Parse(standing.Position, CultureInfo.InvariantCulture),
            standing.Driver.DriverId,
            standing.Driver.FamilyName,
            $"{standing.Driver.GivenName} {standing.Driver.FamilyName}",
            standing.Constructors.Count > 0 ? standing.Constructors[0].Name : string.Empty,
            decimal.Parse(standing.Points, CultureInfo.InvariantCulture),
            int.Parse(standing.Wins, CultureInfo.InvariantCulture),
            standing.Driver.Nationality ?? string.Empty);

    private static ConstructorStanding ToConstructorStanding(ErgastConstructorStandingDto standing) =>
        new(
            int.Parse(standing.Position, CultureInfo.InvariantCulture),
            standing.Constructor.Name,
            decimal.Parse(standing.Points, CultureInfo.InvariantCulture),
            int.Parse(standing.Wins, CultureInfo.InvariantCulture),
            standing.Constructor.Nationality ?? string.Empty);
}
