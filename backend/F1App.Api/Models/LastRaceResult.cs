namespace F1App.Api.Models;

public record LastRaceResult(
    string RaceName,
    string RaceDate,
    IReadOnlyList<DriverState> Drivers);
