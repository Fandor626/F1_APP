using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.Ergast;

public record ErgastRaceResultResponseDto(
    [property: JsonPropertyName("MRData")] ErgastRaceResultMrDataDto MRData);

public record ErgastRaceResultMrDataDto(
    [property: JsonPropertyName("RaceTable")] ErgastRaceResultRaceTableDto RaceTable);

public record ErgastRaceResultRaceTableDto(
    [property: JsonPropertyName("Races")] IReadOnlyList<ErgastRaceResultRaceDto> Races);

public record ErgastRaceResultRaceDto(
    [property: JsonPropertyName("raceName")] string RaceName = "",
    [property: JsonPropertyName("round")] string Round = "",
    [property: JsonPropertyName("date")] string Date = "",
    [property: JsonPropertyName("Results")] IReadOnlyList<ErgastResultDto> Results = default!);

public record ErgastResultDto(
    [property: JsonPropertyName("Driver")] ErgastDriverDto Driver,
    [property: JsonPropertyName("Constructor")] ErgastConstructorDto Constructor,
    [property: JsonPropertyName("Time")] ErgastResultTimeDto? Time,
    [property: JsonPropertyName("position")] string? Position = null,
    [property: JsonPropertyName("number")] string? Number = null,
    [property: JsonPropertyName("status")] string? Status = null);

public record ErgastResultTimeDto(
    [property: JsonPropertyName("time")] string Time);
