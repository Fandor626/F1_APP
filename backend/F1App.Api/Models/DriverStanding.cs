namespace F1App.Api.Models;

public record DriverStanding(
    int Position,
    string DriverId,        // Ergast driverId, used for cross-service matching (e.g. win probability)
    string DriverName,      // family-name-only, used on standings cards
    string FullName,        // "{givenName} {familyName}", used for championship delta
    string ConstructorName,
    decimal Points,
    int Wins = 0,
    string Nationality = "");
