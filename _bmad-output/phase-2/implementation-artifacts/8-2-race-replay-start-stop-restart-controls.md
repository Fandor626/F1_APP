---
baseline_commit: 58f372f
---

# Story 8.2: Race Replay start/stop/restart controls

Status: review

## Story

As a fan viewing a fallback/past race,
I want to start playing it back and restart from the beginning,
so that I can actively watch it unfold instead of only reading a static result.

## Acceptance Criteria

1. **Given** the Live Race page is in fallback mode (Story 8.1), **when** the page renders, **then** a Replay control bar is visible, fixed to the bottom of the viewport.
2. **Given** I click Play, **when** playback starts, **then** per-lap race data is fetched once from a new endpoint (`GET /api/races/{season}/{round}/replay`), and positions/gaps/tyres advance lap by lap using the full live-store setter sequence (`setDrivers` via `normalizeSnapshot`, `setLapChart`, `setFastestSectors`, `setTimeline`) — not just the drivers field (AD-1, AD-2).
3. **Given** playback is running, **when** I click Restart, **then** playback returns to lap 1 without a page reload.
4. **Given** the replay data fetch, **when** the backend assembles it, **then** it's cached with a 7-day TTL (same tier as other historical/immutable race data), so repeated views of the same race don't re-fetch.

## Tasks / Subtasks

- [x] Task 0 (discovered during implementation — user explicitly chose the approach, see Dev Notes): Per-lap position reconstruction approach
  - [x] User chose cumulative-lap-time-based reconstruction over full GPS/timestamp correlation
  - [x] `LastRaceResult` extended with `Season`/`Round` (needed by the frontend to know which race to request replay for — wasn't previously exposed)
- [x] Task 1: `RaceReplayService` (AC 2, 4)
  - [x] Reuses the exact historical `IOpenF1Client` methods Story 8.1 built (session lookup by date, laps/stints/race_control/pit/drivers-for-session) — zero new OpenF1 client surface
  - [x] Builds one `RaceStateSnapshot`-shaped frame per lap: drivers ranked by laps-completed then cumulative elapsed time, progressive lap chart, progressive session-best sectors, progressive timeline (race-control + pit + fastest-lap markers)
  - [x] `GET /api/races/{season}/{round}/replay` on `RacesController`, 7-day cache TTL
  - [x] `ParseRaceControlEvent` made fully `static` (was instance + optional-override) so `RaceReplayService` can reuse it without needing a `RaceDataOrchestrator` instance
- [x] Task 2: Frontend replay engine (AC 1, 2, 3)
  - [x] `replayStore.ts` (Zustand, AD-3): `currentLapIndex`, `isPlaying`, `speed` only — the frame array itself never enters this store
  - [x] `useRaceReplayQuery.ts`: fetches the frame array once, `enabled` only after the user's first Play click (not eagerly on page load)
  - [x] `ReplayBar.tsx`: play/pause toggle + restart button + lap readout; client-side interval ticking (AD-4) at `baseIntervalMs / speed`; applies each frame to `liveRaceStore` via the full setter sequence on every `currentLapIndex` change
  - [x] Wired into `LiveRacePage.tsx`: shown when `isFallback && hasLiveData && lastRaceData` (a real race with known season/round)
- [x] Task 3: Tests
  - [x] 6 new `RaceReplayServiceTests` — including one that caught and drove a fix for a real ranking bug (see Debug Log)
  - [x] 5 new `ReplayBar.test.tsx`, 2 new `LiveRacePage.test.tsx` tests

## Dev Notes

- **User explicitly chose the position-reconstruction approach** — asked directly given real complexity/accuracy trade-offs, rather than assumed. Two options were presented: (a) cumulative-lap-time based (rank by total elapsed race time; fully real lap-time data; can diverge slightly from true track position during in-progress pit cycles) vs (b) full GPS/timestamp correlation (accurate but requires correlating three separate OpenF1 datasets by timestamp — significantly larger scope). **User chose (a).** This is documented here so the trade-off is visible to whoever reads this story later, not just implied by the code.
- **`LastRaceResult` needed `Season`/`Round` — a real gap found while wiring the frontend.** The replay endpoint is parameterized by season/round (per Architecture AD-2's exact route shape), but nothing previously exposed those values to the page that needs to construct the request — `/api/races/last-result` only had `raceName`/`raceDate`/`drivers`. Extended the model and its one construction site (`RaceScheduleService.GetLastRaceResultAsync`) rather than adding a second endpoint.
- **Real bug caught by a test, not by inspection: naive cumulative-time ranking put retired drivers *ahead*.** A driver who retired after lap 1 has a *smaller* total elapsed time than a driver who's completed 20 laps — sorting purely by cumulative time ranks the retiree first, which is backwards. Fixed by ranking on laps-completed first (descending), cumulative time second (ascending) — matching how real race classification actually works. `RaceReplayServiceTests.GetReplayAsync_RetiredDriverStopsAdvancing_FallsBehindInLaterFrames` exists specifically to guard this.
- **`GapToCarAhead` between drivers on different lap counts is still a raw time delta**, not a "+1 LAP" style label — an accepted simplification consistent with the user's chosen cumulative-time-based scope. A driver a lap down will show a large numeric gap rather than lap-count notation. Flagging this rather than silently deciding it was fine.
- **`ParseRaceControlEvent` refactored to fully `static`** (previously an instance method with an optional driver-info override added in Story 8.1). `RaceReplayService` needed to call it without holding a `RaceDataOrchestrator` instance (which is a `BackgroundService` with live-polling state that has no business being instantiated by a request-scoped replay service). The one live call site (`PollRaceControlAsync`) now passes `_driverInfo` explicitly instead of relying on an implicit default.
- **Do not touch `RaceDataOrchestrator`'s fallback enrichment (Story 8.1) for this story.** `EnrichFallbackFromOpenF1Async` builds a *static final-state* snapshot; `RaceReplayService` builds a *full per-lap sequence*. They share the historical `IOpenF1Client` methods and the `ParseRaceControlEvent` parser, and nothing else — this is intentional, not an oversight (see Story 8.1's own Dev Notes, which already flagged this exact split).
- **`ReplayBar`'s fetch trigger is gated on `hasStarted` (a local boolean flipped on first Play click), not on `isFallback` alone** — per AC 2's "per-lap race data is fetched once... when playback starts," not eagerly when the fallback page loads (most fallback visits never click Play).
- **`useLastRaceResult`'s `enabled` condition changed from `isFallback && !hasLiveData` to just `isFallback`** (in `LiveRacePage.tsx`) — needed so season/round are available regardless of whether SignalR or this REST call ends up being the one that actually populates the driver list. Previously, if SignalR delivered fallback data before this REST call ever fired, the query would stay permanently disabled and the Replay bar would never have a season/round to work with. The driver-populating side effect is still correctly guarded by `!hasLiveData` — only the fetch-trigger condition changed.

### Project Structure Notes

- New: `backend/F1App.Api/Services/RaceReplayService.cs`, `backend/F1App.Api.Tests/Services/RaceReplayServiceTests.cs`.
- Modified: `backend/F1App.Api/Models/LastRaceResult.cs`, `backend/F1App.Api/Services/RaceScheduleService.cs`, `backend/F1App.Api/Services/RaceDataOrchestrator.cs` (static `ParseRaceControlEvent`), `backend/F1App.Api/Services/CacheKeys.cs`, `backend/F1App.Api/Controllers/RacesController.cs`, `backend/F1App.Api/Program.cs`, `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs` (call-site updates).
- New: `frontend/src/features/live-race/store/replayStore.ts`, `frontend/src/features/live-race/hooks/useRaceReplayQuery.ts`, `frontend/src/features/live-race/ReplayBar/ReplayBar.tsx`, `frontend/src/features/live-race/ReplayBar/ReplayBar.test.tsx`.
- Modified: `frontend/src/shared/types/f1.ts` (`LastRaceResult` gains `season`/`round`), `frontend/src/shared/api/queryKeys.ts`, `frontend/src/features/live-race/LiveRacePage.tsx`, `frontend/src/features/live-race/LiveRacePage.test.tsx`.

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 8.2]
- [Source: _bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md#AD-1, #AD-2, #AD-3, #AD-4]
- [Source: _bmad-output/phase-2/implementation-artifacts/8-1-guaranteed-non-empty-live-race-page.md] — the historical OpenF1 client methods and `ParseRaceControlEvent` this story reuses were built there
- [Source: backend/F1App.Api/Clients/IOpenF1Client.cs] — historical methods (`GetRaceSessionsAsync`, `GetLapsForSessionAsync`, etc.), all from Story 8.1
- [Source: frontend/src/features/live-race/store/liveRaceStore.ts] — the shared setter sequence replay frames apply through

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- `RaceReplayServiceTests.GetReplayAsync_RetiredDriverStopsAdvancing_FallsBehindInLaterFrames` failed on first run with a retired driver ranked *ahead* of an active one — root-caused to sorting purely by cumulative elapsed time (a retiree's small elapsed time looks like a fast lap, not a DNF). Fixed the sort to rank by laps-completed descending, then cumulative time ascending. Documented in Dev Notes as a real correctness fix the test suite caught, not something found by inspection.
- `RaceDataOrchestratorTests.cs`: 6 existing `ParseRaceControlEvent` tests needed updating for the new fully-static signature (previously callable as `sut.ParseRaceControlEvent(msg)` with an implicit default; now `RaceDataOrchestrator.ParseRaceControlEvent(msg, sut._driverInfo)`).
- Backend: build clean; same 4 pre-existing test failures (confirmed unrelated across all of Epic 7/8.1); 203 → 209 passing (+6 new).
- Frontend: full suite same 11 pre-existing failures, 133 passing (+7 new). Typecheck clean. One lint error is the same pre-existing `TrackMap.tsx` issue flagged in Stories 7.3/8.1, not touched here.

### Completion Notes List

- `RaceReplayService` reuses 100% of Story 8.1's historical OpenF1 integration — no new client methods needed, validating that investment.
- All 4 ACs covered by tests; the ranking bug fix and the `useLastRaceResult` enabled-condition fix are both documented as real issues found and fixed during implementation, not silently absorbed.
- Speed control (button group, 2x/4x) and scrub bar are explicitly out of scope — Stories 8.3/8.4/8.5. `replayStore.speed` exists and the ticking interval already respects it, but no UI exposes it yet.

### File List

- `backend/F1App.Api/Services/RaceReplayService.cs` (new)
- `backend/F1App.Api.Tests/Services/RaceReplayServiceTests.cs` (new)
- `backend/F1App.Api/Models/LastRaceResult.cs` (modified)
- `backend/F1App.Api/Services/RaceScheduleService.cs` (modified)
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` (modified)
- `backend/F1App.Api/Services/CacheKeys.cs` (modified)
- `backend/F1App.Api/Controllers/RacesController.cs` (modified)
- `backend/F1App.Api/Program.cs` (modified)
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs` (modified)
- `frontend/src/features/live-race/store/replayStore.ts` (new)
- `frontend/src/features/live-race/hooks/useRaceReplayQuery.ts` (new)
- `frontend/src/features/live-race/ReplayBar/ReplayBar.tsx` (new)
- `frontend/src/features/live-race/ReplayBar/ReplayBar.test.tsx` (new)
- `frontend/src/shared/types/f1.ts` (modified)
- `frontend/src/shared/api/queryKeys.ts` (modified)
- `frontend/src/features/live-race/LiveRacePage.tsx` (modified)
- `frontend/src/features/live-race/LiveRacePage.test.tsx` (modified)

## Change Log

- 2026-07-13: Implemented Race Replay start/stop/restart controls (FR-6). Built `RaceReplayService`, fully reusing Story 8.1's historical OpenF1 integration, per user-approved cumulative-lap-time position reconstruction. Caught and fixed a real ranking bug (retired drivers sorting ahead of active ones) via test-driven development. Extended `LastRaceResult` with season/round to unblock the frontend's replay request.
