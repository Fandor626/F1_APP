---
baseline_commit: da45528
---

# Story 7.2: Persistent Championship Sidebar

Status: review

## Change Log

- 2026-07-13: Implemented persistent Championship Sidebar (FR-3). New `ChampionshipSidebar.tsx` reuses the existing standings hooks (AD-8, no new fetch), desktop sticky left rail via a responsive grid in `CalendarPage.tsx`, mobile collapsible drawer. All 5 ACs covered by tests; full suite + typecheck + lint clean (same 11 pre-existing unrelated failures as Story 7.1).

## Story

As a returning fan,
I want to see current Drivers' and Constructors' standings right on the Calendar page,
so that I don't have to visit Standings separately just to check who's leading.

## Acceptance Criteria

1. **Given** I am on the Calendar page at a desktop-width viewport, **when** the page loads, **then** a sticky left-rail sidebar shows current Drivers' and Constructors' Championship standings.
2. **Given** the sidebar is showing standings, **when** I compare its values to the Standings page, **then** both are identical, sourced from the same query — no separate fetch/cache path (Architecture AD-8).
3. **Given** I switch the Calendar filter (Story 7.1) or scroll the race list, **when** I look at the sidebar, **then** it remains visible/reachable and its content doesn't reset.
4. **Given** a mobile-width viewport (`< md`), **when** the page loads, **then** the sidebar collapses to a tappable drawer summary above the race list, expanding to the same two groups on tap.
5. **Given** a screen-reader user navigating page landmarks, **when** they reach the sidebar, **then** it's an independently reachable, labelled region (`aria-label="Championship standings"`). (FR-3 · PRD §4.1 · UX-DR1 · AD-8)

## Tasks / Subtasks

- [x] Task 1: Build `ChampionshipSidebar.tsx` (AC: 1, 2, 5)
  - [x] Reuses `useDriverStandings()` / `useConstructorStandings()` — same query keys, no new backend call
  - [x] Renders top 3 rows per group
  - [x] `<aside aria-label="Championship standings">` wrapping the component
  - [x] `bg-bg-inset` well, no border, no card fill
- [x] Task 2: Desktop sticky layout (AC: 1, 3)
  - [x] `CalendarPage.tsx` wraps the sidebar and existing content block in a `md:grid-cols-[250px_1fr]` grid
  - [x] Sidebar uses `md:sticky md:top-8 md:self-start`; `filterSchedule`/`splitSchedule`/`RaceFilterTabs.tsx` untouched
- [x] Task 3: Mobile collapsed-drawer treatment (AC: 4)
  - [x] Hand-built toggle button (`aria-expanded`/`aria-controls`) shown only `< md`; content always visible `≥ md`
- [x] Task 4: Tests (AC: 1, 2, 3, 4, 5)
  - [x] New `ChampionshipSidebar.test.tsx` — 3 tests (landmark, top-3 rendering, mobile toggle)
  - [x] `CalendarPage.test.tsx` — 1 new test verifying the sidebar persists (scoped via `within()`, since `RaceWeekendCard`'s own standings preview creates duplicate driver-name text until Story 7.4 removes it — see Debug Log)

## Dev Notes

- **Previous story (7.1) intelligence:** `CalendarPage.tsx` now has a `useState<RaceFilter>('future')` filter, a `filterSchedule` helper, and renders `<RaceFilterTabs value={filter} onChange={setFilter} />` above the loading/error/empty/`Schedule` block. This story wraps that existing block (unchanged internally) plus the new sidebar in a two-column grid — **do not** touch `filterSchedule`, `splitSchedule`, or `RaceFilterTabs.tsx`. The filter tabs stay outside the grid (full width, above it), matching `EXPERIENCE.md`'s Flow 1 ("she flips the filter to Past... the sidebar doesn't move or reset").
- **`[ASSUMPTION]` — row count per group.** No source document pins an exact count for the sidebar's standings list (PRD FR-3 says "current Drivers' Championship standings" with no number; `DESIGN.md`'s `sidebar-championship` token spec gives layout/color only, not row count). Used **top 3 per group** — the same slice depth `RaceWeekendCard.tsx` currently uses for its own per-card standings preview, which this sidebar is explicitly replacing (FR-3: "replacing the need to show this data on every race card"). This is a judgment call, not a hard requirement — flag if it needs revisiting.
- **Reality check — no existing mobile drawer/hamburger pattern to reuse.** `phase-1/planning-artifacts/ux-designs/.../EXPERIENCE.md` describes the top nav as "collapsing to a hamburger menu on mobile," but `frontend/src/App.tsx`'s `<nav>` is a plain flex row with zero responsive collapse logic — no hamburger, no drawer component exists anywhere in the codebase (confirmed via repo-wide search). Build the sidebar's mobile toggle as a self-contained disclosure (a button + conditionally-rendered content), not by extending or copying a nonexistent nav pattern.
- **Reuse, don't refetch.** `useDriverStandings()`/`useConstructorStandings()` are the exact hooks `StandingsPage.tsx` and (today) `RaceWeekendCard.tsx` already call — TanStack Query dedupes by query key automatically, so calling the same hooks here does not create a second network request when Standings-page data is already cached; this satisfies AC 2 (AD-8) without any special wiring.
- **Loading state:** mirror `RaceWeekendCard.tsx`'s existing pattern for this exact same data (`driverStandings && constructorStandings && (...)`) — render nothing until both resolve. No AC requires a loading skeleton for the sidebar; don't add one (scope discipline).
- **`bg-bg-inset` is a real, already-used Tailwind class** in this codebase (`--color-bg-inset: #11141a` in `index.css`; used identically in `StreakCounter.tsx`, `FanCardWizard.tsx`, `TrajectoryChart.tsx`, `CircuitTrackLayout.tsx`). Use it directly — it is not a new token.
- **Do not touch `RaceWeekendCard.tsx` in this story.** Its own standings block is removed in Story 7.4, not here. Story 7.2 only adds the sidebar; the cards still show their (soon-to-be-redundant) standings preview until 7.4 lands — this is expected, not a bug, per the epic's own story ordering.

### Project Structure Notes

- New: `frontend/src/features/calendar/ChampionshipSidebar.tsx`, `frontend/src/features/calendar/ChampionshipSidebar.test.tsx`.
- Modified: `frontend/src/features/calendar/CalendarPage.tsx`, `frontend/src/features/calendar/CalendarPage.test.tsx`.
- No backend changes, no new dependencies, no new query keys.

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 7.2]
- [Source: _bmad-output/phase-2/planning-artifacts/prds/prd-F1_poc-2026-07-11/prd.md#FR-3]
- [Source: _bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/DESIGN.md#sidebar-championship] — width 250px, `bg-inset` well, no border/card fill, mobile drawer
- [Source: _bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/EXPERIENCE.md#Component Patterns, Responsive & Platform, Flow 1]
- [Source: _bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md#AD-8]
- [Source: frontend/src/shared/api/ergast.ts#useDriverStandings, #useConstructorStandings, #queryKeys.standings]
- [Source: frontend/src/features/calendar/RaceWeekendCard.tsx#StandingsColumn] — pattern precedent for row rendering (top-3 slice), not reused directly (private, scheduled for removal in 7.4)
- [Source: frontend/src/App.tsx] — confirms no existing hamburger/drawer pattern
- [Source: frontend/src/shared/mocks/handlers/ergastHandlers.ts#sampleDriverStandings, #sampleConstructorStandings] — test fixtures

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- First `CalendarPage.test.tsx` sidebar-persistence test used an unscoped `screen.getByText(driverName)` and failed with "multiple elements found" — `RaceWeekendCard.tsx` still renders its own top-3 standings preview (removed only in Story 7.4), so driver names legitimately appear twice on the page today. Fixed by scoping the query with `within(sidebar())`. This is expected transitional duplication, not a bug — documented in Dev Notes and left as-is since fixing `RaceWeekendCard.tsx` is explicitly out of this story's scope.
- Full suite: same 11 pre-existing unrelated failures as Story 7.1 (localStorage-unavailable environment issue, 1 locale-dependent time assertion), reconfirmed present on this story's own changes too — no new failures introduced (111 baseline passing → 115 passing, +4 new tests, 0 new failures).
- Typecheck and lint: clean on all touched/new files.

### Completion Notes List

- Implemented `ChampionshipSidebar.tsx`: reuses existing `useDriverStandings`/`useConstructorStandings` hooks (no new query key, satisfies AD-8 by construction), renders top-3 rows per group in a `bg-bg-inset` well (no border/card fill, per DESIGN.md), wrapped in a labelled `<aside>` landmark.
- `CalendarPage.tsx` now lays out the filter-controlled content and the sidebar in a responsive grid (`md:grid-cols-[250px_1fr]`); sidebar is `md:sticky` so it stays in view as the race list scrolls. Story 7.1's filter/list logic was not touched.
- Mobile treatment is a hand-built disclosure toggle — verified there's no existing hamburger/drawer component anywhere in the codebase to reuse (checked `App.tsx`'s nav, which the phase-1 UX doc claims collapses on mobile but doesn't).
- Row count (top 3 per group) is a documented `[ASSUMPTION]` — no source document pins an exact number; matched the depth `RaceWeekendCard.tsx` already used for the same data before this story replaces that usage in 7.4.
- All 5 ACs covered by tests (4 new: 3 in `ChampionshipSidebar.test.tsx`, 1 in `CalendarPage.test.tsx`).

### File List

- `frontend/src/features/calendar/ChampionshipSidebar.tsx` (new)
- `frontend/src/features/calendar/ChampionshipSidebar.test.tsx` (new)
- `frontend/src/features/calendar/CalendarPage.tsx` (modified)
- `frontend/src/features/calendar/CalendarPage.test.tsx` (modified)
