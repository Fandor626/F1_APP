---
stepsCompleted: [1, 2, 3, 4, 5, 6]
status: complete
documentsUsed:
  prd: '_bmad-output/planning-artifacts/prds/prd-F1_poc-2026-06-15/prd.md'
  prdAddendum: '_bmad-output/planning-artifacts/prds/prd-F1_poc-2026-06-15/addendum.md'
  architecture: '_bmad-output/planning-artifacts/architecture.md'
  epics: '_bmad-output/planning-artifacts/epics.md'
  uxDesign: '_bmad-output/planning-artifacts/ux-designs/ux-F1_poc-2026-06-16/DESIGN.md'
  uxExperience: '_bmad-output/planning-artifacts/ux-designs/ux-F1_poc-2026-06-16/EXPERIENCE.md'
---

# Implementation Readiness Assessment Report

**Date:** 2026-06-16
**Project:** F1_poc

## Document Discovery

**PRD — Whole document**
- `prds/prd-F1_poc-2026-06-15/prd.md` (companion: `addendum.md` — stack/deployment decisions, not a duplicate PRD)

**Architecture — Whole document**
- `architecture.md`

**Epics & Stories — Whole document**
- `epics.md`

**UX Design — Spine pair**
- `ux-designs/ux-F1_poc-2026-06-16/DESIGN.md`
- `ux-designs/ux-F1_poc-2026-06-16/EXPERIENCE.md`
- (4 mockups under `ux-designs/ux-F1_poc-2026-06-16/mockups/`, referenced from EXPERIENCE.md)

**Issues found:** None. No duplicate whole+sharded versions for any document type. All four required document types present.

## PRD Analysis

### Functional Requirements

FR-1: Season race list — User can view all Race Weekends in the current season as a chronological scrollable list; the next upcoming Race Weekend card is visually distinguished. Past Race Weekends remain visible below it.

FR-2: Race Weekend card — Each card displays circuit name, country flag, weekend date range, main race date/time, top 3 Drivers' Championship standings with points, and top 3 Constructors with points.

FR-3: Race Weekend detail view — Clicking a card opens a detail view listing every Session in order (FP1/FP2/FP3 or Sprint Shootout/Sprint, Qualifying, Race) with date and time; sprint weekends are correctly differentiated from standard weekends.

FR-4: Contextual detail data — Detail view displays last year's race winner at this Circuit (driver, team, time/gap) and the current Championship Delta between the top two Drivers. Omitted/labelled "First race at this circuit" when no prior result exists.

FR-5: Timezone toggle — A toggle switches all Session times between Track Time and Local Time (browser-detected); default is Local Time; all times update immediately on toggle.

FR-6: Pre-race win probability widget — After Qualifying, detail view shows each Driver's grid position with a calculated win probability (%) derived from historical Ergast data (win rate from grid slot at this circuit, championship standing, weather where available). Absent before qualifying; probabilities sum to ~100%.

FR-7: Animated live track map — SVG circuit layout renders driver dots positioned via OpenF1 real-time x/y coordinates with smooth interpolation between updates so dots glide rather than jump; each dot shows racing number and is coloured by team; on-screen within 10 seconds of session start.

FR-8: Mini-sector colour coding — Driver dots colour-coded by current mini-sector status (purple = fastest overall, green = personal best, yellow = normal pace, white = in/out-lap); colour updates each completed mini-sector.

FR-9: Live gap list — All drivers shown in current race order with real-time Gaps to the car ahead, updated each lap; gaps under 1 second are highlighted as active battles.

FR-10: Live tyre tracker — Each gap list entry shows the driver's current Tyre Compound (colour-coded circle) and number of laps on the current Stint, updating on each tyre data refresh.

FR-11: Pit window estimator — An indicator activates on a driver's entry when their current Stint lap count enters the historically typical pit window for their compound at this Circuit (derived from Ergast historical pit data); deactivates after the driver pits.

FR-12: Live lap time chart — Line chart plots each Driver's lap time per completed lap; pit-out laps appear as visible spikes; hovering a point shows exact lap time and gap to the race's fastest lap.

FR-13: Fastest Sector board — Panel displays current fastest S1/S2/S3 times with the Driver holding each (highlighted purple), updated live as records are broken.

FR-14: Race event timeline — Horizontal timeline with lap number on the X-axis and markers for Safety Car, Virtual Safety Car, pit stops (per driver), DNFs, fastest lap, and red flags. Grows lap by lap live; becomes a static browsable archive after the session ends.

FR-15: Live championship impact tracker — Each driver entry shows a live Championship Delta annotation: how their current race position would change their points gap to their nearest rival if the race ended now. Updates each lap; clearly labelled as "if race ended now" vs. official standings.

FR-16: Fallback to last race — When no live Session is in progress, the live race page shows the most recently completed race in a static/replay view with all the same UI components populated from historical data, clearly labelled as a past race. Page never shows an empty/error state outside of actual API failures.

FR-17: Standings page with toggle — Drivers' Championship and Constructors' Championship presented as two instantly-switchable tabs (no reload). Driver standings show position, name, nationality flag, Constructor, points, wins. Constructor standings show position, name, nationality flag, points, wins.

FR-18: Championship trajectory chart — Multi-line chart of cumulative points per Driver across all completed race rounds (X = round number, Y = total points); hovering a point shows race name, result position, and points scored that round. Only completed rounds are plotted.

FR-19: F1 Season Wrapped — After the final race of the season, the standings page surfaces a shareable Season Wrapped section: most dramatic race (largest position swings), driver with most DNFs, biggest points comeback, most positions gained in a single race, and most-improved Constructor. Calculated on demand from Ergast full-season data; exportable as a client-side-generated image; only visible after the season's final race.

FR-20: Circuit profile page — Displays SVG track layout, all-time lap record (driver, team, year), list of all past race winners at this circuit (year, team), and circuit stats (length, corners, DRS zones, year of first F1 race), all sourced from Ergast historical records. Historical name variants of the same physical track are grouped under one entry.

FR-21: Driver career profile page — Displays career totals (races, wins, podiums, poles, fastest laps, championship titles), Constructor history year by year, and a career cumulative points progression chart (same visual style as FR-18), sourced from Ergast.

FR-22: Driver head-to-head comparison — User selects two Drivers from a searchable dropdown with optional, additive season/Circuit filters; returns a side-by-side stat card (qualifying avg position, race finish avg, DNF count, points scored, fastest laps, wins) within the filtered scope; no filter = all-time across the full Ergast dataset.

FR-23: F1 news feed — Aggregates headlines from public F1 RSS feeds (Formula1.com, Autosport, RaceFans) as a card list (title, source, timestamp); clicking opens the article in a new tab. Fetched and cached by the C# backend to avoid browser CORS issues; refreshes on a configurable interval (default 15 minutes); shows a clear "no news available" state if all feeds are unavailable.

FR-24: Race weekend streak — A counter on the calendar page shows how many consecutive Race Weekends the user has visited the app during a live Session, stored in browser localStorage. Resets if a Race Weekend passes without a live-session visit. No account or backend required; local to the browser only.

FR-25: My F1 Fan Card — A one-time setup wizard lets the user pick a favourite Driver, Constructor, and Circuit; the app generates a styled card showing those picks plus current season stats. Stored in localStorage, re-accessible to change picks, and exportable as a shareable image (client-side generation, no personal data in the export).

Total FRs: 25

### Non-Functional Requirements

NFR-1: The live race page must load and display correct driver positions within 10 seconds of a session starting. (Performance)

NFR-2: Live car movement must be smoothly interpolated client-side between OpenF1 coordinate updates, assuming an update frequency of at least 1/sec. (Performance / UX)

NFR-3: No backend database for the POC — all persistence is browser localStorage. (Architecture constraint)

NFR-4: All data must come from free, public, no-auth-required APIs only (Ergast, OpenF1, RSS feeds). (Constraint)

NFR-5: Responsive web layout — desktop is the primary target; mobile must remain usable. (Usability)

NFR-6: Fan Card and Season Wrapped share images must be generated client-side — no server-side image rendering. (Constraint)

NFR-7: localStorage data must use versioned keys with migration logic from day one to prevent silent corruption across deploys. (Reliability)

NFR-8: The Ergast base URL must be configurable from day one so migration to the Jolpica API (if Ergast is discontinued) requires zero code refactor. (Maintainability)

NFR-9: The SignalR client must use indefinite retry with exponential backoff (ceiling ~60s) to recover from Render free-tier sleep/wake cycles. (Reliability)

NFR-10: OpenF1 endpoint data must be joined using a configurable timestamp tolerance window (default 500ms) to absorb cross-endpoint clock skew (observed 200-800ms). (Data integrity)

NFR-11: `DateTimeOffset` must be used throughout the C# backend; `DateTime` is banned, to prevent silent event-ordering corruption across timezone boundaries. (Correctness)

NFR-12: Degraded-mode behaviour when OpenF1 is unavailable must follow an explicit state machine (live → stale → fallback-to-last-race) with defined entry triggers and a debounced recovery path — not an implicit check. (Reliability)

NFR-13: The news feed refresh interval must be configurable, defaulting to every 15 minutes. (Performance)

NFR-14: No tracking of page views, DAU, or engagement scores — this is a hobby project. (Privacy / Non-goal)

Total NFRs: 14

### Additional Requirements

- Non-Users (POC, §2.2): professional analysts needing certified telemetry, users expecting official broadcast video/audio, users requiring an account/personalisation beyond local storage — explicitly out of scope.
- Non-Goals (§5): auth/accounts, push notifications (highest-priority post-POC unlock per PM note), live video/audio, official licensed assets, native mobile app, betting/fantasy/wagering, social features, race prediction game (deferred), weather forecast (deferred), team radio audio (deferred), DRS indicator on track map.
- MVP Scope (§6): all 5 pages in scope; FR-1–FR-25 in scope; localStorage-only persistence; Ergast + OpenF1 only; browser timezone detection only; responsive web (desktop primary); C# backend as proxy/aggregator.
- Deployment Plan (§6.3): POC is local-dev only; post-POC target is React→Vercel/Netlify, C#→Render/Railway (addendum.md).
- Open Questions (§8): OpenF1 coordinate→SVG mapping approach; which RSS feeds are CORS-safe/key-free; on-demand vs. pre-generated Season Wrapped; post-POC auth migration scope for streak/fan card.
- Assumptions Index (§9): A-1 browser timezone detection sufficiency (FR-5); A-2 OpenF1 ≥1/sec update frequency (FR-7); A-3 OpenF1 coordinates normalisable to SVG space (FR-7); A-4 Ergast round count drives final-race detection (FR-19); A-5 OpenF1 single-stream/parallel low-latency endpoints for tyre/sector/race-control/gap data (§4.2).
- Addendum stack decisions: React (SPA) frontend, C# ASP.NET Core backend (proxy/aggregator/RSS fetcher/win-probability calculator), Ergast (historical) + OpenF1 (live) as sole data sources, browser localStorage only — no backend DB for POC.

### PRD Completeness Assessment

PRD is well-structured and complete for POC scope: vision, target users with 3 named user journeys (UJ-1–UJ-3), a glossary disambiguating domain terms, 25 globally-numbered FRs grouped by page/feature with explicit "Consequences" per FR, explicit non-goals and MVP scope boundaries, success metrics, open questions, and an indexed assumptions list cross-referenced to the FRs they affect. Technical/stack decisions are correctly deferred to `addendum.md` rather than mixed into the PRD. No ambiguity found in FR numbering or scope boundaries. Open Questions (§8) are pre-acknowledged unknowns, not gaps in the document itself — architecture.md already resolves three of the four (track-map normalisation, RSS feeds, Season Wrapped generation timing); only the post-POC auth-migration question remains genuinely open, and it is explicitly scoped post-POC.

## Epic Coverage Validation

### Coverage Matrix

| FR Number | PRD Requirement (summary) | Epic Coverage | Status |
|---|---|---|---|
| FR-1 | Season race list | Epic 1, Story 1.2 | ✓ Covered |
| FR-2 | Race Weekend card w/ top-3 standings | Epic 1, Story 1.3 | ✓ Covered |
| FR-3 | Race Weekend detail view (sessions) | Epic 1, Story 1.4 | ✓ Covered |
| FR-4 | Contextual detail data (winner, delta) | Epic 1, Story 1.5 | ✓ Covered |
| FR-5 | Timezone toggle | Epic 1, Story 1.6 | ✓ Covered |
| FR-6 | Win probability widget | Epic 1, Story 1.7 | ✓ Covered |
| FR-7 | Animated live track map | Epic 3, Story 3.1 | ✓ Covered |
| FR-8 | Mini-sector colour coding | Epic 3, Story 3.2 | ✓ Covered |
| FR-9 | Live gap list | Epic 2, Story 2.1 | ✓ Covered |
| FR-10 | Live tyre tracker | Epic 2, Story 2.2 | ✓ Covered |
| FR-11 | Pit window estimator | Epic 3, Story 3.3 | ✓ Covered |
| FR-12 | Live lap time chart | Epic 2, Story 2.3 | ✓ Covered |
| FR-13 | Fastest Sector board | Epic 3, Story 3.4 | ✓ Covered |
| FR-14 | Race event timeline | Epic 3, Story 3.5 | ✓ Covered |
| FR-15 | Live championship impact tracker | Epic 2, Story 2.4 | ✓ Covered |
| FR-16 | Fallback to last race | Epic 2, Story 2.5 | ✓ Covered |
| FR-17 | Standings page with toggle | Epic 4, Story 4.1 | ✓ Covered |
| FR-18 | Championship trajectory chart | Epic 4, Story 4.2 | ✓ Covered |
| FR-19 | F1 Season Wrapped | Epic 4, Story 4.3 | ✓ Covered |
| FR-20 | Circuit profile page | Epic 5, Story 5.1 | ✓ Covered |
| FR-21 | Driver career profile page | Epic 5, Story 5.2 | ✓ Covered |
| FR-22 | Driver head-to-head comparison | Epic 5, Story 5.3 | ✓ Covered |
| FR-23 | F1 news feed | Epic 6, Story 6.1 | ✓ Covered |
| FR-24 | Race weekend streak | Epic 6, Story 6.2 | ✓ Covered |
| FR-25 | My F1 Fan Card | Epic 6, Story 6.3 | ✓ Covered |

Epic 1, Story 1.1 (Project Scaffolding & Health Check) carries no FR — correctly so, it's the starter-template/foundation story, not a duplicate or orphaned entry.

### Missing Requirements

None. No critical, high, or any-priority missing FRs found. No FRs appear in epics.md that aren't traceable to a PRD FR number (no scope drift).

### Coverage Statistics

- Total PRD FRs: 25
- FRs covered in epics: 25
- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

**Found.** Spine pair at `ux-designs/ux-F1_poc-2026-06-16/`: `DESIGN.md` (visual identity, status: final) + `EXPERIENCE.md` (IA/behavior/flows, status: final), with 4 supporting mockups (`live-race.html`, `calendar.html`, `standings.html`, `fancard-wizard.html`).

### UX ↔ PRD Alignment

- EXPERIENCE.md's Information Architecture covers all 9 surfaces derived from the PRD's 5 feature groups, each row citing its source FR range — full match, no orphaned or invented surfaces.
- EXPERIENCE.md's 3 Key Flows are the PRD's UJ-1/UJ-2/UJ-3 carried over by name and content (race-day fan, casual fan, history enthusiast), not reinvented — exact alignment.
- UX adds presentation-layer decisions the PRD intentionally left open (dark-by-default theme, top-nav pattern, fully-custom components, the Gap List density treatment) — these fill gaps rather than contradict anything; expected and correctly scoped to the UX layer, not the PRD.
- No UX requirement found that contradicts a PRD non-goal (e.g., no auth-gated UI implied anywhere, consistent with PRD §5).

### UX ↔ Architecture Alignment

- **Strong, traceable match:** EXPERIENCE.md's "stale value" component (dimmed text + `~` prefix) maps directly to architecture's `isStale: true` per-field flag in `RaceStateSnapshot` (architecture's "Partial join behaviour" section) — the UX layer didn't invent this convention, it correctly inherited it from architecture.
- Performance requirements align: EXPERIENCE.md's Key Flow 1 cites the same "within 10 seconds" threshold as architecture's NFR and `CacheWarmupService` design.
- Component approach (fully custom, no headless library) is consistent with architecture's frontend stack, which never names a UI/component library (React + Tailwind + Zustand + TanStack Query only) — UX confirms rather than conflicts with this.
- Race-control `aria-live` announcements (EXPERIENCE.md Accessibility Floor) are implementable against architecture's existing data: `EventTimeline.tsx` already consumes discrete OpenF1 race-control messages, not just the continuous snapshot stream — the data architecture needs for this exists.

### Alignment Issues

None blocking. Two non-blocking notes for implementation-time attention:

1. **Constructor color roster gap.** DESIGN.md explicitly flags only 4 of ~10 current-season constructor colors as decided, recommending a runtime-loaded color mapping (mirroring architecture's `circuit-configs/*.json` pattern). No story/AC in `epics.md` currently creates this asset — recommend adding it as an early task within Epic 2 or Epic 3 (whichever implements `team-chip` first) rather than leaving it implicit.
2. **Mobile Live Race tabs unnamed in architecture's project tree.** EXPERIENCE.md's Responsive & Platform section specifies a tabbed mobile adaptation for Live Race; architecture's file tree doesn't name a specific component for it (e.g., no `LiveRaceMobileTabs.tsx`). Not a conflict — normal granularity difference, naturally falls under `LiveRacePage.tsx` — but worth a one-line architecture addendum if AI dev agents need an explicit file name to target.

### Warnings

None. UX is present, not merely implied, and is well-aligned with both PRD and Architecture.

## Epic Quality Review

Applied rigorously against create-epics-and-stories standards: user-value focus, epic independence, story sizing/dependencies, AC completeness, starter-template and entity-creation timing.

### A. User Value Focus

All 6 epics have a user-centric title and a goal statement describing a user outcome, not a technical milestone. **Epic 1's title ("Project Foundation & Race Calendar") is dual-purpose by design, not a violation** — 6 of its 7 stories are pure user-facing FRs (FR-1–FR-6); only Story 1.1 is scaffolding, and bundling project setup into Epic 1 Story 1 is the explicitly correct pattern per the starter-template special check (architecture names a starter template, so Epic 1 Story 1 must set it up). No epic resembles a pure technical milestone ("Database Setup," "API Development," etc.).

### B. Epic Independence

Verified each epic delivers complete, standalone functionality and does not require a later epic:
- Epic 1 — fully standalone (calendar + scaffolding).
- Epic 2 — Story 2.4's championship delta reads standings data already established in Epic 1, Story 1.3 (not Epic 4's standings *page* — the underlying data fetch predates it). No forward dependency.
- Epic 3 — depends on Epic 2's `RaceStateSnapshot`/pipeline (prior epic, allowed).
- Epic 4 — independent; reuses Epic 1's Ergast standings fetch.
- Epic 5, Story 5.2 — explicitly cites Epic 4, Story 4.2's chart style (prior epic, allowed backward reference).
- Epic 6 — Story 6.2 cites Epic 2's live-session state, Story 6.3 cites Epic 4's standings data (both prior epics, allowed — this is why Fan Engagement is ordered last).

No epic requires a *later* epic to function. No circular dependencies found.

### C. Story Sizing, Dependencies, and AC Quality

- No forward dependencies found in any of the 25 stories — every cross-reference points to a same-or-earlier story/epic.
- No "technical-only" stories (e.g., no "set up all models" or equivalent) outside the one sanctioned scaffolding story (1.1), which itself has concrete, testable ACs (dev server reachable, CORS verified end-to-end, JSON casing configured).
- AC format is consistent Given/When/Then across all 25 stories; criteria are specific and measurable (e.g., "within 10 seconds," "probabilities sum to ~100%," "3-5 consecutive valid responses") — no vague criteria like "user can view standings" found.
- Error/edge conditions are well covered: Ergast-unavailable (1.2), stale/degraded data (2.1, 2.5), uncalibrated circuit (3.1), in-progress season (4.3), all-feeds-down (6.1), no-prior-circuit-result (1.5), pre-qualifying absence (1.7).

**Minor concern:** Story 5.3 (Head-to-Head) doesn't restate an Ergast-unavailable error condition inline in its own AC, unlike most other data-dependent stories. Not a gap in practice — EXPERIENCE.md's State Patterns table already covers "Ergast unavailable" for the Head-to-Head surface — but worth adding explicitly to the story for self-containment.

### D. Database/Entity Creation Timing

Not applicable in the traditional sense — there is no backend database for this POC (NFR-3: localStorage + `IMemoryCache` only). No violation possible; confirmed no story creates persistence structures prematurely. `RaceStateSnapshot`/`DriverState` (the closest equivalent to entities) are introduced in Epic 2, Story 2.1 — exactly when first needed, not upfront.

### E. Starter Template & Greenfield Checks

- Architecture names starter templates (Vite + React 19 + TS; ASP.NET Core 10) → Epic 1, Story 1.1 is the required "set up initial project" story, including scaffolding commands, dependency install, and initial configuration (CORS, JSON casing). **Compliant.**
- Greenfield indicators present: initial project setup story (1.1), dev-environment configuration (ports, CORS, env vars) — all accounted for. No CI/CD pipeline story exists yet, but none was claimed as in-scope for the POC either (architecture's deployment plan is explicitly post-POC); not a defect for this phase.
- No brownfield indicators apply (no existing system to integrate with).

### Best Practices Compliance Checklist (all 6 epics)

- [x] Epic delivers user value
- [x] Epic can function independently
- [x] Stories appropriately sized
- [x] No forward dependencies
- [x] No premature entity/table creation (N/A — no DB)
- [x] Clear, testable acceptance criteria
- [x] Traceability to FRs maintained (100%, per Epic Coverage Validation above)

### Findings Summary

- 🔴 **Critical Violations:** None.
- 🟠 **Major Issues:** None.
- 🟡 **Minor Concerns:** 2 — (1) Story 5.3 doesn't inline its Ergast-unavailable error condition (covered at the UX-spine level only); (2) the constructor-color-roster and mobile-tabs naming notes carried over from UX Alignment above.

## Summary and Recommendations

### Overall Readiness Status

**READY.**

### Critical Issues Requiring Immediate Action

None. Zero critical and zero major issues were found across document discovery, PRD analysis, epic coverage, UX alignment, and epic quality review.

### Recommended Next Steps

1. Before or during Epic 2/3 implementation, add a small task to create a runtime-loaded constructor-color mapping asset (e.g. `team-colors.json`, mirroring architecture's `circuit-configs/*.json` pattern) — only 4 of ~10 current-season teams have a decided color today, and no story currently owns creating the full set.
2. Optionally add one line to Story 5.3 (Head-to-Head, in `epics.md`) making its Ergast-unavailable behavior explicit inline, for self-containment — the behavior itself is already correctly specified in `EXPERIENCE.md`'s State Patterns table, so this is a documentation-completeness nice-to-have, not a functional gap.
3. Optionally add one line to `architecture.md`'s project tree naming the mobile tabbed-adaptation component for Live Race (e.g. under `LiveRacePage.tsx`), so AI dev agents implementing Epic 2/3 have an explicit target file for the mobile breakpoint behavior already specified in `EXPERIENCE.md`.
4. Proceed to **Sprint Planning** (`bmad-sprint-planning`) — there is nothing blocking it.

### Final Note

This assessment identified 3 minor, non-blocking issues across 2 categories (UX Alignment, Epic Quality Review) — no critical or major issues anywhere, and 100% FR-to-story traceability. All three are optional polish items that can be addressed inline during implementation rather than requiring a return to planning. You may proceed to Sprint Planning as-is, or address the notes above first — either is reasonable given their severity.

---

**Assessed by:** bmad-check-implementation-readiness (BMad Method)
**Date:** 2026-06-16
