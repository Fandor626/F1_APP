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
}
