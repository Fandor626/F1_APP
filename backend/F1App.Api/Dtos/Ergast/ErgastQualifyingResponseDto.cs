using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.Ergast;

public record ErgastQualifyingResponseDto(
    [property: JsonPropertyName("MRData")] ErgastQualifyingMrDataDto MRData);

public record ErgastQualifyingMrDataDto(
    [property: JsonPropertyName("RaceTable")] ErgastQualifyingRaceTableDto RaceTable);

public record ErgastQualifyingRaceTableDto(
    [property: JsonPropertyName("Races")] IReadOnlyList<ErgastQualifyingRaceDto> Races);

public record ErgastQualifyingRaceDto(
    [property: JsonPropertyName("Circuit")] ErgastCircuitDto Circuit,
    [property: JsonPropertyName("QualifyingResults")] IReadOnlyList<ErgastQualifyingResultDto> QualifyingResults);

public record ErgastQualifyingResultDto(
    [property: JsonPropertyName("position")] string Position,
    [property: JsonPropertyName("Driver")] ErgastDriverDto Driver,
    [property: JsonPropertyName("Constructor")] ErgastConstructorDto Constructor);
