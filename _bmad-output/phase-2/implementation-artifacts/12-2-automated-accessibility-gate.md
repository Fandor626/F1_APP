---
baseline_commit: f18b9da
---

# Story 12.2: Automated accessibility gate

Status: review

## Story

_This story is framed around the developer, not a fan, because it's the enforcement mechanism for an explicit, numbered PRD success metric (SM-2 / NFR1 — WCAG 2.1 AA, axe/Lighthouse ≥ 95) rather than a fan-facing capability._

As the developer maintaining F1_poc,
I want automated accessibility checks running in CI against the redesigned pages,
so that WCAG AA regressions are caught before they ship, not discovered manually.

## Acceptance Criteria

1. **Given** the existing Playwright E2E suite, **when** `@axe-core/playwright` assertions are added, **then** they run against Calendar, Live Race, Standings, Fan Card, and Race Weekend Detail — the five pages named in the PRD's SM-2 success metric.
2. **Given** these assertions, **when** run in CI, **then** a score below the ≥95 threshold, or any critical/serious axe violation, fails the build.
3. **Given** this gate, **when** wired in, **then** it replaces phase-1's posture of no automated accessibility check with a real enforced floor (AD-12).

## Tasks / Subtasks

- [x] Task 0: Reality check — neither a Playwright suite nor a CI pipeline existed (AC 1, 3)
  - [x] AD-12's rule text assumes "the existing Playwright E2E suite (`playwright/`)" — verified via full-repo search that no `playwright.config.ts`, no `tests`/`e2e` directory, and no `@playwright/test` dependency existed anywhere. The unused `playwright` (not `@playwright/test`) entry in `frontend/package.json`'s devDependencies was dead weight, never wired to any script or test file.
  - [x] Verified no `.github/workflows/` directory existed at all — there was no CI pipeline of any kind (lint/typecheck/unit-test/build), let alone one to "wire into." Both the Playwright harness and a baseline CI pipeline had to be created from scratch for this story's AC to mean anything in practice.
- [x] Task 1: Playwright + axe harness (AC 1)
  - [x] New top-level `playwright/` directory (matching AD-12's named path), with its own `package.json` (`@playwright/test`, `@axe-core/playwright`) — kept separate from `frontend/`'s Vite/Vitest toolchain rather than mixed into it, since it's a different test runner with different lifecycle (browser automation vs. jsdom component tests).
  - [x] `playwright.config.ts`: `webServer` array starts both the real backend (`dotnet run`, port 5000) and the real frontend dev server (`npm run dev`, port 5173) — not a mocked API layer. This matters concretely: Live Race's Story 8.1 fallback-enrichment content (positions/gaps/tyres from a real completed race, via live Ergast/OpenF1 calls) only exists when the backend can actually reach those APIs; a stubbed backend would only ever expose the page's leanest empty/next-race state, defeating the point of accessibility-testing it. CORS for the CI-started backend is granted via an `AllowedOrigins__0` env-var override (ASP.NET Core's `__` config-section-delimiter convention) rather than checking in the gitignored `appsettings.Development.json`.
  - [x] `tests/accessibility.spec.ts`: one test per page — Calendar (`/`), Live Race (`/live`), Standings (`/standings`), Fan Card (`/fan-card`), Race Weekend Detail (`/races/1`) — running `AxeBuilder(...).analyze()` and asserting zero `critical`/`serious`-impact violations.
- [x] Task 2: Reconcile the "≥95" threshold with axe's violation-based output (AC 2)
  - [x] `@axe-core/playwright` produces a violation list, not a Lighthouse-style 0–100 score — confirmed neither the architecture doc, PRD, nor epics.md resolves this mismatch (the PRD flags it as `[ASSUMPTION: exact tooling/threshold]`, and the implementation-readiness report separately flags it as an open risk to confirm before this story). Since AD-12's Rule text names only `@axe-core/playwright` (never Lighthouse) as the CI tool, and epics.md's own AC phrases the two conditions as "score below ≥95, **or** any critical/serious axe violation" (read as overlapping/alternate framings of the same bar, not two separately-computed numbers), the implementation decision made here is: **zero critical/serious axe violations is the enforced floor**, treated as the practical, CI-computable equivalent of the qualitative "≥95" target. This is a genuine implementation decision on an unresolved gap, not a literal AC requirement — flagged here for visibility rather than silently assumed.
- [x] Task 3: Baseline CI pipeline (AC 2, 3)
  - [x] New `.github/workflows/ci.yml` — `frontend` job (lint, `tsc -b`, Vitest), `backend` job (`dotnet test`), and an `accessibility` job (depends on both) that installs Playwright + Chromium, boots the real frontend+backend, and runs the axe suite, uploading the HTML report as a build artifact on every run (pass or fail) for triage.
- [x] Task 4: Fix the real violations the gate found (AC 2, 3)
  - [x] Running the new gate against the actual app immediately surfaced a real, pre-existing WCAG AA failure: `--color-text-tertiary` (`#6b7280`) has only 3.4:1–3.7:1 contrast against the app's dark card/app backgrounds (`#1b1f26`/`#14171c`) — below the 4.5:1 AA minimum for normal text. This affected the sitewide attribution footer (`App.tsx`, visible on all five tested pages) and several Live Race sub-panels (`TrackMap`, `GapList`/`DriverRow`, `FastestSectorBoard`, `LapTimeChart`, `RaceEventTimeline`) that used the same literal hex value directly rather than the CSS token.
  - [x] Fixed by darkening the `--color-text-tertiary` token itself (`#6b7280` → `#8890a0`, verified ≥4.5:1 against every dark background in use — `bg-app`, `bg-card`, `bg-card-hover`, `bg-inset` — via a WCAG relative-luminance calculation, not eyeballed) and updating the handful of raw-hex `text-[#6b7280]` usages in `live-race/` to match, so the fix is consistent everywhere the color appears, not just on the tested pages. Left two non-text usages of the same literal (`tyreUtils.ts`'s fallback tyre-dot color, `RaceEventTimeline.tsx`'s DNF marker `backgroundColor`) untouched — axe's `color-contrast` rule only flags text, and changing a categorical marker color is a different, out-of-scope design decision.
  - [x] After the fix, all five pages pass with zero critical/serious violations, verified by a real local run of the full Playwright suite (not just config/syntax validation) against the actual backend and frontend.

## Dev Notes

- **This story required real infrastructure creation, not just test-writing** — no Playwright suite, no CI pipeline existed. Both were built minimally-but-functionally: the CI workflow covers exactly what's needed for this story's gate to mean something (frontend checks, backend checks, and the accessibility job depending on both), not a broader CI overhaul.
- The accessibility gate is deliberately **not** mocked — it boots the real backend and frontend so the pages under test reflect actual production behavior (including live third-party data dependencies for Ergast/OpenF1/RSS). This is the most accurate approach but means CI runs are dependent on those services' availability/latency; a future story could introduce a deterministic mock layer for the accessibility job specifically if this proves flaky, but that's out of this story's scope.
- `AxeBuilder(...).analyze()` is called with axe's default rule set (WCAG 2.0/2.1 A/AA rules) — no rules were disabled or configured away to make tests pass; every violation found was fixed at the source.
- Confirmed via `git stash`-style comparison that the frontend/backend unit test suites are unaffected by the color-token change (204/208 frontend, same 4 pre-existing unrelated failures; backend unaffected since this was a CSS-only change).

### Architecture Compliance — AD-12 (verbatim)

> **AD-12 — Accessibility AA is an automated CI gate, not a manual checklist**
> - **Binds:** PRD §7 Accessibility NFR, SM-2
> - **Prevents:** WCAG AA regressing silently — the PRD's success metric names a Lighthouse/axe threshold, but phase-1 had no automated a11y check in CI
> - **Rule:** Add `@axe-core/playwright` assertions to the existing Playwright E2E suite (`playwright/`), run against the five pages SM-2 names (Calendar, Live Race, Standings, Fan Card, Race Weekend Detail), wired into the existing CI pipeline rather than a separate manual audit step.

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 12.2]
- [Source: _bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md#AD-12]
- [Source: _bmad-output/phase-2/planning-artifacts/prd.md] — SM-2's `[ASSUMPTION: exact tooling/threshold]` flag
- [Source: _bmad-output/phase-2/planning-artifacts/implementation-readiness-report-2026-07-13.md] — flags the same threshold ambiguity as an open risk ahead of this story
- [Source: _bmad-output/phase-2/implementation-artifacts/8-1-guaranteed-non-empty-live-race-page.md] — Live Race's fallback-enrichment behavior, relevant to why this gate needs a real backend

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Completion Notes List

- All 5 tasks complete. Verified with a real, full local run (not a dry/config-only check): `npx playwright test` against the actual `dotnet run` backend and `npm run dev` frontend — 5/5 accessibility tests passing after the contrast fixes.
- Full frontend suite after the color-token change: 204/208 passing — the 4 failures are the same pre-existing, unrelated `dateUtils.test.ts` locale-formatting issues noted throughout this epic. `npx tsc -b` clean; `eslint` clean on every file this story touched (one pre-existing, unrelated `TrackMap.tsx` `set-state-in-effect` error remains, not introduced by this story).
- The accessibility gate immediately found and this story fixed a real, previously-unnoticed WCAG AA violation (insufficient text contrast) affecting every page in the app, not just the five under test — a concrete demonstration of AC 3's "real enforced floor."

### File List

- New: `playwright/package.json`, `playwright/package-lock.json`
- New: `playwright/playwright.config.ts`
- New: `playwright/tsconfig.json`
- New: `playwright/.gitignore`
- New: `playwright/tests/accessibility.spec.ts`
- New: `.github/workflows/ci.yml`
- Modified: `frontend/src/index.css` (`--color-text-tertiary` contrast fix)
- Modified: `frontend/src/App.tsx` (footer text color)
- Modified: `frontend/src/features/live-race/FastestSectorBoard/FastestSectorBoard.tsx`
- Modified: `frontend/src/features/live-race/GapList/DriverRow.tsx`
- Modified: `frontend/src/features/live-race/GapList/GapList.tsx`
- Modified: `frontend/src/features/live-race/LapTimeChart/LapTimeChart.tsx`
- Modified: `frontend/src/features/live-race/RaceEventTimeline/RaceEventTimeline.tsx`
- Modified: `frontend/src/features/live-race/TrackMap/TrackMap.tsx`
