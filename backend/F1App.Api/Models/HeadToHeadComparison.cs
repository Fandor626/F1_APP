namespace F1App.Api.Models;

public record DriverOption(string DriverId, string FullName);

public record HeadToHeadDriverStats(
    string DriverId,
    string FullName,
    double? QualifyingAveragePosition,
    double? RaceFinishAveragePosition,
    int DnfCount,
    int PointsScored,
    int FastestLaps,
    int Wins,
    int RacesCompared);

public record HeadToHeadComparison(HeadToHeadDriverStats DriverA, HeadToHeadDriverStats DriverB);
