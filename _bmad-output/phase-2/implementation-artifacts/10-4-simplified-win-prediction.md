---
baseline_commit: d070278
---

# Story 10.4: Simplified win prediction

Status: review

## Story

As a fan checking a Race Weekend the evening before qualifying or after it,
I want a plain-language read on who's likely to win and why,
so that I understand the stakes without parsing a probability table.

## Acceptance Criteria

1. **Given** qualifying results are available for this Race Weekend (existing MVP win-probability calculation), **when** I open its Race Weekend Detail page, **then** a Win Prediction callout names a likely winner and gives at least one concrete plain-language reason — no percentages shown by default.
2. **Given** the Win Prediction callout, **when** rendered, **then** it uses the `accent-editorial` bordered-card treatment (UX-DR7, UX-DR15) — the first non-chrome use of that token, never to be reused for any team-scoped or broadcast-coded value.
3. **Given** the callout, **when** I activate its toggle (`aria-expanded`/`aria-controls`), **then** it expands to reveal the original MVP raw per-grid-slot win-probability table beneath a dashed divider — collapsed by default.
4. **Given** no qualifying session has run yet for this Race Weekend, **when** I open its Race Weekend Detail page, **then** the Win Prediction callout is simply absent, not a placeholder or error.

## Tasks / Subtasks

- [x] Task 1: `WinPredictionCallout` component (AC 1, 2, 3, 4)
  - [x] New `frontend/src/features/calendar/WinPredictionCallout.tsx`. Finds the top pick by sorting entries by `winProbability` descending — the backend's `/api/races/{round}/win-probability` sorts by grid position, not probability, so `entries[0]` cannot be assumed to be the likely winner.
  - [x] Plain-language sentence: "Most likely to win: **{name}**. {reason}." — no percentage anywhere in the default (collapsed) view.
  - [x] Reason text (`buildReason`) is grounded only in fields the endpoint actually returns (`gridPosition`, `winProbability`) — no pole-history or practice-pace data exists server-side to draw on, unlike the UX mockup's illustrative copy. Pole-position start gets a dedicated sentence; otherwise references grid position, with an added "clear edge" qualifier when the probability gap to 2nd place is ≥10 points.
  - [x] Styling: `rounded-lg border border-accent-editorial bg-bg-card`, matching `SeasonWrappedCard.tsx`'s existing bordered-card Tailwind pattern (the closest prior precedent, though that one doesn't use the token as a callout for opinionated/predictive content — confirmed via full-codebase `accent-editorial` grep that every other usage today is hover/focus/link/toggle-active chrome, never a content callout, satisfying AC 2's "first non-chrome use" claim).
  - [x] Toggle: `<button aria-expanded={isExpanded} aria-controls="win-prediction-raw-table">`, modeled on `ChampionshipSidebar.tsx`'s existing button+aria-expanded+aria-controls+id pattern (the only structural precedent for this interaction in the codebase) rather than the `Modal` primitive — this reveals inline content, not an overlay, so `Modal` doesn't apply.
  - [x] Reveals the original `WinProbabilityWidget` (kept unmodified, not deleted — matches the UX memlog's explicit "MVP's original component, kept" decision) beneath a `border-t border-dashed` divider, collapsed by default (`useState(false)`).
  - [x] `entries.length === 0` renders `null` — this is Story 10.4's AC 4; the existing call site's `winProbs.length > 0` guard in `RaceWeekendDetailView.tsx` already covers the same condition, so this is a defensive double-check, not new logic.
- [x] Task 2: Wire into Race Weekend Detail (AC 1–4)
  - [x] `RaceWeekendDetailView.tsx`: swapped the `WinProbabilityWidget` import/render for `WinPredictionCallout` at the same call site (last section on the page, after `ContextualData`). No other changes to the page.
- [x] Task 3: Tests
  - [x] New `WinPredictionCallout.test.tsx`: renders nothing for zero entries; picks the highest-probability driver even when it isn't the first array element (out-of-probability-order fixture, mirroring the real backend's grid-position sort); pole-position reason text for a P1 top pick; no `%` text anywhere by default; toggle starts `aria-expanded="false"`, flips to `"true"` and reveals the raw percentage table on click.
  - [x] `RaceWeekendDetailView.test.tsx`: new integration test overriding the win-probability mock handler with real entries, asserting the callout's plain-language text and toggle-gated raw table (scoped via a new `data-testid="win-prediction-callout"` to disambiguate "Max Verstappen," which also appears as plain text in the page's Championship Gap section); a second test confirms the callout is absent for the existing empty-array default mock (AC 4).

## Dev Notes

- The UX mockup's illustrative prediction copy ("starts from pole here two of the last three years... practice session pace") draws on data the real `/api/races/{round}/win-probability` endpoint does not expose (no pole-history, no practice-pace fields on `WinProbabilityEntry`) — the actual reason text here is deliberately more conservative, grounded only in `gridPosition`/`winProbability`, rather than fabricating unavailable context.
- `WinProbabilityWidget.tsx` is unchanged and still exported/used — this story wraps it, per the UX memlog's explicit instruction not to delete it.
- Confirmed via full-codebase grep that `accent-editorial` had no prior content-callout usage — every existing usage is chrome (hover/focus/link/toggle-active states, borders on unrelated cards). This callout is the first place it anchors a "product opinion" surface, per DESIGN.md's explicit note about this being a deliberate, non-scope-creep expansion of the token's role.

### Architecture / UX-DR references (verbatim from epics.md / DESIGN.md)

> **UX-DR7 / UX-DR15 (`prediction-callout`):** `background: bg-card`, `border: accent-editorial`, `radius: rounded-lg`, `label: accent-editorial` — "new non-team use of accent-editorial — the product-voice color applied to a content callout, not just live/link/focus chrome." Toggle "extends inline-link treatment for affordance, but rendered as a bordered button"; `revealsRawTable: true`.
>
> Do's/Don'ts: "Use `accent-editorial` for the Win Prediction callout — it's the product's own voice/opinion." / "Don't use `accent-editorial` (or any callout treatment resembling it) for a team-scoped or broadcast-coded value."

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 10.4]
- [Source: _bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/DESIGN.md] — `prediction-callout` token spec, Brand & Style narrative on the `accent-editorial` expansion
- [Source: backend/F1App.Api/Services/WinProbabilityService.cs] — confirms grid-position sort order and the absence of any server-computed "reason" field
- [Source: frontend/src/features/standings/SeasonWrappedCard.tsx] — closest existing `accent-editorial`-bordered-card Tailwind pattern
- [Source: frontend/src/features/standings/ChampionshipSidebar.tsx] — `aria-expanded`/`aria-controls` toggle pattern reused

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Completion Notes List

- All 3 tasks complete. Full frontend suite: 200/204 passing — the 4 failures are the same pre-existing, unrelated `dateUtils.test.ts` locale-formatting issues noted throughout Epic 9/10's stories.
- `npx tsc -b` clean, `eslint` clean on all touched files.
- No backend changes needed — this story is presentation-only, reusing the existing `/api/races/{round}/win-probability` endpoint as-is.

### File List

- New: `frontend/src/features/calendar/WinPredictionCallout.tsx`
- New: `frontend/src/features/calendar/WinPredictionCallout.test.tsx`
- Modified: `frontend/src/features/calendar/RaceWeekendDetailView.tsx`
- Modified: `frontend/src/features/calendar/RaceWeekendDetailView.test.tsx`
