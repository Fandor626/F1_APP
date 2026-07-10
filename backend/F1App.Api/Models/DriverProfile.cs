namespace F1App.Api.Models;

public record DriverCareerTotals(int Races, int Wins, int Podiums, int Poles, int FastestLaps, int Titles);

public record ConstructorHistoryEntry(int Season, IReadOnlyList<string> ConstructorNames);

public record DriverCareerPoint(int Season, int Round, string RaceName, int PointsThisRound, decimal CumulativePoints);

public record DriverProfile(
    string DriverId,
    string FullName,
    string Nationality,
    DriverCareerTotals CareerTotals,
    IReadOnlyList<ConstructorHistoryEntry> ConstructorHistory,
    IReadOnlyList<DriverCareerPoint> CareerPoints);
