using F1App.Api.Dtos.Ergast;

namespace F1App.Api.Clients;

public interface IErgastClient
{
    Task<ErgastRaceTableDto> GetCurrentSeasonScheduleAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ErgastDriverStandingDto>> GetCurrentDriverStandingsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ErgastConstructorStandingDto>> GetCurrentConstructorStandingsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ErgastConstructorStandingDto>> GetConstructorStandingsByRoundAsync(int round, CancellationToken cancellationToken);

    Task<IReadOnlyList<ErgastResultDto>> GetCircuitResultsAsync(int season, string circuitId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ErgastQualifyingResultDto>> GetQualifyingResultsAsync(int round, CancellationToken cancellationToken);

    Task<ErgastRaceResultRaceDto?> GetLastRaceResultsAsync(CancellationToken cancellationToken);

    Task<ErgastRaceResultRaceDto?> GetRaceResultsByRoundAsync(int round, CancellationToken cancellationToken);

    Task<IReadOnlyList<ErgastPitStopDto>> GetCircuitPitStopsAsync(int season, string circuitId, CancellationToken cancellationToken);

    Task<ErgastCircuitDto?> GetCircuitInfoAsync(string circuitId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ErgastRaceResultRaceDto>> GetAllCircuitResultsAsync(string circuitId, CancellationToken cancellationToken);

    Task<ErgastDriverDto?> GetDriverInfoAsync(string driverId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ErgastRaceResultRaceDto>> GetAllDriverResultsAsync(string driverId, CancellationToken cancellationToken);

    Task<ErgastDriverStandingDto?> GetSeasonChampionAsync(int season, CancellationToken cancellationToken);
}
