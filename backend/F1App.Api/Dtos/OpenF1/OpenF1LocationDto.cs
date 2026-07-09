using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.OpenF1;

public record OpenF1LocationDto(
    [property: JsonPropertyName("driver_number")] int DriverNumber,
    [property: JsonPropertyName("x")] double X,
    [property: JsonPropertyName("y")] double Y,
    [property: JsonPropertyName("date")] DateTimeOffset Date
);
