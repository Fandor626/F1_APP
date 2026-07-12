# Review: Version / Reality-Check Audit — ARCHITECTURE-SPINE.md (F1_poc Phase 2)

**Reviewed file:** `_bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md`
**Review date:** 2026-07-12
**Lens:** Verify every committed technology/version decision was web-researched or reality-checked rather than asserted from training data.

---

## 1. `@axe-core/playwright` (AD-12, new stack addition)

**Verdict: Correct, current, and appropriately hedged. No changes needed.**

- The package name `@axe-core/playwright` is still the correct, actively maintained package for wiring axe-core accessibility assertions into a Playwright suite. It is published by Deque Labs under the `dequelabs/axe-core-npm` monorepo.
- Current published version as of review date is **4.12.1**, with a release cadence showing activity within the last ~1-2 weeks — the package is actively maintained, not abandoned.
- The package has **not** been renamed or superseded. Playwright's own official docs (`playwright.dev/docs/accessibility-testing`) continue to recommend `@axe-core/playwright` as the standard approach for automated a11y assertions in Playwright suites as of 2026. It remains the "lowest-friction stack" for this per multiple 2026 QA-focused sources.
- Note: this package does **not** follow strict SemVer — its major.minor tracks the bundled axe-core engine version (e.g. `4.12.x` bundles axe-core `4.12.x`), with patch releases carrying fixes/features rather than breaking changes. This nuance isn't stated in the spine, but it doesn't invalidate anything committed there.
- The spine's version pin, `^4.10 (verify current at implementation time)`, is good practice: it commits to a real floor version that exists and is compatible, while explicitly flagging it for a freshness check at implementation time rather than asserting a hard pin as if verified-forever. This is exactly the right posture for a fast-moving dev-tooling dependency and requires no correction.
- No evidence of a rename/successor package (e.g. no indication `@axe-core/playwright` has been folded into something like `@axe-devtools/playwright`, which is a distinct, separate Deque commercial product — not a successor).

**Sources checked:** npm registry search results, `dequelabs/axe-core-npm` GitHub README (playwright package), Playwright official accessibility-testing docs, multiple independent 2026 QA blog posts on Playwright+axe.

---

## 2. Other named technologies / versions in the spine — asserted vs. verified

Scope note per instructions: `f1db/f1db` and its `CC-BY-4.0` licensing (AD-5/AD-7) were sourced by an earlier UX step, not this architecture review. Did a light sanity check anyway (see below) since it's cheap and load-bearing; treated as already-verified otherwise.

**No findings requiring correction.** Specifics checked:

- **`f1db/f1db` + CC-BY-4.0` (AD-5, AD-7):** Sanity-checked only, per scope. `github.com/f1db/f1db` exists, is an active "Open Source Formula 1 Database" project, and is licensed CC-BY-4.0. Consistent with the spine's claims. No red flags.
- **`f1db` layout-suffix naming** (`monza-7` vs `monza-1`, AD-5): Not independently verified (would require inspecting the actual f1db data files) — this is a data-sourcing/UX-layer detail, not an architecture-layer claim, and is out of this review's scope per the task framing. Flagging only so it isn't silently assumed verified by this review.
- **`crypto.randomUUID()`** (Consistency Conventions, Fan Card `cardId`): a long-stable, still-current Web API (baseline-available across all evergreen browsers for years). Not a risk.
- No other new package names, version numbers, or "as of" technology claims appear in the spine beyond the one stack addition — everything else in the Stack section is explicitly marked as unchanged/inherited from phase-1, which is the correct move (it doesn't re-assert or re-date phase-1's decisions, it just points at them).

**One process observation (not a defect):** The spine's own AD-6 models the right instinct — it explicitly calls out `[ADOPTED — reality check]` for a spot-checked-against-actual-code claim (the `TrackMap.tsx` base-URL bug). The `@axe-core/playwright` version note (`verify current at implementation time`) has the same self-aware, non-asserted quality. Nothing else in the document makes a specific version/technology claim that reads as confidently asserted-from-training-data without a hedge or without being scoped as "inherited, out of scope for this pass."

---

## 3. Spot-check of inherited phase-1 stack (React 19, ASP.NET Core 10, TanStack Query v5, etc.)

Scope note: not a full re-verification of phase-1 — spot-checks only, per instructions.

**No blocking findings. Two minor staleness notes worth flagging to the team (informational, not spine defects):**

- **ASP.NET Core 10 — confirmed current and correctly chosen.** .NET 10 (and ASP.NET Core 10) released November 11, 2025, as an LTS release supported through November 2028. As of today (2026-07-12) this is the current, correct LTS choice — no concern.
- **TanStack Query v5 — confirmed current.** Still on major version 5 as of mid-2026 (latest patch ~5.101.x, released within the last few weeks of the review date). No v6 exists yet. The spine's inherited pin is accurate.
- **React 19 — confirmed current.** Still the latest major (19.x, patched through mid-2026); no React 20 has shipped. Inherited pin is accurate.
- **React Router v7 — minor staleness flag.** **React Router v8 was released in June 2026** (~1 month before this review), starting a new yearly major-release cadence. The v7→v8 upgrade is described by the React Router team as low-friction ("breaking changes are quite minimal... changes you can make in v7 first"), but it does include at least one relevant breaking change: **`react-router-dom` has been dropped as a package**, with imports moving to `react-router` / `react-router/dom`. This means any new phase-2 code written against the phase-1-inherited "React Router v7" pin should double check whether the installed dependency is still v7 (fine) or has already been bumped to v8 in `package.json` (in which case `react-router-dom` imports in any new phase-2 code would break). This is inherited from phase-1 and out of this spine's own decision scope, but since the spine restates "React Router v7" as a currently-accurate inherited fact rather than flagging it as time-sensitive, it's worth a one-line heads-up to whoever writes the first phase-2 story touching routing.
- **Vite — no version pinned in the spine, so no defect, but noting for awareness:** Vite 8.0 shipped March 2026 (Rolldown-based, current major as of review date). The spine (and presumably phase-1) don't pin a specific Vite major, so there's nothing to correct here.
- **Tailwind v4, `@microsoft/signalr`, `html-to-image`, `zod`, `CodeHollow.FeedReader`, `xunit`/`Moq`, `WireMock.Net`:** not independently re-verified — no red flags surfaced during research, and per task scope these don't warrant a deep dive unless something looked off. Nothing did.

---

## Summary

| # | Item | Verdict |
| --- | --- | --- |
| 1 | `@axe-core/playwright` package name/version/currency | Verified correct — current, maintained, still the recommended approach. Spine's hedge ("verify current at implementation time") is appropriate and needs no change. |
| 2 | Other named tech/versions in the spine | No mis-assertions found. `f1db/f1db` CC-BY-4.0 sanity-checked and consistent. |
| 3 | Inherited phase-1 stack spot-check | React 19, ASP.NET Core 10, TanStack Query v5 all confirmed current as of 2026-07-12. React Router v8 shipped ~1 month ago (v7→v8 mostly non-breaking but drops `react-router-dom` as a package) — worth a heads-up note, not a spine defect. |

**Overall: the spine passes the reality-check lens.** The one new dependency it introduces is correctly identified, current, and appropriately hedged rather than confidently over-asserted. No corrections to the spine are required. The single actionable item is informational (React Router v8 exists now) and belongs in implementation-time verification, not a spine rewrite — consistent with how the spine itself already treats the axe-core version.
