---
baseline_commit: "2cb3c36"
---

# Story 4.1: Standings Page with Toggle

Status: done

## Story

As a fan tracking the championship,
I want to see Drivers' and Constructors' standings with an instant toggle,
So that I can check either championship without a reload.

## Acceptance Criteria

1. **Given** the standings page loads **Then** the Drivers tab shows position, name, nationality flag, Constructor, points, and wins for every driver.
2. **Given** the Constructors tab is selected **When** switched **Then** it shows position, name, nationality flag, points, and wins for every constructor, with no page reload.
3. **And** switching between tabs is instant (local state, no route change, no refetch).
4. Standings render as a real `<table>` element (EXPERIENCE.md Accessibility Floor), not styled divs.

## Tasks / Subtasks

### Task 1: Backend — Add `Nationality` to Ergast driver/constructor DTOs (AC: 1, 2)

- [ ] `backend/F1App.Api/Dtos/Ergast/ErgastStandingsResponseDto.cs` — add a trailing optional `Nationality` property to `ErgastDriverDto` and `ErgastConstructorDto` so existing positional call sites in tests keep compiling untouched:
  ```csharp
  public record ErgastDriverDto(
      [property: JsonPropertyName("driverId")] string DriverId,
      [property: JsonPropertyName("givenName")] string GivenName,
      [property: JsonPropertyName("familyName")] string FamilyName,
      [property: JsonPropertyName("code")] string? Code = null,
      [property: JsonPropertyName("nationality")] string? Nationality = null);
  ```
  ```csharp
  public record ErgastConstructorDto(
      [property: JsonPropertyName("constructorId")] string ConstructorId,
      [property: JsonPropertyName("name")] string Name,
      [property: JsonPropertyName("nationality")] string? Nationality = null);
  ```

### Task 2: Backend — Extend `DriverStanding`/`ConstructorStanding` models (AC: 1, 2)

- [ ] `backend/F1App.Api/Models/DriverStanding.cs` — add trailing `Wins` (int) and `Nationality` (string) with defaults so the existing 6-arg positional constructors in `RaceDataOrchestratorTests`/`WinProbabilityServiceTests` keep compiling:
  ```csharp
  public record DriverStanding(
      int Position,
      string DriverId,
      string DriverName,
      string FullName,
      string ConstructorName,
      decimal Points,
      int Wins = 0,
      string Nationality = "");
  ```
- [ ] `backend/F1App.Api/Models/ConstructorStanding.cs`:
  ```csharp
  public record ConstructorStanding(
      int Position,
      string ConstructorName,
      decimal Points,
      int Wins = 0,
      string Nationality = "");
  ```

### Task 3: Backend — Parse wins/nationality in `StandingsService` (AC: 1, 2)

- [ ] `backend/F1App.Api/Services/StandingsService.cs` — `ToDriverStanding`/`ToConstructorStanding` parse `standing.Wins` (`int.Parse`) and pass through `standing.Driver.Nationality`/`standing.Constructor.Nationality` (`?? ""`):
  ```csharp
  private static DriverStanding ToDriverStanding(ErgastDriverStandingDto standing) =>
      new(
          int.Parse(standing.Position, CultureInfo.InvariantCulture),
          standing.Driver.DriverId,
          standing.Driver.FamilyName,
          $"{standing.Driver.GivenName} {standing.Driver.FamilyName}",
          standing.Constructors.Count > 0 ? standing.Constructors[0].Name : string.Empty,
          decimal.Parse(standing.Points, CultureInfo.InvariantCulture),
          int.Parse(standing.Wins, CultureInfo.InvariantCulture),
          standing.Driver.Nationality ?? string.Empty);

  private static ConstructorStanding ToConstructorStanding(ErgastConstructorStandingDto standing) =>
      new(
          int.Parse(standing.Position, CultureInfo.InvariantCulture),
          standing.Constructor.Name,
          decimal.Parse(standing.Points, CultureInfo.InvariantCulture),
          int.Parse(standing.Wins, CultureInfo.InvariantCulture),
          standing.Constructor.Nationality ?? string.Empty);
  ```
  No controller/route changes — `GET /api/standings/drivers` and `GET /api/standings/constructors` already return the full list (not just top 3); the existing "top 3" behaviour is a frontend slice in `RaceWeekendCard.tsx`.

### Task 4: Backend — Tests (AC: 1, 2)

- [ ] `backend/F1App.Api.Tests/Services/StandingsServiceTests.cs` — extend the `DriverStanding`/`ConstructorStanding` test helpers to pass nationality, and assert `Wins`/`Nationality` map through in `GetCurrentDriverStandingsAsync_MapsPositionNameConstructorAndPoints` / `GetCurrentConstructorStandingsAsync_MapsPositionNameAndPoints`.
- [ ] `backend/F1App.Api.Tests/Controllers/StandingsControllerTests.cs` — assert the JSON body contains `"wins"` and `"nationality"` camelCase keys.
- [ ] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass.

### Task 5: Frontend — Extend schemas and mock data (AC: 1, 2)

- [ ] `frontend/src/shared/api/ergast.ts` — add `wins: z.number()` and `nationality: z.string()` to `DriverStandingSchema` and `ConstructorStandingSchema`.
- [ ] `frontend/src/shared/mocks/handlers/ergastHandlers.ts` — expand `sampleDriverStandings`/`sampleConstructorStandings` to a full 8-driver / 4-constructor roster (mirrors the UX mockup roster) with `wins`/`nationality` populated, since Story 4.1's own test needs more than 3 rows to prove "every driver" renders (not just top 3, which is what the Story 1.3 card already covers).

### Task 6: Frontend — Nationality → country-name map (AC: 1, 2)

- [ ] Create `frontend/src/features/standings/nationalityToCountry.ts` — Ergast returns nationality as an adjective ("British", "Dutch", "Monegasque"), but `CountryFlag` is keyed on country names ("UK", "Netherlands", "Monaco"). Map the common F1 nationalities to the country names already in `COUNTRY_NAME_TO_ISO` (extending that map first for any gaps — e.g. Finland, Denmark, Canada, Brazil aren't all present, "USA"/"Canada" absent for American/Canadian drivers, etc.):
  ```ts
  export const NATIONALITY_TO_COUNTRY: Record<string, string> = {
    British: 'UK',
    Dutch: 'Netherlands',
    Monegasque: 'Monaco',
    Australian: 'Australia',
    Mexican: 'Mexico',
    Italian: 'Italy',
    German: 'Germany',
    French: 'France',
    Spanish: 'Spain',
    Finnish: 'Finland',
    Canadian: 'Canada',
    Brazilian: 'Brazil',
    Japanese: 'Japan',
    Thai: 'Thailand',
    Chinese: 'China',
    Danish: 'Denmark',
    Belgian: 'Belgium',
    Austrian: 'Austria',
    American: 'USA',
    Polish: 'Poland',
    Argentine: 'Argentina',
    'New Zealander': 'New Zealand',
    Swiss: 'Switzerland',
    Swedish: 'Sweden',
    Russian: 'Russia',
  }
  ```
- [ ] `frontend/src/shared/components/CountryFlag.tsx` — extend `COUNTRY_NAME_TO_ISO` with any nationalities' target countries missing from the current race-host-only list: `Finland: 'FI'`, `Denmark: 'DK'`, `Canada: 'CA'` (already present), `Brazil: 'BR'` (present), `Thailand: 'TH'`, `Poland: 'PL'`, `Argentina: 'AR'`, `'New Zealand': 'NZ'`, `Switzerland: 'CH'`, `Sweden: 'SE'` — add only the ones genuinely absent.

### Task 7: Frontend — Constructor accent-dot colours (AC: 1, 2)

- [ ] Create `frontend/src/features/standings/constructorColors.ts` — reuses the four existing `--color-team-*` design tokens for the constructors that have one, falls back to `var(--color-text-dim)` for any other constructor (DESIGN.md: "constructor accents used sparingly," no token exists yet for every team on the grid):
  ```ts
  const CONSTRUCTOR_COLOR: Record<string, string> = {
    'Red Bull Racing': 'var(--color-team-redbull)',
    Ferrari: 'var(--color-team-ferrari)',
    Mercedes: 'var(--color-team-mercedes)',
    McLaren: 'var(--color-team-mclaren)',
  }

  export function constructorColor(name: string): string {
    return CONSTRUCTOR_COLOR[name] ?? 'var(--color-text-dim)'
  }
  ```

### Task 8: Frontend — `DriversStandingsTable` / `ConstructorsStandingsTable` (AC: 1, 2, 4)

- [ ] Create `frontend/src/features/standings/DriversStandingsTable.tsx` — real `<table>` per the Accessibility Floor and the `standings.html` mockup (`Pos | Driver (team dot + flag + name) | Constructor | Points | Wins`), consuming `useDriverStandings()`.
- [ ] Create `frontend/src/features/standings/ConstructorsStandingsTable.tsx` — real `<table>` (`Pos | Constructor (dot + flag + name) | Points | Wins`), consuming `useConstructorStandings()`.
- [ ] Both handle `isPending` (skeleton, mirrors `CalendarSkeleton`) and `isError` (`role="alert"` message, mirrors `CalendarPage`).

### Task 9: Frontend — `StandingsPage` with tab toggle (AC: 1, 2, 3)

- [ ] Create `frontend/src/features/standings/StandingsPage.tsx` — one `<h1>Standings</h1>`, a local `useState<'drivers' | 'constructors'>('drivers')` (no route/query param — AC 3 requires no reload/route change), a tab-toggle pair styled like `TimezoneToggle.tsx`/the mockup's `.tab-toggle`, and conditionally renders `<DriversStandingsTable />` or `<ConstructorsStandingsTable />`.
- [ ] Create `frontend/src/features/standings/index.ts` barrel export.

### Task 10: Frontend — Routing and nav (AC: 1, 2)

- [ ] `frontend/src/router.tsx` — add `{ path: 'standings', element: <StandingsPage /> }` as a sibling of `races/:round` and `live`.
- [ ] `frontend/src/App.tsx` — add a third `<Link to="/standings">Standings</Link>` beside "Calendar"/"Live Race".

### Task 11: Frontend — Tests (AC: 1, 2, 3, 4)

- [ ] Create `frontend/src/features/standings/StandingsPage.test.tsx` (mirrors `CalendarPage.test.tsx`'s `QueryClientProvider` + MSW pattern):
  - Renders the Drivers table by default, showing every mocked driver row (not just 3) with position/name/constructor/points/wins.
  - Clicking "Constructors" switches instantly (no `waitFor`/refetch needed — same query client, already-fetched data) to the constructors table.
  - Asserts `getByRole('table')` is present (real `<table>`, not styled divs).
- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass.
- [ ] Run `npx tsc --noEmit` from `frontend/` — must be clean.

## Dev Notes

### Architecture Alignment

- Matches `architecture.md`'s `frontend/src/features/standings/` file tree (`StandingsPage.tsx`, `DriversStandingsTable.tsx`, `ConstructorsStandingsTable.tsx`) and `StandingsController.cs` FR-17/18 comment.
- Reuses Epic 1's established Ergast client, `IMemoryCache` 1h TTL convention, `queryKeys.standings.*`, and `useDriverStandings`/`useConstructorStandings` hooks as-is — this story is additive on the DTOs/models, not a new pipeline.
- `nationalityToCountry.ts` and `constructorColors.ts` are scoped to `features/standings/` (not `shared/`) since no other feature currently needs nationality-flag or constructor-dot rendering; can be promoted to `shared/` later if Epic 5/6 profile pages need the same mapping.

### Regressions to Guard

- `Nationality`/`Wins` are added as **trailing optional** parameters on existing positional records (`ErgastDriverDto`, `ErgastConstructorDto`, `DriverStanding`, `ConstructorStanding`) specifically so the many existing positional constructor calls across `RaceScheduleServiceTests`, `RaceDataOrchestratorTests`, `WinProbabilityServiceTests`, `RacesControllerTests` keep compiling without modification.
- Story 1.3's `RaceWeekendCard.tsx` top-3 snippet reads `driverStandings`/`constructorStandings` from the same hooks/schemas — adding new required zod fields must not change the shape of the fields it already reads (`position`, `driverName`, `constructorName`, `points`).

### Files to Create / Modify

**Backend MODIFY:**
- `backend/F1App.Api/Dtos/Ergast/ErgastStandingsResponseDto.cs`
- `backend/F1App.Api/Models/DriverStanding.cs`
- `backend/F1App.Api/Models/ConstructorStanding.cs`
- `backend/F1App.Api/Services/StandingsService.cs`
- `backend/F1App.Api.Tests/Services/StandingsServiceTests.cs`
- `backend/F1App.Api.Tests/Controllers/StandingsControllerTests.cs`

**Frontend CREATE:**
- `frontend/src/features/standings/StandingsPage.tsx`
- `frontend/src/features/standings/DriversStandingsTable.tsx`
- `frontend/src/features/standings/ConstructorsStandingsTable.tsx`
- `frontend/src/features/standings/nationalityToCountry.ts`
- `frontend/src/features/standings/constructorColors.ts`
- `frontend/src/features/standings/index.ts`
- `frontend/src/features/standings/StandingsPage.test.tsx`

**Frontend MODIFY:**
- `frontend/src/shared/api/ergast.ts`
- `frontend/src/shared/mocks/handlers/ergastHandlers.ts`
- `frontend/src/shared/components/CountryFlag.tsx`
- `frontend/src/router.tsx`
- `frontend/src/App.tsx`
- `frontend/src/features/calendar/RaceWeekendCard.test.tsx` (hardcoded `'312 pts'` assertion updated to `'298 pts'` — the expanded standings mock roster changed Norris's mock points to match the UX mockup)

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.1: Standings Page with Toggle]
- [Source: _bmad-output/planning-artifacts/architecture.md line 578, 520-528 — StandingsController, feature folder tree]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-F1_poc-2026-06-16/mockups/standings.html — tab-toggle, standings-table markup/classes]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-F1_poc-2026-06-16/EXPERIENCE.md — Accessibility Floor: real `<table>`; Standings toggle: instant, no reload]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

None.

### Completion Notes List

- Implemented as planned. `Nationality`/`Wins` added as trailing optional parameters on existing positional records, so no unrelated test file needed modification beyond the standings-specific ones.
- Expanded the standings mock roster (`ergastHandlers.ts`) to 8 drivers / 4 constructors mirroring the UX mockup so the "every driver/constructor renders" AC has a real multi-row fixture to assert against; this changed Norris's mock points (312 → 298), requiring a one-line fixup in `RaceWeekendCard.test.tsx`'s pre-existing assertion.
- **Environment note**: this environment has no .NET SDK installed, so `dotnet build`/`dotnet test` could not be executed here — the backend changes (Task 1-4) were written and manually reviewed against the existing test patterns but not compiler-verified. `frontend/.env.local` also didn't exist in this checkout (gitignored, required by architecture.md for `VITE_API_BASE_URL`) — created it from `.env.example` to unblock frontend test runs; this uncovered that it (and the 4 pre-existing `dateUtils.test.ts` locale-format failures) predate this story and are unrelated to it.
- All 82 frontend tests pass except the 4 pre-existing, unrelated `dateUtils.test.ts` locale failures (present before this story on a clean `main` checkout); `tsc --noEmit` and `eslint` are clean.

### File List

See "Files to Create / Modify" above — unchanged from plan.

### Change Log

| Date | Change |
|------|--------|
| 2026-07-10 | Story created and implemented directly (bmad create-story + dev-story cycle) |
