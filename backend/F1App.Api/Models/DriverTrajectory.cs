namespace F1App.Api.Models;

public record TrajectoryPoint(
    int Round,
    string RaceName,
    int? ResultPosition,
    int PointsThisRound,
    decimal CumulativePoints);

public record DriverTrajectory(
    string DriverId,
    string DriverName,
    string ConstructorName,
    IReadOnlyList<TrajectoryPoint> Points);
