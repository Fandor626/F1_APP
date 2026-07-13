---
baseline_commit: 3d3362e817fefb2320981a201e455a6ee510289c
---

# Story 7.1: Calendar filter — upcoming by default

Status: review

## Change Log

- 2026-07-13: Implemented Future/Past/All filter (FR-1, FR-2). New `RaceFilterTabs.tsx`; `CalendarPage.tsx` filters the schedule client-side before it reaches the existing pin-next-race logic. All 4 ACs covered by tests; full suite + typecheck + lint run clean (3 pre-existing unrelated failures confirmed via baseline comparison).

## Story

As a returning fan,
I want the Calendar to show only upcoming races by default, with an easy way to see past or all races,
so that I can see what's next without wading through races that already happened.

## Acceptance Criteria

1. **Given** I open the Calendar page with no prior filter interaction, **when** the page loads, **then** only the next Race Weekend and all Race Weekends after it are rendered — zero past Race Weekends shown — **and** the next upcoming Race Weekend remains visually pinned (existing "Next race" section).
2. **Given** I am on the Calendar page, **when** I select "Past", **then** only completed Race Weekends are shown, **and** the selected option stays visually indicated at all times.
3. **Given** I am on the Calendar page, **when** I select "All", **then** the full season is shown, unfiltered (this is the current, pre-story behavior).
4. **Given** the filter control, **when** I navigate it via keyboard, **then** it behaves as a `role="tablist"`/`role="tab"` pattern — arrow keys move focus and selection between the three options, `Tab` moves in/out of the control. (FR-1, FR-2 · PRD §4.1 · UX-DR12)

## Tasks / Subtasks

- [x] Task 1: Add filter state and filtering logic to `CalendarPage.tsx` (AC: 1, 2, 3)
  - [x] Add `useState<'future' | 'past' | 'all'>('future')` for the filter
  - [x] Write a pure `filterSchedule(races, filter, now)` helper (co-located in `CalendarPage.tsx` next to `splitSchedule`)
  - [x] Apply `filterSchedule` to the fetched races before they reach `Schedule`/`splitSchedule` — `splitSchedule` itself is untouched
  - [x] Default filter is `'future'` on initial mount
- [x] Task 2: Build the filter control UI (AC: 2, 3, 4)
  - [x] New component `RaceFilterTabs.tsx` — three buttons (All / Future / Past), `role="tablist"`/`role="tab"`/`aria-selected`, roving `tabIndex`
  - [x] Arrow-key handling: `ArrowRight`/`ArrowLeft` move focus+selection between the three tabs, wrapping at the ends
  - [x] Visual treatment matches the Standings toggle's look; interaction pattern is the fuller accessible tablist per AC 4 (see Dev Notes reality check)
- [x] Task 3: Tests (AC: 1, 2, 3, 4)
  - [x] Updated `CalendarPage.test.tsx` default-render assertion to the new Future-default behavior
  - [x] Added test: selecting "Past" shows only past races, no pinned "Next race" section
  - [x] Added test: selecting "All" reproduces the original full-list behavior (reused original fixture assertions)
  - [x] Added test: keyboard arrow navigation moves selection between tabs, using `fireEvent.keyDown` — see Debug Log (deviation from the `@testing-library/user-event` reference in this task's original spec)

## Dev Notes

- **This is the first story in Epic 7 and in Phase 2.** No previous phase-2 story exists yet — no prior-story intelligence to inherit.
- **Reality check — read before implementing the "consistent with Standings toggle" requirement:** `EXPERIENCE.md` (UX-DR12) says this filter should be "consistent with the existing Standings Drivers/Constructors toggle behavior." Having read `frontend/src/features/standings/StandingsPage.tsx` (lines 17–38), the actual existing toggle is **two plain `<button>` elements with `aria-pressed`, no `role="tablist"`, no arrow-key navigation** — native browser Tab/Enter/Space only. This does **not** satisfy AC 4's explicit tablist/arrow-key requirement. Resolution used in Tasks above: match the Standings toggle **visually** (same classes/look), but implement the **fuller accessible pattern** (`tablist`/`tab`/roving tabindex/arrow keys) that AC 4 explicitly demands — do not silently downgrade AC 4 to match the older, simpler pattern. This is a deliberate, documented divergence, not an oversight.
- **Current shipped behavior (read `frontend/src/features/calendar/CalendarPage.tsx` in full before touching it):** The page has no filter today. `splitSchedule(races, now)` finds the soonest race with `raceStart >= now` and pins it in a "Next race" section; every other race (past AND future) renders in the "Season schedule" list below, chronologically, via `RaceWeekendCard`. This means **today's default view already includes past races in "Season schedule"** — exactly the behavior FR-1 says must change. The "All" filter (AC 3) must reproduce this exact current behavior unchanged; "Future" (the new default) must strip past races from that same list; "Past" must show only past races (with no "pinned next race" concept, since none of them is upcoming).
- **`RaceWeekend.raceStart`** (from `frontend/src/shared/api/ergast.ts`) is the ISO datetime field to filter on — same field `splitSchedule` already uses for its own past/future comparison (`new Date(race.raceStart) >= now`). Reuse that exact comparison for filter consistency (don't introduce a second date-comparison convention using `weekendStart` or a different operator).
- **Do not touch `RaceWeekendCard.tsx` in this story.** Story 7.4 (later in this epic) redesigns the card itself (track outline, fastest-lap block, removing standings). This story only changes which races reach the list and adds the filter control — the card component and its `data-testid="race-weekend-card"` are unchanged.
- **`StreakCounter`** in the page header is unrelated to this story — do not modify.
- Architecture spine (`ARCHITECTURE-SPINE.md`) has no AD specific to this story; it's pure frontend presentation/filter logic with no new data fetching, no new backend endpoint, no Zustand/TanStack Query boundary concerns (the existing `useRaceSchedule()` query is unchanged — filtering happens client-side on already-fetched data, not via a new query parameter).

### Project Structure Notes

- New file: `frontend/src/features/calendar/RaceFilterTabs.tsx` (+ co-located `RaceFilterTabs.test.tsx` if you want unit-level coverage separate from the `CalendarPage.test.tsx` integration tests — optional, `CalendarPage.test.tsx` coverage is the AC-mandated minimum).
- Modified: `frontend/src/features/calendar/CalendarPage.tsx`, `frontend/src/features/calendar/CalendarPage.test.tsx`.
- No backend changes. No new dependencies — build the tablist by hand (matches the project's established "fully hand-built components, no headless library" convention from `DESIGN.md` Foundation).
- Follows existing conventions: component file `PascalCase.tsx`, test co-located and same-named `.test.tsx` (per phase-1 architecture.md "Frontend test co-location").

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 7.1] — story origin, acceptance criteria
- [Source: _bmad-output/phase-2/planning-artifacts/prds/prd-F1_poc-2026-07-11/prd.md#FR-1, #FR-2] — requirement text
- [Source: _bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/EXPERIENCE.md#Component Patterns, UX-DR12] — "Calendar filter (tab-toggle)... mirrors the existing Standings Drivers/Constructors toggle behavior"
- [Source: frontend/src/features/calendar/CalendarPage.tsx] — current implementation being modified
- [Source: frontend/src/features/standings/StandingsPage.tsx#L17-L38] — visual pattern to match (not interaction pattern — see Dev Notes reality check)
- [Source: frontend/src/shared/api/ergast.ts#RaceWeekendSchema] — `RaceWeekend` type, `raceStart` field

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- `@testing-library/user-event` (referenced in this story's original Task 3 spec) is **not installed** in `frontend/package.json` devDependencies. Rather than add a new dependency for a single keydown simulation, used `fireEvent.keyDown` from the already-present `@testing-library/react` — equivalent coverage for this test's purpose (verifying the component's own `onKeyDown` handler), no new dependency introduced.
- Ran full frontend suite (`npx vitest run`) after implementation: 11 pre-existing failures in `dateUtils.test.ts`, `FanCardPage.test.tsx`, `StreakCounter.test.tsx` — verified via `git stash` that these fail identically on the unmodified baseline commit (`window.localStorage` unavailable in this test runner environment; one locale-dependent time-format assertion). Not caused by this story; not touched by this story's files.
- `npx tsc --noEmit -p tsconfig.app.json`: pre-existing errors in `TrackMap.test.tsx` (unrelated `global` reference), zero errors in any file this story touched.
- `npx eslint` on all 3 touched/new files: clean.

### Completion Notes List

- Implemented the Future/Past/All filter as a new `RaceFilterTabs.tsx` component (accessible tablist: `role="tablist"`/`role="tab"`, `aria-selected`, roving tabindex, `ArrowLeft`/`ArrowRight` navigation with wraparound) and wired it into `CalendarPage.tsx` via a new pure `filterSchedule` helper, defaulting to `'future'`.
- Confirmed via code reading (not assumption) that the existing Standings Drivers/Constructors toggle does **not** have tablist/arrow-key semantics — matched its visual styling only, built the fuller accessible interaction pattern this story's AC 4 requires. Documented as a deliberate divergence from a UX-doc claim that didn't match shipped reality.
- `splitSchedule` (existing pinned-next-race logic) required zero changes — filtering the array before it reaches `splitSchedule` was sufficient; the existing `{next && (...)}` guard already handles the "no upcoming race in this filtered set" case (e.g., the Past filter) correctly.
- `RaceWeekendCard.tsx` was not touched, per story scope (reserved for Story 7.4).
- All 4 ACs covered by tests in `CalendarPage.test.tsx`; full suite run to confirm no regressions (see Debug Log for the 3 pre-existing unrelated failures).

### File List

- `frontend/src/features/calendar/RaceFilterTabs.tsx` (new)
- `frontend/src/features/calendar/CalendarPage.tsx` (modified)
- `frontend/src/features/calendar/CalendarPage.test.tsx` (modified)
