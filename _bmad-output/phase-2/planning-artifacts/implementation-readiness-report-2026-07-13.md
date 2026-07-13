---
stepsCompleted: [1, 2, 3, 4, 5, 6]
documentsIncluded:
  - '_bmad-output/phase-2/planning-artifacts/prds/prd-F1_poc-2026-07-11/prd.md'
  - '_bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md'
  - '_bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/DESIGN.md'
  - '_bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/EXPERIENCE.md'
  - '_bmad-output/phase-2/planning-artifacts/epics.md'
---

# Implementation Readiness Assessment Report

**Date:** 2026-07-13
**Project:** F1_poc Phase 2 — UI/UX Improvement Pass

## Document Inventory

### PRD Files Found

**Whole Documents:**
- `prds/prd-F1_poc-2026-07-11/prd.md`

### Architecture Files Found

**Whole Documents:**
- `architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md`

### Epics & Stories Files Found

**Whole Documents:**
- `epics.md`

### UX Design Files Found

**Whole Documents (bmad-ux spine pair):**
- `ux-designs/ux-F1_poc-2026-07-11/DESIGN.md`
- `ux-designs/ux-F1_poc-2026-07-11/EXPERIENCE.md`

## Issues Found

None. One candidate per document type, no whole+sharded duplicates.

## PRD Analysis

### Functional Requirements

FR-1: Upcoming-focused default view. By default, the Calendar page displays the next Race Weekend and all Race Weekends after it; past Race Weekends are not shown. On page load with no filter interaction, zero past Race Weekends are rendered; the next upcoming Race Weekend remains visually distinguished.

FR-2: Race filter control. The user can switch the Calendar's race list between three views: All, Future, and Past. Selecting "Past" shows only completed Race Weekends; "Future" matches the FR-1 default; "All" shows the full season. The selected filter is visually indicated at all times.

FR-3: Persistent Championship Sidebar. The Calendar page displays a persistent sidebar showing current Drivers' Championship standings (driver, points) and Constructors' Championship standings (constructor, points), replacing per-card standings repetition. Sidebar content matches the same live standings data source used by the Standings page, and remains visible/accessible regardless of active filter or scroll position. A third sidebar slot was left open during brainstorming ("maybe something else") — see Open Question 5, unresolved.

FR-4: Redesigned Race Weekend card. Each Race Weekend card is visually larger/more prominent, no longer repeats full championship standings, and additionally displays the circuit's real track outline and fastest-lap context (all-time fastest lap and current/most-recent-year fastest lap at that circuit, each with driver's name). No race card renders driver/constructor standings; every card renders a recognizable track outline; cards for circuits without qualifying historical data omit the fastest-lap block gracefully. Track outline rendering technique is an implementation decision (addendum.md).

FR-5: Guaranteed non-empty Live Race page. When no Session is currently live, the Live Race page always renders the most recently completed Race's full data set (positions, gaps, tyres, sectors, timeline — same components as a live session), clearly labelled as a past race. Never renders an empty/error state when the only issue is "no session currently live" (genuine API failures remain a distinct state). Treated as a regression fix closing the gap between MVP FR-16's original intent and current shipped behavior.

FR-6: Race Replay controls. The user can start, stop, and restart a Race Replay of the fallback race from FR-5. Controls are visible whenever the page is in fallback mode; "Restart" returns playback to lap 1 without a page reload.

FR-7: Replay scrub bar and lap jump. The user can drag a scrub bar or directly select a specific lap to jump the Race Replay to. Jumping to a lap updates all dependent views (positions, gaps, tyres) to that lap's state, not just a visual timeline marker.

FR-8: Replay playback speed control. The user can select a playback speed for the Race Replay (e.g. 1x, 2x, 4x). Changing speed takes effect immediately without restarting playback.

FR-9: Replay pause and resume. The user can pause the Race Replay at any point and resume from exactly that point — not from the start or the live edge.

FR-10: Fan Card creation prompt. Users who have not yet created a Fan Card see a prompt (popup or inline) on the Standings page inviting them to create one. Prompt does not appear for users who already have at least one Fan Card; dismissing the prompt does not block further use of the page (suppression window is a UX-stage decision, left open).

FR-11: Fan Card visual redesign. The Fan Card displays the chosen driver's photo, the driver's autograph/signature, the associated Constructor's team logo, and the team principal's name, in addition to whatever the MVP card already showed. Every supported driver/constructor pairing renders all four new elements without a broken-image or missing-data state under normal conditions. Card remains exportable as a client-side-generated image. Sourcing/licensing of driver photos and autograph assets is an implementation detail (addendum.md).

FR-12: Multiple Fan Cards per user. A user can create and hold more than one Fan Card within the same browser. Creating a new card does not overwrite or delete an existing one; all of a user's cards are viewable/accessible from the Fan Card page.

FR-13: Track layout visualization. The Race Weekend detail page displays the circuit's real track outline/shape — the same visual asset class as FR-4's card-level track outline, shown here at greater size/detail. Circuits without a sourced outline asset degrade gracefully (omitted or generic placeholder), mirroring FR-4's degradation pattern.

FR-14: Track lap record context. The Race Weekend detail page displays the all-time fastest lap at this circuit and the fastest lap of the current (or most recently completed) year at this circuit, each with driver's name. Matches the same underlying record data as FR-4; presented with more detail/prominence.

FR-15: Track historical data and records. The Race Weekend detail page displays additional circuit historical data and records beyond lap times: at minimum, the list of past race winners at this circuit (year, driver, team) and core circuit stats (length, number of corners, DRS zones, year of first F1 race), drawing on the same historical dataset already scoped for MVP's Circuit Profile page (MVP FR-20). Circuits lacking full historical data degrade gracefully (partial display, not error).

FR-16: Simplified win prediction. The Race Weekend detail page presents a Win Prediction in plain language — a likely winner and a short, human-readable rationale — built on top of the existing MVP FR-6 win-probability calculation, without exposing raw percentages or the underlying calculation by default. Prediction text names a driver and gives at least one concrete reason. No percentage figures or statistical methodology shown in this view by default; the relationship between this and MVP's existing raw win-probability display was an open question, resolved by UX as a collapsed-by-default toggle beneath the plain-language callout (see UX EXPERIENCE.md, Open Question 2 resolution).

FR-17: Structured career stats presentation. Driver and Constructor profile pages present career statistics and history in a clearly structured, scannable layout: grouped by category (season-by-season results, career totals, head-to-head where applicable) rather than a single undifferentiated list, with each group individually scannable without horizontal scrolling on a 360px-wide mobile viewport. Underlying data is unchanged from MVP FR-20–FR-22; this FR governs presentation/structure only.

FR-18: News item preview. Each News Feed item displays an associated photo/thumbnail and a short text snippet/summary, in addition to the existing title, source, and publish time. Items whose source feed lacks an image or snippet degrade gracefully (title-only) rather than showing a broken-image placeholder. No new external data source is introduced — depends on confirming existing feed fields carry this data (Open Question 4b, resolved by Architecture AD-11: `CodeHollow.FeedReader` already exposes the needed fields at parse time).

Total FRs: 18

### Non-Functional Requirements

NFR1 (Accessibility, PRD §7): All Phase 2 surfaces target WCAG 2.1 AA: sufficient color contrast for team-colored UI elements, keyboard operability for the Calendar filter (FR-2), the Replay controls (FR-6–FR-9), and any Fan Card creation flow, and screen-reader-appropriate labelling for the track outline visuals (FR-4, FR-13) and Championship Sidebar (FR-3). Success metric SM-2: Calendar, Live Race, Standings, Fan Card, and Race Weekend Detail each score Lighthouse/axe Accessibility ≥ 95.

NFR2 (Mobile / Responsive, PRD §7): Every redesigned surface must be fully usable on common mobile viewport widths, with one-handed operation feasible for the Replay controls in particular. Success metric SM-3: Lighthouse mobile Performance ≥ 85 on the five redesigned pages.

NFR3 (Performance, PRD §7): Added visual richness (track outlines, Fan Card imagery, News Feed thumbnails, Championship Sidebar) must not introduce animation jank or materially regress page load time. Counter-metric SM-C1: Time-to-Interactive regression capped at roughly 20% over current baseline on any redesigned page — a placeholder threshold pending confirmation per the PRD's own Assumptions Index.

NFR4 (Visual Consistency, PRD §7): A single, coherent visual language (color, spacing, typography, iconography) applies across all redesigned pages. Flagged by the PRD itself as inferred rather than explicitly stated (Assumptions Index, §7).

Total NFRs: 4

### Additional Requirements

- **Non-Goals (PRD §5, binding constraints, not gaps):** No user accounts/authentication; no gamification of Fan Cards (no rarity tiers, collection mechanics, trading, badges); no monetization/advertising changes; no new live-data source integrations (Race Replay simulates existing historical data); no new top-level pages/destinations beyond new Fan Card entry points.
- **Counter-metric SM-C2:** New interaction surfaces (filters, replay scrubber, multi-card creation, prompts) must not add extra required steps to the core "see the next race" or "watch the live/last race" journeys.
- **Open Questions carried into implementation (PRD §9):** OQ1 (which existing Race Weekend Detail fields to remove — content audit, not yet resolved), OQ5 (Championship Sidebar third slot — left open), OQ6 (rollout sequencing — assumed incremental/page-by-page, not yet confirmed).
- **Addendum technical decisions:** FR-4/FR-13 track outline sourcing resolved during the UX phase (f1db/f1db, CC-BY-4.0) and carried into Architecture AD-5/6/7. FR-11 Fan Card asset sourcing resolved during the Architecture phase (AD-10: manual curation, no new API).

### PRD Completeness Assessment

The PRD is internally consistent and explicit about what it does *not* resolve — every one of its 6 open questions and 12 inline `[ASSUMPTION]` tags was either resolved downstream (by UX or Architecture, with the resolving decision traceable) or explicitly carried forward as a still-open item. Three genuine open items remain unresolved by any downstream document: **OQ1** (which Race Weekend Detail fields to cut — a content-audit task, not a technical one), **OQ5** (Championship Sidebar third slot), and **OQ6** (rollout sequencing). None of the three blocks story-level implementation — OQ1 and OQ5 are additive/subtractive content decisions that don't change component contracts, and OQ6 is a sequencing choice the Epic List already made unnecessary to resolve (every epic ships independently). Flagged for the Epic Coverage and Story Quality checks below rather than treated as a blocker here.

## Epic Coverage Validation

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Upcoming-focused default Calendar view | Epic 7, Story 7.1 | ✓ Covered |
| FR-2 | Race filter control (All/Future/Past) | Epic 7, Story 7.1 | ✓ Covered |
| FR-3 | Persistent Championship Sidebar | Epic 7, Story 7.2 | ✓ Covered |
| FR-4 | Redesigned Race Weekend card | Epic 7, Story 7.4 (asset foundation: Story 7.3) | ✓ Covered |
| FR-5 | Guaranteed non-empty Live Race page | Epic 8, Story 8.1 | ✓ Covered |
| FR-6 | Race Replay start/stop/restart | Epic 8, Story 8.2 | ✓ Covered |
| FR-7 | Replay scrub bar and lap jump | Epic 8, Story 8.3 | ✓ Covered |
| FR-8 | Replay playback speed control | Epic 8, Story 8.4 | ✓ Covered |
| FR-9 | Replay pause and resume | Epic 8, Story 8.5 | ✓ Covered |
| FR-10 | Fan Card creation prompt | Epic 9, Story 9.2 (foundation: Story 9.1 Modal) | ✓ Covered |
| FR-11 | Fan Card visual redesign | Epic 9, Story 9.3 | ✓ Covered |
| FR-12 | Multiple Fan Cards per user | Epic 9, Story 9.4 | ✓ Covered |
| FR-13 | Track layout visualization | Epic 10, Story 10.1 | ✓ Covered |
| FR-14 | Track lap record context | Epic 10, Story 10.2 | ✓ Covered |
| FR-15 | Track historical data and records | Epic 10, Story 10.3 | ✓ Covered |
| FR-16 | Simplified win prediction | Epic 10, Story 10.4 | ✓ Covered |
| FR-17 | Structured career stats presentation | Epic 11, Story 11.1 | ✓ Covered |
| FR-18 | News item preview | Epic 12, Story 12.1 | ✓ Covered |

NFR1 (Accessibility) is additionally covered by a dedicated story: Epic 12, Story 12.2 (Automated accessibility gate). NFR2–NFR4 are cross-cutting quality bars embedded as acceptance criteria within individual stories (e.g. Story 8.4's mobile one-handed AC for NFR2) rather than standalone stories — consistent with the epics document's own framing.

### Missing Requirements

None. All 18 PRD FRs have at least one story with acceptance criteria addressing them. No FRs appear in the epics document that aren't traceable to the PRD.

### Coverage Statistics

- Total PRD FRs: 18
- FRs covered in epics: 18
- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

Found — `bmad-ux` spine pair (`DESIGN.md` + `EXPERIENCE.md`), status `final`, plus 4 rendered key-screen mocks (`key-calendar.html`, `key-live-race.html`, `key-fancard.html`, `key-race-weekend-detail.html`).

### UX ↔ PRD Alignment

EXPERIENCE.md's Information Architecture delta table and Key Flows (Flow 1–5) map cleanly onto the PRD's FR-1–FR-18 and UJ-1–UJ-5 — every PRD user journey has a corresponding UX flow, and every FR has a named UX component or explicit "spine-only, no mock" designation. No UX requirement exists outside PRD scope; UX-DR15 (`accent-editorial`'s new content-callout role) is a visual-treatment decision in service of FR-16, not a separate requirement.

UX itself discloses several visual-spec gaps rather than silently deciding them: News Feed thumbnail sizing (UX-DR10), Profile grouped-stats section styling (UX-DR11), the past-winners list row treatment, and the Championship Sidebar's third slot (PRD Open Question 5) are all named as open in `DESIGN.md`. These are low-novelty presentation details, not missing functional coverage — carried forward as implementation-time judgment calls, consistent with how the epics document already treats them (see Story Quality Review, next).

### UX ↔ Architecture Alignment

**Good, but only because a real misalignment was caught and fixed during the Architecture phase's own reviewer gate — not because the two were consistent from the start.** Documenting the history for traceability:

- `EXPERIENCE.md`/`DESIGN.md` specified the FR-10 Fan Card prompt as reusing "the existing one-level-deep overlay pattern" — this pattern does not exist anywhere in the shipped codebase (verified: no modal/dialog/portal component; `FanCardWizard` renders inline). The Architecture spine's adversarial reviewer pass caught this and added AD-13 (a new shared `Modal` primitive), which Epic 9's Story 9.1 now implements as a real foundation story rather than an assumed reuse. **Status: resolved**, and the epics document already reflects the corrected reality.
- All other UX components have a direct, verified Architecture counterpart: `replay-bar` ↔ AD-1–AD-4 (`replayStore`, `ReplayBar.tsx`); `sidebar-championship` ↔ AD-8 (`ChampionshipSidebar.tsx`, shared query); `trading-card` ↔ AD-10 (asset curation) and AD-9 (storage migration); track outline usage in `weekend-card-v2` and the Race Weekend Detail page ↔ AD-5/AD-6/AD-7 (shared `circuit-configs` asset, same-origin fetch fix, sitewide attribution); `news-preview-row` ↔ AD-11 (`NewsItem.cs` extension).
- Architecture's Structural Seed doesn't enumerate a separate file for every UX sub-component (e.g. `prediction-callout`, `track-records-section`, and `circuit-stat-tile` all land inside one modified `RaceWeekendDetailView.tsx`) — appropriate per the spine's own scope (scaffold, not a full mirror of every component).
- Architecture's AD-12 (axe-core CI gate) directly operationalizes the PRD/UX accessibility requirements (NFR1, `EXPERIENCE.md`'s Accessibility Floor section) rather than leaving them as an unverified aspiration.

### Warnings

None outstanding. The one substantive UX↔Architecture gap found during this project (FR-10's assumed-but-nonexistent modal pattern) is already closed in both the Architecture spine and the epics document.

## Epic Quality Review

Applying create-epics-and-stories standards without compromise, including to this session's own output.

### Epic Structure Validation

| Epic | User-Value Title? | Independent of Later Epics? | Notes |
| --- | --- | --- | --- |
| 7 — Calendar & Season Overview | ✓ | ✓ (first in sequence) | — |
| 8 — Live Race Replay | ✓ | ✓ | — |
| 9 — Fan Card Discovery, Redesign & Multi-Card | ✓ | ✓ | Story-level issue found, see below |
| 10 — Race Weekend Detail | ✓ | ✓ | Soft backward dependency on Epic 7 (see Minor Concerns) |
| 11 — Profile Clarity | ✓ | ✓ | — |
| 12 — News Feed Preview | ✓ | ✓ | Story-level issue found, see below |

No epic is a disguised technical milestone; no epic requires a later epic to function.

### Within-Epic Dependency Check

All 20 stories reviewed for forward references. **No forward dependencies found** — every story that references another story's output references a lower-numbered (already-completed) story (e.g. 7.4 ← 7.2, 7.3; 8.3/8.4/8.5 ← 8.2; 9.2 ← 9.1).

### Findings by Severity

#### 🔴 Critical Violations

None.

#### 🟠 Major Issues

1. **Story 9.1 ("Shared Modal primitive") has no standalone fan-observable outcome.** Nothing in the app consumes a Modal until Story 9.2 ships — a fan gains literally nothing from 9.1 shipping alone. This differs from Story 7.3, which fixes an existing, currently-broken production code path (`TrackMap.tsx`) with an immediate, independent fan-facing effect. 9.1 reads closer to the workflow's own bad-example pattern ("Build reusable components" with no direct user story). **Recommendation:** merge Story 9.1 into Story 9.2 — build the Modal primitive as part of delivering the Fan Card prompt, so the merged story's acceptance criteria (dialog behavior + prompt behavior) ship together as one demonstrable increment.
2. **Story 12.2 ("Automated accessibility gate") is framed around "the developer," not a fan** — it has no direct end-user-facing outcome, which is the textbook definition of a technical story per this review's own standard. It is, however, the sole enforcement mechanism for an explicit, numbered PRD success metric (SM-2 / NFR1) that has no other natural home among the fan-facing stories. **Recommendation:** keep it, but state the justification explicitly in the epics document itself (not just in this report) so a future reader doesn't mistake it for scope creep.

#### 🟡 Minor Concerns

1. **Story 7.3 is infra/asset-flavored.** Accepted as justified (see above — it fixes a live, existing bug with immediate independent value), but flagged for visibility since it sits close to the "technical milestone" pattern this review is instructed to distrust by default.
2. **Story 9.2's dismissal AC** ("does not reappear on my next few visits") is not independently measurable as written. This is intentional — the Architecture spine's Deferred list explicitly assigns the exact suppression window to implementation time — but the story text itself doesn't say so, which could read as vagueness to a dev agent working from the story in isolation rather than the full document set.
3. **Epic 10's Story 10.1 has a soft backward dependency on Epic 7's Story 7.3** for full-season visual richness. Functionally independent (graceful degradation is an explicit AC), and the epics document already discloses this in Epic 10's Architecture note — no document change needed, noted here for completeness.

### Best Practices Compliance Checklist (aggregate)

- [x] Epics deliver user value (6/6)
- [x] Epics function independently of later epics (6/6)
- [x] Stories appropriately sized (20/20, with two flagged for reframing — see Major Issues)
- [x] No forward dependencies (0 found across 20 stories)
- [x] No database/entity creation violations (app remains DB-free; no premature schema work)
- [x] Given/When/Then acceptance criteria throughout (20/20)
- [x] Traceability to FRs maintained (18/18 FRs traceable; 2 stories — 7.3, 9.1 — are justified non-FR foundation work with explicit rationale)
- [x] Brownfield indicators present and correct (Story 7.3 is a compatibility/bugfix story against existing code; Story 9.4 is an explicit storage-migration story; no false "starter template" story was added, correctly, since Architecture specifies none)

## Summary and Recommendations

### Overall Readiness Status

**READY** — with 2 non-blocking recommendations to apply before or during sprint planning.

### Critical Issues Requiring Immediate Action

None. Zero critical violations found across document alignment, FR coverage, or epic/story structure.

### Recommended Next Steps

1. **Merge Story 9.1 into Story 9.2.** The Modal primitive has no fan-observable effect until the prompt (9.2) consumes it — ship them as one story so every increment in the sprint plan is independently demonstrable. Low-risk, mechanical edit to `epics.md`.
2. **Add an explicit justification line to Story 12.2** in `epics.md` stating it exists to enforce PRD success metric SM-2 (Accessibility, NFR1) — so it isn't mistaken for unscoped technical work by a future reader working from the epics document alone, without this report alongside it.
3. **Carry forward three still-open PRD questions into sprint planning**, not as blockers but as items to resolve before their respective stories start: OQ1 (Race Weekend Detail field-removal content audit, affects Epic 10), OQ5 (Championship Sidebar third slot, affects Epic 7 Story 7.2 — currently correctly shipped as a two-slot sidebar with the third slot explicitly deferred), and the SM-C1/SM-2/SM-3 placeholder numeric thresholds (20% TTI regression, axe ≥95, Lighthouse ≥85) — confirm these are the real target numbers before Story 12.2 (the CI gate) is implemented, since they become hard CI failure thresholds.

### Final Note

This assessment reviewed 5 documents (PRD, Architecture, DESIGN.md + EXPERIENCE.md, epics.md) covering 18 FRs, 4 NFRs, 15 UX-DRs, and 20 stories across 6 epics. It found 0 critical issues, 2 major issues (both addressed by small edits to `epics.md`, not a structural rework), and 3 minor concerns (all already accepted with documented rationale). Traceability is complete: every FR maps to a story, every UX component maps to an Architecture decision, and every story maps to a Given/When/Then acceptance criteria set with no forward dependencies. The one significant gap this whole planning chain surfaced — the FR-10 modal pattern that was assumed but never actually built — was already caught and closed during the Architecture phase's own reviewer gate, before it ever reached this check. Recommend applying the 2 major-issue fixes to `epics.md`, then proceeding to `bmad-sprint-planning`.

---

**Assessed by:** Implementation Readiness workflow (bmad-check-implementation-readiness)
**Date:** 2026-07-13
