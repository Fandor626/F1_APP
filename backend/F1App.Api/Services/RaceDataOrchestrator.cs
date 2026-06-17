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
    internal IReadOnlyDictionary<int, OpenF1DriverInfoDto> _driverInfo = new Dictionary<int, OpenF1DriverInfoDto>();

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

            _raceWeekendActive = raceTable.Races.Any(race =>
            {
                var raceDate = DateTime.ParseExact(race.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;
                // Weekend starts at FP1 day; fall back to 2 days before race if no FP1 data yet
                var weekendStart = race.FirstPractice is not null
                    ? DateTime.ParseExact(race.FirstPractice.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date
                    : raceDate.AddDays(-2);
                return today >= weekendStart && today <= raceDate;
            });

            _raceWeekendCheckExpiry = now.AddHours(1);
            logger.LogInformation("RaceDataOrchestrator: race weekend active={Active}", _raceWeekendActive);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail-open: if we can't determine the schedule, assume a race might be on
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "RaceDataOrchestrator: failed to load driver info; will use driver numbers as fallback");
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
            }
        }
    }

    private async Task PublishSnapshotLoopAsync(CancellationToken ct)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var snapshot = BuildSnapshot();
            if (snapshot.Drivers.Count > 0)
            {
                await hubContext.Clients.Group("race").SendAsync("RaceSnapshot", snapshot, ct);
            }
        }
    }

    internal RaceStateSnapshot BuildSnapshot()
    {
        var drivers = new List<DriverState>();

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
            });
        }

        return new RaceStateSnapshot
        {
            CapturedAt = timeProvider.GetUtcNow(),
            Drivers = [.. drivers.OrderBy(d => d.Position)],
        };
    }
}
