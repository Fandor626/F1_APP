namespace F1App.Api.Models;

public record DriverStanding(
    int Position,
    string DriverName,
    string ConstructorName,
    decimal Points);
