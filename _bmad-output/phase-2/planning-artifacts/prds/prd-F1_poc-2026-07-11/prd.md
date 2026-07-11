---
title: F1_poc Phase 2 — UI/UX Improvement Pass
status: final
created: 2026-07-11
updated: 2026-07-11
---

# PRD: F1_poc Phase 2 — UI/UX Improvement Pass

## 0. Document Purpose

This PRD is written for the System Architect, UX designer, and future developers continuing work on F1_poc. It specifies Phase 2: a page-by-page UI/UX improvement pass on top of the already-shipped, functionally complete MVP (see `prd-F1_poc-2026-06-15/prd.md`, all 6 epics done). This document does not restate MVP functional requirements it doesn't change — it builds on that PRD and its Glossary, and specifies only what's new or altered.

Primary input is the 2026-07-11 brainstorming session (`_bmad-output/brainstorming/brainstorm-phase-2-ux-improvements-2026-07-11/`), converged via MoSCoW: everything the product owner described is Must-have for this phase; two coach-suggested items are parked as Could (§6.2). Features are grouped by page/surface with globally numbered FRs (this document's own FR-1 through FR-18) for stable downstream reference. Assumptions are tagged inline `[ASSUMPTION]` and indexed in §10.

---

## 1. Vision

The MVP proved the concept: a working, feature-complete F1 fan app built on free public data, covering the full fan lifecycle from calendar to live race to standings to fan identity. Phase 2 does not add new capability areas — it makes the existing product feel like something worth putting in front of real users.

The unifying theme, named directly by the product owner during brainstorming: **less clutter, always show something useful.** Concretely, two moves recur across every page in this PRD: (1) stop repeating the same generic data everywhere (championship standings on every race card) in favor of showing specific, textured content once, in the right place (a driver's actual photo, a track's actual shape, a real lap record); and (2) never leave the user looking at an empty or dead page — give them something to look at, and where possible something to *do* (filter, replay, scrub), not just a static state.

Phase 2 also explicitly raises the bar on accessibility and mobile usability. The product currently has no real users; this phase exists to close that quality gap *before* real users arrive `[ASSUMPTION: "launch-grade" stakes here means production craft quality in anticipation of future public users, not enterprise operational requirements like SLAs or compliance — there is no team, no accounts, no payment flow]`.

---

## 2. Target User

Reuses the MVP's Jobs To Be Done and Non-Users (§2.1–2.2 of `prd-F1_poc-2026-06-15/prd.md`) unchanged, with one addition and one clarification below. Key User Journeys in §2.3 are new for Phase 2 — they describe the *redesigned* experience, not a replacement of MVP's UJ-1–UJ-3 (which still hold structurally; Phase 2 changes how they look and feel, not what they accomplish).

### 2.1 Jobs To Be Done (addition)

- Quickly see what matters right now (the next race, the current leader) without wading through irrelevant past data or repeated boilerplate.

### 2.2 Non-Users (Phase 2 clarification)

- Users seeking gamified or collectible Fan Card mechanics (rarity, trading, badges) — explicitly out of scope; see §5.

### 2.3 Key User Journeys

- **UJ-1. A casual fan opens the Calendar and instantly sees what's next.**
  - **Persona + context:** A returning fan, mid-week, no live session running.
  - **Entry state:** Unauthenticated, lands on the Calendar page (as today).
  - **Path:** Page loads already filtered to "Future" by default — the next race is visually distinguished at the top; a filter control is visible but untouched. The persistent Championship Sidebar shows current Drivers' and Constructors' standings without the user scrolling past a wall of past-race cards.
  - **Climax:** The user sees the next race and current standings within the first screen, with no scrolling and no mental filtering of stale data.
  - **Resolution:** User clicks the next race's card, or switches the filter to "Past" deliberately if that's what they wanted.
  - Realizes: FR-1, FR-2, FR-3, FR-4.

- **UJ-2. A fan opens the app when nothing is live and gets a real race to watch, not a blank page.**
  - **Persona + context:** Same fan, Tuesday afternoon, no session in progress.
  - **Entry state:** Opens the Live Race page directly (e.g. from a bookmark).
  - **Path:** Page loads populated with the most recently completed race's data (extends existing fallback behavior, hardening it — see FR-5) with a clear "past race" label. A replay control bar is visible at the bottom. User taps play; the race simulates lap by lap, gaps and positions updating as they would live. User drags the scrub bar to jump to the final laps.
  - **Climax:** The page is never empty, and the fan can actively explore a past race instead of just reading a static result.
  - **Resolution:** User pauses at a specific lap, or lets it play to the end.
  - **Edge case:** If the user backgrounds the tab mid-replay and returns later, playback resumes paused at the same lap, not reset. `[ASSUMPTION: no persistence across page reloads/sessions — resume-on-return applies only within the same page session]`
  - Realizes: FR-5, FR-6, FR-7, FR-8, FR-9.

- **UJ-3. A new visitor gets nudged into creating a Fan Card and ends up making two.**
  - **Persona + context:** A fan who's used the app a couple of times but never noticed the Fan Card page.
  - **Entry state:** Browsing the Standings page.
  - **Path:** A prompt (popup or inline nudge) invites them to pick a favorite driver and build a card. They do — the resulting card shows the driver's photo, autograph, team logo, and team principal. They like it enough to make a second one for another driver they follow.
  - **Climax:** The card looks good enough to want to keep or share, without any account creation.
  - **Resolution:** Both cards persist locally in the browser; the user can view either at any time.
  - Realizes: FR-10, FR-11, FR-12.

- **UJ-4. A fan checks a Race Weekend before it starts and gets a fast, plain-language read.**
  - **Persona + context:** Same fan, evening before qualifying.
  - **Entry state:** Opens a Race Weekend detail page from the Calendar.
  - **Path:** The page shows the circuit's actual track layout, the all-time and this-year fastest lap with driver names, and relevant track history/records — without unrelated or confusing fields cluttering the view. A simple win prediction states who's likely to win and why, in a sentence, not a probability table.
  - **Climax:** The fan understands the stakes and context for this weekend in under a few seconds of reading.
  - **Resolution:** Fan sets a reminder or moves on to check standings — or, curious about a driver mentioned in the win prediction, clicks through to that driver's profile and finds the same clear, grouped presentation of career stats (FR-17) rather than a jarring drop into a differently-styled page.
  - Realizes: FR-13, FR-14, FR-15, FR-16, FR-17.

- **UJ-5. A fan skims the News Feed and decides whether an article is worth leaving the app for.**
  - **Persona + context:** Same fan, browsing between races.
  - **Entry state:** Opens the News Feed page (unchanged entry point).
  - **Path:** Each headline now shows an associated photo and a short snippet, not just a bare title.
  - **Climax:** The fan can judge relevance before clicking out to the source site.
  - **Resolution:** Clicks through to read the full article on the source site (unchanged — still redirects out), or skips it.
  - Realizes: FR-18.

---

## 3. Glossary

Reuses the MVP Glossary (`Race Weekend`, `Session`, `Circuit`, `Driver`, `Constructor`, `Gap`, `Stint`, `Tyre Compound`, `Sector`, `Championship Delta`, `Track Time`, `Local Time`, `Ergast API`, `OpenF1 API`) unchanged. New terms introduced by Phase 2:

- **Championship Sidebar** — A persistent panel on the Calendar page showing current Drivers' and Constructors' Championship standings, replacing per-card repetition of the same data.
- **Race Replay** — A client-side simulated playback of a completed Race's lap-by-lap data (positions, gaps, tyres) driven by historical data, used to populate the Live Race page when no Session is currently live. Distinct from live telemetry — always clearly labelled as a replay of a past Race.
- **Fan Card** — A personal, shareable digital card representing a user's chosen favorite Driver and their Constructor, stored client-side per browser with no account required, exportable as an image. A user may hold multiple Fan Cards.
- **Win Prediction** — A simplified, plain-language presentation of the pre-race Win Probability calculation (MVP FR-6): names a likely winner and a short rationale rather than exposing raw percentages or methodology.

---

## 4. Features

### 4.1 Calendar & Season Overview

**Description:** The Calendar page currently shows every race — past, current, and future — by default, and repeats full championship standings on every single race card. Phase 2 refocuses the default view on what's next, adds user control over what's shown, moves standings to one persistent location, and makes each race card carry more specific, useful content instead of repeated boilerplate. Realizes UJ-1.

**Functional Requirements:**

#### FR-1: Upcoming-focused default view

By default, the Calendar page displays the next Race Weekend and all Race Weekends after it; past Race Weekends are not shown.

**Consequences (testable):**
- On page load with no filter interaction, zero past Race Weekends are rendered.
- The next upcoming Race Weekend remains visually distinguished (per existing MVP FR-1 behavior).

#### FR-2: Race filter control

The user can switch the Calendar's race list between three views: All, Future, and Past.

**Consequences (testable):**
- Selecting "Past" shows only completed Race Weekends; selecting "Future" matches the FR-1 default; selecting "All" shows the full season.
- The selected filter is visually indicated at all times.

#### FR-3: Persistent Championship Sidebar

The Calendar page displays a persistent sidebar showing the current Drivers' Championship standings (driver, points) and Constructors' Championship standings (constructor, points), replacing the need to show this data on every race card.

**Consequences (testable):**
- Sidebar content matches the same live standings data source used by the Standings page.
- Sidebar remains visible/accessible regardless of which filter (FR-2) is active or how far the user has scrolled the race list `[ASSUMPTION: "persistent" means sticky/always-reachable on desktop; on mobile it may collapse to an accessible summary or drawer — exact mobile treatment deferred to bmad-ux]`.

**Notes:** A third sidebar slot was left open during brainstorming ("maybe something else") — see Open Question 5.

#### FR-4: Redesigned Race Weekend card

Each Race Weekend card is visually larger/more prominent than the current MVP card, no longer repeats full championship standings, and additionally displays the circuit's real track outline and fastest-lap context (the all-time fastest lap and the current or most recent year's fastest lap at that circuit, each with the driver's name).

**Consequences (testable):**
- No race card renders driver/constructor standings data (moved to FR-3).
- Every card renders a recognizable track outline for its circuit.
- Every card with available historical lap data renders both an all-time and a current/recent-year fastest lap value with driver attribution; cards for circuits without qualifying historical data omit this gracefully rather than showing an error state `[ASSUMPTION: mirrors the existing MVP FR-4 "omitted/labelled" pattern for missing contextual data]`.

**Out of Scope:** Track outline rendering technique (SVG generation/source) is an implementation decision — lives in `addendum.md`, not here.

---

### 4.2 Live Race Experience — Always-On & Replayable

**Description:** MVP's FR-16 already specified that the Live Race page should fall back to the last completed race when nothing is live. In practice, the page currently shows an empty state instead — this section both hardens that existing commitment and extends it into an actively explorable replay, rather than a static fallback view. Realizes UJ-2.

**Functional Requirements:**

#### FR-5: Guaranteed non-empty Live Race page

When no Session is currently live, the Live Race page always renders the most recently completed Race's full data set (positions, gaps, tyres, sectors, timeline — the same components used for a live session), clearly labelled as a past race.

**Consequences (testable):**
- The Live Race page never renders an empty state or error state when the only issue is "no session currently live" (genuine API failures remain a distinct, separately-handled state, per MVP FR-16).
- This closes the gap between MVP FR-16's original intent and current shipped behavior — treat as a regression fix, not new scope.

#### FR-6: Race Replay controls

The user can start, stop, and restart a Race Replay of the fallback race from FR-5.

**Consequences (testable):**
- Replay controls are visible whenever the page is in fallback (non-live) mode.
- "Restart" returns playback to lap 1 without a page reload.

#### FR-7: Replay scrub bar and lap jump

The user can drag a scrub bar or otherwise directly select a specific lap to jump the Race Replay to.

**Consequences (testable):**
- Jumping to a lap updates all dependent views (positions, gaps, tyres) to that lap's state, not just a visual timeline marker.

#### FR-8: Replay playback speed control

The user can select a playback speed for the Race Replay (e.g. 1x, 2x, 4x).

**Consequences (testable):**
- Changing speed takes effect immediately without restarting playback.

#### FR-9: Replay pause and resume

The user can pause the Race Replay at any point and resume from exactly that point.

**Consequences (testable):**
- Resuming after pause continues from the paused lap/position, not from the start or the live edge.

---

### 4.3 Standings — Fan Card Discovery

**Description:** The Standings page is high-traffic and currently does nothing to surface the Fan Card feature to users who haven't found it. Phase 2 adds a lightweight discovery nudge. Realizes UJ-3 (entry point only — see §4.4 for the card itself).

**Functional Requirements:**

#### FR-10: Fan Card creation prompt

Users who have not yet created a Fan Card see a prompt (popup or inline) on the Standings page inviting them to create one.

**Consequences (testable):**
- Prompt does not appear for users who already have at least one Fan Card.
- Dismissing the prompt does not block further use of the Standings page `[ASSUMPTION: dismissal is remembered per browser for some period rather than reappearing every visit — exact suppression window is a UX-stage decision]`.

---

### 4.4 Fan Card Redesign & Multi-Card Support

**Description:** The current Fan Card's visual design is a known weak point. Phase 2 redesigns it with richer, driver-specific content, and lets a user hold more than one card — while deliberately staying a clean, personal artifact rather than a gamified collectible system (explicit scope decision from brainstorming; see §5). No account system is introduced — cards remain client-side per browser, consistent with the MVP's existing no-auth design. Realizes UJ-3.

**Functional Requirements:**

#### FR-11: Fan Card visual redesign

The Fan Card displays the chosen driver's photo, the driver's autograph/signature, the associated Constructor's team logo, and the team principal's name (e.g. Toto Wolff for Mercedes), in addition to whatever the MVP card already showed.

**Consequences (testable):**
- Every supported driver/constructor pairing renders all four new elements without a broken-image or missing-data state under normal conditions `[ASSUMPTION: driver photo, autograph, and team-principal data are sourceable — see Open Question 4]`.
- Card remains exportable as a client-side-generated image (existing MVP capability, preserved).

**Out of Scope:** Sourcing/licensing of driver photos and autograph assets — implementation detail for `addendum.md`.

#### FR-12: Multiple Fan Cards per user

A user can create and hold more than one Fan Card (e.g. for two or three different drivers) within the same browser.

**Consequences (testable):**
- Creating a new card does not overwrite or delete an existing one.
- All of a user's cards are viewable/accessible from the Fan Card page.

---

### 4.5 Race Weekend Detail — Context & Clarity

**Description:** The detail view should read as clear and purposeful, not a data dump. This section both tightens existing content and adds specific, high-value context: real track visuals, lap records, historical data, and a simplified win prediction. Realizes UJ-4.

**Functional Requirements:**

#### FR-13: Track layout visualization

The Race Weekend detail page displays the circuit's real track outline/shape — the same visual asset class as FR-4's card-level track outline, shown here at greater size/detail.

**Consequences (testable):**
- Every Race Weekend detail page renders a recognizable track outline for its circuit, at a larger/more detailed treatment than the FR-4 card-level version.
- Circuits without a sourced outline asset degrade gracefully (omitted or a generic placeholder, not a broken-image state), mirroring FR-4's degradation pattern.

**Out of Scope:** Track outline rendering technique (SVG generation/source) — see `addendum.md`.

#### FR-14: Track lap record context

The Race Weekend detail page displays the all-time fastest lap at this circuit and the fastest lap of the current (or most recently completed) year at this circuit, each with the driver's name.

**Consequences (testable):**
- Matches the same underlying record data as FR-4; presented here with more detail/prominence than the card-level summary.

#### FR-15: Track historical data and records

The Race Weekend detail page displays additional circuit historical data and records beyond lap times: at minimum, the list of past race winners at this circuit (year, driver, team) and core circuit stats (length, number of corners, DRS zones, year of first F1 race) `[ASSUMPTION: draws on the same historical dataset already scoped for the MVP's Circuit Profile page (MVP FR-20) rather than requiring new data sourcing]`.

**Consequences (testable):**
- Page renders at least the past-winners list and the four named circuit stats (length, corners, DRS zones, first-race year) for any circuit with MVP FR-20 data available.
- Circuits lacking full historical data degrade gracefully (partial display, not an error state), mirroring FR-4's degradation pattern.

#### FR-16: Simplified win prediction

The Race Weekend detail page presents a Win Prediction (per Glossary) in plain language — a likely winner and a short, human-readable rationale — built on top of the existing MVP FR-6 win-probability calculation, without exposing raw percentages or the underlying calculation to the user.

**Consequences (testable):**
- Prediction text names a driver and gives at least one concrete reason (e.g. grid position, recent form, historical track performance).
- No percentage figures or statistical methodology are shown in this view (MVP's existing grid-position win-probability display, if kept, would need to move behind a secondary/expandable "details" affordance or be superseded by this — flagged as an Open Question).

**Notes:** `[NOTE FOR PM]` Confirm which currently-displayed Race Weekend Detail fields should be removed as part of the "no unnecessary/confusing fields" clean-up — see Open Question 1. This is a content-audit task, not a single testable FR.

---

### 4.6 Circuit / Driver / Constructor Profile Clarity

**Description:** Extends the MVP's existing Circuit Profile, Driver Career Profile, and Driver Head-to-Head pages (MVP FR-20–FR-22) with better-structured presentation of the career statistics and history they already contain. Realizes UJ-4 (extended — see UJ-4's updated Realizes list in §2.3).

**Functional Requirements:**

#### FR-17: Structured career stats presentation

Driver and Constructor profile pages present career statistics and history (wins, races won, finishing positions, etc.) in a clearly structured, scannable layout: grouped by category (season-by-season results, career totals, head-to-head where applicable) rather than a single undifferentiated list, with each group individually scannable without horizontal scrolling on a 360px-wide mobile viewport.

**Consequences (testable):**
- Underlying data is unchanged from MVP FR-20–FR-22; this FR governs presentation/structure only, not new data.
- Career stats render in at least two visually distinct groups (e.g. season-by-season vs. career totals) rather than one flat list.
- No group requires horizontal scrolling at a 360px viewport width.

**Out of Scope:** Specific layout/visual treatment — a UX-stage decision (`bmad-ux`).

---

### 4.7 News Feed Preview

**Description:** Extends the MVP's News Feed (MVP FR-23) so a user can judge an article's relevance before leaving the app. The redirect-out-on-click behavior is preserved as-is. Realizes UJ-5.

**Functional Requirements:**

#### FR-18: News item preview

Each News Feed item displays an associated photo/thumbnail and a short text snippet/summary, in addition to the existing title, source, and publish time.

**Consequences (testable):**
- Items whose source feed lacks an image or snippet degrade gracefully (title-only, as today) rather than showing a broken-image placeholder `[ASSUMPTION: not every RSS source will have both fields populated]`.
- Clicking an item still redirects to the original source article (unchanged from MVP FR-23).

**Feature-specific NFRs:**
- No new external data source is introduced — see Open Question 4 on confirming existing feed fields carry this data.

---

## 5. Non-Goals (Explicit)

- **No user accounts or authentication.** Fan Cards (including multiple cards, FR-12) remain client-side/browser-local, consistent with the MVP's no-auth design. Introducing accounts is out of scope for this phase.
- **No gamification of Fan Cards.** No rarity tiers, collection mechanics, trading, or badges — explicit scope boundary set during brainstorming. Fan Cards stay a clean, personal artifact.
- **No monetization or advertising** changes in this phase.
- **No new live-data source integrations.** Race Replay (FR-6–FR-9) simulates already-available historical data; it is not a new real-time telemetry capability.
- **No new top-level pages/destinations.** Phase 2 restructures and enriches the existing seven surfaces (Calendar, Live Race, Standings, Fan Card, Race Weekend Detail, Circuit/Driver/Constructor Profiles, News Feed); it does not add new navigation destinations, aside from new *entry points* into the existing Fan Card page (FR-10).

---

## 6. MVP Scope

*(Retained heading name for consistency with the MVP PRD's structure; "MVP" here refers to Phase 2's own minimum shippable scope.)*

### 6.1 In Scope

- FR-1 through FR-18, as specified in §4.

### 6.2 Out of Scope for This Phase

- **Sidebar live countdown to next race lights-out** — parked as Could during brainstorming convergence: it was a coach-suggested spark rather than a user-requested item, and it's additive polish rather than something the "less clutter, always useful" thesis requires. Candidate for a future phase.
- **Standings-page custom loader showing the current leader** — parked as Could for the same reason: coach spark, not user-requested, purely additive. Candidate for a future phase.
- Everything listed in §5 (Non-Goals).

---

## 7. Cross-Cutting NFRs

- **Accessibility.** All Phase 2 surfaces target WCAG 2.1 AA: sufficient color contrast for team-colored UI elements, keyboard operability for the Calendar filter (FR-2), the Replay controls (FR-6–FR-9), and any Fan Card creation flow, and screen-reader-appropriate labelling for the track outline visuals (FR-4, FR-13) and Championship Sidebar (FR-3). Directly serves the stated goal of the app being "more accessible."
- **Mobile / responsive.** Every redesigned surface must be fully usable on common mobile viewport widths, with one-handed operation feasible for the Replay controls in particular (the scenario that surfaced this requirement during brainstorming).
- **Performance.** Added visual richness (track outlines, Fan Card imagery, News Feed thumbnails, Championship Sidebar) must not introduce animation jank or materially regress page load time — see counter-metric SM-C1.
- **Visual consistency.** A single, coherent visual language (color, spacing, typography, iconography) applies across all redesigned pages `[ASSUMPTION: not explicitly stated by the user, but implied by "the application should be better then now... and better at all" — flagged for confirmation]`. Detailed visual/brand direction (palette, type, tone) is deferred to `bmad-ux`; this PRD does not prescribe it.

---

## 8. Success Metrics

*No real users exist yet — metrics are proxy/completion-based rather than usage-analytics-based for this phase.*

**Primary**
- **SM-1**: Feature completion — 100% of FR-1 through FR-18 implemented and verified to match their specified behavior via PM/dev acceptance review. Validates FR-1–FR-18.
- **SM-2**: Accessibility bar — Calendar, Live Race, Standings, Fan Card, and Race Weekend Detail pages each score WCAG 2.1 AA on an automated audit (e.g. axe or Lighthouse Accessibility ≥ 95). Validates the Accessibility NFR (§7). `[ASSUMPTION: exact tooling/threshold]`

**Secondary**
- **SM-3**: Mobile experience quality — Lighthouse mobile Performance score ≥ 85 on all five redesigned pages (Calendar, Live Race, Standings, Fan Card, Race Weekend Detail). Validates the Mobile/Performance NFRs (§7). `[ASSUMPTION: exact threshold]`
- **SM-4**: Subjective quality bar — page-by-page before/after review confirms the app looks and feels ready to show real users. Directly validates the qualitative goal stated during Discovery ("should be better then now, more accessible and better at all").

**Counter-metrics (do not optimize)**
- **SM-C1**: Added content (Sidebar, track visuals, Fan Card imagery, News thumbnails) must not increase Time-to-Interactive on any redesigned page by more than `[ASSUMPTION: 20%]` over its current baseline — guards against trading clutter for slowness. Counterbalances SM-1/SM-4.
- **SM-C2**: New interaction surfaces (filters, replay scrubber, multi-card creation, prompts) must not add extra required steps to the core "see the next race" or "watch the live/last race" journeys. Counterbalances SM-1's push toward feature richness.

---

## 9. Open Questions

1. Which currently-displayed fields on the Race Weekend Detail page should be removed as part of the "clear/informative, no unnecessary fields" pass (FR-13–FR-16 area)? Needs a content audit, likely during the UX phase.
2. What is the intended relationship between the new simplified Win Prediction (FR-16) and the MVP's existing raw win-probability-per-grid-slot display (MVP FR-6) — does the new prediction replace it, sit above it, or hide it behind a "details" toggle?
3. Detailed visual/brand direction (palette, typography, iconography, motorsport-motif usage) — deferred to `bmad-ux`.
4. Data sourcing for two new content types: (a) driver photos, autographs, and team-principal names for the Fan Card redesign (FR-11); (b) whether the existing RSS news source(s) actually populate `Description`/enclosure-image fields with usable content for FR-18, or whether a source change is needed. `[NOTE FOR PM]` — this could affect FR-11/FR-18 feasibility and should be spiked early.
5. Is there anything else for the third Championship Sidebar slot (FR-3), left open during brainstorming ("maybe something else"), or does it stay a two-item sidebar (drivers + constructors)?
6. Rollout sequencing — ship all seven feature areas (§4.1–4.7) together, or incrementally by page, matching the order this PRD covers them in? `[ASSUMPTION: incremental, mirroring how MVP epics were sequenced by feature area — pending confirmation]`

---

## 10. Assumptions Index

- §1 — "Launch-grade" stakes interpreted as production craft quality for future public users, not enterprise operational requirements.
- §2.3 (UJ-2) — Replay pause state does not persist across page reloads/sessions, only within a session.
- §4.1 (FR-3) — "Persistent" sidebar may collapse to a drawer/summary on mobile; exact treatment deferred to UX.
- §4.1 (FR-4) — Missing historical lap data degrades gracefully, mirroring MVP FR-4's existing pattern.
- §4.3 (FR-10) — Fan Card prompt dismissal is remembered for some period rather than reappearing every visit; exact window is a UX-stage decision.
- §4.4 (FR-11) — Driver photo, autograph, and team-principal data are assumed sourceable (see Open Question 4).
- §4.5 (FR-15) — Additional circuit historical data draws on the dataset already scoped for MVP's Circuit Profile page.
- §4.7 (FR-18) — Not every RSS source will have both an image and a snippet populated; graceful degradation assumed acceptable.
- §7 — Visual consistency across pages is implied by the user's stated goal, not explicitly stated; flagged for confirmation.
- §8 (SM-2, SM-3) — Specific accessibility/performance score thresholds are provisional, pending confirmation.
- §8 (SM-C1) — 20% Time-to-Interactive regression ceiling is a placeholder, pending confirmation.
- §9.6 — Incremental (page-by-page) rollout sequencing assumed, matching MVP's epic-by-feature-area pattern.
