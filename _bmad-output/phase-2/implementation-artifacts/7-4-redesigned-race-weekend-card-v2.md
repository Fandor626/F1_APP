---
baseline_commit: a156069
---

# Story 7.4: Redesigned Race Weekend card v2

Status: review

## Change Log

- 2026-07-13: Redesigned the Race Weekend card (FR-4) — track outline sub-panel via new `TrackOutline.tsx`, two-line fastest-lap block, standings removed (now owned by Story 7.2's sidebar). Discovered mid-implementation that the backend had no per-circuit "recent year" lap-record data; extended `RaceScheduleService`/`CircuitProfileService` to compute and bundle both lap records into the existing `/api/races` response (parallelized, cache-reused, no new endpoint or extra frontend requests).

## Story

As a fan scanning the Calendar,
I want each race card to show a real track shape and meaningful lap-record context instead of repeated standings,
so that every card tells me something new about that specific race.

## Acceptance Criteria

1. **Given** the Championship Sidebar (Story 7.2) now shows standings, **when** any Race Weekend card renders, **then** it no longer displays driver/constructor standings data.
2. **Given** a circuit with available outline data (Story 7.3), **when** a Race Weekend card renders, **then** it shows a recognizable track outline in a distinct left-side sub-panel, card text content to its right.
3. **Given** a circuit with available historical lap data, **when** a Race Weekend card renders, **then** it shows an all-time fastest lap and a current/recent-year fastest lap, each with driver name, as two explicit labeled lines.
4. **Given** a circuit without qualifying historical lap data, **when** a Race Weekend card renders, **then** the fastest-lap block is omitted gracefully, not shown as an error.
5. **Given** the card's existing click-through behavior, **when** I click anywhere on the card, **then** it still navigates to the Race Weekend detail view, unchanged.
6. **Given** the card's sizing, **when** new content is added, **then** the card grows taller only — same width/grid column as the phase-1 card.
7. **Given** a track outline visual, **when** a screen reader encounters it, **then** it has an accessible name identifying the circuit, not a literal shape description (UX-DR13). (FR-4 · PRD §4.1 · UX-DR2, UX-DR13 · AD-5, AD-6, AD-7)

## Tasks / Subtasks

- [x] Task 0 (discovered during implementation, not in the original spec — see Dev Notes): Backend now returns per-race lap-record data
  - [x] `GET /api/races` (`RaceWeekendSummary`) gains `AllTimeLapRecord`/`RecentLapRecord` fields, computed via the existing `CircuitProfileService` (parallelized across distinct circuits, 7-day cache reused)
  - [x] `CircuitProfile` model gains `RecentLapRecord` (most-recent-season fastest lap at that circuit) alongside the existing all-time `LapRecord`
- [x] Task 1: Build `TrackOutline.tsx` (AC: 2, 4, 7)
  - [x] New shared component, fetches `/circuit-configs/{circuitId}.json` (relative path), renders `trackPath` in an `<svg viewBox>` — no live GPS/dot logic
  - [x] `<svg role="img" aria-label="Track layout: {circuitName}">`
  - [x] Missing/failed config → renders nothing
  - [x] Sized via parent CSS (`className` prop), not internally fixed
- [x] Task 2: Redesign `RaceWeekendCard.tsx` (AC: 1, 2, 3, 5, 6)
  - [x] Removed `StandingsColumn`, standings hooks, and the standings block entirely
  - [x] `TrackOutline` in a left sub-panel (168px, `bg-bg-inset`), content flows right (flex row)
  - [x] Two-line fastest-lap block, each line independently omitted when its record is null; whole block omitted when both are null
  - [x] `<Link>` wrapper, `data-testid`, `isNext` badge unchanged
- [x] Task 3: Frontend schema (AC: 3, 4)
  - [x] `RaceWeekendSchema` gains `allTimeLapRecord`/`recentLapRecord` — reused the existing `LapRecordSchema` (moved earlier in the file so both schemas can reference it) rather than duplicating it
- [x] Task 4: Tests (AC: 1–7)
  - [x] New `TrackOutline.test.tsx` — 3 tests (renders + correct fetch URL, omits on failure, omits while loading)
  - [x] `RaceWeekendCard.test.tsx` — replaced the standings test with a no-standings test, added fastest-lap-shown/omitted tests and an accessible-track-outline integration test

## Dev Notes

- **Backend data didn't exist for this story's AC 3 — this was discovered, not assumed, during implementation.** Neither `RaceWeekendSummary` (the `/api/races` schedule response this card is built from) nor any other existing endpoint carried a "current/recent-year fastest lap at this circuit" value — only `CircuitProfileService`'s existing `LapRecord` (all-time, scanning every historical race at a circuit). Two designs were considered: (a) each card fetches `/api/circuits/{circuitId}/profile` independently (N+1 frontend requests, up to 24 on a cold Calendar load, hammering the fragile/deprecated Ergast API in a burst — a real reliability concern per phase-1 architecture's own risk notes), or (b) bundle both lap records into the existing single `/api/races` response, computed server-side. Chose (b): `RaceScheduleService.GetCurrentSeasonScheduleAsync` now resolves each distinct circuit's profile via `Task.WhenAll` (parallel, not N sequential round-trips) and reuses `CircuitProfileService`'s own 7-day cache — one HTTP call from the frontend, same as today.
- **`CircuitProfile.RecentLapRecord`** = the fastest lap in the most recent season this circuit has raced (not necessarily this calendar year, if the circuit hasn't run yet this season) — computed by `CircuitProfileService.FindRecentLapRecord`, filtering the same already-fetched `races` list `FindLapRecord` (all-time) already scans. No new external API call.
- **Do not build TrackOutline as part of `TrackMap.tsx` or reuse `TrackMap.tsx` directly.** `TrackMap.tsx` is the *live* map (GPS dot interpolation, `useLiveRaceStore`, fallback-mode awareness) — architecturally a different concern (per Architecture AD-5, `TrackOutline.tsx` is the new *decorative* consumer of the same `circuit-configs/{circuitId}.json` asset, shared between this story's card and Story 10.1's Race Weekend Detail page, with zero live-race-state dependency).
- **`circuit-configs` fetch must use the Story 7.3 fix** — relative path (`/circuit-configs/${circuitId}.json`), never `VITE_API_BASE_URL`-prefixed. `TrackOutline.tsx` is a brand-new file, so there's no "old bug" to carry forward here — just don't reintroduce the pattern Story 7.3 just fixed elsewhere.
- **Card layout note (read `RaceWeekendCard.tsx` before editing):** the current card is a single `<Link>` block with everything stacked vertically (flag+name, circuit, dates, then the standings grid at the bottom). AC 2 needs a left-side sub-panel with the rest of the content to its right — this changes the card from a single vertical stack to a `flex flex-row` at the top level (outline panel + a content column), while AC 6 requires the *overall* card to keep the same width — only height grows. Existing `data-testid="race-weekend-card"` stays on the outermost `<Link>`.
- **Graceful degradation, matching established project convention:** cards for circuits without qualifying historical lap data already have precedent (MVP FR-4's "omitted/labelled" pattern per the PRD's own note on FR-4) — this story's fastest-lap block follows the same rule: each line independently optional, never an error state, never a placeholder skeleton for permanently-absent data.

### Project Structure Notes

- New: `frontend/src/features/calendar/TrackOutline.tsx`, `frontend/src/features/calendar/TrackOutline.test.tsx`.
- Modified: `frontend/src/features/calendar/RaceWeekendCard.tsx`, `frontend/src/features/calendar/RaceWeekendCard.test.tsx`, `frontend/src/shared/api/ergast.ts`.
- Backend (already done in Task 0): `backend/F1App.Api/Models/CircuitProfile.cs`, `backend/F1App.Api/Models/RaceWeekendSummary.cs`, `backend/F1App.Api/Services/CircuitProfileService.cs`, `backend/F1App.Api/Services/RaceScheduleService.cs`, plus 4 test files (`CircuitProfileServiceTests.cs` unaffected — verified; `RaceScheduleServiceTests.cs`, `SeasonWrappedServiceTests.cs`, `StandingsServiceTests.cs` updated for the new constructor/record shape).

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 7.4]
- [Source: _bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md#AD-5, #AD-6, #AD-7]
- [Source: _bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/DESIGN.md#weekend-card-v2] — track-outline sub-panel width (~168px), two-line fastest-lap block structure
- [Source: frontend/src/features/calendar/RaceWeekendCard.tsx] — current implementation being redesigned
- [Source: frontend/src/features/live-race/TrackMap/TrackMap.tsx] — sibling consumer of the same `circuit-configs` asset, NOT reused directly (different concern: live vs decorative)
- [Source: backend/F1App.Api/Services/CircuitProfileService.cs#FindLapRecord, #FindRecentLapRecord]
- [Source: backend/F1App.Api/Services/RaceScheduleService.cs#GetCurrentSeasonScheduleAsync]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- **Discovered the "recent year" lap-record data didn't exist anywhere in the backend** while implementing Task 2 (this wasn't flagged in the Architecture spine's Capability Map for FR-4, which cited only AD-5/6/7 — all about the outline, not lap-record sourcing). Resolved as Task 0: extended the existing `/api/races` response rather than adding a new endpoint or having each card fetch `/api/circuits/{id}/profile` independently — the latter would mean up to 24 concurrent requests against the fragile/deprecated Ergast API on every cold Calendar load, which phase-1's own architecture doc flags as a real risk. Full reasoning in Dev Notes.
- `RaceWeekendSchema`'s new fields used `.nullable().optional()`, not just `.nullable()` — the backend's global `JsonIgnoreCondition.WhenWritingNull` (confirmed in `Program.cs`) omits null fields from JSON entirely rather than sending `null`, so the key may be absent, not just null-valued.
- `TrackOutline.tsx`'s effect originally copied `TrackMap.tsx`'s existing fetch pattern (`setConfig(null); setUnavailable(false)` synchronously at the top of the effect) and tripped `react-hooks/set-state-in-effect`. Since this is a brand-new file (unlike `TrackMap.tsx`, where the same pre-existing pattern was out of this story's scope to fix), rewrote it properly: a `cancelled` flag guards all `setState` calls, which now only happen inside the fetch's `.then()`/`.catch()`, not synchronously in the effect body.
- Backend: build clean, same 4 pre-existing test failures as Stories 7.1–7.3 confirmed unrelated (verified via `git stash`), +1 new passing test (196 total, 192 passing).
- Frontend: full suite same 11 pre-existing failures, 124 passing (+6 new tests). Typecheck and lint both clean.

### Completion Notes List

- Backend: `RaceScheduleService.GetCurrentSeasonScheduleAsync` now resolves each distinct circuit's `CircuitProfile` in parallel (`Task.WhenAll`) and merges `AllTimeLapRecord`/`RecentLapRecord` into `RaceWeekendSummary` — one HTTP call from the frontend, unchanged from today; `CircuitProfileService.FindRecentLapRecord` reuses the same already-fetched race-results list `FindLapRecord` scans, no extra Ergast call.
- Frontend: new `TrackOutline.tsx` is the shared decorative-outline component (Architecture's structural seed anticipated this for both this story and Story 10.1) — deliberately independent of `TrackMap.tsx`'s live-race concerns.
- `RaceWeekendCard.tsx` restructured from a single vertical stack to a flex row (outline panel + content column) while keeping the card's overall width unchanged, per AC 6.
- Reused the existing `LapRecordSchema` (already defined for `CircuitProfileSchema`) for the new fields rather than duplicating it — moved its declaration earlier in `ergast.ts` since it's now referenced by two schemas.

### File List

- `backend/F1App.Api/Models/CircuitProfile.cs` (modified)
- `backend/F1App.Api/Models/RaceWeekendSummary.cs` (modified)
- `backend/F1App.Api/Services/CircuitProfileService.cs` (modified)
- `backend/F1App.Api/Services/RaceScheduleService.cs` (modified)
- `backend/F1App.Api.Tests/Services/RaceScheduleServiceTests.cs` (modified)
- `backend/F1App.Api.Tests/Services/SeasonWrappedServiceTests.cs` (modified)
- `backend/F1App.Api.Tests/Services/StandingsServiceTests.cs` (modified)
- `frontend/src/features/calendar/TrackOutline.tsx` (new)
- `frontend/src/features/calendar/TrackOutline.test.tsx` (new)
- `frontend/src/features/calendar/RaceWeekendCard.tsx` (modified)
- `frontend/src/features/calendar/RaceWeekendCard.test.tsx` (modified)
- `frontend/src/shared/api/ergast.ts` (modified)
