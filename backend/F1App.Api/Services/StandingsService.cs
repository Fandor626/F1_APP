using System.Globalization;
using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace F1App.Api.Services;

public class StandingsService(IErgastClient ergastClient, IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

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

    private static DriverStanding ToDriverStanding(ErgastDriverStandingDto standing) =>
        new(
            int.Parse(standing.Position, CultureInfo.InvariantCulture),
            standing.Driver.DriverId,
            standing.Driver.FamilyName,
            $"{standing.Driver.GivenName} {standing.Driver.FamilyName}",
            standing.Constructors.Count > 0 ? standing.Constructors[0].Name : string.Empty,
            decimal.Parse(standing.Points, CultureInfo.InvariantCulture));

    private static ConstructorStanding ToConstructorStanding(ErgastConstructorStandingDto standing) =>
        new(
            int.Parse(standing.Position, CultureInfo.InvariantCulture),
            standing.Constructor.Name,
            decimal.Parse(standing.Points, CultureInfo.InvariantCulture));
}
