---
baseline_commit: "483d375"
---

# Story 5.2: Driver Career Profile Page

Status: done

## Story

As a history enthusiast,
I want to see a driver's full career stats,
So that I can understand their place in F1 history.

## Acceptance Criteria

1. **Given** a driver name is clicked anywhere in the app **When** the profile page opens **Then** it shows career totals (races, wins, podiums, poles, fastest laps, titles), Constructor history year by year, and a career cumulative points progression chart, sourced from Ergast.
2. **And** the career chart uses the same visual style as the championship trajectory chart (Epic 4, Story 4.2).

## API behaviour verified live (against `https://api.jolpi.ca/ergast/f1`, the configured `ErgastBaseUrl`) before writing any code

- `/drivers/{driverId}/results.json?limit=1000` returns **every** race in the driver's career (verified: Verstappen = 242 entries as of this check, well under the 1000 cap), each `Races[]` entry with exactly **one** `Results` row (already filtered to that driver), chronologically ascending by season/round, and — critically — each row already carries `grid`, `status`, `points`, and `FastestLap.rank` (all added to `ErgastResultDto` in Stories 4.3/5.1). This one call is the source for races/wins/podiums/poles/fastest-laps/constructor-history/career-points — no other per-stat endpoint is needed.
- `position` is populated even for retirees (e.g. `"position": "13", "positionText": "R"` for a DNF) — reconfirms Story 4.3's finding that `status`, not `position`, is the DNF signal; this story doesn't need DNF detection itself, just noting the same field behaves consistently here.
- **"Titles" cannot be queried as a single all-time call.** Both `/drivers/{driverId}/driverStandings.json` and `/drivers/{driverId}/driverStandings/1.json` return `400 Bad Request: Missing one of the required parameters ['season_year']` on Jolpica — the driverStandings resource requires a season in the path. Confirmed working alternative: `/{season}/driverStandings/1.json` returns that season's champion (reuses the exact existing `ErgastDriverStandingsResponseDto`/`ErgastDriverStandingDto` DTOs from `GetCurrentDriverStandingsAsync`). This story counts titles by calling that endpoint once per **distinct season present in the driver's own results** (already known from the one big results fetch above) and checking whether the returned champion's `driverId` matches — for a 20-season veteran that's ~20 small calls, not 20 large ones, and it's all folded into the same 7-day cache as everything else.
- `/drivers/{driverId}.json` returns driver bio (`givenName`, `familyName`, `nationality`) — reuses the existing `ErgastDriverDto`.
- **"Poles" is derived as `grid == "1"`**, not from a separate qualifying-results call — pole position and grid position 1 are the same thing in the overwhelming majority of races (grid penalties are the rare exception this POC accepts as a known simplification, same spirit as Story 5.1's static-facts gap).

## Tasks / Subtasks

### Task 1: Backend — DTOs and `IErgastClient`/`ErgastClient` methods (AC: 1)

- [ ] Create `backend/F1App.Api/Dtos/Ergast/ErgastDriverInfoResponseDto.cs`:
  ```csharp
  using System.Text.Json.Serialization;

  namespace F1App.Api.Dtos.Ergast;

  public record ErgastDriverInfoResponseDto(
      [property: JsonPropertyName("MRData")] ErgastDriverInfoMrDataDto MRData);

  public record ErgastDriverInfoMrDataDto(
      [property: JsonPropertyName("DriverTable")] ErgastDriverInfoTableDto DriverTable);

  public record ErgastDriverInfoTableDto(
      [property: JsonPropertyName("Drivers")] IReadOnlyList<ErgastDriverDto> Drivers);
  ```
- [ ] `IErgastClient.cs` — add:
  ```csharp
  Task<ErgastDriverDto?> GetDriverInfoAsync(string driverId, CancellationToken cancellationToken);
  Task<IReadOnlyList<ErgastRaceResultRaceDto>> GetAllDriverResultsAsync(string driverId, CancellationToken cancellationToken);
  Task<ErgastDriverStandingDto?> GetSeasonChampionAsync(int season, CancellationToken cancellationToken);
  ```
- [ ] `ErgastClient.cs`:
  ```csharp
  public async Task<ErgastDriverDto?> GetDriverInfoAsync(string driverId, CancellationToken cancellationToken)
  {
      var response = await httpClient.GetFromJsonAsync<ErgastDriverInfoResponseDto>(
          $"drivers/{driverId}.json", cancellationToken)
          ?? throw new InvalidOperationException($"Ergast returned an empty response for driver {driverId}.");

      return response.MRData.DriverTable.Drivers.Count == 0
          ? null
          : response.MRData.DriverTable.Drivers[0];
  }

  public async Task<IReadOnlyList<ErgastRaceResultRaceDto>> GetAllDriverResultsAsync(string driverId, CancellationToken cancellationToken)
  {
      // limit=1000 comfortably covers any F1 career to date (most-raced
      // drivers sit around 350-400 starts).
      var response = await httpClient.GetFromJsonAsync<ErgastRaceResultResponseDto>(
          $"drivers/{driverId}/results.json?limit=1000", cancellationToken)
          ?? throw new InvalidOperationException($"Ergast returned an empty response for {driverId} results.");

      return response.MRData.RaceTable.Races;
  }

  public async Task<ErgastDriverStandingDto?> GetSeasonChampionAsync(int season, CancellationToken cancellationToken)
  {
      var response = await httpClient.GetFromJsonAsync<ErgastDriverStandingsResponseDto>(
          $"{season}/driverStandings/1.json", cancellationToken)
          ?? throw new InvalidOperationException($"Ergast returned an empty response for the {season} champion.");

      var lists = response.MRData.StandingsTable.StandingsLists;
      return lists.Count == 0 || lists[0].DriverStandings.Count == 0
          ? null
          : lists[0].DriverStandings[0];
  }
  ```

### Task 2: Backend — `DriverProfile` models (AC: 1, 2)

- [ ] Create `backend/F1App.Api/Models/DriverProfile.cs`:
  ```csharp
  namespace F1App.Api.Models;

  public record DriverCareerTotals(int Races, int Wins, int Podiums, int Poles, int FastestLaps, int Titles);

  public record ConstructorHistoryEntry(int Season, IReadOnlyList<string> ConstructorNames);

  public record DriverCareerPoint(int Season, int Round, string RaceName, int PointsThisRound, decimal CumulativePoints);

  public record DriverProfile(
      string DriverId,
      string FullName,
      string Nationality,
      DriverCareerTotals CareerTotals,
      IReadOnlyList<ConstructorHistoryEntry> ConstructorHistory,
      IReadOnlyList<DriverCareerPoint> CareerPoints);
  ```

### Task 3: Backend — `DriverProfileService` (AC: 1, 2)

- [ ] `CacheKeys.cs` — add `public static string DriverProfile(string driverId) => $"driver:profile:{driverId}";`.
- [ ] Create `backend/F1App.Api/Services/DriverProfileService.cs`:
  ```csharp
  using System.Globalization;
  using F1App.Api.Clients;
  using F1App.Api.Models;
  using Microsoft.Extensions.Caching.Memory;

  namespace F1App.Api.Services;

  public class DriverProfileService(IErgastClient ergastClient, IMemoryCache cache)
  {
      private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

      public async Task<DriverProfile?> GetDriverProfileAsync(string driverId, CancellationToken cancellationToken)
      {
          var cacheKey = CacheKeys.DriverProfile(driverId);
          if (cache.TryGetValue(cacheKey, out DriverProfile? cached))
              return cached;

          var profile = await ComputeAsync(driverId, cancellationToken);
          cache.Set(cacheKey, profile, CacheTtl);
          return profile;
      }

      private async Task<DriverProfile?> ComputeAsync(string driverId, CancellationToken cancellationToken)
      {
          var driverInfo = await ergastClient.GetDriverInfoAsync(driverId, cancellationToken);
          if (driverInfo is null) return null;

          var races = await ergastClient.GetAllDriverResultsAsync(driverId, cancellationToken);
          var ordered = races
              .OrderBy(r => int.Parse(r.Season, CultureInfo.InvariantCulture))
              .ThenBy(r => int.Parse(r.Round, CultureInfo.InvariantCulture))
              .ToList();

          var wins = 0;
          var podiums = 0;
          var poles = 0;
          var fastestLaps = 0;
          var cumulative = 0m;
          var careerPoints = new List<DriverCareerPoint>();
          var constructorsBySeason = new SortedDictionary<int, List<string>>();

          foreach (var race in ordered)
          {
              var result = race.Results.FirstOrDefault();
              if (result is null) continue;

              if (result.Position == "1") wins++;
              if (result.Position is "1" or "2" or "3") podiums++;
              if (result.Grid == "1") poles++;
              if (result.FastestLap?.Rank == "1") fastestLaps++;

              var season = int.Parse(race.Season, CultureInfo.InvariantCulture);
              constructorsBySeason.TryAdd(season, []);
              if (!constructorsBySeason[season].Contains(result.Constructor.Name))
                  constructorsBySeason[season].Add(result.Constructor.Name);

              var pointsThisRound = (int)decimal.Parse(result.Points ?? "0", CultureInfo.InvariantCulture);
              cumulative += pointsThisRound;
              careerPoints.Add(new DriverCareerPoint(
                  season, int.Parse(race.Round, CultureInfo.InvariantCulture), race.RaceName, pointsThisRound, cumulative));
          }

          var titles = 0;
          foreach (var season in constructorsBySeason.Keys)
          {
              var champion = await ergastClient.GetSeasonChampionAsync(season, cancellationToken);
              if (champion?.Driver.DriverId == driverId) titles++;
          }

          var totals = new DriverCareerTotals(ordered.Count, wins, podiums, poles, fastestLaps, titles);
          var constructorHistory = constructorsBySeason
              .Select(kv => new ConstructorHistoryEntry(kv.Key, kv.Value))
              .ToList();

          return new DriverProfile(
              driverId,
              $"{driverInfo.GivenName} {driverInfo.FamilyName}",
              driverInfo.Nationality ?? string.Empty,
              totals,
              constructorHistory,
              careerPoints);
      }
  }
  ```

### Task 4: Backend — `DriversController`, DI registration (AC: 1, 2)

- [ ] Create `backend/F1App.Api/Controllers/DriversController.cs`:
  ```csharp
  using F1App.Api.Models;
  using F1App.Api.Services;
  using Microsoft.AspNetCore.Mvc;

  namespace F1App.Api.Controllers;

  [ApiController]
  [Route("api/drivers")]
  public class DriversController(DriverProfileService driverProfileService) : ControllerBase
  {
      [HttpGet("{driverId}")]
      public async Task<ActionResult<DriverProfile>> GetProfile(string driverId, CancellationToken cancellationToken)
      {
          var profile = await driverProfileService.GetDriverProfileAsync(driverId, cancellationToken);
          return profile is null ? NotFound() : Ok(profile);
      }
  }
  ```
- [ ] `Program.cs` — `builder.Services.AddScoped<DriverProfileService>();`.

### Task 5: Backend — Tests (AC: 1, 2)

- [ ] `ErgastClientContractTests.cs` — add tests for `GetDriverInfoAsync`, `GetAllDriverResultsAsync`, `GetSeasonChampionAsync`.
- [ ] Create `backend/F1App.Api.Tests/Services/DriverProfileServiceTests.cs`:
  - `GetDriverProfileAsync_ReturnsNullWhenDriverInfoNotFound`.
  - `GetDriverProfileAsync_ComputesCareerTotals` — a small 4-race, 2-season fixture verifying wins/podiums/poles/fastest-laps counts individually (a podium that isn't a win, a pole that didn't convert to a win, a fastest lap set by a non-winner).
  - `GetDriverProfileAsync_CountsTitlesFromDistinctSeasonsOnly` — 3 races across 2 seasons (not 3), verifying `GetSeasonChampionAsync` is called exactly twice, not three times, and only counts seasons where `champion.Driver.DriverId` matches.
  - `GetDriverProfileAsync_BuildsConstructorHistoryGroupedBySeasonWithoutDuplicates` — same constructor across multiple races in one season appears once in that season's entry.
  - `GetDriverProfileAsync_BuildsCumulativeCareerPointsInChronologicalOrder`.
  - Caches result and doesn't re-fetch on second call.
- [ ] `backend/F1App.Api.Tests/Controllers/` — add `DriversControllerTests.cs` (200 + 404 cases).
- [ ] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass. *(Could not be executed in this environment — no .NET SDK installed; same constraint as every prior Epic 4/5 story.)*

### Task 6: Frontend — `driverId` on standings, schema, hook (AC: 1)

- [ ] `frontend/src/shared/api/ergast.ts` — add `driverId: z.string()` to `DriverStandingSchema` (the backend model already carries it; only the frontend zod schema was missing it, silently dropping the field — same class of gap Story 5.1 found and fixed for `RaceWeekend`/`RaceWeekendDetail`). Add `DriverProfileSchema` (`driverId`, `fullName`, `nationality`, `careerTotals: {races,wins,podiums,poles,fastestLaps,titles}`, `constructorHistory: [{season, constructorNames: string[]}]`, `careerPoints: [{season, round, raceName, pointsThisRound, cumulativePoints}]`) and a 404-aware `useDriverProfile(driverId)` hook, mirroring `useCircuitProfile`'s `fetchCircuitProfile`-style helper exactly (extract a small shared `fetchNullable404Json` helper instead of copy-pasting a second near-identical function).
- [ ] `frontend/src/shared/api/queryKeys.ts` — add `driverProfile: (driverId: string) => ['drivers', 'profile', driverId] as const`.
- [ ] `frontend/src/shared/mocks/handlers/ergastHandlers.ts` — add `driverId` to every `sampleDriverStandings` entry (Norris → `'norris'`, etc. — real Ergast driverId slugs); add `sampleDriverProfile` fixture + `/api/drivers/:driverId` handler.

### Task 7: Frontend — `DriverProfilePage` + `DriverCareerChart` (AC: 1, 2)

- [ ] Create `frontend/src/features/profiles/DriverCareerChart.tsx` — single-line Recharts `LineChart` styled identically to `TrajectoryChart.tsx` (same card wrapper classes, axis tick/grid colors via the same `--color-*` tokens, same custom-tooltip pattern showing race name / points that round / cumulative), but with exactly one `Line` (this driver's career) instead of one-per-constructor-leader — satisfies AC 2's "same visual style" requirement by sharing the actual styling constants, not just resembling them.
- [ ] Create `frontend/src/features/profiles/DriverProfilePage.tsx` — one `<h1>`, a stat row for the 6 career totals, a constructor-history table (year, constructor(s)) per the Accessibility Floor real-`<table>` precedent, and `<DriverCareerChart />`.
- [ ] `frontend/src/features/profiles/index.ts` — export `DriverProfilePage`.

### Task 8: Frontend — Routing and driver-name links (AC: 1)

- [ ] `frontend/src/router.tsx` — add `{ path: 'drivers/:driverId', element: <DriverProfilePage /> }`.
- [ ] `frontend/src/features/standings/DriversStandingsTable.tsx` — wrap each driver's name in a `<Link to={`/drivers/${standing.driverId}`}>` (this table is not itself a whole-row link, unlike `RaceWeekendCard`, so no nested-link concern).

### Task 9: Frontend — Tests (AC: 1, 2)

- [ ] Create `frontend/src/features/profiles/DriverProfilePage.test.tsx` (mirrors `CircuitProfilePage.test.tsx`'s MemoryRouter/MSW harness): happy path renders all 6 totals, constructor history rows, and the career chart line; 404 shows a clear "not found" state.
- [ ] Update `frontend/src/features/standings/DriversStandingsTable.test.tsx` (or `StandingsPage.test.tsx` if that's where it's covered) for the new driver-name link.
- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass.
- [ ] Run `npx tsc --noEmit -p tsconfig.app.json` from `frontend/` — must be clean (**use the `-p tsconfig.app.json` form** — Story 5.1 found the bare `--noEmit` invocation silently checks nothing).

## Dev Notes

### Architecture Alignment

- Matches `architecture.md`'s `DriverProfilePage.tsx` (FR-21) and `DriversController.cs` ("FR-21,22: driver profile, head-to-head" — this story implements the FR-21 half; Story 5.3 adds the FR-22 head-to-head route to the same controller).
- AC 2 is satisfied by literally sharing `TrajectoryChart.tsx`'s styling constants/structure in `DriverCareerChart.tsx`, not by independently arriving at a similar-looking chart.

### Regressions to Guard

- `DriverStandingSchema` was silently dropping `driverId` before this story (the backend always sent it; the frontend zod schema just never declared it, so zod stripped it on parse). Any other schema that mirrors a backend model 1:1 should be spot-checked against the actual backend record shape — this is the second time this exact class of gap has been found (`CircuitId` in Story 5.1 was the first).
- `DriverProfileService.ComputeAsync` calls `GetSeasonChampionAsync` once per **distinct season**, not once per race — a driver with many races in the same season (obviously all of them) must not trigger repeat calls for that season.
- `result.Results.FirstOrDefault()` (not `Results[0]`) guards against a theoretical empty `Results` array for a given race entry, even though live verification never observed one — defensive parity with how `CircuitProfileService` and `SeasonWrappedService` both guard against missing/empty result rows rather than indexing directly.

### Files to Create / Modify

**Backend CREATE:**
- `backend/F1App.Api/Dtos/Ergast/ErgastDriverInfoResponseDto.cs`
- `backend/F1App.Api/Models/DriverProfile.cs`
- `backend/F1App.Api/Services/DriverProfileService.cs`
- `backend/F1App.Api/Controllers/DriversController.cs`
- `backend/F1App.Api.Tests/Services/DriverProfileServiceTests.cs`
- `backend/F1App.Api.Tests/Controllers/DriversControllerTests.cs`

**Backend MODIFY:**
- `backend/F1App.Api/Clients/IErgastClient.cs`
- `backend/F1App.Api/Clients/ErgastClient.cs`
- `backend/F1App.Api/Services/CacheKeys.cs`
- `backend/F1App.Api/Program.cs`
- `backend/F1App.Api.Tests/Clients/ErgastClientContractTests.cs`

**Frontend CREATE:**
- `frontend/src/features/profiles/DriverProfilePage.tsx`
- `frontend/src/features/profiles/DriverProfilePage.test.tsx`
- `frontend/src/features/profiles/DriverCareerChart.tsx`

**Frontend MODIFY:**
- `frontend/src/shared/api/ergast.ts`
- `frontend/src/shared/api/queryKeys.ts`
- `frontend/src/shared/mocks/handlers/ergastHandlers.ts`
- `frontend/src/features/profiles/index.ts`
- `frontend/src/features/standings/DriversStandingsTable.tsx`
- `frontend/src/features/standings/StandingsPage.test.tsx` (wrapped its render helper in `MemoryRouter` — `DriversStandingsTable` now renders `<Link>`, which throws outside a Router context — and added a driver-name-link assertion)
- `frontend/src/router.tsx`

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.2: Driver Career Profile Page]
- [Source: _bmad-output/planning-artifacts/architecture.md — DriverProfilePage.tsx, DriversController.cs]
- [Live-verified: `https://api.jolpi.ca/ergast/f1/drivers/max_verstappen/results.json`, `/driverStandings.json` (season-required error), `/2023/driverStandings/1.json`, `/drivers/max_verstappen.json`]
- [Source: frontend/src/features/standings/TrajectoryChart.tsx — visual style this story's career chart reuses per AC 2]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

None.

### Completion Notes List

- Implemented per plan. All key Ergast/Jolpica API assumptions (driver-scoped results endpoint shape, the season-required error on `driverStandings` without a season, the `/​{season}/driverStandings/1.json` champion lookup) were verified live against `https://api.jolpi.ca/ergast/f1` before writing any code, per the story's own "API behaviour verified live" section — no guessing on endpoint shapes this time.
- `DriverCareerChart` deliberately shares `TrajectoryChart.tsx`'s exact styling tokens/structure (card wrapper, axis colors, tooltip layout) rather than approximating them, to genuinely satisfy AC 2 rather than just resemble it.
- Extracted `fetchNullable404Json` as a shared helper (used by both `useCircuitProfile` and the new `useDriverProfile`) instead of copy-pasting Story 5.1's inline `fetchCircuitProfile` a second time.
- Found and fixed the same class of gap Story 5.1 found for `CircuitId`: `DriverStandingSchema` was silently dropping the backend's `driverId` field because the frontend zod schema never declared it. Fixed alongside adding the field needed for driver-name linking.
- `StandingsPage.test.tsx` needed a `MemoryRouter` wrapper for the first time — `DriversStandingsTable` now renders `<Link>` elements, which throw when rendered outside router context.
- **Environment note (same as every Epic 4/5 story)**: no .NET SDK in this environment, so backend changes were written and manually reviewed but not compiler-verified via `dotnet build`/`dotnet test`.
- All frontend tests pass except the 4 pre-existing, unrelated `dateUtils.test.ts` locale failures; `tsc -p tsconfig.app.json` is clean except the one pre-existing `TrackMap.test.tsx` issue noted in Story 5.1; `eslint` is fully clean (no new pre-existing-pattern findings this time).

### File List

See "Files to Create / Modify" above — unchanged from plan except the noted `StandingsPage.test.tsx` MemoryRouter fix.

### Change Log

| Date | Change |
|------|--------|
| 2026-07-10 | Story created and implemented directly (bmad create-story + dev-story cycle) |
