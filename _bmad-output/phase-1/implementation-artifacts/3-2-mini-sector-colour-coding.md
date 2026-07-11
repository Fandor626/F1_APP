---
baseline_commit: "4e96dba"
---

# Story 3.2: Mini-Sector Colour Coding

Status: ready-for-dev

## Story

As a race-day fan,
I want driver dots colour-coded by their current sector pace,
So that I can spot who's pushing without watching every car.

## Acceptance Criteria

1. **Given** the track map is rendering **When** a driver completes a timing sector (S1, S2, or S3) **Then** their dot border updates to: purple (session fastest), green (personal best), yellow (normal pace), white (in/out-lap).
2. **Given** the sector colour is determined **When** a new sector is completed **Then** it reflects the *most recently* completed sector (S3 > S2 > S1 in priority order).
3. **Given** `sessionMode === 'fallback'` **When** the TrackMap renders **Then** dots have no sector colour (null status).
4. **Given** a driver has no sector data yet **When** the dot renders **Then** the dot border uses the default dark colour (no sector ring).

## Tasks / Subtasks

### Task 1: Backend — Extend `OpenF1LapDto` with sector fields (AC: 1)

- [ ] Update `backend/F1App.Api/Dtos/OpenF1/OpenF1LapDto.cs`:
  ```csharp
  public record OpenF1LapDto(
      [property: JsonPropertyName("driver_number")] int DriverNumber,
      [property: JsonPropertyName("lap_number")] int LapNumber,
      [property: JsonPropertyName("date_start")] DateTimeOffset DateStart,
      [property: JsonPropertyName("lap_duration")] double? LapDuration,
      [property: JsonPropertyName("is_pit_out_lap")] bool IsPitOutLap,
      [property: JsonPropertyName("duration_sector_1")] double? DurationSector1 = null,
      [property: JsonPropertyName("duration_sector_2")] double? DurationSector2 = null,
      [property: JsonPropertyName("duration_sector_3")] double? DurationSector3 = null
  );
  ```
  **OpenF1 `/laps` actual field names** (confirmed): `duration_sector_1`, `duration_sector_2`, `duration_sector_3`.
  Sector fields are null mid-lap and become non-null as each sector completes.
  Existing `MakeLap(lapNum, duration, isPitOut)` call sites in tests still compile — new params have defaults.

### Task 2: Backend — Add `MiniSectorStatus` to `DriverState` (AC: 1, 4)

- [ ] Update `backend/F1App.Api/Models/DriverState.cs`:
  ```csharp
  public string? MiniSectorStatus { get; init; } // "purple" | "green" | "yellow" | "white" | null
  ```
  Use `string?` (not enum) — enum requires `JsonStringEnumConverter` which is not configured globally; plain string avoids serialization mismatch between C# and TypeScript.

### Task 3: Backend — Track sector bests and compute status in `RaceDataOrchestrator` (AC: 1, 2)

- [ ] Add fields to `RaceDataOrchestrator` after `_latestLocations`:
  ```csharp
  // Session-best sector times (only go down); initialized to MaxValue
  private double _sessionBestS1 = double.MaxValue;
  private double _sessionBestS2 = double.MaxValue;
  private double _sessionBestS3 = double.MaxValue;
  // Per-driver personal best per sector: driverNum → [s1best, s2best, s3best]
  internal readonly ConcurrentDictionary<int, double[]> _personalBestSectors = new();
  // Latest computed dot colour per driver
  internal readonly ConcurrentDictionary<int, string> _latestSectorStatus = new();
  ```

- [ ] Update `PollLapsAsync` to compute sector status after the existing `_driverCurrentLap` / `_driverLapTimes` update block. Append inside the `foreach (var lap in laps)` loop **after** the existing code:
  ```csharp
  ComputeSectorStatus(lap);
  ```

- [ ] Add private method `ComputeSectorStatus` after `PollLapsAsync`:
  ```csharp
  private void ComputeSectorStatus(OpenF1LapDto lap)
  {
      if (lap.IsPitOutLap)
      {
          _latestSectorStatus[lap.DriverNumber] = "white";
          return;
      }

      // Find the most-recently completed sector (S3 has priority over S2 over S1)
      double? sectorTime = null;
      int sectorIndex = 0; // 0-based: 0=S1, 1=S2, 2=S3

      if (lap.DurationSector3.HasValue) { sectorTime = lap.DurationSector3; sectorIndex = 2; }
      else if (lap.DurationSector2.HasValue) { sectorTime = lap.DurationSector2; sectorIndex = 1; }
      else if (lap.DurationSector1.HasValue) { sectorTime = lap.DurationSector1; sectorIndex = 0; }

      if (!sectorTime.HasValue) return;

      var time = sectorTime.Value;

      // Update session best
      ref double sessionBest = ref sectorIndex switch
      {
          0 => ref _sessionBestS1,
          1 => ref _sessionBestS2,
          _ => ref _sessionBestS3,
      };
      var isSessionBest = time < sessionBest;
      if (isSessionBest) sessionBest = time;

      // Update personal best
      var personal = _personalBestSectors.GetOrAdd(lap.DriverNumber, _ => [double.MaxValue, double.MaxValue, double.MaxValue]);
      var isPersonalBest = time < personal[sectorIndex];
      if (isPersonalBest) personal[sectorIndex] = time;

      var colour = isSessionBest ? "purple" : isPersonalBest ? "green" : "yellow";
      _latestSectorStatus[lap.DriverNumber] = colour;
  }
  ```

- [ ] Update `BuildSnapshot` — in the live/stale driver assembly loop, add after `double? x = null, y = null;` block:
  ```csharp
  _latestSectorStatus.TryGetValue(driverNum, out var sectorStatus);
  ```
  And in `drivers.Add(new DriverState { ... })`, add:
  ```csharp
  MiniSectorStatus = sectorStatus,
  ```
  **Fallback path**: `_fallbackDrivers` are already built without `MiniSectorStatus` — they default to `null`. No change needed in `LoadFallbackDataAsync`.

### Task 4: Backend — Tests (AC: 1, 2, 3, 4)

- [ ] Add helper to `RaceDataOrchestratorTests.cs` after `MakeLocation`:
  ```csharp
  private static OpenF1LapDto MakeLapWithSectors(
      int driverNum, int lapNum, bool isPitOut = false,
      double? s1 = null, double? s2 = null, double? s3 = null) =>
      new(driverNum, lapNum, DateTimeOffset.UtcNow, s1.HasValue && s2.HasValue && s3.HasValue ? s1 + s2 + s3 : null,
          isPitOut, s1, s2, s3);
  ```

- [ ] Add 5 new test cases in `RaceDataOrchestratorTests.cs`:
  ```csharp
  [Fact]
  public void ComputeSectorStatus_PitOutLap_SetsWhite()
  {
      var sut = CreateOrchestrator();
      sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, isPitOut: true, s1: 30.0));
      Assert.Equal("white", sut._latestSectorStatus[1]);
  }

  [Fact]
  public void ComputeSectorStatus_FirstSectorTime_IsSessionBest_SetsPurple()
  {
      var sut = CreateOrchestrator();
      sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 25.0));
      Assert.Equal("purple", sut._latestSectorStatus[1]);
  }

  [Fact]
  public void ComputeSectorStatus_SecondDriverFasterS1_FirstDriverBecomesGreen()
  {
      var sut = CreateOrchestrator();
      sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 25.0)); // driver 1 sets session best
      sut.ComputeSectorStatus(MakeLapWithSectors(2, 1, s1: 24.5)); // driver 2 beats it
      sut.ComputeSectorStatus(MakeLapWithSectors(1, 2, s1: 25.0)); // driver 1 repeats personal best
      Assert.Equal("purple", sut._latestSectorStatus[2]);
      Assert.Equal("green", sut._latestSectorStatus[1]); // personal best but not session best
  }

  [Fact]
  public void ComputeSectorStatus_NonBestTime_SetsYellow()
  {
      var sut = CreateOrchestrator();
      sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 25.0)); // sets personal best
      sut.ComputeSectorStatus(MakeLapWithSectors(1, 2, s1: 26.0)); // slower
      Assert.Equal("yellow", sut._latestSectorStatus[1]);
  }

  [Fact]
  public void BuildSnapshot_WithSectorStatus_PopulatesMiniSectorStatusOnDriver()
  {
      var now = DateTimeOffset.UtcNow;
      var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
      sut._latestPositions[1] = MakePosition(1, 1, now);
      sut._latestSectorStatus[1] = "purple";

      var snapshot = sut.BuildSnapshot();

      Assert.Single(snapshot.Drivers);
      Assert.Equal("purple", snapshot.Drivers[0].MiniSectorStatus);
  }
  ```
  **Note**: `ComputeSectorStatus` must be `internal` (not private) for tests to call it directly.

### Task 5: Frontend — Extend types (AC: 1, 4)

- [ ] Update `frontend/src/shared/types/f1.ts` — add `miniSectorStatus` to `DriverState`:
  ```ts
  miniSectorStatus: 'purple' | 'green' | 'yellow' | 'white' | null
  ```
  This is a **required** field (not optional `?`) — matches the pattern of `x` and `y` which are `number | null`. The backend always sends it (defaults to `null` when null from `WhenWritingNull` config, but the type should be explicitly nullable union not optional).

  **Important**: `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` means null fields are omitted from JSON. Frontend receives no `miniSectorStatus` key → TypeScript accesses `undefined`. Add null-coalescing in `DriverDot` to handle this edge case gracefully.

### Task 6: Frontend — Update `InterpolatedPosition` and `useTrackInterpolation` (AC: 1)

- [ ] Update `frontend/src/features/live-race/TrackMap/useTrackInterpolation.ts` — add `miniSectorStatus` to `InterpolatedPosition`:
  ```ts
  export interface InterpolatedPosition {
    driverNumber: number
    driverCode: string
    teamColour: string
    miniSectorStatus: 'purple' | 'green' | 'yellow' | 'white' | null
    svgX: number
    svgY: number
  }
  ```
  In the `result.push({...})` call inside the rAF loop, add:
  ```ts
  miniSectorStatus: driver.miniSectorStatus ?? null,
  ```

### Task 7: Frontend — Update `DriverDot` to render sector colour ring (AC: 1, 4)

- [ ] Update `frontend/src/features/live-race/TrackMap/DriverDot.tsx`:
  ```tsx
  const SECTOR_COLOURS: Record<string, string> = {
    purple: '#bf00ff',
    green: '#00d2be',
    yellow: '#ffd700',
    white: '#e0e0e0',
  }

  interface DriverDotProps {
    driverCode: string
    teamColour: string
    svgX: number
    svgY: number
    miniSectorStatus: 'purple' | 'green' | 'yellow' | 'white' | null
  }

  export function DriverDot({ driverCode, teamColour, svgX, svgY, miniSectorStatus }: DriverDotProps) {
    const strokeColour = miniSectorStatus ? (SECTOR_COLOURS[miniSectorStatus] ?? '#0c0e11') : '#0c0e11'
    const strokeWidth = miniSectorStatus ? 3 : 2

    return (
      <g transform={`translate(${svgX},${svgY})`} data-testid={`driver-dot-${driverCode}`}>
        <circle r={9} fill={`#${teamColour}`} stroke={strokeColour} strokeWidth={strokeWidth} />
        <text
          textAnchor="middle"
          dominantBaseline="central"
          fontSize={7}
          fontWeight={700}
          fill="#0c0e11"
        >
          {driverCode}
        </text>
      </g>
    )
  }
  ```

### Task 8: Frontend — Update `TrackMap` to pass `miniSectorStatus` to `DriverDot` (AC: 1)

- [ ] Update `frontend/src/features/live-race/TrackMap/TrackMap.tsx` — in the `positions.map(p => ...)` render:
  ```tsx
  {positions.map(p => (
    <DriverDot
      key={p.driverNumber}
      driverCode={p.driverCode}
      teamColour={p.teamColour}
      svgX={p.svgX}
      svgY={p.svgY}
      miniSectorStatus={p.miniSectorStatus}
    />
  ))}
  ```

### Task 9: Frontend — Tests and regression fixes (AC: 1, 2, 3, 4)

- [ ] Update `frontend/src/features/live-race/TrackMap/TrackMap.test.tsx` — add `miniSectorStatus: null` to the driver in the "renders driver dots" test and add a new test:
  ```tsx
  it('renders driver dot with purple stroke for session-fastest sector', async () => {
    vi.mocked(global.fetch).mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(mockConfig),
    } as Response)
    useLiveRaceStore.setState({
      drivers: {
        '1': {
          driverNumber: 1, driverCode: 'VER', teamName: 'Red Bull Racing',
          teamColour: '3671C6', position: 1, gapToCarAhead: null,
          gapIsStale: false, tyreCompound: null, stintLaps: null,
          championshipDelta: null, x: -1500.0, y: 823.0,
          miniSectorStatus: 'purple',
        },
      },
    })
    render(<TrackMap circuitId="monza" />)
    await waitFor(() => expect(screen.getByTestId('driver-dot-VER')).toBeInTheDocument(), { timeout: 2000 })
    const circle = screen.getByTestId('driver-dot-VER').querySelector('circle')
    expect(circle?.getAttribute('stroke')).toBe('#bf00ff')
  })
  ```

- [ ] Update `frontend/src/features/live-race/GapList/GapList.test.tsx` — add `miniSectorStatus: null` to all `makeDriver` calls / store state where `DriverState` is constructed.

- [ ] Update `frontend/src/features/live-race/LapTimeChart/LapTimeChart.test.tsx` — same as above.

- [ ] Update `frontend/src/shared/utils/normalizeSnapshot.test.ts` — add `miniSectorStatus: null` to `makeDriver`.

- [ ] Update `frontend/src/features/live-race/LiveRacePage.test.tsx` — add `miniSectorStatus: null` to the driver in the REST fallback test.

- [ ] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass.
- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass.

## Dev Notes

### Architecture Alignment

- `MiniSectorStatus` is `string?` in `DriverState` (not an enum). The backend `DefaultIgnoreCondition = WhenWritingNull` omits null from JSON, so the frontend receives no key when null. Frontend code defensively uses `?? null` to handle `undefined`.
- `ComputeSectorStatus` must be declared `internal` (not `private`) so `RaceDataOrchestratorTests` can call it directly (via `InternalsVisibleTo` already set up on the test project).
- Session best fields (`_sessionBestS1/2/3`) are plain `double` fields — written and read only from `PollLapsAsync` (single-threaded). They don't need ConcurrentDictionary.
- Personal bests are stored in `double[]` arrays inside `ConcurrentDictionary<int, double[]>` — `GetOrAdd` is thread-safe; the array elements are only ever written from `PollLapsAsync` (same single-threaded context).
- `_latestSectorStatus` (ConcurrentDictionary) is safe to read from `BuildSnapshot` (called from `PublishSnapshotLoopAsync` on a different task).
- The `MakeLap` helper in tests uses positional constructor `(lapNum, duration, isPitOut)`. The new sector fields are added with `= null` defaults as the last positional params — existing call sites are unchanged.

### Data Availability Note

OpenF1's `/laps` endpoint populates sector times progressively:
- After S1 completes → `duration_sector_1` non-null, S2/S3 null
- After S2 completes → S1 and S2 non-null, S3 null
- After lap ends → all three set

The `PollLapsAsync` timer fires every 5 seconds; this is intentional — sector times only change once per sector (~20-60s intervals). The status updates on the next poll after a sector completes.

### Colour Convention

Classic F1 sector colours:
- **Purple** (`#bf00ff`) — session fastest (matches broadcast convention)
- **Green** (`#00d2be`) — personal best (matches F1 TV green)  
- **Yellow** (`#ffd700`) — slower than personal best
- **White** (`#e0e0e0`) — formation/out/in lap (driver not on a timed lap)

### Regressions to Guard

- **`makeDriver` helpers** in `GapList.test.tsx`, `LapTimeChart.test.tsx`, `normalizeSnapshot.test.ts`, `LiveRacePage.test.tsx` must all add `miniSectorStatus: null` — TypeScript will error if the required field is missing (since it's in the `DriverState` interface).
- **Existing `DriverDot` test** in `TrackMap.test.tsx` passes `x: -1500.0, y: 823.0` — add `miniSectorStatus: null` there too.
- **`TrackMap`** passes `miniSectorStatus` to each `DriverDot` — if prop is forgotten, TypeScript will catch it at compile time.

### Files to Create / Modify

**Backend MODIFY:**
- `backend/F1App.Api/Dtos/OpenF1/OpenF1LapDto.cs` — add 3 sector fields with defaults
- `backend/F1App.Api/Models/DriverState.cs` — add `MiniSectorStatus`
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` — add 5 fields, `ComputeSectorStatus` method, update `PollLapsAsync`, update `BuildSnapshot`
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs` — helper + 5 tests

**Frontend MODIFY:**
- `frontend/src/shared/types/f1.ts` — add `miniSectorStatus` to `DriverState`
- `frontend/src/features/live-race/TrackMap/useTrackInterpolation.ts` — add to `InterpolatedPosition`, populate in rAF loop
- `frontend/src/features/live-race/TrackMap/DriverDot.tsx` — add `miniSectorStatus` prop + stroke colour logic
- `frontend/src/features/live-race/TrackMap/TrackMap.tsx` — pass prop through
- `frontend/src/features/live-race/TrackMap/TrackMap.test.tsx` — update existing + add 1 new test
- `frontend/src/features/live-race/GapList/GapList.test.tsx` — add `miniSectorStatus: null`
- `frontend/src/features/live-race/LapTimeChart/LapTimeChart.test.tsx` — add `miniSectorStatus: null`
- `frontend/src/shared/utils/normalizeSnapshot.test.ts` — add `miniSectorStatus: null`
- `frontend/src/features/live-race/LiveRacePage.test.tsx` — add `miniSectorStatus: null`

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

### File List

### Change Log

| Date | Change |
|------|--------|
| 2026-07-09 | Story created via bmad-create-story |
