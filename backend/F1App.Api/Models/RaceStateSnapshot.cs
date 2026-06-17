namespace F1App.Api.Models;

public record RaceStateSnapshot
{
    public DateTimeOffset CapturedAt { get; init; }
    public IReadOnlyList<DriverState> Drivers { get; init; } = [];
}
