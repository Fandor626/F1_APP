namespace F1App.Api.Models;

public record DriverStanding(
    int Position,
    string DriverName,      // family-name-only, used on standings cards
    string FullName,        // "{givenName} {familyName}", used for championship delta
    string ConstructorName,
    decimal Points);
