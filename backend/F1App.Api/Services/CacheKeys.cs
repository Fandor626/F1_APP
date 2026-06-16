namespace F1App.Api.Services;

public static class CacheKeys
{
    public const string CurrentSeasonRaceSchedule = "races:current-season";
    public const string CurrentDriverStandings = "standings:drivers:current";
    public const string CurrentConstructorStandings = "standings:constructors:current";

    public static string CircuitPriorResults(int season, string circuitId) => $"results:{season}:{circuitId}";
}
