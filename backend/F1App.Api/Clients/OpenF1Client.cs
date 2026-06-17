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
}
