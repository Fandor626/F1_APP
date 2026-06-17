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

    public async Task<IReadOnlyList<ErgastDriverStandingDto>> GetCurrentDriverStandingsAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<ErgastDriverStandingsResponseDto>("current/driverStandings.json", cancellationToken)
            ?? throw new InvalidOperationException("Ergast returned an empty response for current driver standings.");

        return response.MRData.StandingsTable.StandingsLists.Count == 0
            ? []
            : response.MRData.StandingsTable.StandingsLists[0].DriverStandings;
    }

    public async Task<IReadOnlyList<ErgastConstructorStandingDto>> GetCurrentConstructorStandingsAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<ErgastConstructorStandingsResponseDto>("current/constructorStandings.json", cancellationToken)
            ?? throw new InvalidOperationException("Ergast returned an empty response for current constructor standings.");

        return response.MRData.StandingsTable.StandingsLists.Count == 0
            ? []
            : response.MRData.StandingsTable.StandingsLists[0].ConstructorStandings;
    }

    public async Task<IReadOnlyList<ErgastResultDto>> GetCircuitResultsAsync(int season, string circuitId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<ErgastRaceResultResponseDto>(
            $"{season}/circuits/{circuitId}/results/1.json", cancellationToken)
            ?? throw new InvalidOperationException($"Ergast returned an empty response for {circuitId} results in {season}.");

        return response.MRData.RaceTable.Races.Count == 0
            ? []
            : response.MRData.RaceTable.Races[0].Results;
    }

    public async Task<IReadOnlyList<ErgastQualifyingResultDto>> GetQualifyingResultsAsync(int round, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<ErgastQualifyingResponseDto>(
            $"current/{round}/qualifying.json", cancellationToken)
            ?? throw new InvalidOperationException($"Ergast returned an empty response for round {round} qualifying.");

        return response.MRData.RaceTable.Races.Count == 0
            ? []
            : response.MRData.RaceTable.Races[0].QualifyingResults;
    }
}
