using F1App.Api.Dtos.OpenF1;

namespace F1App.Api.Clients;

public interface IOpenF1Client
{
    Task<IReadOnlyList<OpenF1PositionDto>> GetLatestPositionsAsync(DateTimeOffset since, CancellationToken ct);
    Task<IReadOnlyList<OpenF1IntervalDto>> GetLatestIntervalsAsync(DateTimeOffset since, CancellationToken ct);
    Task<IReadOnlyList<OpenF1DriverInfoDto>> GetSessionDriversAsync(CancellationToken ct);
    // Full refresh — stints don't expose a date filter; max ~80 records per race
    Task<IReadOnlyList<OpenF1StintDto>> GetLatestStintsAsync(CancellationToken ct);
    // Incremental — uses date_start (not date!) as the filter field
    Task<IReadOnlyList<OpenF1LapDto>> GetLatestLapsAsync(DateTimeOffset since, CancellationToken ct);
}
