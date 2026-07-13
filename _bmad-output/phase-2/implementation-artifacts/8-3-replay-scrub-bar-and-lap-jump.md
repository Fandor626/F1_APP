---
baseline_commit: c337f6f
---

# Story 8.3: Replay scrub bar and lap jump

Status: review

## Change Log

- 2026-07-13: Added the scrub bar to `ReplayBar.tsx` (FR-7) using a native `<input type="range">` — implicit `role="slider"` and native keyboard stepping satisfy UX-DR4 with zero custom keydown code. Empirically confirmed jsdom doesn't simulate that native stepping (a real test-environment limitation, documented rather than glossed over) and fixed a real mock-isolation bug in the existing `ReplayBar.test.tsx` (`clearAllMocks()` doesn't reset `mockReturnValue`, letting one test's data leak into the next).

## Story

As a fan replaying a past race,
I want to drag a scrub bar or jump straight to a specific lap,
so that I can skip to the moment I care about instead of watching from the start.

## Acceptance Criteria

1. **Given** the Replay bar (Story 8.2), **when** I drag the scrub bar or select a specific lap, **then** all dependent views (positions, gaps, tyres) update to that lap's state immediately — not just a visual timeline marker.
2. **Given** the scrub bar, **when** rendered, **then** it uses discrete snap-to-lap ticks, never a continuous/analog seek control.
3. **Given** I scrub while playback is running, **when** I release the scrub, **then** playback state is unaffected — if it was playing, it keeps playing from the new lap; if paused, it stays paused at the new lap (AD-4).
4. **Given** the scrub bar, **when** I use the keyboard, **then** `Tab` focuses it (`role="slider"`), Left/Right arrow keys step one lap at a time, and `Home`/`End` jump to lap 1 and the final lap (UX-DR4).
5. **Given** the lap readout (`Lap {n} / {total}`), **when** I scrub or play, **then** it always reflects the current position live, during both playback and scrub-drag, never lagging behind.

## Tasks / Subtasks

- [x] Task 1: Add the scrub bar to `ReplayBar.tsx` (AC 1, 2, 3, 4, 5)
  - [x] Native `<input type="range">` (`min=0`, `max=totalLaps-1`, `step=1`) — implicit `role="slider"`/`aria-valuenow` etc., native Left/Right/Home/End keyboard behavior, no custom keydown code
  - [x] `<datalist>` with one `<option>` per lap, wired via `list` attribute — native tick marks
  - [x] `onChange` calls `setCurrentLapIndex` only — never touches `isPlaying` (unchanged Story 8.2 action)
  - [x] Not rendered until `frames` has loaded (`totalLaps > 0` guard)
- [x] Task 2: Tests (AC 1–5)
  - [x] Scrub → `liveRaceStore` and lap readout update immediately (via `fireEvent.change`)
  - [x] Scrubbing leaves `isPlaying` unchanged in both directions (parameterized test)
  - [x] **Empirically verified jsdom does not simulate native range-input keyboard stepping** (see Debug Log) — tests instead verify the element is a genuine native range input with correct min/max/step, which is what makes the real (unverifiable-in-jsdom) browser keyboard behavior apply
  - [x] Lap readout reflects `currentLapIndex` immediately after a scrub

## Dev Notes

- **This story adds ONE control to the existing `ReplayBar.tsx`** (Story 8.2) — it does not restructure anything. `replayStore`, `useRaceReplayQuery`, and the frame-application `useEffect` all already exist and already do exactly what AC 1/3/5 need; this story is purely "add an `<input type="range">` wired to `setCurrentLapIndex`."
- **Read `ReplayBar.tsx` in full before editing** (Story 8.2's file). The lap readout (`data-testid="replay-lap-readout"`) already renders `Lap {currentLapIndex + 1} / {totalLaps || '–'}` reactively off the store — AC 5 requires no new code, just don't break this by scoping the new scrub bar's state incorrectly.
- **Verify jsdom's native range-input keyboard behavior actually fires `onChange` before relying on it in tests.** Some jsdom versions don't fully simulate native browser stepping behavior for `<input type="range">` on arrow-key press. If `fireEvent.keyDown` doesn't trigger the expected value change in this project's jsdom/vitest setup, test the equivalent outcome via `fireEvent.change` with the expected stepped value instead, and note in the story's Debug Log which behavior the test environment actually exercises versus what real browsers do — don't silently claim keyboard coverage the test doesn't actually verify.
- **No backend changes.** This story is entirely frontend, consuming data Story 8.2 already fetches.

### Project Structure Notes

- Modified: `frontend/src/features/live-race/ReplayBar/ReplayBar.tsx`, `frontend/src/features/live-race/ReplayBar/ReplayBar.test.tsx`.
- No new files expected — this is small enough to stay within the existing `ReplayBar.tsx`, not a new sub-component, unless the scrub-bar-specific logic grows large enough to warrant extraction (unlikely for this scope).

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 8.3]
- [Source: _bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/EXPERIENCE.md#Interaction Primitives — UX-DR4] — "Tab to focus, role=slider... Left/Right arrow keys step one lap at a time... Home/End jump to lap 1 and the final lap"
- [Source: _bmad-output/phase-2/implementation-artifacts/8-2-race-replay-start-stop-restart-controls.md] — `replayStore`, `ReplayBar.tsx`, the frame-application effect this story builds on

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- **Verified jsdom's range-input keyboard behavior empirically before relying on it in tests** — wrote a standalone probe test asserting `<input type="range">`'s `.value` after `fireEvent.keyDown(el, { key: 'ArrowRight' })`. Result: value stayed unchanged (jsdom does not implement native browser range-stepping-on-keypress). This confirms the story's own pre-registered caution. Tests instead verify the DOM element's native attributes (`type`, `min`, `max`, `step`) are correct, which is what makes the (real, spec-defined, but jsdom-unsimulated) keyboard behavior work in an actual browser — not asserting a behavior this test suite can't actually exercise.
- **Found and fixed a real test-isolation bug in the existing `ReplayBar.test.tsx`** (pre-existing from Story 8.2, surfaced only now because a new test happened to assert *absence* of data after prior tests had configured presence): `vi.clearAllMocks()` clears call history but not a mock's `mockReturnValue` configuration, so a later test's `{ data: [...] }` silently persisted into the next test that expected `{ data: undefined }`. Fixed by re-establishing the default return value explicitly in `beforeEach`, protecting every test in the file, not just the new ones.
- Frontend: full suite same 11 pre-existing failures, 138 passing (+5 new). Typecheck clean. Lint clean (no new pre-existing-style findings this time — the scrub bar is a native element, no effect/state pattern to trip the earlier `react-hooks/set-state-in-effect` rule).

### Completion Notes List

- Scrub bar implemented as a native `<input type="range">` rather than a custom drag control — the deliberate choice documented in the story spec, confirmed correct: it gets `role="slider"`, ARIA value attributes, and Left/Right/Home/End keyboard stepping entirely for free from the HTML specification.
- `onChange` wiring reuses the exact `setCurrentLapIndex` action from Story 8.2 with no modification — AC 3 (scrubbing never touches `isPlaying`) was satisfied by that story's existing design, not new code here.
- All 5 ACs covered by tests, with an explicit, honest note on the one AC (4, keyboard stepping) that real browsers satisfy via native semantics but this jsdom-based suite cannot directly exercise.

### File List

- `frontend/src/features/live-race/ReplayBar/ReplayBar.tsx` (modified)
- `frontend/src/features/live-race/ReplayBar/ReplayBar.test.tsx` (modified)
