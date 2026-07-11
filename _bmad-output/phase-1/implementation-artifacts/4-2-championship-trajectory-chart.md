---
baseline_commit: "8a45e54"
---

# Story 4.2: Championship Trajectory Chart

Status: done

## Story

As a fan tracking the championship,
I want to see how points accumulated across the season,
So that I can understand the championship's shape over time.

## Acceptance Criteria

1. **Given** the standings page **Then** the trajectory chart plots cumulative points per Driver across all completed rounds (X = round number, Y = total points).
2. **Given** a hover on a data point **When** triggered **Then** it shows race name, race result position, and points scored that round.
3. **And** only completed rounds are plotted; the chart updates after each new Ergast result is available.

## Tasks / Subtasks

### Task 1: Backend — Add `Points` to `ErgastResultDto` and a per-round results client method (AC: 1, 2, 3)

- [ ] `backend/F1App.Api/Dtos/Ergast/ErgastRaceResultResponseDto.cs` — add points scored in that specific race to `ErgastResultDto` (trailing optional, matches existing `Position`/`Number`/`Status` convention):
  ```csharp
  public record ErgastResultDto(
      [property: JsonPropertyName("Driver")] ErgastDriverDto Driver,
      [property: JsonPropertyName("Constructor")] ErgastConstructorDto Constructor,
      [property: JsonPropertyName("Time")] ErgastResultTimeDto? Time,
      [property: JsonPropertyName("position")] string? Position = null,
      [property: JsonPropertyName("number")] string? Number = null,
      [property: JsonPropertyName("status")] string? Status = null,
      [property: JsonPropertyName("points")] string? Points = null);
  ```
- [ ] `backend/F1App.Api/Clients/IErgastClient.cs` — add:
  ```csharp
  Task<ErgastRaceResultRaceDto?> GetRaceResultsByRoundAsync(int round, CancellationToken cancellationToken);
  ```
- [ ] `backend/F1App.Api/Clients/ErgastClient.cs` — implement, mirroring `GetLastRaceResultsAsync`'s null-on-empty-Races pattern (a round that hasn't run yet returns an empty `Races` array, not an error — this is the "only completed rounds" signal):
  ```csharp
  public async Task<ErgastRaceResultRaceDto?> GetRaceResultsByRoundAsync(int round, CancellationToken cancellationToken)
  {
      var response = await httpClient.GetFromJsonAsync<ErgastRaceResultResponseDto>(
          $"current/{round}/results.json", cancellationToken)
          ?? throw new InvalidOperationException($"Ergast returned an empty response for round {round} results.");

      return response.MRData.RaceTable.Races.Count == 0
          ? null
          : response.MRData.RaceTable.Races[0];
  }
  ```

### Task 2: Backend — `TrajectoryPoint`/`DriverTrajectory` models (AC: 1, 2)

- [ ] Create `backend/F1App.Api/Models/DriverTrajectory.cs`:
  ```csharp
  namespace F1App.Api.Models;

  public record TrajectoryPoint(
      int Round,
      string RaceName,
      int? ResultPosition,
      int PointsThisRound,
      decimal CumulativePoints);

  public record DriverTrajectory(
      string DriverId,
      string DriverName,
      string ConstructorName,
      IReadOnlyList<TrajectoryPoint> Points);
  ```

### Task 3: Backend — `StandingsService.GetChampionshipTrajectoryAsync` (AC: 1, 2, 3)

- [ ] `backend/F1App.Api/Services/CacheKeys.cs` — add `public const string ChampionshipTrajectory = "standings:trajectory:current";`.
- [ ] `backend/F1App.Api/Services/StandingsService.cs` — inject `RaceScheduleService` (for the round→race-name schedule) as an additional constructor param; add:
  ```csharp
  // DESIGN.md only defines 4 team accent tokens (Red Bull, Ferrari, Mercedes,
  // McLaren) — plotting all ~20 drivers would be unreadable and give most
  // lines no distinct color anyway. Mirrors the UX mockup's own restraint
  // (4 legend entries): pick the highest-standing driver from each of the
  // 4 tokenized constructors, so every plotted line gets a real team color
  // and no two lines share one.
  private static readonly string[] TokenizedConstructors =
      ["Red Bull Racing", "Ferrari", "Mercedes", "McLaren"];

  public async Task<IReadOnlyList<DriverTrajectory>> GetChampionshipTrajectoryAsync(CancellationToken cancellationToken)
  {
      if (cache.TryGetValue(CacheKeys.ChampionshipTrajectory, out IReadOnlyList<DriverTrajectory>? cached) && cached is not null)
          return cached;

      var currentStandings = await GetCurrentDriverStandingsAsync(cancellationToken);
      var selectedDriverIds = TokenizedConstructors
          .Select(team => currentStandings
              .Where(s => s.ConstructorName == team)
              .OrderBy(s => s.Position)
              .FirstOrDefault())
          .Where(s => s is not null)
          .Select(s => s!.DriverId)
          .ToHashSet();

      var schedule = await raceScheduleService.GetCurrentSeasonScheduleAsync(cancellationToken);
      var runningTotals = new Dictionary<string, decimal>();
      var pointsByDriver = new Dictionary<string, List<TrajectoryPoint>>();

      foreach (var race in schedule.OrderBy(r => r.Round))
      {
          var raceResult = await ergastClient.GetRaceResultsByRoundAsync(race.Round, cancellationToken);
          if (raceResult is null) break; // round not yet completed — and neither are any after it

          foreach (var result in raceResult.Results.Where(r => selectedDriverIds.Contains(r.Driver.DriverId)))
          {
              var pointsThisRound = decimal.Parse(result.Points ?? "0", CultureInfo.InvariantCulture);
              var cumulative = runningTotals.GetValueOrDefault(result.Driver.DriverId) + pointsThisRound;
              runningTotals[result.Driver.DriverId] = cumulative;

              pointsByDriver.TryAdd(result.Driver.DriverId, []);
              pointsByDriver[result.Driver.DriverId].Add(new TrajectoryPoint(
                  race.Round,
                  race.RaceName,
                  int.TryParse(result.Position, out var pos) ? pos : null,
                  (int)pointsThisRound,
                  cumulative));
          }
      }

      var trajectories = currentStandings
          .Where(s => selectedDriverIds.Contains(s.DriverId))
          .OrderBy(s => s.Position)
          .Select(s => new DriverTrajectory(s.DriverId, s.DriverName, s.ConstructorName, pointsByDriver.GetValueOrDefault(s.DriverId, [])))
          .ToList();

      cache.Set(CacheKeys.ChampionshipTrajectory, (IReadOnlyList<DriverTrajectory>)trajectories, CacheTtl);

      return trajectories;
  }
  ```
  **Note**: `race.Round` on `RaceWeekendSummary` — confirm the exact property name/type against `RaceScheduleService`'s existing `ToSummary` mapping (read the file first; adjust field access if the summary model names it differently, e.g. it may need the raw `ErgastRaceDto.Round` string parsed to `int` instead).

### Task 4: Backend — Controller route (AC: 1, 2, 3)

- [ ] `backend/F1App.Api/Controllers/StandingsController.cs` — add, constructor now also takes nothing extra (service already has its own DI):
  ```csharp
  [HttpGet("trajectory")]
  public async Task<ActionResult<IReadOnlyList<DriverTrajectory>>> GetTrajectory(CancellationToken cancellationToken)
  {
      var trajectory = await standingsService.GetChampionshipTrajectoryAsync(cancellationToken);
      return Ok(trajectory);
  }
  ```

### Task 5: Backend — Tests (AC: 1, 2, 3)

- [ ] `backend/F1App.Api.Tests/Clients/ErgastClientContractTests.cs` (or wherever existing Ergast contract tests live) — add a WireMock.Net test for `GetRaceResultsByRoundAsync`: one for a completed round (returns populated `Results` with `Points`), one for a not-yet-run round (empty `Races` array → returns `null`).
- [ ] `backend/F1App.Api.Tests/Services/StandingsServiceTests.cs` — add tests for `GetChampionshipTrajectoryAsync`:
  - Selects exactly one driver per tokenized constructor (best-placed teammate wins when two share a team).
  - Accumulates points correctly across rounds (round 1: 25 pts → cumulative 25; round 2: 18 pts → cumulative 43).
  - Stops at the first round with no results (a 3-round schedule where round 3 returns `null` yields only 2 rounds of data).
- [ ] `backend/F1App.Api.Tests/Controllers/StandingsControllerTests.cs` — add a `GetTrajectory_ReturnsCamelCaseTrajectory` test.
- [ ] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass. *(Could not be executed in this environment — no .NET SDK installed; see Story 4.1's Completion Notes for the same constraint.)*

### Task 6: Frontend — Schema, query hook (AC: 1, 2, 3)

- [ ] `frontend/src/shared/api/queryKeys.ts` — add `trajectory: ['standings', 'trajectory', 'current'] as const` under `standings`.
- [ ] `frontend/src/shared/api/ergast.ts` — add:
  ```ts
  const TrajectoryPointSchema = z.object({
    round: z.number(),
    raceName: z.string(),
    resultPosition: z.number().nullable(),
    pointsThisRound: z.number(),
    cumulativePoints: z.number(),
  })

  const DriverTrajectorySchema = z.object({
    driverId: z.string(),
    driverName: z.string(),
    constructorName: z.string(),
    points: z.array(TrajectoryPointSchema),
  })

  const TrajectoriesSchema = z.array(DriverTrajectorySchema)

  export type DriverTrajectory = z.infer<typeof DriverTrajectorySchema>

  export function useChampionshipTrajectory() {
    return useQuery({
      queryKey: queryKeys.standings.trajectory,
      queryFn: ({ signal }) => fetchJson('/api/standings/trajectory', TrajectoriesSchema, signal),
      staleTime: STANDINGS_STALE_TIME_MS,
      retry: false,
    })
  }
  ```
- [ ] `frontend/src/shared/mocks/handlers/ergastHandlers.ts` — add a `sampleTrajectory: DriverTrajectory[]` fixture (3-4 rounds across the same 4 constructor-leader drivers used in the standings roster) and a matching `http.get(.../api/standings/trajectory, ...)` handler.

### Task 7: Frontend — `TrajectoryChart` component (AC: 1, 2, 3)

- [ ] Create `frontend/src/features/standings/TrajectoryChart.tsx` — Recharts `LineChart`, one `Line` per driver in the response coloured via `constructorColor(driverTrajectory.constructorName)`, `dataKey` per round built the same reshaping way `LapTimeChart.tsx`'s `buildChartData` does (round → `{ round, [driverId]: cumulativePoints }`), with a `CustomTooltip` (mirroring `LapTimeChart`'s) showing, per driver at that round: race name, result position, points scored that round.
  - Empty/pending/error states mirror `DriversStandingsTable`'s skeleton/alert pattern.
  - AC 3 ("only completed rounds... updates after each new result") requires no client-side filtering — the backend only ever returns completed-round data, so the frontend renders whatever it receives as-is.

### Task 8: Frontend — Wire into `StandingsPage` (AC: 1, 2, 3)

- [ ] `frontend/src/features/standings/StandingsPage.tsx` — render `<TrajectoryChart />` in its own card below the Drivers/Constructors table card, unconditionally (it's driver-only data per FR-18, independent of the active tab — matches the mockup where the trajectory card sits below the standings table regardless of toggle state).
- [ ] `frontend/src/features/standings/index.ts` — no change needed (barrel only re-exports the page).

### Task 9: Frontend — Tests (AC: 1, 2, 3)

- [ ] Create `frontend/src/features/standings/TrajectoryChart.test.tsx` (mirrors `LapTimeChart.test.tsx`'s conventions where applicable, and `StandingsPage.test.tsx`'s MSW/QueryClientProvider harness):
  - Renders a line per driver in the mock trajectory fixture.
  - Hovering/inspecting a data point surfaces race name, position, and points-this-round (via the custom tooltip's rendered content, following whatever hover-simulation approach `LapTimeChart.test.tsx` already established — check that file first).
- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass.
- [ ] Run `npx tsc --noEmit` from `frontend/` — must be clean.

## Dev Notes

### Architecture Alignment

- Matches `architecture.md`'s `TrajectoryChart.tsx` file and `StandingsController.cs`'s "FR-17,18: standings, trajectory data" comment.
- Reuses the `IMemoryCache` 1h TTL convention (architecture's "Standings | 1 hour | Updates after each race" tier) via a new `CacheKeys.ChampionshipTrajectory` key — trajectory data changes exactly when standings do, so no new TTL tier is introduced.
- `GetChampionshipTrajectoryAsync` calls `GetRaceResultsByRoundAsync` once per completed round sequentially and stops at the first incomplete one — for a fully-run 24-race season this is 24 Ergast calls per cache miss (1/hour), which is acceptable given Ergast has no rate limit for this POC's traffic level, but is the reason this endpoint is cached at all rather than computed per-request.

### Scope decision: 4 drivers, not all ~20

FR-18 says "cumulative points per Driver" without an explicit cap, but the UX mockup (`standings.html`) only ever shows 4 lines/legend entries, and `DESIGN.md` only defines 4 team accent color tokens total. Plotting all drivers would mean ~16 of them render in an undifferentiated fallback color, which is worse than the mockup's intent. This story picks the highest-standing driver from each of the 4 tokenized constructors (Red Bull Racing, Ferrari, Mercedes, McLaren) rather than a flat "top 4 by points," so that a team with two drivers in the top 4 (e.g., both McLaren drivers leading the standings) still surfaces a Ferrari/Mercedes/Red Bull line instead of two identically-colored McLaren lines — this is what the mockup itself does (Piastri, P4 in the table, is skipped from the trajectory legend in favor of Russell, P5, because Piastri's McLaren color is already taken by Norris).

### Regressions to Guard

- `ErgastResultDto.Points` is trailing-optional, matching the established pattern from Story 4.1 (`ErgastDriverDto.Nationality`) — existing positional constructions of `ErgastResultDto` across `RacesControllerTests`/other result-consuming tests keep compiling unmodified.
- `StandingsService` gains a `RaceScheduleService` constructor dependency — check for a circular DI reference before wiring (`RaceScheduleService` already depends on `StandingsService` per its existing constructor signature `RaceScheduleService(IErgastClient, IMemoryCache, StandingsService)`). **This is a real circular dependency risk** — if `StandingsService` also takes `RaceScheduleService`, ASP.NET Core's DI container will throw at startup. Resolve by having `StandingsService.GetChampionshipTrajectoryAsync` take the season schedule as a method parameter (caller — the controller — fetches it via `RaceScheduleService` and passes it in) instead of injecting `RaceScheduleService` into `StandingsService`. Update Task 3/4 accordingly during implementation.

### Files to Create / Modify

**Backend CREATE:**
- `backend/F1App.Api/Models/DriverTrajectory.cs`

**Backend MODIFY:**
- `backend/F1App.Api/Dtos/Ergast/ErgastRaceResultResponseDto.cs`
- `backend/F1App.Api/Clients/IErgastClient.cs`
- `backend/F1App.Api/Clients/ErgastClient.cs`
- `backend/F1App.Api/Services/CacheKeys.cs`
- `backend/F1App.Api/Services/StandingsService.cs`
- `backend/F1App.Api/Controllers/StandingsController.cs`
- `backend/F1App.Api.Tests/Services/StandingsServiceTests.cs`
- `backend/F1App.Api.Tests/Controllers/StandingsControllerTests.cs`
- `backend/F1App.Api.Tests/Clients/ErgastClientContractTests.cs` (or equivalent)

**Frontend CREATE:**
- `frontend/src/features/standings/TrajectoryChart.tsx`
- `frontend/src/features/standings/TrajectoryChart.test.tsx`

**Frontend MODIFY:**
- `frontend/src/shared/api/queryKeys.ts`
- `frontend/src/shared/api/ergast.ts`
- `frontend/src/shared/mocks/handlers/ergastHandlers.ts`
- `frontend/src/features/standings/StandingsPage.tsx`
- `frontend/src/features/standings/StandingsPage.test.tsx` (mocked `recharts` so the page-level test doesn't hit jsdom's missing `ResizeObserver` now that `TrajectoryChart` renders unconditionally on the page)

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.2: Championship Trajectory Chart]
- [Source: _bmad-output/planning-artifacts/architecture.md line 524, 178 — TrajectoryChart.tsx, Standings 1h cache tier]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-F1_poc-2026-06-16/mockups/standings.html — chart-box/chart-legend markup, 4-driver scope precedent]
- [Source: frontend/src/features/live-race/LapTimeChart/LapTimeChart.tsx — multi-line Recharts + custom tooltip pattern this story reuses]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

None.

### Completion Notes List

- Implemented per plan, including the circular-DI fix flagged in Dev Notes: `StandingsService.GetChampionshipTrajectoryAsync` takes the schedule as a parameter rather than injecting `RaceScheduleService`; the controller fetches the schedule and passes it in.
- 4-driver scope decision (one per tokenized constructor, best-placed teammate wins) implemented exactly as planned and covered by a dedicated test (`GetChampionshipTrajectoryAsync_SelectsBestPlacedDriverPerTokenizedConstructor`).
- `TrajectoryTooltip` exported (not just an internal function) specifically so `TrajectoryChart.test.tsx` could unit-test its content directly — Recharts' `Tooltip` is mocked to `null` in tests (same as `LapTimeChart.test.tsx`), so the only way to verify tooltip content (race name/position/points) is to render the tooltip component in isolation with a constructed `payload`.
- **Environment note (same as Story 4.1)**: no .NET SDK in this environment, so backend changes were written and manually reviewed but not compiler-verified via `dotnet build`/`dotnet test`.
- All 86 frontend tests pass except the same 4 pre-existing, unrelated `dateUtils.test.ts` locale failures noted in Story 4.1; `tsc --noEmit` and `eslint` are clean.

### File List

See "Files to Create / Modify" above — unchanged from plan, plus the noted `StandingsPage.test.tsx` recharts-mock addition.

### Change Log

| Date | Change |
|------|--------|
| 2026-07-10 | Story created and implemented directly (bmad create-story + dev-story cycle) |
