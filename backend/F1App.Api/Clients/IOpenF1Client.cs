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
    Task<IReadOnlyList<OpenF1LocationDto>> GetLatestLocationsAsync(DateTimeOffset since, CancellationToken ct);
    Task<IReadOnlyList<OpenF1RaceControlDto>> GetLatestRaceControlAsync(DateTimeOffset since, CancellationToken ct);
    Task<IReadOnlyList<OpenF1PitDto>> GetLatestPitStopsAsync(DateTimeOffset since, CancellationToken ct);

    // Historical (explicit session_key) queries — used only for the fallback
    // final-state snapshot (Story 8.1) and Race Replay (Story 8.2), never for
    // the live polling loops above, which are always session_key=latest.
    Task<IReadOnlyList<OpenF1SessionDto>> GetRaceSessionsAsync(int year, CancellationToken ct);
    Task<IReadOnlyList<OpenF1LapDto>> GetLapsForSessionAsync(int sessionKey, CancellationToken ct);
    Task<IReadOnlyList<OpenF1StintDto>> GetStintsForSessionAsync(int sessionKey, CancellationToken ct);
    Task<IReadOnlyList<OpenF1RaceControlDto>> GetRaceControlForSessionAsync(int sessionKey, CancellationToken ct);
    Task<IReadOnlyList<OpenF1PitDto>> GetPitStopsForSessionAsync(int sessionKey, CancellationToken ct);
    Task<IReadOnlyList<OpenF1DriverInfoDto>> GetDriversForSessionAsync(int sessionKey, CancellationToken ct);
}
