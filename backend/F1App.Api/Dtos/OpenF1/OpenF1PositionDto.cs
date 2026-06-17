using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.OpenF1;

public record OpenF1PositionDto(
    [property: JsonPropertyName("driver_number")] int DriverNumber,
    [property: JsonPropertyName("position")] int Position,
    [property: JsonPropertyName("date")] DateTimeOffset Date
);
