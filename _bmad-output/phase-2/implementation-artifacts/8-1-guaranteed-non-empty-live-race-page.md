---
baseline_commit: 5c51d51
---

# Story 8.1: Guaranteed non-empty Live Race page

Status: review

## Story

As a fan checking the Live Race page when nothing is currently live,
I want to see the most recently completed race's full data instead of an empty page,
so that there's always something real to look at.

## Acceptance Criteria

1. **Given** no Session is currently live, **when** I open the Live Race page, **then** the page renders the most recently completed Race's full data set (positions, gaps, tyres, sectors, timeline) using the same components as a live session — **and** it's clearly labelled as a past race (e.g. "Past race — {name}, {date}").
2. **Given** a genuine API failure (not just "nothing live"), **when** the page loads, **then** it shows the existing, separately-handled degraded/error state — not confused with fallback mode.
3. **Given** zero races have been completed this season yet, **when** I open the Live Race page, **then** no Replay bar is shown (N/A until Story 8.2 builds it), and a plain on-brand message names the next race instead of a blank page.
4. **Given** this behavior was already specified in MVP FR-16 but not fully realized in shipped behavior, **when** this story is complete, **then** the gap between original intent and shipped reality is closed — a regression fix, not new scope. (FR-5 · PRD §4.2 · UX-DR14)

## Tasks / Subtasks

- [x] Task 0 (discovered during implementation, not in the original spec — see Dev Notes): Historical OpenF1 integration for fallback enrichment
  - [x] New `OpenF1SessionDto` + 6 new historical (explicit `session_key`) methods on `IOpenF1Client`/`OpenF1Client` — session lookup by date, laps/stints/race_control/pit/drivers for a specific past session
  - [x] `RaceDataOrchestrator.EnrichFallbackFromOpenF1Async`: resolves the OpenF1 session for the last-completed race's date, populates `_fallbackLapChart`, `_fallbackFastestSectors`, `_fallbackTimeline`, and merges tyre compound/stint laps/team colour into `_fallbackDrivers`
  - [x] `BuildSnapshot()`/`BuildFastestSectorBoard()` now use this enriched data in fallback mode instead of unconditionally empty/null
  - [x] `ParseRaceControlEvent` refactored to accept an optional driver-info override (historical session roster vs. live `_driverInfo`) without touching its existing live call site
- [x] Task 1: Frontend — zero-completed-races message (AC 3)
  - [x] `useLastRaceResult` now resolves `204 No Content` to `null` (a real "confirmed no prior race" outcome) instead of throwing on an empty body
  - [x] `LiveRacePage.tsx` detects this state (`isFallback && !hasLiveData && lastRaceFetched && lastRaceData === null`) and renders a plain message naming the next scheduled race (reusing `useRaceSchedule()` + the existing `formatDateRange` util), instead of an empty-looking page
- [x] Task 2: Tests
  - [x] 5 new `RaceDataOrchestratorTests` covering enrichment success, tyre/stint computation, no-matching-session graceful degradation, OpenF1-failure graceful degradation, and `BuildSnapshot` consuming the enriched fields
  - [x] 2 new `LiveRacePage.test.tsx` tests for the zero-races message and its absence once real data arrives

## Dev Notes

- **This story's scope changed significantly from its original spec during implementation — read this before touching anything.** The original story spec (and the epics.md AC it was drawn from) asked for the fallback page to show "positions, gaps, tyres, sectors, timeline" using "the same components as a live session." Investigation found: positions/gaps already worked (existing Ergast-backed `_fallbackDrivers`), but tyres/sectors/timeline/lap-chart were **intentionally** left empty in fallback mode by an explicit MVP-era design decision (`BuildFastestSectorBoard`'s comment: "Ergast has no sector-time archive, so the board is only meaningful during a live/stale session"). Ergast genuinely has no lap-by-lap sector times, tyre stints, or race-control archive for historical races — only OpenF1 does, and the entire `OpenF1Client` was hardcoded to `session_key=latest` (today's live session only), with zero historical-session query capability.
- **User decision (asked explicitly, not assumed):** given the PRD's own text calls FR-5 "a regression fix, not new scope" while also asking for genuinely new capability (historical OpenF1 querying), presented two options — build the real historical integration now, or ship the lighter "never blank, but sectors/timeline stay empty as already designed" interpretation and defer to Story 8.2 (which needs real historical data anyway for replay). **User chose to build the real integration now.** Implemented as Task 0 above — this is the foundation Story 8.2's replay endpoint (Architecture AD-2) will also need; 8.2 should reuse these new `IOpenF1Client` historical methods rather than re-deriving them.
- **OpenF1 session resolution is date-based, not circuit-based.** OpenF1 has no direct "look up session by circuit + round" endpoint; `GetRaceSessionsAsync(year)` returns every Race session for that year, and the orchestrator matches by exact date (`DateOnly.FromDateTime(session.DateStart) == raceDate`, where `raceDate` comes from Ergast's `GetLastRaceResultsAsync().Date`). Verified live against the real OpenF1 API (`https://api.openf1.org/v1/sessions?session_type=Race&year=2025`) before writing this — not assumed from documentation.
- **Fallback enrichment is a static final-state aggregate, not a temporal sequence.** `_fallbackFastestSectors` is the session-best S1/S2/S3 across every lap (not "best so far at a point in time"); `_fallbackTimeline`'s single `FastestLap` entry marks the one overall-fastest lap of the race (not live's "new best replaces old" running sequence); tyre compound/stint laps come from each driver's *final* stint. This is a deliberate simplification appropriate for a non-steppable fallback view — Story 8.2's replay will need the full lap-by-lap temporal sequence and should not reuse this aggregation logic as-is, only the underlying `IOpenF1Client` historical fetch methods.
- **Any failure in `EnrichFallbackFromOpenF1Async` degrades gracefully** (no matching session found, OpenF1 unreachable, etc.) — `_fallbackLapChart`/`_fallbackFastestSectors`/`_fallbackTimeline` simply stay at their empty/null defaults, identical to today's pre-enrichment behavior. This preserves AC 2 (a genuine OpenF1 outage doesn't corrupt or crash the fallback path, it just falls back further to "positions/gaps only," which was already the entire feature before this story).
- **`useLastRaceResult`'s 204 handling was a real, separate bug** blocking AC 3: the query threw on `res.json()` against an empty 204 body (rather than a valid JSON body), meaning the "zero completed races" scenario surfaced as a React Query error state, not the graceful "nothing yet" state AC 3 needs. Fixed as part of Task 1.
- **`LiveRacePage.tsx` now has two fallback data sources** (existing REST `useLastRaceResult`, and the SignalR `RaceSnapshot` push which now also carries the enriched fallback data via `RaceDataOrchestrator`). Left both in place rather than removing the REST path: REST gives near-instant weak data (positions only) while the orchestrator's next broadcast cycle is still assembling the fully-enriched snapshot; SignalR's `handleSnapshot` unconditionally overwrites the store on every message, so the REST-sourced data is transparently upgraded once the richer SignalR snapshot arrives. This is a reasonable "instant-then-upgraded" pattern, not a conflict — verified by reading `useSignalRConnection.ts`'s `handleSnapshot`, which calls `setDrivers`/`setLapChart`/`setFastestSectors`/`setTimeline` unconditionally on every incoming message.

### Project Structure Notes

- New: `backend/F1App.Api/Dtos/OpenF1/OpenF1SessionDto.cs`.
- Modified: `backend/F1App.Api/Clients/IOpenF1Client.cs`, `backend/F1App.Api/Clients/OpenF1Client.cs`, `backend/F1App.Api/Services/RaceDataOrchestrator.cs`, `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`.
- Modified: `frontend/src/features/live-race/hooks/useLastRaceResult.ts`, `frontend/src/features/live-race/LiveRacePage.tsx`, `frontend/src/features/live-race/LiveRacePage.test.tsx`.

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 8.1]
- [Source: _bmad-output/phase-2/planning-artifacts/prds/prd-F1_poc-2026-07-11/prd.md#FR-5]
- [Source: _bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/EXPERIENCE.md#State Patterns — "Zero completed races this season"]
- [Source: backend/F1App.Api/Services/RaceDataOrchestrator.cs#BuildFastestSectorBoard, #LoadFallbackDataAsync, #BuildSnapshot] — existing MVP-era fallback design, now extended
- [Source: frontend/src/features/live-race/hooks/useSignalRConnection.ts#handleSnapshot] — confirms unconditional store overwrite on every RaceSnapshot message
- [Source: https://api.openf1.org/v1/sessions] — verified live for session_key resolution by date/year

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- Verified OpenF1's `/sessions` endpoint live (curl) before writing any client code — confirmed `session_type=Race&year={year}` filtering works and returns `session_key`/`date_start`/`circuit_short_name`.
- `RaceDataOrchestratorTests`: needed to add an optional `openF1Mock` parameter to the existing `CreateOrchestrator()` test helper, since it previously created an unconfigurable `Mock<IOpenF1Client>` internally with no way for a test to set up historical-endpoint expectations.
- Backend: build clean; same 4 pre-existing test failures (confirmed via `git stash`, same as Stories 7.1–7.4); +5 new passing tests (197 → 202 total after the analyzer-warning fix, 197 passing before that commit).
- Frontend: full suite same 11 pre-existing failures, 126 passing (+2 new tests). Typecheck clean. One lint error present is the pre-existing `TrackMap.tsx` `react-hooks/set-state-in-effect` issue (confirmed via `git stash` in Story 7.3), not touched by this story.

### Completion Notes List

- Extended `IOpenF1Client`/`OpenF1Client` with 6 historical (explicit `session_key`) methods, kept fully separate from the existing `session_key=latest` live-polling methods — zero risk to the live path.
- `RaceDataOrchestrator.EnrichFallbackFromOpenF1Async` assembles a static final-state view (lap chart, session-best sectors, timeline, tyre/stint data) from OpenF1's historical endpoints, called once per fallback-entry transition from `LoadFallbackDataAsync`. Fully degrades to today's pre-story behavior on any failure.
- `BuildSnapshot()` and `BuildFastestSectorBoard()` now branch on the enriched fallback fields instead of unconditionally returning empty/null in fallback mode — live-mode behavior is completely unchanged.
- Frontend: fixed a real bug in `useLastRaceResult` (204 handling) and added the zero-completed-races message to `LiveRacePage.tsx`, reusing the existing `useRaceSchedule` hook and `formatDateRange` util rather than introducing new ones.
- All 4 ACs covered by tests; the scope discovery and the user's explicit choice to build the full historical integration are documented above rather than silently absorbed.

### File List

- `backend/F1App.Api/Dtos/OpenF1/OpenF1SessionDto.cs` (new)
- `backend/F1App.Api/Clients/IOpenF1Client.cs` (modified)
- `backend/F1App.Api/Clients/OpenF1Client.cs` (modified)
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` (modified)
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs` (modified)
- `frontend/src/features/live-race/hooks/useLastRaceResult.ts` (modified)
- `frontend/src/features/live-race/LiveRacePage.tsx` (modified)
- `frontend/src/features/live-race/LiveRacePage.test.tsx` (modified)

## Change Log

- 2026-07-13: Implemented guaranteed non-empty Live Race fallback (FR-5). Discovered and closed a real gap where sectors/lap-chart/timeline were intentionally empty in fallback mode (Ergast has no such archive); built real historical OpenF1 session integration per explicit user decision, to be reused by Story 8.2's replay feature. Also fixed a genuine bug in the zero-completed-races edge case (204 response threw instead of resolving gracefully).
