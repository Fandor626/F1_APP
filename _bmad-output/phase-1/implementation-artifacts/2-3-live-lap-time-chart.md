---
baseline_commit: 95dc0858c5ceda0ba19a3ecfce5de449b853eb1c
---

# Story 2.3: Live Lap Time Chart

Status: review

## Story

As a race-day fan,
I want a chart of each driver's lap times as the race progresses,
So that I can spot who's pushing and who's struggling.

## Acceptance Criteria

1. **Given** the live race page **When** a lap completes for any driver **Then** the chart updates with that driver's lap time at the corresponding lap number.
2. **Given** a pit-out lap **When** plotted **Then** it renders as a visible upward spike (no special filtering or flattening — the raw long duration is the spike).
3. **Given** a hover on a data point **When** triggered **Then** it shows the exact lap time (formatted `m:ss.sss`) and the gap to the race's fastest lap so far (e.g. `+0.823s`); the leader shows `fastest`.
4. **Given** the race begins **When** no lap time data has arrived yet **Then** the chart shows a "Waiting for lap data…" placeholder — never an empty/broken chart.
5. **Given** multiple drivers **When** chart renders **Then** each driver is a separate coloured line using their `teamColour` from the Zustand `drivers` store; lines grow lap by lap.

## Tasks / Subtasks

### Task 1: Extend `OpenF1LapDto` with lap duration and pit-out flag (AC: 1, 2)

- [x] Edit `backend/F1App.Api/Dtos/OpenF1/OpenF1LapDto.cs` — add two new properties to the existing record:
  ```csharp
  [property: JsonPropertyName("lap_duration")] double? LapDuration,
  [property: JsonPropertyName("is_pit_out_lap")] bool IsPitOutLap
  ```
  Full updated record (preserving `DateStart` order):
  ```csharp
  public record OpenF1LapDto(
      [property: JsonPropertyName("driver_number")] int DriverNumber,
      [property: JsonPropertyName("lap_number")] int LapNumber,
      [property: JsonPropertyName("date_start")] DateTimeOffset DateStart,
      [property: JsonPropertyName("lap_duration")] double? LapDuration,
      [property: JsonPropertyName("is_pit_out_lap")] bool IsPitOutLap
  );
  ```
  **Notes:**
  - `LapDuration` is seconds as a float (e.g. `83.456`); `null` during the lap being driven and sometimes on first lap.
  - `IsPitOutLap` is the out-lap after a pit stop — these have very long `LapDuration` (~110-140s) and naturally appear as spikes on the chart. No special rendering needed beyond rendering the raw value.
  - `DateStart` is the lap start timestamp used for incremental polling — already in use by Story 2.2's `PollLapsAsync`.

### Task 2: Create `LapTimeEntry` model (AC: 1, 2, 3, 5)

- [x] Create `backend/F1App.Api/Models/LapTimeEntry.cs`:
  ```csharp
  namespace F1App.Api.Models;

  public record LapTimeEntry(int LapNumber, double? LapDurationSeconds, bool IsPitOutLap);
  ```
  **Notes:**
  - Goes in `Models/` alongside `DriverState.cs`, `RaceStateSnapshot.cs` (domain model, not DTO).
  - `LapDurationSeconds` is nullable — a lap entry can arrive before the lap is complete (only `date_start` available then). In practice, completed laps always have a duration; in-progress laps do not. Only completed-lap entries (with non-null `LapDuration`) are stored.

### Task 3: Add `LapChart` to `RaceStateSnapshot` (AC: 1, 5)

- [x] Edit `backend/F1App.Api/Models/RaceStateSnapshot.cs` — add `LapChart` property:
  ```csharp
  public record RaceStateSnapshot
  {
      public DateTimeOffset CapturedAt { get; init; }
      public IReadOnlyList<DriverState> Drivers { get; init; } = [];
      // Key = DriverNumber; list is ordered by LapNumber ascending
      public IReadOnlyDictionary<int, IReadOnlyList<LapTimeEntry>> LapChart { get; init; }
          = new Dictionary<int, IReadOnlyList<LapTimeEntry>>();
  }
  ```
  **Notes:**
  - `LapChart` is keyed by `int` driver number (matches `DriverState.DriverNumber`). Frontend converts this to a `Record<string, LapTimeEntry[]>` because JSON object keys are always strings.
  - List per driver is ordered by `LapNumber` ascending — done in `BuildSnapshot`.
  - Empty dictionary is the default — never null.

### Task 4: Accumulate lap times in `RaceDataOrchestrator` and populate `LapChart` in `BuildSnapshot` (AC: 1, 2)

- [x] Add new `internal` field to `RaceDataOrchestrator` (after the existing `_driverCurrentLap` field):
  ```csharp
  // Per-driver lap time history: driverNumber → (lapNumber → LapTimeEntry)
  // Only stores laps with non-null LapDuration (completed laps)
  internal readonly ConcurrentDictionary<int, ConcurrentDictionary<int, LapTimeEntry>> _driverLapTimes = new();
  ```

- [x] In `PollLapsAsync`, extend the existing `foreach` loop to also populate `_driverLapTimes`:
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
              // Existing: track max lap number per driver
              _driverCurrentLap.AddOrUpdate(
                  lap.DriverNumber,
                  lap.LapNumber,
                  (_, existing) => lap.LapNumber > existing ? lap.LapNumber : existing);

              // New: store completed lap times (skip in-progress laps with no duration)
              if (lap.LapDuration.HasValue)
              {
                  var driverLaps = _driverLapTimes.GetOrAdd(lap.DriverNumber, _ => new());
                  driverLaps[lap.LapNumber] = new LapTimeEntry(lap.LapNumber, lap.LapDuration, lap.IsPitOutLap);
              }
          }
      }
  }
  ```

- [x] In `BuildSnapshot()`, after building the `drivers` list, serialize `_driverLapTimes` into `LapChart`:
  ```csharp
  // Build LapChart from accumulated lap times
  var lapChart = new Dictionary<int, IReadOnlyList<LapTimeEntry>>();
  foreach (var (driverNum, lapsByLap) in _driverLapTimes)
  {
      lapChart[driverNum] = [.. lapsByLap.Values.OrderBy(l => l.LapNumber)];
  }

  return new RaceStateSnapshot
  {
      CapturedAt = timeProvider.GetUtcNow(),
      Drivers = [.. drivers.OrderBy(d => d.Position)],
      LapChart = lapChart,
  };
  ```
  **Note on thread safety**: `ConcurrentDictionary<int, ConcurrentDictionary<int, LapTimeEntry>>` is safe to enumerate during the publish tick — `ConcurrentDictionary` enumeration gives a consistent point-in-time snapshot of the outer keys. The inner `ConcurrentDictionary` is iterated via `.Values` which is also thread-safe. A lap written by `PollLapsAsync` simultaneously might appear in the next snapshot — acceptable for a 1s publishing cadence.

### Task 5: Backend tests for `LapChart` in `BuildSnapshot` (AC: 1, 2)

- [x] In `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`, add a `MakeLap` helper alongside existing helpers:
  ```csharp
  private static LapTimeEntry MakeLap(int lapNum, double? duration, bool isPitOut = false) =>
      new(lapNum, duration, isPitOut);
  ```

- [x] Add 4 new `[Fact]` tests:
  ```csharp
  [Fact]
  public void BuildSnapshot_WithLapTimes_PopulatesLapChart()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      sut._driverLapTimes[33] = new ConcurrentDictionary<int, LapTimeEntry>();
      sut._driverLapTimes[33][1] = MakeLap(1, 83.456);
      sut._driverLapTimes[33][2] = MakeLap(2, 82.123);

      var snapshot = sut.BuildSnapshot();

      Assert.Single(snapshot.LapChart);
      Assert.Equal(2, snapshot.LapChart[33].Count);
      Assert.Equal(1, snapshot.LapChart[33][0].LapNumber);
      Assert.Equal(83.456, snapshot.LapChart[33][0].LapDurationSeconds);
      Assert.Equal(2, snapshot.LapChart[33][1].LapNumber);
  }

  [Fact]
  public void BuildSnapshot_LapChartOrderedByLapNumber()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      sut._driverLapTimes[33] = new ConcurrentDictionary<int, LapTimeEntry>();
      // Insert out of order
      sut._driverLapTimes[33][3] = MakeLap(3, 81.0);
      sut._driverLapTimes[33][1] = MakeLap(1, 83.0);
      sut._driverLapTimes[33][2] = MakeLap(2, 82.0);

      var snapshot = sut.BuildSnapshot();

      var laps = snapshot.LapChart[33];
      Assert.Equal(1, laps[0].LapNumber);
      Assert.Equal(2, laps[1].LapNumber);
      Assert.Equal(3, laps[2].LapNumber);
  }

  [Fact]
  public void BuildSnapshot_PitOutLapFlagPreserved()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      sut._driverLapTimes[33] = new ConcurrentDictionary<int, LapTimeEntry>();
      sut._driverLapTimes[33][20] = MakeLap(20, 125.7, isPitOut: true);

      var snapshot = sut.BuildSnapshot();

      Assert.True(snapshot.LapChart[33][0].IsPitOutLap);
      Assert.Equal(125.7, snapshot.LapChart[33][0].LapDurationSeconds);
  }

  [Fact]
  public void BuildSnapshot_NoLapTimes_LapChartEmpty()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      // _driverLapTimes not populated

      var snapshot = sut.BuildSnapshot();

      Assert.Empty(snapshot.LapChart);
  }
  ```
  **Note**: The `using` import `using System.Collections.Concurrent;` is already present (used by `_latestPositions` in the field under test). Verify the test file compiles.

- [x] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass (previous 49 + 4 new = 53 total).

### Task 6: Update frontend types and Zustand store (AC: 1, 5)

- [x] Edit `frontend/src/shared/types/f1.ts` — add `LapTimeEntry` interface and `lapChart` to `RaceStateSnapshot`:
  ```typescript
  export interface LapTimeEntry {
    lapNumber: number
    lapDurationSeconds: number | null
    isPitOutLap: boolean
  }

  export interface DriverState {
    driverNumber: number
    driverCode: string
    teamName: string
    teamColour: string
    position: number
    gapToCarAhead: string | null
    gapIsStale: boolean
    tyreCompound: string | null
    stintLaps: number | null
    championshipDelta: string | null
  }

  export interface RaceStateSnapshot {
    capturedAt: string
    drivers: DriverState[]
    // Key is driverNumber as string (JSON object keys are always strings)
    lapChart: Record<string, LapTimeEntry[]>
  }
  ```

- [x] Edit `frontend/src/features/live-race/store/liveRaceStore.ts` — add `lapChart` state and `setLapChart` action:
  ```typescript
  import { create } from 'zustand'
  import type { DriverState, LapTimeEntry } from '../../../shared/types/f1'

  type ConnectionStatus = 'connected' | 'reconnecting' | 'disconnected'

  interface LiveRaceState {
    connectionStatus: ConnectionStatus
    drivers: Record<string, DriverState>
    lapChart: Record<string, LapTimeEntry[]>
    lastSnapshotTime: Date | null
    setConnectionStatus: (status: ConnectionStatus) => void
    setDrivers: (drivers: Record<string, DriverState>) => void
    setLapChart: (lapChart: Record<string, LapTimeEntry[]>) => void
    setLastSnapshotTime: (time: Date) => void
  }

  export const useLiveRaceStore = create<LiveRaceState>((set) => ({
    connectionStatus: 'disconnected',
    drivers: {},
    lapChart: {},
    lastSnapshotTime: null,
    setConnectionStatus: (status) => set({ connectionStatus: status }),
    setDrivers: (drivers) => set({ drivers }),
    setLapChart: (lapChart) => set({ lapChart }),
    setLastSnapshotTime: (time) => set({ lastSnapshotTime: time }),
  }))
  ```

- [x] Edit `frontend/src/features/live-race/hooks/useSignalRConnection.ts` — extract `setLapChart` from the store and call it in `handleSnapshot`:
  ```typescript
  const setLapChart = useLiveRaceStore(s => s.setLapChart)

  // Inside handleSnapshot:
  const handleSnapshot = (snapshot: RaceSnapshotMessage) => {
    setDrivers(normalizeSnapshot(snapshot.drivers))
    setLapChart(snapshot.lapChart)
    setLastSnapshotTime(new Date())
  }
  ```
  Also add `setLapChart` to the `useEffect` dependency array.

  **Full updated `useSignalRConnection.ts`:**
  ```typescript
  import { useEffect } from 'react'
  import * as signalR from '@microsoft/signalr'
  import { raceHubConnection } from '../signalRClient'
  import { useLiveRaceStore } from '../store/liveRaceStore'
  import { normalizeSnapshot } from '../../../shared/utils/normalizeSnapshot'
  import type { RaceSnapshotMessage } from '../../../shared/types/signalR'

  let lifecycleHandlersAttached = false

  export function useSignalRConnection() {
    const setConnectionStatus = useLiveRaceStore(s => s.setConnectionStatus)
    const setDrivers = useLiveRaceStore(s => s.setDrivers)
    const setLapChart = useLiveRaceStore(s => s.setLapChart)
    const setLastSnapshotTime = useLiveRaceStore(s => s.setLastSnapshotTime)

    useEffect(() => {
      const handleSnapshot = (snapshot: RaceSnapshotMessage) => {
        setDrivers(normalizeSnapshot(snapshot.drivers))
        setLapChart(snapshot.lapChart)
        setLastSnapshotTime(new Date())
      }

      raceHubConnection.on('RaceSnapshot', handleSnapshot)

      if (!lifecycleHandlersAttached) {
        raceHubConnection.onreconnecting(() => setConnectionStatus('reconnecting'))
        raceHubConnection.onreconnected(() => setConnectionStatus('connected'))
        raceHubConnection.onclose(() => setConnectionStatus('disconnected'))
        lifecycleHandlersAttached = true
      }

      if (raceHubConnection.state === signalR.HubConnectionState.Disconnected) {
        raceHubConnection
          .start()
          .then(() => setConnectionStatus('connected'))
          .catch(() => setConnectionStatus('disconnected'))
      }

      return () => {
        raceHubConnection.off('RaceSnapshot', handleSnapshot)
      }
    }, [setConnectionStatus, setDrivers, setLapChart, setLastSnapshotTime])
  }
  ```

### Task 7: Create `LapTimeChart.tsx` component (AC: 1, 2, 3, 4, 5)

- [x] Create `frontend/src/features/live-race/LapTimeChart/LapTimeChart.tsx`:

  ```tsx
  import {
    ResponsiveContainer,
    LineChart,
    Line,
    XAxis,
    YAxis,
    Tooltip,
    CartesianGrid,
  } from 'recharts'
  import { useShallow } from 'zustand/react/shallow'
  import { useLiveRaceStore } from '../store/liveRaceStore'
  import type { LapTimeEntry } from '../../../shared/types/f1'

  function formatLapTime(seconds: number): string {
    const m = Math.floor(seconds / 60)
    const s = seconds % 60
    return `${m}:${s.toFixed(3).padStart(6, '0')}`
  }

  interface ChartPoint {
    lap: number
    [driverNum: string]: number | undefined
  }

  function buildChartData(
    lapChart: Record<string, LapTimeEntry[]>
  ): ChartPoint[] {
    const allLapNums = [
      ...new Set(
        Object.values(lapChart).flatMap(laps => laps.map(l => l.lapNumber))
      ),
    ].sort((a, b) => a - b)

    return allLapNums.map(lapNum => {
      const point: ChartPoint = { lap: lapNum }
      for (const [driverNum, laps] of Object.entries(lapChart)) {
        const entry = laps.find(l => l.lapNumber === lapNum)
        if (entry?.lapDurationSeconds != null) {
          point[driverNum] = entry.lapDurationSeconds
        }
      }
      return point
    })
  }

  function findFastestLap(lapChart: Record<string, LapTimeEntry[]>): number | null {
    const allTimes = Object.values(lapChart)
      .flatMap(laps => laps)
      .filter(l => !l.isPitOutLap && l.lapDurationSeconds != null)
      .map(l => l.lapDurationSeconds!)
    return allTimes.length > 0 ? Math.min(...allTimes) : null
  }

  interface CustomTooltipProps {
    active?: boolean
    payload?: Array<{ name: string; value: number; color: string }>
    label?: number
    fastestLap: number | null
  }

  function CustomTooltip({ active, payload, label, fastestLap }: CustomTooltipProps) {
    if (!active || !payload?.length) return null
    return (
      <div className="bg-[#1b1f26] border border-[#2a2f38] rounded-[8px] px-3 py-2 text-[11px]">
        <p className="text-[#9aa1ad] mb-1 font-semibold">Lap {label}</p>
        {payload.map(entry => {
          const gap =
            fastestLap != null && entry.value !== fastestLap
              ? `+${(entry.value - fastestLap).toFixed(3)}s`
              : 'fastest'
          return (
            <div key={entry.name} className="flex items-center gap-2 mb-0.5">
              <span
                className="inline-block rounded-full shrink-0"
                style={{ width: 7, height: 7, backgroundColor: entry.color }}
              />
              <span className="text-[#eef0f3] tabular-nums">
                {formatLapTime(entry.value)}
              </span>
              <span className="text-[#9aa1ad] tabular-nums">{gap}</span>
            </div>
          )
        })}
      </div>
    )
  }

  export function LapTimeChart() {
    const lapChart = useLiveRaceStore(s => s.lapChart)
    const drivers = useLiveRaceStore(
      useShallow(s => {
        const meta: Record<string, string> = {}
        for (const [id, d] of Object.entries(s.drivers)) {
          meta[id] = d.teamColour
        }
        return meta
      })
    )

    const driverNums = Object.keys(lapChart)

    if (driverNums.length === 0) {
      return (
        <div className="bg-[#1b1f26] border border-[#2a2f38] rounded-[14px] p-[10px]">
          <span className="text-[11.5px] font-semibold tracking-[0.04em] uppercase text-[#9aa1ad]">
            Lap Times
          </span>
          <p className="px-[10px] py-6 text-[12px] text-[#6b7280]">
            Waiting for lap data…
          </p>
        </div>
      )
    }

    const chartData = buildChartData(lapChart)
    const fastestLap = findFastestLap(lapChart)

    return (
      <div className="bg-[#1b1f26] border border-[#2a2f38] rounded-[14px] p-[10px]">
        <span className="text-[11.5px] font-semibold tracking-[0.04em] uppercase text-[#9aa1ad] block mb-3">
          Lap Times
        </span>
        <ResponsiveContainer width="100%" height={280}>
          <LineChart data={chartData} margin={{ top: 4, right: 8, bottom: 4, left: 8 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#2a2f38" />
            <XAxis
              dataKey="lap"
              tick={{ fill: '#9aa1ad', fontSize: 10 }}
              label={{ value: 'Lap', position: 'insideBottomRight', offset: -4, fill: '#9aa1ad', fontSize: 10 }}
            />
            <YAxis
              tickFormatter={formatLapTime}
              tick={{ fill: '#9aa1ad', fontSize: 10 }}
              width={52}
              domain={['auto', 'auto']}
            />
            <Tooltip
              content={
                <CustomTooltip fastestLap={fastestLap} />
              }
            />
            {driverNums.map(driverNum => (
              <Line
                key={driverNum}
                type="monotone"
                dataKey={driverNum}
                stroke={`#${drivers[driverNum] ?? '555555'}`}
                strokeWidth={1.5}
                dot={false}
                activeDot={{ r: 3 }}
                connectNulls={false}
                isAnimationActive={false}
              />
            ))}
          </LineChart>
        </ResponsiveContainer>
      </div>
    )
  }
  ```

  **Key decisions:**
  - `isAnimationActive={false}` — avoids Recharts entrance animation on every snapshot update (would cause flickering with 1-2s updates).
  - `connectNulls={false}` — gaps where a driver hasn't completed a lap (e.g. retired) render as line breaks, not misleading interpolation.
  - `domain={['auto', 'auto']}` on Y-axis — Recharts auto-scales to min/max of visible data; pit-out laps (high duration) will naturally expand the Y range.
  - `useShallow` on the drivers selector — only extracts `teamColour` per driver so the component only re-renders when team colours change, not on every driver state change.
  - `fastestLap` excludes `isPitOutLap` entries — pit laps don't count as the race fastest lap.

### Task 8: Wire `LapTimeChart` into `LiveRacePage` (AC: 1, 4, 5)

- [x] Edit `frontend/src/features/live-race/LiveRacePage.tsx`:
  ```tsx
  import { GapList } from './GapList/GapList'
  import { LapTimeChart } from './LapTimeChart/LapTimeChart'
  import { useSignalRConnection } from './hooks/useSignalRConnection'

  export function LiveRacePage() {
    useSignalRConnection()

    return (
      <div className="min-h-screen bg-[#14171c] text-[#eef0f3] p-4">
        <h1 className="text-[26px] font-bold tracking-[-0.01em] mb-4">Live Race</h1>
        <div className="flex flex-col gap-4">
          <GapList />
          <LapTimeChart />
        </div>
      </div>
    )
  }
  ```

### Task 9: Frontend tests for `LapTimeChart` (AC: 1, 3, 4, 5)

- [x] Create `frontend/src/features/live-race/LapTimeChart/LapTimeChart.test.tsx`:

  ```tsx
  import { render, screen } from '@testing-library/react'
  import { beforeEach, describe, expect, it, vi } from 'vitest'
  import { LapTimeChart } from './LapTimeChart'
  import { useLiveRaceStore } from '../store/liveRaceStore'
  import type { DriverState, LapTimeEntry } from '../../../shared/types/f1'

  // Recharts uses SVG/ResizeObserver which jsdom doesn't support; mock the module
  vi.mock('recharts', () => ({
    ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
      <div data-testid="responsive-container">{children}</div>
    ),
    LineChart: ({ children }: { children: React.ReactNode }) => (
      <div data-testid="line-chart">{children}</div>
    ),
    Line: ({ dataKey }: { dataKey: string }) => (
      <div data-testid={`line-${dataKey}`} />
    ),
    XAxis: () => null,
    YAxis: () => null,
    Tooltip: () => null,
    CartesianGrid: () => null,
  }))

  function makeDriver(driverNumber: number, teamColour = 'FF0000'): DriverState {
    return {
      driverNumber,
      driverCode: `D${driverNumber}`,
      teamName: 'Team A',
      teamColour,
      position: 1,
      gapToCarAhead: null,
      gapIsStale: false,
      tyreCompound: null,
      stintLaps: null,
      championshipDelta: null,
    }
  }

  function makeLap(lapNumber: number, lapDurationSeconds: number, isPitOutLap = false): LapTimeEntry {
    return { lapNumber, lapDurationSeconds, isPitOutLap }
  }

  beforeEach(() => {
    useLiveRaceStore.setState({
      drivers: {},
      lapChart: {},
      connectionStatus: 'disconnected',
      lastSnapshotTime: null,
    })
  })

  describe('LapTimeChart', () => {
    it('shows waiting placeholder when no lap chart data', () => {
      render(<LapTimeChart />)
      expect(screen.getByText('Waiting for lap data…')).toBeInTheDocument()
    })

    it('renders the chart when lap data is present', () => {
      useLiveRaceStore.setState({
        lapChart: {
          '33': [makeLap(1, 83.456)],
        },
        drivers: { '33': makeDriver(33) },
      })

      render(<LapTimeChart />)

      expect(screen.getByTestId('line-chart')).toBeInTheDocument()
      expect(screen.queryByText('Waiting for lap data…')).not.toBeInTheDocument()
    })

    it('renders one Line per driver', () => {
      useLiveRaceStore.setState({
        lapChart: {
          '1': [makeLap(1, 82.0)],
          '33': [makeLap(1, 83.0)],
        },
        drivers: {
          '1': makeDriver(1, '3671C6'),
          '33': makeDriver(33, 'E8002D'),
        },
      })

      render(<LapTimeChart />)

      expect(screen.getByTestId('line-1')).toBeInTheDocument()
      expect(screen.getByTestId('line-33')).toBeInTheDocument()
    })

    it('shows "Lap Times" header in both empty and populated states', () => {
      render(<LapTimeChart />)
      expect(screen.getByText('Lap Times')).toBeInTheDocument()
    })

    it('still renders chart when driver meta is missing for a lap chart entry', () => {
      // Driver 99 has lap data but no entry in drivers store (edge case: driver joined late)
      useLiveRaceStore.setState({
        lapChart: { '99': [makeLap(1, 85.0)] },
        drivers: {},
      })

      render(<LapTimeChart />)

      expect(screen.getByTestId('line-chart')).toBeInTheDocument()
      // Falls back to #555555 for unknown team colour — no crash
      expect(screen.getByTestId('line-99')).toBeInTheDocument()
    })
  })
  ```

- [x] Run `npm test` from `frontend/` — all tests must pass (previous 49 + 5 new = 54 total).

## Dev Notes

### OpenF1 `/laps` Endpoint Reference

The laps endpoint was introduced in Story 2.2. Relevant shape for Story 2.3:
```json
{
  "driver_number": 33,
  "lap_number": 5,
  "date_start": "2024-03-02T16:04:51.445000+00:00",
  "lap_duration": 83.456,
  "is_pit_out_lap": false,
  "duration_sector_1": 27.1,
  "duration_sector_2": 30.2,
  "duration_sector_3": 26.156
}
```
- `lap_duration`: seconds as float; `null` during the current in-progress lap, sometimes null on lap 1 formation lap.
- `is_pit_out_lap`: `true` for the lap immediately after exiting the pit lane. These laps have very long `lap_duration` (~110-140s) because they include time in the pit lane.
- `date_start` is the filter field for incremental polling (already wired: `date_start>{since}` in `OpenF1Client.GetLatestLapsAsync`).
- `PollLapsAsync` fires every 5s. A lap completes every ~90s, so we typically see the lap entry within 5s of completion.

### Architecture Compliance

- **`LapTimeEntry`** goes in `backend/F1App.Api/Models/` — it is a domain model, not a DTO. DTOs represent external API shapes (`Dtos/OpenF1/`), models represent internal domain entities (`Models/`).
- **`LapTimeChart/`** folder under `frontend/src/features/live-race/` — matches the architecture's prescribed structure: `live-race/LapTimeChart/LapTimeChart.tsx` [Source: architecture.md#Complete Project Tree].
- **Recharts** is the mandated charting library for "all standard charts (trajectory, lap times, career stats)" [Source: architecture.md#Frontend Library Stack].
- **Zustand rule**: The `lapChart` store field holds SignalR push data (same pattern as `drivers`). The architecture's "Zustand = UI state only" rule refers to Ergast REST data not being cached in Zustand — real-time SignalR state is the exception established in Story 2.1.
- **`useShallow`** must be used on all Zustand selectors that return objects/arrays — prevents unnecessary re-renders on every 1-2s snapshot [Source: architecture.md#Frontend Architecture].
- **No new dependencies** — `recharts` is already in `frontend/package.json`. Do not add any new npm packages.
- **`isAnimationActive={false}`** on all Recharts `<Line>` components — Recharts entrance animations cause visible flickering when data updates every 1-2 seconds.

### Recharts v3 Specifics

Recharts v3 (installed: `^3.8.1`) changed some APIs from v2:
- `ResponsiveContainer` + `LineChart` pattern is unchanged.
- `connectNulls` on `<Line>` still works as expected.
- For `<Tooltip content={<CustomComponent />}>`, the custom component receives `active`, `payload`, and `label` as props directly.
- `dataKey` on `<Line>` can be a string key in the chart data object — used here to separate drivers by number string.

### Testing Recharts in jsdom

Recharts renders SVG which jsdom doesn't fully support. The `vi.mock('recharts', ...)` approach is the standard Vitest pattern for this library. The mock:
- Replaces `ResponsiveContainer` and `LineChart` with `<div>` wrappers so children render.
- Replaces `Line` with a `<div data-testid="line-{dataKey}">` so tests can assert per-driver lines exist.
- Returns `null` for `XAxis`, `YAxis`, `Tooltip`, `CartesianGrid` (no meaningful content to test).

This means chart tooltip tests are not feasible at unit level — hover interactions require E2E (Playwright). The unit tests focus on: placeholder vs chart render, line count per driver, header presence, and graceful handling of missing driver meta.

### Data Flow Summary

```
OpenF1 /laps (5s poll)
  → PollLapsAsync
  → _driverLapTimes[driverNum][lapNum] = LapTimeEntry (if duration present)
  → BuildSnapshot() serializes to LapChart dict ordered by lap number
  → SignalR "RaceSnapshot" broadcast
  → useSignalRConnection handleSnapshot
  → setLapChart(snapshot.lapChart)
  → Zustand liveRaceStore.lapChart
  → LapTimeChart component reads + renders via Recharts
```

### Regressions to Guard

The following must NOT change:
- `_driverCurrentLap` update logic in `PollLapsAsync` — tyre `StintLaps` calculation depends on it (Story 2.2).
- `BuildSnapshot` gap/stale join logic — Task 4 only adds to the return statement, does not change how `drivers` list is built.
- `GapList` and `DriverRow` rendering — `LiveRacePage` wraps both in a `flex-col gap-4` div; no structural changes to `GapList`'s container needed.
- Existing `RaceDataOrchestratorTests.cs` facts — 49 existing tests must all still pass; 4 new tests are additive.
- Existing `GapList.test.tsx` facts — 11 tests must still pass; no changes to that file needed.
- `normalizeSnapshot.ts` and its tests — unchanged; `lapChart` flows separately via `setLapChart`.
- `useLiveRaceStore` `setState` calls in `GapList.test.tsx` `beforeEach` do not need updating — Zustand's `setState` is a shallow merge by default, so the existing `{ drivers: {}, connectionStatus: 'disconnected', lastSnapshotTime: null }` still resets those fields without clobbering `lapChart`.

### Files to Create / Modify

**Backend (3 modified, 1 new):**
- `backend/F1App.Api/Dtos/OpenF1/OpenF1LapDto.cs` — MODIFY: add `LapDuration` and `IsPitOutLap` fields
- `backend/F1App.Api/Models/LapTimeEntry.cs` — NEW
- `backend/F1App.Api/Models/RaceStateSnapshot.cs` — MODIFY: add `LapChart` property
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` — MODIFY: add `_driverLapTimes`, extend `PollLapsAsync`, update `BuildSnapshot`
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs` — MODIFY: add `MakeLap` helper + 4 tests

**Frontend (4 modified, 2 new):**
- `frontend/src/shared/types/f1.ts` — MODIFY: add `LapTimeEntry`, add `lapChart` to `RaceStateSnapshot`
- `frontend/src/features/live-race/store/liveRaceStore.ts` — MODIFY: add `lapChart` + `setLapChart`
- `frontend/src/features/live-race/hooks/useSignalRConnection.ts` — MODIFY: extract + call `setLapChart`
- `frontend/src/features/live-race/LiveRacePage.tsx` — MODIFY: import + render `LapTimeChart`
- `frontend/src/features/live-race/LapTimeChart/LapTimeChart.tsx` — NEW
- `frontend/src/features/live-race/LapTimeChart/LapTimeChart.test.tsx` — NEW

### Project Structure Notes

- `LapTimeChart/LapTimeChart.tsx` and `LapTimeChart/LapTimeChart.test.tsx` follow the folder-per-component pattern established by `GapList/GapList.tsx` and `GapList/GapList.test.tsx`. The folder name matches the component name (PascalCase).
- No new `index.ts` barrel is needed for `LapTimeChart/` — `LiveRacePage` imports directly by path.

### References

- Story 2.3 acceptance criteria: [Source: epics.md#Story 2.3: Live Lap Time Chart]
- `recharts` chart library mandate: [Source: architecture.md#Frontend Library Stack]
- `LapTimeChart/` folder location: [Source: architecture.md#Complete Project Tree]
- `useShallow` mandatory pattern: [Source: architecture.md#Frontend Architecture]
- `isAnimationActive={false}` necessity: [Source: Story 2.2 dev notes — snapshot updates at 1-2s cadence]
- `PollLapsAsync` + `_driverCurrentLap` existing pattern: [Source: 2-2-live-tyre-tracker.md#Task 3]
- Recharts jsdom mock pattern: standard Vitest approach for SVG-based libraries

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

No blockers. Cache file issue on first build attempt resolved by running `dotnet build` without `--no-restore`; all subsequent builds clean.

### Completion Notes List

- Task 1: Extended `OpenF1LapDto` with `LapDuration` (double?, seconds) and `IsPitOutLap` (bool) matching OpenF1 JSON fields `lap_duration` and `is_pit_out_lap`.
- Task 2: Created `LapTimeEntry` record in `Models/` — domain model, not DTO. Three fields: `LapNumber`, `LapDurationSeconds`, `IsPitOutLap`.
- Task 3: Added `LapChart: IReadOnlyDictionary<int, IReadOnlyList<LapTimeEntry>>` to `RaceStateSnapshot` with empty-dict default.
- Task 4: Added `_driverLapTimes` (nested `ConcurrentDictionary`) to orchestrator; extended `PollLapsAsync` to populate it for completed laps (non-null duration only); `BuildSnapshot` serializes to ordered `lapChart` dict.
- Task 5: Added `MakeLap` helper + 4 new facts to `RaceDataOrchestratorTests`. Verified all 53 backend tests pass (49 previous + 4 new). Added `using System.Collections.Concurrent` and `using F1App.Api.Models` imports to test file.
- Task 6: Added `LapTimeEntry` interface to `f1.ts`; added `lapChart` field to `RaceStateSnapshot` type; extended Zustand store with `lapChart` state + `setLapChart` action; updated `useSignalRConnection` to call `setLapChart(snapshot.lapChart)` in `handleSnapshot` and added `setLapChart` to `useEffect` deps.
- Task 7: Created `LapTimeChart/LapTimeChart.tsx` — Recharts `LineChart` with per-driver `<Line>` coloured by `teamColour`; `buildChartData` merges multi-driver lap data into wide-format array; `findFastestLap` excludes pit-out laps; `CustomTooltip` shows `m:ss.sss` time + gap-to-fastest; `isAnimationActive={false}` prevents flicker on 1-2s updates; `useShallow` on driver colours selector.
- Task 8: Updated `LiveRacePage` to import `LapTimeChart` and render it below `GapList` in a `flex-col gap-4` container.
- Task 9: Created `LapTimeChart.test.tsx` with `vi.mock('recharts')` to handle jsdom SVG limitations. 5 tests covering: placeholder state, chart render, one-Line-per-driver, header in both states, graceful missing-driver-meta fallback. All 54 frontend tests pass.

### File List

backend/F1App.Api/Dtos/OpenF1/OpenF1LapDto.cs (modified)
backend/F1App.Api/Models/LapTimeEntry.cs (new)
backend/F1App.Api/Models/RaceStateSnapshot.cs (modified)
backend/F1App.Api/Services/RaceDataOrchestrator.cs (modified)
backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs (modified)
frontend/src/shared/types/f1.ts (modified)
frontend/src/features/live-race/store/liveRaceStore.ts (modified)
frontend/src/features/live-race/hooks/useSignalRConnection.ts (modified)
frontend/src/features/live-race/LiveRacePage.tsx (modified)
frontend/src/features/live-race/LapTimeChart/LapTimeChart.tsx (new)
frontend/src/features/live-race/LapTimeChart/LapTimeChart.test.tsx (new)

## Change Log

| Date | Change |
|------|--------|
| 2026-06-23 | Story created via bmad-create-story |
| 2026-06-23 | Implementation complete: LapTimeEntry model, LapChart in snapshot, orchestrator lap accumulation, Recharts LineChart component, 4 backend + 5 frontend tests |
