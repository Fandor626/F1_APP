---
baseline_commit: 15d4955a2404a6aee13fcf957ad8927e845ef7f3
---

# Story 2.2: Live Tyre Tracker

Status: done

## Story

As a race-day fan,
I want to see each driver's current tyre compound and stint length in the gap list,
So that I understand their strategy in real time.

## Acceptance Criteria

1. **Given** the gap list **When** a driver entry renders **Then** it shows the current Tyre Compound as a colour-coded circle and the number of laps on the current Stint (both null/hidden when no tyre data has arrived yet).
2. **Given** new tyre data arrives from OpenF1's `/stints` endpoint **When** the snapshot is assembled **Then** `TyreCompound` and `StintLaps` in `DriverState` update accordingly and the frontend re-renders within the next snapshot cycle.

## Tasks / Subtasks

### Task 1: New DTOs — OpenF1 stints and laps (AC: 1, 2)

- [x] Create `backend/F1App.Api/Dtos/OpenF1/OpenF1StintDto.cs`:
  ```csharp
  // OpenF1 /stints JSON shape (snake_case): driver_number, stint_number,
  // lap_start, lap_end (null = ongoing), compound, tyre_age_at_start
  public record OpenF1StintDto(
      [property: JsonPropertyName("driver_number")] int DriverNumber,
      [property: JsonPropertyName("stint_number")] int StintNumber,
      [property: JsonPropertyName("lap_start")] int LapStart,
      [property: JsonPropertyName("lap_end")] int? LapEnd,
      [property: JsonPropertyName("compound")] string Compound,
      [property: JsonPropertyName("tyre_age_at_start")] int TyreAgeAtStart
  );
  ```
- [x] Create `backend/F1App.Api/Dtos/OpenF1/OpenF1LapDto.cs`:
  ```csharp
  // Minimal shape for lap-number tracking (Story 2.3 will extend this)
  // OpenF1 /laps uses date_start (not date) for its timestamp field
  public record OpenF1LapDto(
      [property: JsonPropertyName("driver_number")] int DriverNumber,
      [property: JsonPropertyName("lap_number")] int LapNumber,
      [property: JsonPropertyName("date_start")] DateTimeOffset DateStart
  );
  ```

### Task 2: Extend OpenF1 client (AC: 2)

- [x] Add `GetLatestStintsAsync` and `GetLatestLapsAsync` to `IOpenF1Client.cs`:
  ```csharp
  // Full refresh — stints don't expose a date filter; max 80 records per race
  Task<IReadOnlyList<OpenF1StintDto>> GetLatestStintsAsync(CancellationToken ct);
  // Incremental — uses date_start (not date!) as the filter field
  Task<IReadOnlyList<OpenF1LapDto>> GetLatestLapsAsync(DateTimeOffset since, CancellationToken ct);
  ```
- [x] Implement both methods in `OpenF1Client.cs`:
  ```csharp
  public async Task<IReadOnlyList<OpenF1StintDto>> GetLatestStintsAsync(CancellationToken ct)
      => await httpClient.GetFromJsonAsync<IReadOnlyList<OpenF1StintDto>>("stints?session_key=latest", ct) ?? [];

  public async Task<IReadOnlyList<OpenF1LapDto>> GetLatestLapsAsync(DateTimeOffset since, CancellationToken ct)
  {
      var url = since == DateTimeOffset.MinValue
          ? "laps?session_key=latest"
          : $"laps?session_key=latest&date_start>{since:yyyy-MM-ddTHH:mm:ss.fff}";
      return await httpClient.GetFromJsonAsync<IReadOnlyList<OpenF1LapDto>>(url, ct) ?? [];
  }
  ```
  **Critical**: the laps endpoint filter is `date_start>` (not `date>`) — the field is named differently from `/position` and `/intervals`.

### Task 3: Add stints + laps polling loops to RaceDataOrchestrator (AC: 2)

- [x] Add new `internal` fields to `RaceDataOrchestrator`:
  ```csharp
  // Latest stint per driver (by highest StintNumber) — full refresh each poll
  internal readonly ConcurrentDictionary<int, OpenF1StintDto> _latestStints = new();
  // Maximum lap_number seen per driver — only ever increases
  internal readonly ConcurrentDictionary<int, int> _driverCurrentLap = new();
  private DateTimeOffset _lastLapPoll = DateTimeOffset.MinValue;
  ```
- [x] Add `PollStintsAsync` (10s timer — stints change only on pit stops):
  ```csharp
  private async Task PollStintsAsync(CancellationToken ct)
  {
      var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
      while (await timer.WaitForNextTickAsync(ct))
      {
          var stints = await openF1Client.GetLatestStintsAsync(ct);
          // For each driver, keep only the stint with the highest StintNumber
          foreach (var stint in stints)
          {
              _latestStints.AddOrUpdate(
                  stint.DriverNumber,
                  stint,
                  (_, existing) => stint.StintNumber > existing.StintNumber ? stint : existing);
          }
      }
  }
  ```
- [x] Add `PollLapsAsync` (5s timer — laps complete roughly every 90s so 5s gives good resolution):
  ```csharp
  private async Task PollLapsAsync(CancellationToken ct)
  {
      var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
      while (await timer.WaitForNextTickAsync(ct))
      {
          var laps = await openF1Client.GetLatestLapsAsync(_lastLapPoll, ct);
          if (laps.Count > 0)
              _lastLapPoll = timeProvider.GetUtcNow();

          foreach (var lap in laps)
          {
              _driverCurrentLap.AddOrUpdate(
                  lap.DriverNumber,
                  lap.LapNumber,
                  (_, existing) => lap.LapNumber > existing ? lap.LapNumber : existing);
          }
      }
  }
  ```
- [x] Add both to `ExecuteAsync`'s `Task.WhenAll`:
  ```csharp
  await Task.WhenAll(
      RunLoopAsync("PositionPoller",  PollPositionAsync,       stoppingToken),
      RunLoopAsync("IntervalPoller",  PollIntervalAsync,       stoppingToken),
      RunLoopAsync("StintsPoller",    PollStintsAsync,         stoppingToken),
      RunLoopAsync("LapsPoller",      PollLapsAsync,           stoppingToken),
      RunLoopAsync("PublishLoop",     PublishSnapshotLoopAsync, stoppingToken)
  );
  ```

### Task 4: Populate TyreCompound and StintLaps in BuildSnapshot (AC: 1, 2)

- [x] In `BuildSnapshot()`, after resolving the gap fields and before calling `drivers.Add(...)`, look up stint and lap data:
  ```csharp
  string? tyreCompound = null;
  int? stintLaps = null;

  if (_latestStints.TryGetValue(driverNum, out var stint))
  {
      tyreCompound = stint.Compound;
      if (_driverCurrentLap.TryGetValue(driverNum, out var currentLap))
      {
          // Total tyre age = age when this set was new + laps completed in this stint
          // currentLap - stint.LapStart + 1 = laps completed in this stint (1-indexed)
          stintLaps = stint.TyreAgeAtStart + Math.Max(0, currentLap - stint.LapStart + 1);
      }
  }
  ```
- [x] Pass `TyreCompound = tyreCompound, StintLaps = stintLaps` into the `new DriverState { ... }` initialiser. These fields already exist on `DriverState` from Story 2.1 as nullable placeholders — no DriverState changes needed.
- [x] Verify the `StintLaps` arithmetic on paper:
  - Stint 1 started lap 1 (LapStart=1), current lap 5: `0 + Max(0, 5-1+1)` = 5 ✓
  - After pitstop: Stint 2 started lap 23 (LapStart=23), TyreAgeAtStart=0, current lap 23: `0 + Max(0, 23-23+1)` = 1 ✓
  - Current lap 35: `0 + Max(0, 35-23+1)` = 13 ✓
  - Used tyres (TyreAgeAtStart=5): lap 3 of stint: `5 + Max(0, 3-1+1)` = 8 ✓

### Task 5: Backend unit tests for stints/laps snapshot assembly (AC: 1, 2)

- [x] In `RaceDataOrchestratorTests.cs`, add 4 new facts using the existing `CreateOrchestrator()` factory (no changes to factory needed):
  ```csharp
  // Helper
  private static OpenF1StintDto MakeStint(int driverNum, int stintNum, int lapStart,
      string compound, int tyreAgeAtStart = 0) =>
      new(driverNum, stintNum, lapStart, null, compound, tyreAgeAtStart);

  [Fact]
  public void BuildSnapshot_WithStintData_PopulatesTyreCompound()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      sut._latestStints[33] = MakeStint(33, 1, 1, "SOFT");

      var snapshot = sut.BuildSnapshot();

      Assert.Equal("SOFT", snapshot.Drivers[0].TyreCompound);
  }

  [Fact]
  public void BuildSnapshot_WithStintAndLapData_PopulatesStintLaps()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      sut._latestStints[33] = MakeStint(33, 2, 23, "MEDIUM", tyreAgeAtStart: 0);
      sut._driverCurrentLap[33] = 35;

      var snapshot = sut.BuildSnapshot();

      // 0 + Max(0, 35 - 23 + 1) = 13
      Assert.Equal(13, snapshot.Drivers[0].StintLaps);
  }

  [Fact]
  public void BuildSnapshot_NoStintData_TyreFieldsNull()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);

      var snapshot = sut.BuildSnapshot();

      Assert.Null(snapshot.Drivers[0].TyreCompound);
      Assert.Null(snapshot.Drivers[0].StintLaps);
  }

  [Fact]
  public void BuildSnapshot_StintWithNoLapData_StintLapsNull()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      sut._latestStints[33] = MakeStint(33, 1, 1, "HARD");
      // _driverCurrentLap NOT set

      var snapshot = sut.BuildSnapshot();

      Assert.Equal("HARD", snapshot.Drivers[0].TyreCompound);
      Assert.Null(snapshot.Drivers[0].StintLaps);  // No lap data yet
  }
  ```

### Task 6: Frontend tyreUtils.ts (AC: 1)

- [x] Create `frontend/src/shared/utils/tyreUtils.ts`:
  ```ts
  // Standard F1 tyre compound colours (Pirelli official palette)
  export const TYRE_COLOURS: Record<string, string> = {
    SOFT: '#E8002D',
    MEDIUM: '#FFF200',
    HARD: '#FFFFFF',
    INTERMEDIATE: '#39B54A',
    WET: '#0067FF',
  }

  // Single-letter abbreviations for compact display
  export const TYRE_ABBREVIATIONS: Record<string, string> = {
    SOFT: 'S',
    MEDIUM: 'M',
    HARD: 'H',
    INTERMEDIATE: 'I',
    WET: 'W',
  }

  export function getTyreColour(compound: string | null): string {
    if (!compound) return '#6b7280'
    return TYRE_COLOURS[compound.toUpperCase()] ?? '#6b7280'
  }

  export function getTyreAbbreviation(compound: string | null): string {
    if (!compound) return '?'
    return TYRE_ABBREVIATIONS[compound.toUpperCase()] ?? compound[0].toUpperCase()
  }
  ```

### Task 7: Update DriverRow.tsx to render tyre data (AC: 1)

- [x] Import `getTyreColour` and `getTyreAbbreviation` from `../../../shared/utils/tyreUtils`:
- [x] Add the tyre compound circle and stint laps between driver code and gap. The full updated JSX return:
  ```tsx
  return (
    <div className="flex items-center gap-3 px-[10px] py-[6px] text-[12px] hover:bg-[#20242c] cursor-default font-[Avenir_Next,sans-serif]">
      <span className="w-5 text-right text-[#6b7280] shrink-0">
        {driver.position}
      </span>
      <span
        className="shrink-0 rounded-full"
        style={{ width: 7, height: 7, backgroundColor: `#${driver.teamColour}` }}
      />
      <span className="w-8 font-semibold text-[#eef0f3] shrink-0">
        {driver.driverCode}
      </span>
      {driver.tyreCompound !== null ? (
        <span className="flex items-center gap-1 shrink-0">
          <span
            className="rounded-full shrink-0"
            style={{
              width: 10,
              height: 10,
              backgroundColor: getTyreColour(driver.tyreCompound),
            }}
            data-testid="tyre-compound"
            aria-label={driver.tyreCompound}
          />
          {driver.stintLaps !== null && (
            <span className="text-[#9aa1ad] tabular-nums" data-testid="stint-laps">
              {driver.stintLaps}
            </span>
          )}
        </span>
      ) : null}
      <span className="ml-auto tabular-nums">{gapDisplay}</span>
    </div>
  )
  ```
  **Layout note**: Tyre circle + lap count sit between driver code and gap. When no tyre data exists, `null` renders nothing — the gap shifts left. This matches F1 TV's compact compound indicator style.

### Task 8: Add tyre tests to GapList.test.tsx (AC: 1, 2)

- [x] Add tyre-specific tests at the end of the `describe('GapList')` block:
  ```tsx
  it('shows tyre compound circle when compound data is present', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { tyreCompound: 'SOFT' }),
      },
    })
    render(<GapList />)
    const tyreCircle = screen.getByTestId('tyre-compound')
    expect(tyreCircle).toBeInTheDocument()
    // SOFT = #E8002D red
    expect(tyreCircle).toHaveStyle({ backgroundColor: 'rgb(232, 0, 45)' })
  })

  it('shows stint laps when both compound and lap data are present', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { tyreCompound: 'MEDIUM', stintLaps: 12 }),
      },
    })
    render(<GapList />)
    expect(screen.getByTestId('stint-laps').textContent).toBe('12')
  })

  it('hides tyre section when compound is null', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { tyreCompound: null, stintLaps: null }),
      },
    })
    render(<GapList />)
    expect(screen.queryByTestId('tyre-compound')).not.toBeInTheDocument()
    expect(screen.queryByTestId('stint-laps')).not.toBeInTheDocument()
  })

  it('hides stint laps when lap count is null even if compound is set', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { tyreCompound: 'HARD', stintLaps: null }),
      },
    })
    render(<GapList />)
    expect(screen.getByTestId('tyre-compound')).toBeInTheDocument()
    expect(screen.queryByTestId('stint-laps')).not.toBeInTheDocument()
  })
  ```

## Dev Notes

### OpenF1 API Details

**`/stints` endpoint**
- URL: `stints?session_key=latest`
- No incremental date filter (stints data doesn't expose a `date` field in the same way as position/intervals). Full refresh every 10 seconds is safe — a race has at most ~80 stints total.
- JSON shape:
  ```json
  {
    "driver_number": 1,
    "meeting_key": 1219,
    "session_key": 9159,
    "stint_number": 2,
    "lap_start": 20,
    "lap_end": null,
    "compound": "MEDIUM",
    "tyre_age_at_start": 3
  }
  ```
  `compound` values: `"SOFT"`, `"MEDIUM"`, `"HARD"`, `"INTERMEDIATE"`, `"WET"`
  `lap_end`: `null` while stint is ongoing; set to the last lap when the stint ends.
  `tyre_age_at_start`: how many laps this set had on it before this stint started (0 for new tyres).

**`/laps` endpoint**
- URL (full): `laps?session_key=latest`
- URL (incremental): `laps?session_key=latest&date_start>{since:yyyy-MM-ddTHH:mm:ss.fff}`
- **CRITICAL**: Uses `date_start>` NOT `date>` — unlike `/position` and `/intervals` which use `date>`. This is a different field name on OpenF1's side.
- Returns one record per driver per completed lap. A 70-lap race produces ~1400 records total; incremental polling after the first fetch keeps payloads small.
- We only need `driver_number` and `lap_number` for Story 2.2. `date_start` is the filter field.

### StintLaps Calculation

`StintLaps` = total tyre age = how many laps this specific set of tyres has been driven (including previous stints for used tyres):

```
StintLaps = TyreAgeAtStart + Max(0, currentLap - LapStart + 1)
```

Where:
- `TyreAgeAtStart` from stints DTO (0 for new, >0 for used tyres)
- `currentLap` from `_driverCurrentLap` (max lap_number ever seen for this driver)
- `LapStart` from stints DTO (first lap of this stint)

This is the standard F1 tyre age metric (e.g., "SOFT • 12" means 12 total laps on this set).

`StintLaps` is `null` when we have stint data but no lap data yet. This happens at race start before the first lap completes — which is correct since the tyre age should show 0 until lap 1 is done.

### Architecture

**No new orchestrator shape changes**: We add 2 new loops to the existing `RunLoopAsync` + `Task.WhenAll` pattern. This keeps the 5-loop architecture designed in Story 2.1 coherent. The `ExecuteAsync` call now has 5 parallel tasks (position, interval, stints, laps, publish).

**`ConcurrentDictionary` for stints**: Same pattern as `_latestPositions` and `_latestIntervals`. The `AddOrUpdate` in `PollStintsAsync` keeps only the highest `StintNumber` per driver (which is the current/latest stint).

**No `InternalsVisibleTo` changes**: `_latestStints` and `_driverCurrentLap` need to be `internal` (not `private`) for the test-direct-mutation pattern used in `RaceDataOrchestratorTests.cs`. The `[assembly: InternalsVisibleTo("F1App.Api.Tests")]` attribute is already set in `Program.cs` from Story 2.1.

**Existing test factory still works**: `CreateOrchestrator()` uses a mock `IOpenF1Client`. Tests directly mutate `_latestStints` and `_driverCurrentLap` via the `internal` accessibility — no mock setup needed for these fields.

### Frontend State Shape

`DriverState.tyreCompound` and `DriverState.stintLaps` are **already declared** in:
- `frontend/src/shared/types/f1.ts` (TypeScript interface)
- `frontend/src/features/live-race/GapList/GapList.test.tsx` (test `makeDriver` helper)
- `frontend/src/shared/utils/normalizeSnapshot.test.ts` (test helper)

All these already have `tyreCompound: null, stintLaps: null` placeholders from Story 2.1. Story 2.2 only adds the DISPLAY logic — no type changes needed.

### Tyre Colours

Pirelli official compound colours used by F1 TV and official apps:
| Compound | Colour | Hex |
|---|---|---|
| SOFT | Red | `#E8002D` |
| MEDIUM | Yellow | `#FFF200` |
| HARD | White | `#FFFFFF` |
| INTERMEDIATE | Green | `#39B54A` |
| WET | Blue | `#0067FF` |

HARD tyres render white on the dark background (`#1a1d24`) — this is intentional and matches F1 broadcast style. A 1px grey border isn't needed at 10px circle size; the contrast is adequate.

### Files Changed

**Backend (4 new, 3 modified)**
- `backend/F1App.Api/Dtos/OpenF1/OpenF1StintDto.cs` — NEW
- `backend/F1App.Api/Dtos/OpenF1/OpenF1LapDto.cs` — NEW
- `backend/F1App.Api/Clients/IOpenF1Client.cs` — add `GetLatestStintsAsync`, `GetLatestLapsAsync`
- `backend/F1App.Api/Clients/OpenF1Client.cs` — implement both
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` — add stints+laps loops, populate `TyreCompound`/`StintLaps` in `BuildSnapshot`
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs` — add 4 facts + `MakeStint` helper

**Frontend (1 new, 2 modified)**
- `frontend/src/shared/utils/tyreUtils.ts` — NEW
- `frontend/src/features/live-race/GapList/DriverRow.tsx` — render tyre circle + stint laps
- `frontend/src/features/live-race/GapList/GapList.test.tsx` — add 4 tyre tests

### Test Commands

```bash
# Backend tests (from repo root)
dotnet test backend/F1App.Api.Tests/

# Frontend tests (from frontend/)
npm test
# or for watch mode:
npm run test -- --watch
```

### Regressions to Guard

The following behaviors from Story 2.1 must NOT change:
- `GapIsStale` logic and `~` prefix render — no changes to gap calculation
- `gapDisplay` branching in `DriverRow.tsx` — tyre section is added BEFORE the gap, not modifying it
- Connection status banner (Live/Reconnecting/Disconnected) in `GapList.tsx` — untouched
- `BuildSnapshot` gap/stale join logic — only the final `drivers.Add(...)` call gets two more fields
- Existing `RaceDataOrchestratorTests.cs` facts — all must still pass (new facts are additive)
- `normalizeSnapshot` — untouched; already handles `tyreCompound`/`stintLaps` as passthrough

### Project Structure Notes

- `OpenF1StintDto.cs` and `OpenF1LapDto.cs` go in `backend/F1App.Api/Dtos/OpenF1/` alongside the three existing DTOs — consistent with the `*Dto` naming and folder rule
- `tyreUtils.ts` goes in `frontend/src/shared/utils/` — it's a pure function with no React dependency and is referenced by `DriverRow` (feature-specific) but may be reused by `PitWindowIndicator` (Story 3.3) and `TrackMap` (Story 3.1). Architecture mandates: pure utils with multi-feature potential live in `shared/utils/`
- No new store slices — `tyreCompound` and `stintLaps` flow through the existing `DriverState` type and `drivers` Zustand slice

### References

- OpenF1 `/stints` endpoint: [Source: architecture.md#Real-Time Architecture]
- `RaceDataOrchestrator` 5-loop pattern: [Source: architecture.md#Real-Time Architecture]
- `ConcurrentDictionary.AddOrUpdate` for latest-per-driver: [Source: 2-1-live-gap-list.md Task 5]
- `InternalsVisibleTo` and `internal` field test pattern: [Source: 2-1-live-gap-list.md Task 7]
- `tyreUtils.ts` location in `shared/utils/`: [Source: architecture.md#Structure Patterns]
- `DateTimeOffset` mandatory throughout C#: [Source: architecture.md#Enforcement Summary point 5]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

No blockers encountered. All tasks implemented in sequence without issues.

### Completion Notes List

- Task 1: Created `OpenF1StintDto` and `OpenF1LapDto` C# records matching OpenF1 JSON shape. Notable: laps endpoint uses `date_start` (not `date`) as timestamp field name, unlike position/intervals.
- Task 2: Extended `IOpenF1Client` with `GetLatestStintsAsync` (no date filter, full refresh) and `GetLatestLapsAsync` (incremental, `date_start>` filter). Implemented in `OpenF1Client`.
- Task 3: Added `_latestStints` and `_driverCurrentLap` ConcurrentDictionaries plus `_lastLapPoll` timestamp to orchestrator. Added `PollStintsAsync` (10s) and `PollLapsAsync` (5s) loops to `Task.WhenAll` — orchestrator now runs 5 parallel loops as designed.
- Task 4: `BuildSnapshot()` now populates `TyreCompound` from latest stint compound and `StintLaps = TyreAgeAtStart + Max(0, currentLap - LapStart + 1)`. Both fields remain `null` when data not yet available — correct for race start.
- Task 5: 4 new `[Fact]` tests added to `RaceDataOrchestratorTests`. Total backend tests: 49 (was 45). `MakeStint` helper added alongside existing `MakePosition`/`MakeInterval` helpers.
- Task 6: Created `tyreUtils.ts` with Pirelli official hex colours, single-letter abbreviations, and `getTyreColour`/`getTyreAbbreviation` helpers.
- Task 7: `DriverRow.tsx` now renders a 10px colour-coded circle (`data-testid="tyre-compound"`) and lap count (`data-testid="stint-laps"`) between driver code and gap. Entire tyre section is conditionally null when `tyreCompound` is null — no empty space when data hasn't arrived.
- Task 8: 4 new component tests in `GapList.test.tsx`. Total frontend tests: 49 (was 45).
- All tests pass: 49 backend (dotnet test), 49 frontend (vitest). TypeScript: 0 errors.

### File List

backend/F1App.Api/Dtos/OpenF1/OpenF1StintDto.cs (new)
backend/F1App.Api/Dtos/OpenF1/OpenF1LapDto.cs (new)
backend/F1App.Api/Clients/IOpenF1Client.cs (modified)
backend/F1App.Api/Clients/OpenF1Client.cs (modified)
backend/F1App.Api/Services/RaceDataOrchestrator.cs (modified)
backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs (modified)
frontend/src/shared/utils/tyreUtils.ts (new)
frontend/src/features/live-race/GapList/DriverRow.tsx (modified)
frontend/src/features/live-race/GapList/GapList.test.tsx (modified)

### Review Findings

- [x] [Review][Patch] `Compound` field is non-nullable but OpenF1 sends null during formation laps [backend/F1App.Api/Dtos/OpenF1/OpenF1StintDto.cs:12] — Fixed: changed to `string? Compound`.
- [x] [Review][Patch] `PollStintsAsync` monotonic-StintNumber guard causes cross-session contamination [backend/F1App.Api/Services/RaceDataOrchestrator.cs:133] — Fixed: replaced AddOrUpdate guard with GroupBy+MaxBy per driver to rebuild dict clean from each full-refresh batch.
- [x] [Review][Defer] `_lastLapPoll` stamped with server wall clock instead of max `DateStart` from response [backend/F1App.Api/Services/RaceDataOrchestrator.cs:140] — deferred, pre-existing pattern used by `_lastPositionPoll` and `_lastIntervalPoll` throughout the orchestrator
- [x] [Review][Defer] `stintLaps` transiently shows 0 immediately after a pit stop [backend/F1App.Api/Services/RaceDataOrchestrator.cs] — deferred, inherent timing gap between stints (10s poll) and laps (5s poll) pollers; acceptable for a polling-based system
- [x] [Review][Defer] No unit tests for `PollStintsAsync`/`PollLapsAsync` loop behaviour — deferred, beyond Task 5 scope (which specifies snapshot assembly tests only); can be addressed in a future hardening story
- [x] [Review][Defer] No immediate first-fetch on startup; PeriodicTimer fires after first interval — deferred, pre-existing pattern across all pollers in the orchestrator

## Change Log

| Date | Change |
|------|--------|
| 2026-06-17 | Story created via bmad-create-story |
| 2026-06-17 | Implementation complete: 2 new backend DTOs, 2 new client methods, 2 new orchestrator poll loops, StintLaps calculation in BuildSnapshot, tyreUtils.ts, DriverRow tyre circle, 4 backend + 4 frontend tests |
| 2026-06-17 | Code review complete: 2 patch findings, 4 deferred, 10 dismissed |
