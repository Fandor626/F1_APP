using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.OpenF1;

// OpenF1 /laps uses date_start (not date) for its timestamp field
public record OpenF1LapDto(
    [property: JsonPropertyName("driver_number")] int DriverNumber,
    [property: JsonPropertyName("lap_number")] int LapNumber,
    [property: JsonPropertyName("date_start")] DateTimeOffset DateStart,
    [property: JsonPropertyName("lap_duration")] double? LapDuration,
    [property: JsonPropertyName("is_pit_out_lap")] bool IsPitOutLap,
    [property: JsonPropertyName("duration_sector_1")] double? DurationSector1 = null,
    [property: JsonPropertyName("duration_sector_2")] double? DurationSector2 = null,
    [property: JsonPropertyName("duration_sector_3")] double? DurationSector3 = null
);
