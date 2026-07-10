using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.OpenF1;

// OpenF1 /pit JSON shape (snake_case). Verified live against
// https://api.openf1.org/v1/pit?session_key=9590 (Monza 2024).
public record OpenF1PitDto(
    [property: JsonPropertyName("date")] DateTimeOffset Date,
    [property: JsonPropertyName("driver_number")] int DriverNumber,
    [property: JsonPropertyName("lap_number")] int LapNumber
);
