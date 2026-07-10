using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using Microsoft.Extensions.Caching.Memory;

namespace F1App.Api.Services;

public class PitWindowService(IErgastClient ergastClient, IMemoryCache cache, ILogger<PitWindowService> logger)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    // Fallback baseline (laps) used when no historical pit-stop data is available
    // for this circuit (new circuit on the calendar, or an Ergast failure).
    // Reflects a typical modern F1 one-stop/two-stop stint length.
    public const double DefaultBaselineLaps = 20.0;

    // Ergast pit-stop data has no tyre-compound breakdown (compound tracking only
    // exists in OpenF1, which has no historical archive) — these multipliers apply
    // a compound-specific spread on top of the circuit's historical baseline stint
    // length, reflecting real-world degradation ordering: soft < medium < hard.
    private static readonly Dictionary<string, double> CompoundFactor = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SOFT"] = 0.8,
        ["MEDIUM"] = 1.0,
        ["HARD"] = 1.25,
        ["INTERMEDIATE"] = 0.9,
        ["WET"] = 0.9,
    };

    public async Task<double> GetBaselineMedianStintLapsAsync(string circuitId, int priorSeason, CancellationToken ct)
    {
        var cacheKey = CacheKeys.PitWindowBaseline(circuitId);
        if (cache.TryGetValue(cacheKey, out double cached))
            return cached;

        var baseline = DefaultBaselineLaps;
        try
        {
            var pitStops = await ergastClient.GetCircuitPitStopsAsync(priorSeason, circuitId, ct);
            var stintLengths = ComputeStintLengths(pitStops);
            if (stintLengths.Count > 0)
                baseline = Median(stintLengths);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "PitWindowService: failed to load historical pit stops for {CircuitId}; using default baseline", circuitId);
        }

        cache.Set(cacheKey, baseline, CacheTtl);
        return baseline;
    }

    // Derives per-driver stint lengths from a race's pit-stop list: the lap gap
    // between the race start (lap 0) and stop 1, between stop 1 and stop 2, etc.
    // Every gap is a completed stint length regardless of compound.
    internal static List<double> ComputeStintLengths(IReadOnlyList<ErgastPitStopDto> pitStops)
    {
        var lengths = new List<double>();
        foreach (var group in pitStops.GroupBy(p => p.DriverId))
        {
            var stops = group
                .Select(p => (Stop: int.TryParse(p.Stop, out var s) ? s : 0, Lap: int.TryParse(p.Lap, out var l) ? l : 0))
                .Where(x => x.Stop > 0 && x.Lap > 0)
                .OrderBy(x => x.Stop)
                .ToList();

            var previousLap = 0;
            foreach (var (_, lap) in stops)
            {
                lengths.Add(lap - previousLap);
                previousLap = lap;
            }
        }
        return lengths;
    }

    private static double Median(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
    }

    // Pure function: the historically typical pit-lap window for a given compound,
    // scaled off the circuit's median historical stint length. Compound- and
    // circuit-specific, not a fixed lap number (AC 3).
    internal static (double Min, double Max) ComputeWindow(double baselineLaps, string? compound)
    {
        var factor = compound is not null && CompoundFactor.TryGetValue(compound, out var f) ? f : 1.0;
        var centre = baselineLaps * factor;
        return (centre * 0.85, centre * 1.15);
    }
}
