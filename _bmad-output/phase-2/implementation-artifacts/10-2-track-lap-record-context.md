---
baseline_commit: 5606767
---

# Story 10.2: Track lap record context

Status: review

## Story

As a fan checking a Race Weekend before it starts,
I want to see the all-time and this-year's fastest lap at this circuit,
so that I have real performance context, not just the track shape.

## Acceptance Criteria

1. **Given** a circuit with lap record data, **when** I open its Race Weekend Detail page, **then** a Track Records section (separate from the track-layout panel, per UX-DR8) shows the all-time fastest lap and the current/most-recently-completed-year's fastest lap, each with driver name.
2. **Given** a driver name in the Track Records section, **when** I click it, **then** it links through to that driver's profile page.
3. **Given** a circuit lacking full record data, **when** the page renders, **then** it shows whatever data exists rather than an error state.

## Tasks / Subtasks

- [x] Task 1: Expose lap records on the Race Weekend Detail endpoint (AC 1, 3)
  - [x] Backend: `RaceWeekendDetail` model gains `LapRecord? AllTimeLapRecord, LapRecord? RecentLapRecord`, mirroring the fields `RaceWeekendSummary` (the `/api/races` list endpoint) already exposes for the same underlying `CircuitProfileService` computation.
  - [x] `RaceScheduleService.GetRaceDetailAsync`: fetches the circuit profile via the existing `GetCircuitProfileSafeAsync` helper (same rate-limit-tolerant wrapper `GetCurrentSeasonScheduleAsync` already uses — a profile fetch failure degrades to `null` records, never a 500) and folds `profile?.LapRecord`/`profile?.RecentLapRecord` into `ToDetail`.
  - [x] Frontend: `RaceWeekendDetailSchema` gains `allTimeLapRecord`/`recentLapRecord` (`LapRecordSchema.nullable().optional()`, matching `RaceWeekendSchema`'s existing treatment for the same fields on the list endpoint) — one `useRaceDetail(round)` query surfaces everything, no second hook.
- [x] Task 2: Add a `driverId` to `LapRecord` for the driver-profile link (AC 2)
  - [x] Backend: `LapRecord` record gains `DriverId` (first field, matching `DriverStanding`'s `DriverId`-before-`DriverName` convention); `CircuitProfileService.FindLapRecord` populates it from `result.Driver.DriverId` (already in scope — `ErgastDriverDto.DriverId` was always available, just not captured).
  - [x] Frontend: `LapRecordSchema` gains `driverId: z.string()` — additive to every existing `LapRecord` consumer (`RaceWeekendCard.tsx`'s calendar-card records, `CircuitProfilePage.tsx`'s all-time record), all of which already receive it live from the backend now that `FindLapRecord` always sets it.
- [x] Task 3: `TrackRecords` panel on the Race Weekend Detail page (AC 1, 2, 3)
  - [x] New `frontend/src/features/calendar/TrackRecords.tsx` — a panel separate from the Story 10.1 track-layout panel (UX-DR8: "not merged into one track mega-card"), rendering up to two `bg-inset` rows ("All-Time Fastest Lap", "{season} Fastest Lap"), each with the driver name as a `<Link to="/drivers/{driverId}">` (matching `DriversStandingsTable.tsx`'s existing driver-link pattern — the only prior precedent for linking a driver name in this codebase).
  - [x] Renders `null` (no heading, no panel) when both records are absent — AC 3's "show whatever data exists" extends to "show nothing, not an error" when there's nothing to show; renders only the row(s) that exist when partial data is available.
  - [x] Wired into `RaceWeekendDetailView.tsx` between the track-layout panel and the Sessions section.
- [x] Task 4: Tests
  - [x] New `TrackRecords.test.tsx`: both records render with correct driver-profile links and times; only-one-record case shows just that row; zero-records case renders nothing.
  - [x] `RaceWeekendDetailView.test.tsx`: new tests asserting the Track Records section appears with working driver links for round 1 (which now has both lap records in the mock fixture) and is entirely absent for round 2 (which has none) — extends `sampleRaceDetailsByRound` in `ergastHandlers.ts` accordingly.
  - [x] Backend: no new unit tests added — `RaceScheduleServiceTests`' existing `GetRaceDetailAsync_*` tests already exercise the new `CircuitProfileService` dependency via the pre-existing `EmptyCircuitProfileService()` test helper (added ahead of this story, evidently in anticipation of it), and `CircuitProfileServiceTests` already covers `FindLapRecord` directly — the `DriverId` field addition is exercised transitively by both.

## Dev Notes

- Confirmed via `git stash` that 4 backend test failures (`CircuitProfileServiceTests` × 2 casing mismatches, `DriversControllerTests.Compare_ReturnsCamelCaseComparison`, `StandingsControllerTests.GetSeasonWrapped_ReturnsNullBodyWhenSeasonInProgress`) and 4 frontend `dateUtils.test.ts` failures all pre-exist on `main`, unrelated to this story's changes.
- `LapRecord`'s constructor-arg reorder (`DriverId` inserted as the new first positional parameter) only had one call site (`CircuitProfileService.FindLapRecord`) and no test constructed it positionally — safe, no other breakage.
- Deliberately did **not** touch `CircuitProfilePage.tsx`'s existing lap-record display to add a driver link — that page is out of this story's scope (Race Weekend Detail only); it's a small, obvious follow-up now that `driverId` flows through the same `LapRecordSchema` it already consumes.

### Architecture / FR / UX-DR references (verbatim from epics.md)

> **FR-14:** The Race Weekend detail page displays the all-time fastest lap and the fastest lap of the current/most-recent year at this circuit, each with the driver's name.
>
> **UX-DR8:** `track-records-section` — Race Weekend Detail, a separate card from the track-layout panel (not merged into one "track" mega-card): all-time and current-year fastest lap, each rendered as a `bg-inset` row with driver name (clickable through to profile).

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 10.2]
- [Source: backend/F1App.Api/Services/CircuitProfileService.cs], [Source: backend/F1App.Api/Services/RaceScheduleService.cs]
- [Source: frontend/src/features/calendar/RaceWeekendCard.tsx] — existing `allTimeLapRecord`/`recentLapRecord` consumer, same data shape
- [Source: frontend/src/features/standings/DriversStandingsTable.tsx] — driver-name-to-profile link pattern reused verbatim

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Completion Notes List

- All 4 tasks complete. Full frontend suite: 188/192 passing; full backend suite: 207/211 passing — all 8 failures pre-existing and unrelated (verified via `git stash`).
- `npx tsc -b` clean, `eslint` clean on all touched frontend files. `dotnet build` clean (only pre-existing NuGet advisory warnings, unrelated to source changes).
- Backend and frontend `LapRecord` shapes now agree exactly (`driverId`, `driverName`, `constructorName`, `time`, `season`) across all three consumers: Calendar card, Race Weekend Detail (new), Circuit Profile page.

### File List

- Modified: `backend/F1App.Api/Models/CircuitProfile.cs`
- Modified: `backend/F1App.Api/Models/RaceWeekendDetail.cs`
- Modified: `backend/F1App.Api/Services/CircuitProfileService.cs`
- Modified: `backend/F1App.Api/Services/RaceScheduleService.cs`
- New: `frontend/src/features/calendar/TrackRecords.tsx`
- New: `frontend/src/features/calendar/TrackRecords.test.tsx`
- Modified: `frontend/src/features/calendar/RaceWeekendDetailView.tsx`
- Modified: `frontend/src/features/calendar/RaceWeekendDetailView.test.tsx`
- Modified: `frontend/src/features/calendar/RaceWeekendCard.test.tsx`
- Modified: `frontend/src/shared/api/ergast.ts`
- Modified: `frontend/src/shared/mocks/handlers/ergastHandlers.ts`
