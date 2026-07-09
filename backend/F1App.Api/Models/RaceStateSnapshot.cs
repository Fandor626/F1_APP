namespace F1App.Api.Models;

public record RaceStateSnapshot
{
    public DateTimeOffset CapturedAt { get; init; }
    public IReadOnlyList<DriverState> Drivers { get; init; } = [];
    // Key = DriverNumber; list is ordered by LapNumber ascending
    public IReadOnlyDictionary<int, IReadOnlyList<LapTimeEntry>> LapChart { get; init; }
        = new Dictionary<int, IReadOnlyList<LapTimeEntry>>();
    public SessionMode SessionMode { get; init; } = SessionMode.Live;
    public string? FallbackRaceName { get; init; }
    public string? CircuitId { get; init; }
}
