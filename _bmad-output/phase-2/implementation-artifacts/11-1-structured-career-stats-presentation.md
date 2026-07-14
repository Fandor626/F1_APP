---
baseline_commit: f698304
---

# Story 11.1: Structured career stats presentation

Status: review

## Story

As a fan viewing a Driver, Constructor, or Circuit profile,
I want career stats grouped into clear sections instead of one long list,
so that I can scan for what I care about quickly.

## Acceptance Criteria

1. **Given** a Driver or Constructor profile page, **when** it renders, **then** career statistics and history render in at least two visually distinct stacked sections (e.g. season-by-season results, career totals, head-to-head where applicable) rather than one flat list.
2. **Given** these sections, **when** viewed at a 360px-wide mobile viewport, **then** no section requires horizontal scrolling.
3. **Given** the underlying data, **when** this story is implemented, **then** it is unchanged from MVP FR-20–FR-22 — this story governs presentation/structure only, not new data.
4. **Given** the sections, **when** rendered, **then** they appear as stacked sections on one scrollable page, not tabs.

## Tasks / Subtasks

- [x] Task 1: Scope confirmation — no Constructor profile page exists (AC 1)
  - [x] Confirmed via full-codebase search: no `ConstructorProfilePage` component, no `/constructors/:id` route, and no Constructor-profile FR anywhere in the phase-1 MVP epics (FR-20 is Circuit profile, FR-21 is Driver profile, FR-22 is Driver head-to-head — no constructor page was ever built). AC 1's "Driver or Constructor profile page" condition has nothing to fix for Constructor since no such page renders anything; no new page was built (would be new scope beyond "presentation-only, no new data").
- [x] Task 2: Label the Driver profile's stat-tile section (AC 1, 4)
  - [x] `frontend/src/features/profiles/DriverProfilePage.tsx`: the career-totals `StatTile` grid was already visually distinct (bordered tiles) and already stacked above Constructor History and the career chart (3 blocks, not tabs) — but it had no heading, unlike the other two sections. Wrapped it in a `<section>` with an `<h2>Career Totals</h2>` heading, matching the established `CircuitProfilePage.tsx` section convention, so all three blocks read unambiguously as distinct labeled sections.
  - [x] No data changes — still reads `data.careerTotals`, `data.constructorHistory`, `data.careerPoints` exactly as before (AC 3).
- [x] Task 3: 360px horizontal-scroll safety net (AC 2)
  - [x] Wrapped `DriverProfilePage.tsx`'s Constructor History table and `CircuitProfilePage.tsx`'s Past Winners table in `<div className="overflow-x-auto">` — neither table currently has enough columns/content to force overflow at 360px (verified: 2 and 3 plain-text columns respectively, no `whitespace-nowrap`, no fixed widths), but neither had this protection either; added as defense-in-depth per AC 2's explicit requirement, consistent with `DriverCareerChart`'s existing `ResponsiveContainer width="100%"` (already scales safely, untouched).
  - [x] `StatTile` grids (`grid-cols-3 sm:grid-cols-6` on Driver, `grid-cols-2` on Circuit) already collapse to their base column count at mobile width — no scroll risk, no change needed.

## Dev Notes

- `CircuitProfilePage.tsx` is **not required** by this story's AC (the Given clause names only "Driver or Constructor," not Circuit, despite the epic-level summary prose mentioning all three) — it was already structurally compliant before this story (3-4 labeled stacked sections, no tabs). The `overflow-x-auto` table fix was applied there too as low-risk, uniform hygiene, not because the AC required it.
- No backend changes, no new API fields, no changes to `DriverProfileSchema`/`CircuitProfileSchema` — purely a markup/structure change (AC 3).

### UX-DR11 (verbatim from epics.md)

> UX-DR11: Profile grouped-stats presentation (Driver/Constructor/Circuit Profile, FR-17) — career stats render as ≥2 visually distinct stacked sections (e.g. season-by-season, career totals, head-to-head where applicable) on one scrollable page, not tabs; no group requires horizontal scroll at 360px width. Section header/divider/spacing treatment was not visually specified by UX (flagged gap) — implement against inherited card/section conventions.

Followed the flagged gap's own instruction: implemented against the inherited section convention already established by `CircuitProfilePage.tsx` (`<section>` + uppercase `<h2>` label), rather than inventing a new visual treatment.

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 11.1]
- [Source: frontend/src/features/profiles/DriverProfilePage.tsx], [Source: frontend/src/features/profiles/CircuitProfilePage.tsx]
- [Source: frontend/src/features/profiles/DriverCareerChart.tsx] — already-responsive chart section, unchanged, used as the third stacked-section precedent

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Completion Notes List

- Both tasks complete. Full frontend suite: 200/204 passing — the 4 failures are the same pre-existing, unrelated `dateUtils.test.ts` locale-formatting issues noted throughout Epic 9/10.
- `npx tsc -b` clean. `eslint` clean on both touched files (one pre-existing, unrelated error remains in `CircuitTrackLayout.tsx`, not touched by this story).
- No Constructor profile page was built — confirmed out of scope per Dev Notes above.

### File List

- Modified: `frontend/src/features/profiles/DriverProfilePage.tsx`
- Modified: `frontend/src/features/profiles/DriverProfilePage.test.tsx`
- Modified: `frontend/src/features/profiles/CircuitProfilePage.tsx`
