using F1App.Api.Dtos.Ergast;

namespace F1App.Api.Clients;

public interface IErgastClient
{
    Task<ErgastRaceTableDto> GetCurrentSeasonScheduleAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ErgastDriverStandingDto>> GetCurrentDriverStandingsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ErgastConstructorStandingDto>> GetCurrentConstructorStandingsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ErgastResultDto>> GetCircuitResultsAsync(int season, string circuitId, CancellationToken cancellationToken);
}
