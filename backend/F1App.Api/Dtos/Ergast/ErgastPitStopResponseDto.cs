using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.Ergast;

public record ErgastPitStopResponseDto(
    [property: JsonPropertyName("MRData")] ErgastPitStopMrDataDto MRData);

public record ErgastPitStopMrDataDto(
    [property: JsonPropertyName("RaceTable")] ErgastPitStopRaceTableDto RaceTable);

public record ErgastPitStopRaceTableDto(
    [property: JsonPropertyName("Races")] IReadOnlyList<ErgastPitStopRaceDto> Races);

public record ErgastPitStopRaceDto(
    [property: JsonPropertyName("PitStops")] IReadOnlyList<ErgastPitStopDto> PitStops = default!);

public record ErgastPitStopDto(
    [property: JsonPropertyName("driverId")] string DriverId,
    [property: JsonPropertyName("lap")] string Lap,
    [property: JsonPropertyName("stop")] string Stop);
