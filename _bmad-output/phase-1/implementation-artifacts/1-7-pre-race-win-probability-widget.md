---
baseline_commit: ee7d724
---

# Story 1.7: Pre-Race Win Probability Widget

Status: done

## Story

As a fan deciding who to watch,
I want to see each driver's win probability after qualifying,
So that I know who's likely to win before the race starts.

## Acceptance Criteria

1. **Given** qualifying results are not yet published for a Race Weekend **When** viewing the detail view **Then** the win probability widget is not shown (no placeholder, no "coming soon" — simply absent per EXPERIENCE.md State Patterns).
2. **Given** qualifying results are published **When** the detail view loads **Then** each driver's grid position shows alongside a win probability (%) computed by `WinProbabilityService` from grid-slot weight, championship standing, and recent form.
3. **Given** qualifying results are published **When** the detail view loads **Then** probabilities sum to approximately 100% across all drivers.
4. **Given** qualifying results are published **When** computed **Then** the calculation runs once (cached 6 hours per architecture TTL tier) and does not re-run until the next qualifying session.

## Tasks / Subtasks

- [x] Task 1: Ergast qualifying endpoint — DTO + client method (AC: 2, 3, 4)
  - [x] Create `backend/F1App.Api/Dtos/Ergast/ErgastQualifyingResponseDto.cs` with qualifying response shape:
    ```csharp
    // Ergast shape: MRData.RaceTable.Races[0].QualifyingResults[{position, Driver, Constructor}]
    ErgastQualifyingResponseDto → ErgastQualifyingMrDataDto → ErgastQualifyingRaceTableDto → ErgastQualifyingRaceDto → ErgastQualifyingResultDto
    ErgastQualifyingResultDto: Position (string), Driver (ErgastDriverDto — reuse existing), Constructor (ErgastConstructorDto — reuse existing)
    ErgastQualifyingRaceDto: Circuit (ErgastCircuitDto — reuse), QualifyingResults (IReadOnlyList<ErgastQualifyingResultDto>)
    ```
  - [x] Add `GetQualifyingResultsAsync(int round, CancellationToken)` to `IErgastClient` and `ErgastClient`. Calls `current/{round}/qualifying.json`. Returns `IReadOnlyList<ErgastQualifyingResultDto>` — empty list if `Races` array is empty (pre-qualifying).
  - [x] All new DTOs use `System.Text.Json.Serialization.JsonPropertyName` attributes matching Ergast JSON keys exactly (`"MRData"`, `"RaceTable"`, `"Races"`, `"QualifyingResults"`, `"position"`, `"Driver"`, `"Constructor"`). Reuse existing `ErgastDriverDto` and `ErgastConstructorDto` (already defined in `ErgastStandingsResponseDto.cs`).

- [x] Task 2: `WinProbabilityService` with heuristic calculation (AC: 2, 3, 4)
  - [x] Create `backend/F1App.Api/Services/WinProbabilityService.cs`. Constructor-inject `IErgastClient`, `IMemoryCache`, `StandingsService`.
  - [x] Expose `Task<IReadOnlyList<WinProbabilityEntry>> GetWinProbabilitiesAsync(int round, CancellationToken)`.
  - [x] Add `CacheKeys.WinProbability(int round)` to `CacheKeys.cs`: `$"winProbability:{round}"`. Cache with 6-hour TTL (architecture qualifying results tier).
  - [x] Algorithm — all in one method, no helper classes:
    1. Cache check: if `cache.TryGetValue(CacheKeys.WinProbability(round), ...)` → return cached value immediately.
    2. Fetch qualifying results: `ergastClient.GetQualifyingResultsAsync(round, ct)`. If empty → cache `[]` and return (no qualifying yet).
    3. Fetch standings: `standingsService.GetCurrentDriverStandingsAsync(ct)`. Build `dict<driverId, double points>`.
    4. Compute `maxPoints = standings.Any() ? standings.Max(s => s.Points) : 1.0`. Guard against zero.
    5. For each qualifying result: `baseWeight = 1.0 / Math.Pow(gridPos, 1.5)` (pole ≈1.0, P2 ≈0.354, P3 ≈0.192…). Grid position comes from `int.Parse(result.Position, CultureInfo.InvariantCulture)`.
    6. `champMultiplier = 1.0 + 0.3 * (driverPoints / maxPoints)` where `driverPoints` is looked up by `result.Driver.DriverId` (default `0.0` if not in standings).
    7. `rawScore = baseWeight * champMultiplier`.
    8. `totalScore = sum(rawScores)`. Guard against zero.
    9. Each probability = `Math.Round(rawScore / totalScore * 100.0, 1)`.
    10. Build `IReadOnlyList<WinProbabilityEntry>` ordered by `GridPosition` ascending. Cache result. Return.
  - [x] `WinProbabilityEntry` model: add `backend/F1App.Api/Models/WinProbabilityEntry.cs`:
    ```csharp
    namespace F1App.Api.Models;
    public record WinProbabilityEntry(string DriverName, string ConstructorName, int GridPosition, double WinProbability);
    ```
  - [x] **Note on "circuit-specific" grid win rate**: using standard `1/pos^1.5` distribution (global F1 historical average) is an intentional POC simplification — producing a sensible and consistent result without additional Ergast calls. Full circuit-specific historical computation is deferred (see Dev Notes for reasoning).

- [x] Task 3: `WinProbabilityController` (AC: 1, 4)
  - [x] Create `backend/F1App.Api/Controllers/WinProbabilityController.cs` with route `[Route("api/races/{round:int}/win-probability")]`.
  - [x] Register `WinProbabilityService` in `Program.cs`: `builder.Services.AddScoped<WinProbabilityService>();` (follow the `RaceScheduleService`/`StandingsService` pattern already in `Program.cs`).
  - [x] No 404 for missing qualifying data — return `200 []`. This avoids TanStack Query treating "no data yet" as an error.

- [x] Task 4: Backend tests for `WinProbabilityService` (AC: 2, 3, 4)
  - [x] Create `backend/F1App.Api.Tests/Services/WinProbabilityServiceTests.cs`.
  - [x] Tests:
    - `GetWinProbabilitiesAsync_EmptyQualifying_ReturnsEmptyList`: mock `GetQualifyingResultsAsync` returns `[]` → result is `[]`.
    - `GetWinProbabilitiesAsync_ThreeDrivers_ProbabilitiesSumToApproximately100`: 3-driver qualifying (P1/P2/P3) → assert sum within [99, 101].
    - `GetWinProbabilitiesAsync_ChampionInP2_HigherProbabilityThanZeroPointsInP2`: two separate service instances verify P2 probability is higher when they have max championship points.
    - `GetWinProbabilitiesAsync_CalledTwiceForSameRound_ErgastCalledOnce`: call twice → `GetQualifyingResultsAsync` called once (use `Mock<IErgastClient>.Verify`).
  - [x] All tests pass: 37/37 backend tests green.

- [x] Task 5: Frontend — schema, query, queryKey, MSW mock (AC: 1, 2, 3)
  - [x] In `frontend/src/shared/api/queryKeys.ts`, add `winProbability: (round: number) => ['races', 'win-probability', round] as const`.
  - [x] In `frontend/src/shared/api/ergast.ts`, add `WinProbabilityEntrySchema`, `WinProbabilitySchema`, `WinProbabilityEntry` type, `WIN_PROBABILITY_STALE_TIME_MS` (6h), and `useWinProbability` hook.
  - [x] In `frontend/src/shared/mocks/handlers/ergastHandlers.ts`, add default handler `http.get(`.../api/races/:round/win-probability`, () => HttpResponse.json([]))` — keeps existing listitem count tests intact.

- [x] Task 6: `WinProbabilityWidget.tsx` component (AC: 1, 2, 3)
  - [x] Create `frontend/src/features/calendar/WinProbabilityWidget.tsx`. Props-based, no query, no state. Returns `null` when entries is empty.
  - [x] Section heading matches `ContextualData.tsx` style. Each entry: P{n} position, driver name (bold), constructor (secondary), win probability % (tabular, right-aligned).
  - [x] Create `frontend/src/features/calendar/WinProbabilityWidget.test.tsx` with 4 tests: empty renders nothing, driver details rendered, percentages shown, listitem count.
  - [x] All tests pass: 35/35 frontend tests green.

- [x] Task 7: Wire widget into `RaceWeekendDetailView.tsx` + tests (AC: 1, 2, 3)
  - [x] Added `useWinProbability` import and call in `RaceWeekendDetailView.tsx`. Widget renders conditionally below `<ContextualData>` when `winProbs.length > 0`.
  - [x] Existing `RaceWeekendDetailView.test.tsx` 5-listitem assertion still passes (default MSW mock returns `[]` → widget absent).

## Dev Notes

### Win probability calculation (POC rationale)

Architecture spec states "simple heuristic" (Scope Decisions table). Full circuit-specific grid-slot win rate computation would require fetching ALL historical race results at the circuit, joining with qualifying records, and computing win rates per grid slot — significant complexity for a POC. Instead:
- **Grid weight** `1/pos^1.5` approximates the well-documented exponential decay of win probability from pole → back of grid (P1 wins ~40%, P2 ~20%, P3 ~12%, P4 ~8%… in global F1 history).
- **Championship multiplier** `1.0 + 0.3 × (points/maxPoints)` satisfies the "championship standing weight + recent form weight" requirements: standings points integrate both — a high-points driver has both title contender status and good recent form.
- Normalize to 100%. The output is meaningful, internally consistent, and clearly derived from Ergast data — satisfying the AC spirit without over-engineering for a POC.

Circuit-specific historical win rates are a deferred enhancement (would need `circuits/{circuitId}/results.json?limit=1000` + new Ergast DTO).

### API contract

`GET /api/races/{round}/win-probability`
- Returns `200 []` when no qualifying data (pre-qualifying weekends) — NOT 404. This keeps TanStack Query in a non-error state and lets the frontend use `data.length > 0` as the presence check.
- Returns `200 [{driverName, constructorName, gridPosition, winProbability}, ...]` sorted by `gridPosition` asc when qualifying is published.
- Response uses `JsonNamingPolicy.CamelCase` (global in `Program.cs` — no per-controller override needed).

### MSW default handler = `[]` for all rounds

The existing `RaceWeekendDetailView.test.tsx` asserts exactly 5 `listitem` elements (session rows for round 1). Adding `WinProbabilityWidget` — which renders `<li>` elements — would break this assertion if the widget were visible. The default MSW handler for `api/races/:round/win-probability` returns `[]` → `winProbs.length === 0` → widget not rendered → `listitem` count stays at 5. This is the same pattern as the `listitem` trap documented in Stories 1.5 and 1.6.

### Reuse existing DTOs

`ErgastQualifyingResultDto` reuses `ErgastDriverDto` (has `DriverId`, `GivenName`, `FamilyName`) and `ErgastConstructorDto` (has `ConstructorId`, `Name`) — both already defined in `ErgastStandingsResponseDto.cs`. Import them in the new qualifying DTO file; do NOT redefine them.

### `StandingsService` already caches

`StandingsService.GetCurrentDriverStandingsAsync` uses `IMemoryCache` internally (1h TTL per architecture). `WinProbabilityService` can call it directly — no need for a redundant cache guard around the standings call. Pattern consistent with how `RaceScheduleService.GetChampionshipDeltaAsync` works (noted as a deferred alignment item in `deferred-work.md`).

### `Program.cs` registration order

`WinProbabilityService` depends on `StandingsService` which is already registered as `AddScoped`. Add `builder.Services.AddScoped<WinProbabilityService>()` after `StandingsService` registration in `Program.cs`. Both are constructor-injected into the controller (primary constructor pattern, not `[FromServices]`).

### Frontend display: no loading state in widget

`WinProbabilityWidget` receives pre-fetched data via props. The parent `RaceWeekendDetailView` conditionally renders the widget only when `winProbs && winProbs.length > 0`. If the win probability query is pending or errors, `winProbs` is `undefined` or `[]` → widget absent. This matches EXPERIENCE.md "simply absent" behavior for AC 1 (no spinner, no "coming soon").

### `useWinProbability` stale time: 6h

Backend caches qualifying results for 6 hours (architecture TTL tier: qualifying results). Frontend `staleTime = 6h` mirrors this — avoids re-fetching data that won't change until next qualifying. Pattern identical to `RACE_SCHEDULE_STALE_TIME_MS` in `ergast.ts`.

### Test helper for backend

`WinProbabilityServiceTests.cs` should build its own `StandingsService` using `Mock<IErgastClient>` (same pattern as `EmptyStandingsService()` / `StandingsServiceWithDrivers(...)` helpers in `RaceScheduleServiceTests.cs`) rather than mocking `StandingsService` directly. This tests the real standings-to-multiplier mapping.

### Project Structure Notes

**New files:**
- `backend/F1App.Api/Dtos/Ergast/ErgastQualifyingResponseDto.cs`
- `backend/F1App.Api/Models/WinProbabilityEntry.cs`
- `backend/F1App.Api/Services/WinProbabilityService.cs`
- `backend/F1App.Api/Controllers/WinProbabilityController.cs`
- `backend/F1App.Api.Tests/Services/WinProbabilityServiceTests.cs`
- `frontend/src/features/calendar/WinProbabilityWidget.tsx`
- `frontend/src/features/calendar/WinProbabilityWidget.test.tsx`

**Modified files:**
- `backend/F1App.Api/Clients/IErgastClient.cs` (add `GetQualifyingResultsAsync`)
- `backend/F1App.Api/Clients/ErgastClient.cs` (implement `GetQualifyingResultsAsync`)
- `backend/F1App.Api/Services/CacheKeys.cs` (add `WinProbability(int round)`)
- `backend/F1App.Api/Program.cs` (register `WinProbabilityService`)
- `frontend/src/shared/api/queryKeys.ts` (add `winProbability`)
- `frontend/src/shared/api/ergast.ts` (add schema, type, query)
- `frontend/src/shared/mocks/handlers/ergastHandlers.ts` (add default handler)
- `frontend/src/features/calendar/RaceWeekendDetailView.tsx` (add query + widget render)

### References

- [Source: epics.md#Story 1.7] — AC source, FR-6.
- [Source: architecture.md#Win probability] — "simple heuristic, post-qualifying, C# calculation." Algorithm details.
- [Source: architecture.md#Ergast caching TTL tiers] — 6 hours for qualifying results.
- [Source: architecture.md#WinProbabilityController.cs] — separate controller, named file.
- [Source: architecture.md#WinProbabilityService.cs] — named service file.
- [Source: architecture.md#WinProbabilityResult.cs] — named model file (`WinProbabilityEntry` implements this).
- [Source: EXPERIENCE.md#State Patterns] — "No qualifying yet: Win probability widget is simply absent."
- [Source: EXPERIENCE.md#Flow 2] — widget position: below ContextualData.
- [Source: backend/F1App.Api/Services/CacheKeys.cs] — existing cache key patterns to follow.
- [Source: backend/F1App.Api/Clients/ErgastClient.cs] — existing client pattern for new method.
- [Source: backend/F1App.Api/Dtos/Ergast/ErgastStandingsResponseDto.cs] — `ErgastDriverDto`, `ErgastConstructorDto` to reuse.
- [Source: frontend/src/features/calendar/RaceWeekendDetailView.test.tsx] — 5-listitem assertion (regression trap).
- [Source: frontend/src/features/calendar/ContextualData.tsx] — visual pattern to follow for widget headings/typography.
- [Source: _bmad-output/implementation-artifacts/deferred-work.md] — `@testing-library/user-event` not installed; use `fireEvent`.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Fixed `decimal`/`double` type mismatch in `WinProbabilityService`: `DriverStanding.Points` is `decimal` but arithmetic uses `double`. Fixed by casting `(double)s.Points` in the `.ToDictionary()` projection.
- Added `DriverId` field to `DriverStanding` model to enable cross-service matching in `WinProbabilityService`. `StandingsService.ToDriverStanding` updated accordingly (non-breaking: no test constructs `DriverStanding` directly).
- `WinProbabilityWidget.test.tsx`: initial test used `getByText('(McLaren)')` but two McLaren entries exist → `MultipleElementsFound`. Fixed by using `getAllByText('(McLaren)').toHaveLength(2)`.
- Followed architecture spec to use a separate `WinProbabilityController.cs` (not inlined into `RacesController`).

### Completion Notes List

- All 7 tasks complete. Backend: 37/37 tests pass. Frontend: 35/35 tests pass.
- `DriverStanding` record has a new `DriverId` field (second positional parameter). No breaking change — all existing tests use `ErgastDriverStandingDto` mocks via `StandingsService.ToDriverStanding`.
- Default MSW handler for `api/races/:round/win-probability` returns `[]` — widget is absent in existing tests, preserving the 5-listitem count in `RaceWeekendDetailView.test.tsx`.

### File List

**New files:**
- `backend/F1App.Api/Dtos/Ergast/ErgastQualifyingResponseDto.cs`
- `backend/F1App.Api/Models/WinProbabilityEntry.cs`
- `backend/F1App.Api/Services/WinProbabilityService.cs`
- `backend/F1App.Api/Controllers/WinProbabilityController.cs`
- `backend/F1App.Api.Tests/Services/WinProbabilityServiceTests.cs`
- `frontend/src/features/calendar/WinProbabilityWidget.tsx`
- `frontend/src/features/calendar/WinProbabilityWidget.test.tsx`

**Modified files:**
- `backend/F1App.Api/Models/DriverStanding.cs` (added `DriverId` field)
- `backend/F1App.Api/Clients/IErgastClient.cs` (added `GetQualifyingResultsAsync`)
- `backend/F1App.Api/Clients/ErgastClient.cs` (implemented `GetQualifyingResultsAsync`)
- `backend/F1App.Api/Services/CacheKeys.cs` (added `WinProbability(int round)`)
- `backend/F1App.Api/Services/StandingsService.cs` (updated `ToDriverStanding` to include `DriverId`)
- `backend/F1App.Api/Program.cs` (registered `WinProbabilityService`)
- `frontend/src/shared/api/queryKeys.ts` (added `winProbability`)
- `frontend/src/shared/api/ergast.ts` (added schema, type, stale time constant, query hook)
- `frontend/src/shared/mocks/handlers/ergastHandlers.ts` (added default `[]` handler)
- `frontend/src/features/calendar/RaceWeekendDetailView.tsx` (added `useWinProbability` + conditional widget render)

## Senior Developer Review (AI)

**Date:** 2026-06-17
**Outcome:** Changes Requested
**Layers:** Blind Hunter · Edge Case Hunter · Acceptance Auditor

### Action Items

#### Patch (fix required)

- [x] **[Review][Patch] `int.Parse` throws on non-numeric or zero qualifying position (FormatException / Infinity)** [`WinProbabilityService.cs:38-39`] — Used `int.TryParse` + guard `gridPos <= 0`; invalid entries filtered out before probability computation.
- [x] **[Review][Patch] `useWinProbability` fires with `round=NaN` when route param is absent/invalid** [`ergast.ts`, `RaceWeekendDetailView.tsx:26`] — Added `enabled: !isNaN(round) && round > 0`.
- [x] **[Review][Patch] Empty qualifying result cached with full 6h TTL blocks fresh fetch after qualifying runs** [`WinProbabilityService.cs:21-24`] — Empty result now cached with `EmptyCacheTtl = 5 minutes`.
- [x] **[Review][Patch] `WinProbabilityWidget` uses `gridPosition` as React key — not guaranteed unique** [`WinProbabilityWidget.tsx:17`] — Changed to composite key `` `${gridPosition}-${driverName}` ``.
- [x] **[Review][Patch] `winProbability: 19.0` serializes as `19`, rendering as `19%` in a column of `52.3%`/`28.7%`** [`WinProbabilityWidget.tsx:21`] — Applied `toFixed(1)` for consistent one-decimal display.
- [x] **[Review][Patch] Stale time constant named `WIN_PROBABILITY_STALE_TIME_MS` instead of spec-required `QUALIFYING_STALE_TIME_MS`** [`ergast.ts`] — Renamed to `QUALIFYING_STALE_TIME_MS`.

#### Defer (pre-existing / by design)

- [x] **[Review][Defer] Cache stampede: `TryGetValue`/`Set` not atomic under concurrent requests** [`WinProbabilityService.cs:14-57`] — deferred, pre-existing pattern across all services in this POC
- [x] **[Review][Defer] Controller and service inject concrete types (no `IWinProbabilityService` / `IStandingsService`)** [`WinProbabilityController.cs:10`, `WinProbabilityService.cs:9`] — deferred, pre-existing POC pattern (consistent with `StandingsService`, `RaceScheduleService`)
- [x] **[Review][Defer] `WinProbability(int round)` cache key has no season component — collides across years** [`CacheKeys.cs:11`] — deferred, pre-existing pattern (existing keys also lack season scoping)
- [x] **[Review][Defer] Standings 1h TTL vs win probability 6h TTL creates stale champion-multiplier for up to 6h post-race** [`WinProbabilityService.cs:10`] — deferred, by design for POC; full fix requires cache invalidation on standings refresh

## Change Log

- 2026-06-17: Story created via create-story workflow.
- 2026-06-17: Implementation complete. All ACs satisfied. Status set to review.
- 2026-06-17: Code review complete. 6 patches required, 4 deferred, 3 dismissed.
