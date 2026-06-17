using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.OpenF1;

public record OpenF1DriverInfoDto(
    [property: JsonPropertyName("driver_number")] int DriverNumber,
    [property: JsonPropertyName("name_acronym")] string NameAcronym,
    [property: JsonPropertyName("team_name")] string TeamName,
    [property: JsonPropertyName("team_colour")] string TeamColour
);
