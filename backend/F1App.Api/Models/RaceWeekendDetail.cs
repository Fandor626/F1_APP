namespace F1App.Api.Models;

public record RaceWeekendDetail(
    int Season,
    int Round,
    string RaceName,
    string CircuitId,
    string CircuitName,
    string Country,
    IReadOnlyList<Session> Sessions,
    CircuitPriorWinner? PriorYearWinner,
    ChampionshipDelta? ChampionshipDelta,
    LapRecord? AllTimeLapRecord,
    LapRecord? RecentLapRecord);
