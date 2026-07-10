namespace F1App.Api.Models;

public record ConstructorStanding(
    int Position,
    string ConstructorName,
    decimal Points,
    int Wins = 0,
    string Nationality = "");
