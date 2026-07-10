using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.Ergast;

public record ErgastCircuitInfoResponseDto(
    [property: JsonPropertyName("MRData")] ErgastCircuitInfoMrDataDto MRData);

public record ErgastCircuitInfoMrDataDto(
    [property: JsonPropertyName("CircuitTable")] ErgastCircuitInfoTableDto CircuitTable);

public record ErgastCircuitInfoTableDto(
    [property: JsonPropertyName("Circuits")] IReadOnlyList<ErgastCircuitDto> Circuits);
