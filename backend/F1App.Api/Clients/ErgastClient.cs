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

    public async Task<IReadOnlyList<ErgastConstructorStandingDto>> GetConstructorStandingsByRoundAsync(int round, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<ErgastConstructorStandingsResponseDto>(
            $"current/{round}/constructorStandings.json", cancellationToken)
            ?? throw new InvalidOperationException($"Ergast returned an empty response for round {round} constructor standings.");

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

    public async Task<ErgastRaceResultRaceDto?> GetLastRaceResultsAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<ErgastRaceResultResponseDto>(
            "current/last/results.json", cancellationToken)
            ?? throw new InvalidOperationException("Ergast returned empty response for last race results.");

        return response.MRData.RaceTable.Races.Count == 0
            ? null
            : response.MRData.RaceTable.Races[0];
    }

    public async Task<ErgastRaceResultRaceDto?> GetRaceResultsByRoundAsync(int round, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<ErgastRaceResultResponseDto>(
            $"current/{round}/results.json", cancellationToken)
            ?? throw new InvalidOperationException($"Ergast returned an empty response for round {round} results.");

        return response.MRData.RaceTable.Races.Count == 0
            ? null
            : response.MRData.RaceTable.Races[0];
    }

    public async Task<IReadOnlyList<ErgastPitStopDto>> GetCircuitPitStopsAsync(int season, string circuitId, CancellationToken cancellationToken)
    {
        var raceResponse = await httpClient.GetFromJsonAsync<ErgastRaceResultResponseDto>(
            $"{season}/circuits/{circuitId}/results/1.json", cancellationToken)
            ?? throw new InvalidOperationException($"Ergast returned an empty response for {circuitId} in {season}.");

        if (raceResponse.MRData.RaceTable.Races.Count == 0)
            return [];

        var round = raceResponse.MRData.RaceTable.Races[0].Round;
        if (string.IsNullOrEmpty(round))
            return [];

        var pitResponse = await httpClient.GetFromJsonAsync<ErgastPitStopResponseDto>(
            $"{season}/{round}/pitstops.json?limit=100", cancellationToken)
            ?? throw new InvalidOperationException($"Ergast returned an empty response for {season}/{round} pit stops.");

        return pitResponse.MRData.RaceTable.Races.Count == 0
            ? []
            : pitResponse.MRData.RaceTable.Races[0].PitStops;
    }
}
