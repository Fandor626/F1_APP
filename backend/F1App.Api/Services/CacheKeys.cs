namespace F1App.Api.Services;

public static class CacheKeys
{
    public const string CurrentSeasonRaceSchedule = "races:current-season";
    public const string CurrentDriverStandings = "standings:drivers:current";
    public const string CurrentConstructorStandings = "standings:constructors:current";
    public const string ChampionshipTrajectory = "standings:trajectory:current";
    public const string SeasonWrapped = "standings:season-wrapped:current";

    public static string CircuitPriorResults(int season, string circuitId) => $"results:{season}:{circuitId}";

    public static string WinProbability(int round) => $"winProbability:{round}";

    public const string LastRaceResult = "races:last-result";

    public static string PitWindowBaseline(string circuitId) => $"pitWindow:{circuitId}";

    public static string CircuitProfile(string circuitId) => $"circuit:profile:{circuitId}";

    public static string DriverProfile(string driverId) => $"driver:profile:{driverId}";

    public const string AllDrivers = "drivers:all";

    public static string DriverStatsForComparison(string driverId, int? season, string? circuitId) =>
        $"driver:h2h-stats:{driverId}:{season?.ToString() ?? "any"}:{circuitId ?? "any"}";
}
