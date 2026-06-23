using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.OpenF1;

// OpenF1 /laps uses date_start (not date) for its timestamp field
public record OpenF1LapDto(
    [property: JsonPropertyName("driver_number")] int DriverNumber,
    [property: JsonPropertyName("lap_number")] int LapNumber,
    [property: JsonPropertyName("date_start")] DateTimeOffset DateStart,
    [property: JsonPropertyName("lap_duration")] double? LapDuration,
    [property: JsonPropertyName("is_pit_out_lap")] bool IsPitOutLap
);
