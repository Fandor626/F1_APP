using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.Ergast;

public record ErgastScheduleResponseDto(
    [property: JsonPropertyName("MRData")] ErgastMrDataDto MRData);

public record ErgastMrDataDto(
    [property: JsonPropertyName("RaceTable")] ErgastRaceTableDto RaceTable);

public record ErgastRaceTableDto(
    [property: JsonPropertyName("season")] string Season,
    [property: JsonPropertyName("Races")] IReadOnlyList<ErgastRaceDto> Races);

public record ErgastRaceDto(
    [property: JsonPropertyName("season")] string Season,
    [property: JsonPropertyName("round")] string Round,
    [property: JsonPropertyName("raceName")] string RaceName,
    [property: JsonPropertyName("Circuit")] ErgastCircuitDto Circuit,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("time")] string? Time,
    [property: JsonPropertyName("FirstPractice")] ErgastSessionDto? FirstPractice,
    [property: JsonPropertyName("SecondPractice")] ErgastSessionDto? SecondPractice,
    [property: JsonPropertyName("ThirdPractice")] ErgastSessionDto? ThirdPractice,
    [property: JsonPropertyName("Qualifying")] ErgastSessionDto? Qualifying,
    [property: JsonPropertyName("Sprint")] ErgastSessionDto? Sprint,
    [property: JsonPropertyName("SprintQualifying")] ErgastSessionDto? SprintQualifying);

public record ErgastSessionDto(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("time")] string? Time);

public record ErgastCircuitDto(
    [property: JsonPropertyName("circuitId")] string CircuitId,
    [property: JsonPropertyName("circuitName")] string CircuitName,
    [property: JsonPropertyName("Location")] ErgastLocationDto Location);

public record ErgastLocationDto(
    [property: JsonPropertyName("locality")] string Locality,
    [property: JsonPropertyName("country")] string Country);
