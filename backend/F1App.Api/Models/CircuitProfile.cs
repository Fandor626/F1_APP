namespace F1App.Api.Models;

public record LapRecord(string DriverName, string ConstructorName, string Time, int Season);

public record CircuitWinner(int Season, string DriverName, string ConstructorName);

public record CircuitStats(double LengthKm, int Corners, int DrsZones);

public record CircuitProfile(
    string CircuitId,
    string CircuitName,
    string Locality,
    string Country,
    int FirstF1Season,
    LapRecord? LapRecord,
    LapRecord? RecentLapRecord,
    IReadOnlyList<CircuitWinner> PastWinners,
    CircuitStats? Stats);
