---
baseline_commit: 2ea1f92
---

# Story 8.4: Replay playback speed control

Status: review

## Change Log

- 2026-07-13: Added the 1x/2x/4x speed group (FR-8) to `ReplayBar.tsx`, reusing `RaceFilterTabs.tsx`'s tab-toggle visual pattern. Added the mobile overflow toggle for Restart + speed group, implemented as a single (non-duplicated) element set with a breakpoint-aware class expression rather than two separately-rendered desktop/mobile copies.

## Story

As a fan replaying a past race,
I want to select a playback speed,
so that I can watch a specific stretch more quickly or more carefully.

## Acceptance Criteria

1. **Given** the Replay bar, **when** I select a speed (1x, 2x, or 4x), **then** playback immediately advances at that rate without restarting.
2. **Given** the speed control, **when** rendered, **then** it's a button group built from the existing tab-toggle visual pattern, with the active speed visually indicated.
3. **Given** a mobile viewport (`< md`), **when** the Replay bar renders, **then** the speed group is tucked behind a compact overflow (`⋯`) button rather than staying inline, keeping play/pause and scrub within one-handed reach (NFR2).

## Tasks / Subtasks

- [x] Task 1: Speed button group UI (AC 1, 2)
  - [x] 3 buttons (1x/2x/4x) styled like `RaceFilterTabs.tsx`'s tab-toggle pattern (pill/segmented control, active option highlighted)
  - [x] `onClick` calls `setSpeed(n)` — the existing ticking effect (Story 8.2) already reads `speed` reactively, so this satisfies AC 1 with no interval-restart logic
- [x] Task 2: Mobile overflow behavior (AC 3)
  - [x] One copy of Restart + speed group (not duplicated per breakpoint) inside a wrapper whose class expression is `${overflowOpen ? 'flex' : 'hidden'} md:flex` — mobile visibility follows the toggle state, desktop `md:flex` unconditionally overrides it to always-visible. Avoids duplicate markup/test-id collisions entirely
  - [x] `⋯` overflow toggle (`md:hidden` — desktop has nothing to toggle) uses local `useState`, matching `ChampionshipSidebar.tsx`'s established mobile-disclosure pattern (Story 7.2)
  - [x] Play/pause and scrub bar untouched — always inline at every width
- [x] Task 3: Tests (AC 1, 2, 3)
  - [x] Selecting each speed updates the store; active speed's `aria-selected` reflects it; lap index and `isPlaying` are untouched by a speed change
  - [x] Overflow toggle's open/close state verified directly (`aria-expanded`)
  - [x] Noted, not silently skipped: which breakpoint state actually renders (`hidden` vs `md:flex` winning) depends on real CSS evaluation jsdom doesn't perform — what's verified is that both the elements and the toggle's own state logic are correct

## Dev Notes

- **Read `ReplayBar.tsx` (Stories 8.2/8.3) in full before editing.** `replayStore.speed` and the ticking `useEffect` (`BASE_TICK_MS / speed` interval, dependency array already includes `speed`) already exist and already work correctly for any speed value — this story is UI-only, adding the control that calls the existing `setSpeed` action. Don't touch the ticking effect itself unless you find it's actually broken (it isn't, per Story 8.2/8.3's passing tests).
- **Check `RaceFilterTabs.tsx` (Story 7.1) or `StandingsPage.tsx`'s Drivers/Constructors toggle for the exact "tab-toggle" visual classes before writing new ones** — DESIGN.md explicitly says the speed group extends this same token; copying slightly different styling would be a visible inconsistency, not just a missed reuse opportunity.
- **The mobile overflow requirement is genuinely CSS-breakpoint-driven** (`< md` viewport), which jsdom's fixed test viewport cannot exercise directly — same limitation already documented in Story 7.2 (`ChampionshipSidebar`'s mobile drawer). What IS testable and must be tested: the overflow toggle's own open/close interaction (a real React state change, independent of which breakpoint is visually active).
- **No backend changes. No new dependencies.**

### Project Structure Notes

- Modified: `frontend/src/features/live-race/ReplayBar/ReplayBar.tsx`, `frontend/src/features/live-race/ReplayBar/ReplayBar.test.tsx`.

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 8.4]
- [Source: _bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/DESIGN.md#replay-bar.speedGroup, #replay-bar.mobile]
- [Source: _bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/EXPERIENCE.md#Responsive & Platform]
- [Source: _bmad-output/phase-2/implementation-artifacts/8-2-race-replay-start-stop-restart-controls.md] — `replayStore.speed`, the ticking effect this story's control drives
- [Source: frontend/src/features/calendar/RaceFilterTabs.tsx] — existing tab-toggle pattern to reuse, not reinvent

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- Considered duplicating Restart + speed group markup once for a `hidden md:flex` (desktop) wrapper and once for a `flex md:hidden` (mobile-overflow) wrapper, matching a naive reading of "two variants." Rejected: it would require distinct test ids per copy to avoid `getByTestId` ambiguity (jsdom renders both simultaneously — no real stylesheet evaluation to actually hide either one), and would let the two copies drift out of sync over time. Used a single element set with a dynamic class expression (`${overflowOpen ? 'flex' : 'hidden'} md:flex`) instead — Tailwind's breakpoint cascade means `md:flex` wins at desktop widths regardless of the mobile-only toggle state, with zero duplication.
- Frontend: full suite same 11 pre-existing failures, 141 passing (+3 new). Typecheck and lint clean.

### Completion Notes List

- Speed selection reuses the exact `setSpeed` action and ticking effect from Story 8.2 unmodified — this story is additive UI only, no changes to replay engine logic.
- Explicitly verified (not assumed) that changing speed doesn't reset `currentLapIndex` or `isPlaying`, satisfying AC 1's "without restarting."
- All 3 ACs covered by tests, with an honest note on the one AC (3, the actual breakpoint swap) that depends on real CSS evaluation this jsdom suite can't perform — what's verified instead is the underlying toggle logic and the absence of duplicated markup.

### File List

- `frontend/src/features/live-race/ReplayBar/ReplayBar.tsx` (modified)
- `frontend/src/features/live-race/ReplayBar/ReplayBar.test.tsx` (modified)
