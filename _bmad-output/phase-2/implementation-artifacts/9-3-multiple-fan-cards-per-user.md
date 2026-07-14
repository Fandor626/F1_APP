---
baseline_commit: 860707b
---

# Story 9.3: Multiple Fan Cards per user

Status: review

## Story

As a fan who follows more than one driver,
I want to create and keep several Fan Cards in the same browser,
so that I don't have to pick just one favorite.

## Acceptance Criteria

1. **Given** I already have one or more Fan Cards, **when** I create a new one, **then** it is added alongside my existing cards — none are overwritten or deleted.
2. **Given** I have N Fan Cards, **when** I view the Fan Card page, **then** all N are visible in a grid (auto-fill, not a carousel/pagination), plus an "Add new card" tile.
3. **Given** the existing MVP single-card storage (`f1app__fanCard__v1`, Zustand `persist`), **when** this story migrates it, **then** it uses `persist`'s own `version`/`migrate` mechanism to wrap the old single-card state into a one-item collection — no key rename, and an existing user's pre-upgrade card is preserved as their first card (AD-9).
4. **Given** zero Fan Cards, **when** I view the Fan Card page, **then** the "Add new card" tile is the primary/only tile shown, framed as an empty state rather than a locked feature.

## Tasks / Subtasks

- [x] Task 1: Migrate store to a multi-card collection (AC 1, 3)
  - [x] `useFanCardStore.ts`: persisted shape changes from a flat `FanCardPicks` object to `{ cards: FanCardEntry[] }`, each entry a `CompleteFanCardPicks` plus a client-generated `id` (`crypto.randomUUID()`).
  - [x] `persist` config: `version: 1` (bumped from the implicit 0), `migrate(persistedState, version)` — when `version === 0`, wraps the old flat state into `{ cards: hasFanCardPicks(old) ? [{ ...old, id: crypto.randomUUID() }] : [] }`. Storage key unchanged (`f1app__fanCard__v1`).
  - [x] Store action is `addCard` (appends), never `setCard`/overwrite — satisfies AC 1 structurally.
  - [x] `hasFanCardPicks` kept as an exported type-guard over a single `FanCardPicks` object (used by `migrate` and available for future card-level checks), no longer used as a store-level "has any card" check.
- [x] Task 2: Update all store consumers for the new shape (AC 1, 2, 4)
  - [x] `FanCardWizard.tsx`: no longer prefills from the store (a wizard instance is always for a *new* card, never in-place editing of an existing one); calls `addCard(picks)` on save instead of the old per-field setters.
  - [x] `FanCardPromptModal.tsx`: `cards.length === 0` replaces the old `!hasFanCardPicks(picks)` gating check.
  - [x] `FanCard.tsx`: drops the `onEdit` prop / "Edit Picks" button — no AC in this story (or 9.1/9.2) specifies in-place editing of one card among several, and the old single-object "edit" semantics don't translate to a multi-card collection. Only "Download Image" remains.
  - [x] `FanCardPage.tsx`: rewritten as a `grid-template-columns: repeat(auto-fill, minmax(226px, 1fr))` grid of `FanCard`s (one per store entry, keyed by `id`) plus a dashed-border "Add new card" tile (`aspect-[5/7]`, matching the trading-card shape from Story 9.2) that switches the page into `FanCardWizard` mode. This is AC 2's grid and AC 4's empty state (zero cards → the grid renders with only the tile) in one component — no separate empty-state branch needed since the grid naturally degrades to "just the tile" when `cards` is empty.
- [x] Task 3: Tests
  - [x] New `useFanCardStore.test.ts`: migration test using `vi.resetModules()` + dynamic `import()` to exercise the real `persist` rehydration path — pre-seeds `localStorage` with a version-0 payload (including the explicit `version: 0` field zustand's `persist` always writes, even for the pre-migration store) and asserts the rehydrated store wraps it into a one-item `cards` array with a generated `id`; a second case asserts an all-null pre-9.3 state migrates to zero cards; a third asserts the storage key is unchanged.
  - [x] `FanCardPage.test.tsx`: rewritten — zero-cards empty state shows only the add tile; clicking it opens the wizard; saving shows the card with preserved MVP text content; saving a second card leaves the first one in place (`cards` grows to length 2, first entry's `driverId` unchanged); PNG export and localStorage persistence assertions carried over.
  - [x] `FanCardPromptModal.test.tsx`, `StandingsPage.test.tsx`: updated `useFanCardStore.setState(...)` fixtures to the new `{ cards: [...] }` shape.

## Dev Notes

- This story builds directly on Story 9.2's `FanCard.tsx` (trading-card visuals) — unchanged here except for the dropped `onEdit` prop; picks/props shape (`CompleteFanCardPicks`) is unaffected since a `FanCardEntry` (`CompleteFanCardPicks & { id: string }`) satisfies it structurally.
- `zustand`'s `persist` middleware writes an explicit `version` field on every serialization (defaulting to `0` if `options.version` is unset) — this bit real pre-migration data *and* my own migration test: a hand-written test fixture omitting `version` entirely gets skipped by `persist`'s migrate check (`typeof deserializedStorageValue.version === "number"` guards it), so the fixture had to include `version: 0` explicitly to match what the real old store actually wrote to `localStorage`.
- `useFanCardStore.persist.rehydrate()` returns a promise — rehydration from `localStorage` on module load is asynchronous, so any test asserting migrated state must `await` it (or `waitFor`) rather than reading `getState()` immediately after import/render in a synchronous assertion.
- No new dependencies. No backend changes.

### Architecture Compliance — AD-9 (verbatim)

> **AD-9 — Fan Card storage migrates via Zustand `persist`'s own versioning, not a key-name bump**
> - **Rule:** Persisted shape changes from a single `FanCardPicks` object to `{ cards: FanCardPicks[] }`, each entry tagged with a client-generated `id` (`crypto.randomUUID()`). Use `persist`'s own `version: 1` (bumped from the implicit `0`) and a `migrate(persistedState, version)` function that, when `version === 0`, wraps the old single-`FanCardPicks` state into `{ cards: [{ ...persistedState, id: crypto.randomUUID() }] }`. The storage key stays `f1app__fanCard__v1` — `persist` owns migration internally; no key rename. Store actions: `addCard` — never `setCard`/overwrite.

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 9.3]
- [Source: _bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md#AD-9]
- [Source: frontend/src/features/fan-engagement/useFanCardStore.ts], [Source: frontend/src/features/fan-engagement/FanCardPage.tsx], [Source: frontend/src/features/fan-engagement/FanCardWizard.tsx], [Source: frontend/src/features/fan-engagement/FanCardPromptModal.tsx]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Completion Notes List

- All 3 tasks complete. Full frontend suite: 181/185 passing — the 4 failures are the same pre-existing, unrelated `dateUtils.test.ts` locale-formatting issues noted in Story 9.1/9.2's Dev Agent Records.
- `npx tsc -b` clean. `eslint .` shows 3 pre-existing errors in `TrackMap.tsx`, `CircuitTrackLayout.tsx`, and `signalR.ts` — confirmed via `git stash` that all 3 exist unmodified on `main` before this story's changes; none of those files were touched.
- Dropped the "Edit Picks" affordance from `FanCard.tsx` — no story (9.1, 9.2, or 9.3) specifies in-place editing of an existing card, and the old single-object edit flow has no clean multi-card equivalent; users can still add more cards freely.

### File List

- New: `frontend/src/features/fan-engagement/useFanCardStore.test.ts`
- Modified: `frontend/src/features/fan-engagement/useFanCardStore.ts`
- Modified: `frontend/src/features/fan-engagement/FanCardWizard.tsx`
- Modified: `frontend/src/features/fan-engagement/FanCardPromptModal.tsx`
- Modified: `frontend/src/features/fan-engagement/FanCard.tsx`
- Modified: `frontend/src/features/fan-engagement/FanCard.test.tsx`
- Modified: `frontend/src/features/fan-engagement/FanCardPage.tsx`
- Modified: `frontend/src/features/fan-engagement/FanCardPage.test.tsx`
- Modified: `frontend/src/features/fan-engagement/FanCardPromptModal.test.tsx`
- Modified: `frontend/src/features/standings/StandingsPage.test.tsx`
