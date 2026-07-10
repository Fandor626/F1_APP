---
baseline_commit: "9086a1b"
---

# Story 5.3: Driver Head-to-Head Comparison

Status: done

## Story

As a history enthusiast,
I want to compare two drivers' stats side by side,
So that I can settle debates about who's better.

## Acceptance Criteria

1. **Given** the head-to-head page **When** the user selects two Drivers from a searchable dropdown **Then** a side-by-side stat card shows qualifying average position, race finish average, DNF count, points scored, fastest laps, and wins.
2. **Given** optional season and/or Circuit filters are applied **Then** they are additive and the stat card recalculates accordingly.
3. **Given** no filters are applied **Then** the comparison covers the full all-time Ergast dataset for both drivers.

## API behaviour verified live (against `https://api.jolpi.ca/ergast/f1`) before writing any code

- `/drivers.json?limit=1000` → `total: "881"` — the entire all-time F1 driver roster fits in one page; this is the source for the searchable dropdown.
- Filter combinations all work as composable URL segments, confirmed individually and combined:
  - `/{season}/drivers/{driverId}/results.json` (season only)
  - `/drivers/{driverId}/circuits/{circuitId}/results.json` (circuit only)
  - `/{season}/drivers/{driverId}/circuits/{circuitId}/results.json` (both — additive, per AC 2)
  - Same three combinations work for `.../qualifying.json` in place of `.../results.json`.
- **Qualifying average position uses real qualifying data, not the `grid` proxy Story 5.2 used for "poles"** — `/drivers/{driverId}/qualifying.json` returns actual `QualifyingResults[].position` per race (which can differ from race-day `grid` due to penalties), reusing the existing `ErgastQualifyingRaceDto`/`ErgastQualifyingResultDto` DTOs as-is (they already model the right shape; only a new multi-race-returning client method is needed, not a new DTO).

## Scope decision: circuit filter is a free-text circuitId input, not a dropdown

Unlike the season filter (a plain number), there's no existing "list every circuit" endpoint/data source wired into the frontend yet (Epic 5's circuit profile page is reached by clicking through from a race, not from a browsable list). Building a full circuit-search dropdown is out of scope for this story; the circuit filter is a free-text input for the Ergast `circuitId` slug (e.g. `monza`), which is what the backend query needs directly anyway. This is a deliberate, documented simplification for a hobby-project POC (NFR-14), not an oversight.

## Tasks / Subtasks

### Task 1: Backend — `IErgastClient`/`ErgastClient` new methods (AC: 1, 2, 3)

- [ ] `IErgastClient.cs` — add:
  ```csharp
  Task<IReadOnlyList<ErgastDriverDto>> GetAllDriversAsync(CancellationToken cancellationToken);
  Task<IReadOnlyList<ErgastRaceResultRaceDto>> GetFilteredDriverResultsAsync(string driverId, int? season, string? circuitId, CancellationToken cancellationToken);
  Task<IReadOnlyList<ErgastQualifyingRaceDto>> GetDriverQualifyingHistoryAsync(string driverId, int? season, string? circuitId, CancellationToken cancellationToken);
  ```
- [ ] `ErgastClient.cs`:
  ```csharp
  public async Task<IReadOnlyList<ErgastDriverDto>> GetAllDriversAsync(CancellationToken cancellationToken)
  {
      var response = await httpClient.GetFromJsonAsync<ErgastDriverInfoResponseDto>(
          "drivers.json?limit=1000", cancellationToken)
          ?? throw new InvalidOperationException("Ergast returned an empty response for the all-time driver list.");

      return response.MRData.DriverTable.Drivers;
  }

  private static string BuildDriverScopedPath(string driverId, int? season, string? circuitId, string resource)
  {
      var prefix = season is not null ? $"{season}/" : "";
      var circuitSegment = circuitId is not null ? $"circuits/{circuitId}/" : "";
      return $"{prefix}drivers/{driverId}/{circuitSegment}{resource}";
  }

  public async Task<IReadOnlyList<ErgastRaceResultRaceDto>> GetFilteredDriverResultsAsync(string driverId, int? season, string? circuitId, CancellationToken cancellationToken)
  {
      var path = BuildDriverScopedPath(driverId, season, circuitId, "results.json?limit=1000");
      var response = await httpClient.GetFromJsonAsync<ErgastRaceResultResponseDto>(path, cancellationToken)
          ?? throw new InvalidOperationException($"Ergast returned an empty response for {path}.");

      return response.MRData.RaceTable.Races;
  }

  public async Task<IReadOnlyList<ErgastQualifyingRaceDto>> GetDriverQualifyingHistoryAsync(string driverId, int? season, string? circuitId, CancellationToken cancellationToken)
  {
      var path = BuildDriverScopedPath(driverId, season, circuitId, "qualifying.json?limit=1000");
      var response = await httpClient.GetFromJsonAsync<ErgastQualifyingResponseDto>(path, cancellationToken)
          ?? throw new InvalidOperationException($"Ergast returned an empty response for {path}.");

      return response.MRData.RaceTable.Races;
  }
  ```
  **Note**: `GetFilteredDriverResultsAsync`/`GetDriverQualifyingHistoryAsync` are new, separate methods from Story 5.2's unfiltered `GetAllDriverResultsAsync` — kept distinct rather than retrofitting optional params onto the existing method, so Story 5.2's `DriverProfileService` call site needs no changes and carries no regression risk.

### Task 2: Backend — `HeadToHead` models (AC: 1, 2, 3)

- [ ] Create `backend/F1App.Api/Models/HeadToHeadComparison.cs`:
  ```csharp
  namespace F1App.Api.Models;

  public record DriverOption(string DriverId, string FullName);

  public record HeadToHeadDriverStats(
      string DriverId,
      string FullName,
      double? QualifyingAveragePosition,
      double? RaceFinishAveragePosition,
      int DnfCount,
      int PointsScored,
      int FastestLaps,
      int Wins,
      int RacesCompared);

  public record HeadToHeadComparison(HeadToHeadDriverStats DriverA, HeadToHeadDriverStats DriverB);
  ```

### Task 3: Backend — `HeadToHeadService` (AC: 1, 2, 3)

- [ ] `CacheKeys.cs` — add:
  ```csharp
  public const string AllDrivers = "drivers:all";
  public static string DriverStatsForComparison(string driverId, int? season, string? circuitId) =>
      $"driver:h2h-stats:{driverId}:{season?.ToString() ?? "any"}:{circuitId ?? "any"}";
  ```
- [ ] Create `backend/F1App.Api/Services/HeadToHeadService.cs`:
  ```csharp
  using System.Globalization;
  using F1App.Api.Clients;
  using F1App.Api.Models;
  using Microsoft.Extensions.Caching.Memory;

  namespace F1App.Api.Services;

  public class HeadToHeadService(IErgastClient ergastClient, IMemoryCache cache)
  {
      private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

      public async Task<IReadOnlyList<DriverOption>> GetAllDriversAsync(CancellationToken cancellationToken)
      {
          if (cache.TryGetValue(CacheKeys.AllDrivers, out IReadOnlyList<DriverOption>? cached) && cached is not null)
              return cached;

          var drivers = await ergastClient.GetAllDriversAsync(cancellationToken);
          var options = drivers
              .Select(d => new DriverOption(d.DriverId, $"{d.GivenName} {d.FamilyName}"))
              .OrderBy(d => d.FullName)
              .ToList();

          cache.Set(CacheKeys.AllDrivers, (IReadOnlyList<DriverOption>)options, CacheTtl);
          return options;
      }

      public async Task<HeadToHeadComparison?> CompareAsync(
          string driverIdA, string driverIdB, int? season, string? circuitId, CancellationToken cancellationToken)
      {
          var statsA = await GetDriverStatsAsync(driverIdA, season, circuitId, cancellationToken);
          var statsB = await GetDriverStatsAsync(driverIdB, season, circuitId, cancellationToken);

          return statsA is null || statsB is null ? null : new HeadToHeadComparison(statsA, statsB);
      }

      private async Task<HeadToHeadDriverStats?> GetDriverStatsAsync(
          string driverId, int? season, string? circuitId, CancellationToken cancellationToken)
      {
          var cacheKey = CacheKeys.DriverStatsForComparison(driverId, season, circuitId);
          if (cache.TryGetValue(cacheKey, out HeadToHeadDriverStats? cached))
              return cached;

          var driverInfo = await ergastClient.GetDriverInfoAsync(driverId, cancellationToken);
          if (driverInfo is null)
          {
              cache.Set(cacheKey, (HeadToHeadDriverStats?)null, CacheTtl);
              return null;
          }

          var races = await ergastClient.GetFilteredDriverResultsAsync(driverId, season, circuitId, cancellationToken);
          var qualifying = await ergastClient.GetDriverQualifyingHistoryAsync(driverId, season, circuitId, cancellationToken);

          var wins = 0;
          var dnf = 0;
          var points = 0;
          var fastestLaps = 0;
          var finishPositions = new List<int>();

          foreach (var race in races)
          {
              var result = race.Results.FirstOrDefault();
              if (result is null) continue;

              if (result.Position == "1") wins++;
              if (result.Status is not null && result.Status != "Finished" && !result.Status.StartsWith('+')) dnf++;
              if (result.FastestLap?.Rank == "1") fastestLaps++;
              points += (int)decimal.Parse(result.Points ?? "0", CultureInfo.InvariantCulture);
              if (int.TryParse(result.Position, out var pos)) finishPositions.Add(pos);
          }

          var qualiPositions = qualifying
              .Select(r => r.QualifyingResults.FirstOrDefault())
              .Where(r => r is not null)
              .Select(r => int.Parse(r!.Position, CultureInfo.InvariantCulture))
              .ToList();

          var stats = new HeadToHeadDriverStats(
              driverId,
              $"{driverInfo.GivenName} {driverInfo.FamilyName}",
              qualiPositions.Count == 0 ? null : qualiPositions.Average(),
              finishPositions.Count == 0 ? null : finishPositions.Average(),
              dnf,
              points,
              fastestLaps,
              wins,
              races.Count);

          cache.Set(cacheKey, stats, CacheTtl);
          return stats;
      }
  }
  ```
  **Design note**: per-driver stats are cached keyed by `(driverId, season, circuitId)`, not per-pair — a session comparing one driver against several others reuses that one driver's cached stats across every comparison, rather than caching `N²` pair combinations.

### Task 4: Backend — `DriversController` additions, DI registration (AC: 1, 2, 3)

- [ ] `DriversController.cs` — add `HeadToHeadService headToHeadService` to the constructor and:
  ```csharp
  [HttpGet]
  public async Task<ActionResult<IReadOnlyList<DriverOption>>> GetAllDrivers(CancellationToken cancellationToken)
  {
      var drivers = await headToHeadService.GetAllDriversAsync(cancellationToken);
      return Ok(drivers);
  }

  [HttpGet("compare")]
  public async Task<ActionResult<HeadToHeadComparison>> Compare(
      [FromQuery] string driverA, [FromQuery] string driverB,
      [FromQuery] int? season, [FromQuery] string? circuitId,
      CancellationToken cancellationToken)
  {
      var comparison = await headToHeadService.CompareAsync(driverA, driverB, season, circuitId, cancellationToken);
      return comparison is null ? NotFound() : Ok(comparison);
  }
  ```
  **Route precedence note**: `[HttpGet("compare")]` and `[HttpGet("{driverId}")]` (from Story 5.2) coexist safely on the same controller — ASP.NET Core's routing always prefers a literal segment match (`compare`) over a parameterized one (`{driverId}`) at the same position, so `GET /api/drivers/compare` never gets misrouted to the profile action with `driverId="compare"`.
- [ ] `Program.cs` — `builder.Services.AddScoped<HeadToHeadService>();`.

### Task 5: Backend — Tests (AC: 1, 2, 3)

- [ ] `ErgastClientContractTests.cs` — add tests for `GetAllDriversAsync`, `GetFilteredDriverResultsAsync` (verify the URL path differs correctly with/without season and/or circuitId), `GetDriverQualifyingHistoryAsync`.
- [ ] Create `backend/F1App.Api.Tests/Services/HeadToHeadServiceTests.cs`:
  - `GetAllDriversAsync_ReturnsSortedDriverOptions`.
  - `CompareAsync_ReturnsNullWhenEitherDriverNotFound`.
  - `CompareAsync_ComputesStatsForBothDrivers` — small fixture, one driver with a DNF/win/fastest-lap, the other without, verifying each of the six stats independently.
  - `CompareAsync_PassesSeasonAndCircuitFiltersThroughToErgastClient` — verifies `GetFilteredDriverResultsAsync`/`GetDriverQualifyingHistoryAsync` are called with the given season/circuitId, not `null`.
  - `GetDriverStatsAsync_ReturnsNullAveragesWhenNoQualifyingOrRaceData` (e.g. a filter combination with zero results).
- [ ] `backend/F1App.Api.Tests/Controllers/DriversControllerTests.cs` — add `GetAllDrivers_ReturnsCamelCaseDriverOptions` and `Compare_ReturnsCamelCaseComparison`.
- [ ] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass. *(Could not be executed in this environment — no .NET SDK installed; same constraint as every prior Epic 4/5 story.)*

### Task 6: Frontend — Schema, hooks (AC: 1, 2, 3)

- [ ] `frontend/src/shared/api/ergast.ts` — add `DriverOptionSchema`, `HeadToHeadDriverStatsSchema` (nullable averages), `HeadToHeadComparisonSchema`, and:
  ```ts
  export function useAllDrivers() {
    return useQuery({
      queryKey: queryKeys.allDrivers,
      queryFn: ({ signal }) => fetchJson('/api/drivers', DriverOptionsSchema, signal),
      staleTime: HISTORICAL_STALE_TIME_MS,
      retry: false,
    })
  }

  export function useHeadToHeadComparison(driverA: string | null, driverB: string | null, season: number | null, circuitId: string | null) {
    const params = new URLSearchParams({ driverA: driverA ?? '', driverB: driverB ?? '' })
    if (season) params.set('season', String(season))
    if (circuitId) params.set('circuitId', circuitId)

    return useQuery({
      queryKey: queryKeys.headToHead(driverA ?? '', driverB ?? '', season, circuitId),
      queryFn: ({ signal }) => fetchNullable404Json(`/api/drivers/compare?${params}`, HeadToHeadComparisonSchema, signal),
      enabled: !!driverA && !!driverB,
      retry: false,
    })
  }
  ```
- [ ] `frontend/src/shared/api/queryKeys.ts` — add `allDrivers` and `headToHead(driverA, driverB, season, circuitId)`.
- [ ] `frontend/src/shared/mocks/handlers/ergastHandlers.ts` — add `sampleDriverOptions` (small list) and `sampleHeadToHeadComparison` fixtures + `/api/drivers` and `/api/drivers/compare` handlers (the latter reading `driverA`/`driverB` query params to decide which fixture pair to return, 404 if either is unrecognized — mirrors real backend semantics).

### Task 7: Frontend — `DriverSearchSelect` + `HeadToHeadPage` (AC: 1, 2, 3)

- [ ] Create `frontend/src/features/profiles/DriverSearchSelect.tsx` — text input that filters the full `DriverOption[]` list client-side (case-insensitive substring match on `fullName`) and shows a dropdown of matches; selecting one calls `onSelect(driverId)`. Reused twice (Driver A / Driver B) on the head-to-head page.
- [ ] Create `frontend/src/features/profiles/HeadToHeadPage.tsx` — one `<h1>`, two `<DriverSearchSelect>` instances, a season number input and a circuitId text input (both optional, additive per AC 2), and a side-by-side stat table (real `<table>`, per Accessibility Floor) once both drivers are selected — six stat rows: qualifying avg, race finish avg, DNFs, points, fastest laps, wins.
- [ ] `frontend/src/features/profiles/index.ts` — export `HeadToHeadPage`.

### Task 8: Frontend — Routing (AC: 1, 2, 3)

- [ ] `frontend/src/router.tsx` — add `{ path: 'head-to-head', element: <HeadToHeadPage /> }`.

### Task 9: Frontend — Tests (AC: 1, 2, 3)

- [ ] Create `frontend/src/features/profiles/HeadToHeadPage.test.tsx`:
  - Selecting two drivers via the search inputs renders the stat table with both drivers' six stats.
  - Typing a season and/or circuitId filter triggers a recalculated comparison (assert the new MSW-mocked filtered response renders).
  - No comparison shown until both drivers are selected.
- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass.
- [ ] Run `npx tsc --noEmit -p tsconfig.app.json` from `frontend/` — must be clean.

## Dev Notes

### Architecture Alignment

- Matches `architecture.md`'s `HeadToHeadPage.tsx` (FR-22) and completes `DriversController.cs`'s "FR-21,22" comment (Story 5.2 did FR-21; this story adds the FR-22 routes to the same controller).
- `GetFilteredDriverResultsAsync`/`GetDriverQualifyingHistoryAsync` reuse the exact same `ErgastRaceResultRaceDto`/`ErgastQualifyingRaceDto` DTOs already in the codebase — no new response-shape DTOs needed, only new URL-building client methods.

### Regressions to Guard

- Do not retrofit optional `season`/`circuitId` parameters onto Story 5.2's `GetAllDriverResultsAsync` — this story adds separate, new client methods specifically to avoid touching that call site.
- The per-driver-stats cache key must include `season` and `circuitId` (not just `driverId`) — a driver's all-time stats and their stats "at Monza only" are different values that must not collide in the cache.
- `[HttpGet("compare")]` must be declared on `DriversController` alongside (not replacing) `[HttpGet("{driverId}")]` — verify both routes resolve correctly, since this is the first time this controller has more than one `GET` action.

### Files to Create / Modify

**Backend CREATE:**
- `backend/F1App.Api/Models/HeadToHeadComparison.cs`
- `backend/F1App.Api/Services/HeadToHeadService.cs`
- `backend/F1App.Api.Tests/Services/HeadToHeadServiceTests.cs`

**Backend MODIFY:**
- `backend/F1App.Api/Clients/IErgastClient.cs`
- `backend/F1App.Api/Clients/ErgastClient.cs`
- `backend/F1App.Api/Services/CacheKeys.cs`
- `backend/F1App.Api/Controllers/DriversController.cs`
- `backend/F1App.Api/Program.cs`
- `backend/F1App.Api.Tests/Clients/ErgastClientContractTests.cs`
- `backend/F1App.Api.Tests/Controllers/DriversControllerTests.cs`

**Frontend CREATE:**
- `frontend/src/features/profiles/HeadToHeadPage.tsx`
- `frontend/src/features/profiles/HeadToHeadPage.test.tsx`
- `frontend/src/features/profiles/DriverSearchSelect.tsx`

**Frontend MODIFY:**
- `frontend/src/shared/api/ergast.ts`
- `frontend/src/shared/api/queryKeys.ts`
- `frontend/src/shared/mocks/handlers/ergastHandlers.ts`
- `frontend/src/features/profiles/index.ts`
- `frontend/src/router.tsx`

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.3: Driver Head-to-Head Comparison]
- [Source: _bmad-output/planning-artifacts/architecture.md — HeadToHeadPage.tsx, DriversController.cs FR-22]
- [Live-verified: `https://api.jolpi.ca/ergast/f1/drivers.json`, `/{season}/drivers/{id}/results.json`, `/drivers/{id}/circuits/{id2}/results.json`, `/{season}/drivers/{id}/circuits/{id2}/qualifying.json`]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

None.

### Completion Notes List

- Implemented per plan. All Ergast/Jolpica filter-combination URLs (season-only, circuit-only, season+circuit, for both results and qualifying) were verified live before writing any code, per the story's own "API behaviour verified live" section.
- `DriverSearchSelect.tsx`'s `<label>` needed an explicit `htmlFor`/`id` pairing — initially omitted, then fixed after `getByLabelText` in the test file correctly failed to resolve it (an accessibility gap that would otherwise have shipped silently, caught by the test rather than a manual review).
- Test helper `selectDriver` had to become `async` using `findByRole` instead of a synchronous `getByRole` — the driver dropdown's matches list depends on `useAllDrivers()` data which isn't available synchronously on first render, so typing into the search box before that query resolves finds zero matches.
- MSW handler order matters for `/api/drivers`, `/api/drivers/compare`, and `/api/drivers/:driverId` — the two more specific paths are registered before the parameterized one so MSW's first-match handler resolution doesn't route `compare` through the `:driverId` profile handler (mirrors the equivalent, already-safe ASP.NET Core route-precedence note for the real backend).
- Circuit filter is a free-text `circuitId` input rather than a dropdown, a deliberate documented scope decision (no "list all circuits" data source is wired into the frontend yet) rather than an oversight.
- **Environment note (same as every Epic 4/5 story)**: no .NET SDK in this environment, so backend changes were written and manually reviewed but not compiler-verified via `dotnet build`/`dotnet test`.
- All frontend tests pass except the 4 pre-existing, unrelated `dateUtils.test.ts` locale failures; `tsc -p tsconfig.app.json` is clean except the one pre-existing `TrackMap.test.tsx` issue noted in Story 5.1; `eslint` shows only the one pre-existing-pattern `CircuitTrackLayout.tsx` finding already flagged in Story 5.1 (no new findings).

### File List

See "Files to Create / Modify" above — unchanged from plan, plus the `DriverSearchSelect.tsx` label-association fix noted above.

### Change Log

| Date | Change |
|------|--------|
| 2026-07-10 | Story created and implemented directly (bmad create-story + dev-story cycle) |
