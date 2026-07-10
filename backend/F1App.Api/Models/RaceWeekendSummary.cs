namespace F1App.Api.Models;

public record RaceWeekendSummary(
    int Season,
    int Round,
    string RaceName,
    string CircuitId,
    string CircuitName,
    string Locality,
    string Country,
    DateTimeOffset WeekendStart,
    DateTimeOffset RaceStart);
