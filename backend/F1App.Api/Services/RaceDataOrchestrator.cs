using System.Collections.Concurrent;
using System.Globalization;
using F1App.Api.Clients;
using F1App.Api.Dtos.OpenF1;
using F1App.Api.Hubs;
using F1App.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace F1App.Api.Services;

public class RaceDataOrchestrator(
    IHubContext<RaceHub> hubContext,
    IOpenF1Client openF1Client,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<RaceDataOrchestrator> logger) : BackgroundService
{
    private readonly TimeSpan _joinTolerance = TimeSpan.FromMilliseconds(
        configuration.GetValue("JoinToleranceMs", 500));

    internal readonly ConcurrentDictionary<int, OpenF1PositionDto> _latestPositions = new();
    internal readonly ConcurrentDictionary<int, OpenF1IntervalDto> _latestIntervals = new();
    // Latest stint per driver (by highest StintNumber) — full refresh each poll
    internal readonly ConcurrentDictionary<int, OpenF1StintDto> _latestStints = new();
    // Maximum lap_number seen per driver — only ever increases
    internal readonly ConcurrentDictionary<int, int> _driverCurrentLap = new();
    // Per-driver lap time history: driverNumber → (lapNumber → LapTimeEntry)
    // Only stores completed laps (non-null LapDuration)
    internal readonly ConcurrentDictionary<int, ConcurrentDictionary<int, LapTimeEntry>> _driverLapTimes = new();
    internal IReadOnlyDictionary<int, OpenF1DriverInfoDto> _driverInfo = new Dictionary<int, OpenF1DriverInfoDto>();
    internal IReadOnlyList<DriverStanding> _driverStandings = [];
    private Dictionary<string, DriverStanding> _standingByPrefix = new(StringComparer.OrdinalIgnoreCase);

    // Fallback state machine fields
    internal DateTimeOffset _lastValidPositionTime = DateTimeOffset.MinValue;
    internal int _consecutiveGoodPolls = 0;
    internal SessionMode _sessionMode = SessionMode.Live;
    internal IReadOnlyList<DriverState> _fallbackDrivers = [];
    internal string? _fallbackRaceName;

    // Location tracking
    internal readonly ConcurrentDictionary<int, OpenF1LocationDto> _latestLocations = new();
    internal string? _activeCircuitId;
    private DateTimeOffset _lastLocationPoll = DateTimeOffset.MinValue;

    // Sector colour tracking
    private double _sessionBestS1 = double.MaxValue;
    private double _sessionBestS2 = double.MaxValue;
    private double _sessionBestS3 = double.MaxValue;
    internal readonly ConcurrentDictionary<int, double[]> _personalBestSectors = new();
    internal readonly ConcurrentDictionary<int, string> _latestSectorStatus = new();

    // Historical median stint length (laps) for the active circuit, used as the
    // pit-window baseline. Loaded once per session in InitialiseDriverInfoAsync;
    // defaults to PitWindowService.DefaultBaselineLaps until then / on failure.
    internal double _pitWindowBaselineLaps = PitWindowService.DefaultBaselineLaps;

    private DateTimeOffset _lastPositionPoll = DateTimeOffset.MinValue;
    private DateTimeOffset _lastIntervalPoll = DateTimeOffset.MinValue;
    private DateTimeOffset _lastLapPoll = DateTimeOffset.MinValue;

    // Race weekend gate — cached for 1h to avoid hammering Ergast
    private bool _raceWeekendActive;
    private DateTimeOffset _raceWeekendCheckExpiry = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!await IsRaceWeekendActiveAsync(stoppingToken))
            {
                logger.LogInformation("RaceDataOrchestrator: not a race weekend — sleeping 1h");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                continue;
            }

            await InitialiseDriverInfoAsync(stoppingToken);

            // Run all polling loops for up to 24h, then re-evaluate race weekend status.
            // sessionCts is linked to stoppingToken so app shutdown stops all loops immediately.
            using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            sessionCts.CancelAfter(TimeSpan.FromHours(24));

            await Task.WhenAll(
                RunLoopAsync("PositionPoller",  PollPositionAsync,         sessionCts.Token),
                RunLoopAsync("IntervalPoller",  PollIntervalAsync,         sessionCts.Token),
                RunLoopAsync("StintsPoller",    PollStintsAsync,           sessionCts.Token),
                RunLoopAsync("LapsPoller",      PollLapsAsync,             sessionCts.Token),
                RunLoopAsync("LocationPoller",  PollLocationAsync,         sessionCts.Token),
                RunLoopAsync("PublishLoop",     PublishSnapshotLoopAsync,  sessionCts.Token)
            );
        }
    }

    private async Task<bool> IsRaceWeekendActiveAsync(CancellationToken ct)
    {
        if (configuration.GetValue("Polling:ForceActive", false))
            return true;

        var now = timeProvider.GetUtcNow();
        if (now < _raceWeekendCheckExpiry)
            return _raceWeekendActive;

        try
        {
            // IErgastClient is scoped (typed HttpClient); create a short-lived scope for this check
            using var scope = scopeFactory.CreateScope();
            var ergast = scope.ServiceProvider.GetRequiredService<IErgastClient>();
            var raceTable = await ergast.GetCurrentSeasonScheduleAsync(ct);
            var today = now.UtcDateTime.Date;

            _raceWeekendActive = false;
            foreach (var race in raceTable.Races)
            {
                var raceDate = DateTime.ParseExact(race.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;
                // Weekend starts at FP1 day; fall back to 2 days before race if no FP1 data yet
                var weekendStart = race.FirstPractice is not null
                    ? DateTime.ParseExact(race.FirstPractice.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date
                    : raceDate.AddDays(-2);
                if (today >= weekendStart && today <= raceDate)
                {
                    _raceWeekendActive = true;
                    _activeCircuitId = race.Circuit.CircuitId;
                    break;
                }
            }

            _raceWeekendCheckExpiry = now.AddHours(1);
            logger.LogInformation("RaceDataOrchestrator: race weekend active={Active}", _raceWeekendActive);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Fail-open: if we can't determine the schedule, assume a race might be on.
            // HttpClient timeouts throw TaskCanceledException — must not crash the host.
            logger.LogWarning(ex, "RaceDataOrchestrator: failed to check race weekend — assuming active");
            _raceWeekendActive = true;
            _raceWeekendCheckExpiry = now.AddMinutes(5);
        }

        return _raceWeekendActive;
    }

    private async Task InitialiseDriverInfoAsync(CancellationToken ct)
    {
        try
        {
            var drivers = await openF1Client.GetSessionDriversAsync(ct);
            _driverInfo = drivers.ToDictionary(d => d.DriverNumber);
            logger.LogInformation("RaceDataOrchestrator: loaded {Count} drivers for current session", drivers.Count);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "RaceDataOrchestrator: failed to load driver info; will use driver numbers as fallback");
        }

        _driverStandings = [];
        _standingByPrefix = new Dictionary<string, DriverStanding>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var standingsSvc = scope.ServiceProvider.GetRequiredService<StandingsService>();
            _driverStandings = await standingsSvc.GetCurrentDriverStandingsAsync(ct);
            foreach (var s in _driverStandings)
            {
                var prefix = s.DriverName.Length >= 3 ? s.DriverName[..3] : s.DriverName;
                _standingByPrefix.TryAdd(prefix, s);
            }
            logger.LogInformation("RaceDataOrchestrator: loaded {Count} driver standings", _driverStandings.Count);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "RaceDataOrchestrator: failed to load driver standings; championship delta unavailable");
        }

        _pitWindowBaselineLaps = PitWindowService.DefaultBaselineLaps;
        if (_activeCircuitId is not null)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var pitWindowSvc = scope.ServiceProvider.GetRequiredService<PitWindowService>();
                var priorSeason = timeProvider.GetUtcNow().Year - 1;
                _pitWindowBaselineLaps = await pitWindowSvc.GetBaselineMedianStintLapsAsync(_activeCircuitId, priorSeason, ct);
                logger.LogInformation("RaceDataOrchestrator: pit window baseline for {CircuitId} = {Baseline} laps", _activeCircuitId, _pitWindowBaselineLaps);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "RaceDataOrchestrator: failed to load pit window baseline; using default");
            }
        }
    }

    private async Task PollLocationAsync(CancellationToken ct)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(800));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var locations = await openF1Client.GetLatestLocationsAsync(_lastLocationPoll, ct);
            if (locations.Count > 0)
                _lastLocationPoll = timeProvider.GetUtcNow();

            foreach (var loc in locations)
            {
                _latestLocations.AddOrUpdate(
                    loc.DriverNumber,
                    loc,
                    (_, existing) => loc.Date > existing.Date ? loc : existing);
            }
        }
    }

    internal SessionMode EvaluateSessionMode()
    {
        switch (_sessionMode)
        {
            case SessionMode.Fallback:
                if (_consecutiveGoodPolls >= 1)
                    return SessionMode.Stale;
                return SessionMode.Fallback;

            case SessionMode.Stale:
                if (_consecutiveGoodPolls >= 4)
                    return SessionMode.Live;
                if (_lastValidPositionTime != DateTimeOffset.MinValue &&
                    (timeProvider.GetUtcNow() - _lastValidPositionTime).TotalSeconds > 20)
                    return SessionMode.Fallback;
                return SessionMode.Stale;

            default: // Live
                if (_lastValidPositionTime == DateTimeOffset.MinValue)
                    return SessionMode.Live;
                var elapsed = (timeProvider.GetUtcNow() - _lastValidPositionTime).TotalSeconds;
                if (elapsed > 20) return SessionMode.Fallback;
                if (elapsed > 10) return SessionMode.Stale;
                return SessionMode.Live;
        }
    }

    private async Task LoadFallbackDataAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var ergast = scope.ServiceProvider.GetRequiredService<IErgastClient>();
            var raceData = await ergast.GetLastRaceResultsAsync(ct);
            if (raceData is null) return;

            _fallbackRaceName = raceData.RaceName;
            _fallbackDrivers = raceData.Results
                .Select((result, idx) => new DriverState
                {
                    DriverNumber = int.TryParse(result.Number, out var num) ? num : idx + 1,
                    DriverCode = result.Driver.Code
                        ?? result.Driver.FamilyName[..Math.Min(3, result.Driver.FamilyName.Length)].ToUpperInvariant(),
                    TeamName = result.Constructor.Name,
                    TeamColour = "555555",
                    Position = int.TryParse(result.Position, out var pos) ? pos : idx + 1,
                    GapToCarAhead = idx == 0 ? null : result.Time?.Time,
                    GapIsStale = false,
                    TyreCompound = null,
                    StintLaps = null,
                    ChampionshipDelta = null,
                })
                .ToList();

            logger.LogInformation("RaceDataOrchestrator: loaded fallback data — {RaceName}, {Count} drivers",
                _fallbackRaceName, _fallbackDrivers.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "RaceDataOrchestrator: failed to load fallback race data; fallback drivers will be empty");
        }
    }

    private async Task RunLoopAsync(string name, Func<CancellationToken, Task> loop, CancellationToken ct)
    {
        var backoff = TimeSpan.FromSeconds(1);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await loop(ct);
                backoff = TimeSpan.FromSeconds(1); // reset on clean exit
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (HttpRequestException ex) when ((int?)ex.StatusCode == 429)
            {
                logger.LogWarning("RaceDataOrchestrator: {LoopName} rate-limited (429) — pausing 60s", name);
                try { await Task.Delay(TimeSpan.FromSeconds(60), ct); }
                catch (OperationCanceledException) { return; }
            }
            catch (HttpRequestException ex) when ((int?)ex.StatusCode == 401)
            {
                logger.LogWarning("RaceDataOrchestrator: {LoopName} received 401 Unauthorized from OpenF1 — endpoint may require auth; pausing 5m", name);
                try { await Task.Delay(TimeSpan.FromMinutes(5), ct); }
                catch (OperationCanceledException) { return; }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RaceDataOrchestrator: {LoopName} crashed — retrying in {Backoff}", name, backoff);
                try { await Task.Delay(backoff, ct); }
                catch (OperationCanceledException) { return; }
                backoff = backoff < TimeSpan.FromSeconds(60) ? backoff + backoff : TimeSpan.FromSeconds(60);
            }
        }
    }

    private async Task PollPositionAsync(CancellationToken ct)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(800));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var positions = await openF1Client.GetLatestPositionsAsync(_lastPositionPoll, ct);
            if (positions.Count > 0)
                _lastPositionPoll = timeProvider.GetUtcNow();

            foreach (var pos in positions)
            {
                _latestPositions.AddOrUpdate(
                    pos.DriverNumber,
                    pos,
                    (_, existing) => pos.Date > existing.Date ? pos : existing);
            }

            if (positions.Count > 0)
            {
                _lastValidPositionTime = timeProvider.GetUtcNow();
                _consecutiveGoodPolls = _consecutiveGoodPolls + 1;
            }
            else
            {
                if (_lastValidPositionTime == DateTimeOffset.MinValue)
                    _lastValidPositionTime = timeProvider.GetUtcNow();
                _consecutiveGoodPolls = 0;
            }
        }
    }

    private async Task PollIntervalAsync(CancellationToken ct)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(900));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var intervals = await openF1Client.GetLatestIntervalsAsync(_lastIntervalPoll, ct);
            if (intervals.Count > 0)
                _lastIntervalPoll = timeProvider.GetUtcNow();

            foreach (var interval in intervals)
            {
                _latestIntervals.AddOrUpdate(
                    interval.DriverNumber,
                    interval,
                    (_, existing) => interval.Date > existing.Date ? interval : existing);
            }
        }
    }

    private async Task PollStintsAsync(CancellationToken ct)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var stints = await openF1Client.GetLatestStintsAsync(ct);
            // Full refresh: rebuild from the authoritative batch so session transitions
            // (where StintNumber resets to 1) replace stale prior-session entries cleanly.
            foreach (var (driverNum, latestStint) in stints
                .GroupBy(s => s.DriverNumber)
                .Select(g => (g.Key, g.MaxBy(s => s.StintNumber)!)))
            {
                _latestStints[driverNum] = latestStint;
            }
        }
    }

    private async Task PollLapsAsync(CancellationToken ct)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var laps = await openF1Client.GetLatestLapsAsync(_lastLapPoll, ct);
            if (laps.Count > 0)
                _lastLapPoll = timeProvider.GetUtcNow();

            foreach (var lap in laps)
            {
                _driverCurrentLap.AddOrUpdate(
                    lap.DriverNumber,
                    lap.LapNumber,
                    (_, existing) => lap.LapNumber > existing ? lap.LapNumber : existing);

                if (lap.LapDuration.HasValue)
                {
                    var driverLaps = _driverLapTimes.GetOrAdd(lap.DriverNumber, _ => new());
                    driverLaps[lap.LapNumber] = new LapTimeEntry(lap.LapNumber, lap.LapDuration, lap.IsPitOutLap);
                }

                ComputeSectorStatus(lap);
            }
        }
    }

    internal void ComputeSectorStatus(OpenF1LapDto lap)
    {
        if (lap.IsPitOutLap)
        {
            _latestSectorStatus[lap.DriverNumber] = "white";
            return;
        }

        double? sectorTime = null;
        int sectorIndex = 0;

        if (lap.DurationSector3.HasValue) { sectorTime = lap.DurationSector3; sectorIndex = 2; }
        else if (lap.DurationSector2.HasValue) { sectorTime = lap.DurationSector2; sectorIndex = 1; }
        else if (lap.DurationSector1.HasValue) { sectorTime = lap.DurationSector1; sectorIndex = 0; }

        if (!sectorTime.HasValue) return;

        var time = sectorTime.Value;

        bool isSessionBest;
        if (sectorIndex == 0) { isSessionBest = time < _sessionBestS1; if (isSessionBest) _sessionBestS1 = time; }
        else if (sectorIndex == 1) { isSessionBest = time < _sessionBestS2; if (isSessionBest) _sessionBestS2 = time; }
        else { isSessionBest = time < _sessionBestS3; if (isSessionBest) _sessionBestS3 = time; }

        var personal = _personalBestSectors.GetOrAdd(lap.DriverNumber, _ => [double.MaxValue, double.MaxValue, double.MaxValue]);
        var isPersonalBest = time < personal[sectorIndex];
        if (isPersonalBest) personal[sectorIndex] = time;

        _latestSectorStatus[lap.DriverNumber] = isSessionBest ? "purple" : isPersonalBest ? "green" : "yellow";
    }

    private async Task PublishSnapshotLoopAsync(CancellationToken ct)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var newMode = EvaluateSessionMode();

            if (newMode == SessionMode.Fallback && _sessionMode != SessionMode.Fallback
                && _fallbackDrivers.Count == 0)
            {
                await LoadFallbackDataAsync(ct);
            }

            _sessionMode = newMode;

            var snapshot = BuildSnapshot();
            if (snapshot.Drivers.Count > 0)
            {
                await hubContext.Clients.Group("race").SendAsync("RaceSnapshot", snapshot, ct);
            }
        }
    }

    internal RaceStateSnapshot BuildSnapshot()
    {
        List<DriverState> drivers;

        if (_sessionMode == SessionMode.Fallback && _fallbackDrivers.Count > 0)
        {
            drivers = [.. _fallbackDrivers];
        }
        else
        {
            drivers = new List<DriverState>();

            foreach (var (driverNum, pos) in _latestPositions)
            {
                string? gap = null;
                bool gapIsStale = true;

                if (_latestIntervals.TryGetValue(driverNum, out var interval))
                {
                    var timeDiffMs = Math.Abs((pos.Date - interval.Date).TotalMilliseconds);
                    gapIsStale = timeDiffMs > _joinTolerance.TotalMilliseconds;
                    gap = gapIsStale ? null : interval.GapToCarAhead;
                }

                string? tyreCompound = null;
                int? stintLaps = null;

                if (_latestStints.TryGetValue(driverNum, out var stint))
                {
                    tyreCompound = stint.Compound;
                    if (_driverCurrentLap.TryGetValue(driverNum, out var currentLap))
                    {
                        // Total tyre age = age when this set was new + laps completed in this stint
                        stintLaps = stint.TyreAgeAtStart + Math.Max(0, currentLap - stint.LapStart + 1);
                    }
                }

                _driverInfo.TryGetValue(driverNum, out var info);

                double? x = null, y = null;
                if (_latestLocations.TryGetValue(driverNum, out var loc))
                {
                    x = loc.X;
                    y = loc.Y;
                }

                _latestSectorStatus.TryGetValue(driverNum, out var sectorStatus);

                var pitWindowActive = false;
                if (stintLaps.HasValue && tyreCompound is not null)
                {
                    var (min, max) = PitWindowService.ComputeWindow(_pitWindowBaselineLaps, tyreCompound);
                    pitWindowActive = stintLaps.Value >= min && stintLaps.Value <= max;
                }

                drivers.Add(new DriverState
                {
                    DriverNumber = driverNum,
                    DriverCode = info?.NameAcronym ?? driverNum.ToString(),
                    TeamName = info?.TeamName ?? "",
                    TeamColour = info?.TeamColour ?? "555555",
                    Position = pos.Position,
                    GapToCarAhead = gap,
                    GapIsStale = gapIsStale,
                    TyreCompound = tyreCompound,
                    StintLaps = stintLaps,
                    X = x,
                    Y = y,
                    MiniSectorStatus = sectorStatus,
                    PitWindowActive = pitWindowActive,
                });
            }

            if (_driverStandings.Count > 0)
            {
                var deltaMap = ComputeChampionshipDeltas(drivers);
                for (int i = 0; i < drivers.Count; i++)
                    drivers[i] = drivers[i] with { ChampionshipDelta = deltaMap.GetValueOrDefault(drivers[i].DriverNumber) };
            }
        }

        var lapChart = new Dictionary<int, IReadOnlyList<LapTimeEntry>>();
        foreach (var (driverNum, lapsByLap) in _driverLapTimes)
        {
            if (_latestPositions.ContainsKey(driverNum))
                lapChart[driverNum] = [.. lapsByLap.Values.OrderBy(l => l.LapNumber)];
        }

        return new RaceStateSnapshot
        {
            CapturedAt = timeProvider.GetUtcNow(),
            Drivers = [.. drivers.OrderBy(d => d.Position)],
            LapChart = lapChart,
            SessionMode = _sessionMode,
            FallbackRaceName = _fallbackRaceName,
            CircuitId = _activeCircuitId,
        };
    }

    internal static int RacePointsForPosition(int position) => position switch
    {
        1 => 25, 2 => 18, 3 => 15, 4 => 12, 5 => 10,
        6 => 8,  7 => 6,  8 => 4,  9 => 2,  10 => 1,
        _ => 0
    };

    private Dictionary<int, string?> ComputeChampionshipDeltas(List<DriverState> raceDrivers)
    {
        // _standingByPrefix is pre-built in InitialiseDriverInfoAsync in production;
        // build lazily here to support test scenarios where _driverStandings is set directly.
        if (_standingByPrefix.Count == 0)
        {
            foreach (var s in _driverStandings)
            {
                var prefix = s.DriverName.Length >= 3 ? s.DriverName[..3] : s.DriverName;
                _standingByPrefix.TryAdd(prefix, s);
            }
        }

        var projected = new List<(int DriverNumber, decimal ProjectedPoints)>();
        foreach (var driver in raceDrivers)
        {
            if (!_driverInfo.TryGetValue(driver.DriverNumber, out var info)) continue;
            if (!_standingByPrefix.TryGetValue(info.NameAcronym, out var standing)) continue;

            projected.Add((driver.DriverNumber, standing.Points + RacePointsForPosition(driver.Position)));
        }

        projected.Sort((a, b) => b.ProjectedPoints.CompareTo(a.ProjectedPoints));

        var result = new Dictionary<int, string?>();
        for (int i = 0; i < projected.Count; i++)
        {
            var (driverNum, pts) = projected[i];
            if (i == 0)
            {
                if (projected.Count > 1)
                {
                    var lead = pts - projected[1].ProjectedPoints;
                    result[driverNum] = $"+{lead:0.#}";
                }
            }
            else
            {
                var trail = projected[i - 1].ProjectedPoints - pts;
                result[driverNum] = trail == 0m ? "=0" : $"−{trail:0.#}";
            }
        }

        return result;
    }
}
