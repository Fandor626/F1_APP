---
baseline_commit: "cc6f2c9"
---

# Story 6.3: My F1 Fan Card

Status: done

## Story

As a fan expressing my F1 identity,
I want to set up a personal fan card with my favorite driver, constructor, and circuit,
So that I can showcase my fandom and share it.

## Acceptance Criteria

1. **Given** the user has not set up a Fan Card **When** they open the setup wizard **Then** they can pick a favourite Driver, Constructor, and Circuit.
2. **Given** picks are made **Then** the card shows them alongside current season stats (driver/constructor points and position, from Epic 4's standings data).
3. **Given** an existing Fan Card **Then** the wizard is re-accessible to change picks at any time.
4. **Given** the user exports the card **Then** it generates a client-side image (`html-to-image`) with only the chosen picks and current stats — no personal data.
5. **And** the card is stored in localStorage with a versioned key.

## Design decisions

- **Pure frontend, no backend changes** — same as Story 6.2; this story reuses Epic 4's existing `/api/standings/drivers`/`/constructors` endpoints as-is.
- **`useFanCardStore.ts` uses Zustand's `persist` middleware**, not the plain `useLocalStorage` hook from Story 6.2 — this is what architecture.md's file tree literally specifies ("Zustand + localStorage persistence"), and it's the right tool here because the Fan Card has multiple related fields (three picks) that benefit from a single store's action-based API (`setDriverPick`, `setConstructorPick`, `setCircuitPick`, `resetFanCard`) rather than one raw JSON blob threaded through `useState`. Storage key: `f1app__fanCard__v1`, following the same versioned-key convention Story 6.2 established.
- **Driver/constructor pick lists come from the current standings** (`useDriverStandings`/`useConstructorStandings`, ~20 drivers / ~4-10 constructors), not the all-time 881-driver list from Story 5.3's head-to-head page — a "current season stats" card only makes sense for drivers/constructors actually racing this season. A simple `<select>` is sufficient at this list size (no need for Story 5.3's search-and-filter combobox).
- **Circuit pick list comes from the current season's schedule** (`useRaceSchedule()`, ~24 circuits) rather than a new "list all circuits" backend endpoint — no such endpoint exists yet (Story 5.1's circuit profile is reached by click-through only), and building one is out of scope for this story. This mirrors Story 5.3's own precedent of deriving pick lists from already-loaded data rather than adding new backend surface for a picker.
- **Circuit pick shows identity only, no stats** — AC 2 only requires driver/constructor points+position; the circuit pick is a fandom expression ("my favourite track"), not a stats card, so it displays name/country and nothing else.
- **Export reuses Story 4.3's `SeasonWrappedCard.tsx` pattern exactly** (a `ref`'d container passed to `html-to-image`'s `toPng`, triggering a browser download) — this is the second consumer of that pattern, exactly as epics.md predicted ("introduces the html-to-image export pattern reused by Epic 6's Fan Card").

## Tasks / Subtasks

### Task 1: Frontend — `useFanCardStore` (AC: 1, 3, 5)

- [ ] Create `frontend/src/features/fan-engagement/useFanCardStore.ts`:
  ```ts
  import { create } from 'zustand'
  import { persist } from 'zustand/middleware'

  export interface FanCardPicks {
    driverId: string | null
    driverName: string | null
    constructorName: string | null
    circuitId: string | null
    circuitName: string | null
  }

  interface FanCardState extends FanCardPicks {
    setDriverPick: (driverId: string, driverName: string) => void
    setConstructorPick: (constructorName: string) => void
    setCircuitPick: (circuitId: string, circuitName: string) => void
    resetFanCard: () => void
  }

  const EMPTY_PICKS: FanCardPicks = {
    driverId: null,
    driverName: null,
    constructorName: null,
    circuitId: null,
    circuitName: null,
  }

  export const useFanCardStore = create<FanCardState>()(
    persist(
      (set) => ({
        ...EMPTY_PICKS,
        setDriverPick: (driverId, driverName) => set({ driverId, driverName }),
        setConstructorPick: (constructorName) => set({ constructorName }),
        setCircuitPick: (circuitId, circuitName) => set({ circuitId, circuitName }),
        resetFanCard: () => set(EMPTY_PICKS),
      }),
      { name: 'f1app__fanCard__v1' },
    ),
  )

  export function hasFanCardPicks(picks: FanCardPicks): boolean {
    return picks.driverId !== null && picks.constructorName !== null && picks.circuitId !== null
  }
  ```

### Task 2: Frontend — `FanCardWizard` (AC: 1, 3)

- [ ] Create `frontend/src/features/fan-engagement/FanCardWizard.tsx` — three `<select>` inputs (Driver, Constructor, Circuit) sourced from `useDriverStandings()`, `useConstructorStandings()`, `useRaceSchedule()` respectively; a "Save" button calls the store's three setters; pre-fills current picks when re-opened to change them (AC 3).

### Task 3: Frontend — `FanCard` display + export (AC: 2, 4)

- [ ] Create `frontend/src/features/fan-engagement/FanCard.tsx` — looks up the picked driver's/constructor's current points+position from `useDriverStandings()`/`useConstructorStandings()` (matched by `driverId`/`constructorName`), renders them alongside the circuit pick; a "Download Image" button using the exact `toPng`-to-download pattern from `standings/SeasonWrapped/SeasonWrappedCard.tsx`; an "Edit Picks" button that re-opens `FanCardWizard`.

### Task 4: Frontend — `FanCardPage` (AC: 1, 3)

- [ ] Create `frontend/src/features/fan-engagement/FanCardPage.tsx` — one `<h1>`; renders `FanCardWizard` when `!hasFanCardPicks(picks)` or when the user has clicked "Edit Picks" (local `isEditing` state), otherwise renders `FanCard`.
- [ ] `frontend/src/features/fan-engagement/index.ts` — export `FanCardPage`.

### Task 5: Frontend — Routing, nav (AC: 1)

- [ ] `frontend/src/router.tsx` — add `{ path: 'fan-card', element: <FanCardPage /> }`.
- [ ] `frontend/src/App.tsx` — add a "Fan Card" nav link (mirrors the mockup's `nav-right` Fan Card link).

### Task 6: Frontend — Tests (AC: 1, 2, 3, 4, 5)

- [ ] Create `frontend/src/features/fan-engagement/FanCardPage.test.tsx` (mirrors `StandingsPage.test.tsx`'s MSW/QueryClientProvider harness):
  - Shows the wizard when no picks exist yet.
  - Picking a driver/constructor/circuit and saving shows the `FanCard` with the correct current points/position for each.
  - "Edit Picks" re-opens the wizard with the existing picks retained (not reset).
  - The picks persist under `f1app__fanCard__v1` in `localStorage` after saving.
  - Clicking "Download Image" calls `html-to-image`'s `toPng` (mock the module, same as `SeasonWrapped.test.tsx`).
- [ ] Clear `localStorage` in `beforeEach` (same convention as Story 6.2's `StreakCounter.test.tsx`).
- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass.
- [ ] Run `npx tsc --noEmit -p tsconfig.app.json` from `frontend/` — must be clean.

## Dev Notes

### Architecture Alignment

- Matches `architecture.md`'s `fan-engagement/FanCard/FanCard.tsx`, `FanCardWizard.tsx`, `useFanCardStore.ts` file tree entries and the `use{Feature}Store` / verb-prefixed-action Zustand naming conventions already established by `liveRaceStore.ts`.
- Second (and final, for this POC) consumer of the `html-to-image` export pattern Story 4.3 introduced — no new export logic invented here.

### Regressions to Guard

- `useFanCardStore`'s `persist` middleware key (`f1app__fanCard__v1`) must not collide with Story 6.2's `f1app__streak__v1` — they are independent features with independent versioned keys, per the architecture's per-feature key format.
- Constructor matching is by `constructorName` string (no `constructorId` exists anywhere in this codebase's frontend types) — this is a known, accepted simplification consistent with how `constructorColors.ts` (Story 4.1) already keys off constructor name, not id.

### Files to Create / Modify

**Frontend CREATE:**
- `frontend/src/features/fan-engagement/useFanCardStore.ts`
- `frontend/src/features/fan-engagement/FanCardWizard.tsx`
- `frontend/src/features/fan-engagement/FanCard.tsx`
- `frontend/src/features/fan-engagement/FanCardPage.tsx`
- `frontend/src/features/fan-engagement/FanCardPage.test.tsx`

**Frontend MODIFY:**
- `frontend/src/features/fan-engagement/index.ts`
- `frontend/src/router.tsx`
- `frontend/src/App.tsx`

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 6.3: My F1 Fan Card]
- [Source: _bmad-output/planning-artifacts/architecture.md — FanCard.tsx, FanCardWizard.tsx, useFanCardStore.ts, localStorage key format]
- [Source: frontend/src/features/standings/SeasonWrapped/SeasonWrappedCard.tsx — html-to-image export pattern reused as-is]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

None.

### Completion Notes List

- Implemented per plan, with one type-correctness fix over the original task snippet: `hasFanCardPicks` is a proper TypeScript type predicate (`picks is CompleteFanCardPicks`) rather than a plain `boolean`-returning function, and a new `CompleteFanCardPicks` (all-`string`, no `null`) type was introduced — the original snippet's `FanCard` prop type (`NonNullable<FanCardPicks>`) was a no-op (`FanCardPicks` is already a non-null object type; `NonNullable` only strips `null`/`undefined` from the type itself, not its nullable fields) and would not have actually narrowed anything.
- Test writing surfaced a real async-timing bug in the test *helper*, not the component: the wizard's three `<select>`s each depend on an independently-resolving `useQuery` (drivers/constructors/schedule), so waiting only for the driver label before firing all three `fireEvent.change` calls could set a `<select>`'s value before its data-driven `<option>`s existed yet (jsdom silently no-ops a `value` assignment that doesn't match any current `<option>`). Fixed by waiting for each specific option to appear before changing that select.
- Fan Card is the second (and, per epics.md's own framing, final for this POC) consumer of the `html-to-image` export pattern Story 4.3 introduced — implemented identically, no new export logic.
- **Environment note**: 100% frontend, same as Story 6.2 — no `.NET` SDK constraint applies here.
- All frontend tests pass except the 4 pre-existing, unrelated `dateUtils.test.ts` locale failures; `tsc -p tsconfig.app.json` is clean except the one pre-existing `TrackMap.test.tsx` issue noted in Story 5.1; `eslint` is fully clean.

### File List

See "Files to Create / Modify" above — unchanged from plan except the `CompleteFanCardPicks` type-predicate fix noted above.

### Change Log

| Date | Change |
|------|--------|
| 2026-07-10 | Story created and implemented directly (bmad create-story + dev-story cycle) |
