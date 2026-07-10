---
baseline_commit: "6a37395"
---

# Story 3.3: Pit Window Estimator

Status: done

## Story

As a race-day fan,
I want to see when a driver is likely to pit,
So that I can anticipate strategy calls before they happen.

## Acceptance Criteria

1. **Given** a driver's current Stint lap count enters the historically typical pit window for their compound at this circuit (from Ergast historical pit data) **When** the gap list renders **Then** a pit window indicator activates on that driver's entry.
2. **Given** the driver pits **When** detected **Then** the indicator deactivates.
3. **And** the indicator is compound- and circuit-specific, not a fixed lap number.
4. **Given** `sessionMode === 'fallback'` (Story 2.5 static replay) **When** the gap list renders **Then** no pit window indicator is shown (the race is over, all pit stops already happened).
5. **Given** a driver has no tyre compound or stint-lap data yet **When** the entry renders **Then** no indicator is shown (no false positive before data arrives).

## Tasks / Subtasks

### Task 1: Backend — Ergast pit-stop DTOs (AC: 1, 3)

- [ ] Create `backend/F1App.Api/Dtos/Ergast/ErgastPitStopResponseDto.cs`:
  ```csharp
  using System.Text.Json.Serialization;

  namespace F1App.Api.Dtos.Ergast;

  public record ErgastPitStopResponseDto(
      [property: JsonPropertyName("MRData")] ErgastPitStopMrDataDto MRData);

  public record ErgastPitStopMrDataDto(
      [property: JsonPropertyName("RaceTable")] ErgastPitStopRaceTableDto RaceTable);

  public record ErgastPitStopRaceTableDto(
      [property: JsonPropertyName("Races")] IReadOnlyList<ErgastPitStopRaceDto> Races);

  public record ErgastPitStopRaceDto(
      [property: JsonPropertyName("PitStops")] IReadOnlyList<ErgastPitStopDto> PitStops = default!);

  public record ErgastPitStopDto(
      [property: JsonPropertyName("driverId")] string DriverId,
      [property: JsonPropertyName("lap")] string Lap,
      [property: JsonPropertyName("stop")] string Stop);
  ```
  Only `driverId`, `lap`, `stop` are needed (duration/time are not used by the window calculation).

- [ ] Update `backend/F1App.Api/Dtos/Ergast/ErgastRaceResultResponseDto.cs` — add a `Round` property to `ErgastRaceResultRaceDto` so the pit-stop lookup can find the round number for a circuit/season without a second Ergast resource type:
  ```csharp
  public record ErgastRaceResultRaceDto(
      [property: JsonPropertyName("raceName")] string RaceName = "",
      [property: JsonPropertyName("round")] string Round = "",
      [property: JsonPropertyName("date")] string Date = "",
      [property: JsonPropertyName("Results")] IReadOnlyList<ErgastResultDto> Results = default!);
  ```
  **Confirmed live**: `{season}/circuits/{circuitId}/results/1.json` already returns `"round"` at the race-object level (verified against `api.jolpi.ca/ergast/f1`); this is additive and does not change any existing call site or test (System.Text.Json ignores unmapped properties it doesn't ask for, and existing tests never assert `Round` so the default `""` is fine when omitted from a mock response).

### Task 2: Backend — `IErgastClient` / `ErgastClient.GetCircuitPitStopsAsync` (AC: 1, 3)

- [ ] Add to `backend/F1App.Api/Clients/IErgastClient.cs`:
  ```csharp
  Task<IReadOnlyList<ErgastPitStopDto>> GetCircuitPitStopsAsync(int season, string circuitId, CancellationToken cancellationToken);
  ```

- [ ] Implement in `backend/F1App.Api/Clients/ErgastClient.cs`. **Important**: Ergast/Jolpica's `pitstops.json` resource requires an explicit season **and** round in the URL — `{season}/circuits/{circuitId}/pitstops.json` returns `400 Bad Request: Missing one of the required parameters ['season_year', 'race_round']` (confirmed live). So this method first resolves the round via the existing circuit/results lookup, then queries pit stops for that specific race:
  ```csharp
  public async Task<IReadOnlyList<ErgastPitStopDto>> GetCircuitPitStopsAsync(int season, string circuitId, CancellationToken cancellationToken)
  {
      var raceResponse = await httpClient.GetFromJsonAsync<ErgastRaceResultResponseDto>(
          $"{season}/circuits/{circuitId}/results/1.json", cancellationToken)
          ?? throw new InvalidOperationException($"Ergast returned an empty response for {circuitId} in {season}.");

      if (raceResponse.MRData.RaceTable.Races.Count == 0)
          return [];

      var round = raceResponse.MRData.RaceTable.Races[0].Round;
      if (string.IsNullOrEmpty(round))
          return [];

      var pitResponse = await httpClient.GetFromJsonAsync<ErgastPitStopResponseDto>(
          $"{season}/{round}/pitstops.json?limit=100", cancellationToken)
          ?? throw new InvalidOperationException($"Ergast returned an empty response for {season}/{round} pit stops.");

      return pitResponse.MRData.RaceTable.Races.Count == 0
          ? []
          : pitResponse.MRData.RaceTable.Races[0].PitStops;
  }
  ```
  Two sequential HTTP calls is acceptable here — this runs once per race weekend (cached 24h by `PitWindowService`), not on the hot polling path.

### Task 3: Backend — `PitWindowService` (AC: 1, 3)

- [ ] Create `backend/F1App.Api/Services/PitWindowService.cs`:
  ```csharp
  using F1App.Api.Clients;
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
      internal static List<double> ComputeStintLengths(IReadOnlyList<Dtos.Ergast.ErgastPitStopDto> pitStops)
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
  ```

### Task 4: Backend — `CacheKeys` (AC: 1)

- [ ] Add to `backend/F1App.Api/Services/CacheKeys.cs`:
  ```csharp
  public static string PitWindowBaseline(string circuitId) => $"pitWindow:{circuitId}";
  ```

### Task 5: Backend — `DriverState.PitWindowActive` (AC: 1, 2, 4, 5)

- [ ] Update `backend/F1App.Api/Models/DriverState.cs` — add:
  ```csharp
  public bool PitWindowActive { get; init; }
  ```
  Plain `bool` (not `bool?`) — always present, defaults to `false`, no `WhenWritingNull` omission concern (unlike `MiniSectorStatus`).

### Task 6: Backend — Wire into `RaceDataOrchestrator` (AC: 1, 2, 3, 4, 5)

- [ ] Add field near the sector-colour tracking fields:
  ```csharp
  // Historical median stint length (laps) for the active circuit, used as the
  // pit-window baseline. Loaded once per session in InitialiseDriverInfoAsync;
  // defaults to PitWindowService.DefaultBaselineLaps until then / on failure.
  internal double _pitWindowBaselineLaps = PitWindowService.DefaultBaselineLaps;
  ```

- [ ] In `InitialiseDriverInfoAsync`, after the existing standings-loading `try/catch` block, add:
  ```csharp
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
  ```
  **Note**: `InitialiseDriverInfoAsync(CancellationToken ct)` already takes `ct` — reuse it, don't add a new parameter.

- [ ] In `BuildSnapshot`, in the live/stale driver-assembly loop (the `foreach (var (driverNum, pos) in _latestPositions)` block), after the existing `_latestSectorStatus.TryGetValue(...)` line, add:
  ```csharp
  var pitWindowActive = false;
  if (stintLaps.HasValue && tyreCompound is not null)
  {
      var (min, max) = PitWindowService.ComputeWindow(_pitWindowBaselineLaps, tyreCompound);
      pitWindowActive = stintLaps.Value >= min && stintLaps.Value <= max;
  }
  ```
  And add `PitWindowActive = pitWindowActive,` to the `drivers.Add(new DriverState { ... })` call.

  **AC 2 (deactivates when driver pits)**: no special-case code needed — when a driver pits, OpenF1's `/stints` feed publishes a new stint with `StintNumber` incremented and `TyreAgeAtStart = 0`; `BuildSnapshot`'s existing `stintLaps` calculation (`stint.TyreAgeAtStart + Math.Max(0, currentLap - stint.LapStart + 1)`) naturally resets to a small number outside the window on the very next snapshot after the pit stop, so the indicator switches off automatically once `_latestStints` reflects the new stint.

  **AC 4 (fallback)**: `_fallbackDrivers` (built in `LoadFallbackDataAsync`) are constructed without setting `PitWindowActive`, so it defaults to `false` — no change needed there, matching the existing `MiniSectorStatus` fallback pattern.

  **AC 5 (no false positive before data arrives)**: guarded by the `stintLaps.HasValue && tyreCompound is not null` check — both must be populated (from `_latestStints` + `_driverCurrentLap`) before any window check runs.

### Task 7: Backend — Register `PitWindowService` (AC: 1)

- [ ] In `backend/F1App.Api/Program.cs`, add alongside the other scoped services:
  ```csharp
  builder.Services.AddScoped<PitWindowService>();
  ```

### Task 8: Backend — Tests (AC: 1, 2, 3, 4, 5)

- [ ] Add to `backend/F1App.Api.Tests/Clients/ErgastClientContractTests.cs`:
  ```csharp
  [Fact]
  public async Task GetCircuitPitStopsAsync_ResolvesRoundThenReturnsPitStops()
  {
      _server
          .Given(Request.Create().WithPath("/2025/circuits/monza/results/1.json").UsingGet())
          .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
          {
              MRData = new { RaceTable = new { Races = new[] { new { round = "16", Results = Array.Empty<object>() } } } },
          }));
      _server
          .Given(Request.Create().WithPath("/2025/16/pitstops.json").UsingGet())
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
                              PitStops = new[]
                              {
                                  new { driverId = "norris", lap = "18", stop = "1" },
                                  new { driverId = "piastri", lap = "20", stop = "1" },
                              },
                          },
                      },
                  },
              },
          }));

      using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
      var client = new ErgastClient(httpClient);

      var pitStops = await client.GetCircuitPitStopsAsync(2025, "monza", CancellationToken.None);

      Assert.Equal(2, pitStops.Count);
      Assert.Equal("norris", pitStops[0].DriverId);
      Assert.Equal("18", pitStops[0].Lap);
  }

  [Fact]
  public async Task GetCircuitPitStopsAsync_ReturnsEmptyWhenNoRaceAtCircuitThatSeason()
  {
      _server
          .Given(Request.Create().WithPath("/2025/circuits/las_vegas/results/1.json").UsingGet())
          .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
          {
              MRData = new { RaceTable = new { Races = Array.Empty<object>() } },
          }));

      using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
      var client = new ErgastClient(httpClient);

      var pitStops = await client.GetCircuitPitStopsAsync(2025, "las_vegas", CancellationToken.None);

      Assert.Empty(pitStops);
  }
  ```
  Note the `round = "16"` field on the first mock — confirms the `Round` property added in Task 1 is read correctly.

- [ ] Create `backend/F1App.Api.Tests/Services/PitWindowServiceTests.cs`:
  ```csharp
  using F1App.Api.Clients;
  using F1App.Api.Dtos.Ergast;
  using F1App.Api.Services;
  using Microsoft.Extensions.Caching.Memory;
  using Microsoft.Extensions.Logging.Abstractions;
  using Moq;

  namespace F1App.Api.Tests.Services;

  public class PitWindowServiceTests
  {
      private static ErgastPitStopDto MakeStop(string driverId, string lap, string stop) => new(driverId, lap, stop);

      [Fact]
      public void ComputeStintLengths_MultipleDriversAndStops_ComputesLapGapsPerDriver()
      {
          var stops = new List<ErgastPitStopDto>
          {
              MakeStop("norris", "20", "1"),
              MakeStop("norris", "40", "2"),
              MakeStop("piastri", "22", "1"),
          };

          var lengths = PitWindowService.ComputeStintLengths(stops);

          Assert.Equal(3, lengths.Count);
          Assert.Contains(20.0, lengths); // norris stint 1: lap 0 -> 20
          Assert.Contains(20.0, lengths); // norris stint 2: lap 20 -> 40
          Assert.Contains(22.0, lengths); // piastri stint 1: lap 0 -> 22
      }

      [Theory]
      [InlineData("SOFT", 20.0, 13.6, 18.4)]
      [InlineData("MEDIUM", 20.0, 17.0, 23.0)]
      [InlineData("HARD", 20.0, 21.25, 28.75)]
      [InlineData(null, 20.0, 17.0, 23.0)]
      public void ComputeWindow_ScalesBaselineByCompound(string? compound, double baseline, double expectedMin, double expectedMax)
      {
          var (min, max) = PitWindowService.ComputeWindow(baseline, compound);

          Assert.True(Math.Abs(expectedMin - min) < 0.01);
          Assert.True(Math.Abs(expectedMax - max) < 0.01);
      }

      [Fact]
      public async Task GetBaselineMedianStintLapsAsync_ComputesMedianFromHistoricalPitStops()
      {
          var mockErgast = new Mock<IErgastClient>();
          mockErgast.Setup(e => e.GetCircuitPitStopsAsync(2025, "monza", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<ErgastPitStopDto>
              {
                  MakeStop("norris", "18", "1"),
                  MakeStop("piastri", "22", "1"),
              });
          var cache = new MemoryCache(new MemoryCacheOptions());
          var sut = new PitWindowService(mockErgast.Object, cache, NullLogger<PitWindowService>.Instance);

          var baseline = await sut.GetBaselineMedianStintLapsAsync("monza", 2025, CancellationToken.None);

          Assert.Equal(20.0, baseline); // median of [18, 22]
      }

      [Fact]
      public async Task GetBaselineMedianStintLapsAsync_NoHistoricalData_ReturnsDefault()
      {
          var mockErgast = new Mock<IErgastClient>();
          mockErgast.Setup(e => e.GetCircuitPitStopsAsync(2025, "new_circuit", It.IsAny<CancellationToken>()))
              .ReturnsAsync([]);
          var cache = new MemoryCache(new MemoryCacheOptions());
          var sut = new PitWindowService(mockErgast.Object, cache, NullLogger<PitWindowService>.Instance);

          var baseline = await sut.GetBaselineMedianStintLapsAsync("new_circuit", 2025, CancellationToken.None);

          Assert.Equal(PitWindowService.DefaultBaselineLaps, baseline);
      }

      [Fact]
      public async Task GetBaselineMedianStintLapsAsync_ErgastThrows_ReturnsDefaultAndDoesNotThrow()
      {
          var mockErgast = new Mock<IErgastClient>();
          mockErgast.Setup(e => e.GetCircuitPitStopsAsync(2025, "monza", It.IsAny<CancellationToken>()))
              .ThrowsAsync(new HttpRequestException("boom"));
          var cache = new MemoryCache(new MemoryCacheOptions());
          var sut = new PitWindowService(mockErgast.Object, cache, NullLogger<PitWindowService>.Instance);

          var baseline = await sut.GetBaselineMedianStintLapsAsync("monza", 2025, CancellationToken.None);

          Assert.Equal(PitWindowService.DefaultBaselineLaps, baseline);
      }

      [Fact]
      public async Task GetBaselineMedianStintLapsAsync_SecondCall_UsesCacheNotErgast()
      {
          var mockErgast = new Mock<IErgastClient>();
          mockErgast.Setup(e => e.GetCircuitPitStopsAsync(2025, "monza", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<ErgastPitStopDto> { MakeStop("norris", "20", "1") });
          var cache = new MemoryCache(new MemoryCacheOptions());
          var sut = new PitWindowService(mockErgast.Object, cache, NullLogger<PitWindowService>.Instance);

          await sut.GetBaselineMedianStintLapsAsync("monza", 2025, CancellationToken.None);
          await sut.GetBaselineMedianStintLapsAsync("monza", 2025, CancellationToken.None);

          mockErgast.Verify(e => e.GetCircuitPitStopsAsync(2025, "monza", It.IsAny<CancellationToken>()), Times.Once);
      }
  }
  ```
  **Note**: `ComputeStintLengths` and `ComputeWindow` must be `internal` (not `private`) — matches the existing `ComputeSectorStatus` convention (`InternalsVisibleTo("F1App.Api.Tests")` is already configured in `Program.cs`).

- [ ] Add to `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`:
  ```csharp
  [Fact]
  public void BuildSnapshot_StintLapsWithinWindow_SetsPitWindowActiveTrue()
  {
      var sut = CreateOrchestrator();
      sut._pitWindowBaselineLaps = 20.0; // MEDIUM window ≈ 17.0–23.0
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      sut._latestStints[33] = MakeStint(33, 1, 1, "MEDIUM", tyreAgeAtStart: 0);
      sut._driverCurrentLap[33] = 19; // stintLaps = 0 + (19 - 1 + 1) = 19, within [17.0, 23.0]

      var snapshot = sut.BuildSnapshot();

      Assert.True(snapshot.Drivers[0].PitWindowActive);
  }

  [Fact]
  public void BuildSnapshot_StintLapsOutsideWindow_SetsPitWindowActiveFalse()
  {
      var sut = CreateOrchestrator();
      sut._pitWindowBaselineLaps = 20.0;
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      sut._latestStints[33] = MakeStint(33, 1, 1, "MEDIUM", tyreAgeAtStart: 0);
      sut._driverCurrentLap[33] = 5; // stintLaps = 5, well below the window

      var snapshot = sut.BuildSnapshot();

      Assert.False(snapshot.Drivers[0].PitWindowActive);
  }

  [Fact]
  public void BuildSnapshot_NoStintData_PitWindowActiveFalse()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      // no _latestStints entry — tyreCompound stays null

      var snapshot = sut.BuildSnapshot();

      Assert.False(snapshot.Drivers[0].PitWindowActive);
  }

  [Fact]
  public void BuildSnapshot_JustPitted_NewStintResetsWindowToFalse()
  {
      var sut = CreateOrchestrator();
      sut._pitWindowBaselineLaps = 20.0;
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      // Driver was on stint 1 at lap 19 (in window), then pits: stint 2 starts at lap 20 fresh
      sut._latestStints[33] = MakeStint(33, 2, 20, "HARD", tyreAgeAtStart: 0);
      sut._driverCurrentLap[33] = 20; // stintLaps = 0 + (20 - 20 + 1) = 1

      var snapshot = sut.BuildSnapshot();

      Assert.False(snapshot.Drivers[0].PitWindowActive);
  }
  ```
  **Note**: `_pitWindowBaselineLaps` must be `internal` (settable) for these tests — matches the `internal` convention already used for `_sessionBestS1` etc. being accessible... actually those are `private`; follow the same visibility already used for fields the tests *do* set directly, e.g. `_latestStints`, `_driverCurrentLap` (both `internal`). Declare `_pitWindowBaselineLaps` as `internal` (not `private`).

- [ ] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass.

### Task 9: Frontend — Extend types (AC: 1, 2, 3, 4, 5)

- [ ] Update `frontend/src/shared/types/f1.ts` — add to `DriverState`:
  ```ts
  pitWindowActive: boolean
  ```
  Required, non-nullable — the backend field is a plain `bool` (not `bool?`), so JSON always includes the key (no `WhenWritingNull` omission concern, unlike `miniSectorStatus`).

### Task 10: Frontend — `PitWindowIndicator` component (AC: 1, 3)

- [ ] Create `frontend/src/features/live-race/GapList/PitWindowIndicator.tsx`:
  ```tsx
  export function PitWindowIndicator() {
    return (
      <span
        className="shrink-0 rounded px-1 py-px text-[9px] font-bold uppercase tracking-wide text-[#0c0e11] bg-[#ffcc00]"
        data-testid="pit-window-indicator"
        title="Pit window open"
      >
        PIT
      </span>
    )
  }
  ```

### Task 11: Frontend — Wire into `DriverRow` (AC: 1, 2, 4, 5)

- [ ] Update `frontend/src/features/live-race/GapList/DriverRow.tsx` — import `PitWindowIndicator` and render it right after the tyre-compound block (inside the same `driver.tyreCompound !== null` conditional's sibling, so it never renders without tyre data — satisfies AC 5):
  ```tsx
  import { PitWindowIndicator } from './PitWindowIndicator'
  ```
  ```tsx
  {driver.pitWindowActive && <PitWindowIndicator />}
  ```
  Place this `<span>`/expression immediately after the closing of the tyre-compound `<span>` block (before the `championshipDelta` block), so the row order is: position → team dot → code → tyre+stint → pit indicator → championship delta → gap.

  **AC 4 (fallback)**: no special-case needed in this component — `pitWindowActive` is always `false` on fallback drivers per Task 6, so the indicator naturally never renders there.

### Task 12: Frontend — Tests and regression fixes (AC: 1, 2, 3, 4, 5)

- [ ] Update `frontend/src/features/live-race/GapList/GapList.test.tsx`:
  - Add `pitWindowActive: false` to every existing driver fixture (`makeDriver` helper or inline store state).
  - Add a new test: rendering a driver with `pitWindowActive: true` shows `getByTestId('pit-window-indicator')`.
  - Add a new test: rendering a driver with `pitWindowActive: false` does NOT show it (`queryByTestId('pit-window-indicator')` is null).

- [ ] Update `frontend/src/features/live-race/LapTimeChart/LapTimeChart.test.tsx` — add `pitWindowActive: false` to all `DriverState` fixtures.

- [ ] Update `frontend/src/features/live-race/TrackMap/TrackMap.test.tsx` — add `pitWindowActive: false` to all `DriverState` fixtures.

- [ ] Update `frontend/src/shared/utils/normalizeSnapshot.test.ts` — add `pitWindowActive: false` to `makeDriver`.

- [ ] Update `frontend/src/features/live-race/LiveRacePage.test.tsx` — add `pitWindowActive: false` to the driver fixture in the REST fallback test.

- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass.
- [ ] Run `npx tsc --noEmit` from `frontend/` (or equivalent build check) to confirm no missing-field TypeScript errors across fixtures.

## Dev Notes

### Architecture Alignment

- The architecture doc (`architecture.md` §4.2, line 505) names `PitWindowIndicator.tsx` as the delivery component and `tyreUtils.ts` as home for "pit window thresholds." This story deviates slightly from the latter: consistent with every other Epic 2/3 story (championship delta, mini-sector colour, tyre compound), **all computation happens backend-side in `RaceDataOrchestrator`/`RaceStateSnapshot`**, and the frontend only renders a boolean flag it's handed. `PitWindowIndicator.tsx` is delivered exactly as named; the threshold *logic* lives in the new `PitWindowService.cs` instead of `tyreUtils.ts`, matching the single-broadcast, backend-owns-state-machine principle already established for this codebase (`RaceDataOrchestrator` assembles one `RaceStateSnapshot`).
- **Ergast/Jolpica pit-stop API constraint (confirmed live against `https://api.jolpi.ca/ergast/f1`)**: `pitstops.json` cannot be filtered by circuit alone — it requires an explicit season+round in the path. There is no cross-season "all pit stops at this circuit" query. This story's design resolves the round via the already-used `{season}/circuits/{circuitId}/results/1.json` lookup (same endpoint `GetCircuitResultsAsync` already calls for Story 1.5's "last year's winner"), then queries pit stops for that one specific race. This means the "historical" baseline is built from **one prior race** (last year's race at this circuit), not a multi-season aggregate — a deliberate scope simplification given Ergast's API shape, consistent with `WinProbabilityService`'s existing precedent of a simplified heuristic over a literal multi-season historical model.
- **No compound breakdown in Ergast data**: pit stop records have no tyre-compound field (compound tracking is OpenF1-only, and OpenF1 has no historical archive). `PitWindowService.ComputeWindow` applies fixed compound multipliers (soft faster degradation → shorter window; hard slower degradation → longer window) on top of the circuit's real historical median stint length. This is the same kind of pragmatic blend of real data + heuristic weighting used in `WinProbabilityService.GetWinProbabilitiesAsync`.
- `PitWindowActive` is a plain `bool` (not `bool?`) — unlike `MiniSectorStatus` (`string?`), there is no `JsonIgnoreCondition.WhenWritingNull` omission concern, so the frontend field is required/non-nullable with no defensive `?? false` needed (though harmless if added).
- `_pitWindowBaselineLaps` must be `internal` (not `private`) so `RaceDataOrchestratorTests` can set it directly before calling `BuildSnapshot()` — same visibility pattern already used for `_latestStints`, `_driverCurrentLap`, `_latestSectorStatus`.
- `ComputeStintLengths` and `ComputeWindow` in `PitWindowService` must be `internal` static methods (not `private`) so `PitWindowServiceTests` can call them directly — `InternalsVisibleTo("F1App.Api.Tests")` is already configured in `Program.cs`.
- Baseline is loaded once per session (in `InitialiseDriverInfoAsync`, alongside driver info and standings) and cached 24h in `IMemoryCache` — it does not change during a race weekend, so there's no need to reload it in the polling loops.

### Regressions to Guard

- Every test file that constructs a `DriverState` object (`GapList.test.tsx`, `LapTimeChart.test.tsx`, `TrackMap.test.tsx`, `normalizeSnapshot.test.ts`, `LiveRacePage.test.tsx`) will fail TypeScript compilation without `pitWindowActive: false` added — this is a **required** field, same as `miniSectorStatus` was in Story 3.2.
- Do not change the `foreach (var (driverNum, pos) in _latestPositions)` loop's existing variable names (`tyreCompound`, `stintLaps`) — the new pit-window block reads them directly; reordering or renaming would silently break the AC 5 guard.
- `LoadFallbackDataAsync`'s `DriverState` construction must NOT set `PitWindowActive` — leaving it unset (defaults to `false`) is the correct AC 4 behaviour; do not add a fallback-specific pit-window calculation.

### Files to Create / Modify

**Backend CREATE:**
- `backend/F1App.Api/Dtos/Ergast/ErgastPitStopResponseDto.cs`
- `backend/F1App.Api/Services/PitWindowService.cs`
- `backend/F1App.Api.Tests/Services/PitWindowServiceTests.cs`

**Backend MODIFY:**
- `backend/F1App.Api/Dtos/Ergast/ErgastRaceResultResponseDto.cs` — add `Round` to `ErgastRaceResultRaceDto`
- `backend/F1App.Api/Clients/IErgastClient.cs` — add `GetCircuitPitStopsAsync`
- `backend/F1App.Api/Clients/ErgastClient.cs` — implement `GetCircuitPitStopsAsync`
- `backend/F1App.Api/Services/CacheKeys.cs` — add `PitWindowBaseline`
- `backend/F1App.Api/Models/DriverState.cs` — add `PitWindowActive`
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` — add `_pitWindowBaselineLaps` field, load in `InitialiseDriverInfoAsync`, compute in `BuildSnapshot`
- `backend/F1App.Api/Program.cs` — register `PitWindowService`
- `backend/F1App.Api.Tests/Clients/ErgastClientContractTests.cs` — 2 new tests
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs` — 4 new tests

**Frontend CREATE:**
- `frontend/src/features/live-race/GapList/PitWindowIndicator.tsx`

**Frontend MODIFY:**
- `frontend/src/shared/types/f1.ts` — add `pitWindowActive`
- `frontend/src/features/live-race/GapList/DriverRow.tsx` — render `PitWindowIndicator`
- `frontend/src/features/live-race/GapList/GapList.test.tsx` — regression fixes + 2 new tests
- `frontend/src/features/live-race/LapTimeChart/LapTimeChart.test.tsx` — regression fix
- `frontend/src/features/live-race/TrackMap/TrackMap.test.tsx` — regression fix
- `frontend/src/shared/utils/normalizeSnapshot.test.ts` — regression fix
- `frontend/src/features/live-race/LiveRacePage.test.tsx` — regression fix

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.3: Pit Window Estimator]
- [Source: _bmad-output/planning-artifacts/architecture.md line 505, 557, 695 — PitWindowIndicator.tsx, tyreUtils.ts, FR-11 mapping]
- [Source: backend/F1App.Api/Services/RaceDataOrchestrator.cs — existing stint/lap/sector tracking patterns]
- [Source: backend/F1App.Api/Services/WinProbabilityService.cs — precedent for simplified heuristic over literal historical model]
- Live-verified against `https://api.jolpi.ca/ergast/f1` (the project's configured `ErgastBaseUrl`): `pitstops.json` requires season+round; `circuits/{circuitId}/results.json` returns `round` at the race-object level.

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

None.

### Completion Notes List

- Implemented pit-window estimation entirely as planned: `PitWindowService` computes a per-circuit median historical stint length from the prior year's Ergast pit stops (round resolved via the existing `circuits/{circuitId}/results/1.json` lookup, since Jolpica's `pitstops.json` requires an explicit season+round), then `RaceDataOrchestrator.BuildSnapshot` applies a compound-specific multiplier to flag `PitWindowActive` per driver.
- Fixed a floating-point precision issue in the `ComputeWindow` theory test (`Assert.Equal` with `precision:1` rounding didn't handle `28.749999999999996` as expected) by switching to a manual tolerance assertion.
- All 108 backend tests and 72 frontend tests pass; `tsc --noEmit` is clean.

### File List

**Backend created:**
- `backend/F1App.Api/Dtos/Ergast/ErgastPitStopResponseDto.cs`
- `backend/F1App.Api/Services/PitWindowService.cs`
- `backend/F1App.Api.Tests/Services/PitWindowServiceTests.cs`

**Backend modified:**
- `backend/F1App.Api/Dtos/Ergast/ErgastRaceResultResponseDto.cs`
- `backend/F1App.Api/Clients/IErgastClient.cs`
- `backend/F1App.Api/Clients/ErgastClient.cs`
- `backend/F1App.Api/Services/CacheKeys.cs`
- `backend/F1App.Api/Models/DriverState.cs`
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs`
- `backend/F1App.Api/Program.cs`
- `backend/F1App.Api.Tests/Clients/ErgastClientContractTests.cs`
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`

**Frontend created:**
- `frontend/src/features/live-race/GapList/PitWindowIndicator.tsx`

**Frontend modified:**
- `frontend/src/shared/types/f1.ts`
- `frontend/src/features/live-race/GapList/DriverRow.tsx`
- `frontend/src/features/live-race/GapList/GapList.test.tsx`
- `frontend/src/features/live-race/LapTimeChart/LapTimeChart.test.tsx`
- `frontend/src/features/live-race/TrackMap/TrackMap.test.tsx`
- `frontend/src/shared/utils/normalizeSnapshot.test.ts`
- `frontend/src/features/live-race/LiveRacePage.test.tsx`

### Change Log

| Date | Change |
|------|--------|
| 2026-07-10 | Story created via bmad-create-story |
