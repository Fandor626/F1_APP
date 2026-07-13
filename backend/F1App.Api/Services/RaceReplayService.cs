using System.Globalization;
using F1App.Api.Clients;
using F1App.Api.Dtos.OpenF1;
using F1App.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace F1App.Api.Services;

// Builds the per-lap Race Replay frame array (Story 8.2, Architecture AD-2).
// Reuses the exact same historical (explicit session_key) OpenF1Client
// methods RaceDataOrchestrator.EnrichFallbackFromOpenF1Async (Story 8.1)
// already built — no new OpenF1 client surface needed.
//
// Position reconstruction is cumulative-lap-time based (a deliberate,
// user-approved simplification over full GPS/timestamp correlation): a
// driver's position at lap N is their rank by total elapsed race time
// through their own most recently completed lap at or before N. This is
// exactly real lap-time data, not fabricated — it can differ slightly from
// true track position during in-progress pit cycles, where on-track order
// and time-elapsed order briefly diverge.
public class RaceReplayService(
    IErgastClient ergastClient,
    IOpenF1Client openF1Client,
    IMemoryCache cache,
    ILogger<RaceReplayService> logger)
{
    // Historical race data is immutable once published — 7 days, matching
    // the same tier CircuitProfileService/RaceDataOrchestrator's fallback
    // enrichment already use for historical OpenF1/Ergast data.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    public async Task<IReadOnlyList<RaceStateSnapshot>?> GetReplayAsync(int round, CancellationToken ct)
    {
        var cacheKey = CacheKeys.RaceReplay(round);
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<RaceStateSnapshot>? cached))
            return cached;

        var frames = await BuildReplayAsync(round, ct);
        if (frames is not null)
            cache.Set(cacheKey, frames, CacheTtl);

        return frames;
    }

    private async Task<IReadOnlyList<RaceStateSnapshot>?> BuildReplayAsync(int round, CancellationToken ct)
    {
        var raceData = await ergastClient.GetRaceResultsByRoundAsync(round, ct);
        if (raceData is null) return null;

        if (!DateOnly.TryParse(raceData.Date, CultureInfo.InvariantCulture, out var raceDate))
            return null;

        var year = int.Parse(raceData.Season, CultureInfo.InvariantCulture);
        var sessions = await openF1Client.GetRaceSessionsAsync(year, ct);
        var session = sessions.FirstOrDefault(s => DateOnly.FromDateTime(s.DateStart.UtcDateTime) == raceDate);
        if (session is null)
        {
            logger.LogInformation("RaceReplayService: no OpenF1 session found for round {Round} ({Date})", round, raceDate);
            return null;
        }

        var sessionKey = session.SessionKey;

        var lapsTask = openF1Client.GetLapsForSessionAsync(sessionKey, ct);
        var stintsTask = openF1Client.GetStintsForSessionAsync(sessionKey, ct);
        var raceControlTask = openF1Client.GetRaceControlForSessionAsync(sessionKey, ct);
        var pitTask = openF1Client.GetPitStopsForSessionAsync(sessionKey, ct);
        var driversTask = openF1Client.GetDriversForSessionAsync(sessionKey, ct);
        await Task.WhenAll(lapsTask, stintsTask, raceControlTask, pitTask, driversTask);

        var laps = lapsTask.Result.Where(l => l.LapDuration.HasValue).ToList();
        if (laps.Count == 0)
        {
            logger.LogInformation("RaceReplayService: OpenF1 session {SessionKey} has no completed laps", sessionKey);
            return null;
        }

        var stints = stintsTask.Result;
        var sessionDrivers = driversTask.Result.ToDictionary(d => d.DriverNumber);
        var maxLap = laps.Max(l => l.LapNumber);

        var lapsByDriver = laps
            .GroupBy(l => l.DriverNumber)
            .ToDictionary(g => g.Key, g => g.OrderBy(l => l.LapNumber).ToList());

        // Cumulative elapsed race time per driver, keyed by the lap it was
        // reached at (sparse — a driver may not have a completed-lap entry
        // for every lap number if they pitted-out-lap-only or retired).
        var cumulativeByDriver = new Dictionary<int, SortedDictionary<int, double>>();
        foreach (var (driverNum, driverLaps) in lapsByDriver)
        {
            var running = 0.0;
            var map = new SortedDictionary<int, double>();
            foreach (var lap in driverLaps)
            {
                running += lap.LapDuration!.Value;
                map[lap.LapNumber] = running;
            }
            cumulativeByDriver[driverNum] = map;
        }

        var timelineByLap = BuildTimelineByLap(raceControlTask.Result, pitTask.Result, laps, sessionDrivers);

        var frames = new List<RaceStateSnapshot>(maxLap);
        double fastestS1 = double.MaxValue, fastestS2 = double.MaxValue, fastestS3 = double.MaxValue;
        int? fastestS1Driver = null, fastestS2Driver = null, fastestS3Driver = null;
        var lapChart = new Dictionary<int, List<LapTimeEntry>>();
        var timelineSoFar = new List<RaceTimelineEvent>();

        for (var lapNumber = 1; lapNumber <= maxLap; lapNumber++)
        {
            // Rank every driver by cumulative time through their own most
            // recently completed lap at or before this frame's lap number —
            // a retired/lapped driver's cumulative time simply stops
            // advancing, so they correctly fall behind in later frames.
            var ranked = new List<(int DriverNumber, double CumTime, int LastLap)>();
            foreach (var (driverNum, cumMap) in cumulativeByDriver)
            {
                var lastLap = 0;
                foreach (var lap in cumMap.Keys)
                {
                    if (lap > lapNumber) break;
                    lastLap = lap;
                }
                if (lastLap == 0) continue;
                ranked.Add((driverNum, cumMap[lastLap], lastLap));
            }
            // Rank by laps completed first (more laps = ahead), then by
            // cumulative time within the same lap count. Ranking on raw
            // cumulative time alone is wrong: a driver who retired after lap
            // 1 has a *smaller* elapsed time than a driver on lap 20, which
            // would rank the retiree ahead — backwards from reality.
            ranked.Sort((a, b) => a.LastLap != b.LastLap
                ? b.LastLap.CompareTo(a.LastLap)
                : a.CumTime.CompareTo(b.CumTime));

            var driverStates = new List<DriverState>(ranked.Count);
            for (var i = 0; i < ranked.Count; i++)
            {
                var (driverNum, cumTime, lastLap) = ranked[i];
                sessionDrivers.TryGetValue(driverNum, out var info);

                var stint = stints
                    .Where(s => s.DriverNumber == driverNum && s.LapStart <= lastLap)
                    .OrderByDescending(s => s.LapStart)
                    .FirstOrDefault();

                string? gap = i == 0 ? null : FormatGap(cumTime - ranked[i - 1].CumTime);

                driverStates.Add(new DriverState
                {
                    DriverNumber = driverNum,
                    DriverCode = info?.NameAcronym ?? driverNum.ToString(CultureInfo.InvariantCulture),
                    TeamName = info?.TeamName ?? "",
                    TeamColour = info?.TeamColour ?? "555555",
                    Position = i + 1,
                    GapToCarAhead = gap,
                    GapIsStale = false,
                    TyreCompound = stint?.Compound,
                    StintLaps = stint is not null
                        ? stint.TyreAgeAtStart + Math.Max(0, lastLap - stint.LapStart + 1)
                        : null,
                });

                if (!lapChart.TryGetValue(driverNum, out var entries))
                {
                    entries = [];
                    lapChart[driverNum] = entries;
                }
                if (lapsByDriver.TryGetValue(driverNum, out var driverLaps))
                {
                    var lapAtN = driverLaps.FirstOrDefault(l => l.LapNumber == lapNumber);
                    if (lapAtN is not null && entries.Count < lapNumber)
                        entries.Add(new LapTimeEntry(lapAtN.LapNumber, lapAtN.LapDuration, lapAtN.IsPitOutLap));
                }
            }

            // Progressive session-best sectors, updated with this lap's times only.
            foreach (var lap in laps.Where(l => l.LapNumber == lapNumber))
            {
                if (lap.DurationSector1 is { } s1 && s1 < fastestS1) { fastestS1 = s1; fastestS1Driver = lap.DriverNumber; }
                if (lap.DurationSector2 is { } s2 && s2 < fastestS2) { fastestS2 = s2; fastestS2Driver = lap.DriverNumber; }
                if (lap.DurationSector3 is { } s3 && s3 < fastestS3) { fastestS3 = s3; fastestS3Driver = lap.DriverNumber; }
            }

            if (timelineByLap.TryGetValue(lapNumber, out var eventsThisLap))
                timelineSoFar.AddRange(eventsThisLap);

            frames.Add(new RaceStateSnapshot
            {
                CapturedAt = session.DateStart,
                Drivers = [.. driverStates.OrderBy(d => d.Position)],
                LapChart = lapChart.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<LapTimeEntry>)[.. kv.Value]),
                SessionMode = SessionMode.Fallback,
                FallbackRaceName = raceData.RaceName,
                // Left null deliberately: ErgastRaceResultRaceDto (round-result
                // lookups) carries no circuit field, and per AD-1, circuitId is
                // set once by the frontend when replay mode is entered (it
                // already has it from the live/fallback SignalR snapshot), not
                // per-frame — no replay frame needs to carry it.
                CircuitId = null,
                FastestSectors = new FastestSectorBoard(
                    MakeSectorEntry(fastestS1, fastestS1Driver, sessionDrivers),
                    MakeSectorEntry(fastestS2, fastestS2Driver, sessionDrivers),
                    MakeSectorEntry(fastestS3, fastestS3Driver, sessionDrivers)),
                Timeline = [.. timelineSoFar.OrderBy(e => e.LapNumber)],
            });
        }

        logger.LogInformation(
            "RaceReplayService: built {FrameCount} replay frames for round {Round} from OpenF1 session {SessionKey}",
            frames.Count, round, sessionKey);

        return frames;
    }

    private static FastestSectorEntry? MakeSectorEntry(
        double best, int? driverNum, IReadOnlyDictionary<int, OpenF1DriverInfoDto> sessionDrivers)
    {
        if (driverNum is null || best == double.MaxValue) return null;
        sessionDrivers.TryGetValue(driverNum.Value, out var info);
        return new FastestSectorEntry(
            driverNum.Value,
            info?.NameAcronym ?? driverNum.Value.ToString(CultureInfo.InvariantCulture),
            info?.TeamColour ?? "555555",
            best);
    }

    private static string FormatGap(double seconds) =>
        "+" + seconds.ToString("0.000", CultureInfo.InvariantCulture);

    private static Dictionary<int, List<RaceTimelineEvent>> BuildTimelineByLap(
        IReadOnlyList<OpenF1RaceControlDto> raceControl,
        IReadOnlyList<OpenF1PitDto> pitStops,
        IReadOnlyList<OpenF1LapDto> laps,
        IReadOnlyDictionary<int, OpenF1DriverInfoDto> sessionDrivers)
    {
        var byLap = new Dictionary<int, List<RaceTimelineEvent>>();

        void Add(RaceTimelineEvent evt)
        {
            if (!byLap.TryGetValue(evt.LapNumber, out var list))
            {
                list = [];
                byLap[evt.LapNumber] = list;
            }
            list.Add(evt);
        }

        foreach (var msg in raceControl)
        {
            var evt = RaceDataOrchestrator.ParseRaceControlEvent(msg, sessionDrivers);
            if (evt is not null) Add(evt);
        }

        foreach (var pit in pitStops)
        {
            var code = sessionDrivers.TryGetValue(pit.DriverNumber, out var info)
                ? info.NameAcronym
                : pit.DriverNumber.ToString(CultureInfo.InvariantCulture);
            Add(new RaceTimelineEvent(pit.LapNumber, "PitStop", code, null));
        }

        // Progressive fastest-lap markers: a lap earns a "FastestLap" event
        // only if it beats every lap before it (matches the live orchestrator's
        // "new best replaces old" semantics, per-driver-agnostic session best).
        var runningBest = double.MaxValue;
        foreach (var lap in laps.Where(l => l.LapDuration.HasValue).OrderBy(l => l.LapNumber))
        {
            if (lap.LapDuration!.Value >= runningBest) continue;
            runningBest = lap.LapDuration.Value;
            var code = sessionDrivers.TryGetValue(lap.DriverNumber, out var info)
                ? info.NameAcronym
                : lap.DriverNumber.ToString(CultureInfo.InvariantCulture);
            Add(new RaceTimelineEvent(lap.LapNumber, "FastestLap", code, null));
        }

        return byLap;
    }
}
