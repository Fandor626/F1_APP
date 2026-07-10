using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.OpenF1;

// OpenF1 /race_control JSON shape (snake_case). Verified live against
// https://api.openf1.org/v1/race_control?session_key=9488 (Australia 2024):
// category is "SafetyCar" | "Flag" | "CarEvent" | "Other" | "Drs" | "SessionStatus" | ...
// flag is "GREEN" | "YELLOW" | "DOUBLE YELLOW" | "RED" | "BLUE" | "CHEQUERED" | null.
// driver_number is almost always null even for car-specific messages — the car
// number appears only in free-text `message` (e.g. "CAR 44 (HAM) STOPPED AT TURN 10"),
// so driver_number is intentionally NOT modelled here; RaceDataOrchestrator parses
// the car number out of `message` where needed (DNF detection).
public record OpenF1RaceControlDto(
    [property: JsonPropertyName("date")] DateTimeOffset Date,
    [property: JsonPropertyName("lap_number")] int? LapNumber,
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("flag")] string? Flag,
    [property: JsonPropertyName("message")] string? Message
);
