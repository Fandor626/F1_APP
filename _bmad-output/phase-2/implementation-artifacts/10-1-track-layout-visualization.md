---
baseline_commit: e86e0ca
---

# Story 10.1: Track layout visualization

Status: review

## Story

As a fan checking a Race Weekend before it starts,
I want to see the circuit's real track shape in detail,
so that I understand the track's character, not just a name.

## Acceptance Criteria

1. **Given** a circuit with available outline data (from Story 7.3's circuit-configs coverage), **when** I open its Race Weekend Detail page, **then** it renders a recognizable track outline, at a larger/more detailed treatment than the FR-4 card-level version — same underlying `circuit-configs/{circuitId}.json` asset, no separate file (AD-5).
2. **Given** a circuit without a sourced outline asset, **when** I open its Race Weekend Detail page, **then** the outline is omitted gracefully (or a generic placeholder), not a broken-image state.
3. **Given** the track outline, **when** rendered, **then** it fetches via the same relative same-origin path fixed in Story 7.3 (AD-6).
4. **Given** the track outline visual, **when** a screen reader encounters it, **then** it has an accessible name identifying the circuit (UX-DR13).

## Tasks / Subtasks

- [x] Task 1: Reuse `TrackOutline` on the Race Weekend Detail page (AC 1, 2, 3, 4)
  - [x] `frontend/src/features/calendar/RaceWeekendDetailView.tsx`: import the existing `TrackOutline` component (already built for FR-4's Calendar card, `frontend/src/features/calendar/TrackOutline.tsx`) — no new component, no new asset, per AD-5's "same file, different CSS sizes" rule.
  - [x] Render it inside a new bordered/inset panel (`data-testid="race-weekend-track-layout"`), sized larger (`h-[320px]`) than the Calendar card's `168px`-wide crop, placed above the Sessions list.
  - [x] `TrackOutline` already fetches via the correct relative same-origin path (`/circuit-configs/{id}.json`, AD-6, fixed in Story 7.3) and already returns `null` on fetch failure/loading — no broken-image state (AC 2) with zero new code.
  - [x] `TrackOutline` already carries `role="img"` `aria-label={`Track layout: ${circuitName}`}` — AC 4 satisfied unchanged, reusing Story 7.3/7.4's UX-DR13 work verbatim.
- [x] Task 2: Tests
  - [x] `RaceWeekendDetailView.test.tsx`: new test asserting the accessible-named track outline renders once its `circuit-configs` fetch (mocked via an MSW `server.use` handler, matching this file's existing MSW-based mocking pattern rather than overriding `globalThis.fetch` — the latter would also intercept the page's own `/api/races/:round` MSW-backed request) resolves; a second test asserting a 404 response leaves the wrapping panel in place but renders no `img`-role element (graceful omission, not a broken-image state).

## Dev Notes

- **Key finding: no new component was needed.** `TrackOutline.tsx` was already built (evidently as part of Story 7.4/FR-4 landing) exactly per the architecture spine's plan — its own doc comment already says "shared by the Calendar card and Race Weekend Detail page" — and already satisfies every AC in this story (correct fetch path, graceful null-on-failure, accessible name). Story 10.1's entire scope reduced to importing and rendering it larger on `RaceWeekendDetailView.tsx`.
- `frontend/src/features/profiles/CircuitTrackLayout.tsx` is a **separate, stale** component (used only by `CircuitProfilePage.tsx`) that still has the pre-Story-7.3 `${VITE_API_BASE_URL}`-prefixed fetch bug and no accessible name. It was **not** touched by this story — out of scope (Circuit Profile page is a different story/epic) — but is a known latent inconsistency worth flagging for a future cleanup.
- `RaceWeekendDetailView.tsx` already had `data.circuitId`/`data.circuitName` available from `useRaceDetail` — no new hook, no backend change.
- Test gotcha: this test file already relies on the global MSW server (`../../shared/test/server`) for `/api/races/:round`; overriding `globalThis.fetch` directly (as `TrackOutline.test.tsx` does in isolation) would have also hijacked that MSW-intercepted request. Used `server.use(http.get('/circuit-configs/bahrain.json', ...))` instead, consistent with the rest of this file's mocking approach.

### Architecture Compliance — AD-5, AD-6 (verbatim)

> **AD-5 — Circuit outline extends the existing `circuit-configs` asset, not a second asset class**
> - **Rule:** `circuit-configs/{circuitId}.json`'s existing `trackPath` field... is the single source for every rendering of a circuit's shape: the calendar card (FR-4, small crop), Race Weekend Detail (FR-13, large crop), and the existing live-map overlay all consume the same file at different CSS sizes — no new fields, no second file per circuit.

> **AD-6 — Circuit-config assets are fetched same-origin, never through the backend API base URL**
> - **Rule:** All `circuit-configs` fetches use a relative same-origin path — `fetch('/circuit-configs/{id}.json')` — never `${VITE_API_BASE_URL}`-prefixed.

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 10.1]
- [Source: _bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md#AD-5, AD-6]
- [Source: _bmad-output/phase-2/implementation-artifacts/7-3-circuit-outline-asset-coverage-fetch-fix.md]
- [Source: frontend/src/features/calendar/TrackOutline.tsx], [Source: frontend/src/features/calendar/RaceWeekendCard.tsx] — existing FR-4 consumer, same treatment pattern reused here

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Completion Notes List

- Task complete. Full frontend suite: 183/187 passing — the 4 failures are the same pre-existing, unrelated `dateUtils.test.ts` locale-formatting issues noted in prior stories' Dev Agent Records.
- `npx tsc -b` clean. `eslint src/features/calendar` clean.

### File List

- Modified: `frontend/src/features/calendar/RaceWeekendDetailView.tsx`
- Modified: `frontend/src/features/calendar/RaceWeekendDetailView.test.tsx`
