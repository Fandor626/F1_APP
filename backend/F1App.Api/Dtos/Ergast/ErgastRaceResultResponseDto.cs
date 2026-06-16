using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.Ergast;

public record ErgastRaceResultResponseDto(
    [property: JsonPropertyName("MRData")] ErgastRaceResultMrDataDto MRData);

public record ErgastRaceResultMrDataDto(
    [property: JsonPropertyName("RaceTable")] ErgastRaceResultRaceTableDto RaceTable);

public record ErgastRaceResultRaceTableDto(
    [property: JsonPropertyName("Races")] IReadOnlyList<ErgastRaceResultRaceDto> Races);

public record ErgastRaceResultRaceDto(
    [property: JsonPropertyName("Results")] IReadOnlyList<ErgastResultDto> Results);

public record ErgastResultDto(
    [property: JsonPropertyName("Driver")] ErgastDriverDto Driver,
    [property: JsonPropertyName("Constructor")] ErgastConstructorDto Constructor,
    [property: JsonPropertyName("Time")] ErgastResultTimeDto? Time);

public record ErgastResultTimeDto(
    [property: JsonPropertyName("time")] string Time);
