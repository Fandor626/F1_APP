using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.Ergast;

public record ErgastDriverInfoResponseDto(
    [property: JsonPropertyName("MRData")] ErgastDriverInfoMrDataDto MRData);

public record ErgastDriverInfoMrDataDto(
    [property: JsonPropertyName("DriverTable")] ErgastDriverInfoTableDto DriverTable);

public record ErgastDriverInfoTableDto(
    [property: JsonPropertyName("Drivers")] IReadOnlyList<ErgastDriverDto> Drivers);
