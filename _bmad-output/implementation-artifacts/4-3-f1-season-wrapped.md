---
baseline_commit: "22c6eea"
---

# Story 4.3: F1 Season Wrapped

Status: done

## Story

As a fan at the end of the season,
I want a shareable recap of the season's highlights,
So that I can celebrate and share it with friends.

## Acceptance Criteria

1. **Given** the final race of the season has been completed (per Ergast round count) **Then** a Season Wrapped section appears with: most dramatic race (largest position swings), driver with most DNFs, biggest points comeback, most positions gained in a single race, and the most-improved Constructor.
2. **Given** the season is still in progress **Then** Season Wrapped is not shown.
3. **And** the wrap is calculated on demand from full-season Ergast data and presented as a shareable card, exported client-side via `html-to-image` with no server-side rendering.

## Scope decisions (no dedicated UX mockup exists for this card — EXPERIENCE.md only specifies the "absent when in progress" rule)

FR-19's five stats aren't formally defined by Ergast fields, so this story fixes concrete, documented definitions:

- **Most dramatic race**: sum of `|grid − finish|` across all classified results in a race; the race with the highest total wins.
- **Most DNFs**: per driver, count of results where `status` is neither `"Finished"` nor starts with `"+"` (lapped-but-classified finishes aren't DNFs).
- **Biggest points comeback**: per driver, `(largest gap-to-season-leader across any completed round) − (gap-to-season-leader at the final round)`, computed from cumulative points built from each round's `points` field (not a fresh `driverStandings`-per-round fetch — see Regressions). The driver with the largest positive reduction wins; a driver who led wire-to-wire has a comeback of 0 and cannot win.
- **Most positions gained in a single race**: max single-result `grid − finish` (positive = moved up) across every driver-race entry in the season.
- **Most-improved Constructor**: constructor standings position at an early-season checkpoint (`round = min(5, total scheduled rounds)`) minus final position; the constructor that moved up the most places wins.

These are POC-appropriate heuristics (NFR-14: hobby project, no formal certification), not the only valid interpretations — documented here so future stories don't need to reverse-engineer the definitions from code.

## Tasks / Subtasks

### Task 1: Backend — Add `Grid` to `ErgastResultDto`; new `IErgastClient` method for round-scoped constructor standings (AC: 1)

- [ ] `backend/F1App.Api/Dtos/Ergast/ErgastRaceResultResponseDto.cs` — add starting grid position, trailing optional (same convention as Story 4.2's `Points`):
  ```csharp
  public record ErgastResultDto(
      [property: JsonPropertyName("Driver")] ErgastDriverDto Driver,
      [property: JsonPropertyName("Constructor")] ErgastConstructorDto Constructor,
      [property: JsonPropertyName("Time")] ErgastResultTimeDto? Time,
      [property: JsonPropertyName("position")] string? Position = null,
      [property: JsonPropertyName("number")] string? Number = null,
      [property: JsonPropertyName("status")] string? Status = null,
      [property: JsonPropertyName("points")] string? Points = null,
      [property: JsonPropertyName("grid")] string? Grid = null);
  ```
- [ ] `backend/F1App.Api/Clients/IErgastClient.cs` — add:
  ```csharp
  Task<IReadOnlyList<ErgastConstructorStandingDto>> GetConstructorStandingsByRoundAsync(int round, CancellationToken cancellationToken);
  ```
- [ ] `backend/F1App.Api/Clients/ErgastClient.cs` — implement, mirroring `GetCurrentConstructorStandingsAsync` but hitting `current/{round}/constructorStandings.json`:
  ```csharp
  public async Task<IReadOnlyList<ErgastConstructorStandingDto>> GetConstructorStandingsByRoundAsync(int round, CancellationToken cancellationToken)
  {
      var response = await httpClient.GetFromJsonAsync<ErgastConstructorStandingsResponseDto>(
          $"current/{round}/constructorStandings.json", cancellationToken)
          ?? throw new InvalidOperationException($"Ergast returned an empty response for round {round} constructor standings.");

      return response.MRData.StandingsTable.StandingsLists.Count == 0
          ? []
          : response.MRData.StandingsTable.StandingsLists[0].ConstructorStandings;
  }
  ```

### Task 2: Backend — `SeasonWrapped` models (AC: 1)

- [ ] Create `backend/F1App.Api/Models/SeasonWrapped.cs`:
  ```csharp
  namespace F1App.Api.Models;

  public record SeasonWrapped(
      DramaticRaceAward MostDramaticRace,
      DriverStatAward MostDnfs,
      DriverStatAward BiggestPointsComeback,
      DriverRaceAward MostPositionsGainedInARace,
      ConstructorImprovementAward MostImprovedConstructor);

  public record DramaticRaceAward(string RaceName, int Round, int TotalPositionSwing);

  public record DriverStatAward(string DriverId, string DriverName, string ConstructorName, int Value);

  public record DriverRaceAward(string DriverId, string DriverName, string ConstructorName, string RaceName, int PositionsGained);

  public record ConstructorImprovementAward(string ConstructorName, int EarlySeasonPosition, int FinalPosition, int PositionsImproved);
  ```

### Task 3: Backend — `SeasonWrappedService` (AC: 1, 2, 3)

- [ ] `backend/F1App.Api/Services/CacheKeys.cs` — add `public const string SeasonWrapped = "standings:season-wrapped:current";`.
- [ ] Create `backend/F1App.Api/Services/SeasonWrappedService.cs`:
  ```csharp
  using System.Globalization;
  using F1App.Api.Clients;
  using F1App.Api.Dtos.Ergast;
  using F1App.Api.Models;
  using Microsoft.Extensions.Caching.Memory;

  namespace F1App.Api.Services;

  public class SeasonWrappedService(IErgastClient ergastClient, IMemoryCache cache)
  {
      private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

      // Unlike every other cached method in this codebase, a `null` result here
      // (season still in progress) is itself a meaningful, cacheable answer —
      // not a cache miss — so this method uses a bare TryGetValue instead of
      // the `&& cached is not null` guard used by StandingsService. Without this,
      // every single request during an in-progress season would re-walk the
      // "is the final round done yet" check against Ergast.
      public async Task<SeasonWrapped?> GetSeasonWrappedAsync(IReadOnlyList<RaceWeekendSummary> schedule, CancellationToken cancellationToken)
      {
          if (cache.TryGetValue(CacheKeys.SeasonWrapped, out SeasonWrapped? cached))
              return cached;

          var wrapped = await ComputeAsync(schedule, cancellationToken);
          cache.Set(CacheKeys.SeasonWrapped, wrapped, CacheTtl);
          return wrapped;
      }

      private async Task<SeasonWrapped?> ComputeAsync(IReadOnlyList<RaceWeekendSummary> schedule, CancellationToken cancellationToken)
      {
          if (schedule.Count == 0) return null;

          var finalRound = schedule.Max(r => r.Round);
          var finalRoundResult = await ergastClient.GetRaceResultsByRoundAsync(finalRound, cancellationToken);
          if (finalRoundResult is null) return null; // season still in progress — cheap short-circuit, no further calls

          var roundResults = new List<(int Round, string RaceName, ErgastRaceResultRaceDto Result)>();
          foreach (var race in schedule.OrderBy(r => r.Round))
          {
              var result = race.Round == finalRound
                  ? finalRoundResult
                  : await ergastClient.GetRaceResultsByRoundAsync(race.Round, cancellationToken);
              if (result is not null)
                  roundResults.Add((race.Round, race.RaceName, result));
          }

          var mostDramaticRace = FindMostDramaticRace(roundResults);
          var mostDnfs = FindMostDnfs(roundResults);
          var biggestComeback = FindBiggestPointsComeback(roundResults);
          var mostGained = FindMostPositionsGainedInARace(roundResults);
          var mostImprovedConstructor = await FindMostImprovedConstructorAsync(schedule.Count, cancellationToken);

          if (mostDramaticRace is null || mostDnfs is null || biggestComeback is null || mostGained is null || mostImprovedConstructor is null)
              return null; // insufficient data to build a meaningful wrap (e.g. a 0-round or all-DNS season)

          return new SeasonWrapped(mostDramaticRace, mostDnfs, biggestComeback, mostGained, mostImprovedConstructor);
      }

      private static DramaticRaceAward? FindMostDramaticRace(List<(int Round, string RaceName, ErgastRaceResultRaceDto Result)> roundResults)
      {
          (int Round, string RaceName, int Swing)? best = null;
          foreach (var (round, raceName, result) in roundResults)
          {
              var swing = 0;
              foreach (var r in result.Results)
              {
                  if (int.TryParse(r.Grid, out var grid) && int.TryParse(r.Position, out var pos))
                      swing += Math.Abs(grid - pos);
              }
              if (best is null || swing > best.Value.Swing)
                  best = (round, raceName, swing);
          }
          return best is null ? null : new DramaticRaceAward(best.Value.RaceName, best.Value.Round, best.Value.Swing);
      }

      private static DriverStatAward? FindMostDnfs(List<(int Round, string RaceName, ErgastRaceResultRaceDto Result)> roundResults)
      {
          var dnfCounts = new Dictionary<string, (string Name, string Constructor, int Count)>();
          foreach (var (_, _, result) in roundResults)
          {
              foreach (var r in result.Results)
              {
                  var isDnf = r.Status is not null && r.Status != "Finished" && !r.Status.StartsWith('+');
                  if (!isDnf) continue;

                  var existing = dnfCounts.GetValueOrDefault(r.Driver.DriverId, (r.Driver.FamilyName, r.Constructor.Name, 0));
                  dnfCounts[r.Driver.DriverId] = (existing.Name, existing.Constructor, existing.Count + 1);
              }
          }
          if (dnfCounts.Count == 0) return null;
          var top = dnfCounts.OrderByDescending(kv => kv.Value.Count).First();
          return new DriverStatAward(top.Key, top.Value.Name, top.Value.Constructor, top.Value.Count);
      }

      private static DriverStatAward? FindBiggestPointsComeback(List<(int Round, string RaceName, ErgastRaceResultRaceDto Result)> roundResults)
      {
          var cumulative = new Dictionary<string, decimal>();
          var driverMeta = new Dictionary<string, (string Name, string Constructor)>();
          var gapHistoryByDriver = new Dictionary<string, List<decimal>>();

          foreach (var (_, _, result) in roundResults.OrderBy(r => r.Round))
          {
              foreach (var r in result.Results)
              {
                  var pts = decimal.Parse(r.Points ?? "0", CultureInfo.InvariantCulture);
                  cumulative[r.Driver.DriverId] = cumulative.GetValueOrDefault(r.Driver.DriverId) + pts;
                  driverMeta[r.Driver.DriverId] = (r.Driver.FamilyName, r.Constructor.Name);
              }

              var leaderPoints = cumulative.Count == 0 ? 0 : cumulative.Values.Max();
              foreach (var driverId in cumulative.Keys)
              {
                  gapHistoryByDriver.TryAdd(driverId, []);
                  gapHistoryByDriver[driverId].Add(leaderPoints - cumulative[driverId]);
              }
          }

          string? bestDriverId = null;
          var bestComeback = 0m;
          foreach (var (driverId, gaps) in gapHistoryByDriver)
          {
              if (gaps.Count == 0) continue;
              var comeback = gaps.Max() - gaps[^1];
              if (comeback > bestComeback)
              {
                  bestComeback = comeback;
                  bestDriverId = driverId;
              }
          }

          if (bestDriverId is null) return null;
          var meta = driverMeta[bestDriverId];
          return new DriverStatAward(bestDriverId, meta.Name, meta.Constructor, (int)bestComeback);
      }

      private static DriverRaceAward? FindMostPositionsGainedInARace(List<(int Round, string RaceName, ErgastRaceResultRaceDto Result)> roundResults)
      {
          (string DriverId, string Name, string Constructor, string RaceName, int Gained)? best = null;
          foreach (var (_, raceName, result) in roundResults)
          {
              foreach (var r in result.Results)
              {
                  if (!int.TryParse(r.Grid, out var grid) || !int.TryParse(r.Position, out var pos)) continue;
                  var gained = grid - pos;
                  if (best is null || gained > best.Value.Gained)
                      best = (r.Driver.DriverId, r.Driver.FamilyName, r.Constructor.Name, raceName, gained);
              }
          }
          return best is null
              ? null
              : new DriverRaceAward(best.Value.DriverId, best.Value.Name, best.Value.Constructor, best.Value.RaceName, best.Value.Gained);
      }

      private async Task<ConstructorImprovementAward?> FindMostImprovedConstructorAsync(int totalRounds, CancellationToken cancellationToken)
      {
          var checkpointRound = Math.Min(5, totalRounds);
          var checkpointStandings = await ergastClient.GetConstructorStandingsByRoundAsync(checkpointRound, cancellationToken);
          var finalStandings = await ergastClient.GetCurrentConstructorStandingsAsync(cancellationToken);

          var checkpointPositions = checkpointStandings.ToDictionary(
              s => s.Constructor.Name, s => int.Parse(s.Position, CultureInfo.InvariantCulture));

          ConstructorImprovementAward? best = null;
          foreach (var final in finalStandings)
          {
              if (!checkpointPositions.TryGetValue(final.Constructor.Name, out var checkpointPos)) continue;

              var finalPos = int.Parse(final.Position, CultureInfo.InvariantCulture);
              var improved = checkpointPos - finalPos;
              if (best is null || improved > best.PositionsImproved)
                  best = new ConstructorImprovementAward(final.Constructor.Name, checkpointPos, finalPos, improved);
          }
          return best;
      }
  }
  ```
  **Note**: reuses `ErgastConstructorStandingDto`/`ErgastDriverStandingDto` already imported via `F1App.Api.Dtos.Ergast`.

### Task 4: Backend — Register service, controller route (AC: 1, 2, 3)

- [ ] `backend/F1App.Api/Program.cs` — add `builder.Services.AddScoped<SeasonWrappedService>();` alongside `StandingsService`/`RaceScheduleService`.
- [ ] `backend/F1App.Api/Controllers/StandingsController.cs` — add `SeasonWrappedService seasonWrappedService` to the constructor and:
  ```csharp
  [HttpGet("season-wrapped")]
  public async Task<ActionResult<SeasonWrapped?>> GetSeasonWrapped(CancellationToken cancellationToken)
  {
      var schedule = await raceScheduleService.GetCurrentSeasonScheduleAsync(cancellationToken);
      var wrapped = await seasonWrappedService.GetSeasonWrappedAsync(schedule, cancellationToken);
      return Ok(wrapped); // 200 + null body when season in progress — a real "there is nothing yet" state, not a 404
  }
  ```

### Task 5: Backend — Tests (AC: 1, 2, 3)

- [ ] Create `backend/F1App.Api.Tests/Services/SeasonWrappedServiceTests.cs` covering, at minimum:
  - Returns `null` when the final scheduled round has no results yet (season in progress) — and verifies `GetRaceResultsByRoundAsync` was only called once (for the final round), not for every round, proving the cheap short-circuit.
  - Computes `MostDramaticRace` correctly from a small multi-race fixture with known grid/finish deltas.
  - Computes `MostDnfs` correctly, excluding `"Finished"` and lapped (`"+1 Lap"`) statuses.
  - Computes `BiggestPointsComeback` correctly from a fixture where a driver trails badly early then closes the gap.
  - Computes `MostPositionsGainedInARace` correctly from a known grid→finish jump.
  - Computes `MostImprovedConstructor` correctly from distinct checkpoint vs. final constructor standings fixtures.
  - Caches the result (including a cached `null`) and doesn't re-fetch on a second call.
- [ ] `backend/F1App.Api.Tests/Clients/ErgastClientContractTests.cs` — add a WireMock.Net test for `GetConstructorStandingsByRoundAsync`.
- [ ] `backend/F1App.Api.Tests/Controllers/StandingsControllerTests.cs` — add `GetSeasonWrapped_ReturnsNullWhenSeasonInProgress` and a happy-path test.
- [ ] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass. *(Could not be executed in this environment — no .NET SDK installed; same constraint as Stories 4.1/4.2.)*

### Task 6: Frontend — Schema, query hook (AC: 1, 2, 3)

- [ ] `frontend/src/shared/api/queryKeys.ts` — add `seasonWrapped: ['standings', 'season-wrapped', 'current'] as const` under `standings`.
- [ ] `frontend/src/shared/api/ergast.ts` — add schemas/types for `DramaticRaceAward`, `DriverStatAward`, `DriverRaceAward`, `ConstructorImprovementAward`, `SeasonWrapped` (all fields camelCase, mirroring the C# record property names), and:
  ```ts
  const SeasonWrappedSchema = z.object({
    mostDramaticRace: DramaticRaceAwardSchema,
    mostDnfs: DriverStatAwardSchema,
    biggestPointsComeback: DriverStatAwardSchema,
    mostPositionsGainedInARace: DriverRaceAwardSchema,
    mostImprovedConstructor: ConstructorImprovementAwardSchema,
  }).nullable()

  export function useSeasonWrapped() {
    return useQuery({
      queryKey: queryKeys.standings.seasonWrapped,
      queryFn: ({ signal }) => fetchJson('/api/standings/season-wrapped', SeasonWrappedSchema, signal),
      staleTime: STANDINGS_STALE_TIME_MS,
      retry: false,
    })
  }
  ```
  **Note**: `fetchJson`'s current implementation calls `response.json()` unconditionally — a `200` with a literal `null` JSON body parses fine through `.json()` and then through a `.nullable()` zod schema, no special-casing needed.
- [ ] `frontend/src/shared/mocks/handlers/ergastHandlers.ts` — add a `sampleSeasonWrapped: SeasonWrapped` fixture and a `http.get(.../api/standings/season-wrapped, ...)` handler returning it (AC 1 fixture); a second exported `null`-returning variant isn't needed since tests can `server.use(...)` an inline override for the AC-2 "in progress" case.

### Task 7: Frontend — `SeasonWrappedCard` component with `html-to-image` export (AC: 1, 2, 3)

- [ ] Create `frontend/src/features/standings/SeasonWrapped/SeasonWrappedCard.tsx` — renders the 5 stats as a self-contained card (own `ref` for export), each stat labelled clearly (e.g. "Most Dramatic Race", "Most DNFs", "Biggest Points Comeback", "Most Positions Gained in a Race", "Most-Improved Constructor") with the relevant driver/constructor/race name and number.
  - A "Share" / "Download Image" button calls `htmlToImage.toPng(cardRef.current)` (from `html-to-image`) and triggers a browser download of the resulting data URL — this is the first consumer of the dependency in the codebase, establishing the pattern Epic 6's Fan Card will reuse (per epics.md's own framing).
- [ ] Create `frontend/src/features/standings/SeasonWrapped/SeasonWrapped.tsx` — thin wrapper: calls `useSeasonWrapped()`, renders nothing (`return null`) when `data` is `null`/`undefined`/pending/error (AC 2 — absent, not a placeholder), renders `<SeasonWrappedCard wrapped={data} />` when populated (AC 1).

### Task 8: Frontend — Wire into `StandingsPage` (AC: 1, 2)

- [ ] `frontend/src/features/standings/StandingsPage.tsx` — render `<SeasonWrapped />` below `<TrajectoryChart />`.

### Task 9: Frontend — Tests (AC: 1, 2, 3)

- [ ] Create `frontend/src/features/standings/SeasonWrapped/SeasonWrapped.test.tsx`:
  - Renders nothing when the API returns `null` (season in progress).
  - Renders all 5 stat labels and their values when populated.
  - Clicking the export button calls `html-to-image`'s `toPng` (mock the module, since it relies on canvas APIs jsdom doesn't implement).
- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass.
- [ ] Run `npx tsc --noEmit` from `frontend/` — must be clean.

## Dev Notes

### Architecture Alignment

- Matches `architecture.md`'s `SeasonWrapped.tsx` / `SeasonWrappedCard.tsx` file tree and the "FR-19: Season Wrapped" / `html-to-image` export-target comments.
- This is the first consumer of `html-to-image` in the codebase — epics.md explicitly frames this story as "introduces the html-to-image export pattern reused by Epic 6's Fan Card," so the export implementation here (ref-to-node → `toPng` → trigger download) is the pattern Epic 6 should copy, not reinvent.
- New `SeasonWrappedService` (rather than folding into `StandingsService`) mirrors the existing `WinProbabilityService`/`NewsFeedService` precedent of one service per distinct on-demand calculation, and keeps `StandingsService` from growing a fifth unrelated responsibility.

### Regressions to Guard

- `BiggestPointsComeback` deliberately computes cumulative points from each round's `results.points` field rather than issuing one `driverStandings.json` call per round — the latter would be 24+ extra Ergast calls per cache miss on top of the 24 `results.json` calls already required for the other four stats. This trades perfect precision (official standings occasionally reflect post-race penalty adjustments the raw results don't) for roughly half the network cost; acceptable for a POC per NFR-14.
- `GetSeasonWrappedAsync`'s cache check is deliberately `cache.TryGetValue(key, out var cached)` **without** an `&& cached is not null` guard (unlike every sibling method in `StandingsService`) — this is intentional so an in-progress season's `null` result is itself cached and doesn't re-walk the "is the final round done" check on every request. Don't "fix" this to match the other methods' pattern.
- The final round's result is fetched once and reused (not re-fetched inside the main `roundResults` loop) — `GetRaceResultsByRoundAsync` is called exactly `totalRounds` times per cache miss for a completed season, not `totalRounds + 1`.

### Files to Create / Modify

**Backend CREATE:**
- `backend/F1App.Api/Models/SeasonWrapped.cs`
- `backend/F1App.Api/Services/SeasonWrappedService.cs`
- `backend/F1App.Api.Tests/Services/SeasonWrappedServiceTests.cs`

**Backend MODIFY:**
- `backend/F1App.Api/Dtos/Ergast/ErgastRaceResultResponseDto.cs`
- `backend/F1App.Api/Clients/IErgastClient.cs`
- `backend/F1App.Api/Clients/ErgastClient.cs`
- `backend/F1App.Api/Services/CacheKeys.cs`
- `backend/F1App.Api/Controllers/StandingsController.cs`
- `backend/F1App.Api/Program.cs`
- `backend/F1App.Api.Tests/Clients/ErgastClientContractTests.cs`
- `backend/F1App.Api.Tests/Controllers/StandingsControllerTests.cs`

**Frontend CREATE:**
- `frontend/src/features/standings/SeasonWrapped/SeasonWrapped.tsx`
- `frontend/src/features/standings/SeasonWrapped/SeasonWrappedCard.tsx`
- `frontend/src/features/standings/SeasonWrapped/SeasonWrapped.test.tsx`

**Frontend MODIFY:**
- `frontend/src/shared/api/queryKeys.ts`
- `frontend/src/shared/api/ergast.ts`
- `frontend/src/shared/mocks/handlers/ergastHandlers.ts`
- `frontend/src/features/standings/StandingsPage.tsx`
- `frontend/src/features/standings/StandingsPage.test.tsx` (scoped its driver/constructor-name assertions to `within(screen.getByRole('table'))` — once `SeasonWrapped` renders unconditionally on the same page, several sample names (e.g. "Russell", "Mercedes") legitimately appear in both the standings table and the wrapped card, so page-wide `getByText` became ambiguous)

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.3: F1 Season Wrapped]
- [Source: _bmad-output/planning-artifacts/architecture.md line 109, 525-527, 720 — html-to-image, SeasonWrapped.tsx/SeasonWrappedCard.tsx, client-side image generation]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-F1_poc-2026-06-16/EXPERIENCE.md line 74 — Season Wrapped absent entirely when in progress, not a disabled placeholder]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

None.

### Completion Notes List

- Implemented per plan. `FindMostDramaticRace`, `FindMostDnfs`, `FindBiggestPointsComeback`, `FindMostPositionsGainedInARace`, and `FindMostImprovedConstructorAsync` were made `internal` (not `private`) on `SeasonWrappedService`, mirroring `RaceDataOrchestrator`'s established testability convention — this let each of the five award calculations be unit-tested directly with small hand-built fixtures instead of only exercised indirectly through the full orchestration + mocked `IErgastClient`.
- Discovered and fixed a real test collision while wiring `SeasonWrapped` unconditionally onto `StandingsPage`: the mock `sampleSeasonWrapped` fixture reused driver/constructor names ("Russell", "Mercedes", "Hamilton", "Ferrari") that also appear in `sampleDriverStandings`/`sampleConstructorStandings`, making `StandingsPage.test.tsx`'s page-wide `getByText` calls ambiguous. Fixed by scoping those assertions to `within(screen.getByRole('table'))` rather than picking non-overlapping fixture names, since a real app page legitimately can (and here, will) mention the same name in two different sections.
- **Environment note (same as Stories 4.1/4.2)**: no .NET SDK in this environment, so backend changes were written and manually reviewed but not compiler-verified via `dotnet build`/`dotnet test`.
- All 89 frontend tests pass except the same 4 pre-existing, unrelated `dateUtils.test.ts` locale failures noted in prior Epic 4 stories; `tsc --noEmit` and `eslint` are clean. A jsdom-only "Not implemented: navigation" console warning appears when the export button's test clicks the generated `<a href="data:...">` — cosmetic (jsdom doesn't implement data-URL navigation), not a test failure, and irrelevant in a real browser.

### File List

See "Files to Create / Modify" above — unchanged from plan, plus the noted `StandingsPage.test.tsx` scoping fix.

### Change Log

| Date | Change |
|------|--------|
| 2026-07-10 | Story created and implemented directly (bmad create-story + dev-story cycle) |
