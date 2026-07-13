---
baseline_commit: f036b5f
---

# Story 8.5: Replay pause and resume

Status: review

## Change Log

- 2026-07-13: Added a `visibilitychange` listener to `ReplayBar.tsx` that auto-pauses playback when the tab is backgrounded while playing (AC 3). Confirmed with new fake-timer tests that AC 1 and 2 (pause halts advancing; resume continues from the exact paused lap) were already satisfied by Story 8.2's existing `play`/`pause`/ticking-effect implementation — no changes needed there.

## Story

As a fan replaying a past race,
I want to pause at any point and resume from exactly that point,
so that I don't lose my place if I need to stop and look at something.

## Acceptance Criteria

1. **Given** playback is running, **when** I click Pause, **then** playback halts at the current lap and stops advancing.
2. **Given** playback is paused, **when** I click Play again, **then** it resumes from exactly the paused lap.
3. **Given** I background the tab mid-replay, **when** I return within the same page session, **then** playback is still paused at the same lap (no cross-reload persistence required).

## Tasks / Subtasks

- [x] Task 1: Verify and formally test AC 1 and 2 (already satisfied by construction)
  - [x] Read `ReplayBar.tsx` and `replayStore.ts` in full before writing anything — the play/pause toggle (`handlePlayPause`), the ticking `useEffect`, and `currentLapIndex` persistence in the Zustand store are all unchanged since Story 8.2 and already implement pause-halts / resume-from-exact-lap correctly
  - [x] Add a direct test: start playback, advance via the fake-timer tick, click pause — assert the interval stops advancing `currentLapIndex` on further timer advances (no existing test asserts this negative — the play/pause tests added in 8.2 only check the button label toggling and `isPlaying`, not that ticking actually stops)
  - [x] Add a direct test: pause at a non-zero lap, click play again — assert playback resumes and continues advancing from that exact lap index, not from 0 or from a stale value
- [x] Task 2: Auto-pause on tab backgrounding (AC 3 — the one genuinely new behavior)
  - [x] Add a `document.addEventListener('visibilitychange', ...)` effect in `ReplayBar.tsx`: when `document.hidden` becomes `true` while `isPlaying`, call the store's `pause()`. Do nothing on the reverse transition (returning to the tab does not auto-resume — AC 3 only requires it to still be paused, not to restart)
  - [x] Clean up the listener on unmount
  - [x] If playback was already paused when the tab is backgrounded, the listener is a no-op — nothing to test beyond "doesn't throw / doesn't change state"
- [x] Task 3: Tests (AC 3)
  - [x] Simulate backgrounding while playing: set `document.hidden` (via `Object.defineProperty` override, since jsdom's `document.hidden` is read-only by default) and dispatch a `visibilitychange` event — assert `isPlaying` becomes `false` and `currentLapIndex` is whatever it was at that moment
  - [x] Simulate backgrounding while already paused — assert no state change (still paused, same lap)
  - [x] No test can exercise real OS-level tab backgrounding or actual `setInterval` throttling — document.hidden + a dispatched event is the standard jsdom-testable proxy for this browser API, and it's what's actually being coded against

## Dev Notes

- **This is the smallest story in Epic 8.** AC 1 and 2 require no new implementation — `replayStore.play()`/`pause()` (Story 8.2) already halt/resume the ticking `useEffect` correctly, and `currentLapIndex` is never touched by pause/play, only by the ticking interval and the scrub bar (Story 8.3). The only genuinely new code is the `visibilitychange` listener for AC 3.
- **Read `ReplayBar.tsx` in full before editing** (unchanged since Story 8.4 — see `frontend/src/features/live-race/ReplayBar/ReplayBar.tsx`). The ticking effect at lines ~57-71 already reads `isPlaying` from a dependency array and tears down its `setInterval` via the effect's cleanup function whenever `isPlaying` flips to `false` — this is exactly what "halts and stops advancing" means, and it already works. Do not rewrite this effect; only add a new, separate effect for the visibility listener.
- **Why AC 3 needs new code and isn't already satisfied**: browsers throttle (not necessarily fully suspend) `setInterval` timers in backgrounded tabs — they don't reliably stop firing. Relying on that undocumented throttling to produce "still paused at the same lap" would be fragile and unverifiable. The explicit fix is to actively call `pause()` when the tab becomes hidden while playing, so the state is deterministically paused regardless of browser timer-throttling behavior. This is a real UX decision (background = auto-pause), not just a workaround — it also means a user who backgrounds the tab doesn't come back to find the replay has silently jumped ahead.
- **jsdom test note**: `document.hidden` is a getter with no setter in jsdom by default. Use `Object.defineProperty(document, 'hidden', { value: true, configurable: true })` before dispatching `document.dispatchEvent(new Event('visibilitychange'))`, and reset it in an afterEach/next test's setup to avoid leaking `hidden: true` into later tests (the same category of mock-isolation bug documented in Story 8.3's Debug Log — don't repeat it here for a different piece of module-level state).
- **No backend changes. No new dependencies.**

### Project Structure Notes

- Modified: `frontend/src/features/live-race/ReplayBar/ReplayBar.tsx`, `frontend/src/features/live-race/ReplayBar/ReplayBar.test.tsx`.
- No changes expected to `replayStore.ts` — `play()`/`pause()`/`currentLapIndex` already do everything this story's ACs need; the new listener only calls the existing `pause()` action.

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 8.5]
- [Source: _bmad-output/phase-2/implementation-artifacts/8-2-race-replay-start-stop-restart-controls.md] — `replayStore.play()`/`pause()`, the ticking effect this story adds a sibling effect alongside
- [Source: _bmad-output/phase-2/implementation-artifacts/8-3-replay-scrub-bar-and-lap-jump.md] — documents the `clearAllMocks()` mock-isolation pitfall relevant to this story's own module-level `document.hidden` state
- [Source: frontend/src/features/live-race/ReplayBar/ReplayBar.tsx]
- [Source: frontend/src/features/live-race/store/replayStore.ts]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- Wrote fake-timer tests for AC 1/2 (`clicking pause halts playback...`, `clicking play again after pause resumes from exactly the paused lap`) *before* touching any implementation code — both passed immediately against the unmodified Story 8.2 `play`/`pause`/ticking-effect logic, confirming those ACs needed no new code, only formal test coverage that didn't exist yet.
- The `visibilitychange` test failed as expected before the listener existed (`expected true to be false` on `isPlaying`), then passed once the effect was added — genuine red/green cycle for the one new piece of behavior in this story.
- Reused the `document.hidden` reset-in-`beforeEach` pattern the same way Story 8.3 documented for `mockReturnValue` leakage: `Object.defineProperty(document, 'hidden', ...)` is module-level DOM state that would otherwise leak `true` into whichever test runs next.
- Frontend: full suite same 11 pre-existing failures (unrelated `dateUtils` formatting tests), 145 passing (+4 new). Typecheck clean. Lint clean (`eslint src/features/live-race/ReplayBar/`, zero findings).

### Completion Notes List

- AC 1 and AC 2 required zero implementation changes — Story 8.2's `play()`/`pause()` actions and the ticking `useEffect`'s cleanup-on-`isPlaying`-false already satisfied "halts and stops advancing" / "resumes from exactly the paused lap." This story added the missing direct test coverage that proved it, rather than assuming it from adjacent tests.
- AC 3 (backgrounding auto-pauses) is the one real addition: a `visibilitychange` listener that calls the existing `pause()` action when the tab becomes hidden while playing. Deliberately does not auto-resume on return — the AC only requires "still paused at the same lap," not restarting playback.
- All 3 ACs covered by tests; the two backgrounding tests are jsdom's standard testable proxy for the Page Visibility API (`document.hidden` + a dispatched `visibilitychange` event), not real OS-level tab switching, which no browser-based test tooling here can exercise.

### File List

- `frontend/src/features/live-race/ReplayBar/ReplayBar.tsx` (modified)
- `frontend/src/features/live-race/ReplayBar/ReplayBar.test.tsx` (modified)
