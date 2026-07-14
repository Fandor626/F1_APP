---
baseline_commit: cef07ab
---

# Story 10.3: Track historical data and records

Status: review

## Story

As a fan checking a Race Weekend before it starts,
I want to see the circuit's history — past winners and core stats,
so that I get a fuller picture of what this track means.

## Acceptance Criteria

1. **Given** a circuit with historical data (reusing the dataset already scoped for MVP's Circuit Profile page), **when** I open its Race Weekend Detail page, **then** a Circuit History section shows four stat tiles (length, corners, DRS zones, first F1 race year) and a past-winners list (year, driver, team).
2. **Given** a driver name in the past-winners list, **when** I click it, **then** it links through to that driver's profile page.
3. **Given** a circuit with only partial historical data, **when** the page renders, **then** it shows whatever data exists (e.g. stat tiles without a full winners list) rather than an error state.

## Tasks / Subtasks

- [x] Task 1: Fold circuit-history fields into the Race Weekend Detail endpoint (AC 1, 3)
  - [x] Backend: `RaceWeekendDetail` model gains `int? FirstF1Season`, `IReadOnlyList<CircuitWinner> PastWinners`, `CircuitStats? Stats` — reusing the same `CircuitProfile` object `RaceScheduleService.GetRaceDetailAsync` already fetches for Story 10.2's lap records (`GetCircuitProfileSafeAsync`, same rate-limit-tolerant wrapper, no new fetch).
  - [x] `FirstF1Season` mapped as `circuitProfile is null ? null : circuitProfile.FirstF1Season` (the source field is a non-nullable `int` on `CircuitProfile` that defaults to `0` for an unknown circuit — the `int?` distinguishes "no profile available" from a genuine year). `PastWinners` defaults to `[]`, never null, when the profile is unavailable.
  - [x] Frontend: `RaceWeekendDetailSchema` gains `firstF1Season`, `pastWinners`, `stats` (all `.nullable().optional()`/`.optional()`, matching the existing lap-record fields' `JsonIgnoreCondition.WhenWritingNull` convention) — one `useRaceDetail(round)` query, no new hook.
- [x] Task 2: Add `driverId` to `CircuitWinner` for the driver-profile link (AC 2)
  - [x] Backend: `CircuitWinner` record gains `DriverId` (second field, after `Season`, matching `LapRecord`'s `DriverId`-before-`DriverName` convention); `CircuitProfileService.ComputeAsync`'s winner-builder populates it from `x.Winner.Driver.DriverId` (same `ErgastDriverDto.DriverId` already captured for `LapRecord` in Story 10.2 — was already in scope, just not read here).
  - [x] Frontend: `CircuitWinnerSchema` gains `driverId: z.string()` — additive; the only existing consumer (`CircuitProfilePage.tsx`'s past-winners table) is unaffected since it doesn't (yet) use the field.
- [x] Task 3: `CircuitHistory` panel on the Race Weekend Detail page (AC 1, 2, 3)
  - [x] New `frontend/src/features/calendar/CircuitHistory.tsx` — a panel separate from `TrackRecords` (Story 10.2), per UX-DR9: four `bg-inset` stat tiles (Length, Corners, DRS Zones, First F1 Race) above a past-winners table (Year, Driver, Team), driver names as `<Link to="/drivers/{driverId}">` (same pattern as `TrackRecords.tsx`'s `LapRecordRow`).
  - [x] Renders `null` entirely when there's no stats data (`stats` and `firstF1Season` both absent) and no winners — AC 3's "shows whatever data exists" extends to "shows nothing, not an error" when there's genuinely nothing. Renders the stat-tile grid without the winners table when `pastWinners` is empty (the AC's own example: "stat tiles without a full winners list"), and vice versa.
  - [x] Wired into `RaceWeekendDetailView.tsx` immediately after `<TrackRecords />`, before the Sessions section — grouping the two circuit-context panels together.
- [x] Task 4: Tests
  - [x] New `CircuitHistory.test.tsx`: full data renders all four tiles + winners table with correct driver-profile links; partial data (stats only, empty winners) renders tiles without a table; zero data renders nothing.
  - [x] `RaceWeekendDetailView.test.tsx`: new tests asserting the Circuit History section renders with correct data for round 1 (now has `firstF1Season`/`stats`/`pastWinners` in the mock fixture) and is entirely absent for round 2 (still has none) — added a `data-testid="track-records"` wrapper to `TrackRecords.tsx` so this file's existing Track Records assertions (round 1 also has a `recentLapRecord` for "Lando Norris", who now also appears as a Circuit History past winner) can scope unambiguously with `within(...)`.
  - [x] Backend: new `RaceScheduleServiceTests.GetRaceDetailAsync_PopulatesCircuitHistoryFromCircuitProfile` — asserts `FirstF1Season`/`PastWinners`/`PastWinners[0].DriverId` populate from a stubbed `CircuitProfileService`, and that `Stats` is legitimately `null` for a test-fixture `circuitId` not present in `CircuitStaticFacts` (exercises the AC 3 partial-data path directly). Extended `CircuitProfileServiceTests.GetCircuitProfileAsync_ComputesFirstSeasonWinnersAndLapRecord` with a `PastWinners[0].DriverId` assertion.

## Dev Notes

- Confirmed (again, via `git stash`) that the same 4 backend and 4 frontend test failures pre-exist on `main`, unrelated to this story.
- `CircuitWinner`'s constructor-arg insertion (`DriverId` as the new second positional parameter) had one call site (`CircuitProfileService.ComputeAsync`'s winner-builder) and no test constructing it positionally — safe.
- Deliberately did **not** add driver-profile links to `CircuitProfilePage.tsx`'s own past-winners table — out of this story's scope (Race Weekend Detail only), though `driverId` now flows through the same `CircuitWinnerSchema` that page already consumes, making it a trivial follow-up.
- `CircuitProfileSchema` (backing `useCircuitProfile`, the MVP Circuit Profile page) still doesn't expose `recentLapRecord` even though the backend model has carried it since before this story — a pre-existing gap, unrelated to and out of scope for Story 10.3.

### Architecture / FR / UX-DR references (verbatim from epics.md)

> **FR-15:** The Race Weekend detail page displays additional circuit historical data and records: past race winners (year, driver, team) and core circuit stats (length, corners, DRS zones, first F1 race year).
>
> **UX-DR9:** `circuit-stat-tile` grid — Race Weekend Detail's Circuit History card: four `bg-inset` tiles (length, corners, DRS zones, first F1 race year) above a past-winners list (year, driver, team; driver names link to profiles). Circuits with partial historical data show whatever exists rather than an error state.

Architecture spine: `FR-13–FR-15 (track layout, records, history) | RaceWeekendDetailView.tsx | AD-5, AD-6, AD-7 (outline); inherited Circuit Profile data (no new AD)` — no new architecture decision needed; this story (like 10.2) folds more `CircuitProfile` fields through the pipeline Story 10.2 established.

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 10.3]
- [Source: _bmad-output/phase-2/implementation-artifacts/10-2-track-lap-record-context.md] — established the `CircuitProfileService`-folded-into-`RaceWeekendDetail` pattern this story extends
- [Source: backend/F1App.Api/Services/CircuitProfileService.cs], [Source: backend/F1App.Api/Services/RaceScheduleService.cs]
- [Source: frontend/src/features/calendar/TrackRecords.tsx] — visual/link pattern reused for `CircuitHistory.tsx`
- [Source: frontend/src/features/profiles/CircuitProfilePage.tsx] — existing MVP stats/past-winners rendering (different visual treatment: combined `bg-card` block vs. this story's four `bg-inset` tiles per UX-DR9)

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Completion Notes List

- All 4 tasks complete. Full frontend suite: 193/197 passing; full backend suite: 209/213 passing (2 net new backend tests, both passing) — all pre-existing failures unrelated (verified via `git stash`).
- `npx tsc -b` clean, `eslint` clean on all touched frontend files (one pre-existing unrelated error remains in `CircuitTrackLayout.tsx`, untouched by this story). `dotnet build` clean.

### File List

- Modified: `backend/F1App.Api/Models/CircuitProfile.cs`
- Modified: `backend/F1App.Api/Models/RaceWeekendDetail.cs`
- Modified: `backend/F1App.Api/Services/CircuitProfileService.cs`
- Modified: `backend/F1App.Api/Services/RaceScheduleService.cs`
- Modified: `backend/F1App.Api.Tests/Services/CircuitProfileServiceTests.cs`
- Modified: `backend/F1App.Api.Tests/Services/RaceScheduleServiceTests.cs`
- New: `frontend/src/features/calendar/CircuitHistory.tsx`
- New: `frontend/src/features/calendar/CircuitHistory.test.tsx`
- Modified: `frontend/src/features/calendar/RaceWeekendDetailView.tsx`
- Modified: `frontend/src/features/calendar/RaceWeekendDetailView.test.tsx`
- Modified: `frontend/src/features/calendar/TrackRecords.tsx` (added `data-testid` for test scoping)
- Modified: `frontend/src/shared/api/ergast.ts`
- Modified: `frontend/src/shared/mocks/handlers/ergastHandlers.ts`
