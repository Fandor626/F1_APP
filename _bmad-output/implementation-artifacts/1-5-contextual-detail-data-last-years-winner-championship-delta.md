---
baseline_commit: 9791a256f1b8f90fa696088d3ee944e78d512025
---

# Story 1.5: Contextual Detail Data — Last Year's Winner & Championship Delta

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a history-curious fan,
I want the detail view to show last year's winner at this circuit and the current championship gap,
so that I have context heading into the weekend.

## Acceptance Criteria

1. **Given** a circuit with a prior-year result **When** the detail view loads **Then** last year's winner (driver, team, time/gap) is displayed.
2. **Given** a circuit with no prior F1 race (no result at this circuit in `season - 1`) **When** the detail view loads **Then** the field is omitted or labelled "First race at this circuit."
3. **Given** current standings **When** the detail view loads **Then** the Championship Delta between the top two Drivers is displayed.

## Tasks / Subtasks

- [x] Task 1: Add an Ergast/Jolpica client method for per-circuit prior-year results (AC: 1, 2)
  - [x] Add `Dtos/Ergast/ErgastRaceResultResponseDto.cs` with the response shape for `GET /{season}/circuits/{circuitId}/results/1.json`: `ErgastRaceResultResponseDto → MRData → RaceTable → Races: IReadOnlyList<ErgastRaceResultRaceDto>` where each race has `Results: IReadOnlyList<ErgastResultDto>`. Reuse the **existing** `ErgastDriverDto` and `ErgastConstructorDto` from `Dtos/Ergast/ErgastStandingsResponseDto.cs` for `ErgastResultDto.Driver`/`.Constructor` — do not redeclare them. Add `ErgastResultTimeDto(string Time)` for the nested `Time` object.
  - [x] Add `GetCircuitResultsAsync(int season, string circuitId, CancellationToken)` to `IErgastClient` and `ErgastClient`, calling `"{season}/circuits/{circuitId}/results/1.json"` (relative — base address already ends in `/`) and flattening to `IReadOnlyList<ErgastResultDto>` exactly like the two standings methods already do (`Races.Count == 0 ? [] : Races[0].Results`) — empty list means "no prior race," never an exception.
  - [x] Verified live against the real upstream (Jolpica, currently configured in `appsettings.json`): `https://api.jolpi.ca/ergast/f1/2025/circuits/bahrain/results/1.json` returns one race with `Results[0].Driver.{givenName,familyName}`, `.Constructor.name`, `.Time.time` (e.g. `"1:35:39.435"`). A circuit with no result for that season returns `"Races":[]` with HTTP 200 (confirmed against `las_vegas`/2025) — no 404, so the "no prior race" path is a plain empty-list check, not error handling.
- [x] Task 2: Compute last year's winner and the championship delta in `RaceScheduleService` (AC: 1, 2, 3)
  - [x] Add `Models/CircuitPriorWinner.cs`: `public record CircuitPriorWinner(string DriverName, string ConstructorName, string? Time);` — `DriverName` is `"{givenName} {familyName}"` (full name; note this differs from `DriverStanding.DriverName`, which is family-name-only — intentional, both are correct for their own context, do not unify them).
  - [x] Add `Models/ChampionshipDelta.cs`: `public record ChampionshipDelta(string LeaderName, string RunnerUpName, decimal PointsGap);`
  - [x] Extend `Models/RaceWeekendDetail.cs` with two new trailing properties: `CircuitPriorWinner? PriorYearWinner` and `ChampionshipDelta? ChampionshipDelta` (type name and property name matching is valid C#, mirrors the glossary term — keep it).
  - [x] `RaceScheduleService` constructor gains a third dependency: `StandingsService standingsService` (already registered in `Program.cs` as `AddScoped<StandingsService>()` — no DI wiring changes needed).
  - [x] In `GetRaceDetailAsync`, after resolving the race for the requested round, look up the prior-year winner via `ergastClient.GetCircuitResultsAsync(race.Season - 1, race.Circuit.CircuitId, ct)`. Map the first (only) result to `CircuitPriorWinner`, or `null` when the list is empty. Cache the mapped result (including the `null` case — see Dev Notes on caching nulls) under a new per-`(season, circuitId)` key for **7 days**, matching architecture's "historical race results" TTL tier — this is a different cache than the 24h schedule cache, add a `CacheKeys.CircuitPriorResults(int season, string circuitId)` method (not a `const`, since it's parameterised) alongside the existing consts.
  - [x] Compute the championship delta from `standingsService.GetCurrentDriverStandingsAsync(ct)`: order by `Position`, take the top two, `PointsGap = standings[0].Points - standings[1].Points`. If fewer than two driver standings exist (e.g. pre-season), return `null` for `ChampionshipDelta` rather than throwing — not explicitly covered by an AC, but required so the endpoint doesn't 500 in that edge case.
  - [x] Pass both into `ToDetail(...)`.
- [x] Task 3: Update existing backend tests broken by the constructor/behavior change (AC: 1, 2, 3 — regression prevention)
  - [x] `RaceScheduleServiceTests.cs`: **every** `new RaceScheduleService(ergastClient.Object, new MemoryCache(...))` call site (8 of them) now needs a third `StandingsService` argument — construct one from the same or a separate `Mock<IErgastClient>` (with `GetCurrentDriverStandingsAsync` stubbed, even if just to `[]`) plus a fresh `MemoryCache`. Tests that don't care about the contextual fields can stub minimal/empty data; this is purely a compile-fix, not new behavior coverage.
  - [x] Add new test cases: prior-year winner present (maps name/constructor/time correctly), prior-year winner absent (empty `Results` → `PriorYearWinner` is `null`), championship delta computed correctly from top two standings (sorted by position, not array order), championship delta `null` when standings has 0 or 1 entries, and a cache test mirroring the existing "`...CachesResultAndDoesNotCallErgastTwice`" pattern for the new circuit-results call.
  - [x] `RacesControllerTests.cs`: `GetDetail_ReturnsSessionsForMatchingRound` currently mocks `IErgastClient` with only `GetCurrentSeasonScheduleAsync` configured. Since `StandingsService` is resolved via real DI in this `WebApplicationFactory`-based test and shares the same overridden `IErgastClient` mock, the new code path will call the **same mock's** unconfigured `GetCurrentDriverStandingsAsync` and `GetCircuitResultsAsync` — add explicit `.Setup(...)` returns for both (e.g. empty standings/results, or two sample standings + a sample result) so the test doesn't NRE or assert on accidental Moq defaults. `GetDetail_ReturnsNotFoundForUnknownRound` is unaffected — the round lookup fails before either new call happens.
  - [x] Add a `GetDetail_*` controller test asserting `priorYearWinner` and `championshipDelta` round-trip as camelCase JSON, plus one asserting `priorYearWinner` is absent from the JSON body when null (per the global `JsonIgnoreCondition.WhenWritingNull` policy — don't assert a literal `null` key).
  - [x] `ErgastClientContractTests.cs`: add a WireMock contract test for `GetCircuitResultsAsync` hitting `/{season}/circuits/{circuitId}/results/1.json`, mirroring the existing driver/constructor standings contract tests (`WithBodyAsJson` matching the real Jolpica shape verified in Task 1).
- [x] Task 4: Frontend — extend the Ergast API layer (AC: 1, 2, 3)
  - [x] In `shared/api/ergast.ts`, add `PriorYearWinnerSchema` (`driverName`, `constructorName`, `time` optional string) and `ChampionshipDeltaSchema` (`leaderName`, `runnerUpName`, `pointsGap: z.number()`); extend `RaceWeekendDetailSchema` with `priorYearWinner: PriorYearWinnerSchema.optional()` and `championshipDelta: ChampionshipDeltaSchema.optional()`. Export the two new inferred types (`PriorYearWinner`, `ChampionshipDelta`) alongside the existing exports — `useRaceDetail` needs no changes, the schema extension flows through automatically.
- [x] Task 5: Frontend — build `ContextualData.tsx` and wire it into the detail view (AC: 1, 2, 3)
  - [x] Add `frontend/src/features/calendar/ContextualData.tsx` (named in architecture's project tree under `FR-4`). Pure presentational component taking `priorYearWinner?: PriorYearWinner` and `championshipDelta?: ChampionshipDelta` as props — no data fetching of its own, the parent already has the data from `useRaceDetail`.
  - [x] Last-year-winner block: when `priorYearWinner` is present, show driver name, constructor name, and `time` (omit the time fragment if `time` is undefined — old/incomplete Ergast records). When absent, render the literal copy **"First race at this circuit."** (exact wording from `EXPERIENCE.md`'s Voice and Tone and State Patterns tables — match casing/punctuation exactly, it's a tested microcopy string).
  - [x] Championship-delta block: render leader/runner-up names and the points gap (no UX-mandated exact phrasing exists for this static, non-live field — keep it plain/fan-to-fan per Voice and Tone, no exclamation marks, no "LIVE" framing; this is current official standings, not the live "if race ended now" projection from FR-15/Epic 2, so no provisional-data caveat is needed here).
  - [x] Do **not** wrap driver/constructor names in a link to a profile page — `DriverProfilePage`/`CircuitProfilePage` (FR-20/21) don't exist yet (Epic 5). `RaceWeekendCard.tsx` already renders driver/constructor names as plain text for the same reason; stay consistent.
  - [x] Style with the existing typography tokens already used in `RaceWeekendDetailView.tsx` (the `text-[11.5px] ... uppercase` label pattern used for the "Sessions" heading, `text-[13px]` body text) — no new visual tokens needed, this section is calm-editorial like the rest of the page, not the Gap List's dense exception.
  - [x] **Do not render this section as a `<ul>`/`<li>` list** — `RaceWeekendDetailView.test.tsx` asserts `screen.getAllByRole('listitem')` has length 5 (one per session) for both standard and sprint weekends; any new `listitem`-role elements on the page will break those two existing tests. Use `<div>`/`<dl>` or similar.
  - [x] Render `<ContextualData priorYearWinner={data.priorYearWinner} championshipDelta={data.championshipDelta} />` in `RaceWeekendDetailView.tsx`, directly below the closing `</ul>` of the Sessions list (matches `EXPERIENCE.md` Flow 2's order: sessions → timezone toggle [Story 1.6, not yet built] → last year's winner + championship gap → win probability widget [Story 1.7, not yet built]).
- [x] Task 6: Frontend tests and fixtures (AC: 1, 2, 3)
  - [x] Add `frontend/src/features/calendar/ContextualData.test.tsx` co-located per the project's test convention — render with props directly (no MSW needed, it's not a data-fetching component): asserts the winner/team/time render when present, asserts the literal "First race at this circuit." copy when `priorYearWinner` is `undefined`, asserts the championship delta renders leader/runner-up/gap.
  - [x] Update `shared/mocks/handlers/ergastHandlers.ts`'s `sampleRaceDetailsByRound`: give round 1 (`Bahrain Grand Prix`) a `priorYearWinner` and a `championshipDelta`, and leave round 2 (`Saudi Arabian Grand Prix`) without `priorYearWinner` (to exercise the "first race" path through the existing MSW-backed `RaceWeekendDetailView.test.tsx` suite without adding new test cases there, though new dedicated assertions in `ContextualData.test.tsx` are still required per the previous subtask).

## Dev Notes

- **Endpoint, verified live (2026-06-16):** `GET {ErgastBaseUrl}/{season}/circuits/{circuitId}/results/1.json` — `results/1.json` filters to finishing position 1 (the race winner), scoped to one circuit/season. Confirmed against the actually-configured upstream (`https://api.jolpi.ca/ergast/f1`, set in both `appsettings.json` and `appsettings.Development.json` — this project migrated off `ergast.com` in Story 1.2 per NFR-8's zero-refactor config swap). A circuit/season with no race returns HTTP 200 with `"Races":[]`, not a 404 — there is no error path to handle here, only an empty-list check.
- **Caching:** this is a *new* cache tier, not a reuse of the existing 24h schedule cache or 1h standings cache. Architecture's TTL table specifies **7 days** for "historical race results" (`architecture.md#Data Architecture`) — prior-year results are immutable, unlike the live current-season schedule. Use a dedicated cache key per `(season, circuitId)` since round numbers can differ between the current and prior season if the calendar reshuffles.
- **Caching a `null` result:** when a circuit has no prior-year race, you'll want to cache that `null`/empty outcome too (so repeat detail-view loads for a new circuit don't re-hit Ergast every time within the 7-day window) — `IMemoryCache.Set` accepts `null` values fine on .NET 10. If you do this, don't reuse the existing file's `cache.TryGetValue(key, out X? cached) && cached is not null` idiom unchanged — that pattern treats a legitimately-cached `null` the same as a cache miss. Either branch on it explicitly, or accept the minor inefficiency of re-fetching "no result" every call (traffic is trivial for a hobby POC) — either is acceptable, pick one and be consistent.
- **`StandingsService` becomes a dependency of `RaceScheduleService`.** Both are already `Scoped` in `Program.cs` (`builder.Services.AddScoped<RaceScheduleService>()` / `AddScoped<StandingsService>()`) — DI resolves the new constructor parameter automatically, no `Program.cs` changes needed. The only fallout is every **direct** `new RaceScheduleService(...)` call in tests (see Task 3).
- **Two different "driver name" shapes already exist in this codebase and that's correct, not a bug:** `DriverStanding.DriverName` (used on the calendar card) is family-name-only, set that way in Story 1.3. `CircuitPriorWinner.DriverName` for this story should be the full `"{givenName} {familyName}"` — a one-off historical fact reads better with a full name than a standings table row does. Don't "fix" one to match the other.
- **Architecture's controller/service split still applies:** `RacesController` stays a thin pass-through (no signature change needed at all — it already just calls `raceScheduleService.GetRaceDetailAsync(round, ct)` and returns the result). All new logic belongs in `RaceScheduleService`, not the controller.
- **UX source is the spine, not a mock.** `EXPERIENCE.md` explicitly states Race Weekend Detail is "spine-only by design (no mock)" — there's no `calendar.html`-style HTML reference for this specific section. Follow the Component/State Patterns tables and Voice and Tone table (cited below) as the authoritative contract instead of inventing a layout from scratch or trying to match a mockup that doesn't cover this page.
- **Exact required microcopy:** `"First race at this circuit."` — this string appears verbatim in both `EXPERIENCE.md`'s Voice and Tone table (as a positive "Do" example) and its State Patterns table ("No prior result at this circuit" row). Match it exactly, including the trailing period.
- **No exact microcopy is mandated for the championship-delta phrasing** — unlike the winner-absent case, there's no scripted string in the UX docs for this. Use plain, fan-to-fan language consistent with the rest of the Voice and Tone table (no hype, no exclamation marks).

### Project Structure Notes

- All new backend files land in existing folders per architecture's tree: `Dtos/Ergast/`, `Models/`, no new top-level folders.
- `ContextualData.tsx` lands in `frontend/src/features/calendar/`, exactly where architecture's Complete Project Tree already names it (`architecture.md#Project Structure & Boundaries`, frontend tree, calendar feature block) — no path decision to make, it's pre-named.
- No discrepancies found between architecture's structure and the current actual repo state for this story's scope.

### References

- [Source: epics.md#Story 1.5: Contextual Detail Data — Last Year's Winner & Championship Delta] — AC source.
- [Source: prd.md#FR-4: Contextual detail data] — feature description, consequences (omit-or-label rule).
- [Source: architecture.md#Data Architecture] — 7-day TTL for historical race results; proactive cache-warm note (warm-up is for schedule/standings/circuit metadata only — prior-year results are queried lazily per circuit, not warmed).
- [Source: architecture.md#Project Structure & Boundaries] — `ContextualData.tsx` and `RacesController` named against FR-4; backend `Dtos/Ergast/` and `Models/` folder placement.
- [Source: architecture.md#Implementation Patterns & Consistency Rules] — `*Dto` suffix, controllers-are-thin rule, `DateTimeOffset`-only rule (not directly exercised by this story — no new date math beyond what `RaceScheduleService` already does), JSON camelCase + omit-null-fields policy (`JsonIgnoreCondition.WhenWritingNull`, already global in `Program.cs`).
- [Source: ux-designs/ux-F1_poc-2026-06-16/EXPERIENCE.md#Information Architecture] — Race Weekend Detail is spine-only, no mock.
- [Source: ux-designs/ux-F1_poc-2026-06-16/EXPERIENCE.md#Voice and Tone] — exact "First race at this circuit." copy; plain/no-hype tone rule.
- [Source: ux-designs/ux-F1_poc-2026-06-16/EXPERIENCE.md#State Patterns] — "No prior result at this circuit" row, confirms omit/replace behavior (not an error state).
- [Source: ux-designs/ux-F1_poc-2026-06-16/EXPERIENCE.md#Key Flows, Flow 2] — page order (sessions → timezone toggle → last-year winner + championship gap → win probability), confirms this section sits below Sessions.
- [Source: backend/F1App.Api/Services/RaceScheduleService.cs, StandingsService.cs, Clients/ErgastClient.cs, Controllers/RacesController.cs] — existing patterns this story extends (read in full before editing).
- [Source: backend/F1App.Api.Tests/Services/RaceScheduleServiceTests.cs, Controllers/RacesControllerTests.cs, Clients/ErgastClientContractTests.cs] — existing tests whose call sites/mocks this story's constructor change will break; read in full before editing.
- [Source: frontend/src/shared/api/ergast.ts, features/calendar/RaceWeekendDetailView.tsx, RaceWeekendDetailView.test.tsx, RaceWeekendCard.tsx] — existing frontend patterns this story extends; the `listitem`-role count assertion in the detail-view test is a regression trap, called out explicitly above.
- [Web, verified live 2026-06-16: `https://api.jolpi.ca/ergast/f1/{season}/circuits/{circuitId}/results/1.json` — confirmed real response shape (`Driver.givenName/familyName`, `Constructor.name`, `Time.time`) and confirmed empty-`Races`-array (HTTP 200, not 404) behavior for a circuit/season with no result, against the project's actually-configured Jolpica base URL.]

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6

### Debug Log References

None — no environment or tooling issues encountered.

### Completion Notes List

- Added `IErgastClient.GetCircuitResultsAsync`, verified live against the actually-configured Jolpica upstream before writing the contract test (real shape, real empty-`Races`-array behavior).
- `RaceScheduleService` now depends on `StandingsService` and composes `CircuitPriorWinner` (7-day cache, keyed by `(priorSeason, circuitId)`, cached value includes the `null` "no prior race" outcome) and `ChampionshipDelta` (computed from the top two driver standings sorted by position) into `RaceWeekendDetail`. `RacesController` required no changes — it was already a thin pass-through.
- Updated all 8 direct-construction call sites in `RaceScheduleServiceTests.cs` and added explicit `IErgastClient` stubs in `RacesControllerTests.cs` for the two new dependency calls, per the regression risk flagged in Dev Notes — confirmed via full test run, no breakage.
- Added 5 new `RaceScheduleServiceTests` cases (winner present/absent, cache-hit, delta computed/omitted), 1 new `RacesControllerTests` case (camelCase round-trip + null-field omission), 2 new `ErgastClientContractTests` cases (winner parsed, empty-race-list).
- Frontend: extended `ergast.ts`'s zod schema with optional `priorYearWinner`/`championshipDelta`; built `ContextualData.tsx` as a pure presentational component (no `<ul>/<li>` — confirmed it doesn't break the existing `RaceWeekendDetailView.test.tsx` `listitem`-count assertions); wired into `RaceWeekendDetailView.tsx` below the Sessions list. Driver/constructor names render as plain text (no links), consistent with `RaceWeekendCard.tsx`, since profile pages don't exist yet.
- Verification: backend `dotnet build` (solution-wide, 0 warnings/0 errors) and `dotnet test` (32/32 passing, +8 from this story); frontend `vitest run` (22/22 passing, +4), `eslint .` (clean), `tsc -b` (clean).

### File List

**Added:**
- `backend/F1App.Api/Dtos/Ergast/ErgastRaceResultResponseDto.cs`
- `backend/F1App.Api/Models/CircuitPriorWinner.cs`
- `backend/F1App.Api/Models/ChampionshipDelta.cs`
- `frontend/src/features/calendar/ContextualData.tsx`
- `frontend/src/features/calendar/ContextualData.test.tsx`

**Modified:**
- `backend/F1App.Api/Clients/IErgastClient.cs`
- `backend/F1App.Api/Clients/ErgastClient.cs`
- `backend/F1App.Api/Services/CacheKeys.cs`
- `backend/F1App.Api/Services/RaceScheduleService.cs`
- `backend/F1App.Api/Models/RaceWeekendDetail.cs`
- `backend/F1App.Api.Tests/Clients/ErgastClientContractTests.cs`
- `backend/F1App.Api.Tests/Services/RaceScheduleServiceTests.cs`
- `backend/F1App.Api.Tests/Controllers/RacesControllerTests.cs`
- `frontend/src/shared/api/ergast.ts`
- `frontend/src/features/calendar/RaceWeekendDetailView.tsx`
- `frontend/src/shared/mocks/handlers/ergastHandlers.ts`
- `_bmad-output/implementation-artifacts/1-5-contextual-detail-data-last-years-winner-championship-delta.md` (this file)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (status tracking)

### Review Findings

- [x] [Review][Decision] ChampionshipDelta driver names are family-name-only → **fixed**: added `FullName` property to `DriverStanding`, `GetChampionshipDeltaAsync` now uses `FullName`; all affected tests and MSW mocks updated to full names ("Lando Norris", "Max Verstappen").
- [x] [Review][Decision] "Last Year's Winner" `<h2>` heading renders unconditionally → **kept as-is**: heading always visible with fallback copy satisfies AC 2's "label" option.
- [x] [Review][Patch] `null` prior-winner cache reliability → **dismissed**: the existing cache test `GetRaceDetailAsync_CachesCircuitResultsAndDoesNotCallErgastTwice` exercises this exact path and passes (32/32); `IMemoryCache.TryGetValue<T>` returns `true` for stored `null` in .NET 10 via the `result == null` branch. False positive.
- [x] [Review][Defer] `PointsGap` can be zero when top-two standings are tied on points [RaceScheduleService.cs, GetChampionshipDeltaAsync] — deferred, edge-case Ergast data quality issue, not a correctness bug for a hobby POC
- [x] [Review][Defer] `ErgastResultDto` lacks a position field — winner assumed as `results[0]` without position verification [ErgastRaceResultResponseDto.cs] — deferred, URL `/results/1.json` filter verified live against Jolpica
- [x] [Review][Defer] `GetChampionshipDeltaAsync` has no inline cache guard [RaceScheduleService.cs] — deferred, `StandingsService.GetCurrentDriverStandingsAsync` already caches internally with 1h TTL
- [x] [Review][Defer] `int.Parse(race.Season)` throws on non-numeric string [RaceScheduleService.cs] — deferred, pre-existing pattern throughout the codebase
- [x] [Review][Defer] Cache stampede on concurrent cold-cache detail requests [RaceScheduleService.cs, GetPriorYearWinnerAsync] — deferred, single-instance hobby POC
- [x] [Review][Defer] Missing test case: `priorYearWinner` absent with `championshipDelta` present combination [ContextualData.test.tsx] — deferred, minor gap; individual branches are covered

## Change Log

- 2026-06-16: Implemented all 6 tasks (Ergast client method, service-layer winner/delta computation, regression fixes for the constructor-dependency change, frontend schema + `ContextualData.tsx`, full test coverage). All ACs satisfied; full backend + frontend regression suites pass. Status → review.
- 2026-06-17: Code review — 2 decision-needed, 1 patch, 6 deferred, 1 dismissed.
