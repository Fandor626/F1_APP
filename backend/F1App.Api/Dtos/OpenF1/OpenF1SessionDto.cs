using System.Text.Json.Serialization;

namespace F1App.Api.Dtos.OpenF1;

// OpenF1 /sessions JSON shape (snake_case) — used only to resolve the
// numeric session_key for a specific past Race session by date, since every
// other OpenF1 endpoint is keyed by session_key, not by date or circuit.
public record OpenF1SessionDto(
    [property: JsonPropertyName("session_key")] int SessionKey,
    [property: JsonPropertyName("session_type")] string SessionType,
    [property: JsonPropertyName("date_start")] DateTimeOffset DateStart
);
