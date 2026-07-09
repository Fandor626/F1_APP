---
baseline_commit: ""
---

# Story 2.5: Fallback to Last Race

Status: ready-for-dev

## Story

As a fan checking the app outside a live session,
I want the live race page to show the most recently completed race instead of an empty page,
So that the page is always useful.

## Acceptance Criteria

1. **Given** no live Session is in progress **When** the live race page loads **Then** it shows the most recently completed race in static/replay view using the same gap list, tyre tracker, lap chart, and championship-impact components, populated from historical data, clearly labelled as a past race.
2. **Given** OpenF1 becomes unavailable mid-session (HTTP timeout >5s, empty driver array, or stale timestamp >10-20s) **When** detected **Then** the page transitions through the live→stale→fallback-to-last-race state machine rather than showing an empty/error state.
3. **Given** the stream recovers **When** 3-5 consecutive valid responses arrive **Then** the page transitions back to live, debounced to prevent flapping (Fallback→Stale on first good poll, Stale→Live after 4 consecutive good polls).
4. **Given** the page is in fallback mode **When** rendered **Then** a banner clearly labels the displayed data as a past race (showing race name), distinct from the live connection status widget.
5. **Given** the page is in stale mode (data is 10–20s old) **When** rendered **Then** a warning banner indicates the connection is unstable and data may be delayed.
6. **Given** no Ergast data is available for the last race **When** entering fallback **Then** the page shows GapList's existing "Waiting for race data…" empty state — no crash.

## Tasks / Subtasks

### Task 1: Add `SessionMode` enum and update `RaceStateSnapshot` (AC: 2, 3, 4)

- [ ] Create `backend/F1App.Api/Models/SessionMode.cs`:
  ```csharp
  namespace F1App.Api.Models;
  
  public enum SessionMode { Live, Stale, Fallback }
  ```

- [ ] Update `backend/F1App.Api/Models/RaceStateSnapshot.cs` — add two new properties (with defaults so existing tests compile unchanged):
  ```csharp
  public SessionMode SessionMode { get; init; } = SessionMode.Live;
  public string? FallbackRaceName { get; init; }
  ```
  Full updated record:
  ```csharp
  namespace F1App.Api.Models;
  
  public record RaceStateSnapshot
  {
      public DateTimeOffset CapturedAt { get; init; }
      public IReadOnlyList<DriverState> Drivers { get; init; } = [];
      public IReadOnlyDictionary<int, IReadOnlyList<LapTimeEntry>> LapChart { get; init; }
          = new Dictionary<int, IReadOnlyList<LapTimeEntry>>();
      public SessionMode SessionMode { get; init; } = SessionMode.Live;
      public string? FallbackRaceName { get; init; }
  }
  ```

### Task 2: Extend Ergast DTOs and client for last-race data (AC: 1, 6)

- [ ] Update `backend/F1App.Api/Dtos/Ergast/ErgastStandingsResponseDto.cs` — add `Code` (optional) to `ErgastDriverDto` without breaking existing positional-constructor call sites (add default `null`):
  ```csharp
  public record ErgastDriverDto(
      [property: JsonPropertyName("driverId")] string DriverId,
      [property: JsonPropertyName("givenName")] string GivenName,
      [property: JsonPropertyName("familyName")] string FamilyName,
      [property: JsonPropertyName("code")] string? Code = null);
  ```
  **Why:** `ErgastResultDto.Driver` references this shared record. In fallback snapshot building we need the 3-letter code (e.g. "HAM") for `DriverCode` without having OpenF1 driver info available.

- [ ] Update `backend/F1App.Api/Dtos/Ergast/ErgastRaceResultResponseDto.cs` — add `RaceName` and `Date` to `ErgastRaceResultRaceDto`, and `Position` + `Number` to `ErgastResultDto` (all with defaults so existing `GetCircuitResultsAsync` tests that don't provide these fields still pass):
  ```csharp
  public record ErgastRaceResultRaceDto(
      [property: JsonPropertyName("raceName")] string RaceName = "",
      [property: JsonPropertyName("date")] string Date = "",
      [property: JsonPropertyName("Results")] IReadOnlyList<ErgastResultDto> Results = default!);
  
  public record ErgastResultDto(
      [property: JsonPropertyName("position")] string? Position,
      [property: JsonPropertyName("number")] string? Number,
      [property: JsonPropertyName("Driver")] ErgastDriverDto Driver,
      [property: JsonPropertyName("Constructor")] ErgastConstructorDto Constructor,
      [property: JsonPropertyName("Time")] ErgastResultTimeDto? Time,
      [property: JsonPropertyName("status")] string? Status = null);
  ```
  **Note:** `ErgastRaceResultRaceDto` currently uses a positional constructor (`Results` is the only param). Adding `RaceName` and `Date` BEFORE `Results` with defaults makes JSON deserialization work (System.Text.Json uses `[JsonPropertyName]` attributes, not order).
  
  **Existing usage check:** `GetCircuitResultsAsync` in `ErgastClient.cs` accesses only `response.MRData.RaceTable.Races[0].Results` — unaffected. `RaceScheduleServiceTests.cs` accesses `ErgastDriverDto` via positional constructor `new ErgastDriverDto("id", "Given", familyName)` — `Code` defaults to `null`, so this still compiles.

- [ ] Update `backend/F1App.Api/Clients/IErgastClient.cs` — add:
  ```csharp
  Task<ErgastRaceResultRaceDto?> GetLastRaceResultsAsync(CancellationToken cancellationToken);
  ```

- [ ] Implement `GetLastRaceResultsAsync` in `backend/F1App.Api/Clients/ErgastClient.cs`:
  ```csharp
  public async Task<ErgastRaceResultRaceDto?> GetLastRaceResultsAsync(CancellationToken cancellationToken)
  {
      var response = await httpClient.GetFromJsonAsync<ErgastRaceResultResponseDto>(
          "current/last/results.json", cancellationToken)
          ?? throw new InvalidOperationException("Ergast returned empty response for last race results.");
  
      return response.MRData.RaceTable.Races.Count == 0
          ? null
          : response.MRData.RaceTable.Races[0];
  }
  ```
  **Ergast endpoint:** `current/last/results.json` returns full results for the most recently completed race of the current season. Returns `[]` for `Races` if no race has been completed yet (e.g. pre-season).

- [ ] Add contract test in `backend/F1App.Api.Tests/Clients/ErgastClientContractTests.cs`:
  ```csharp
  [Fact]
  public async Task GetLastRaceResultsAsync_ParsesRaceNameAndResultsFromCurrentLastJson()
  {
      _server
          .Given(Request.Create().WithPath("/current/last/results.json").UsingGet())
          .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
          {
              MRData = new
              {
                  RaceTable = new
                  {
                      Races = new[]
                      {
                          new
                          {
                              raceName = "Canadian Grand Prix",
                              date = "2026-06-08",
                              Results = new[]
                              {
                                  new
                                  {
                                      position = "1",
                                      number = "4",
                                      Driver = new { driverId = "norris", givenName = "Lando", familyName = "Norris", code = "NOR" },
                                      Constructor = new { constructorId = "mclaren", name = "McLaren" },
                                      Time = new { time = "1:32:13.576" },
                                      status = "Finished",
                                  },
                                  new
                                  {
                                      position = "2",
                                      number = "81",
                                      Driver = new { driverId = "piastri", givenName = "Oscar", familyName = "Piastri", code = "PIA" },
                                      Constructor = new { constructorId = "mclaren", name = "McLaren" },
                                      Time = new { time = "+5.014" },
                                      status = "Finished",
                                  },
                              },
                          },
                      },
                  },
              },
          }));
  
      using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
      var client = new ErgastClient(httpClient);
  
      var result = await client.GetLastRaceResultsAsync(CancellationToken.None);
  
      Assert.NotNull(result);
      Assert.Equal("Canadian Grand Prix", result.RaceName);
      Assert.Equal(2, result.Results.Count);
      Assert.Equal("1", result.Results[0].Position);
      Assert.Equal("NOR", result.Results[0].Driver.Code);
      Assert.Equal("McLaren", result.Results[0].Constructor.Name);
      Assert.Equal("+5.014", result.Results[1].Time?.Time);
  }
  
  [Fact]
  public async Task GetLastRaceResultsAsync_ReturnsNullWhenNoCompletedRaceThisSeason()
  {
      _server
          .Given(Request.Create().WithPath("/current/last/results.json").UsingGet())
          .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
          {
              MRData = new { RaceTable = new { Races = Array.Empty<object>() } },
          }));
  
      using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
      var client = new ErgastClient(httpClient);
  
      var result = await client.GetLastRaceResultsAsync(CancellationToken.None);
  
      Assert.Null(result);
  }
  ```

### Task 3: Add `LastRaceResult` model and REST endpoint (AC: 1)

- [ ] Create `backend/F1App.Api/Models/LastRaceResult.cs`:
  ```csharp
  namespace F1App.Api.Models;
  
  public record LastRaceResult(
      string RaceName,
      string RaceDate,
      IReadOnlyList<DriverState> Drivers);
  ```

- [ ] Add cache key in `backend/F1App.Api/Services/CacheKeys.cs`:
  ```csharp
  public const string LastRaceResult = "races:last-result";
  ```

- [ ] Add `GetLastRaceResultAsync` to `backend/F1App.Api/Services/RaceScheduleService.cs`. Insert after `GetRaceDetailAsync`:
  ```csharp
  public async Task<LastRaceResult?> GetLastRaceResultAsync(CancellationToken cancellationToken)
  {
      if (cache.TryGetValue(CacheKeys.LastRaceResult, out LastRaceResult? cached) && cached is not null)
          return cached;
  
      var raceData = await ergastClient.GetLastRaceResultsAsync(cancellationToken);
      if (raceData is null) return null;
  
      var drivers = raceData.Results
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
  
      var lastResult = new LastRaceResult(raceData.RaceName, raceData.Date, drivers);
      cache.Set(CacheKeys.LastRaceResult, lastResult, ResultsCacheTtl);
      return lastResult;
  }
  ```
  **Notes:**
  - `DriverCode` falls back to first-3-chars-uppercase of family name when `Code` is null (pre-2014 Ergast records may omit it; current 2025/2026 results always include it).
  - `GapToCarAhead` for P1 is `null` (no car ahead); for P2+ uses `Time.time` which is the gap to race leader (e.g. "+5.014"). This is "gap to leader" not "gap to car ahead" — the `isFallback` banner tells users this is historical replay.
  - `TeamColour = "555555"` (neutral grey) — Ergast race results don't include team hex colours. The team dot in `DriverRow` will be grey in fallback mode. This is acceptable for POC.
  - Cache uses `ResultsCacheTtl` (7 days) — last race results are immutable once published.

- [ ] Add endpoint in `backend/F1App.Api/Controllers/RacesController.cs`:
  ```csharp
  [HttpGet("last-result")]
  public async Task<ActionResult<LastRaceResult>> GetLastResult(CancellationToken cancellationToken)
  {
      var result = await raceScheduleService.GetLastRaceResultAsync(cancellationToken);
      return result is null ? NoContent() : Ok(result);
  }
  ```

### Task 4: Implement fallback state machine in `RaceDataOrchestrator` (AC: 2, 3)

- [ ] Add tracking fields to `backend/F1App.Api/Services/RaceDataOrchestrator.cs` (after the existing `_standingByPrefix` field):
  ```csharp
  // Fallback state machine fields
  internal DateTimeOffset _lastValidPositionTime = DateTimeOffset.MinValue; // MinValue = not yet tracked
  internal int _consecutiveGoodPolls = 0;
  internal SessionMode _sessionMode = SessionMode.Live;
  internal IReadOnlyList<DriverState> _fallbackDrivers = [];
  internal string? _fallbackRaceName;
  ```
  **Why internal?** `InternalsVisibleTo("F1App.Api.Tests")` in `Program.cs` makes these directly settable in tests without reflection.

- [ ] Update `PollPositionAsync` to track valid data and good poll counter. After:
  ```csharp
  foreach (var pos in positions)
  { ... }
  ```
  Add position tracking:
  ```csharp
  if (positions.Count > 0)
  {
      _lastValidPositionTime = timeProvider.GetUtcNow();
      _consecutiveGoodPolls = _consecutiveGoodPolls + 1;
  }
  else
  {
      // First empty poll starts the staleness clock
      if (_lastValidPositionTime == DateTimeOffset.MinValue)
          _lastValidPositionTime = timeProvider.GetUtcNow();
      _consecutiveGoodPolls = 0;
  }
  ```

- [ ] Add `internal SessionMode EvaluateSessionMode()` after `InitialiseDriverInfoAsync`:
  ```csharp
  internal SessionMode EvaluateSessionMode()
  {
      switch (_sessionMode)
      {
          case SessionMode.Fallback:
              // Recovery path: first valid response → stale
              if (_consecutiveGoodPolls >= 1)
                  return SessionMode.Stale;
              return SessionMode.Fallback;
  
          case SessionMode.Stale:
              // Recovery path: 4 consecutive valid → live
              if (_consecutiveGoodPolls >= 4)
                  return SessionMode.Live;
              // Re-degrade if data stays stale for >20s (prevents getting stuck in Stale)
              if (_lastValidPositionTime != DateTimeOffset.MinValue &&
                  (timeProvider.GetUtcNow() - _lastValidPositionTime).TotalSeconds > 20)
                  return SessionMode.Fallback;
              return SessionMode.Stale;
  
          default: // Live
              if (_lastValidPositionTime == DateTimeOffset.MinValue)
                  return SessionMode.Live; // startup grace — don't fallback before first poll
              var elapsed = (timeProvider.GetUtcNow() - _lastValidPositionTime).TotalSeconds;
              if (elapsed > 20) return SessionMode.Fallback;
              if (elapsed > 10) return SessionMode.Stale;
              return SessionMode.Live;
      }
  }
  ```

- [ ] Add `LoadFallbackDataAsync` private method after `EvaluateSessionMode`:
  ```csharp
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
  ```
  **Note:** Uses `IErgastClient` (scoped, via `IHttpClientFactory`) through `IServiceScopeFactory` — same pattern as `IsRaceWeekendActiveAsync`. Does NOT use `RaceScheduleService` here to avoid the cache (the orchestrator's fallback runs at a different TTL rhythm than the REST endpoint's 7-day cache).

- [ ] Update `PublishSnapshotLoopAsync` to evaluate session mode and load fallback data on transition:
  ```csharp
  private async Task PublishSnapshotLoopAsync(CancellationToken ct)
  {
      var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
      while (await timer.WaitForNextTickAsync(ct))
      {
          var newMode = EvaluateSessionMode();
          
          // On transition to Fallback: load last race data if not already cached
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
  ```
  **Existing line removed:** `private async Task PublishSnapshotLoopAsync(CancellationToken ct)` currently is:
  ```csharp
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
  ```
  Replace entirely with the version above.

- [ ] Update `BuildSnapshot` to branch on session mode. Replace the existing `var drivers = new List<DriverState>(); foreach (var (driverNum, pos) in _latestPositions) { ... }` block and the existing `if (_driverStandings.Count > 0)` block with:
  ```csharp
  internal RaceStateSnapshot BuildSnapshot()
  {
      List<DriverState> drivers;
  
      if (_sessionMode == SessionMode.Fallback && _fallbackDrivers.Count > 0)
      {
          // Fallback mode: use pre-built historical driver list from last Ergast race
          drivers = [.. _fallbackDrivers];
      }
      else
      {
          // Live or Stale mode: build from live OpenF1 polling data
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
      };
  }
  ```
  **`LapChart` in fallback mode:** will be empty (no live laps being tracked). `LapTimeChart` will show "Waiting for lap data…" — acceptable for historical replay in the POC.

### Task 5: Backend tests for state machine (AC: 2, 3)

- [ ] Add to `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs` — state machine tests:
  ```csharp
  // EvaluateSessionMode tests
  [Fact]
  public void EvaluateSessionMode_NoDataYet_ReturnsLive()
  {
      var sut = CreateOrchestrator();
      // _lastValidPositionTime is DateTimeOffset.MinValue by default
      Assert.Equal(SessionMode.Live, sut.EvaluateSessionMode());
  }
  
  [Fact]
  public void EvaluateSessionMode_RecentData_ReturnsLive()
  {
      var now = DateTimeOffset.UtcNow;
      var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
      sut._lastValidPositionTime = now.AddSeconds(-5);
      Assert.Equal(SessionMode.Live, sut.EvaluateSessionMode());
  }
  
  [Fact]
  public void EvaluateSessionMode_Data11sAgo_ReturnsStale()
  {
      var now = DateTimeOffset.UtcNow;
      var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
      sut._lastValidPositionTime = now.AddSeconds(-11);
      Assert.Equal(SessionMode.Stale, sut.EvaluateSessionMode());
  }
  
  [Fact]
  public void EvaluateSessionMode_Data21sAgo_ReturnsFallback()
  {
      var now = DateTimeOffset.UtcNow;
      var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
      sut._lastValidPositionTime = now.AddSeconds(-21);
      Assert.Equal(SessionMode.Fallback, sut.EvaluateSessionMode());
  }
  
  [Fact]
  public void EvaluateSessionMode_RecoveryFromFallbackFirstGoodPoll_ReturnsStale()
  {
      var now = DateTimeOffset.UtcNow;
      var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
      sut._lastValidPositionTime = now.AddSeconds(-25);
      sut._sessionMode = SessionMode.Fallback;
      sut._consecutiveGoodPolls = 1;
      Assert.Equal(SessionMode.Stale, sut.EvaluateSessionMode());
  }
  
  [Fact]
  public void EvaluateSessionMode_RecoveryFromStale4GoodPolls_ReturnsLive()
  {
      var now = DateTimeOffset.UtcNow;
      var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
      sut._lastValidPositionTime = now.AddSeconds(-5);
      sut._sessionMode = SessionMode.Stale;
      sut._consecutiveGoodPolls = 4;
      Assert.Equal(SessionMode.Live, sut.EvaluateSessionMode());
  }
  
  [Fact]
  public void EvaluateSessionMode_StaleWith3GoodPolls_StaysStale()
  {
      // Need 4 good polls; 3 is not enough to recover to Live
      var now = DateTimeOffset.UtcNow;
      var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
      sut._lastValidPositionTime = now.AddSeconds(-5);
      sut._sessionMode = SessionMode.Stale;
      sut._consecutiveGoodPolls = 3;
      Assert.Equal(SessionMode.Stale, sut.EvaluateSessionMode());
  }
  
  [Fact]
  public void EvaluateSessionMode_StaleDataGoesStaleAgain_ReturnsFallback()
  {
      // Was in Stale, but now data is 21s old and no good polls — re-degrade to Fallback
      var now = DateTimeOffset.UtcNow;
      var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
      sut._lastValidPositionTime = now.AddSeconds(-21);
      sut._sessionMode = SessionMode.Stale;
      sut._consecutiveGoodPolls = 0;
      Assert.Equal(SessionMode.Fallback, sut.EvaluateSessionMode());
  }
  
  // BuildSnapshot in Fallback mode
  [Fact]
  public void BuildSnapshot_FallbackModeWithData_UsesFallbackDrivers()
  {
      var sut = CreateOrchestrator();
      sut._sessionMode = SessionMode.Fallback;
      sut._fallbackRaceName = "Canadian Grand Prix";
      sut._fallbackDrivers =
      [
          new DriverState
          {
              DriverNumber = 4, DriverCode = "NOR", TeamName = "McLaren",
              TeamColour = "555555", Position = 1, GapIsStale = false,
          },
          new DriverState
          {
              DriverNumber = 81, DriverCode = "PIA", TeamName = "McLaren",
              TeamColour = "555555", Position = 2, GapToCarAhead = "+5.014", GapIsStale = false,
          },
      ];
  
      var snapshot = sut.BuildSnapshot();
  
      Assert.Equal(SessionMode.Fallback, snapshot.SessionMode);
      Assert.Equal("Canadian Grand Prix", snapshot.FallbackRaceName);
      Assert.Equal(2, snapshot.Drivers.Count);
      Assert.Equal("NOR", snapshot.Drivers[0].DriverCode);
      Assert.Equal("+5.014", snapshot.Drivers[1].GapToCarAhead);
  }
  
  [Fact]
  public void BuildSnapshot_FallbackModeNoData_ReturnsEmptyDrivers()
  {
      var sut = CreateOrchestrator();
      sut._sessionMode = SessionMode.Fallback;
      // _fallbackDrivers remains empty (Ergast fetch failed or no races completed)
  
      var snapshot = sut.BuildSnapshot();
  
      Assert.Equal(SessionMode.Fallback, snapshot.SessionMode);
      Assert.Empty(snapshot.Drivers);
  }
  
  [Fact]
  public void BuildSnapshot_LiveMode_SetsSessionModeLive()
  {
      var now = DateTimeOffset.UtcNow;
      var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
      sut._latestPositions[33] = MakePosition(33, 1, now);
      sut._lastValidPositionTime = now.AddSeconds(-2);
  
      var snapshot = sut.BuildSnapshot();
  
      Assert.Equal(SessionMode.Live, snapshot.SessionMode);
      Assert.Null(snapshot.FallbackRaceName);
  }
  ```

### Task 6: Update frontend types and store (AC: 4, 5)

- [ ] Update `frontend/src/shared/types/f1.ts` — add `sessionMode` and `fallbackRaceName` to `RaceStateSnapshot`, and add `LastRaceResult` interface:
  ```ts
  export interface RaceStateSnapshot {
    capturedAt: string
    drivers: DriverState[]
    lapChart: Record<string, LapTimeEntry[]>
    sessionMode: 'live' | 'stale' | 'fallback'   // NEW
    fallbackRaceName: string | null               // NEW
  }
  
  // NEW — shape of GET /api/races/last-result
  export interface LastRaceResult {
    raceName: string
    raceDate: string
    drivers: DriverState[]
  }
  ```

- [ ] Update `frontend/src/features/live-race/store/liveRaceStore.ts` — add `sessionMode`, `fallbackRaceName`, and their setters:
  ```ts
  import { create } from 'zustand'
  import type { DriverState, LapTimeEntry } from '../../../shared/types/f1'
  
  type ConnectionStatus = 'connected' | 'reconnecting' | 'disconnected'
  type SessionMode = 'live' | 'stale' | 'fallback'
  
  interface LiveRaceState {
    connectionStatus: ConnectionStatus
    sessionMode: SessionMode           // NEW
    fallbackRaceName: string | null    // NEW
    drivers: Record<string, DriverState>
    lapChart: Record<string, LapTimeEntry[]>
    lastSnapshotTime: Date | null
    setConnectionStatus: (status: ConnectionStatus) => void
    setSessionMode: (mode: SessionMode) => void         // NEW
    setFallbackRaceName: (name: string | null) => void  // NEW
    setDrivers: (drivers: Record<string, DriverState>) => void
    setLapChart: (lapChart: Record<string, LapTimeEntry[]>) => void
    setLastSnapshotTime: (time: Date) => void
  }
  
  export const useLiveRaceStore = create<LiveRaceState>((set) => ({
    connectionStatus: 'disconnected',
    sessionMode: 'live',           // NEW
    fallbackRaceName: null,        // NEW
    drivers: {},
    lapChart: {},
    lastSnapshotTime: null,
    setConnectionStatus: (status) => set({ connectionStatus: status }),
    setSessionMode: (mode) => set({ sessionMode: mode }),                   // NEW
    setFallbackRaceName: (name) => set({ fallbackRaceName: name }),         // NEW
    setDrivers: (drivers) => set({ drivers }),
    setLapChart: (lapChart) => set({ lapChart }),
    setLastSnapshotTime: (time) => set({ lastSnapshotTime: time }),
  }))
  ```

- [ ] Add `lastRaceResult` to `frontend/src/shared/api/queryKeys.ts`:
  ```ts
  export const queryKeys = {
    races: ['races', 'current'] as const,
    raceDetail: (round: number) => ['races', 'detail', round] as const,
    winProbability: (round: number) => ['races', 'win-probability', round] as const,
    lastRaceResult: ['races', 'last-result'] as const,   // NEW
    standings: {
      drivers: ['standings', 'drivers', 'current'] as const,
      constructors: ['standings', 'constructors', 'current'] as const,
    },
  }
  ```

### Task 7: Create `useFallbackState` hook and update `useSignalRConnection` (AC: 4, 5)

- [ ] Create `frontend/src/features/live-race/hooks/useFallbackState.ts`:
  ```ts
  import { useLiveRaceStore } from '../store/liveRaceStore'
  
  export function useFallbackState() {
    const sessionMode = useLiveRaceStore(s => s.sessionMode)
    const fallbackRaceName = useLiveRaceStore(s => s.fallbackRaceName)
  
    return {
      sessionMode,
      fallbackRaceName,
      isFallback: sessionMode === 'fallback',
      isStale: sessionMode === 'stale',
      isLive: sessionMode === 'live',
    }
  }
  ```

- [ ] Update `frontend/src/features/live-race/hooks/useSignalRConnection.ts` — forward `sessionMode` + `fallbackRaceName` from snapshot, and add client-side 10s fallback trigger for when no snapshot is received after connecting:
  ```ts
  import { useEffect } from 'react'
  import * as signalR from '@microsoft/signalr'
  import { raceHubConnection } from '../signalRClient'
  import { useLiveRaceStore } from '../store/liveRaceStore'
  import { normalizeSnapshot } from '../../../shared/utils/normalizeSnapshot'
  import type { RaceSnapshotMessage } from '../../../shared/types/signalR'
  
  let lifecycleHandlersAttached = false
  
  export function useSignalRConnection() {
    const setConnectionStatus = useLiveRaceStore(s => s.setConnectionStatus)
    const setDrivers = useLiveRaceStore(s => s.setDrivers)
    const setLapChart = useLiveRaceStore(s => s.setLapChart)
    const setLastSnapshotTime = useLiveRaceStore(s => s.setLastSnapshotTime)
    const setSessionMode = useLiveRaceStore(s => s.setSessionMode)           // NEW
    const setFallbackRaceName = useLiveRaceStore(s => s.setFallbackRaceName) // NEW
  
    useEffect(() => {
      const handleSnapshot = (snapshot: RaceSnapshotMessage) => {
        setDrivers(normalizeSnapshot(snapshot.drivers))
        setLapChart(snapshot.lapChart)
        setLastSnapshotTime(new Date())
        setSessionMode(snapshot.sessionMode ?? 'live')                        // NEW
        if (snapshot.fallbackRaceName) setFallbackRaceName(snapshot.fallbackRaceName) // NEW
      }
  
      raceHubConnection.on('RaceSnapshot', handleSnapshot)
  
      if (!lifecycleHandlersAttached) {
        raceHubConnection.onreconnecting(() => setConnectionStatus('reconnecting'))
        raceHubConnection.onreconnected(() => setConnectionStatus('connected'))
        raceHubConnection.onclose(() => setConnectionStatus('disconnected'))
        lifecycleHandlersAttached = true
      }
  
      if (raceHubConnection.state === signalR.HubConnectionState.Disconnected) {
        raceHubConnection
          .start()
          .then(() => setConnectionStatus('connected'))
          .catch(() => setConnectionStatus('disconnected'))
      }
  
      // NEW: Client-side fallback trigger — if 10s pass with no snapshot received after
      // connecting (server is running but no race weekend active, so it doesn't emit),
      // switch to fallback mode so LiveRacePage fetches from the REST endpoint.
      const noDataTimeout = window.setTimeout(() => {
        if (useLiveRaceStore.getState().lastSnapshotTime === null) {
          setSessionMode('fallback')
        }
      }, 10_000)
  
      return () => {
        raceHubConnection.off('RaceSnapshot', handleSnapshot)
        window.clearTimeout(noDataTimeout)  // NEW
      }
    }, [setConnectionStatus, setDrivers, setLapChart, setLastSnapshotTime, setSessionMode, setFallbackRaceName])
  }
  ```

### Task 8: Update `LiveRacePage` with banner and REST fallback (AC: 1, 4, 5, 6)

- [ ] Create `frontend/src/features/live-race/hooks/useLastRaceResult.ts`:
  ```ts
  import { useQuery } from '@tanstack/react-query'
  import { queryKeys } from '../../../shared/api/queryKeys'
  import type { LastRaceResult } from '../../../shared/types/f1'
  
  const apiBase = import.meta.env.VITE_API_BASE_URL ?? ''
  
  export function useLastRaceResult(options?: { enabled?: boolean }) {
    return useQuery<LastRaceResult>({
      queryKey: queryKeys.lastRaceResult,
      queryFn: async () => {
        const res = await fetch(`${apiBase}/api/races/last-result`)
        if (!res.ok) throw new Error('Failed to fetch last race result')
        return res.json()
      },
      staleTime: 60 * 60 * 1000, // 1h — historical race results don't change
      enabled: options?.enabled ?? true,
    })
  }
  ```

- [ ] Update `frontend/src/features/live-race/LiveRacePage.tsx`:
  ```tsx
  import { useEffect } from 'react'
  import { GapList } from './GapList/GapList'
  import { LapTimeChart } from './LapTimeChart/LapTimeChart'
  import { useSignalRConnection } from './hooks/useSignalRConnection'
  import { useFallbackState } from './hooks/useFallbackState'
  import { useLastRaceResult } from './hooks/useLastRaceResult'
  import { useLiveRaceStore } from './store/liveRaceStore'
  import { normalizeSnapshot } from '../../shared/utils/normalizeSnapshot'
  
  export function LiveRacePage() {
    useSignalRConnection()
  
    const { isFallback, isStale, fallbackRaceName } = useFallbackState()
    const drivers = useLiveRaceStore(s => s.drivers)
    const setDrivers = useLiveRaceStore(s => s.setDrivers)
    const setFallbackRaceName = useLiveRaceStore(s => s.setFallbackRaceName)
  
    const hasLiveData = Object.keys(drivers).length > 0
  
    // REST fallback: fetch last race data when in fallback mode and no live data in store
    const { data: lastRaceData } = useLastRaceResult({ enabled: isFallback && !hasLiveData })
  
    useEffect(() => {
      if (lastRaceData && isFallback && !hasLiveData) {
        setDrivers(normalizeSnapshot(lastRaceData.drivers))
        setFallbackRaceName(lastRaceData.raceName)
      }
    }, [lastRaceData, isFallback, hasLiveData, setDrivers, setFallbackRaceName])
  
    return (
      <div className="min-h-screen bg-[#14171c] text-[#eef0f3] p-4">
        <h1 className="text-[26px] font-bold tracking-[-0.01em] mb-4">Live Race</h1>
        {isFallback && (
          <div
            className="mb-4 px-3 py-2 bg-[#2a2f38] rounded-[8px] text-[12px] text-[#9aa1ad] border border-[#3a4050]"
            data-testid="fallback-banner"
          >
            📺 Past Race — {fallbackRaceName ?? 'Last Race'}
          </div>
        )}
        {isStale && (
          <div
            className="mb-4 px-3 py-2 bg-[#2a1f0e] rounded-[8px] text-[12px] text-[#d8b65c] border border-[#4a3a1a]"
            data-testid="stale-banner"
          >
            ⚠️ Connection unstable — data may be delayed
          </div>
        )}
        <div className="flex flex-col gap-4">
          <GapList />
          <LapTimeChart />
        </div>
      </div>
    )
  }
  ```

### Task 9: Frontend tests (AC: 1, 4, 5, 6)

- [ ] Create `frontend/src/features/live-race/LiveRacePage.test.tsx`:
  ```tsx
  import { render, screen, waitFor } from '@testing-library/react'
  import { beforeEach, describe, expect, it, vi } from 'vitest'
  import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
  import { LiveRacePage } from './LiveRacePage'
  import { useLiveRaceStore } from './store/liveRaceStore'
  
  // Prevent actual SignalR connection from starting in tests
  vi.mock('./hooks/useSignalRConnection', () => ({
    useSignalRConnection: vi.fn(),
  }))
  
  // Prevent REST fetch for last race result from running in most tests
  vi.mock('./hooks/useLastRaceResult', () => ({
    useLastRaceResult: vi.fn().mockReturnValue({ data: null, isPending: false }),
  }))
  
  function renderWithQueryClient(ui: React.ReactElement) {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    })
    return render(
      <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
    )
  }
  
  beforeEach(() => {
    useLiveRaceStore.setState({
      connectionStatus: 'disconnected',
      sessionMode: 'live',
      fallbackRaceName: null,
      drivers: {},
      lapChart: {},
      lastSnapshotTime: null,
    })
    vi.clearAllMocks()
  })
  
  describe('LiveRacePage', () => {
    it('shows no banner in live mode', () => {
      useLiveRaceStore.setState({ sessionMode: 'live' })
      renderWithQueryClient(<LiveRacePage />)
      expect(screen.queryByTestId('fallback-banner')).not.toBeInTheDocument()
      expect(screen.queryByTestId('stale-banner')).not.toBeInTheDocument()
    })
  
    it('shows past race banner with race name in fallback mode', () => {
      useLiveRaceStore.setState({ sessionMode: 'fallback', fallbackRaceName: 'Canadian Grand Prix' })
      renderWithQueryClient(<LiveRacePage />)
      expect(screen.getByTestId('fallback-banner')).toBeInTheDocument()
      expect(screen.getByText(/Canadian Grand Prix/)).toBeInTheDocument()
    })
  
    it('shows generic past race label when fallback race name is not yet known', () => {
      useLiveRaceStore.setState({ sessionMode: 'fallback', fallbackRaceName: null })
      renderWithQueryClient(<LiveRacePage />)
      expect(screen.getByTestId('fallback-banner')).toBeInTheDocument()
      expect(screen.getByText(/Last Race/)).toBeInTheDocument()
    })
  
    it('shows stale data warning in stale mode', () => {
      useLiveRaceStore.setState({ sessionMode: 'stale' })
      renderWithQueryClient(<LiveRacePage />)
      expect(screen.getByTestId('stale-banner')).toBeInTheDocument()
      expect(screen.getByText(/data may be delayed/i)).toBeInTheDocument()
    })
  
    it('shows no fallback banner in stale mode (only stale banner)', () => {
      useLiveRaceStore.setState({ sessionMode: 'stale' })
      renderWithQueryClient(<LiveRacePage />)
      expect(screen.queryByTestId('fallback-banner')).not.toBeInTheDocument()
      expect(screen.getByTestId('stale-banner')).toBeInTheDocument()
    })
  
    it('populates store with REST fallback data when in fallback mode and no drivers', async () => {
      const { useLastRaceResult } = await import('./hooks/useLastRaceResult')
      vi.mocked(useLastRaceResult).mockReturnValue({
        data: {
          raceName: 'Austrian Grand Prix',
          raceDate: '2026-07-06',
          drivers: [
            {
              driverNumber: 4, driverCode: 'NOR', teamName: 'McLaren',
              teamColour: '555555', position: 1, gapToCarAhead: null,
              gapIsStale: false, tyreCompound: null, stintLaps: null, championshipDelta: null,
            },
          ],
        } as never,
        isPending: false,
      } as never)
  
      useLiveRaceStore.setState({ sessionMode: 'fallback', drivers: {} })
      renderWithQueryClient(<LiveRacePage />)
  
      await waitFor(() => {
        const { drivers } = useLiveRaceStore.getState()
        expect(Object.keys(drivers)).toHaveLength(1)
        expect(drivers['4']?.driverCode).toBe('NOR')
      })
    })
  })
  ```

- [ ] Update `GapList.test.tsx` `beforeEach` to include the new store fields so existing tests don't fail:
  ```ts
  beforeEach(() => {
    useLiveRaceStore.setState({
      drivers: {},
      connectionStatus: 'disconnected',
      lastSnapshotTime: null,
      sessionMode: 'live',       // NEW — must match LiveRaceState shape
      fallbackRaceName: null,    // NEW
    })
  })
  ```
  **Why:** Zustand `setState` in tests merges shallowly. The `sessionMode` and `fallbackRaceName` fields are now part of the store shape. Not including them in `beforeEach` means tests that run after one that set `sessionMode: 'stale'` would see stale state.

- [ ] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass (target: existing 70 + ~11 new = ~81).

- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass (target: existing 58 + ~6 GapList beforeEach changes + 5 LiveRacePage tests = ~63 total; GapList tests count stays 58 since we only add fields to beforeEach, not tests).

## Dev Notes

### Architecture: State Machine Design

The NFR-12 state machine is split across two layers:

**Server-side (authoritative):** `RaceDataOrchestrator` detects staleness via `_lastValidPositionTime` + `_consecutiveGoodPolls`:
- `PollPositionAsync` updates `_lastValidPositionTime` on every non-empty response
- `EvaluateSessionMode()` runs every second in `PublishSnapshotLoopAsync`
- The emitted `RaceStateSnapshot.SessionMode` tells the frontend what mode we're in

**Client-side (startup fallback only):** `useSignalRConnection` adds a 10-second timer that fires if `lastSnapshotTime === null` — this covers the "no live session, server is sleeping" case where the server emits no SignalR messages at all. Uses `useLiveRaceStore.getState()` (not a closure) to check freshly at fire time.

### State Transition Thresholds

| Condition | Transition |
|---|---|
| Live, elapsed >10s since last valid position | Live → Stale |
| Live, elapsed >20s since last valid position | Live → Fallback |
| Stale, 4+ consecutive good polls | Stale → Live |
| Stale, elapsed >20s (data stays bad) | Stale → Fallback |
| Fallback, 1+ consecutive good poll | Fallback → Stale |
| Stale, 4+ consecutive good polls | Stale → Live |

`_consecutiveGoodPolls` resets to 0 on any empty positions response.

### `_lastValidPositionTime` Sentinel Value

`DateTimeOffset.MinValue` (not `null`) is used as "no poll has completed yet". The `EvaluateSessionMode` default case returns `Live` for `MinValue` — this prevents immediately entering Fallback on startup before the polling loops have a chance to run (which takes ~800ms for the first `PeriodicTimer` tick).

When the first poll returns empty, `MinValue` is replaced with `timeProvider.GetUtcNow()`, starting the staleness clock.

### Fallback Snapshot Data

When `_sessionMode == Fallback && _fallbackDrivers.Count > 0`, `BuildSnapshot` bypasses the live data path entirely. The `_fallbackDrivers` list is pre-built from Ergast's `current/last/results.json`.

Key limitations accepted for POC:
- `GapToCarAhead` for P2+ is "gap to race leader", not "gap to car ahead" — the past race banner makes this clear
- `TyreCompound = null` and `StintLaps = null` — tyre section in `DriverRow` will be hidden (existing null check handles this)
- `TeamColour = "555555"` (grey) — Ergast doesn't include team hex colours in race results
- `LapChart` remains empty — `LapTimeChart` shows "Waiting for lap data…"
- `ChampionshipDelta = null` — no delta computation in fallback mode

### REST Fallback Path

`LiveRacePage` uses `useLastRaceResult` (TanStack Query, 1h staleTime) when `isFallback && !hasLiveData`. On success, injects data into Zustand store via `setDrivers(normalizeSnapshot(lastRaceData.drivers))`. This reuses the exact same normalization and render path as live data — no new code in `GapList` or `DriverRow`.

**Why inject into store rather than render separately?** The `GapList` component reads only from `useLiveRaceStore`. Making it accept props would require changes to `GapList` and all tests — more invasive than the store injection.

### Ergast Endpoint

`current/last/results.json` returns the most recently completed race of the current season. Returns `Races: []` if no race has been completed yet (pre-season, or first race of season hasn't happened). The endpoint is in the public Ergast/Jolpica API.

### Frontend: `useEffect` Dependency Array in `LiveRacePage`

The `useEffect` that injects REST data into the store has `[lastRaceData, isFallback, hasLiveData, setDrivers, setFallbackRaceName]` as deps. `setDrivers` and `setFallbackRaceName` are stable Zustand action references (never change), so this is safe.

The `!hasLiveData` guard prevents re-injecting when live data has since arrived (e.g., if the session starts while the page is open).

### Regressions to Guard

- **Existing `BuildSnapshot` tests** — all set `_sessionMode = Live` implicitly (default). The new code only branches on Fallback, so all existing tests remain valid.
- **Championship delta tests** — `ComputeChampionshipDeltas` is not called when `_sessionMode == Fallback`. Existing delta tests don't set `_sessionMode` (defaults to Live) — they still exercise the championship delta path.
- **`GapList.test.tsx` `beforeEach`** — must be updated to include `sessionMode: 'live'` and `fallbackRaceName: null` (new store fields). Failure to do this would cause tests that run after a `sessionMode: 'stale'` test to see stale state. See Task 9 for the updated `beforeEach`.

### Files to Create / Modify

**Backend NEW:**
- `backend/F1App.Api/Models/SessionMode.cs`
- `backend/F1App.Api/Models/LastRaceResult.cs`

**Backend MODIFY:**
- `backend/F1App.Api/Models/RaceStateSnapshot.cs`
- `backend/F1App.Api/Dtos/Ergast/ErgastStandingsResponseDto.cs` — add `Code?` to `ErgastDriverDto`
- `backend/F1App.Api/Dtos/Ergast/ErgastRaceResultResponseDto.cs` — add `RaceName`, `Date` to `ErgastRaceResultRaceDto`; add `Position`, `Number`, `Status?` to `ErgastResultDto`
- `backend/F1App.Api/Clients/IErgastClient.cs`
- `backend/F1App.Api/Clients/ErgastClient.cs`
- `backend/F1App.Api/Services/CacheKeys.cs`
- `backend/F1App.Api/Services/RaceScheduleService.cs`
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs`
- `backend/F1App.Api/Controllers/RacesController.cs`
- `backend/F1App.Api.Tests/Clients/ErgastClientContractTests.cs`
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`

**Frontend NEW:**
- `frontend/src/features/live-race/hooks/useFallbackState.ts`
- `frontend/src/features/live-race/hooks/useLastRaceResult.ts`
- `frontend/src/features/live-race/LiveRacePage.test.tsx`

**Frontend MODIFY:**
- `frontend/src/shared/types/f1.ts`
- `frontend/src/shared/api/queryKeys.ts`
- `frontend/src/features/live-race/store/liveRaceStore.ts`
- `frontend/src/features/live-race/hooks/useSignalRConnection.ts`
- `frontend/src/features/live-race/LiveRacePage.tsx`
- `frontend/src/features/live-race/GapList/GapList.test.tsx` — update `beforeEach` only

## Dev Agent Record

### Agent Model Used

_to be filled_

### Debug Log References

### Completion Notes List

### File List

### Change Log

| Date | Change |
|------|--------|
| 2026-06-23 | Story created via bmad-create-story |
