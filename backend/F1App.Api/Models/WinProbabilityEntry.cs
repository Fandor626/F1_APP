namespace F1App.Api.Models;

public record WinProbabilityEntry(
    string DriverName,
    string ConstructorName,
    int GridPosition,
    double WinProbability);
