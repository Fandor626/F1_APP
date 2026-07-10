namespace F1App.Api.Models;

public record FastestSectorEntry(
    int DriverNumber,
    string DriverCode,
    string TeamColour,
    double TimeSeconds
);

public record FastestSectorBoard(
    FastestSectorEntry? S1,
    FastestSectorEntry? S2,
    FastestSectorEntry? S3
);
