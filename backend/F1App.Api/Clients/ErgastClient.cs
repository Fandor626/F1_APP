using System.Net.Http.Json;
using F1App.Api.Dtos.Ergast;

namespace F1App.Api.Clients;

public class ErgastClient(HttpClient httpClient) : IErgastClient
{
    public async Task<ErgastRaceTableDto> GetCurrentSeasonScheduleAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<ErgastScheduleResponseDto>("current.json", cancellationToken)
            ?? throw new InvalidOperationException("Ergast returned an empty response for the current season schedule.");

        return response.MRData.RaceTable;
    }
}
