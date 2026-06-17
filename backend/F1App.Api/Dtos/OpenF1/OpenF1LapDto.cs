using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.OpenF1;

// Minimal shape for lap-number tracking (Story 2.3 will extend with duration/sector fields)
// OpenF1 /laps uses date_start (not date) for its timestamp field
public record OpenF1LapDto(
    [property: JsonPropertyName("driver_number")] int DriverNumber,
    [property: JsonPropertyName("lap_number")] int LapNumber,
    [property: JsonPropertyName("date_start")] DateTimeOffset DateStart
);
