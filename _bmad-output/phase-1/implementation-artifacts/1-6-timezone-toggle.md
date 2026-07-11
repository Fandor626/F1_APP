---
baseline_commit: b4a130f
---

# Story 1.6: Timezone Toggle

Status: done

## Story

As a fan checking session times,
I want to toggle between Track Time and Local Time,
so that I know exactly when to watch in my own timezone.

## Acceptance Criteria

1. **Given** the detail view **When** the timezone toggle is switched **Then** all Session times update immediately between Track Time and browser-detected Local Time.
2. **Given** the detail view loads **When** no toggle interaction has occurred **Then** Local Time is the default displayed state.

## Tasks / Subtasks

- [x] Task 1: Extend `formatSessionTime` in `dateUtils.ts` to support Track Time mode (AC: 1)
  - [x] Add a second export `formatSessionTimeForMode(iso: string, mode: 'local' | 'track'): string` (keep the existing zero-arg `formatSessionTime(iso)` as-is to avoid breaking `RaceWeekendDetailView.tsx` and its existing test assertions; the detail view will switch to the new function in Task 3).
  - [x] For `'local'` mode: identical to the existing `SESSION_TIME_FORMATTER.format(new Date(iso))` — no change in behavior.
  - [x] For `'track'` mode: extract the UTC offset embedded in the ISO string (e.g. `+03:00`, `-05:00`, `+00:00`), shift the UTC epoch value by that offset, then format using a `Intl.DateTimeFormat` instance with `timeZone: 'UTC'` and the same `{ weekday: 'short', day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }` options — this makes the UTC formatter show the circuit's local wall-clock time without any browser-timezone conversion.
  - [x] Offset extraction: parse with `/([+-])(\d{2}):(\d{2})$/` against the ISO string; treat `Z` suffix or a missing match as `+00:00`. Never use `Date.getTimezoneOffset()` — that's the browser offset, not the circuit's.
  - [x] Offset shift math: `const offsetMs = sign * (hours * 60 + minutes) * 60_000; new Date(new Date(iso).getTime() + offsetMs)` — the adjusted date fed into the UTC formatter yields the circuit's wall-clock time.
  - [x] Add `TRACK_TIME_FORMATTER` as a module-level constant (same options as `SESSION_TIME_FORMATTER` but with `timeZone: 'UTC'` added) so it isn't re-created on every call.

- [x] Task 2: Build `TimezoneToggle.tsx` (AC: 1, 2)
  - [x] Create `frontend/src/features/calendar/TimezoneToggle.tsx` — architecture-named file (the project tree already lists it under `← FR-5`).
  - [x] Props: `mode: 'local' | 'track'` and `onToggle: () => void`.
  - [x] Render a two-state button/switch labelled **"Track"** and **"Local"** per UX EXPERIENCE.md Component Patterns table ("Two-state switch (Track / Local)"). The active option is visually distinguished.
  - [x] Use existing typography tokens consistent with `RaceWeekendDetailView.tsx` (e.g. `text-[13px]`, `text-text-secondary`, `text-text-primary` for active vs inactive label). No new visual tokens.
  - [x] The component is purely presentational: no state of its own, no data fetching.
  - [x] Keyboard accessible: both states reachable via `Tab` and activated via `Enter`/`Space` (per EXPERIENCE.md accessibility rule for all interactive elements, including toggles).

- [x] Task 3: Wire the toggle into `RaceWeekendDetailView.tsx` (AC: 1, 2)
  - [x] Add `const [tzMode, setTzMode] = useState<'local' | 'track'>('local')` — `'local'` default satisfies AC 2. This is local React state: the timezone preference is scoped to a single detail view, resets on navigate-away (correct behavior — no cross-page persistence needed). Architecture names Zustand for "timezone toggle selection" as future UI state, but the calendar feature has no store file and this toggle has no cross-component consumers — local state is the right call for this story.
  - [x] Replace the `formatSessionTime(session.start)` call in the session list `<li>` with `formatSessionTimeForMode(session.start, tzMode)`.
  - [x] Render `<TimezoneToggle>` inline with the "Sessions" `<h2>` heading row (control adjacent to what it controls); `<ContextualData />` remains below the sessions list.
  - [x] Add `import { useState } from 'react'` if not already present.

- [x] Task 4: Tests for `dateUtils.ts` track-time logic (AC: 1)
  - [x] In `frontend/src/shared/utils/dateUtils.test.ts`, add test cases for `formatSessionTimeForMode`:
    - `'local'` mode returns the same string as the existing `formatSessionTime` for the same input.
    - `'track'` mode for `'2026-03-13T15:30:00+03:00'` includes `'15:30'` in the output (circuit is UTC+3; output should show 15:30, not 12:30 UTC).
    - `'track'` mode for `'2026-03-08T18:00:00+00:00'` includes `'18:00'` in the output (UTC circuit; no shift).
    - `'track'` mode for `'2026-03-20T14:00:00-05:00'` includes `'14:00'` in the output (UTC-5 circuit; shift backward by 5h from 19:00 UTC → 14:00 display).
    - `'track'` mode for a `Z`-suffix string (e.g. `'2026-03-08T18:00:00Z'`) includes `'18:00'` in the output (Z treated as +00:00).
  - [x] Use `toContain` on the formatted string rather than exact equality — the formatter's locale-dependent `hour: '2-digit'` output (e.g. `06:00 PM` vs `18:00`) varies by test environment. Asserting the time digits are present is sufficient and environment-portable.

- [x] Task 5: Build `TimezoneToggle.test.tsx` co-located with the component (AC: 1, 2)
  - [x] Create `frontend/src/features/calendar/TimezoneToggle.test.tsx`.
  - [x] Test: renders with `mode='local'` → "Local" label is visually active/highlighted (e.g. check for a class or `aria-pressed`).
  - [x] Test: renders with `mode='track'` → "Track" label is visually active/highlighted.
  - [x] Test: clicking "Track" when `mode='local'` calls `onToggle` once.
  - [x] Test: clicking "Local" when `mode='track'` calls `onToggle` once.
  - [x] Do **not** test time formatting here — that belongs in `dateUtils.test.ts`.

- [x] Task 6: Update `RaceWeekendDetailView.test.tsx` if needed (AC: 1, 2 — regression prevention)
  - [x] The existing 4 tests assert session labels (`FP1`/`FP2`/…), the error message, and the back link — none assert specific time strings, so no time-format assertion will break.
  - [x] The toggle element is new DOM — verified the existing `screen.getAllByRole('listitem')` assertions still pass (toggle renders two `<button>` elements, no `<li>` or `role="listitem"`). All 4 tests pass with no changes needed.
  - [x] No regressions surfaced; no new tests added.

## Dev Notes

- **UX default conflict:** EXPERIENCE.md's Component Patterns table (the authoritative contract) says "Local is the default on first load." Flow 2's narrative says "see every session listed in Track Time by default, then flip to Local Time" — this describes the action of toggling, not the actual default state. The epics ACs (AC 2: "Local Time is the default") and the Component Patterns table are consistent. **Implement Local as the default.**

- **Track Time implementation approach:** The ISO strings returned by the backend always contain the circuit's UTC offset (e.g. `2026-03-13T15:30:00+03:00`). The circuit's wall-clock time is the time component in the string itself (`15:30`). To display it correctly: shift the UTC epoch value by the offset so it appears at UTC, then format with `timeZone: 'UTC'`. This avoids any dependency on IANA timezone names (which aren't embedded in the session data) and correctly handles non-whole-hour offsets like `+05:30` if they ever appear.

- **Do not use `Etc/GMT±N` timezone names in `Intl.DateTimeFormat`.** Those zones only support whole-hour offsets and use inverted sign convention (e.g. `Etc/GMT-3` is UTC+3), making them error-prone. The UTC-shift approach is more robust and simpler.

- **Keep existing `formatSessionTime(iso)` unchanged.** `RaceWeekendDetailView.tsx` currently calls it; `RaceWeekendDetailView.test.tsx` exercises the rendered output. Changing the signature would ripple the tests unnecessarily. Add `formatSessionTimeForMode` as a new export alongside the existing function; the detail view switches to it in Task 3.

- **No Zustand store for this story.** Architecture names Zustand for "timezone toggle selection" as future UI state (e.g. for a global toggle persisted across navigation), but the calendar feature has no store directory and this toggle has no cross-component consumers. Local `useState` is the correct call for this story's scope: the preference resets on navigate-away (which matches the "default is Local on load" AC), and introducing a Zustand slice for a single boolean would be premature abstraction. If Epic 2+ requires a persistent timezone preference, extract to a store then.

- **`TimezoneToggle.tsx` is architecture-named.** The project tree in `architecture.md#Project Structure & Boundaries` already lists `TimezoneToggle.tsx` under `features/calendar/` against `← FR-5`. No path decision needed.

- **No backend changes.** The ISO strings with embedded offsets are already returned by the `/api/races/:round` endpoint from Story 1.4. The timezone conversion is purely client-side.

- **`RaceWeekendDetailView.test.tsx` listitem count.** The existing tests assert exactly 5 `listitem` elements (one per session). `TimezoneToggle` must not render any `<li>` or `role="listitem"` elements or those assertions will break — same trap as `ContextualData.tsx` in Story 1.5.

- **Toggle placement in the DOM.** EXPERIENCE.md Flow 2 order: sessions → timezone toggle → last year's winner + championship gap → win probability widget. So `<TimezoneToggle>` goes between `</ul>` (sessions) and `<ContextualData>` in `RaceWeekendDetailView.tsx`.

### Project Structure Notes

- `TimezoneToggle.tsx` and `TimezoneToggle.test.tsx` land in `frontend/src/features/calendar/` per the architecture tree — no new folders.
- `dateUtils.ts` and `dateUtils.test.ts` are in `frontend/src/shared/utils/` — modify in place, no new files in `shared/`.
- No backend files touched.

### References

- [Source: epics.md#Story 1.6: Timezone Toggle] — AC source, user story statement.
- [Source: prd.md#FR-5: Timezone toggle] — feature description (two-state toggle, all times update immediately, Local default).
- [Source: architecture.md#Project Structure & Boundaries] — `TimezoneToggle.tsx` named under `features/calendar/` against FR-5.
- [Source: architecture.md#Implementation Patterns & Consistency Rules] — Zustand hard rule (UI state only, never server data); "one store file per feature slice" (calendar has none yet).
- [Source: ux-designs/ux-F1_poc-2026-06-16/EXPERIENCE.md#Component Patterns] — "Two-state switch (Track / Local). All session times update immediately on flip; Local is the default on first load."
- [Source: ux-designs/ux-F1_poc-2026-06-16/EXPERIENCE.md#Key Flows, Flow 2] — page order: sessions → timezone toggle → last year's winner + championship gap.
- [Source: ux-designs/ux-F1_poc-2026-06-16/EXPERIENCE.md#Accessibility] — toggles reachable via Tab and operable via Enter/Space.
- [Source: frontend/src/shared/utils/dateUtils.ts] — existing `SESSION_TIME_FORMATTER` and `formatSessionTime` to extend, not replace.
- [Source: frontend/src/features/calendar/RaceWeekendDetailView.tsx] — current state; `formatSessionTime` call site, `<ContextualData>` placement.
- [Source: frontend/src/features/calendar/RaceWeekendDetailView.test.tsx] — 5-listitem assertion regression trap; no time-string assertions (so time format change won't break it).
- [Source: frontend/src/shared/mocks/handlers/ergastHandlers.ts] — mock ISO strings with embedded offsets (`+00:00`, `+03:00`) available for testing.

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6

### Debug Log References

- `@testing-library/user-event` not installed (only `@testing-library/react` and `@testing-library/dom` are in the project). Switched `TimezoneToggle.test.tsx` from `userEvent.click` to `fireEvent.click` — no behavior difference for simple click tests.

### Completion Notes List

- Added `formatSessionTimeForMode(iso, mode)` to `dateUtils.ts` alongside the existing `formatSessionTime`. Track mode: parses the UTC offset from the ISO string with `/([+-])(\d{2}):(\d{2})$/` (Z/missing = +00:00), shifts the UTC epoch by that offset, formats with `Intl.DateTimeFormat(undefined, {..., timeZone: 'UTC'})` so the UTC display shows the circuit's wall-clock time. Module-level `TRACK_TIME_FORMATTER` constant avoids per-call re-allocation. `formatSessionTime` unchanged — no call-site regressions.
- Built `TimezoneToggle.tsx` as a pure presentational component: two `<button>` elements with `aria-pressed`, `onToggle` callback, Tailwind tokens from the existing palette. Keyboard accessible natively via buttons (Tab + Enter/Space).
- Wired into `RaceWeekendDetailView.tsx` with `useState<'local'|'track'>('local')` — Local default satisfies AC 2. Toggle placed inline with the "Sessions" `<h2>` so the control is adjacent to what it controls. `formatSessionTimeForMode` called in the session list `<li>`.
- All 4 existing `RaceWeekendDetailView.test.tsx` assertions pass unchanged — toggle renders `<button>` elements, no `listitem` role added.
- Verification: `vitest run` → 31/31 passing (+9 from this story: 5 dateUtils, 4 TimezoneToggle); `tsc -b` clean; `eslint .` clean.

### File List

**Added:**
- `frontend/src/features/calendar/TimezoneToggle.tsx`
- `frontend/src/features/calendar/TimezoneToggle.test.tsx`

**Modified:**
- `frontend/src/shared/utils/dateUtils.ts` (added `TRACK_TIME_FORMATTER`, `parseOffsetMinutes`, `formatSessionTimeForMode`; patch: added UTC-shift comment)
- `frontend/src/shared/utils/dateUtils.test.ts` (added 5 `formatSessionTimeForMode` test cases)
- `frontend/src/features/calendar/TimezoneToggle.tsx` (patch: added `tabIndex={-1}` + `aria-disabled` to active button)
- `frontend/src/features/calendar/RaceWeekendDetailView.tsx` (added `useState` for `tzMode`, `TimezoneToggle` render, switched to `formatSessionTimeForMode`)
- `frontend/src/shared/mocks/handlers/ergastHandlers.ts` (updated session ISO strings for Bahrain/Jeddah to `+03:00` circuit-local offsets)
- `backend/F1App.Api/Services/RaceScheduleService.cs` (added `CircuitTimezones` dict, `CombineDateAndTime` circuit-local conversion, `BuildSessions` passes circuitId)
- `backend/F1App.Api.Tests/Services/RaceScheduleServiceTests.cs` (added `GetRaceDetailAsync_SessionsConvertToCircuitLocalTime` test)
- `_bmad-output/implementation-artifacts/1-6-timezone-toggle.md` (this file)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (status tracking)
- `_bmad-output/implementation-artifacts/deferred-work.md` (3 deferred items added)

## Review Findings

- [x] [Review][Decision] Backend normalizes all session times to UTC (`+00:00`) — resolved by adding `CircuitTimezones` IANA dictionary and `TimeZoneInfo.ConvertTime` in `RaceScheduleService.cs`. Sessions now carry correct circuit-local `DateTimeOffset` (e.g. Bahrain `+03:00`). MSW mock updated to match. Backend 33/33, frontend 31/31.
- [x] [Review][Patch] Active button `onClick={undefined}` is not keyboard-accessible — resolved: added `tabIndex={-1}` and `aria-disabled={true}` to the currently-active button in `TimezoneToggle.tsx`. Active button is removed from tab order and announced as disabled by screen readers.
- [x] [Review][Patch] `TRACK_TIME_FORMATTER` `timeZone: 'UTC'` is non-obvious — resolved: added one-line comment above the formatter explaining the UTC-shift pattern (caller pre-shifts timestamp by circuit offset; UTC display shows circuit wall-clock time). [dateUtils.ts]
- [x] [Review][Defer] ARIA semantics: two `aria-pressed` sibling buttons vs. `role="radiogroup"` + `role="radio"` — using `aria-pressed` on independent buttons doesn't communicate mutual exclusivity to assistive technology; `role="radiogroup"` with `role="radio"` children would be more semantically correct [TimezoneToggle.tsx] — deferred, best-practice improvement not a correctness bug
- [x] [Review][Defer] DST edge case — static UTC offset in ISO string may be wrong if the backend uses the standard (non-DST) offset for a session that falls in a DST period [dateUtils.ts:34-46] — deferred, upstream data quality concern
- [x] [Review][Defer] Null/invalid-date guard in `formatSessionTimeForMode` — malformed ISO string produces `Invalid Date` silently; pre-existing concern mitigated by Zod schema at the API boundary [dateUtils.ts:41-46] — deferred, pre-existing, Zod guards at boundary

## Change Log

- 2026-06-17: Story created via create-story workflow.
- 2026-06-17: Implemented all 6 tasks. New `formatSessionTimeForMode` with UTC-shift track-time logic; `TimezoneToggle.tsx` pure presentational component with `aria-pressed`; wired into `RaceWeekendDetailView.tsx` with local `useState` defaulting to Local. All ACs satisfied; 31/31 tests passing; tsc + eslint clean. Status → review.
- 2026-06-17: Applied all 3 review patches. (1) Backend circuit-local conversion: added `CircuitTimezones` IANA dict + `TimeZoneInfo.ConvertTime` in `RaceScheduleService.cs`; MSW mock updated to `+03:00` for Bahrain/Jeddah. (2) `TimezoneToggle.tsx` active button: `tabIndex={-1}` + `aria-disabled`. (3) `dateUtils.ts` `TRACK_TIME_FORMATTER`: UTC-shift comment. Backend 33/33, frontend 31/31. Status → done.
