namespace F1App.Api.Models;

// EventType: "SafetyCar" | "VirtualSafetyCar" | "RedFlag" | "PitStop" | "Dnf" | "FastestLap"
public record RaceTimelineEvent(
    int LapNumber,
    string EventType,
    string? DriverCode,
    string? Detail
);
