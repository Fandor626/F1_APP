---
baseline_commit: 2a6ce9d
---

# Story 2.1: Live Gap List

Status: done

## Story

As a race-day fan,
I want to see all drivers in current race order with real-time gaps to the car ahead,
So that I can follow the race battle without needing F1 TV.

## Acceptance Criteria

1. **Given** a live session is in progress **When** the live race page loads **Then** it connects to the SignalR hub at `/hubs/race` and receives `RaceSnapshot` broadcasts every 1–2 seconds.
2. **Given** a snapshot is received **When** rendered **Then** all drivers render in current race order (1st → 20th) with their position, driver code, team colour chip, and gap to the car ahead.
3. **Given** a driver's gap to the car ahead is under 1 second **When** rendered **Then** that gap cell is visually highlighted as an active battle.
4. **Given** a new snapshot arrives with a different race order **When** rendered **Then** the list re-sorts automatically without any user interaction.
5. **Given** a timestamp join for a driver's interval misses the 500ms tolerance window **When** the snapshot is assembled **Then** the gap field for that driver is emitted as `null` with `gapIsStale: true`; the frontend renders `~` prefix or dimmed text instead of a confidently wrong number.
6. **Given** the SignalR connection drops **When** auto-reconnect fires **Then** the connection status updates from `connected` → `reconnecting` and back to `connected` when re-established; the page never shows an empty/broken state, only a status indicator update.

## Tasks / Subtasks

### Task 1: OpenF1 typed client + DTOs (AC: 1, 5)

- [x] Create `backend/F1App.Api/Clients/IOpenF1Client.cs`:
  ```csharp
  public interface IOpenF1Client
  {
      Task<IReadOnlyList<OpenF1PositionDto>> GetLatestPositionsAsync(DateTimeOffset since, CancellationToken ct);
      Task<IReadOnlyList<OpenF1IntervalDto>> GetLatestIntervalsAsync(DateTimeOffset since, CancellationToken ct);
      Task<IReadOnlyList<OpenF1DriverInfoDto>> GetSessionDriversAsync(CancellationToken ct);
  }
  ```
- [x] Create `backend/F1App.Api/Clients/OpenF1Client.cs`:
  - Primary constructor injects `HttpClient httpClient`
  - BaseAddress is set to `OpenF1BaseUrl` from config (trailing slash required, same pattern as ErgastClient)
  - For `GetLatestPositionsAsync`: GET `position?session_key=latest` when `since == DateTimeOffset.MinValue`; otherwise `position?session_key=latest&date>{since:yyyy-MM-ddTHH:mm:ss.fff}` — the `>` filter is part of the query string as-is (HttpClient encodes it correctly)
  - Same incremental pattern for `GetLatestIntervalsAsync` using `intervals?session_key=latest`
  - For `GetSessionDriversAsync`: GET `drivers?session_key=latest` — fetched once at session init, no date filter
  - Return `[]` (not throw) if the HTTP response deserialises to null — OpenF1 may return `[]` between sessions
- [x] Create `backend/F1App.Api/Dtos/OpenF1/OpenF1PositionDto.cs`:
  ```csharp
  // OpenF1 JSON shape (snake_case): driver_number, position, date, session_key, meeting_key
  public record OpenF1PositionDto(
      [property: JsonPropertyName("driver_number")] int DriverNumber,
      [property: JsonPropertyName("position")] int Position,
      [property: JsonPropertyName("date")] DateTimeOffset Date
  );
  ```
- [x] Create `backend/F1App.Api/Dtos/OpenF1/OpenF1IntervalDto.cs`:
  ```csharp
  // OpenF1 JSON shape: driver_number, gap_to_car_ahead (string|null), interval (string|null), date
  public record OpenF1IntervalDto(
      [property: JsonPropertyName("driver_number")] int DriverNumber,
      [property: JsonPropertyName("gap_to_car_ahead")] string? GapToCarAhead,
      [property: JsonPropertyName("date")] DateTimeOffset Date
  );
  ```
- [x] Create `backend/F1App.Api/Dtos/OpenF1/OpenF1DriverInfoDto.cs`:
  ```csharp
  // OpenF1 JSON shape: driver_number, name_acronym, team_name, team_colour
  public record OpenF1DriverInfoDto(
      [property: JsonPropertyName("driver_number")] int DriverNumber,
      [property: JsonPropertyName("name_acronym")] string NameAcronym,
      [property: JsonPropertyName("team_name")] string TeamName,
      [property: JsonPropertyName("team_colour")] string TeamColour  // hex without #, e.g. "FF8A1E"
  );
  ```

### Task 2: Domain models (AC: 2, 3, 5)

- [x] Create `backend/F1App.Api/Models/RaceStateSnapshot.cs`:
  ```csharp
  public record RaceStateSnapshot
  {
      public DateTimeOffset CapturedAt { get; init; }
      public IReadOnlyList<DriverState> Drivers { get; init; } = [];
  }
  ```
- [x] Create `backend/F1App.Api/Models/DriverState.cs`:
  ```csharp
  public record DriverState
  {
      public int DriverNumber { get; init; }
      public string DriverCode { get; init; } = "";       // e.g. "VER" from OpenF1 name_acronym
      public string TeamName { get; init; } = "";
      public string TeamColour { get; init; } = "555555"; // hex without #
      public int Position { get; init; }
      public string? GapToCarAhead { get; init; }         // null for leader; "1.234" for others
      public bool GapIsStale { get; init; }               // true when timestamp join missed tolerance
      // Placeholders for Stories 2.2–2.4 (not populated in this story):
      public string? TyreCompound { get; init; }
      public int? StintLaps { get; init; }
      public string? ChampionshipDelta { get; init; }
  }
  ```
- [x] Create `backend/F1App.Api/Models/FallbackTrigger.cs`:
  ```csharp
  // Not used by Story 2.1 logic yet — Story 2.5 reads this. Defined here so the
  // orchestrator can emit the correct enum value when a trigger is detected.
  public enum FallbackTrigger { Timeout, EmptyArray, StaleTimestamp }
  ```

### Task 3: SignalR hub (AC: 1, 6)

- [x] Create `backend/F1App.Api/Hubs/RaceHub.cs`:
  ```csharp
  using Microsoft.AspNetCore.SignalR;
  namespace F1App.Api.Hubs;

  public class RaceHub : Hub
  {
      public override async Task OnConnectedAsync()
      {
          await Groups.AddToGroupAsync(Context.ConnectionId, "race");
          await base.OnConnectedAsync();
      }

      public override async Task OnDisconnectedAsync(Exception? exception)
      {
          await Groups.RemoveFromGroupAsync(Context.ConnectionId, "race");
          await base.OnDisconnectedAsync(exception);
      }
  }
  ```
  Clients subscribe to the `"race"` group on connect. The orchestrator broadcasts to the group, not `All`, to avoid pushing to admin/monitoring connections if any are added later.

### Task 4: RaceDataOrchestrator — the real-time pipeline core (AC: 1, 2, 4, 5)

- [x] Create `backend/F1App.Api/Services/RaceDataOrchestrator.cs`:
  ```csharp
  public class RaceDataOrchestrator(
      IHubContext<RaceHub> hubContext,
      IOpenF1Client openF1Client,
      IConfiguration configuration,
      TimeProvider timeProvider,
      ILogger<RaceDataOrchestrator> logger) : BackgroundService
  {
      // ...
  }
  ```
  - `_joinTolerance` loaded from config: `configuration.GetValue("JoinToleranceMs", 500)` → `TimeSpan.FromMilliseconds()`
  - `_latestPositions`: `ConcurrentDictionary<int, OpenF1PositionDto>`
  - `_latestIntervals`: `ConcurrentDictionary<int, OpenF1IntervalDto>`
  - `_driverInfo`: `IReadOnlyDictionary<int, OpenF1DriverInfoDto>` — fetched once on start

- [x] `ExecuteAsync(CancellationToken stoppingToken)`:
  1. Call `await InitialiseDriverInfoAsync(stoppingToken)` — fetches driver info, logs count
  2. Start the three loops in parallel using `RunLoopAsync(name, loopFn, stoppingToken)` wrapper
  3. Await `Task.WhenAll(...)` — method only exits when stoppingToken fires

- [x] `RunLoopAsync(string name, Func<CancellationToken, Task> loop, CancellationToken ct)`:
  - While-loop around the inner function
  - Catches `OperationCanceledException` → break
  - Catches all other exceptions → `logger.LogError(ex, ...)` → continue (failure isolation per architecture)

- [x] `PollPositionAsync(CancellationToken ct)`:
  - `PeriodicTimer` at 800ms
  - Track `_lastPositionPoll` (DateTimeOffset, initially `DateTimeOffset.MinValue`)
  - On each tick: call `openF1Client.GetLatestPositionsAsync(_lastPositionPoll, ct)`
  - Update `_lastPositionPoll = timeProvider.GetUtcNow()`
  - For each result: `_latestPositions.AddOrUpdate(pos.DriverNumber, pos, (_, existing) => pos.Date > existing.Date ? pos : existing)`

- [x] `PollIntervalAsync(CancellationToken ct)`:
  - Same pattern as `PollPositionAsync` but using `_latestIntervals` and `GetLatestIntervalsAsync`
  - Offset timer slightly: 900ms (avoids lock contention with position poll)

- [x] `PublishSnapshotLoopAsync(CancellationToken ct)`:
  - `PeriodicTimer` at 1 second
  - Call `BuildSnapshot()` → if `Drivers.Count > 0`, broadcast:
    ```csharp
    await hubContext.Clients.Group("race").SendAsync("RaceSnapshot", snapshot, ct);
    ```

- [x] `BuildSnapshot()` (internal, testable via InternalsVisibleTo):
  - Iterate `_latestPositions`
  - For each driver: attempt join with `_latestIntervals[driverNum]`
  - Compute `timeDiff = Math.Abs((pos.Date - interval.Date).TotalMilliseconds)`
  - `gapIsStale = timeDiff > _joinTolerance.TotalMilliseconds`
  - If stale: `GapToCarAhead = null, GapIsStale = true`; else use `interval.GapToCarAhead`
  - Lookup driver info for code/team/colour (fallback: `driverNum.ToString()` / `""` / `"555555"`)
  - Return `new RaceStateSnapshot { CapturedAt = timeProvider.GetUtcNow(), Drivers = [...sorted by Position...] }`

### Task 5: Program.cs wiring (AC: 1, 6)

- [x] Add SignalR to `Program.cs`:
  ```csharp
  builder.Services.AddSignalR();
  ```
  (SignalR is part of ASP.NET Core 10 shared framework — no NuGet package needed)

- [x] Register `OpenF1Client` with `IHttpClientFactory`:
  ```csharp
  var openF1BaseUrl = builder.Configuration["OpenF1BaseUrl"]!.TrimEnd('/') + "/";
  builder.Services.AddHttpClient<IOpenF1Client, OpenF1Client>(client =>
  {
      client.BaseAddress = new Uri(openF1BaseUrl);
      client.Timeout = TimeSpan.FromSeconds(10);
  });
  ```

- [x] Register `TimeProvider` singleton (BCL, no package needed):
  ```csharp
  builder.Services.AddSingleton(TimeProvider.System);
  ```

- [x] Register `RaceDataOrchestrator` as hosted service:
  ```csharp
  builder.Services.AddHostedService<RaceDataOrchestrator>();
  ```

- [x] Update CORS policy — SignalR requires `AllowCredentials()`:
  ```csharp
  policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();   // ← required for SignalR WebSocket upgrade
  ```

- [x] Map the hub after `app.UseCors(...)`:
  ```csharp
  app.MapHub<RaceHub>("/hubs/race");
  ```
  Hub mapping must come AFTER `UseCors` — same ordering rule as REST controllers.

- [x] Add `OpenF1BaseUrl` to `appsettings.Development.json`:
  ```json
  "OpenF1BaseUrl": "https://api.openf1.org/v1",
  "JoinToleranceMs": 500
  ```

### Task 6: Backend tests (AC: 5 primarily)

- [x] Create `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`. Add `[assembly: InternalsVisibleTo("F1App.Api.Tests")]` to `F1App.Api` project (add to `Program.cs` or a dedicated `AssemblyInfo.cs`).
- [x] Tests:
  - `BuildSnapshot_EmptyPositions_ReturnsEmptyDriverList`: `_latestPositions` empty → `snapshot.Drivers.Count == 0`
  - `BuildSnapshot_DriverWithNoInterval_GapNullAndStale`: position exists, no matching interval → `GapToCarAhead == null`, `GapIsStale == true`
  - `BuildSnapshot_DriverWithIntervalWithinTolerance_GapNotStale`: position and interval timestamps differ by 400ms (< 500ms) → `GapIsStale == false`, `GapToCarAhead == "1.234"`
  - `BuildSnapshot_DriverWithIntervalOutsideTolerance_GapIsStale`: timestamps differ by 600ms → `GapIsStale == true`, `GapToCarAhead == null`
  - `BuildSnapshot_DriversOrderedByPosition`: 3 drivers with positions 3, 1, 2 → snapshot.Drivers ordered [1, 2, 3]
  - `BuildSnapshot_UsesTimeProvider_ForCapturedAt`: mock `TimeProvider` returning a fixed `DateTimeOffset` → `snapshot.CapturedAt` equals that value

  Use `Mock<IHubContext<RaceHub>>` and `Mock<IOpenF1Client>` for the constructor. For `TimeProvider`, use `new FakeTimeProvider()` (from `Microsoft.Extensions.TimeProvider.Testing` NuGet package — add to the test project only). If that package is unavailable, sub-class `TimeProvider` manually:
  ```csharp
  internal sealed class FakeTimeProvider(DateTimeOffset fixedTime) : TimeProvider
  {
      public override DateTimeOffset GetUtcNow() => fixedTime;
  }
  ```

### Task 7: Frontend shared types (AC: 2, 5)

- [x] Create `frontend/src/shared/types/f1.ts`:
  ```ts
  export interface DriverState {
    driverNumber: number
    driverCode: string      // "VER"
    teamName: string
    teamColour: string      // hex without "#", e.g. "FF8A1E"
    position: number
    gapToCarAhead: string | null   // null for leader or when stale
    gapIsStale: boolean
    // Placeholders — null until Stories 2.2–2.4:
    tyreCompound: string | null
    stintLaps: number | null
    championshipDelta: string | null
  }

  export interface RaceStateSnapshot {
    capturedAt: string   // ISO 8601
    drivers: DriverState[]
  }
  ```

- [x] Create `frontend/src/shared/types/signalR.ts`:
  ```ts
  import type { RaceStateSnapshot } from './f1'

  export interface RaceSnapshotMessage extends RaceStateSnapshot {}
  ```

### Task 8: normalizeSnapshot utility + test (AC: 2, 4)

- [x] Create `frontend/src/shared/utils/normalizeSnapshot.ts`:
  ```ts
  import type { DriverState } from '../types/f1'

  export function normalizeSnapshot(drivers: DriverState[]): Record<string, DriverState> {
    return Object.fromEntries(drivers.map(d => [String(d.driverNumber), d]))
  }
  ```

- [x] Create `frontend/src/shared/utils/normalizeSnapshot.test.ts`:
  - `normalizeSnapshot_emptyArray_returnsEmptyRecord`
  - `normalizeSnapshot_threeDrivers_keyedByDriverNumber`
  - `normalizeSnapshot_duplicateDriverNumber_lastWins` (the array should never have duplicates from the API, but guard behavior is worth specifying)

### Task 9: Zustand liveRaceStore (AC: 1, 6)

- [x] Create `frontend/src/features/live-race/store/liveRaceStore.ts`:
  ```ts
  import { create } from 'zustand'
  import type { DriverState } from '../../../shared/types/f1'

  type ConnectionStatus = 'connected' | 'reconnecting' | 'disconnected'

  interface LiveRaceState {
    connectionStatus: ConnectionStatus
    drivers: Record<string, DriverState>
    lastSnapshotTime: Date | null
    setConnectionStatus: (status: ConnectionStatus) => void
    setDrivers: (drivers: Record<string, DriverState>) => void
    setLastSnapshotTime: (time: Date) => void
  }

  export const useLiveRaceStore = create<LiveRaceState>((set) => ({
    connectionStatus: 'disconnected',
    drivers: {},
    lastSnapshotTime: null,
    setConnectionStatus: (status) => set({ connectionStatus: status }),
    setDrivers: (drivers) => set({ drivers }),
    setLastSnapshotTime: (time) => set({ lastSnapshotTime: time }),
  }))
  ```

### Task 10: SignalR client singleton (AC: 1, 6)

- [x] Create `frontend/src/features/live-race/signalRClient.ts`:
  ```ts
  import * as signalR from '@microsoft/signalr'

  export const raceHubConnection = new signalR.HubConnectionBuilder()
    .withUrl(import.meta.env.VITE_SIGNALR_HUB_URL as string)
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        const delays = [0, 2000, 5000, 10000, 30000, 60000]
        return delays[retryContext.previousRetryCount] ?? 60000
      },
    })
    .build()
  ```
  This is a module-level singleton — never import this inside a component or `useEffect`. The connection is created once when the module loads, matching the architecture's explicit requirement.

### Task 11: useSignalRConnection hook (AC: 1, 4, 5, 6)

- [x] Create `frontend/src/features/live-race/hooks/useSignalRConnection.ts`:
  ```ts
  import { useEffect } from 'react'
  import { raceHubConnection } from '../signalRClient'
  import { useLiveRaceStore } from '../store/liveRaceStore'
  import { normalizeSnapshot } from '../../../shared/utils/normalizeSnapshot'
  import type { RaceSnapshotMessage } from '../../../shared/types/signalR'
  import * as signalR from '@microsoft/signalr'

  export function useSignalRConnection() {
    const setConnectionStatus = useLiveRaceStore(s => s.setConnectionStatus)
    const setDrivers = useLiveRaceStore(s => s.setDrivers)
    const setLastSnapshotTime = useLiveRaceStore(s => s.setLastSnapshotTime)

    useEffect(() => {
      const handleSnapshot = (snapshot: RaceSnapshotMessage) => {
        setDrivers(normalizeSnapshot(snapshot.drivers))
        setLastSnapshotTime(new Date())
      }

      raceHubConnection.on('RaceSnapshot', handleSnapshot)

      raceHubConnection.onreconnecting(() => setConnectionStatus('reconnecting'))
      raceHubConnection.onreconnected(() => setConnectionStatus('connected'))
      raceHubConnection.onclose(() => setConnectionStatus('disconnected'))

      if (raceHubConnection.state === signalR.HubConnectionState.Disconnected) {
        raceHubConnection
          .start()
          .then(() => setConnectionStatus('connected'))
          .catch(() => setConnectionStatus('disconnected'))
      }

      return () => {
        raceHubConnection.off('RaceSnapshot', handleSnapshot)
      }
    }, [setConnectionStatus, setDrivers, setLastSnapshotTime])
  }
  ```
  Only starts the connection if it is `Disconnected` — avoids double-starting on React Strict Mode double-effect in dev. Does not stop the connection on unmount — the singleton connection outlives individual page mounts (intentional: reconnecting on every navigation would be disruptive).

### Task 12: GapList + DriverRow components (AC: 2, 3, 4, 5)

- [x] Create `frontend/src/features/live-race/GapList/DriverRow.tsx`:
  - Props: `driverId: string`
  - Subscribe to store: `const driver = useLiveRaceStore(s => s.drivers[driverId])`
  - Return `null` if driver is undefined
  - Layout (single line, never wraps): `[P{pos}] [●team] [CODE] [gap]`
  - Team colour dot: `<span style={{ backgroundColor: \`#${driver.teamColour}\` }}>`
  - Gap cell: 
    - Leader (null + not stale): `"—"`
    - Stale: `"~" + (driver.gapToCarAhead ?? "–")` with `opacity-50` or `text-secondary` class
    - Battle (< 1s, not stale): render with a highlight class (e.g. `text-accent-editorial` or `ring-1`)
    - Normal: plain gap string
  - Battle detection: `parseFloat(driver.gapToCarAhead ?? '99') < 1.0`
  - DESIGN.md gap-list-row: `padding: dense-row (6px 10px)`, `fontSize: numeric-dense (12px)`, `hoverBackground: bg-card-hover`
  - Tailwind classes: `flex items-center gap-3 px-[10px] py-[6px] text-[12px] hover:bg-[#20242c] cursor-default`

- [x] Create `frontend/src/features/live-race/GapList/GapList.tsx`:
  - Does NOT call `useSignalRConnection()` — the connection is established once in `LiveRacePage.tsx` (the host), not in child components
  - Get sorted driver ID list:
    ```ts
    const sortedDriverIds = useLiveRaceStore(
      useShallow(s =>
        Object.values(s.drivers)
          .sort((a, b) => a.position - b.position)
          .map(d => String(d.driverNumber))
      )
    )
    ```
  - Get `connectionStatus` for the status badge
  - If `sortedDriverIds.length === 0`: show skeleton / "Waiting for race data…"
  - Map `sortedDriverIds` → `<DriverRow key={id} driverId={id} />`
  - Connection status badge (per EXPERIENCE.md "Stale value" + "Live badge" patterns):
    ```tsx
    <span className={connectionStatus === 'connected' ? 'text-sector-green' : 'text-text-secondary'}>
      {connectionStatus === 'connected' ? '● Live' : connectionStatus === 'reconnecting' ? '◌ Reconnecting…' : '○ Disconnected'}
    </span>
    ```
  - Import `useShallow` from `zustand/react/shallow` (Zustand v5)

### Task 13: GapList tests (AC: 2, 3, 5)

- [x] Create `frontend/src/features/live-race/GapList/GapList.test.tsx`:
  - `beforeEach`: reset store via `useLiveRaceStore.setState({ drivers: {}, connectionStatus: 'disconnected', lastSnapshotTime: null })` — this is a MERGE (not replace), so action functions in the store are preserved. Do NOT pass `true` as second arg (that would wipe the action functions).
  - No need to mock `signalRClient.ts` — `useSignalRConnection` is called in `LiveRacePage`, not `GapList`. The `GapList` test renders `<GapList />` directly and pre-populates the Zustand store instead.

  - Test: `renders_waiting_message_when_no_drivers`:
    Store: `{ drivers: {}, connectionStatus: 'disconnected' }` → renders "Waiting for race data…"

  - Test: `renders_drivers_sorted_by_position`:
    Store: `{ drivers: { '33': { position: 2, driverCode: 'VER', ... }, '44': { position: 1, driverCode: 'HAM', ... } } }` → HAM appears before VER in the DOM

  - Test: `highlights_gap_under_one_second`:
    Store: one driver with `gapToCarAhead: '0.456', gapIsStale: false` → gap text has a highlight CSS class or is rendered within a battle element (use `data-testid="battle-gap"` if needed)

  - Test: `shows_tilde_prefix_for_stale_gap`:
    Store: driver with `gapIsStale: true, gapToCarAhead: null` → rendered gap starts with `~` or shows dimmed styling

  - Test: `shows_connected_status_when_connected`:
    Store: `{ connectionStatus: 'connected' }` → "Live" text visible

### Task 14: LiveRacePage + route (AC: 1)

- [x] Create `frontend/src/features/live-race/LiveRacePage.tsx`:
  ```tsx
  import { GapList } from './GapList/GapList'
  import { useSignalRConnection } from './hooks/useSignalRConnection'

  export function LiveRacePage() {
    useSignalRConnection()  // establishes hub connection once at page level

    return (
      <div className="min-h-screen bg-[#14171c] text-[#eef0f3] p-4">
        <h1 className="text-[26px] font-bold tracking-[-0.01em] mb-4">Live Race</h1>
        <GapList />
      </div>
    )
  }
  ```

- [x] Create `frontend/src/features/live-race/index.ts`:
  ```ts
  export { LiveRacePage } from './LiveRacePage'
  ```

- [x] Update `frontend/src/router.tsx` — add `/live` route:
  ```ts
  import { LiveRacePage } from './features/live-race'
  // ...
  { path: 'live', element: <LiveRacePage /> }
  ```

- [x] Update `frontend/src/App.tsx` — add minimal nav link to live race:
  ```tsx
  import { Outlet, Link } from 'react-router-dom'

  function App() {
    return (
      <>
        <nav className="h-14 bg-[#1b1f26] border-b border-[#2a2f38] flex items-center gap-6 px-6">
          <Link to="/" className="text-[#eef0f3] text-sm font-semibold hover:text-[#d8b65c]">Calendar</Link>
          <Link to="/live" className="text-[#eef0f3] text-sm font-semibold hover:text-[#d8b65c]">Live Race</Link>
        </nav>
        <Outlet />
      </>
    )
  }

  export default App
  ```

## Dev Notes

### This story establishes the entire real-time pipeline

Story 2.1 is the most infrastructure-heavy story in Epic 2. It builds:
- The OpenF1 client + polling loops
- The `RaceDataOrchestrator` (the single orchestrator that owns all live data)
- The SignalR hub + broadcast mechanism
- The frontend singleton connection + Zustand store + normalization

Stories 2.2–2.5 extend the orchestrator by adding more data fields to the snapshot. They do NOT change the orchestrator's structure — they add to it.

### RaceDataOrchestrator — only 2 active polling loops in this story

Per the architecture, the orchestrator ultimately owns 5 polling loops. For Story 2.1, only `/position` and `/intervals` are implemented as active loops. The loops for `/car_data`, `/pit`, and `/race_control` are added in Stories 2.2–2.3 and 2.5 as that data becomes needed. Do not stub the other loops — just don't add them yet.

### CORS AllowCredentials() is mandatory for SignalR

SignalR's WebSocket upgrade performs a preflight that requires `AllowCredentials()` in the CORS policy. Without it, the browser blocks the connection and the client gets a CORS error even though REST calls work fine. This is a common gotcha — apply it to the `FrontendDevCorsPolicy` in `Program.cs`.

### OpenF1 incremental polling

The `?date>{timestamp}` filter (where `>` is part of the URL query, not encoded) gives only records newer than the last poll. On the first poll, pass `DateTimeOffset.MinValue` to get the latest data for the current session (OpenF1 returns all records since session start, so take only the most recent per driver by keeping the one with the later `date`). Use `ConcurrentDictionary.AddOrUpdate` to ensure only the latest entry per driver is kept.

The OpenF1 URL format for date filters: `position?session_key=latest&date>2026-06-17T14:00:00.000` — the `>` should be passed as-is in the URL; `HttpClient` and `HttpRequestMessage` will percent-encode it correctly.

### Timestamp join tolerance (500ms window)

The join compares `pos.Date` and `interval.Date` for the same driver. If the absolute difference exceeds `JoinToleranceMs` (config default: 500ms), `GapIsStale = true` and `GapToCarAhead = null`. The frontend renders `~` or dimmed text for stale values per EXPERIENCE.md "Stale value" pattern.

### DriverState placeholder fields (null is correct for this story)

`TyreCompound`, `StintLaps`, `ChampionshipDelta` are intentionally `null` in the `DriverState` model for this story. The frontend `DriverRow` should simply not render those fields when null (Story 2.2 adds tyres, Story 2.4 adds championship delta). Do not render a placeholder or "loading" state for these — omit them entirely when null.

### Zustand v5 — useShallow import

Zustand v5 changed the shallow equality import:
- v4: `import { shallow } from 'zustand/shallow'` + `useStore(selector, shallow)`
- v5: `import { useShallow } from 'zustand/react/shallow'` + `useStore(useShallow(selector))`

Use the v5 form. This applies specifically in `GapList.tsx` for the `sortedDriverIds` selector.

### Store reset in tests (Zustand v5)

Reset Zustand state between tests using `useLiveRaceStore.setState({...})`. In Zustand v5 this is still available directly on the store object. Reset in `beforeEach`/`afterEach` — don't rely on test isolation by default.

### @microsoft/signalr v10 API

Already installed (`package.json` confirms `"@microsoft/signalr": "^10.0.0"`). The `HubConnectionBuilder` API is identical to v7/v8. The custom `IRetryPolicy` object form (`{ nextRetryDelayInMilliseconds: (ctx) => number }`) is the right way to get indefinite retry — the array form `withAutomaticReconnect([...])` stops after the array is exhausted.

### App.tsx navigation

`App.tsx` currently just renders `<Outlet />`. This story adds a minimal top nav with Calendar and Live Race links. Full nav styling (hamburger, streak badge, all routes) is out of scope — that work belongs to a later story once all pages exist. Use inline Tailwind classes matching DESIGN.md values exactly.

### TimeProvider in backend tests

The BCL `TimeProvider` class is abstract. Use a `FakeTimeProvider` subclass in tests. `Microsoft.Extensions.TimeProvider.Testing` (NuGet) provides one, but if not installed, define it in the test project (`internal sealed class FakeTimeProvider : TimeProvider`). Do NOT use `DateTimeOffset.UtcNow` directly in `RaceDataOrchestrator` — always call `timeProvider.GetUtcNow()`.

### Backend tests — InternalsVisibleTo

To test `BuildSnapshot()` directly without going through the polling loop, expose it as `internal`. Add this attribute at the top of `Program.cs` (before the `var builder = ...` line — C# top-level statement files accept assembly attributes at the top):
```csharp
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("F1App.Api.Tests")]
```
The existing `public partial class Program;` at the bottom of `Program.cs` already follows this same pattern for integration tests — add the new attribute alongside it.

### Signal R Hub vs All broadcast

The hub broadcasts to `Clients.Group("race")` (not `Clients.All`). Clients join the group in `OnConnectedAsync`. This is a slight over-engineering for a POC (All would work with one viewer) but costs nothing and keeps the broadcast pattern correct for the architecture.

### MSW mock for SignalR in frontend tests

MSW v2 does not intercept WebSocket / SignalR connections — only HTTP. The `signalRClient.ts` module must be mocked via `vi.mock()` in component tests. The mock object only needs the methods that `useSignalRConnection` calls: `on`, `off`, `start`, `onreconnecting`, `onreconnected`, `onclose`, and `state`. See Task 13 for the exact mock shape.

### Project Structure Notes

**New backend files:**
- `backend/F1App.Api/Clients/IOpenF1Client.cs`
- `backend/F1App.Api/Clients/OpenF1Client.cs`
- `backend/F1App.Api/Dtos/OpenF1/OpenF1PositionDto.cs`
- `backend/F1App.Api/Dtos/OpenF1/OpenF1IntervalDto.cs`
- `backend/F1App.Api/Dtos/OpenF1/OpenF1DriverInfoDto.cs`
- `backend/F1App.Api/Models/RaceStateSnapshot.cs`
- `backend/F1App.Api/Models/DriverState.cs`
- `backend/F1App.Api/Models/FallbackTrigger.cs`
- `backend/F1App.Api/Hubs/RaceHub.cs`
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs`
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`

**Modified backend files:**
- `backend/F1App.Api/Program.cs` (SignalR, CORS credentials, OpenF1Client, TimeProvider, RaceDataOrchestrator, hub mapping)
- `backend/F1App.Api/appsettings.Development.json` (add `OpenF1BaseUrl`, `JoinToleranceMs`)

**New frontend files:**
- `frontend/src/shared/types/f1.ts`
- `frontend/src/shared/types/signalR.ts`
- `frontend/src/shared/utils/normalizeSnapshot.ts`
- `frontend/src/shared/utils/normalizeSnapshot.test.ts`
- `frontend/src/features/live-race/signalRClient.ts`
- `frontend/src/features/live-race/store/liveRaceStore.ts`
- `frontend/src/features/live-race/hooks/useSignalRConnection.ts`
- `frontend/src/features/live-race/GapList/GapList.tsx`
- `frontend/src/features/live-race/GapList/GapList.test.tsx`
- `frontend/src/features/live-race/GapList/DriverRow.tsx`
- `frontend/src/features/live-race/LiveRacePage.tsx`
- `frontend/src/features/live-race/index.ts`

**Modified frontend files:**
- `frontend/src/router.tsx` (add `/live` route)
- `frontend/src/App.tsx` (add minimal nav with Calendar + Live Race links)

### References

- [Source: epics.md#Story 2.1] — Acceptance criteria, FR-9 spec
- [Source: architecture.md#Real-Time Architecture] — RaceDataOrchestrator, snapshot push pattern, 500ms join tolerance, fallback state machine
- [Source: architecture.md#Frontend Architecture] — normalizeSnapshot, Zustand slice selectors, SignalR singleton
- [Source: architecture.md#Naming Patterns] — `RaceHub`, `"RaceSnapshot"` / `"SubscribeToRace"` message names, `useLiveRaceStore`, `signalRClient.ts`
- [Source: architecture.md#Complete Project Tree] — exact file paths for all new files
- [Source: architecture.md#Communication Patterns] — `liveRaceStore.ts` location, action naming (`setConnectionStatus`, `setDrivers`)
- [Source: architecture.md#Enforcement Summary] — Rule 4: Zustand never holds server data (exception: `DriverState` is a SignalR push payload, not a server-fetched query — Zustand is correct here)
- [Source: EXPERIENCE.md#Component Patterns] — Gap list row single-line dense layout, `~` prefix for stale values, battle highlight under 1s
- [Source: EXPERIENCE.md#State Patterns] — "Stream degraded" state machine, connection status indicator
- [Source: DESIGN.md] — color tokens (`bg-app: #14171c`, `bg-card: #1b1f26`, `text-primary: #eef0f3`, `accent-editorial: #d8b65c`, `bg-card-hover: #20242c`), `gap-list-row` dense-row padding
- [Source: architecture.md#Gap Analysis Results] — `VITE_SIGNALR_HUB_URL=http://localhost:5000/hubs/race`, `JoinToleranceMs: 500`
- [Source: 1-7-pre-race-win-probability-widget.md#Dev Notes] — `CacheKeys.cs` pattern, `ErgastClient.cs` pattern for new client
- [Source: backend/F1App.Api/Program.cs] — existing DI registration order, CORS policy name, exception handler
- [Source: frontend/src/shared/api/ergast.ts] — zod schema pattern (not needed here — SignalR payload is typed via TypeScript, not zod)
- [Source: frontend/src/shared/mocks/handlers/ergastHandlers.ts] — test mock pattern reference; SignalR uses `vi.mock` instead

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- `[assembly: InternalsVisibleTo("F1App.Api.Tests")]` must come AFTER all `using` statements in `Program.cs` (C# top-level statement rule: using clauses must precede assembly attributes). Placing it before `using` causes CS1529.

### Completion Notes List

- All 14 tasks implemented. Backend: 45/45 tests pass. Frontend: 44/44 tests pass.
- `FakeTimeProvider` defined locally in test project (subclass of BCL `TimeProvider`) — `Microsoft.Extensions.TimeProvider.Testing` not installed.
- `useShallow` imported from `zustand/react/shallow` (Zustand v5 form, not `zustand/shallow`).
- `useSignalRConnection()` called once in `LiveRacePage` (host); `GapList` tests pre-populate Zustand store directly — no SignalR mock needed.
- `data-testid="battle-gap"` and `data-testid="stale-gap"` added to `DriverRow` for deterministic test assertions.
- `DriverState` placeholder fields (`tyreCompound`, `stintLaps`, `championshipDelta`) are null — populated by Stories 2.2–2.4.

### File List

**New backend files:**
- `backend/F1App.Api/Clients/IOpenF1Client.cs`
- `backend/F1App.Api/Clients/OpenF1Client.cs`
- `backend/F1App.Api/Dtos/OpenF1/OpenF1PositionDto.cs`
- `backend/F1App.Api/Dtos/OpenF1/OpenF1IntervalDto.cs`
- `backend/F1App.Api/Dtos/OpenF1/OpenF1DriverInfoDto.cs`
- `backend/F1App.Api/Models/RaceStateSnapshot.cs`
- `backend/F1App.Api/Models/DriverState.cs`
- `backend/F1App.Api/Models/FallbackTrigger.cs`
- `backend/F1App.Api/Hubs/RaceHub.cs`
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs`
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`

**Modified backend files:**
- `backend/F1App.Api/Program.cs`
- `backend/F1App.Api/appsettings.Development.json`

**New frontend files:**
- `frontend/src/shared/types/f1.ts`
- `frontend/src/shared/types/signalR.ts`
- `frontend/src/shared/utils/normalizeSnapshot.ts`
- `frontend/src/shared/utils/normalizeSnapshot.test.ts`
- `frontend/src/features/live-race/signalRClient.ts`
- `frontend/src/features/live-race/store/liveRaceStore.ts`
- `frontend/src/features/live-race/hooks/useSignalRConnection.ts`
- `frontend/src/features/live-race/GapList/GapList.tsx`
- `frontend/src/features/live-race/GapList/GapList.test.tsx`
- `frontend/src/features/live-race/GapList/DriverRow.tsx`
- `frontend/src/features/live-race/LiveRacePage.tsx`
- `frontend/src/features/live-race/index.ts`

**Modified frontend files:**
- `frontend/src/router.tsx`
- `frontend/src/App.tsx`

## Senior Developer Review (AI)

**Date:** 2026-06-17
**Reviewer:** claude-sonnet-4-6 (3-layer parallel review: Blind Hunter + Edge Case Hunter + Acceptance Auditor)
**Outcome:** Changes Requested — 6 patch items, no blocking failures on ACs

### Action Items

**Patch:**
- [x] P1 — Poll timestamp advanced even when API returns empty response [RaceDataOrchestrator.cs:77,95]
- [x] P2 — `VITE_SIGNALR_HUB_URL` silently becomes `"undefined"` string if env var missing [signalRClient.ts:4]
- [x] P3 — `onreconnecting`/`onreconnected`/`onclose` handlers accumulate on every effect run, never deregistered [useSignalRConnection.ts:20-22]
- [x] P4 — Stale-gap test asserts only `^~` prefix, not full `~–` rendered text [GapList.test.tsx:79]
- [x] P5 — normalizeSnapshot duplicate-driver test doesn't assert `Object.keys(result).length === 1` [normalizeSnapshot.test.ts:38]
- [x] P6 — `reconnecting` connection status not covered by any GapList test [GapList.test.tsx]

**Deferred:**
- [x] D1 — Session transition: stale driver data from prior session persists in ConcurrentDictionaries [RaceDataOrchestrator.cs:20-21] — deferred, Story 2.5 scope
- [x] D2 — PeriodicTimer recreated without dispose on exception restart, no backoff [RaceDataOrchestrator.cs:73,90] — deferred, acceptable for POC
- [x] D3 — `_lastPositionPoll` DateTimeOffset not volatile; torn read possible on 32-bit [RaceDataOrchestrator.cs:24] — deferred, .NET 64-bit safe in practice
- [x] D4 — `InvalidOperationException` conflated with upstream errors in global exception handler [Program.cs:77] — deferred, pre-existing pattern
- [x] D5 — `InitialiseDriverInfoAsync` has no retry; transient failure leaves driver names as numbers for process lifetime [RaceDataOrchestrator.cs:38] — deferred, acceptable for POC
- [x] D6 — `BuildSnapshot` observes partial ConcurrentDictionary state during publish tick [RaceDataOrchestrator.cs:124] — deferred, acceptable for 1s snapshot POC
- [x] D7 — `parseFloat` on non-numeric gap strings (e.g. `"LAP"`, `"+1 LAP"`) — NaN < 1.0 is safe but string renders verbatim [DriverRow.tsx:15] — deferred, acceptable for POC
- [x] D8 — Race leader may show `~–` if OpenF1 omits them from intervals feed entirely [RaceDataOrchestrator.cs:129] — deferred, depends on real API behavior
- [x] D9 — `setDrivers` replaces entire drivers map on each snapshot; driver disappears on any partial snapshot [liveRaceStore.ts:20] — deferred, backend only publishes when count > 0
- [x] D10 — `OpenF1BaseUrl`/`ErgastBaseUrl` null-bang crashes process with no useful message on missing config [Program.cs:39,46] — deferred, pre-existing pattern
- [x] D11 — `appsettings.json` has no fallback values for config keys; new contributor clone fails immediately — deferred, setup/docs issue
- [x] D12 — Test coverage gaps: LAP string gaps, session transitions, `PublishSnapshotLoopAsync` gate, `ExecuteAsync` integration [RaceDataOrchestratorTests.cs] — deferred, POC scope

### Tasks / Subtasks — Review Follow-ups (AI)

- [x] [AI-Review][P1] Only advance `_lastPositionPoll`/`_lastIntervalPoll` when API response is non-empty [RaceDataOrchestrator.cs:77,95]
- [x] [AI-Review][P2] Guard `VITE_SIGNALR_HUB_URL` — throw or warn if undefined at module load [signalRClient.ts:4]
- [x] [AI-Review][P3] Track whether lifecycle handlers already registered; guard against accumulation [useSignalRConnection.ts:20-22]
- [x] [AI-Review][P4] Assert full `~–` text content in stale-gap test [GapList.test.tsx:79]
- [x] [AI-Review][P5] Add `expect(Object.keys(result)).toHaveLength(1)` to duplicate-key test [normalizeSnapshot.test.ts:38]
- [x] [AI-Review][P6] Add test asserting `'◌ Reconnecting…'` text when `connectionStatus === 'reconnecting'` [GapList.test.tsx]

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-06-17 | Story implemented: full real-time pipeline — OpenF1 polling, SignalR hub, RaceDataOrchestrator, GapList UI with battle/stale states, Zustand store, 45 backend + 44 frontend tests pass | claude-sonnet-4-6 |
| 2026-06-17 | Code review completed: 6 patch items, 12 deferred, 2 dismissed. Story moved back to in-progress for patch fixes. | claude-sonnet-4-6 |
