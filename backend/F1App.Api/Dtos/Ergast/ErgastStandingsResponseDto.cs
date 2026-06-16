using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.Ergast;

public record ErgastDriverStandingsResponseDto(
    [property: JsonPropertyName("MRData")] ErgastDriverStandingsMrDataDto MRData);

public record ErgastDriverStandingsMrDataDto(
    [property: JsonPropertyName("StandingsTable")] ErgastDriverStandingsTableDto StandingsTable);

public record ErgastDriverStandingsTableDto(
    [property: JsonPropertyName("StandingsLists")] IReadOnlyList<ErgastDriverStandingsListDto> StandingsLists);

public record ErgastDriverStandingsListDto(
    [property: JsonPropertyName("DriverStandings")] IReadOnlyList<ErgastDriverStandingDto> DriverStandings);

public record ErgastDriverStandingDto(
    [property: JsonPropertyName("position")] string Position,
    [property: JsonPropertyName("points")] string Points,
    [property: JsonPropertyName("wins")] string Wins,
    [property: JsonPropertyName("Driver")] ErgastDriverDto Driver,
    [property: JsonPropertyName("Constructors")] IReadOnlyList<ErgastConstructorDto> Constructors);

public record ErgastDriverDto(
    [property: JsonPropertyName("driverId")] string DriverId,
    [property: JsonPropertyName("givenName")] string GivenName,
    [property: JsonPropertyName("familyName")] string FamilyName);

public record ErgastConstructorStandingsResponseDto(
    [property: JsonPropertyName("MRData")] ErgastConstructorStandingsMrDataDto MRData);

public record ErgastConstructorStandingsMrDataDto(
    [property: JsonPropertyName("StandingsTable")] ErgastConstructorStandingsTableDto StandingsTable);

public record ErgastConstructorStandingsTableDto(
    [property: JsonPropertyName("StandingsLists")] IReadOnlyList<ErgastConstructorStandingsListDto> StandingsLists);

public record ErgastConstructorStandingsListDto(
    [property: JsonPropertyName("ConstructorStandings")] IReadOnlyList<ErgastConstructorStandingDto> ConstructorStandings);

public record ErgastConstructorStandingDto(
    [property: JsonPropertyName("position")] string Position,
    [property: JsonPropertyName("points")] string Points,
    [property: JsonPropertyName("wins")] string Wins,
    [property: JsonPropertyName("Constructor")] ErgastConstructorDto Constructor);

public record ErgastConstructorDto(
    [property: JsonPropertyName("constructorId")] string ConstructorId,
    [property: JsonPropertyName("name")] string Name);
