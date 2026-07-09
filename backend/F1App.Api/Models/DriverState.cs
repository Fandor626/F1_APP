namespace F1App.Api.Models;

public record DriverState
{
    public int DriverNumber { get; init; }
    public string DriverCode { get; init; } = "";
    public string TeamName { get; init; } = "";
    public string TeamColour { get; init; } = "555555";
    public int Position { get; init; }
    public string? GapToCarAhead { get; init; }
    public bool GapIsStale { get; init; }
    public string? TyreCompound { get; init; }
    public int? StintLaps { get; init; }
    public string? ChampionshipDelta { get; init; }
    public double? X { get; init; }
    public double? Y { get; init; }
}
