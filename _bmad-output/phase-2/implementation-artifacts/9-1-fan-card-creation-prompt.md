---
baseline_commit: 1096178
---

# Story 9.1: Fan Card creation prompt

Status: review

## Story

As a fan who hasn't created a Fan Card yet,
I want to be invited to create one while I'm on the Standings page, through a proper dialog I can operate with a mouse, touch, or keyboard alike,
so that I discover the feature instead of never finding it.

This story also builds the app's first shared `Modal` primitive (`shared/components/Modal.tsx`) — no modal/dialog/portal component exists anywhere in the codebase today, despite the phase-1 UX spec assuming one does (Architecture AD-13, reality-check). It's built here, as part of delivering this prompt, rather than as a separate foundation story with no fan-observable effect on its own.

## Acceptance Criteria

1. **Given** I have zero Fan Cards, **when** I visit the Standings page, **then** a prompt, rendered via a new shared Modal primitive, invites me to create a Fan Card.
2. **Given** the Modal is open (the prompt, or any future consumer), **when** it renders, **then** it is portal-rendered above page content, with `role="dialog"` and `aria-modal="true"`; pressing Escape or clicking the backdrop closes it; Tab keeps focus trapped within its interactive elements; closing returns focus to the element that triggered it.
3. **Given** this Modal primitive is now the app's sole overlay mechanism, **when** any future feature needs a dialog, **then** it reuses this component — no ad hoc fixed-position divs for modal-like UI elsewhere in the app.
4. **Given** I already hold at least one Fan Card, **when** I visit the Standings page, **then** the prompt does not appear.
5. **Given** the prompt is shown, **when** I choose to proceed, **then** it launches directly into the existing 3-step Fan Card wizard (`FanCardWizard.tsx`), not a separate lightweight picker.
6. **Given** the prompt is shown, **when** I dismiss it ("Not now"), **then** Standings continues to work normally, and the prompt does not reappear for a suppression period (exact duration is a dev-time constant — see Dev Notes, not a fixed number required by this AC).

## Tasks / Subtasks

- [x] Task 1: Build the shared `Modal` primitive (AC 2, 3)
  - [x] New file `frontend/src/shared/components/Modal.tsx`. Props: `{ isOpen: boolean; onClose: () => void; ariaLabel: string; children: React.ReactNode }`. Render `null` when `!isOpen` — no hidden-but-mounted DOM.
  - [x] Portal-render via `createPortal(..., document.body)` — this is the "portal-rendered above page content" requirement (AC 2). No dialog/portal/focus-trap library exists in `package.json` (verified — only `react`/`react-dom`); build it by hand, consistent with the rest of this codebase's fully hand-built component style.
  - [x] Backdrop: a `fixed inset-0` overlay div behind the content panel. Clicking the backdrop itself calls `onClose`; clicking anywhere inside the content panel must NOT close it — implement via an `onClick` on the backdrop element checking `event.target === event.currentTarget` (not by `stopPropagation` on the inner panel, which is more fragile against future nested-click-handler additions).
  - [x] Content panel: `role="dialog"` `aria-modal="true"` `aria-label={ariaLabel}`.
  - [x] Escape key closes: a `keydown` listener (attach in a `useEffect` scoped to `isOpen`, remove on cleanup/close).
  - [x] Focus trap: on open, capture `document.activeElement` as the trigger element to restore later. Query all focusable descendants inside the panel (`a[href], button:not([disabled]), textarea, input, select, [tabindex]:not([tabindex="-1"])`) and move focus to the first one. On `Tab` at the last focusable element, wrap to the first; on `Shift+Tab` at the first, wrap to the last (prevent default and set focus manually in both wrap cases).
  - [x] On close (transition from open → closed, or unmount while open), restore focus to the captured trigger element.
  - [x] This is a generic, content-agnostic primitive — it must not know anything about Fan Cards. Task 2 is the first consumer.
- [x] Task 2: Fan Card prompt content + gating logic (AC 1, 4, 5, 6)
  - [x] New file `frontend/src/features/fan-engagement/FanCardPromptModal.tsx` — self-contained: it reads its own visibility inputs and manages its own open/closed state, so `StandingsPage.tsx` only needs to render `<FanCardPromptModal />` with no props.
  - [x] Zero-cards check: `!hasFanCardPicks(useFanCardStore((s) => s))` — reuse the existing store/helper from `useFanCardStore.ts` unchanged (AC 4). This story does not touch the fan-card storage shape (that's Story 9.3's multi-card migration).
  - [x] Suppression check: use the existing `useLocalStorage` hook (`frontend/src/shared/hooks/useLocalStorage.ts`), NOT Zustand `persist` — this is a lightweight one-off dismissal timestamp, matching how `streakStorage`/`StreakCounter` already use `useLocalStorage` for similar "remember across visits" UI flags in this same feature folder, not full application state. Key: `f1app__fanCardPromptDismissedAt__v1` (follows the established `f1app__{feature}__{version}` naming convention), value: `number | null` (a timestamp, or `null` if never dismissed).
  - [x] Suppression window: define `const PROMPT_SUPPRESSION_MS = 7 * 24 * 60 * 60 * 1000` (7 days) as a local constant with a comment noting the architecture spine leaves the exact duration as an unconstrained dev-time choice (Deferred list) — this is not a value the ACs pin down, so don't treat it as load-bearing.
  - [x] Visible = `!hasFanCardPicks(picks) && (dismissedAt === null || Date.now() - dismissedAt > PROMPT_SUPPRESSION_MS)`. Implemented as `mountedAt - dismissedAt` rather than a live `Date.now()` read in render — see Dev Notes addendum below (purity lint).
  - [x] Prompt body (initial `Modal` content): heading + short invite copy, a primary "Create Fan Card" button and a secondary "Not now" button.
  - [x] "Not now" → `setDismissedAt(Date.now())`, then closes the modal (AC 6). Standings itself is untouched by this — no gating of any other Standings functionality.
  - [x] "Create Fan Card" → swap the Modal's rendered content (local `useState`, e.g. `mode: 'prompt' | 'wizard'`) to mount `<FanCardWizard onDone={handleWizardDone} />` directly inside the same still-open `Modal` — this is "launches directly into the existing 3-step wizard" (AC 5): the real wizard component, not a new picker, and it stays inside the one Modal instance rather than closing and reopening a second dialog.
  - [x] `handleWizardDone`: close the modal, then `navigate('/fan-card')` (`useNavigate` from `react-router-dom` — this is the app's first use of programmatic navigation; grep confirms no prior `useNavigate` call exists, so there's no existing convention to match beyond the library's own API) so the fan lands on their new card, matching the UX flow narrative ("clicks through the wizard... and lands back on the Fan Card page").
  - [x] Do not persist a dismissal timestamp when the wizard completes successfully — only "Not now" writes one. Completing the wizard makes the whole prompt permanently moot anyway (AC 4 already hides it once a card exists), so no suppression bookkeeping is needed on that path.
- [x] Task 3: Wire into Standings (AC 1, 4)
  - [x] `frontend/src/features/standings/StandingsPage.tsx`: add one import and render `<FanCardPromptModal />` once near the top of the returned JSX. `StandingsPage.tsx` otherwise stays as-is — no new state, no prop drilling; the modal component is fully self-governing per Task 2.
  - [x] Export `FanCardPromptModal` from `frontend/src/features/fan-engagement/index.ts` alongside the existing `FanCardPage`/`NewsFeedPage`/`StreakCounter` exports, so `StandingsPage.tsx` imports it the same cross-feature way `router.tsx` already imports `FanCardPage`.
- [x] Task 4: Tests
  - [x] `Modal.test.tsx`: renders nothing when `isOpen={false}`; portal-renders content as a descendant of `document.body` when open; has `role="dialog"`/`aria-modal="true"`; Escape calls `onClose`; clicking the backdrop calls `onClose`; clicking inside the content panel does NOT call `onClose`; `Tab` from the last focusable element wraps to the first; `Shift+Tab` from the first wraps to the last; focus moves into the modal on open; focus returns to the element that had focus before opening, once closed.
  - [x] `FanCardPromptModal.test.tsx`: does not render when `useFanCardStore` already has complete picks; renders when picks are empty and no suppression is active; does not render when a dismissal timestamp is within the suppression window; renders again once past the window (asserted via a pre-set localStorage timestamp offset by `PROMPT_SUPPRESSION_MS`, not a mocked `Date.now()` — see Dev Notes addendum); clicking "Not now" writes a dismissal timestamp and closes the modal; clicking "Create Fan Card" swaps to `FanCardWizard`'s rendered fields; completing the wizard closes the modal, navigates to `/fan-card` (mocked `useNavigate`), and writes no suppression timestamp.
  - [x] `StandingsPage.test.tsx`: added one integration-level test confirming the prompt appears on the page when there are zero Fan Card picks (extends the existing `renderPage()` helper, which already wraps in `MemoryRouter` + `QueryClientProvider` — no new test harness needed).

## Dev Notes

**Read before writing any code:**
- `frontend/src/features/fan-engagement/FanCardWizard.tsx` — takes exactly one prop, `onDone: () => void`, and is currently mounted inline inside `FanCardPage.tsx` (not as an overlay). It is NOT a paged/sequential 3-step UI despite the story name — it's a single screen with three `<select>` fields (driver → constructor → circuit) and one "Save Fan Card" button that calls `onDone()` after persisting all three picks via `useFanCardStore`. "3-step" refers to the three picks, not three sequential screens. Mount it verbatim inside the new `Modal` — do not modify `FanCardWizard.tsx` itself, its `onDone` signature already fits this use perfectly.
- `frontend/src/features/fan-engagement/useFanCardStore.ts` — Zustand `persist` store, key `f1app__fanCard__v1`, single-pick shape (`{ driverId, driverName, constructorName, circuitId, circuitName }`, all nullable), with an exported `hasFanCardPicks(picks)` type-guard helper. This is a **single-card** store today — Story 9.3 (not this story) migrates it to a multi-card array via `persist`'s `version`/`migrate` mechanism. Do not anticipate that shape here; just use `hasFanCardPicks` as-is for the zero-cards check.
- `frontend/src/features/standings/StandingsPage.tsx` — plain component, `useState` tab toggle (`drivers`/`constructors`), no early returns or loading gates blocking the top of the render — a clean, low-risk insertion point.
- `frontend/src/shared/hooks/useLocalStorage.ts` — the app's second (non-Zustand) localStorage pattern: `useLocalStorage<T>(key, defaultValue): [T, (value: T) => void]`, backed by plain `useState` + `try/catch`-guarded `localStorage` reads/writes. Already used by this same feature folder's `streakStorage.ts`/`StreakCounter.tsx` for comparable "remember across visits" UI flags. Use this, not Zustand `persist`, for the dismissal timestamp — a one-off flag doesn't need a dedicated store with actions.
- **Confirmed via full-codebase search: no modal/dialog/portal/backdrop component or CSS pattern exists anywhere today.** `ChampionshipSidebar.tsx`'s mobile "drawer" is not an overlay — it's an inline `hidden`/`block` class toggle with no `fixed` positioning, no backdrop, no portal. There is nothing structural to reuse for `Modal.tsx`; only the color-token vocabulary (`border-border-soft`, `bg-bg-card`, `bg-bg-inset`, `accent-editorial`, `text-text-primary/secondary/tertiary`) is shared across the codebase and should carry through into the Modal's and prompt's styling for visual consistency.
- No focus-trap or dialog library is installed (`package.json` has only `react`/`react-dom` — no `focus-trap`, `@radix-ui/*`, `headlessui`). Architecture AD-13 explicitly calls for a hand-built primitive, consistent with the rest of the app.
- `frontend/src/router.tsx` — `/fan-card` route already exists and renders `FanCardPage`; this story's `navigate('/fan-card')` call after wizard completion reuses that existing route, no router changes needed.
- `react-router-dom` is `^7.17.0` (`createBrowserRouter`). No component anywhere yet calls `useNavigate` — this story is the first. `StandingsPage.test.tsx` already wraps its render in `MemoryRouter`, so the new tests have a harness to extend rather than invent.

### Architecture Compliance — AD-13 (verbatim)

> **AD-13 — First modal/overlay primitive is a shared, hand-built component**
> - **Binds:** FR-10 (Fan Card creation prompt); DESIGN.md's inherited "fully hand-built components, no headless library" rule; any future modal usage
> - **Prevents:** `[ADOPTED — reality check]` EXPERIENCE.md describes FR-10 as reusing an "existing one-level-deep overlay pattern," but no modal/dialog/portal component exists anywhere in the current codebase — `FanCardWizard` currently renders inline inside `FanCardPage`, not as an overlay. Left unaddressed, two devs would each hand-roll a one-off modal for FR-10 with no shared accessibility contract (focus trap, Escape close, focus return, `aria-modal`), directly undermining the WCAG AA gate (AD-12).
> - **Rule:** Add one shared `Modal` primitive to `frontend/src/shared/components/` — portal-rendered, traps focus, closes on `Escape` and backdrop click, returns focus to the trigger element on close, `role="dialog"` `aria-modal="true"`. FR-10's Fan Card prompt is the first consumer: `StandingsPage` renders the prompt via `Modal`, with `FanCardWizard` mounted inside it. This is the only sanctioned overlay mechanism going forward — no ad hoc fixed-position divs for modal-like UI.

Deferred item this story resolves at implementation time (not a fixed AC requirement): *"Fan Card prompt suppression window (FR-10): a dismissal-duration constant, not a structural decision — two devs choosing different N doesn't create incompatibility. Revisit at story-write time."* → resolved above as 7 days, a local constant, easily changed later without any architectural impact.

### Project Structure Notes

- New: `frontend/src/shared/components/Modal.tsx`, `frontend/src/shared/components/Modal.test.tsx`
- New: `frontend/src/features/fan-engagement/FanCardPromptModal.tsx`, `frontend/src/features/fan-engagement/FanCardPromptModal.test.tsx`
- Modified: `frontend/src/features/fan-engagement/index.ts` (add export), `frontend/src/features/standings/StandingsPage.tsx` (one import + one render line), `frontend/src/features/standings/StandingsPage.test.tsx` (one new test)
- No backend changes. No new dependencies (portal via `react-dom`'s existing `createPortal`, already available).

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 9.1]
- [Source: _bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md#AD-13]
- [Source: _bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/EXPERIENCE.md] — Flow 3 (Aisha) narrative walkthrough of this exact prompt-to-wizard-to-card flow; feature/state-pattern tables describing the modal and suppression behavior
- [Source: frontend/src/features/fan-engagement/FanCardWizard.tsx] — mounted as-is inside the new Modal
- [Source: frontend/src/features/fan-engagement/useFanCardStore.ts] — `hasFanCardPicks` zero-cards check
- [Source: frontend/src/shared/hooks/useLocalStorage.ts] — suppression-timestamp storage
- [Source: frontend/src/features/standings/StandingsPage.tsx], [Source: frontend/src/features/standings/StandingsPage.test.tsx] — insertion point and existing test harness (`MemoryRouter` + `QueryClientProvider`)

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

`react-hooks/purity` flagged a direct `Date.now()` read in the render body (the `isOpen` suppression check). Fixed by capturing the mount timestamp once via a lazy `useState(() => Date.now())` initializer — the same sanctioned shape as `useState(() => Math.random())` for one-time impure reads — and deriving `suppressed` from that captured value plus `dismissedAt`, both pure state by the time the comparison runs. A first attempt using `useLayoutEffect` + `setSuppressed` avoided the purity rule but tripped `react-hooks/set-state-in-effect` instead (derived-state-via-effect anti-pattern); the lazy-initializer approach avoids both rules and needs no effect at all.

### Completion Notes List

- All 4 tasks complete. Task 1 (Modal primitive) was already implemented and passing (10/10 tests) at story pickup; Tasks 2-4 implemented this session.
- Full frontend suite: 170/174 passing. The 4 failures are pre-existing, unrelated `dateUtils.test.ts` locale-formatting issues (last touched in Story 1.6) — confirmed via `git status`/`git log` that this file has no relation to this story's changes.
- `npx tsc --noEmit` clean. `eslint` clean on all touched/new files.
- Suppression-window test uses a pre-set localStorage timestamp offset by `PROMPT_SUPPRESSION_MS` rather than mocking global `Date.now()` — simpler, no fake-timer setup needed, and exercises the same code path.

### File List

- New: `frontend/src/features/fan-engagement/FanCardPromptModal.tsx`
- New: `frontend/src/features/fan-engagement/FanCardPromptModal.test.tsx`
- New: `frontend/src/shared/components/Modal.tsx` (Task 1, pre-existing at pickup)
- New: `frontend/src/shared/components/Modal.test.tsx` (Task 1, pre-existing at pickup)
- Modified: `frontend/src/features/fan-engagement/index.ts` (added `FanCardPromptModal` export)
- Modified: `frontend/src/features/standings/StandingsPage.tsx` (mounts `<FanCardPromptModal />`)
- Modified: `frontend/src/features/standings/StandingsPage.test.tsx` (fan-card-store reset + new integration test)
