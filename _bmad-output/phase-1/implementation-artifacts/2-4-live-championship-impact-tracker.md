---
baseline_commit: "5fb9314726cbf86cfd8a703482c0fa7ef554c7ea"
---

# Story 2.4: Live Championship Impact Tracker

Status: review

## Story

As a fan following championship stakes,
I want to see how each driver's current race position would affect their points gap to their nearest rival,
So that I understand what's on the line right now.

## Acceptance Criteria

1. **Given** the gap list **When** a driver entry renders **Then** it shows a Championship Delta annotation: the projected points gap to their nearest rival if the race ended at the current position.
2. **Given** the calculation **When** run **Then** it merges current race provisional F1 points (25/18/15/12/10/8/6/4/2/1 for P1-P10, 0 for P11+) with the most recent official Ergast driver standings.
3. **Given** the championship leader in the projected standings **When** displayed **Then** the delta shows `"+X"` — their lead over projected P2.
4. **Given** any other driver in the projected standings **When** displayed **Then** the delta shows `"−X"` — how far behind the driver directly above them in projected standings.
5. **Given** the live race page **When** the gap list panel header renders **Then** it is clearly labelled with "pts if race ended now" to distinguish deltas from official standings.
6. **Given** Ergast standings are unavailable **When** `_driverStandings` is empty **Then** `ChampionshipDelta` is `null` for all drivers — no crash, no stale data shown.
7. **Given** an OpenF1 driver with no match in Ergast standings **When** delta computed **Then** `ChampionshipDelta` is `null` for that driver — no crash.
8. **Given** positions change during the race **When** a new snapshot is published **Then** the deltas update automatically (computed in `BuildSnapshot` every tick).

## Tasks / Subtasks

### Task 1: Add `_driverStandings` field and hydration to `RaceDataOrchestrator` (AC: 2, 6)

- [x] In `backend/F1App.Api/Services/RaceDataOrchestrator.cs`, add the field after `_driverInfo`:
  ```csharp
  internal IReadOnlyList<DriverStanding> _driverStandings = [];
  ```

- [x] Extend `InitialiseDriverInfoAsync` to also fetch driver standings via `StandingsService` (Scoped — must use `IServiceScopeFactory`). Append a second try/catch block after the existing one:
  ```csharp
  try
  {
      using var scope = scopeFactory.CreateScope();
      var standingsSvc = scope.ServiceProvider.GetRequiredService<StandingsService>();
      _driverStandings = await standingsSvc.GetCurrentDriverStandingsAsync(ct);
      logger.LogInformation("RaceDataOrchestrator: loaded {Count} driver standings", _driverStandings.Count);
  }
  catch (Exception ex) when (ex is not OperationCanceledException)
  {
      logger.LogWarning(ex, "RaceDataOrchestrator: failed to load driver standings; championship delta unavailable");
  }
  ```
  **Why scope?** `StandingsService` is `AddScoped<StandingsService>()` in `Program.cs`. `RaceDataOrchestrator` is a singleton `BackgroundService`, so it cannot inject scoped services directly. The same `IServiceScopeFactory` pattern is already used in `IsRaceWeekendActiveAsync`.

  **Why `StandingsService`?** It owns the 1h `IMemoryCache` TTL logic. Using it ensures the cache is shared across all consumers. `IMemoryCache` is Singleton so the cache state is preserved across scopes.

  **Usings to add** (if not already present): `using F1App.Api.Services;` (already present — `StandingsService` is in the same namespace).

### Task 2: Add `RacePointsForPosition` static helper (AC: 2)

- [x] In `backend/F1App.Api/Services/RaceDataOrchestrator.cs`, add a static helper method accessible to tests:
  ```csharp
  internal static int RacePointsForPosition(int position) => position switch
  {
      1 => 25, 2 => 18, 3 => 15, 4 => 12, 5 => 10,
      6 => 8,  7 => 6,  8 => 4,  9 => 2,  10 => 1,
      _ => 0
  };
  ```
  **Note:** `InternalsVisibleTo("F1App.Api.Tests")` is already declared in `Program.cs` at the assembly level, so `internal static` methods are directly callable in tests.

### Task 3: Implement `ComputeChampionshipDeltas` and wire into `BuildSnapshot` (AC: 1, 2, 3, 4, 6, 7, 8)

- [x] Add a private helper method `ComputeChampionshipDeltas` to `RaceDataOrchestrator`. Place it after `BuildSnapshot`:
  ```csharp
  private Dictionary<int, string?> ComputeChampionshipDeltas(List<DriverState> raceDrivers)
  {
      // Build a lookup: first-3-chars-of-family-name → standing
      // e.g. "VER" → DriverStanding for Verstappen
      var standingByPrefix = new Dictionary<string, DriverStanding>(StringComparer.OrdinalIgnoreCase);
      foreach (var s in _driverStandings)
      {
          var prefix = s.DriverName.Length >= 3 ? s.DriverName[..3] : s.DriverName;
          standingByPrefix.TryAdd(prefix, s); // first entry wins on collision (extremely rare)
      }

      // Compute projected championship totals for each race driver that can be matched
      var projected = new List<(int DriverNumber, decimal ProjectedPoints)>();
      foreach (var driver in raceDrivers)
      {
          if (!_driverInfo.TryGetValue(driver.DriverNumber, out var info)) continue;
          if (!standingByPrefix.TryGetValue(info.NameAcronym, out var standing)) continue;

          var racePoints = RacePointsForPosition(driver.Position);
          projected.Add((driver.DriverNumber, standing.Points + racePoints));
      }

      // Sort descending by projected points
      projected.Sort((a, b) => b.ProjectedPoints.CompareTo(a.ProjectedPoints));

      var result = new Dictionary<int, string?>();
      for (int i = 0; i < projected.Count; i++)
      {
          var (driverNum, pts) = projected[i];
          if (i == 0)
          {
              // Championship leader: show gap they lead P2 by
              if (projected.Count > 1)
              {
                  var lead = pts - projected[1].ProjectedPoints;
                  result[driverNum] = $"+{lead:0.#}";
              }
          }
          else
          {
              // Everyone else: gap to driver directly above in projected standings
              var trail = projected[i - 1].ProjectedPoints - pts;
              result[driverNum] = $"−{trail:0.#}";
          }
      }

      return result;
  }
  ```

  **Format details:**
  - `0.#` — shows at least 1 integer digit, at most 1 decimal digit, but omits the decimal if zero. Since F1 points are integers, output is always like `"+45"` or `"−12"` (never `"−12.0"`).
  - `−` is the Unicode minus sign (U+2212), not the hyphen-minus `-`. Renders correctly in all modern browsers.
  - No delta is set for the championship leader when there's only 1 matched driver (edge case: `null`).

- [x] Wire `ComputeChampionshipDeltas` into `BuildSnapshot`. After the existing `foreach` loop over `_latestPositions` that builds the `drivers` list, and **before** the `lapChart` block, add:
  ```csharp
  // Championship deltas: computed as a post-pass so all projected totals are available
  if (_driverStandings.Count > 0)
  {
      var deltaMap = ComputeChampionshipDeltas(drivers);
      for (int i = 0; i < drivers.Count; i++)
          drivers[i] = drivers[i] with { ChampionshipDelta = deltaMap.GetValueOrDefault(drivers[i].DriverNumber) };
  }
  ```

  **Why a post-pass?** The delta for each driver depends on ALL other drivers' projected totals (to find the nearest rival). So it cannot be computed inside the per-driver `foreach` loop — all positions must be known first.

  **`with {}` record syntax**: `DriverState` is a `record` so `with {}` creates a shallow copy with the specified property changed. `drivers` is `List<DriverState>` (mutable list), so index-assignment works.

  **No new field added to `DriverState`**: `ChampionshipDelta` is already `string? ChampionshipDelta { get; init; }` in `DriverState.cs`. The comment `// Placeholders for Stories 2.2–2.4` should be removed from `DriverState.cs` as all three placeholders are now populated.

- [x] Remove the now-stale placeholder comment from `backend/F1App.Api/Models/DriverState.cs`:
  ```csharp
  // Before (remove this line):
  // Placeholders for Stories 2.2–2.4 (not populated in this story):
  ```

### Task 4: Backend tests for `RacePointsForPosition` and `ChampionshipDelta` in `BuildSnapshot` (AC: 1, 2, 3, 4, 6, 7)

- [x] In `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs`, add a `MakeStanding` helper alongside existing helpers:
  ```csharp
  private static DriverStanding MakeStanding(string driverName, decimal points, int position = 1) =>
      new(position, driverName.ToLowerInvariant(), driverName, $"First {driverName}", "Team", points);
  ```
  **Note:** `DriverStanding` constructor is `(int Position, string DriverId, string DriverName, string FullName, string ConstructorName, decimal Points)`.

- [x] Add tests for `RacePointsForPosition`:
  ```csharp
  [Theory]
  [InlineData(1, 25)]
  [InlineData(2, 18)]
  [InlineData(3, 15)]
  [InlineData(10, 1)]
  [InlineData(11, 0)]
  [InlineData(20, 0)]
  public void RacePointsForPosition_ReturnsCorrectPoints(int position, int expected)
  {
      Assert.Equal(expected, RaceDataOrchestrator.RacePointsForPosition(position));
  }
  ```

- [x] Add tests for `ChampionshipDelta` in `BuildSnapshot`:
  ```csharp
  [Fact]
  public void BuildSnapshot_LeaderHasPositiveChampionshipDelta()
  {
      var sut = CreateOrchestrator();
      // Driver 33 P1 in race, Driver 44 P2 in race
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      sut._latestPositions[44] = MakePosition(44, 2, DateTimeOffset.UtcNow);
      // VER = 300 official pts, HAM = 250 official pts
      sut._driverStandings = [MakeStanding("Verstappen", 300), MakeStanding("Hamilton", 250)];
      sut._driverInfo = new Dictionary<int, OpenF1DriverInfoDto>
      {
          [33] = new(33, "VER", "Red Bull Racing", "3671C6"),
          [44] = new(44, "HAM", "Mercedes", "27F4D2"),
      };

      var snapshot = sut.BuildSnapshot();

      // VER projected: 300 + 25 = 325; HAM projected: 250 + 18 = 268; gap = 57
      var ver = snapshot.Drivers.Single(d => d.DriverNumber == 33);
      Assert.Equal("+57", ver.ChampionshipDelta);
  }

  [Fact]
  public void BuildSnapshot_TrailerHasNegativeChampionshipDelta()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      sut._latestPositions[44] = MakePosition(44, 2, DateTimeOffset.UtcNow);
      sut._driverStandings = [MakeStanding("Verstappen", 300), MakeStanding("Hamilton", 250)];
      sut._driverInfo = new Dictionary<int, OpenF1DriverInfoDto>
      {
          [33] = new(33, "VER", "Red Bull Racing", "3671C6"),
          [44] = new(44, "HAM", "Mercedes", "27F4D2"),
      };

      var snapshot = sut.BuildSnapshot();

      // HAM is projected P2; gap to VER = 325 - 268 = 57
      var ham = snapshot.Drivers.Single(d => d.DriverNumber == 44);
      Assert.Equal("−57", ham.ChampionshipDelta);
  }

  [Fact]
  public void BuildSnapshot_NoStandings_DeltaIsNull()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      // _driverStandings left as empty []

      var snapshot = sut.BuildSnapshot();

      Assert.Null(snapshot.Drivers[0].ChampionshipDelta);
  }

  [Fact]
  public void BuildSnapshot_UnmatchedDriver_DeltaIsNull()
  {
      var sut = CreateOrchestrator();
      sut._latestPositions[99] = MakePosition(99, 1, DateTimeOffset.UtcNow);
      sut._driverStandings = [MakeStanding("Verstappen", 300)];
      // Driver 99 has no _driverInfo entry → no acronym for matching

      var snapshot = sut.BuildSnapshot();

      Assert.Null(snapshot.Drivers[0].ChampionshipDelta);
  }

  [Fact]
  public void BuildSnapshot_SingleMatchedDriver_LeaderDeltaIsNull()
  {
      // Only 1 driver matched → no rival to compute gap against → null
      var sut = CreateOrchestrator();
      sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
      sut._driverStandings = [MakeStanding("Verstappen", 300)];
      sut._driverInfo = new Dictionary<int, OpenF1DriverInfoDto>
      {
          [33] = new(33, "VER", "Red Bull Racing", "3671C6"),
      };

      var snapshot = sut.BuildSnapshot();

      Assert.Null(snapshot.Drivers[0].ChampionshipDelta);
  }
  ```

- [x] The existing tests must also add the `using F1App.Api.Clients;` import if not present (needed for `OpenF1DriverInfoDto` constructor call in new tests). Check existing imports — `using F1App.Api.Dtos.OpenF1;` is already there for `OpenF1PositionDto`, etc. `OpenF1DriverInfoDto` is in the same namespace.

- [x] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass (previous 53 + 6 theory cases + 5 facts = at minimum 58 passing).

### Task 5: Update `DriverRow.tsx` to display `championshipDelta` (AC: 1, 3, 4)

- [x] Edit `frontend/src/features/live-race/GapList/DriverRow.tsx`. Add the championship delta annotation after the tyre section, before the `ml-auto` gap span. The delta should be muted, small, and right-aligned:

  ```tsx
  {driver.championshipDelta !== null && driver.championshipDelta !== undefined && (
    <span
      className="text-[10px] text-[#6b7280] tabular-nums shrink-0"
      data-testid="championship-delta"
    >
      {driver.championshipDelta}
    </span>
  )}
  ```

  **Placement**: Add this span between the tyre section (lines ~50-63) and the `<span className="ml-auto tabular-nums">` gap display. The `ml-auto` on the gap pushes gap to the far right; the delta appears left of it in the flow.

  **Full updated return JSX structure** (after tyre block, before gap):
  ```tsx
  {/* existing tyre block */}
  {driver.tyreCompound !== null ? (
    <span className="flex items-center gap-1 shrink-0">
      ...existing tyre/stintLaps content...
    </span>
  ) : null}
  {/* NEW: championship delta */}
  {driver.championshipDelta !== null && driver.championshipDelta !== undefined && (
    <span
      className="text-[10px] text-[#6b7280] tabular-nums shrink-0"
      data-testid="championship-delta"
    >
      {driver.championshipDelta}
    </span>
  )}
  {/* existing gap */}
  <span className="ml-auto tabular-nums">{gapDisplay}</span>
  ```

### Task 6: Add "if race ended now" label to `GapList.tsx` header (AC: 5)

- [x] Edit `frontend/src/features/live-race/GapList/GapList.tsx`. In the panel header, add a small subtitle below "Race Order":

  ```tsx
  <div className="flex items-center justify-between px-[10px] py-[8px] border-b border-[#2a2f38]">
    <div className="flex flex-col">
      <span className="text-[11.5px] font-semibold tracking-[0.04em] uppercase text-[#9aa1ad]">
        Race Order
      </span>
      <span className="text-[9px] text-[#6b7280] tracking-wide">
        pts if race ended now
      </span>
    </div>
    <span ...existing connection status...>
      ...
    </span>
  </div>
  ```

  **Note**: Wrapping "Race Order" in a `<div className="flex flex-col">` container to stack the subtitle below it. The connection status span on the right is unchanged.

### Task 7: Update `GapList.test.tsx` with championship delta tests (AC: 1, 5, 6)

- [x] In `frontend/src/features/live-race/GapList/GapList.test.tsx`, add tests for the new championship delta rendering and the "if race ended now" label:
  ```typescript
  it('shows championship delta when set', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { championshipDelta: '+45' }),
      },
    })
    render(<GapList />)
    expect(screen.getByTestId('championship-delta').textContent).toBe('+45')
  })

  it('hides championship delta when null', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { championshipDelta: null }),
      },
    })
    render(<GapList />)
    expect(screen.queryByTestId('championship-delta')).not.toBeInTheDocument()
  })

  it('shows negative delta for trailing driver', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { championshipDelta: '−12' }),
      },
    })
    render(<GapList />)
    expect(screen.getByTestId('championship-delta').textContent).toBe('−12')
  })

  it('renders "pts if race ended now" label in header', () => {
    render(<GapList />)
    expect(screen.getByText('pts if race ended now')).toBeInTheDocument()
  })
  ```

  **Note on existing `beforeEach`**: It calls `useLiveRaceStore.setState({ drivers: {}, connectionStatus: 'disconnected', lastSnapshotTime: null })`. This does NOT include `lapChart` — but Zustand `setState` is a shallow merge, so `lapChart` keeps its previous value. This is fine because GapList doesn't read `lapChart`.

- [x] Run `npm test` from `frontend/` — all tests must pass (previous 54 + 4 new = 58 total).

## Dev Notes

### Championship Delta Calculation — How It Works

1. **Official standings** (Ergast): fetched once per session in `InitialiseDriverInfoAsync`, cached in `_driverStandings`. Points are `decimal` (whole numbers for modern F1 seasons, but stored as decimal for type safety).

2. **Race provisional points** (`RacePointsForPosition`): F1 standard allocation. P1=25, P2=18, P3=15, P4=12, P5=10, P6=8, P7=6, P8=4, P9=2, P10=1, P11+=0. Does NOT include fastest-lap bonus (simplification for POC).

3. **Matching**: `OpenF1DriverInfoDto.NameAcronym` (3-char, e.g. "VER") is matched to `DriverStanding.DriverName` (family name, e.g. "Verstappen") by comparing the first 3 characters case-insensitively. This works for all current F1 drivers. Edge case: if two drivers had the same 3-letter family prefix (vanishingly rare), `TryAdd` keeps the first entry.

4. **Projection**: `projected = officialPoints + RacePointsForPosition(currentRacePosition)`. This is computed for all matched drivers simultaneously.

5. **Nearest rival**: Drivers sorted by projected total descending. P1 shows lead over P2; P2+ shows deficit to the driver directly above.

6. **Labelling**: Architecture mandates "clearly labelled... as projection based on last official standings". Done via "pts if race ended now" label in GapList header.

### `DriverState.ChampionshipDelta` — Already Stubbed

`ChampionshipDelta` is already declared in `DriverState.cs` as `public string? ChampionshipDelta { get; init; }`. It defaults to `null`. Stories 2.2 and 2.3 preserved it as null. This story populates it in `BuildSnapshot`. **No type changes needed** to `DriverState`.

The `f1.ts` TypeScript type already has `championshipDelta: string | null`. **No frontend type changes needed.**

### Matching `NameAcronym` to `DriverName` — Verified for 2025 Grid

All 2025 F1 driver codes match the first 3 characters of their Ergast family name:
- VER → Verstappen, NOR → Norris, LEC → Leclerc, PIA → Piastri
- HAM → Hamilton, RUS → Russell, SAI → Sainz, ALO → Alonso
- STR → Stroll, TSU → Tsunoda, LAW → Lawson, ANT → Antonelli
- HUL → Hulkenberg, MAG → Magnussen, GAS → Gasly, OCO → Ocon
- BOT → Bottas, ZHO → Zhou, BEA → Bearman, SAR → Sargeant

### Architecture Compliance

- **No business logic in controllers** — all computation in `BuildSnapshot` and `ComputeChampionshipDeltas` within `RaceDataOrchestrator`.
- **`DateTimeOffset` throughout** — not used directly in delta calculation, but `_driverStandings` is fetched with an async method using the existing `CancellationToken` pattern.
- **Scoped service from singleton** — must use `IServiceScopeFactory` pattern (already established in `IsRaceWeekendActiveAsync`).
- **`InternalsVisibleTo("F1App.Api.Tests")`** — already declared in `Program.cs`. `internal static RacePointsForPosition` and `internal IReadOnlyList<DriverStanding> _driverStandings` are accessible from tests.
- **`useShallow` in Zustand selectors** — `DriverRow` reads a single primitive field (`driver.championshipDelta`) so no `useShallow` needed there. `GapList` already uses `useShallow` for `sortedDriverIds`. No changes needed.
- **No new npm packages** — pure rendering addition to existing components.

### Regressions to Guard

- **Existing `BuildSnapshot` behaviour**: The `with {}` post-pass for deltas updates the in-memory `drivers` list; it does NOT affect the `_latestPositions`, `_latestIntervals`, `_latestStints`, `_driverLapTimes` dictionaries. No polling loop data is mutated.
- **`lapChart` computation**: Unchanged. The delta post-pass happens BEFORE the `lapChart` block in `BuildSnapshot`.
- **All existing `RaceDataOrchestratorTests`**: The new `_driverStandings` field defaults to `[]` (empty), so `if (_driverStandings.Count > 0)` guards all delta computation. Existing tests that don't set `_driverStandings` continue to produce `ChampionshipDelta = null` — no regressions.
- **GapList tests**: `makeDriver` helper already has `championshipDelta: null`. Tests that don't explicitly set it continue to pass. The new "pts if race ended now" label appears in all GapList renders — update any test that uses `getByText` in a way that would conflict (unlikely; label text is unique).
- **`DriverRow` existing tests** in GapList.test.tsx: all use `championshipDelta: null`, so no delta element renders — `queryByTestId('championship-delta')` would be null. No changes needed to existing tests.

### `_driverInfo` Type for Tests

When setting `sut._driverInfo` in tests, use:
```csharp
sut._driverInfo = new Dictionary<int, OpenF1DriverInfoDto>
{
    [33] = new OpenF1DriverInfoDto(33, "VER", "Red Bull Racing", "3671C6"),
};
```
`_driverInfo` is declared `internal IReadOnlyDictionary<int, OpenF1DriverInfoDto>` — assignable from `Dictionary<T, U>` since `Dictionary` implements `IReadOnlyDictionary`.

### `DriverStanding` Constructor

```csharp
public record DriverStanding(
    int Position,
    string DriverId,        // slug e.g. "verstappen"
    string DriverName,      // family name e.g. "Verstappen" — used for 3-char prefix match
    string FullName,        // "Max Verstappen"
    string ConstructorName,
    decimal Points);
```
Use `DriverName` (3rd param) for the prefix match, not `DriverId` or `FullName`.

### Files to Create / Modify

**Backend (2 modified):**
- `backend/F1App.Api/Models/DriverState.cs` — MODIFY: remove stale placeholder comment
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` — MODIFY: add `_driverStandings`, extend `InitialiseDriverInfoAsync`, add `RacePointsForPosition`, add `ComputeChampionshipDeltas`, update `BuildSnapshot`
- `backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs` — MODIFY: add `MakeStanding` helper + tests

**Frontend (2 modified):**
- `frontend/src/features/live-race/GapList/DriverRow.tsx` — MODIFY: add championship delta display
- `frontend/src/features/live-race/GapList/GapList.tsx` — MODIFY: add "pts if race ended now" header label
- `frontend/src/features/live-race/GapList/GapList.test.tsx` — MODIFY: add 4 new tests

### Unicode Minus Sign

The delta for trailing drivers uses `−` (U+2212, true minus sign) not `-` (U+002D, hyphen-minus). In C# string: `$"−{trail:0.#}"`. In the TypeScript test: `'−12'` (copy-paste the Unicode char to avoid hyphen confusion). Both strings must use the same character for assertions to pass.

### References

- Story 2.4 acceptance criteria: [Source: epics.md#Story 2.4: Live Championship Impact Tracker]
- FR-15 championship impact tracker: [Source: epics.md line 49, architecture.md line 71]
- `DriverState.ChampionshipDelta` stub: [Source: backend/F1App.Api/Models/DriverState.cs]
- `DriverStanding` model: [Source: backend/F1App.Api/Models/DriverStanding.cs]
- `StandingsService.GetCurrentDriverStandingsAsync`: [Source: backend/F1App.Api/Services/StandingsService.cs]
- `IServiceScopeFactory` scope pattern: [Source: backend/F1App.Api/Services/RaceDataOrchestrator.cs#IsRaceWeekendActiveAsync]
- F1 points allocation: [Source: epics.md#Story 2.4] (standard 25/18/15/12/10/8/6/4/2/1)
- Architecture camelCase mandate: [Source: architecture.md#Backend Architecture] — covered by SignalR JSON config already applied in Story 2.3 fix
- `InternalsVisibleTo` declaration: [Source: backend/F1App.Api/Program.cs line 8]
- UX: "provisional championship delta. Never wraps to a second line": [Source: ux-designs/EXPERIENCE.md line 54]
- Architecture: "championship delta. ~2-4KB per snapshot": [Source: architecture.md line 196]
- Architecture: "Merge must be clearly labelled in UI": [Source: architecture.md line 71]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- ✅ Added `_driverStandings` field + standings fetch in `InitialiseDriverInfoAsync` via scoped `StandingsService`
- ✅ Added `internal static RacePointsForPosition` (P1=25…P10=1, P11+=0) — verified with 12 theory test cases
- ✅ Added `ComputeChampionshipDeltas` private method: family-name prefix match, projected total sort, nearest-rival gap
- ✅ Wired delta post-pass into `BuildSnapshot` using record `with {}` syntax — 5 fact tests confirm all ACs (leader positive, trailer negative, no standings → null, unmatched → null, single driver → null)
- ✅ Removed stale placeholder comment from `DriverState.cs`
- ✅ `DriverRow.tsx` renders `driver.championshipDelta` when non-null (data-testid="championship-delta")
- ✅ `GapList.tsx` header now has "pts if race ended now" subtitle — architecture labelling requirement met
- ✅ 4 new frontend tests; all 58 pass (previous 54 + 4 new)
- ✅ All 70 backend tests pass (previous 53 + 17 new: 12 theory + 5 fact)

### File List

- backend/F1App.Api/Models/DriverState.cs
- backend/F1App.Api/Services/RaceDataOrchestrator.cs
- backend/F1App.Api.Tests/Services/RaceDataOrchestratorTests.cs
- frontend/src/features/live-race/GapList/DriverRow.tsx
- frontend/src/features/live-race/GapList/GapList.tsx
- frontend/src/features/live-race/GapList/GapList.test.tsx
- _bmad-output/implementation-artifacts/2-4-live-championship-impact-tracker.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

## Change Log

| Date | Change |
|------|--------|
| 2026-06-23 | Story created via bmad-create-story |
| 2026-06-23 | Story implemented — 70 backend tests, 58 frontend tests, all passing |
