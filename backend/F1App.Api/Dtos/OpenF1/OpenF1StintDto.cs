using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.OpenF1;

// OpenF1 /stints JSON shape (snake_case): driver_number, stint_number,
// lap_start, lap_end (null = ongoing), compound, tyre_age_at_start
public record OpenF1StintDto(
    [property: JsonPropertyName("driver_number")] int DriverNumber,
    [property: JsonPropertyName("stint_number")] int StintNumber,
    [property: JsonPropertyName("lap_start")] int LapStart,
    [property: JsonPropertyName("lap_end")] int? LapEnd,
    [property: JsonPropertyName("compound")] string? Compound,
    [property: JsonPropertyName("tyre_age_at_start")] int TyreAgeAtStart
);
