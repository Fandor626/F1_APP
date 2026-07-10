using System.Net.Http.Json;
using F1App.Api.Dtos.OpenF1;

namespace F1App.Api.Clients;

public class OpenF1Client(HttpClient httpClient) : IOpenF1Client
{
    public async Task<IReadOnlyList<OpenF1PositionDto>> GetLatestPositionsAsync(DateTimeOffset since, CancellationToken ct)
    {
        var url = since == DateTimeOffset.MinValue
            ? "position?session_key=latest"
            : $"position?session_key=latest&date>{since:yyyy-MM-ddTHH:mm:ss.fff}";

        return await httpClient.GetFromJsonAsync<IReadOnlyList<OpenF1PositionDto>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<OpenF1IntervalDto>> GetLatestIntervalsAsync(DateTimeOffset since, CancellationToken ct)
    {
        var url = since == DateTimeOffset.MinValue
            ? "intervals?session_key=latest"
            : $"intervals?session_key=latest&date>{since:yyyy-MM-ddTHH:mm:ss.fff}";

        return await httpClient.GetFromJsonAsync<IReadOnlyList<OpenF1IntervalDto>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<OpenF1DriverInfoDto>> GetSessionDriversAsync(CancellationToken ct)
    {
        return await httpClient.GetFromJsonAsync<IReadOnlyList<OpenF1DriverInfoDto>>("drivers?session_key=latest", ct) ?? [];
    }

    public async Task<IReadOnlyList<OpenF1StintDto>> GetLatestStintsAsync(CancellationToken ct)
    {
        return await httpClient.GetFromJsonAsync<IReadOnlyList<OpenF1StintDto>>("stints?session_key=latest", ct) ?? [];
    }

    public async Task<IReadOnlyList<OpenF1LapDto>> GetLatestLapsAsync(DateTimeOffset since, CancellationToken ct)
    {
        var url = since == DateTimeOffset.MinValue
            ? "laps?session_key=latest"
            : $"laps?session_key=latest&date_start>{since:yyyy-MM-ddTHH:mm:ss.fff}";

        return await httpClient.GetFromJsonAsync<IReadOnlyList<OpenF1LapDto>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<OpenF1LocationDto>> GetLatestLocationsAsync(DateTimeOffset since, CancellationToken ct)
    {
        var url = since == DateTimeOffset.MinValue
            ? "location?session_key=latest"
            : $"location?session_key=latest&date>{since:yyyy-MM-ddTHH:mm:ss.fff}";

        return await httpClient.GetFromJsonAsync<IReadOnlyList<OpenF1LocationDto>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<OpenF1RaceControlDto>> GetLatestRaceControlAsync(DateTimeOffset since, CancellationToken ct)
    {
        var url = since == DateTimeOffset.MinValue
            ? "race_control?session_key=latest"
            : $"race_control?session_key=latest&date>{since:yyyy-MM-ddTHH:mm:ss.fff}";

        return await httpClient.GetFromJsonAsync<IReadOnlyList<OpenF1RaceControlDto>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<OpenF1PitDto>> GetLatestPitStopsAsync(DateTimeOffset since, CancellationToken ct)
    {
        var url = since == DateTimeOffset.MinValue
            ? "pit?session_key=latest"
            : $"pit?session_key=latest&date>{since:yyyy-MM-ddTHH:mm:ss.fff}";

        return await httpClient.GetFromJsonAsync<IReadOnlyList<OpenF1PitDto>>(url, ct) ?? [];
    }
}
