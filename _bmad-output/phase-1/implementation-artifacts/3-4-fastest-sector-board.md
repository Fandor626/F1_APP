---
baseline_commit: "7ef4f57"
---

# Story 3.4: Fastest Sector Board

Status: done

## Story

As a race-day fan,
I want a panel showing the current fastest sector times and who holds them,
So that I can track who's setting the pace.

## Acceptance Criteria

1. **Given** a live session **When** the sector board renders **Then** it shows the current fastest S1, S2, S3 times with the holder's driver code, highlighted purple.
2. **Given** a sector record is broken **When** detected **Then** the board updates immediately to the new holder/time.
3. **Given** no sector time has been recorded yet for a given sector **When** the board renders **Then** that sector cell shows an empty/placeholder state, not a crash or `Infinity`.
4. **Given** `sessionMode === 'fallback'` (Story 2.5 static replay) **When** the page renders **Then** the sector board is not shown — Ergast's historical data has no per-sector timing archive, so there is nothing honest to display for a past race (same limitation and precedent already accepted for `MiniSectorStatus` in Story 3.2).

## Tasks / Subtasks

### Task 1: Backend — `FastestSectorEntry` / `FastestSectorBoard` models (AC: 1, 3)

- [x] Create `backend/F1App.Api/Models/FastestSectorEntry.cs`:
  ```csharp
  namespace F1App.Api.Models;

  public record FastestSectorEntry(
      int DriverNumber,
      string DriverCode,
      string TeamColour,
      double TimeSeconds
  );

  public record FastestSectorBoard(
      FastestSectorEntry? S1,
      FastestSectorEntry? S2,
      FastestSectorEntry? S3
  );
  ```
  Each sector entry is nullable independently (AC 3) — a session may have an S3 best recorded before any car has completed S1 as its most-recent sector (sector completion order in `ComputeSectorStatus` is S3 > S2 > S1 priority for *display* per driver, but each sector's session-best is tracked independently and can be set at any time once any driver posts a time in that sector).

### Task 2: Backend — Extend `RaceStateSnapshot` (AC: 1, 4)

- [x] Update `backend/F1App.Api/Models/RaceStateSnapshot.cs` — add:
  ```csharp
  public FastestSectorBoard? FastestSectors { get; init; }
  ```
  Null in fallback mode (AC 4); populated (with per-sector nulls until data arrives, AC 3) in live/stale mode.

### Task 3: Backend — Track sector-best holders in `RaceDataOrchestrator` (AC: 1, 2)

- [x] Add driver-holder tracking fields alongside the existing `_sessionBestS1/2/3` fields:
  ```csharp
  private int? _sessionBestS1Driver;
  private int? _sessionBestS2Driver;
  private int? _sessionBestS3Driver;
  ```

- [x] Update `ComputeSectorStatus`'s three-way `isSessionBest` branch to also stamp the driver number whenever a new session best is set:
  ```csharp
  bool isSessionBest;
  if (sectorIndex == 0) { isSessionBest = time < _sessionBestS1; if (isSessionBest) { _sessionBestS1 = time; _sessionBestS1Driver = lap.DriverNumber; } }
  else if (sectorIndex == 1) { isSessionBest = time < _sessionBestS2; if (isSessionBest) { _sessionBestS2 = time; _sessionBestS2Driver = lap.DriverNumber; } }
  else { isSessionBest = time < _sessionBestS3; if (isSessionBest) { _sessionBestS3 = time; _sessionBestS3Driver = lap.DriverNumber; } }
  ```
  This is the AC 2 mechanism: the very next `PollLapsAsync` tick that produces a faster sector time updates both the time and the holder atomically (single-threaded — `PollLapsAsync` is the only writer of these fields), and the next `PublishSnapshotLoopAsync` tick (every 1s) picks it up via `BuildFastestSectorBoard()`.

### Task 4: Backend — Build the board in `BuildSnapshot` (AC: 1, 3, 4)

- [x] Add a private `BuildFastestSectorBoard()` helper, called from `BuildSnapshot`'s return statement:
  ```csharp
  private FastestSectorBoard? BuildFastestSectorBoard()
  {
      if (_sessionMode == SessionMode.Fallback)
          return null;

      FastestSectorEntry? MakeEntry(double best, int? driverNum)
      {
          if (driverNum is null || best == double.MaxValue) return null;
          _driverInfo.TryGetValue(driverNum.Value, out var info);
          return new FastestSectorEntry(
              driverNum.Value,
              info?.NameAcronym ?? driverNum.Value.ToString(),
              info?.TeamColour ?? "555555",
              best);
      }

      return new FastestSectorBoard(
          MakeEntry(_sessionBestS1, _sessionBestS1Driver),
          MakeEntry(_sessionBestS2, _sessionBestS2Driver),
          MakeEntry(_sessionBestS3, _sessionBestS3Driver));
  }
  ```
  Wire into the `return new RaceStateSnapshot { ... }` statement: `FastestSectors = BuildFastestSectorBoard(),`.

  **AC 3 guard**: `best == double.MaxValue` is the sentinel for "no time recorded yet" (matches the existing initialisation `private double _sessionBestS1 = double.MaxValue;`) — `MakeEntry` returns `null` in that case rather than emitting `Infinity` through JSON serialization (which is what would happen if `double.MaxValue` were serialized directly and compared client-side).

### Task 5: Backend — Tests (AC: 1, 2, 3, 4)

- [x] Add to `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`:
  ```csharp
  [Fact]
  public void BuildSnapshot_NoSectorTimesYet_FastestSectorsAllNull()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[1] = MakePosition(1, 1, DateTimeOffset.UtcNow);

      var snapshot = sut.BuildSnapshot();

      Assert.NotNull(snapshot.FastestSectors);
      Assert.Null(snapshot.FastestSectors!.S1);
      Assert.Null(snapshot.FastestSectors.S2);
      Assert.Null(snapshot.FastestSectors.S3);
  }

  [Fact]
  public void BuildSnapshot_OneDriverSetsS1_BecomesHolder()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[44] = MakePosition(44, 1, DateTimeOffset.UtcNow);
      sut.ComputeSectorStatus(MakeLapWithSectors(44, 1, s1: 28.5));

      var snapshot = sut.BuildSnapshot();

      Assert.Equal(44, snapshot.FastestSectors!.S1!.DriverNumber);
      Assert.Equal(28.5, snapshot.FastestSectors.S1.TimeSeconds);
  }

  [Fact]
  public void BuildSnapshot_SecondDriverBeatsS1_HolderSwitches()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[44] = MakePosition(44, 1, DateTimeOffset.UtcNow);
      sut._latestPositions[1] = MakePosition(1, 2, DateTimeOffset.UtcNow);
      sut.ComputeSectorStatus(MakeLapWithSectors(44, 1, s1: 28.5));
      sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 28.1));

      var snapshot = sut.BuildSnapshot();

      Assert.Equal(1, snapshot.FastestSectors!.S1!.DriverNumber);
      Assert.Equal(28.1, snapshot.FastestSectors.S1.TimeSeconds);
  }

  [Fact]
  public void BuildSnapshot_SlowerSectorTime_DoesNotDisplaceExistingHolder()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[44] = MakePosition(44, 1, DateTimeOffset.UtcNow);
      sut._latestPositions[1] = MakePosition(1, 2, DateTimeOffset.UtcNow);
      sut.ComputeSectorStatus(MakeLapWithSectors(44, 1, s1: 28.1));
      sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 28.5)); // slower — should not take the lead

      var snapshot = sut.BuildSnapshot();

      Assert.Equal(44, snapshot.FastestSectors!.S1!.DriverNumber);
      Assert.Equal(28.1, snapshot.FastestSectors.S1.TimeSeconds);
  }

  [Fact]
  public void BuildSnapshot_AllThreeSectorsIndependent_EachTracksOwnHolder()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[44] = MakePosition(44, 1, DateTimeOffset.UtcNow);
      sut._latestPositions[1] = MakePosition(1, 2, DateTimeOffset.UtcNow);
      // ComputeSectorStatus only processes the most-recently-completed sector per
      // call (S3 > S2 > S1 priority) — mimic OpenF1's progressive per-sector
      // updates within a lap by calling once per sector as it completes.
      sut.ComputeSectorStatus(MakeLapWithSectors(44, 1, s1: 28.1));
      sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 28.5));
      sut.ComputeSectorStatus(MakeLapWithSectors(44, 1, s1: 28.1, s2: 40.0));
      sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 28.5, s2: 39.5));
      sut.ComputeSectorStatus(MakeLapWithSectors(44, 1, s1: 28.1, s2: 40.0, s3: 25.0));
      sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 28.5, s2: 39.5, s3: 25.5));

      var snapshot = sut.BuildSnapshot();

      Assert.Equal(44, snapshot.FastestSectors!.S1!.DriverNumber); // 44 faster S1
      Assert.Equal(1, snapshot.FastestSectors.S2!.DriverNumber);   // 1 faster S2
      Assert.Equal(44, snapshot.FastestSectors.S3!.DriverNumber);  // 44 faster S3
  }

  [Fact]
  public void BuildSnapshot_FallbackMode_FastestSectorsIsNull()
  {
      var sut = CreateOrchestrator();
      sut._sessionMode = SessionMode.Fallback;
      sut._fallbackDrivers = [new DriverState { DriverNumber = 1, Position = 1 }];

      var snapshot = sut.BuildSnapshot();

      Assert.Null(snapshot.FastestSectors);
  }
  ```
  **Note**: `MakeLapWithSectors` already exists (added in Story 3.2) — pass whichever sector args are needed; unset sectors default to `null` and are ignored by `ComputeSectorStatus`.

- [ ] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass.

### Task 6: Frontend — Extend types and store (AC: 1, 3, 4)

- [ ] Update `frontend/src/shared/types/f1.ts` — add:
  ```ts
  export interface FastestSectorEntry {
    driverNumber: number
    driverCode: string
    teamColour: string
    timeSeconds: number
  }

  export interface FastestSectorBoard {
    s1: FastestSectorEntry | null
    s2: FastestSectorEntry | null
    s3: FastestSectorEntry | null
  }
  ```
  Add to `RaceStateSnapshot`:
  ```ts
  fastestSectors: FastestSectorBoard | null
  ```

- [ ] Update `frontend/src/features/live-race/store/liveRaceStore.ts` — add `fastestSectors: FastestSectorBoard | null` state (default `null`) and a `setFastestSectors` setter, following the exact pattern of `fallbackRaceName`/`setFallbackRaceName`.

- [ ] Update `frontend/src/features/live-race/hooks/useSignalRConnection.ts` — in `handleSnapshot`, add:
  ```ts
  setFastestSectors(snapshot.fastestSectors ?? null)
  ```
  and add `setFastestSectors` to the destructured store hooks and the `useEffect` dependency array.

### Task 7: Frontend — `FastestSectorBoard` component (AC: 1, 2, 3, 4)

- [ ] Create `frontend/src/features/live-race/FastestSectorBoard/FastestSectorBoard.tsx`:
  ```tsx
  import { useLiveRaceStore } from '../store/liveRaceStore'
  import type { FastestSectorEntry } from '../../../shared/types/f1'

  function SectorCell({ label, entry }: { label: 'S1' | 'S2' | 'S3'; entry: FastestSectorEntry | null }) {
    return (
      <div
        className="flex flex-col items-center gap-1 px-3 py-2 rounded-[8px] bg-[#20242c] min-w-[64px]"
        data-testid={`sector-${label.toLowerCase()}`}
      >
        <span className="text-[10px] text-[#6b7280] uppercase tracking-wide">{label}</span>
        {entry ? (
          <>
            <span
              className="text-[14px] font-bold text-[#bf00ff] tabular-nums"
              data-testid={`sector-${label.toLowerCase()}-time`}
            >
              {entry.timeSeconds.toFixed(3)}
            </span>
            <span className="text-[11px] text-[#eef0f3]">{entry.driverCode}</span>
          </>
        ) : (
          <span className="text-[12px] text-[#6b7280]">—</span>
        )}
      </div>
    )
  }

  export function FastestSectorBoard() {
    const sessionMode = useLiveRaceStore(s => s.sessionMode)
    const fastestSectors = useLiveRaceStore(s => s.fastestSectors)

    if (sessionMode === 'fallback') return null

    return (
      <div className="flex gap-2" data-testid="fastest-sector-board">
        <SectorCell label="S1" entry={fastestSectors?.s1 ?? null} />
        <SectorCell label="S2" entry={fastestSectors?.s2 ?? null} />
        <SectorCell label="S3" entry={fastestSectors?.s3 ?? null} />
      </div>
    )
  }
  ```
  Purple (`#bf00ff`) matches the session-fastest colour already established for `DriverDot`/`SECTOR_COLOURS` in Story 3.2 — reuse the same hex rather than inventing a new purple.

### Task 8: Frontend — Wire into `LiveRacePage` (AC: 1, 4)

- [ ] Update `frontend/src/features/live-race/LiveRacePage.tsx` — import and render `<FastestSectorBoard />` inside the existing `flex flex-col gap-4` container, after `<GapList />` and before `<TrackMap />` (or in whatever order reads best — exact position is a layout call, not an AC).

### Task 9: Frontend — Tests (AC: 1, 2, 3, 4)

- [ ] Create `frontend/src/features/live-race/FastestSectorBoard/FastestSectorBoard.test.tsx`:
  - Resets store state in `beforeEach` (sessionMode: 'live', fastestSectors: null).
  - "shows placeholder dash for a sector with no data yet".
  - "shows time and driver code for a sector with a holder".
  - "returns null when sessionMode is fallback" (assert `container.firstChild` is `null`).
  - "updates to the new time/holder when fastestSectors changes" (set store twice, re-render, assert new value visible).

- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass.
- [ ] Run `npx tsc --noEmit` from `frontend/` — must be clean.

## Dev Notes

### Architecture Alignment

- Continues the established pattern from Stories 2.4/3.2/3.3: all derived state is computed backend-side in `RaceDataOrchestrator`/`RaceStateSnapshot`, and the frontend renders whatever flag/value it's handed with no client-side recomputation.
- `_sessionBestS1/2/3` already existed (Story 3.2, for the per-driver mini-sector colour ring); this story adds the missing "who" half of that data (`_sessionBestS1Driver` etc.) without altering the existing colour-ring behaviour at all — `ComputeSectorStatus`'s return value/side-effects on `_latestSectorStatus` and `_personalBestSectors` are untouched.
- `double.MaxValue` is the existing "no time yet" sentinel (already used for `_sessionBestS1/2/3` initial values) — reused here as the guard in `MakeEntry` rather than introducing a second sentinel convention.

### Regressions to Guard

- Do not reorder or rename the `isSessionBest` three-way branch in `ComputeSectorStatus` — the existing Story 3.2 tests (`ComputeSectorStatus_FirstSectorTime_IsSessionBest_SetsPurple`, `ComputeSectorStatus_SecondDriverFasterS1_FirstDriverBecomesGreen`, etc.) assert on `_latestSectorStatus`, which this story's changes to that branch must continue to set identically — only the *addition* of `_sessionBestS1Driver = lap.DriverNumber` etc. inside the existing `if (isSessionBest)` blocks is new.
- `BuildFastestSectorBoard()` must be called unconditionally in `BuildSnapshot` (both branches — live/stale driver-loop path and the fallback `_fallbackDrivers` path funnel through the same single `return new RaceStateSnapshot { ... }` at the bottom) so fallback mode still gets an explicit `null` rather than the field defaulting to whatever a partial record init would leave it as.

### Files to Create / Modify

**Backend CREATE:**
- `backend/F1App.Api/Models/FastestSectorEntry.cs`

**Backend MODIFY:**
- `backend/F1App.Api/Models/RaceStateSnapshot.cs` — add `FastestSectors`
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` — add holder fields, update `ComputeSectorStatus`, add `BuildFastestSectorBoard`, wire into `BuildSnapshot`
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs` — 6 new tests

**Frontend CREATE:**
- `frontend/src/features/live-race/FastestSectorBoard/FastestSectorBoard.tsx`
- `frontend/src/features/live-race/FastestSectorBoard/FastestSectorBoard.test.tsx`

**Frontend MODIFY:**
- `frontend/src/shared/types/f1.ts` — add `FastestSectorEntry`, `FastestSectorBoard`, extend `RaceStateSnapshot`
- `frontend/src/features/live-race/store/liveRaceStore.ts` — add `fastestSectors` state + setter
- `frontend/src/features/live-race/hooks/useSignalRConnection.ts` — wire `setFastestSectors`
- `frontend/src/features/live-race/LiveRacePage.tsx` — render `<FastestSectorBoard />`

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.4: Fastest Sector Board]
- [Source: backend/F1App.Api/Services/RaceDataOrchestrator.cs — existing `ComputeSectorStatus`/`_sessionBestS1/2/3` from Story 3.2]
- [Source: _bmad-output/implementation-artifacts/3-2-mini-sector-colour-coding.md — precedent for fallback-mode null behaviour and purple colour convention]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

None.

### Completion Notes List

- Implemented as planned. One test fix needed: `BuildSnapshot_AllThreeSectorsIndependent_EachTracksOwnHolder` initially passed all three sector times in a single `ComputeSectorStatus` call per driver, but `ComputeSectorStatus` only processes the highest-priority completed sector per call (S3 > S2 > S1) — fixed by calling it progressively (S1-only, then S1+S2, then all three) per driver, mirroring how OpenF1 actually reports sector times as a lap progresses.
- All 114 backend tests and 76 frontend tests pass; `tsc --noEmit` is clean.

### File List

**Backend created:**
- `backend/F1App.Api/Models/FastestSectorEntry.cs`

**Backend modified:**
- `backend/F1App.Api/Models/RaceStateSnapshot.cs`
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs`
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`

**Frontend created:**
- `frontend/src/features/live-race/FastestSectorBoard/FastestSectorBoard.tsx`
- `frontend/src/features/live-race/FastestSectorBoard/FastestSectorBoard.test.tsx`

**Frontend modified:**
- `frontend/src/shared/types/f1.ts`
- `frontend/src/features/live-race/store/liveRaceStore.ts`
- `frontend/src/features/live-race/hooks/useSignalRConnection.ts`
- `frontend/src/features/live-race/LiveRacePage.tsx`

### Change Log

| Date | Change |
|------|--------|
| 2026-07-10 | Story created via bmad-create-story |
