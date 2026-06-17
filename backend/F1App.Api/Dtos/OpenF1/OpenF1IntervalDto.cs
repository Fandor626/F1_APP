using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.OpenF1;

public record OpenF1IntervalDto(
    [property: JsonPropertyName("driver_number")] int DriverNumber,
    [property: JsonPropertyName("gap_to_car_ahead")] string? GapToCarAhead,
    [property: JsonPropertyName("date")] DateTimeOffset Date
);
