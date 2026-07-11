---
baseline_commit: "befa3d7"
---

# Story 3.5: Race Event Timeline

Status: done

## Story

As a race-day fan,
I want a timeline of key race events,
So that I can see the full shape of the race at a glance.

## Acceptance Criteria

1. **Given** a live race **When** the timeline renders **Then** a horizontal bar shows lap number on the X-axis with markers for Safety Car, VSC, pit stops (per driver), DNFs, fastest lap, and red flags, sourced from OpenF1 race control and pit data.
2. **And** the timeline grows lap by lap during the live race.
3. **Given** a completed race (live or fallback per Epic 2's Story 2.5) **When** viewed **Then** the timeline is a static, browsable archive.
4. **Given** no events have occurred yet **When** the timeline renders **Then** an empty/placeholder state is shown, not a crash.

## Tasks / Subtasks

### Task 1: Backend — OpenF1 DTOs for race control and pit data (AC: 1)

- [ ] Create `backend/F1App.Api/Dtos/OpenF1/OpenF1RaceControlDto.cs`:
  ```csharp
  using System.Text.Json.Serialization;

  namespace F1App.Api.Dtos.OpenF1;

  // OpenF1 /race_control JSON shape (snake_case). Verified live against
  // https://api.openf1.org/v1/race_control?session_key=9488 (Australia 2024):
  // category is "SafetyCar" | "Flag" | "CarEvent" | "Other" | "Drs" | "SessionStatus" | ...
  // flag is "GREEN" | "YELLOW" | "DOUBLE YELLOW" | "RED" | "BLUE" | "CHEQUERED" | null.
  // driver_number is almost always null even for car-specific messages — the car
  // number appears only in free-text `message` (e.g. "CAR 44 (HAM) STOPPED AT TURN 10"),
  // so driver_number is intentionally NOT modelled here; RaceDataOrchestrator parses
  // the car number out of `message` where needed (DNF detection).
  public record OpenF1RaceControlDto(
      [property: JsonPropertyName("date")] DateTimeOffset Date,
      [property: JsonPropertyName("lap_number")] int? LapNumber,
      [property: JsonPropertyName("category")] string? Category,
      [property: JsonPropertyName("flag")] string? Flag,
      [property: JsonPropertyName("message")] string? Message
  );
  ```

- [ ] Create `backend/F1App.Api/Dtos/OpenF1/OpenF1PitDto.cs`:
  ```csharp
  using System.Text.Json.Serialization;

  namespace F1App.Api.Dtos.OpenF1;

  // OpenF1 /pit JSON shape (snake_case). Verified live against
  // https://api.openf1.org/v1/pit?session_key=9590 (Monza 2024).
  public record OpenF1PitDto(
      [property: JsonPropertyName("date")] DateTimeOffset Date,
      [property: JsonPropertyName("driver_number")] int DriverNumber,
      [property: JsonPropertyName("lap_number")] int LapNumber
  );
  ```
  Only `driver_number` and `lap_number` are needed for the timeline marker (pit duration is out of scope for this story).

### Task 2: Backend — `IOpenF1Client` / `OpenF1Client` new methods (AC: 1)

- [ ] Add to `backend/F1App.Api/Clients/IOpenF1Client.cs`:
  ```csharp
  Task<IReadOnlyList<OpenF1RaceControlDto>> GetLatestRaceControlAsync(DateTimeOffset since, CancellationToken ct);
  Task<IReadOnlyList<OpenF1PitDto>> GetLatestPitStopsAsync(DateTimeOffset since, CancellationToken ct);
  ```

- [ ] Implement in `backend/F1App.Api/Clients/OpenF1Client.cs`, following the exact `date>` incremental-filter pattern already used by `GetLatestPositionsAsync`/`GetLatestIntervalsAsync`/`GetLatestLocationsAsync`:
  ```csharp
  public async Task<IReadOnlyList<OpenF1RaceControlDto>> GetLatestRaceControlAsync(DateTimeOffset since, CancellationToken ct)
  {
      var url = since == DateTimeOffset.MinValue
          ? "race_control?session_key=latest"
          : $"race_control?session_key=latest&date>{since:yyyy-MM-ddTHH:mm:ss.fff}";

      return await httpClient.GetFromJsonAsync<IReadOnlyList<OpenF1RaceControlDto>>(url, ct) ?? [];
  }

  public async Task<IReadOnlyList<OpenF1PitDto>> GetLatestPitStopsAsync(DateTimeOffset since, CancellationToken ct)
  {
      var url = since == DateTimeOffset.MinValue
          ? "pit?session_key=latest"
          : $"pit?session_key=latest&date>{since:yyyy-MM-ddTHH:mm:ss.fff}";

      return await httpClient.GetFromJsonAsync<IReadOnlyList<OpenF1PitDto>>(url, ct) ?? [];
  }
  ```

### Task 3: Backend — `RaceTimelineEvent` model (AC: 1)

- [ ] Create `backend/F1App.Api/Models/RaceTimelineEvent.cs`:
  ```csharp
  namespace F1App.Api.Models;

  // EventType: "SafetyCar" | "VirtualSafetyCar" | "RedFlag" | "PitStop" | "Dnf" | "FastestLap"
  public record RaceTimelineEvent(
      int LapNumber,
      string EventType,
      string? DriverCode,
      string? Detail
  );
  ```

### Task 4: Backend — Extend `RaceStateSnapshot` (AC: 1, 2, 3, 4)

- [ ] Update `backend/F1App.Api/Models/RaceStateSnapshot.cs` — add:
  ```csharp
  public IReadOnlyList<RaceTimelineEvent> Timeline { get; init; } = [];
  ```
  **Design note (AC 3)**: unlike `FastestSectors`/`MiniSectorStatus` (Stories 3.2/3.4), Timeline does NOT need a fallback-mode null branch. The accumulated `_timelineEvents` list naturally stops growing once the race ends or OpenF1 goes quiet (no new race-control/pit/lap records arrive to append), so it becomes a "static, browsable archive" for free — there's no separate historical rebuild needed. If the orchestrator process never observed a live session at all (pure cold-start REST fallback for a past race), `Timeline` is simply empty, same honesty precedent as Story 3.4's Ergast-fallback limitation.

### Task 5: Backend — Track timeline events in `RaceDataOrchestrator` (AC: 1, 2)

- [ ] Add fields alongside the sector-colour/pit-window tracking fields:
  ```csharp
  internal readonly ConcurrentQueue<RaceTimelineEvent> _timelineEvents = new();
  private DateTimeOffset _lastRaceControlPoll = DateTimeOffset.MinValue;
  private DateTimeOffset _lastPitEventPoll = DateTimeOffset.MinValue;
  // Session-fastest lap tracking (single-writer: PollLapsAsync only)
  private double _sessionFastestLapTime = double.MaxValue;
  ```

- [ ] Add two new polling loops (same `PeriodicTimer` pattern as the existing pollers):
  ```csharp
  private async Task PollRaceControlAsync(CancellationToken ct)
  {
      var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
      while (await timer.WaitForNextTickAsync(ct))
      {
          var messages = await openF1Client.GetLatestRaceControlAsync(_lastRaceControlPoll, ct);
          if (messages.Count > 0)
              _lastRaceControlPoll = timeProvider.GetUtcNow();

          foreach (var msg in messages)
          {
              var evt = ParseRaceControlEvent(msg);
              if (evt is not null)
                  _timelineEvents.Enqueue(evt);
          }
      }
  }

  internal RaceTimelineEvent? ParseRaceControlEvent(OpenF1RaceControlDto msg)
  {
      var lapNumber = msg.LapNumber ?? 0;
      var message = msg.Message ?? "";
      var upper = message.ToUpperInvariant();

      if (msg.Category == "SafetyCar" && upper.Contains("DEPLOYED"))
      {
          var isVirtual = upper.Contains("VIRTUAL");
          return new RaceTimelineEvent(lapNumber, isVirtual ? "VirtualSafetyCar" : "SafetyCar", null, message);
      }

      if (string.Equals(msg.Flag, "RED", StringComparison.OrdinalIgnoreCase))
      {
          return new RaceTimelineEvent(lapNumber, "RedFlag", null, message);
      }

      if (msg.Category == "CarEvent" && upper.Contains("STOPPED"))
      {
          var driverNum = ExtractDriverNumber(message);
          string? driverCode = driverNum is not null && _driverInfo.TryGetValue(driverNum.Value, out var info)
              ? info.NameAcronym
              : driverNum?.ToString();
          return new RaceTimelineEvent(lapNumber, "Dnf", driverCode, message);
      }

      return null;
  }

  private static int? ExtractDriverNumber(string message)
  {
      var match = System.Text.RegularExpressions.Regex.Match(message, @"CAR (\d+)");
      return match.Success ? int.Parse(match.Groups[1].Value) : null;
  }

  private async Task PollPitEventsAsync(CancellationToken ct)
  {
      var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
      while (await timer.WaitForNextTickAsync(ct))
      {
          var pits = await openF1Client.GetLatestPitStopsAsync(_lastPitEventPoll, ct);
          if (pits.Count > 0)
              _lastPitEventPoll = timeProvider.GetUtcNow();

          foreach (var pit in pits)
          {
              _driverInfo.TryGetValue(pit.DriverNumber, out var info);
              var driverCode = info?.NameAcronym ?? pit.DriverNumber.ToString();
              _timelineEvents.Enqueue(new RaceTimelineEvent(pit.LapNumber, "PitStop", driverCode, null));
          }
      }
  }
  ```
  **Verified live data conventions** (against `https://api.openf1.org/v1/race_control?session_key=9488`, Australia 2024): `category: "SafetyCar"` messages read `"VIRTUAL SAFETY CAR DEPLOYED"` / `"...ENDING"` (only `"DEPLOYED"` becomes a marker — `"ENDING"` is intentionally skipped so each SC/VSC period produces exactly one timeline marker, not two); `category: "CarEvent"` messages read like `"CAR 44 (HAM) STOPPED AT TURN 10"` for retirements/mechanical stops — `driver_number` is `null` on these entries even though the message clearly references a car, hence the regex extraction.

- [ ] In `PollLapsAsync`'s existing `foreach (var lap in laps)` loop, after the existing `ComputeSectorStatus(lap);` call, add a call to a new extracted `internal` method (extracted for direct unit testability, mirroring `ComputeSectorStatus`'s existing pattern):
  ```csharp
  ComputeSectorStatus(lap);
  TryEnqueueFastestLap(lap);
  ```
  ```csharp
  internal void TryEnqueueFastestLap(OpenF1LapDto lap)
  {
      if (!lap.LapDuration.HasValue || lap.LapDuration.Value >= _sessionFastestLapTime)
          return;

      _sessionFastestLapTime = lap.LapDuration.Value;
      _driverInfo.TryGetValue(lap.DriverNumber, out var info);
      var driverCode = info?.NameAcronym ?? lap.DriverNumber.ToString();
      _timelineEvents.Enqueue(new RaceTimelineEvent(lap.LapNumber, "FastestLap", driverCode, null));
  }
  ```

- [ ] Register the two new loops in `ExecuteAsync`'s `Task.WhenAll(...)`:
  ```csharp
  await Task.WhenAll(
      RunLoopAsync("PositionPoller",     PollPositionAsync,         sessionCts.Token),
      RunLoopAsync("IntervalPoller",     PollIntervalAsync,         sessionCts.Token),
      RunLoopAsync("StintsPoller",       PollStintsAsync,           sessionCts.Token),
      RunLoopAsync("LapsPoller",         PollLapsAsync,             sessionCts.Token),
      RunLoopAsync("LocationPoller",     PollLocationAsync,         sessionCts.Token),
      RunLoopAsync("RaceControlPoller",  PollRaceControlAsync,      sessionCts.Token),
      RunLoopAsync("PitEventPoller",     PollPitEventsAsync,        sessionCts.Token),
      RunLoopAsync("PublishLoop",        PublishSnapshotLoopAsync,  sessionCts.Token)
  );
  ```

### Task 6: Backend — Wire into `BuildSnapshot` (AC: 1, 2, 3)

- [ ] Add to the `return new RaceStateSnapshot { ... }` statement:
  ```csharp
  Timeline = [.. _timelineEvents.OrderBy(e => e.LapNumber)],
  ```
  Sorting by `LapNumber` gives deterministic client-side rendering even though the three producing loops (race control @3s, pit @5s, laps @5s) can interleave insertion order loosely within the same lap.

### Task 7: Backend — Tests (AC: 1, 2, 3, 4)

- [ ] Add to `backend/F1App.Api.Tests/Clients/OpenF1ClientContractTests.cs`:
  ```csharp
  [Fact]
  public async Task GetLatestRaceControlAsync_ParsesMessages()
  {
      _server
          .Given(Request.Create().WithPath("/race_control").UsingGet())
          .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new[]
          {
              new { date = "2024-03-24T04:26:24Z", lap_number = 17, category = "SafetyCar", flag = (string?)null, message = "VIRTUAL SAFETY CAR DEPLOYED" },
          }));

      using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
      var client = new OpenF1Client(httpClient);

      var messages = await client.GetLatestRaceControlAsync(DateTimeOffset.MinValue, CancellationToken.None);

      var msg = Assert.Single(messages);
      Assert.Equal(17, msg.LapNumber);
      Assert.Equal("SafetyCar", msg.Category);
      Assert.Equal("VIRTUAL SAFETY CAR DEPLOYED", msg.Message);
  }

  [Fact]
  public async Task GetLatestPitStopsAsync_ParsesDriverAndLap()
  {
      _server
          .Given(Request.Create().WithPath("/pit").UsingGet())
          .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new[]
          {
              new { date = "2024-09-01T13:11:37Z", driver_number = 27, lap_number = 5 },
          }));

      using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
      var client = new OpenF1Client(httpClient);

      var pits = await client.GetLatestPitStopsAsync(DateTimeOffset.MinValue, CancellationToken.None);

      var pit = Assert.Single(pits);
      Assert.Equal(27, pit.DriverNumber);
      Assert.Equal(5, pit.LapNumber);
  }
  ```

- [ ] Add to `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`:
  ```csharp
  private static OpenF1RaceControlDto MakeRaceControl(int? lapNumber, string? category, string? flag, string message) =>
      new(DateTimeOffset.UtcNow, lapNumber, category, flag, message);

  [Fact]
  public void ParseRaceControlEvent_VirtualSafetyCarDeployed_ReturnsVirtualSafetyCarEvent()
  {
      var sut = CreateOrchestrator();
      var evt = sut.ParseRaceControlEvent(MakeRaceControl(17, "SafetyCar", null, "VIRTUAL SAFETY CAR DEPLOYED"));

      Assert.NotNull(evt);
      Assert.Equal("VirtualSafetyCar", evt!.EventType);
      Assert.Equal(17, evt.LapNumber);
  }

  [Fact]
  public void ParseRaceControlEvent_SafetyCarDeployed_ReturnsSafetyCarEvent()
  {
      var sut = CreateOrchestrator();
      var evt = sut.ParseRaceControlEvent(MakeRaceControl(30, "SafetyCar", null, "SAFETY CAR DEPLOYED"));

      Assert.NotNull(evt);
      Assert.Equal("SafetyCar", evt!.EventType);
  }

  [Fact]
  public void ParseRaceControlEvent_SafetyCarEnding_ReturnsNull()
  {
      var sut = CreateOrchestrator();
      var evt = sut.ParseRaceControlEvent(MakeRaceControl(18, "SafetyCar", null, "VIRTUAL SAFETY CAR ENDING"));

      Assert.Null(evt);
  }

  [Fact]
  public void ParseRaceControlEvent_RedFlag_ReturnsRedFlagEvent()
  {
      var sut = CreateOrchestrator();
      var evt = sut.ParseRaceControlEvent(MakeRaceControl(10, "Flag", "RED", "RED FLAG"));

      Assert.NotNull(evt);
      Assert.Equal("RedFlag", evt!.EventType);
  }

  [Fact]
  public void ParseRaceControlEvent_CarStopped_ExtractsDriverNumberAndReturnsDnfEvent()
  {
      var sut = CreateOrchestrator();
      sut._driverInfo = new Dictionary<int, OpenF1DriverInfoDto>
      {
          [44] = new(44, "HAM", "Mercedes", "00D2BE"),
      };
      var evt = sut.ParseRaceControlEvent(MakeRaceControl(17, "CarEvent", null, "CAR 44 (HAM) STOPPED AT TURN 10"));

      Assert.NotNull(evt);
      Assert.Equal("Dnf", evt!.EventType);
      Assert.Equal("HAM", evt.DriverCode);
  }

  [Fact]
  public void ParseRaceControlEvent_UnrelatedMessage_ReturnsNull()
  {
      var sut = CreateOrchestrator();
      var evt = sut.ParseRaceControlEvent(MakeRaceControl(1, "Drs", null, "DRS DISABLED"));

      Assert.Null(evt);
  }

  [Fact]
  public void BuildSnapshot_NoEvents_TimelineIsEmpty()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[1] = MakePosition(1, 1, DateTimeOffset.UtcNow);

      var snapshot = sut.BuildSnapshot();

      Assert.Empty(snapshot.Timeline);
  }

  [Fact]
  public void BuildSnapshot_TimelineSortedByLapNumber()
  {
      var sut = CreateOrchestrator();
      sut._timelineEvents.Enqueue(new RaceTimelineEvent(20, "PitStop", "VER", null));
      sut._timelineEvents.Enqueue(new RaceTimelineEvent(5, "SafetyCar", null, null));

      var snapshot = sut.BuildSnapshot();

      Assert.Equal(2, snapshot.Timeline.Count);
      Assert.Equal(5, snapshot.Timeline[0].LapNumber);
      Assert.Equal(20, snapshot.Timeline[1].LapNumber);
  }

  [Fact]
  public void TryEnqueueFastestLap_FirstFastLap_EnqueuesFastestLapEvent()
  {
      var sut = CreateOrchestrator();
      sut._driverInfo = new Dictionary<int, OpenF1DriverInfoDto> { [1] = new(1, "VER", "Red Bull Racing", "3671C6") };

      sut.TryEnqueueFastestLap(MakeLapWithSectors(1, 10, s1: 25, s2: 30, s3: 20)); // duration = 75

      var snapshot = sut.BuildSnapshot();
      var evt = Assert.Single(snapshot.Timeline);
      Assert.Equal("FastestLap", evt.EventType);
      Assert.Equal("VER", evt.DriverCode);
      Assert.Equal(10, evt.LapNumber);
  }

  [Fact]
  public void TryEnqueueFastestLap_SlowerLap_DoesNotEnqueue()
  {
      var sut = CreateOrchestrator();
      sut.TryEnqueueFastestLap(MakeLapWithSectors(1, 10, s1: 25, s2: 30, s3: 20)); // duration = 75, sets baseline
      sut.TryEnqueueFastestLap(MakeLapWithSectors(1, 11, s1: 26, s2: 30, s3: 20)); // duration = 76, slower

      var snapshot = sut.BuildSnapshot();
      Assert.Single(snapshot.Timeline);
  }

  [Fact]
  public void TryEnqueueFastestLap_NoLapDuration_DoesNotEnqueue()
  {
      var sut = CreateOrchestrator();
      sut.TryEnqueueFastestLap(MakeLapWithSectors(1, 10, s1: 25)); // s2/s3 null => LapDuration null

      Assert.Empty(sut.BuildSnapshot().Timeline);
  }
  ```
  **Note**: `ParseRaceControlEvent`, `TryEnqueueFastestLap` must be `internal` (not `private`) and `_timelineEvents` must be `internal` (not `private`) for these tests — same visibility convention as `ComputeSectorStatus`/`_latestSectorStatus`.

- [ ] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass.

### Task 8: Frontend — Extend types and store (AC: 1, 2, 3, 4)

- [ ] Update `frontend/src/shared/types/f1.ts` — add:
  ```ts
  export interface RaceTimelineEvent {
    lapNumber: number
    eventType: 'SafetyCar' | 'VirtualSafetyCar' | 'RedFlag' | 'PitStop' | 'Dnf' | 'FastestLap'
    driverCode: string | null
    detail: string | null
  }
  ```
  Add to `RaceStateSnapshot`:
  ```ts
  timeline: RaceTimelineEvent[]
  ```

- [ ] Update `frontend/src/features/live-race/store/liveRaceStore.ts` — add `timeline: RaceTimelineEvent[]` (default `[]`) and `setTimeline`, following the `fastestSectors`/`setFastestSectors` pattern from Story 3.4.

- [ ] Update `frontend/src/features/live-race/hooks/useSignalRConnection.ts` — in `handleSnapshot`, add `setTimeline(snapshot.timeline ?? [])`; add `setTimeline` to the destructured hooks and the `useEffect` dependency array.

### Task 9: Frontend — `RaceEventTimeline` component (AC: 1, 2, 3, 4)

- [ ] Create `frontend/src/features/live-race/RaceEventTimeline/RaceEventTimeline.tsx`:
  ```tsx
  import { useLiveRaceStore } from '../store/liveRaceStore'
  import type { RaceTimelineEvent } from '../../../shared/types/f1'

  const EVENT_STYLE: Record<RaceTimelineEvent['eventType'], { label: string; colour: string }> = {
    SafetyCar: { label: 'SC', colour: '#ffcc00' },
    VirtualSafetyCar: { label: 'VSC', colour: '#ffe066' },
    RedFlag: { label: 'RED', colour: '#e8002d' },
    PitStop: { label: 'PIT', colour: '#3671c6' },
    Dnf: { label: 'DNF', colour: '#6b7280' },
    FastestLap: { label: 'FL', colour: '#bf00ff' },
  }

  export function RaceEventTimeline() {
    const timeline = useLiveRaceStore(s => s.timeline)

    if (timeline.length === 0) {
      return (
        <div
          className="px-3 py-4 text-[12px] text-[#6b7280] bg-[#20242c] rounded-[8px]"
          data-testid="race-event-timeline-empty"
        >
          No race events yet
        </div>
      )
    }

    const maxLap = Math.max(1, ...timeline.map(e => e.lapNumber))

    return (
      <div className="relative h-16 bg-[#20242c] rounded-[8px] px-2" data-testid="race-event-timeline">
        <div className="absolute inset-x-2 top-1/2 h-px bg-[#3a4050]" />
        {timeline.map((event, i) => {
          const pct = (event.lapNumber / maxLap) * 100
          const style = EVENT_STYLE[event.eventType]
          return (
            <div
              key={i}
              className="absolute top-1/2 -translate-y-1/2 -translate-x-1/2 flex flex-col items-center"
              style={{ left: `${pct}%` }}
              data-testid={`timeline-event-${event.eventType}`}
              title={`Lap ${event.lapNumber}${event.driverCode ? ` — ${event.driverCode}` : ''}`}
            >
              <span className="w-2 h-2 rounded-full" style={{ backgroundColor: style.colour }} />
              <span className="text-[9px] text-[#9aa1ad] mt-0.5">{style.label}</span>
            </div>
          )
        })}
      </div>
    )
  }
  ```
  **AC 4**: the empty-state branch returns before any lap-number math runs, so there's no risk of a `0/0` division crash.
  **AC 2/3**: this component has no special-casing for "live" vs "ended" — it always renders whatever `timeline` currently holds. It grows automatically as new SignalR snapshots arrive (AC 2) and simply stops changing once the source stops producing new events (AC 3), exactly mirroring the backend design note in Task 4.

### Task 10: Frontend — Wire into `LiveRacePage` (AC: 1)

- [ ] Update `frontend/src/features/live-race/LiveRacePage.tsx` — import and render `<RaceEventTimeline />` inside the existing `flex flex-col gap-4` container (position is a layout call, not an AC — e.g. after `<FastestSectorBoard />`).

### Task 11: Frontend — Tests (AC: 1, 2, 3, 4)

- [ ] Create `frontend/src/features/live-race/RaceEventTimeline/RaceEventTimeline.test.tsx`:
  - Resets store state in `beforeEach` (`timeline: []`).
  - "shows empty state when no events" — asserts `getByTestId('race-event-timeline-empty')`.
  - "renders a marker for each event type" — sets `timeline` with one of each `eventType`, asserts each `timeline-event-*` test id is present.
  - "positions later-lap events further right" — two events at lap 5 and lap 50, assert the lap-50 marker's inline `left` style percentage is greater than the lap-5 marker's.

- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass.
- [ ] Run `npx tsc --noEmit` from `frontend/` — must be clean.

## Dev Notes

### Architecture Alignment

- Adds the two remaining OpenF1 polling loops (`/race_control`, `/pit`) that the architecture doc (`architecture.md` line 190) always intended as part of `RaceDataOrchestrator`'s five source loops (`/position`, `/intervals`, `/car_data`→ mapped to this codebase's `/location`, `/race_control`, `/pit`) but that Stories 2.x/3.x hadn't needed until now (pit *window estimation* in Story 3.3 used stint-transition detection instead of the `/pit` endpoint directly — this story is the first consumer of the raw `/pit` feed).
- Continues the backend-computes/frontend-renders convention: `RaceDataOrchestrator` parses raw OpenF1 messages into typed `RaceTimelineEvent`s; the frontend does no text parsing or classification.
- **OpenF1 schema verified live** against `https://api.openf1.org/v1/race_control?session_key=9488` (Australia 2024, VSC deployment) and `https://api.openf1.org/v1/pit?session_key=9590` (Monza 2024, pit stops) — see field comments in the new DTO files.

### Regressions to Guard

- `PollLapsAsync`'s existing `foreach` body order matters: `_driverCurrentLap`/`_driverLapTimes` update → `ComputeSectorStatus(lap)` → new fastest-lap check. Don't reorder ahead of `ComputeSectorStatus` since that's an independent, already-tested code path; appending after it is safest.
- `_timelineEvents` is a `ConcurrentQueue`, not a `List` — three different polling loops (`PollRaceControlAsync`, `PollPitEventsAsync`, `PollLapsAsync`) enqueue into it concurrently, so it must stay a thread-safe collection type. `BuildSnapshot`'s `.OrderBy(e => e.LapNumber)` produces a stable, deterministic array for the client on every call without needing a lock.
- Do not gate `Timeline` construction on `_sessionMode` — unlike `FastestSectors` (Story 3.4), Timeline must NOT return `null`/empty specifically in fallback mode; it always reflects the accumulated queue (see Task 4 design note).

### Files to Create / Modify

**Backend CREATE:**
- `backend/F1App.Api/Dtos/OpenF1/OpenF1RaceControlDto.cs`
- `backend/F1App.Api/Dtos/OpenF1/OpenF1PitDto.cs`
- `backend/F1App.Api/Models/RaceTimelineEvent.cs`

**Backend MODIFY:**
- `backend/F1App.Api/Clients/IOpenF1Client.cs`
- `backend/F1App.Api/Clients/OpenF1Client.cs`
- `backend/F1App.Api/Models/RaceStateSnapshot.cs`
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs`
- `backend/F1App.Api.Tests/Clients/OpenF1ClientContractTests.cs`
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`

**Frontend CREATE:**
- `frontend/src/features/live-race/RaceEventTimeline/RaceEventTimeline.tsx`
- `frontend/src/features/live-race/RaceEventTimeline/RaceEventTimeline.test.tsx`

**Frontend MODIFY:**
- `frontend/src/shared/types/f1.ts`
- `frontend/src/features/live-race/store/liveRaceStore.ts`
- `frontend/src/features/live-race/hooks/useSignalRConnection.ts`
- `frontend/src/features/live-race/LiveRacePage.tsx`

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.5: Race Event Timeline]
- [Source: _bmad-output/planning-artifacts/architecture.md line 190 — RaceDataOrchestrator's five OpenF1 polling loops]
- Live-verified against `https://api.openf1.org/v1`: `/race_control` and `/pit` schemas, `SafetyCar`/`CarEvent` category conventions.

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

None.

### Completion Notes List

- Implemented as planned, with one refinement over the original task list: the fastest-lap detection block was extracted into its own `internal TryEnqueueFastestLap(OpenF1LapDto lap)` method (mirroring `ComputeSectorStatus`'s testability pattern) instead of inlining it in `PollLapsAsync`, so it could be unit-tested directly rather than only exercised through the polling loop.
- Verified OpenF1 `/race_control` and `/pit` schemas live against `https://api.openf1.org/v1` (Australia 2024 session 9488 for SafetyCar/CarEvent conventions, Monza 2024 session 9590 for pit stops) before writing the DTOs.
- All 127 backend tests and 79 frontend tests pass; `tsc --noEmit` is clean.

### File List

**Backend created:**
- `backend/F1App.Api/Dtos/OpenF1/OpenF1RaceControlDto.cs`
- `backend/F1App.Api/Dtos/OpenF1/OpenF1PitDto.cs`
- `backend/F1App.Api/Models/RaceTimelineEvent.cs`

**Backend modified:**
- `backend/F1App.Api/Clients/IOpenF1Client.cs`
- `backend/F1App.Api/Clients/OpenF1Client.cs`
- `backend/F1App.Api/Models/RaceStateSnapshot.cs`
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs`
- `backend/F1App.Api.Tests/Clients/OpenF1ClientContractTests.cs`
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`

**Frontend created:**
- `frontend/src/features/live-race/RaceEventTimeline/RaceEventTimeline.tsx`
- `frontend/src/features/live-race/RaceEventTimeline/RaceEventTimeline.test.tsx`

**Frontend modified:**
- `frontend/src/shared/types/f1.ts`
- `frontend/src/features/live-race/store/liveRaceStore.ts`
- `frontend/src/features/live-race/hooks/useSignalRConnection.ts`
- `frontend/src/features/live-race/LiveRacePage.tsx`

### Change Log

| Date | Change |
|------|--------|
| 2026-07-10 | Story created via bmad-create-story |
