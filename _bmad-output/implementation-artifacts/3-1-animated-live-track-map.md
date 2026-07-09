---
baseline_commit: ""
---

# Story 3.1: Animated Live Track Map

Status: ready-for-dev

## Story

As a race-day fan,
I want to see an animated track map with driver dots positioned in real time,
so that I can visualise the race like a broadcast.

## Acceptance Criteria

1. **Given** a live session **When** the track map renders **Then** an SVG circuit layout shows driver dots positioned from OpenF1 real-time x/y coordinates, transformed via the per-circuit affine config (`public/circuit-configs/{circuitId}.json`).
2. **Given** coordinate updates arrive via SignalR snapshots **When** rendered **Then** movement is smoothly interpolated client-side between snapshot samples so dots glide rather than jump; each dot shows racing number and is coloured by team colour.
3. **Given** a circuit with no calibrated config asset yet **When** loading **Then** a clear "Track map unavailable for this circuit" state is shown instead of broken/invisible positions.
4. **Given** `sessionMode === 'fallback'` (no live session) **When** rendered **Then** the TrackMap panel is hidden (fallback drivers have no x/y coordinates).
5. **Given** the page is on a race weekend **When** the TrackMap loads **Then** it is visible and on-screen within 10 seconds of session start (satisfied by the existing polling bootstrap cadence).

## Tasks / Subtasks

### Task 1: Backend — `OpenF1LocationDto` and `GetLatestLocationsAsync` (AC: 1)

- [ ] Create `backend/F1App.Api/Dtos/OpenF1/OpenF1LocationDto.cs`:
  ```csharp
  using System.Text.Json.Serialization;

  namespace F1App.Api.Dtos.OpenF1;

  public record OpenF1LocationDto(
      [property: JsonPropertyName("driver_number")] int DriverNumber,
      [property: JsonPropertyName("x")] double X,
      [property: JsonPropertyName("y")] double Y,
      [property: JsonPropertyName("date")] DateTimeOffset Date
  );
  ```
  **OpenF1 `/location` response shape:** `[{ "driver_number": 1, "x": -1500.3, "y": 823.1, "z": 0.0, "date": "2024-06-08T12:34:56.123Z", "session_key": 9159 }, ...]`
  The `z` field (altitude) is ignored — not declared in the DTO so System.Text.Json ignores it by default.

- [ ] Add to `backend/F1App.Api/Clients/IOpenF1Client.cs`:
  ```csharp
  Task<IReadOnlyList<OpenF1LocationDto>> GetLatestLocationsAsync(DateTimeOffset since, CancellationToken ct);
  ```

- [ ] Implement in `backend/F1App.Api/Clients/OpenF1Client.cs`:
  ```csharp
  public async Task<IReadOnlyList<OpenF1LocationDto>> GetLatestLocationsAsync(DateTimeOffset since, CancellationToken ct)
  {
      var url = since == DateTimeOffset.MinValue
          ? "location?session_key=latest"
          : $"location?session_key=latest&date>{since:yyyy-MM-ddTHH:mm:ss.fff}";

      return await httpClient.GetFromJsonAsync<IReadOnlyList<OpenF1LocationDto>>(url, ct) ?? [];
  }
  ```

### Task 2: Backend — Add `X`, `Y` to `DriverState` and `CircuitId` to `RaceStateSnapshot` (AC: 1, 4)

- [ ] Update `backend/F1App.Api/Models/DriverState.cs` — add optional coordinate fields:
  ```csharp
  public double? X { get; init; }
  public double? Y { get; init; }
  ```
  **Backward compatibility:** both default to `null`. Existing `BuildSnapshot` tests that don't set location data see `null` — no breakage. Frontend renders dot only when both are non-null.

- [ ] Update `backend/F1App.Api/Models/RaceStateSnapshot.cs` — add circuit identity field:
  ```csharp
  public string? CircuitId { get; init; }
  ```
  **Populated by:** `RaceDataOrchestrator._activeCircuitId` (set when race weekend detection finds the active race — see Task 3). Null when `Polling:ForceActive = true` or race detection fails.

### Task 3: Backend — Location polling loop and circuit capture in `RaceDataOrchestrator` (AC: 1, 2)

- [ ] Add fields to `RaceDataOrchestrator` after `_standingByPrefix`:
  ```csharp
  internal readonly ConcurrentDictionary<int, OpenF1LocationDto> _latestLocations = new();
  internal string? _activeCircuitId;
  private DateTimeOffset _lastLocationPoll = DateTimeOffset.MinValue;
  ```

- [ ] Update `IsRaceWeekendActiveAsync` to capture `_activeCircuitId` when active race is found. Replace the LINQ expression:
  ```csharp
  _raceWeekendActive = raceTable.Races.Any(race =>
  {
      var raceDate = DateTime.ParseExact(race.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;
      var weekendStart = race.FirstPractice is not null
          ? DateTime.ParseExact(race.FirstPractice.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date
          : raceDate.AddDays(-2);
      return today >= weekendStart && today <= raceDate;
  });
  ```
  With:
  ```csharp
  _raceWeekendActive = false;
  foreach (var race in raceTable.Races)
  {
      var raceDate = DateTime.ParseExact(race.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;
      var weekendStart = race.FirstPractice is not null
          ? DateTime.ParseExact(race.FirstPractice.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date
          : raceDate.AddDays(-2);
      if (today >= weekendStart && today <= raceDate)
      {
          _raceWeekendActive = true;
          _activeCircuitId = race.Circuit.CircuitId;
          break;
      }
  }
  ```

- [ ] Add `PollLocationAsync` method (after `PollLapsAsync`):
  ```csharp
  private async Task PollLocationAsync(CancellationToken ct)
  {
      var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(800));
      while (await timer.WaitForNextTickAsync(ct))
      {
          var locations = await openF1Client.GetLatestLocationsAsync(_lastLocationPoll, ct);
          if (locations.Count > 0)
              _lastLocationPoll = timeProvider.GetUtcNow();

          foreach (var loc in locations)
          {
              _latestLocations.AddOrUpdate(
                  loc.DriverNumber,
                  loc,
                  (_, existing) => loc.Date > existing.Date ? loc : existing);
          }
      }
  }
  ```

- [ ] Add `"LocationPoller"` to the `Task.WhenAll` in `ExecuteAsync`:
  ```csharp
  await Task.WhenAll(
      RunLoopAsync("PositionPoller",  PollPositionAsync,         sessionCts.Token),
      RunLoopAsync("IntervalPoller",  PollIntervalAsync,         sessionCts.Token),
      RunLoopAsync("StintsPoller",    PollStintsAsync,           sessionCts.Token),
      RunLoopAsync("LapsPoller",      PollLapsAsync,             sessionCts.Token),
      RunLoopAsync("LocationPoller",  PollLocationAsync,         sessionCts.Token),
      RunLoopAsync("PublishLoop",     PublishSnapshotLoopAsync,  sessionCts.Token)
  );
  ```

- [ ] Update `BuildSnapshot` to populate `X`, `Y`, and `CircuitId`. In the live/stale driver assembly loop, add after `StintLaps` is computed:
  ```csharp
  double? x = null, y = null;
  if (_latestLocations.TryGetValue(driverNum, out var loc))
  {
      x = loc.X;
      y = loc.Y;
  }
  ```
  And in `drivers.Add(new DriverState { ... })`:
  ```csharp
  X = x,
  Y = y,
  ```
  And in the `return new RaceStateSnapshot { ... }`:
  ```csharp
  CircuitId = _activeCircuitId,
  ```

### Task 4: Backend — Contract test and orchestrator tests (AC: 1, 2)

- [ ] Add to `backend/F1App.Api.Tests/Clients/ErgastClientContractTests.cs` — wait, this is OpenF1. Add to a new or existing `OpenF1ClientContractTests.cs` file. Check if it exists first:

  Create `backend/F1App.Api.Tests/Clients/OpenF1ClientContractTests.cs` (if not present):
  ```csharp
  using F1App.Api.Clients;
  using WireMock.RequestBuilders;
  using WireMock.ResponseBuilders;
  using WireMock.Server;

  namespace F1App.Api.Tests.Clients;

  public class OpenF1ClientContractTests : IDisposable
  {
      private readonly WireMockServer _server = WireMockServer.Start();

      [Fact]
      public async Task GetLatestLocationsAsync_ParsesDriverCoordinates()
      {
          _server
              .Given(Request.Create().WithPath("/location").WithParam("session_key", "latest").UsingGet())
              .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new[]
              {
                  new { driver_number = 1, x = -1500.3, y = 823.1, z = 0.0, date = "2024-06-08T12:34:56.123Z", session_key = 9159 },
                  new { driver_number = 4, x = -1480.0, y = 810.5, z = 0.0, date = "2024-06-08T12:34:56.200Z", session_key = 9159 },
              }));

          using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
          var client = new OpenF1Client(httpClient);

          var locs = await client.GetLatestLocationsAsync(DateTimeOffset.MinValue, CancellationToken.None);

          Assert.Equal(2, locs.Count);
          Assert.Equal(1, locs[0].DriverNumber);
          Assert.Equal(-1500.3, locs[0].X);
          Assert.Equal(823.1, locs[0].Y);
          Assert.Equal(4, locs[1].DriverNumber);
      }

      [Fact]
      public async Task GetLatestLocationsAsync_UsesDateFilterWhenSinceProvided()
      {
          var since = new DateTimeOffset(2024, 6, 8, 12, 34, 0, TimeSpan.Zero);
          _server
              .Given(Request.Create()
                  .WithPath("/location")
                  .WithParam("session_key", "latest")
                  .UsingGet())
              .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(Array.Empty<object>()));

          using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
          var client = new OpenF1Client(httpClient);

          var locs = await client.GetLatestLocationsAsync(since, CancellationToken.None);

          Assert.Empty(locs);
      }

      public void Dispose() => _server.Stop();
  }
  ```

- [ ] Add to `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`:
  ```csharp
  // Helper — add alongside MakePosition, MakeInterval etc.
  private static OpenF1LocationDto MakeLocation(int driverNum, double x, double y, DateTimeOffset date) =>
      new(driverNum, x, y, date);

  [Fact]
  public void BuildSnapshot_WithLocationData_PopulatesXYOnDriverState()
  {
      var now = DateTimeOffset.UtcNow;
      var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
      sut._latestPositions[1] = MakePosition(1, 1, now);
      sut._latestLocations[1] = MakeLocation(1, -1500.3, 823.1, now);

      var snapshot = sut.BuildSnapshot();

      Assert.Single(snapshot.Drivers);
      Assert.Equal(-1500.3, snapshot.Drivers[0].X);
      Assert.Equal(823.1, snapshot.Drivers[0].Y);
  }

  [Fact]
  public void BuildSnapshot_WithoutLocationData_XYAreNull()
  {
      var now = DateTimeOffset.UtcNow;
      var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
      sut._latestPositions[1] = MakePosition(1, 1, now);
      // No entry in _latestLocations

      var snapshot = sut.BuildSnapshot();

      Assert.Single(snapshot.Drivers);
      Assert.Null(snapshot.Drivers[0].X);
      Assert.Null(snapshot.Drivers[0].Y);
  }

  [Fact]
  public void BuildSnapshot_ActiveCircuitId_IncludedInSnapshot()
  {
      var sut = CreateOrchestrator();
      sut._activeCircuitId = "monza";

      var snapshot = sut.BuildSnapshot();

      Assert.Equal("monza", snapshot.CircuitId);
  }
  ```

### Task 5: Frontend — Types and circuit config asset (AC: 1, 3)

- [ ] Update `frontend/src/shared/types/f1.ts` — extend `DriverState` with optional coordinates, and `RaceStateSnapshot` with `circuitId`:
  ```ts
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
    x: number | null        // NEW — raw OpenF1 x coordinate (meters)
    y: number | null        // NEW — raw OpenF1 y coordinate (meters)
  }

  export interface RaceStateSnapshot {
    capturedAt: string
    drivers: DriverState[]
    lapChart: Record<string, LapTimeEntry[]>
    sessionMode: 'live' | 'stale' | 'fallback'
    fallbackRaceName: string | null
    circuitId: string | null   // NEW
  }
  ```

- [ ] Create `frontend/public/circuit-configs/monza.json`:
  ```json
  {
    "circuitId": "monza",
    "viewBox": "0 0 900 600",
    "transform": {
      "scaleX": 0.0023,
      "scaleY": -0.0023,
      "translateX": 450,
      "translateY": 380,
      "rotationDeg": 15
    },
    "trackPath": "M150,300 L150,170 Q150,140 210,130 L600,90 Q650,85 655,135 L650,200 Q645,230 600,228 L420,225 Q400,225 400,260 L402,290 Q404,320 370,322 L180,322 Q150,322 150,300 Z"
  }
  ```
  **Notes:**
  - `trackPath` is in SVG viewport coordinates (0 0 900 600) — a hand-crafted approximation of Monza's circuit outline for visual context. Accuracy sufficient for POC.
  - `transform` values from [Source: _bmad-output/planning-artifacts/architecture.md#Gap Analysis Results]. Production calibration requires recording live OpenF1 x/y samples at known track landmarks.
  - New circuits: drop a calibrated JSON file here — no code change required.

### Task 6: Frontend — `useTrackInterpolation` hook (AC: 2)

- [ ] Create `frontend/src/features/live-race/TrackMap/useTrackInterpolation.ts`:
  ```ts
  import { useEffect, useRef, useState } from 'react'
  import type { DriverState } from '../../../shared/types/f1'

  export interface InterpolatedPosition {
    driverNumber: number
    driverCode: string
    teamColour: string
    svgX: number
    svgY: number
  }

  interface Transform {
    scaleX: number
    scaleY: number
    translateX: number
    translateY: number
    rotationDeg: number
  }

  function applyTransform(rawX: number, rawY: number, t: Transform): [number, number] {
    const rad = (t.rotationDeg * Math.PI) / 180
    const rotX = rawX * Math.cos(rad) - rawY * Math.sin(rad)
    const rotY = rawX * Math.sin(rad) + rawY * Math.cos(rad)
    return [rotX * t.scaleX + t.translateX, rotY * t.scaleY + t.translateY]
  }

  interface Sample {
    x: number
    y: number
    receivedAt: number // performance.now() timestamp
  }

  export function useTrackInterpolation(
    drivers: Record<string, DriverState>,
    transform: Transform | null
  ): InterpolatedPosition[] {
    // Ring buffer: last 2 samples per driver (enough for linear interpolation)
    const samplesRef = useRef<Map<number, [Sample, Sample | null]>>(new Map())
    const [positions, setPositions] = useState<InterpolatedPosition[]>([])
    const rafRef = useRef<number>(0)

    // Ingest new snapshot data into the sample buffer
    useEffect(() => {
      const now = performance.now()
      for (const driver of Object.values(drivers)) {
        if (driver.x == null || driver.y == null) continue
        const newSample: Sample = { x: driver.x, y: driver.y, receivedAt: now }
        const existing = samplesRef.current.get(driver.driverNumber)
        if (!existing) {
          samplesRef.current.set(driver.driverNumber, [newSample, null])
        } else {
          samplesRef.current.set(driver.driverNumber, [newSample, existing[0]])
        }
      }
    }, [drivers])

    // rAF render loop — interpolates between last two samples
    useEffect(() => {
      if (!transform) return

      const tick = () => {
        const now = performance.now()
        const result: InterpolatedPosition[] = []

        for (const driver of Object.values(drivers)) {
          const entry = samplesRef.current.get(driver.driverNumber)
          if (!entry) continue
          const [newest, prev] = entry

          let rawX: number
          let rawY: number

          if (!prev) {
            rawX = newest.x
            rawY = newest.y
          } else {
            const span = newest.receivedAt - prev.receivedAt
            const elapsed = now - prev.receivedAt
            const alpha = span > 0 ? Math.min(1, elapsed / span) : 1
            rawX = prev.x + alpha * (newest.x - prev.x)
            rawY = prev.y + alpha * (newest.y - prev.y)
          }

          const [svgX, svgY] = applyTransform(rawX, rawY, transform)
          result.push({
            driverNumber: driver.driverNumber,
            driverCode: driver.driverCode,
            teamColour: driver.teamColour,
            svgX,
            svgY,
          })
        }

        setPositions(result)
        rafRef.current = requestAnimationFrame(tick)
      }

      rafRef.current = requestAnimationFrame(tick)
      return () => cancelAnimationFrame(rafRef.current)
    }, [drivers, transform])

    return positions
  }
  ```
  **Key design decisions:**
  - Stores only the 2 most recent samples per driver (newest + previous). Linear interpolation between them.
  - `alpha` is clamped to [0, 1]: when elapsed > span (a new snapshot is late), dots snap to the last known position instead of extrapolating off-screen.
  - `performance.now()` timestamps (ms, monotonic) avoid wall-clock drift between snapshot arrivals.
  - Drivers with `x == null || y == null` (fallback mode, no location data) are skipped — only drivers with valid coordinates appear on the map.

### Task 7: Frontend — `DriverDot` component (AC: 1, 2)

- [ ] Create `frontend/src/features/live-race/TrackMap/DriverDot.tsx`:
  ```tsx
  interface DriverDotProps {
    driverCode: string
    teamColour: string
    svgX: number
    svgY: number
  }

  export function DriverDot({ driverCode, teamColour, svgX, svgY }: DriverDotProps) {
    return (
      <g transform={`translate(${svgX},${svgY})`} data-testid={`driver-dot-${driverCode}`}>
        <circle r={9} fill={`#${teamColour}`} stroke="#0c0e11" strokeWidth={2} />
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

### Task 8: Frontend — `TrackMap` component (AC: 1, 2, 3, 4)

- [ ] Create `frontend/src/features/live-race/TrackMap/TrackMap.tsx`:
  ```tsx
  import { useEffect, useState } from 'react'
  import { useLiveRaceStore } from '../store/liveRaceStore'
  import { useFallbackState } from '../hooks/useFallbackState'
  import { useTrackInterpolation } from './useTrackInterpolation'
  import { DriverDot } from './DriverDot'

  interface CircuitConfig {
    circuitId: string
    viewBox: string
    transform: {
      scaleX: number
      scaleY: number
      translateX: number
      translateY: number
      rotationDeg: number
    }
    trackPath: string
  }

  const apiBase = import.meta.env.VITE_API_BASE_URL ?? ''

  interface TrackMapProps {
    circuitId: string | null
  }

  export function TrackMap({ circuitId }: TrackMapProps) {
    const drivers = useLiveRaceStore(s => s.drivers)
    const { isFallback } = useFallbackState()

    const [config, setConfig] = useState<CircuitConfig | null>(null)
    const [unavailable, setUnavailable] = useState(false)

    useEffect(() => {
      if (!circuitId) return
      setConfig(null)
      setUnavailable(false)

      fetch(`${apiBase}/circuit-configs/${circuitId}.json`)
        .then(r => {
          if (!r.ok) throw new Error('not found')
          return r.json() as Promise<CircuitConfig>
        })
        .then(setConfig)
        .catch(() => setUnavailable(true))
    }, [circuitId])

    const positions = useTrackInterpolation(isFallback ? {} : drivers, config?.transform ?? null)

    if (isFallback) return null

    return (
      <div
        className="bg-[#1a1e26] border border-[#2c313b] rounded-[12px] overflow-hidden"
        data-testid="track-map"
      >
        <div className="px-4 pt-3 pb-1 flex justify-between items-baseline">
          <span className="text-[11px] font-semibold tracking-[0.04em] uppercase text-[#6b7280]">
            Circuit
          </span>
          {circuitId && (
            <span className="text-[11px] text-[#9aa1ad] capitalize">
              {circuitId.replace(/_/g, ' ')}
            </span>
          )}
        </div>

        {!circuitId || unavailable ? (
          <div
            className="flex items-center justify-center h-[200px] text-[12px] text-[#6b7280]"
            data-testid="track-map-unavailable"
          >
            Track map unavailable for this circuit
          </div>
        ) : !config ? (
          <div className="flex items-center justify-center h-[200px] text-[12px] text-[#6b7280]">
            Loading…
          </div>
        ) : (
          <svg
            viewBox={config.viewBox}
            className="w-full"
            style={{ maxHeight: 230 }}
            data-testid="track-map-svg"
          >
            <path
              d={config.trackPath}
              fill="none"
              stroke="#2c313b"
              strokeWidth={9}
              strokeLinejoin="round"
              strokeLinecap="round"
            />
            {positions.map(p => (
              <DriverDot
                key={p.driverNumber}
                driverCode={p.driverCode}
                teamColour={p.teamColour}
                svgX={p.svgX}
                svgY={p.svgY}
              />
            ))}
          </svg>
        )}
      </div>
    )
  }
  ```

### Task 9: Frontend — `liveRaceStore` circuitId and `useSignalRConnection` update (AC: 1)

- [ ] Update `frontend/src/features/live-race/store/liveRaceStore.ts` — add `circuitId` state:
  ```ts
  // Add to LiveRaceState interface:
  circuitId: string | null
  setCircuitId: (id: string | null) => void

  // Add to initial state:
  circuitId: null,

  // Add action:
  setCircuitId: (id) => set({ circuitId: id }),
  ```

- [ ] Update `frontend/src/features/live-race/hooks/useSignalRConnection.ts` — forward `circuitId` from snapshot:
  ```ts
  // Add alongside other store selectors:
  const setCircuitId = useLiveRaceStore(s => s.setCircuitId)

  // In handleSnapshot, after setSessionMode:
  if (snapshot.circuitId) setCircuitId(snapshot.circuitId)
  ```

- [ ] Update `frontend/src/features/live-race/LiveRacePage.tsx` — read `circuitId` from store and render `TrackMap`:
  ```tsx
  import { TrackMap } from './TrackMap/TrackMap'

  // Inside LiveRacePage:
  const circuitId = useLiveRaceStore(s => s.circuitId)

  // Add TrackMap in the layout (new right column or stacked below):
  <div className="flex flex-col gap-4">
    <GapList />
    <TrackMap circuitId={circuitId} />
    <LapTimeChart />
  </div>
  ```

### Task 10: Frontend — Tests (AC: 1, 2, 3, 4)

- [ ] Create `frontend/src/features/live-race/TrackMap/TrackMap.test.tsx`:
  ```tsx
  import { render, screen, waitFor } from '@testing-library/react'
  import { beforeEach, describe, expect, it, vi } from 'vitest'
  import { TrackMap } from './TrackMap'
  import { useLiveRaceStore } from '../store/liveRaceStore'

  const mockConfig = {
    circuitId: 'monza',
    viewBox: '0 0 900 600',
    transform: { scaleX: 0.0023, scaleY: -0.0023, translateX: 450, translateY: 380, rotationDeg: 0 },
    trackPath: 'M150,300 L600,300 Z',
  }

  beforeEach(() => {
    useLiveRaceStore.setState({
      drivers: {},
      connectionStatus: 'disconnected',
      lastSnapshotTime: null,
      sessionMode: 'live',
      fallbackRaceName: null,
      circuitId: null,
    })
    vi.resetAllMocks()
    global.fetch = vi.fn()
  })

  describe('TrackMap', () => {
    it('shows unavailable message when circuitId is null', () => {
      render(<TrackMap circuitId={null} />)
      expect(screen.getByTestId('track-map-unavailable')).toBeInTheDocument()
    })

    it('shows unavailable message when circuit config fetch fails', async () => {
      vi.mocked(global.fetch).mockResolvedValue({ ok: false } as Response)
      render(<TrackMap circuitId="unknowncircuit" />)
      await waitFor(() =>
        expect(screen.getByTestId('track-map-unavailable')).toBeInTheDocument()
      )
    })

    it('renders SVG when config loads successfully', async () => {
      vi.mocked(global.fetch).mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(mockConfig),
      } as Response)
      render(<TrackMap circuitId="monza" />)
      await waitFor(() => expect(screen.getByTestId('track-map-svg')).toBeInTheDocument())
    })

    it('returns null in fallback mode', () => {
      useLiveRaceStore.setState({ sessionMode: 'fallback' })
      const { container } = render(<TrackMap circuitId="monza" />)
      expect(container.firstChild).toBeNull()
    })

    it('renders driver dots for drivers with x/y coordinates', async () => {
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
          },
        },
      })
      render(<TrackMap circuitId="monza" />)
      await waitFor(() => expect(screen.getByTestId('track-map-svg')).toBeInTheDocument())
      // Driver dot appears after rAF tick — use waitFor with a relaxed timeout
      await waitFor(
        () => expect(screen.getByTestId('driver-dot-VER')).toBeInTheDocument(),
        { timeout: 2000 }
      )
    })
  })
  ```

- [ ] Update `frontend/src/features/live-race/LiveRacePage.test.tsx` `beforeEach` to include `circuitId: null` in `useLiveRaceStore.setState({...})`.

- [ ] Update `frontend/src/features/live-race/GapList/GapList.test.tsx` `beforeEach` to include `circuitId: null`.

- [ ] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass.
- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass.

## Dev Notes

### Architecture Alignment

- **Location URL pattern** follows existing OpenF1Client conventions exactly: `location?session_key=latest&date>{since:...}` (same format as `position` and `intervals`).
- **`_latestLocations` ConcurrentDictionary** follows the same pattern as `_latestPositions`, `_latestIntervals`, `_latestStints`.
- **`CircuitId` in `RaceStateSnapshot`** is populated from `_activeCircuitId` which is set during `IsRaceWeekendActiveAsync`. When `Polling:ForceActive = true` (local dev), the race detection loop doesn't find an active race (no matching date) so `_activeCircuitId` stays null — `TrackMap` shows the unavailable state. To test locally, override `circuitId` in store state via browser devtools.
- **`TrackMap` location:** `frontend/src/features/live-race/TrackMap/` — per architecture file-tree at `architecture.md:496-500`.
- **`circuit-configs/` location:** `frontend/public/circuit-configs/` — served as a static asset by Vite dev server and production build. No API route needed.
- **`useTrackInterpolation`** is in the `TrackMap/` folder (co-located with the component that uses it), per architecture: `architecture.md:500`.

### Transform Math

The affine transform in `useTrackInterpolation.applyTransform` applies:
1. **Rotation** (in-place around origin): `x' = x*cos(θ) - y*sin(θ)`, `y' = x*sin(θ) + y*cos(θ)`
2. **Scale + translate**: `svgX = x' * scaleX + translateX`, `svgY = y' * scaleY + translateY`

`scaleY` is negative (e.g., `-0.0023`) because OpenF1 y-coordinates increase northward, but SVG y-coordinates increase downward.

### Circuit Config Calibration Note

The Monza config values come from [Source: _bmad-output/planning-artifacts/architecture.md#Gap Analysis Results]. These are *initial POC estimates* — the exact values for production accuracy require recording live OpenF1 location samples at known track landmarks (pit lane entry, turn apexes) and solving the affine system. This is expected post-POC work.

### Fallback Mode Behaviour

- `TrackMap` returns `null` when `isFallback === true` (AC: 4). Fallback drivers (from Ergast historical results) have no x/y coordinates, and showing an empty map with no dots would be confusing.
- The store's `circuitId` is not reset when entering fallback mode — it stays as the last known active circuit. This means if a live session recovers, the circuit config is already loaded.

### Testing Approach for rAF

`useTrackInterpolation` uses `requestAnimationFrame`. In jsdom (Vitest), `requestAnimationFrame` is mocked as `setTimeout(cb, 0)`. This means the `waitFor` with a `2000ms` timeout in `TrackMap.test.tsx` is enough to let the rAF callback fire and the dot appear in the DOM.

### Regressions to Guard

- **`GapList.test.tsx` and `LiveRacePage.test.tsx` `beforeEach`** must be updated to include `circuitId: null` in `useLiveRaceStore.setState`. Otherwise tests that run after a test setting `circuitId: 'monza'` will see stale state.
- **`DriverState` type in `f1.ts`** — `x` and `y` are now required fields in the interface (typed as `number | null`). Check that `makeDriver` helpers in test files explicitly set them (add `x: null, y: null` to defaults).
- **`normalizeSnapshot`** (`shared/utils/normalizeSnapshot.ts`) — just indexes by driverNumber, no changes needed. New `x`/`y` fields pass through automatically.
- **`LastRaceResult.drivers`** (from Ergast) — the REST endpoint `/api/races/last-result` returns `DriverState` objects from the server. The new `X`/`Y` fields default to `null` so the JSON response includes `"x": null, "y": null`. Frontend `DriverState` interface expects these fields, so no change needed in `useLastRaceResult`.

### Files to Create / Modify

**Backend NEW:**
- `backend/F1App.Api/Dtos/OpenF1/OpenF1LocationDto.cs`
- `backend/F1App.Api.Tests/Clients/OpenF1ClientContractTests.cs`

**Backend MODIFY:**
- `backend/F1App.Api/Clients/IOpenF1Client.cs` — add `GetLatestLocationsAsync`
- `backend/F1App.Api/Clients/OpenF1Client.cs` — implement `GetLatestLocationsAsync`
- `backend/F1App.Api/Models/DriverState.cs` — add `X`, `Y`
- `backend/F1App.Api/Models/RaceStateSnapshot.cs` — add `CircuitId`
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` — add fields, poll loop, circuit capture, BuildSnapshot updates
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs` — 3 new tests + `MakeLocation` helper

**Frontend NEW:**
- `frontend/public/circuit-configs/monza.json`
- `frontend/src/features/live-race/TrackMap/TrackMap.tsx`
- `frontend/src/features/live-race/TrackMap/DriverDot.tsx`
- `frontend/src/features/live-race/TrackMap/useTrackInterpolation.ts`
- `frontend/src/features/live-race/TrackMap/TrackMap.test.tsx`

**Frontend MODIFY:**
- `frontend/src/shared/types/f1.ts` — add `x`, `y` to `DriverState`; add `circuitId` to `RaceStateSnapshot`
- `frontend/src/features/live-race/store/liveRaceStore.ts` — add `circuitId`, `setCircuitId`
- `frontend/src/features/live-race/hooks/useSignalRConnection.ts` — forward `circuitId`
- `frontend/src/features/live-race/LiveRacePage.tsx` — add `TrackMap`
- `frontend/src/features/live-race/LiveRacePage.test.tsx` — update `beforeEach`
- `frontend/src/features/live-race/GapList/GapList.test.tsx` — update `beforeEach`

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
