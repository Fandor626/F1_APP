---
baseline_commit: "0200e5b"
---

# Story 6.2: Race Weekend Streak

Status: done

## Story

As a dedicated fan,
I want to track how many consecutive race weekends I've followed live,
So that I can see my own engagement streak.

## Acceptance Criteria

1. **Given** the user visits the app during a live Session (per Epic 2's live state) **Then** the streak counter on the calendar page increments for that Race Weekend, once per weekend.
2. **Given** a Race Weekend passes without a live-session visit **Then** the streak counter resets at the next weekend.
3. **And** the streak is stored in browser localStorage with a versioned key — no account/backend required, does not persist across devices.

## Design decisions

- **Pure frontend, no backend changes** — the AC is entirely a localStorage concern; this is the first Epic 6 story with zero backend work.
- **`useLocalStorage.ts`** (`frontend/src/shared/hooks/`) is a new generic hook — the architecture doc planned it from Epic 1 (NFR-7: "versioned keys with migration logic from day one") but nothing has actually consumed it until now. Following the documented key format `f1app__{featureName}__{key}` (version embedded in the key name itself, e.g. `f1app__streak__v1` — a future schema change bumps to `v2` and the hook simply won't find the old key, defaulting fresh rather than crashing on an incompatible shape). There is nothing to migrate *from* yet (this is the first consumer), so "migration support" here means the mechanism is structurally in place (versioned key naming), not that dead migration code was written for a version bump that hasn't happened.
- **"Which weekend is live right now" isn't tracked anywhere in the live-race feature** (`liveRaceStore.ts` carries driver/session state but no round/season identifier) — verified by inspection before designing this story. Rather than adding a new backend field, the current weekend is derived purely from the already-loaded race schedule: the race whose `[weekendStart, raceStart + 24h]` window contains "now". A live SignalR session can only be active when "now" genuinely falls in some weekend's window, so this is a reliable-enough proxy without new backend plumbing.
- **Streak continuity is tracked by index within the currently-loaded season schedule**, not by round number matching across seasons — `useRaceSchedule()` only ever returns the *current* season (per Story 1.2's scope), so there is no data available to detect "last race of last season → first race of this season" as adjacent. This is an accepted, documented POC limitation: the streak will read as "reset" at the first race of a new season even for a fan who watched every session including last season's finale. Fixing this would require a cross-season schedule endpoint, which is out of scope here.
- **No cross-tab/cross-page reactivity** — `StreakCounter` reads localStorage synchronously at render/mount time rather than subscribing to changes. Since navigation between the Live Race page (where a visit gets recorded) and the Calendar page (where the count is displayed) is a fresh mount each time in this single-page-at-a-time SPA, this is sufficient; no scenario in this app has both pages simultaneously visible needing live sync.

## Tasks / Subtasks

### Task 1: Frontend — `useLocalStorage` versioned hook (AC: 3)

- [ ] Create `frontend/src/shared/hooks/useLocalStorage.ts`:
  ```ts
  import { useCallback, useState } from 'react'

  // Key format: f1app__{featureName}__{version} — e.g. f1app__streak__v1.
  // The version is embedded in the key itself (not a field inside the
  // stored value): bumping the version is how a future breaking schema
  // change is "migrated" — old data is simply never read again under the
  // new key, rather than needing in-place upgrade logic for a version
  // bump that doesn't exist yet.
  export function useLocalStorage<T>(key: string, defaultValue: T): [T, (value: T) => void] {
    const [value, setValue] = useState<T>(() => {
      try {
        const raw = window.localStorage.getItem(key)
        return raw ? (JSON.parse(raw) as T) : defaultValue
      } catch {
        return defaultValue
      }
    })

    const setAndPersist = useCallback(
      (next: T) => {
        setValue(next)
        try {
          window.localStorage.setItem(key, JSON.stringify(next))
        } catch {
          // localStorage unavailable (private browsing quota, disabled
          // storage, etc.) — fail silently; in-memory state for this
          // session still works, it just won't persist across reloads.
        }
      },
      [key],
    )

    return [value, setAndPersist]
  }
  ```

### Task 2: Frontend — Streak storage logic (AC: 1, 2, 3)

- [ ] Create `frontend/src/features/fan-engagement/streakStorage.ts`:
  ```ts
  import type { RaceWeekend } from '../../shared/api/ergast'

  export const STREAK_STORAGE_KEY = 'f1app__streak__v1'

  export interface StreakState {
    count: number
    lastCountedIndex: number | null
  }

  export const DEFAULT_STREAK_STATE: StreakState = { count: 0, lastCountedIndex: null }

  export function readStreakState(): StreakState {
    try {
      const raw = window.localStorage.getItem(STREAK_STORAGE_KEY)
      return raw ? (JSON.parse(raw) as StreakState) : DEFAULT_STREAK_STATE
    } catch {
      return DEFAULT_STREAK_STATE
    }
  }

  function writeStreakState(state: StreakState): void {
    try {
      window.localStorage.setItem(STREAK_STORAGE_KEY, JSON.stringify(state))
    } catch {
      // See useLocalStorage.ts — fail silently.
    }
  }

  // A weekend is "current" if `now` falls between its weekend start and
  // ~1 day after its race — covers the live session window with a buffer
  // for late-running sessions, without needing a live-race-specific
  // round/season signal (none exists in liveRaceStore today).
  export function findCurrentWeekendIndex(races: RaceWeekend[], now: Date): number | null {
    const index = races.findIndex((race) => {
      const start = new Date(race.weekendStart)
      const end = new Date(new Date(race.raceStart).getTime() + 24 * 60 * 60 * 1000)
      return now >= start && now <= end
    })
    return index === -1 ? null : index
  }

  // Idempotent — safe to call on every render while a live session is
  // active; a weekend already counted is a no-op (AC 1: "once per weekend").
  export function recordLiveVisit(currentIndex: number): StreakState {
    const current = readStreakState()
    if (current.lastCountedIndex === currentIndex) return current // already counted this weekend

    // Adjacent to the last counted weekend -> streak continues.
    // Otherwise (gap, or very first visit) -> streak restarts at 1.
    const isConsecutive = current.lastCountedIndex !== null && currentIndex === current.lastCountedIndex + 1
    const next: StreakState = {
      count: isConsecutive ? current.count + 1 : 1,
      lastCountedIndex: currentIndex,
    }
    writeStreakState(next);
    return next
  }
  ```

### Task 3: Frontend — Hooks wiring (AC: 1, 2)

- [ ] Create `frontend/src/features/fan-engagement/useRecordLiveVisit.ts` (used inside `LiveRacePage`):
  ```ts
  import { useEffect } from 'react'
  import { useRaceSchedule } from '../../shared/api/ergast'
  import { useFallbackState } from '../live-race/hooks/useFallbackState'
  import { findCurrentWeekendIndex, recordLiveVisit } from './streakStorage'

  export function useRecordLiveVisit(): void {
    const { isLive } = useFallbackState()
    const { data: schedule } = useRaceSchedule()

    useEffect(() => {
      if (!isLive || !schedule) return
      const currentIndex = findCurrentWeekendIndex(schedule, new Date())
      if (currentIndex !== null) recordLiveVisit(currentIndex)
    }, [isLive, schedule])
  }
  ```

### Task 4: Frontend — `StreakCounter` component (AC: 1)

- [ ] Create `frontend/src/features/fan-engagement/StreakCounter.tsx`:
  ```tsx
  import { readStreakState } from './streakStorage'

  export function StreakCounter() {
    const { count } = readStreakState()
    if (count === 0) return null

    return (
      <span
        data-testid="streak-counter"
        className="inline-flex items-center gap-1.5 rounded-full border border-border-soft bg-bg-inset px-3 py-1 text-[11.5px] font-semibold tracking-[0.04em] text-text-secondary"
      >
        Streak <b className="text-accent-editorial">{count}</b>
      </span>
    )
  }
  ```
  **AC-driven design note**: renders nothing at `count === 0` (a fan who has never watched live has no streak to show — an empty/zero badge would be noise, not information).

### Task 5: Frontend — Wire into pages (AC: 1)

- [ ] `frontend/src/features/live-race/LiveRacePage.tsx` — call `useRecordLiveVisit()` once at the top of the component (side-effect only, no rendered output).
- [ ] `frontend/src/features/calendar/CalendarPage.tsx` — render `<StreakCounter />` next to the page title.
- [ ] `frontend/src/features/fan-engagement/index.ts` — export `StreakCounter`.

### Task 6: Frontend — Tests (AC: 1, 2, 3)

- [ ] Create `frontend/src/features/fan-engagement/streakStorage.test.ts` (pure-function unit tests, no React needed):
  - `findCurrentWeekendIndex` finds the race whose window contains `now`; returns `null` when none matches.
  - `recordLiveVisit` on a fresh (empty) localStorage sets count to 1.
  - `recordLiveVisit` called twice with the **same** index only counts once (AC 1's "once per weekend").
  - `recordLiveVisit` with a **consecutive** index (`lastCountedIndex + 1`) increments the count (AC continuation).
  - `recordLiveVisit` with a **non-consecutive** index (a gap) resets count to 1 (AC 2).
  - Reset `localStorage` in `beforeEach`.
- [ ] Create `frontend/src/features/fan-engagement/StreakCounter.test.tsx`:
  - Renders nothing when no streak is recorded.
  - Renders "Streak 3" (or similar) when `f1app__streak__v1` has `{ count: 3, ... }` pre-seeded in `localStorage`.
- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass.
- [ ] Run `npx tsc --noEmit -p tsconfig.app.json` from `frontend/` — must be clean.

## Dev Notes

### Architecture Alignment

- Matches `architecture.md`'s `shared/hooks/useLocalStorage.ts` and `fan-engagement/StreakCounter.tsx` file tree entries, and the documented `f1app__{featureName}__{key}` localStorage key format (line 371 of architecture.md) applied literally: `f1app__streak__v1`.
- First consumer of the versioned-localStorage pattern promised since Epic 1's NFR-7 — Story 6.3's Fan Card reuses the same `useLocalStorage.ts` hook.

### Regressions to Guard

- `recordLiveVisit` must be idempotent per weekend — calling it on every render tick while `isLive` stays true (which it will, many times per live session) must not increment the count more than once for that weekend. This is enforced by the `lastCountedIndex === currentIndex` early-return, not by only calling it once on mount.
- Do not read `useFallbackState()`/`useLiveRaceStore` from `CalendarPage`/`StreakCounter` expecting it to reflect an active SignalR connection — that store only receives live updates while `LiveRacePage` is mounted (the SignalR connection hook lives there). `StreakCounter` only ever reads the already-persisted localStorage count, never live connection state directly.

### Files to Create / Modify

**Frontend CREATE:**
- `frontend/src/shared/hooks/useLocalStorage.ts`
- `frontend/src/features/fan-engagement/streakStorage.ts`
- `frontend/src/features/fan-engagement/streakStorage.test.ts`
- `frontend/src/features/fan-engagement/useRecordLiveVisit.ts`
- `frontend/src/features/fan-engagement/StreakCounter.tsx`
- `frontend/src/features/fan-engagement/StreakCounter.test.tsx`

**Frontend MODIFY:**
- `frontend/src/features/live-race/LiveRacePage.tsx`
- `frontend/src/features/calendar/CalendarPage.tsx`
- `frontend/src/features/fan-engagement/index.ts`
- `frontend/vite.config.ts` (added `environmentOptions.jsdom.url` — see Completion Notes for why)
- `frontend/package.json` (added `NODE_OPTIONS=--no-experimental-webstorage` to the `test` script — see Completion Notes)

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 6.2: Race Weekend Streak]
- [Source: _bmad-output/planning-artifacts/architecture.md line 371 — localStorage key format; line 552 — useLocalStorage.ts; line 540 — StreakCounter.tsx]
- [Source: frontend/src/features/live-race/hooks/useFallbackState.ts — existing `isLive` signal reused as-is]
- [Source: frontend/src/features/live-race/store/liveRaceStore.ts — confirmed no round/season field exists, informing the schedule-window-based "current weekend" derivation]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

None.

### Completion Notes List

- Implemented per plan, with one design refinement over the original task snippets: `streakStorage.ts` exposes a pure `computeNextStreakState(current, currentIndex)` function (no localStorage I/O) rather than the originally-sketched `recordLiveVisit`/`readStreakState`/`writeStreakState` trio — all actual localStorage I/O goes through `useLocalStorage` (both in `useRecordLiveVisit` and `StreakCounter`), making `useLocalStorage.ts` a genuine, real consumer from day one rather than an unused hook sitting next to hand-rolled localStorage calls. This also made the streak-transition logic trivially unit-testable as a pure function.
- **Found and fixed a real, previously-silent test-environment bug while writing `StreakCounter.test.tsx`**: `window.localStorage` was `undefined` in every test in this suite (not just this story's new tests) — Node 26+ ships its own built-in `globalThis.localStorage`, and Vitest 4.1.9's jsdom-environment setup (`getWindowKeys` in `vitest/dist/chunks/index.DC7d2Pf8.js`) skips copying jsdom's own `window.localStorage` onto the test global whenever a same-named key already exists there, deferring to Node's native (flag-gated, non-functional-without-`--localstorage-file`) implementation instead. Root-caused via a standalone `node -e` JSDOM repro (confirmed jsdom's localStorage works fine with a real origin URL) before finding the actual vitest source-level cause. Fixed with `NODE_OPTIONS=--no-experimental-webstorage` on the `test` npm script, which lets vitest's jsdom populate step install the real, working jsdom `Storage` implementation. Also added `environmentOptions.jsdom.url` to `vite.config.ts` (jsdom's default opaque `about:blank` origin throws `SecurityError: localStorage is not available for opaque origins` even once the shadowing issue above is fixed) — both fixes were necessary together. This explains the "ExperimentalWarning: localStorage is not available" noise visible in every test run throughout this entire session, going back to Epic 1 — it was never actually benign.
- **Environment note**: unlike backend stories, this one is 100% frontend — no `.NET` SDK constraint applies here.
- All frontend tests pass except the 4 pre-existing, unrelated `dateUtils.test.ts` locale failures (confirmed unrelated to localStorage — they're a separate `Intl.DateTimeFormat` locale quirk); `tsc -p tsconfig.app.json` is clean except the one pre-existing `TrackMap.test.tsx` issue noted in Story 5.1; `eslint` is fully clean.

### File List

See "Files to Create / Modify" above — unchanged from plan except the noted `streakStorage.ts` design refinement and the `vite.config.ts`/`package.json` localStorage-in-tests fix.

### Change Log

| Date | Change |
|------|--------|
| 2026-07-10 | Story created and implemented directly (bmad create-story + dev-story cycle) |
