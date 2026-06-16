namespace F1App.Api.Models;

public record RaceWeekendDetail(
    int Season,
    int Round,
    string RaceName,
    string CircuitName,
    string Country,
    IReadOnlyList<Session> Sessions);
