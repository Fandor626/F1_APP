namespace F1App.Api.Models;

public record SeasonWrapped(
    DramaticRaceAward MostDramaticRace,
    DriverStatAward MostDnfs,
    DriverStatAward BiggestPointsComeback,
    DriverRaceAward MostPositionsGainedInARace,
    ConstructorImprovementAward MostImprovedConstructor);

public record DramaticRaceAward(string RaceName, int Round, int TotalPositionSwing);

public record DriverStatAward(string DriverId, string DriverName, string ConstructorName, int Value);

public record DriverRaceAward(string DriverId, string DriverName, string ConstructorName, string RaceName, int PositionsGained);

public record ConstructorImprovementAward(string ConstructorName, int EarlySeasonPosition, int FinalPosition, int PositionsImproved);
