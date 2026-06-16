using F1App.Api.Dtos.Ergast;

namespace F1App.Api.Clients;

public interface IErgastClient
{
    Task<ErgastRaceTableDto> GetCurrentSeasonScheduleAsync(CancellationToken cancellationToken);
}
