---
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - '_bmad-output/phase-2/planning-artifacts/prds/prd-F1_poc-2026-07-11/prd.md'
  - '_bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md'
  - '_bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/DESIGN.md'
  - '_bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/EXPERIENCE.md'
---

# F1_poc Phase 2 - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for F1_poc Phase 2 (the UI/UX Improvement Pass), decomposing the requirements from the phase-2 PRD, UX design spine (DESIGN.md + EXPERIENCE.md), and Architecture spine into implementable stories.

## Requirements Inventory

### Functional Requirements

FR-1: By default, the Calendar page displays the next Race Weekend and all Race Weekends after it; past Race Weekends are not shown.
FR-2: The user can switch the Calendar's race list between three views: All, Future, and Past.
FR-3: The Calendar page displays a persistent sidebar showing the current Drivers' Championship standings and Constructors' Championship standings, replacing per-card standings repetition.
FR-4: Each Race Weekend card is visually larger/more prominent, no longer repeats championship standings, and displays the circuit's real track outline plus all-time and current/recent-year fastest-lap context with driver attribution.
FR-5: When no Session is currently live, the Live Race page always renders the most recently completed Race's full data set, clearly labelled as a past race.
FR-6: The user can start, stop, and restart a Race Replay of the fallback race from FR-5.
FR-7: The user can drag a scrub bar or otherwise directly select a specific lap to jump the Race Replay to.
FR-8: The user can select a playback speed for the Race Replay (e.g. 1x, 2x, 4x).
FR-9: The user can pause the Race Replay at any point and resume from exactly that point.
FR-10: Users who have not yet created a Fan Card see a prompt (popup or inline) on the Standings page inviting them to create one.
FR-11: The Fan Card displays the chosen driver's photo, autograph/signature, the Constructor's team logo, and the team principal's name, in addition to the MVP card's existing content.
FR-12: A user can create and hold more than one Fan Card within the same browser.
FR-13: The Race Weekend detail page displays the circuit's real track outline/shape, at greater size/detail than FR-4's card-level version.
FR-14: The Race Weekend detail page displays the all-time fastest lap and the fastest lap of the current/most-recent year at this circuit, each with the driver's name.
FR-15: The Race Weekend detail page displays additional circuit historical data and records: past race winners (year, driver, team) and core circuit stats (length, corners, DRS zones, first F1 race year).
FR-16: The Race Weekend detail page presents a Win Prediction in plain language — a likely winner and a short, human-readable rationale — without exposing raw percentages by default.
FR-17: Driver and Constructor profile pages present career statistics and history grouped by category (season-by-season, career totals, head-to-head where applicable) rather than a single undifferentiated list, scannable without horizontal scroll at 360px width.
FR-18: Each News Feed item displays an associated photo/thumbnail and a short text snippet, in addition to the existing title, source, and publish time.

### NonFunctional Requirements

NFR1 (Accessibility): All Phase 2 surfaces target WCAG 2.1 AA — sufficient color contrast for team-colored UI elements, full keyboard operability for the Calendar filter, Replay controls, and Fan Card creation flow, and screen-reader-appropriate labelling for track outline visuals and the Championship Sidebar. Success metric: Lighthouse/axe Accessibility ≥ 95 on Calendar, Live Race, Standings, Fan Card, and Race Weekend Detail.
NFR2 (Mobile / Responsive): Every redesigned surface must be fully usable on common mobile viewport widths, with one-handed operation feasible for the Replay controls specifically. Success metric: Lighthouse mobile Performance ≥ 85 on the five redesigned pages.
NFR3 (Performance): Added visual richness (track outlines, Fan Card imagery, News thumbnails, Championship Sidebar) must not introduce animation jank or materially regress page load time — Time-to-Interactive regression capped at roughly 20% over current baseline on any redesigned page.
NFR4 (Visual Consistency): A single, coherent visual language (color, spacing, typography, iconography) applies across all redesigned pages, per the phase-2 DESIGN.md extension of the phase-1 system.

### Additional Requirements

- Brownfield extension — no starter template. All work extends the existing shipped codebase (React 19/Vite frontend, ASP.NET Core 10 backend) per the inherited phase-1 architecture; no scaffolding story is needed for any epic.
- New backend endpoint `GET /api/races/{season}/{round}/replay`, served by a new `RaceReplayService`, assembling historical per-lap `RaceStateSnapshot` frames from existing OpenF1/Ergast sources; cached via `IMemoryCache`, 7-day TTL. (Architecture AD-2)
- New client-side `replayStore` (Zustand) holding only `currentLapIndex`/`isPlaying`/`speed`; the fetched frame array stays in TanStack Query cache only. Client-side interval-timer playback, not a new real-time channel. Scrub never changes `isPlaying`. (AD-3, AD-4)
- Replay frames must run the full existing `liveRaceStore` setter sequence (`setDrivers` via `normalizeSnapshot`, `setLapChart`, `setFastestSectors`, `setTimeline`), not just the drivers field, so SectorBoard/LapTimeChart/EventTimeline stay live during replay. (AD-1)
- `circuit-configs/{circuitId}.json`'s existing `trackPath` field is the single source for every rendering of a circuit's shape (calendar card, Race Weekend Detail, live map) — expand coverage from 1 circuit (Monza) to the full current-season calendar, sourced from f1db/f1db (CC-BY-4.0, `-present` layout). One sitewide attribution credit line required. (AD-5, AD-7)
- Fix existing latent bug: circuit-config assets must be fetched via a relative same-origin path, never `${VITE_API_BASE_URL}`-prefixed — applies to the existing `TrackMap.tsx` call site as well as new FR-4/FR-13 code, otherwise track outlines 404 in production (Vercel/Render are different origins). (AD-6)
- `ChampionshipSidebar` must reuse the existing Standings page TanStack Query hook/key — no new endpoint, no new query key. (AD-8)
- Fan Card storage: `useFanCardStore.ts` (Zustand `persist`, actual key `f1app__fanCard__v1`) changes from a single `FanCardPicks` object to `{ cards: FanCardPicks[] }`. Use `persist`'s own `version: 1` + `migrate()` to wrap the old single-object state into a one-item array — no key rename, no `useLocalStorage`-hook-style migration (that mechanism doesn't apply to this store). (AD-9)
- Fan Card assets are manually curated, not a new external API: hand-curated driver photo assets, a hand-maintained team-principal static config, and a stylized signature-font autograph (not a scanned/licensed signature). Missing driver asset falls back to an initials placeholder. (AD-10)
- News preview reuses the existing `NewsFeedService`/`CodeHollow.FeedReader` pipeline — extract `imageUrl` (enclosure) and `snippet` (truncated description) at parse time onto the existing `Models/NewsItem.cs`, no second network hop. (AD-11)
- Add `@axe-core/playwright` assertions to the existing Playwright E2E suite, run against Calendar, Live Race, Standings, Fan Card, and Race Weekend Detail, wired into the existing CI pipeline. (AD-12)
- Add one shared, hand-built `Modal` primitive (`shared/components/Modal.tsx`) — portal-rendered, focus-trapping, closes on Escape/backdrop, returns focus on close, `role="dialog"`. No modal/overlay component exists in the shipped codebase today (a phase-1 UX-doc assumption that was never actually built) — this is a genuinely new primitive, first consumed by the FR-10 Fan Card prompt. (AD-13)

### UX Design Requirements

UX-DR1: `sidebar-championship` — sticky left rail on desktop (250px fixed column, `bg-inset` well treatment, explicitly no card border/fill), collapsing to a tappable drawer summary above the race list on mobile. Labelled landmark region (`aria-label="Championship standings"`) so it's independently reachable/skippable. Reachable regardless of Calendar filter state or scroll position.
UX-DR2: `weekend-card-v2` — extends the phase-1 card (keeps the `accent-editorial` pinned border for the next race); adds a distinct left-side track-outline sub-panel (`bg-inset` well) with text content to its right, and a two-explicit-line fastest-lap block (all-time + current-year, each with driver name). No longer renders championship standings. Card grows taller only, same width as phase-1.
UX-DR3: `replay-bar` — fixed to the viewport bottom (76px height), visible only in fallback/replay mode. Circular bordered play/pause button (`accent-editorial`); lap readout `Lap {n} / {total}` in the numeric-dense type; scrub bar uses discrete snap-to-lap ticks (never continuous/analog seek), with major/minor tick styling; speed control is a 1x/2x/4x button group built from the existing `tab-toggle` visual pattern. Mobile (`< md`): only play/pause + scrub stay inline; Restart and the speed group collapse behind a compact overflow (`⋯`) button using the same bordered-circular treatment.
UX-DR4: Replay scrub bar keyboard contract — `Tab` to focus, `role="slider"`; `Left`/`Right` arrow keys step one lap at a time (matching the snap-to-lap ticks); `Home`/`End` jump to lap 1 and the final lap.
UX-DR5: `trading-card` — portrait 5:7 aspect ratio; constructor color reduced to a 4px top rule only (never a full-bleed background or photo-panel fill); recessed `bg-inset` photo panel; script-styled autograph line (visual intent — technical realization per AD-10 is a signature-style font). Multiple cards render as a grid (`auto-fill, minmax(226px,1fr)`, never a carousel), plus a dashed-border "Add new card" tile at the same aspect ratio.
UX-DR6: `fancard-prompt-modal` — the FR-10 Standings-page discovery nudge, built on the new shared `Modal` primitive (AD-13; the "existing overlay pattern" this component was originally specified against does not actually exist in the shipped app). Launches directly into the existing 3-step Fan Card wizard, not a separate lightweight picker. Does not appear for users who already hold ≥1 Fan Card; dismissal suppressed for a period (exact window deferred — architecture Deferred list).
UX-DR7: `prediction-callout` — Race Weekend Detail. `accent-editorial`-bordered card (first non-chrome content use of that token — plain-language Win Prediction, no percentages by default) sitting above a toggle button (`aria-expanded`/`aria-controls`) that reveals the original MVP raw win-probability table beneath a dashed divider, collapsed by default. Absent entirely (not a placeholder) when no qualifying session has run yet.
UX-DR8: `track-records-section` — Race Weekend Detail, a separate card from the track-layout panel (not merged into one "track" mega-card): all-time and current-year fastest lap, each rendered as a `bg-inset` row with driver name (clickable through to profile).
UX-DR9: `circuit-stat-tile` grid — Race Weekend Detail's Circuit History card: four `bg-inset` tiles (length, corners, DRS zones, first F1 race year) above a past-winners list (year, driver, team; driver names link to profiles). Circuits with partial historical data show whatever exists rather than an error state.
UX-DR10: `news-preview-row` — thumbnail on the left (list-row style), one-line snippet. Items whose source lacks an image or snippet degrade to title-only (unchanged click-through behavior). No pixel-level sizing was specified by UX (flagged gap) — implement conservatively against the existing list-row/gap-list-row spacing vocabulary.
UX-DR11: Profile grouped-stats presentation (Driver/Constructor/Circuit Profile, FR-17) — career stats render as ≥2 visually distinct stacked sections (e.g. season-by-season, career totals, head-to-head where applicable) on one scrollable page, not tabs; no group requires horizontal scroll at 360px width. Section header/divider/spacing treatment was not visually specified by UX (flagged gap) — implement against inherited card/section conventions.
UX-DR12: Calendar filter control — standard `tablist`/`tab` pattern (arrow-key or Tab navigation between the three options), consistent with the existing Standings Drivers/Constructors toggle. Three-way All/Future/Past switch; Future is the default and initial state on every load; selected filter always visually indicated.
UX-DR13: Track outline visuals (Calendar card, Race Weekend Detail) get a screen-reader-appropriate accessible name identifying the circuit (e.g. "Track layout: Circuit de Spa-Francorchamps") — the shape itself is decorative to a non-sighted user, not literally described.
UX-DR14: Zero-completed-races-this-season state (Live Race page) — no Replay bar (nothing to replay); a plain on-brand message names the next race instead of a blank page (e.g. "No races completed yet this season — first race: {next race name}, {date}"), consistent with phase-1's existing graceful-copy pattern.
UX-DR15: `accent-editorial` takes on a new role — Win Prediction callout background/border — its first use as a content-callout color rather than pure chrome (live status/focus/links). Must not be reused for any team-scoped or broadcast-coded value; that boundary is an explicit Do/Don't, not implicit.

### FR Coverage Map

```
FR-1:  Epic 7  - Upcoming-focused default Calendar view
FR-2:  Epic 7  - All/Future/Past filter control
FR-3:  Epic 7  - Persistent Championship Sidebar
FR-4:  Epic 7  - Redesigned Race Weekend card (track outline + fastest-lap context)
FR-5:  Epic 8  - Guaranteed non-empty Live Race page
FR-6:  Epic 8  - Race Replay start/stop/restart controls
FR-7:  Epic 8  - Replay scrub bar and lap jump
FR-8:  Epic 8  - Replay playback speed control
FR-9:  Epic 8  - Replay pause and resume
FR-10: Epic 9  - Fan Card creation prompt on Standings
FR-11: Epic 9  - Fan Card visual redesign (photo, autograph, logo, principal)
FR-12: Epic 9  - Multiple Fan Cards per user
FR-13: Epic 10 - Track layout visualization on Race Weekend Detail
FR-14: Epic 10 - Track lap record context
FR-15: Epic 10 - Track historical data and records
FR-16: Epic 10 - Simplified win prediction
FR-17: Epic 11 - Structured career stats presentation
FR-18: Epic 12 - News item preview (thumbnail + snippet)
```

## Epic List

### Epic 7: Calendar & Season Overview
Fans landing on the Calendar instantly see what's next — no scrolling past stale races, a filter when they want more, standings always in view, and each race card carries a real track shape and lap-record context instead of repeated boilerplate.
**FRs covered:** FR-1, FR-2, FR-3, FR-4
_Implementation note: does the season-wide `circuit-configs` expansion + same-origin fetch fix (Architecture AD-5/6/7) — Epic 10 reuses that asset for FR-13._

### Epic 8: Live Race — Always Something to Watch
The Live Race page is never empty. When nothing's live, fans get the last completed race fully rendered and can actively play, scrub, speed-control, and pause through it like a video, instead of reading a static result.
**FRs covered:** FR-5, FR-6, FR-7, FR-8, FR-9

### Epic 9: Fan Card Discovery, Redesign & Multi-Card
Fans get nudged toward the Fan Card feature from Standings, and once there, end up with a genuinely good-looking card (driver photo, autograph, team logo, team principal) — and can hold more than one.
**FRs covered:** FR-10, FR-11, FR-12
_Implementation note: introduces the app's first shared `Modal` primitive (Architecture AD-13) — no overlay component exists in the codebase today, despite the phase-1 UX spec assuming one does._

### Epic 10: Race Weekend Detail — Context & Prediction
Checking a weekend before it starts gives fans a real track visual, lap records, circuit history, and a plain-language win prediction — the stakes in a few seconds of reading, not a data dump.
**FRs covered:** FR-13, FR-14, FR-15, FR-16

### Epic 11: Profile Clarity
Driver, Constructor, and Circuit profile pages present career stats in clearly grouped, scannable sections instead of one flat list.
**FRs covered:** FR-17

### Epic 12: News Feed Preview
Fans can judge whether a headline is worth leaving the app for, from a thumbnail and one-line snippet, before clicking through.
**FRs covered:** FR-18

## Epic 7: Calendar & Season Overview

Fans landing on the Calendar instantly see what's next — no scrolling past stale races, a filter when they want more, standings always in view, and each race card carries a real track shape and lap-record context instead of repeated boilerplate.

**FRs covered:** FR-1, FR-2, FR-3, FR-4 | **UX-DRs:** UX-DR1, UX-DR2, UX-DR12, UX-DR13 | **Architecture:** AD-5, AD-6, AD-7, AD-8

### Story 7.1: Calendar filter — upcoming by default

As a returning fan,
I want the Calendar to show only upcoming races by default, with an easy way to see past or all races,
So that I can see what's next without wading through races that already happened.

**Acceptance Criteria:**

**Given** I open the Calendar page with no prior filter interaction
**When** the page loads
**Then** only the next Race Weekend and all Race Weekends after it are rendered — zero past Race Weekends shown
**And** the next upcoming Race Weekend remains visually pinned (existing phase-1 treatment)

**Given** I am on the Calendar page
**When** I select "Past"
**Then** only completed Race Weekends are shown
**And** the selected option stays visually indicated at all times

**Given** I am on the Calendar page
**When** I select "All"
**Then** the full season is shown, unfiltered

**Given** the filter control
**When** I navigate it via keyboard
**Then** it behaves as a standard `tablist`/`tab` pattern (arrow-key or Tab between the three options), matching the existing Standings toggle (UX-DR12)

### Story 7.2: Persistent Championship Sidebar

As a returning fan,
I want to see current Drivers' and Constructors' standings right on the Calendar page,
So that I don't have to visit Standings separately just to check who's leading.

**Acceptance Criteria:**

**Given** I am on the Calendar page at a desktop-width viewport
**When** the page loads
**Then** a sticky left-rail sidebar shows current Drivers' and Constructors' Championship standings

**Given** the sidebar is showing standings
**When** I compare its values to the Standings page
**Then** both are identical, sourced from the same query — no separate fetch/cache path (AD-8)

**Given** I switch the Calendar filter or scroll the race list
**When** I look at the sidebar
**Then** it remains visible/reachable and its content doesn't reset

**Given** a mobile-width viewport (`< md`)
**When** the page loads
**Then** the sidebar collapses to a tappable drawer summary above the race list, expanding to the same two groups on tap

**Given** a screen-reader user navigating page landmarks
**When** they reach the sidebar
**Then** it's an independently reachable, labelled region (`aria-label="Championship standings"`)

### Story 7.3: Circuit outline asset coverage (season-wide) + same-origin fetch fix

As a fan browsing any race on the Calendar or viewing the live Track Map,
I want every circuit to show its real track shape reliably in every environment,
So that I get an accurate visual instead of a missing or broken map.

**Acceptance Criteria:**

**Given** the full current-season calendar
**When** `circuit-configs` assets are generated
**Then** every circuit has a `circuit-configs/{circuitId}.json` with a `trackPath` sourced from f1db's `-present` layout (e.g. `monza-7`), not a placeholder shape

**Given** frontend and backend run on different origins (production: Vercel + Render)
**When** any component fetches a circuit-configs asset
**Then** the fetch uses a relative same-origin path (`/circuit-configs/{id}.json`), never `${VITE_API_BASE_URL}`-prefixed

**Given** the existing live Track Map (`TrackMap.tsx`)
**When** it fetches its circuit config
**Then** it uses the same relative-path fix — the pre-existing 404 no longer occurs (AD-6 regression fix)

**Given** a circuit with no available outline data
**When** its config is requested
**Then** the consuming component omits the outline gracefully rather than showing a broken-image state

**Given** the sitewide f1db attribution requirement
**When** any page renders a track outline
**Then** a persistent site-level credit line ("Track outlines: f1db/f1db, CC-BY-4.0") appears exactly once, not per-instance (AD-7)

### Story 7.4: Redesigned Race Weekend card v2

As a fan scanning the Calendar,
I want each race card to show a real track shape and meaningful lap-record context instead of repeated standings,
So that every card tells me something new about that specific race.

**Acceptance Criteria:**

**Given** the Championship Sidebar (Story 7.2) now shows standings
**When** any Race Weekend card renders
**Then** it no longer displays driver/constructor standings data

**Given** a circuit with available outline data (Story 7.3)
**When** a Race Weekend card renders
**Then** it shows a recognizable track outline in a distinct left-side sub-panel, card text content to its right

**Given** a circuit with available historical lap data
**When** a Race Weekend card renders
**Then** it shows an all-time fastest lap and a current/recent-year fastest lap, each with driver name, as two explicit labeled lines

**Given** a circuit without qualifying historical lap data
**When** a Race Weekend card renders
**Then** the fastest-lap block is omitted gracefully, not shown as an error

**Given** the card's existing click-through behavior
**When** I click anywhere on the card
**Then** it still navigates to the Race Weekend detail view, unchanged

**Given** the card's sizing
**When** new content is added
**Then** the card grows taller only — same width/grid column as the phase-1 card

**Given** a track outline visual
**When** a screen reader encounters it
**Then** it has an accessible name identifying the circuit, not a literal shape description (UX-DR13)

## Epic 8: Live Race — Always Something to Watch

The Live Race page is never empty; fans can actively play, scrub, speed-control, and pause a replay of the last completed race.

**FRs covered:** FR-5, FR-6, FR-7, FR-8, FR-9 | **UX-DRs:** UX-DR3, UX-DR4, UX-DR14 | **Architecture:** AD-1, AD-2, AD-3, AD-4

### Story 8.1: Guaranteed non-empty Live Race page

As a fan checking the Live Race page when nothing is currently live,
I want to see the most recently completed race's full data instead of an empty page,
So that there's always something real to look at.

**Acceptance Criteria:**

**Given** no Session is currently live
**When** I open the Live Race page
**Then** the page renders the most recently completed Race's full data set (positions, gaps, tyres, sectors, timeline) using the same components as a live session
**And** it's clearly labelled as a past race (e.g. "Past race — {name}, {date}")

**Given** a genuine API failure (not just "nothing live")
**When** the page loads
**Then** it shows the existing, separately-handled degraded/error state — not confused with fallback mode

**Given** zero races have been completed this season yet
**When** I open the Live Race page
**Then** no Replay bar is shown, and a plain on-brand message names the next race instead of a blank page (UX-DR14)

**Given** this behavior was already specified in MVP FR-16 but not fully realized in shipped behavior
**When** this story is complete
**Then** the gap between original intent and shipped reality is closed — a regression fix, not new scope

### Story 8.2: Race Replay start/stop/restart controls

As a fan viewing a fallback/past race,
I want to start playing it back and restart from the beginning,
So that I can actively watch it unfold instead of only reading a static result.

**Acceptance Criteria:**

**Given** the Live Race page is in fallback mode (Story 8.1)
**When** the page renders
**Then** a Replay control bar is visible, fixed to the bottom of the viewport

**Given** I click Play
**When** playback starts
**Then** per-lap race data is fetched once from a new endpoint (`GET /api/races/{season}/{round}/replay`), and positions/gaps/tyres advance lap by lap using the full live-store setter sequence (`setDrivers` via `normalizeSnapshot`, `setLapChart`, `setFastestSectors`, `setTimeline`) — not just the drivers field (AD-1, AD-2)

**Given** playback is running
**When** I click Restart
**Then** playback returns to lap 1 without a page reload

**Given** the replay data fetch
**When** the backend assembles it
**Then** it's cached with a 7-day TTL, so repeated views of the same race don't re-fetch

### Story 8.3: Replay scrub bar and lap jump

As a fan replaying a past race,
I want to drag a scrub bar or jump straight to a specific lap,
So that I can skip to the moment I care about instead of watching from the start.

**Acceptance Criteria:**

**Given** the Replay bar (Story 8.2)
**When** I drag the scrub bar or select a specific lap
**Then** all dependent views (positions, gaps, tyres) update to that lap's state immediately — not just a timeline marker

**Given** the scrub bar
**When** rendered
**Then** it uses discrete snap-to-lap ticks, never a continuous/analog seek control

**Given** I scrub while playback is running
**When** I release the scrub
**Then** playback state is unaffected — still playing if it was playing, still paused if it was paused (AD-4)

**Given** the scrub bar
**When** I use the keyboard
**Then** Tab focuses it (`role="slider"`), Left/Right step one lap, Home/End jump to first/last lap (UX-DR4)

**Given** the lap readout (`Lap {n} / {total}`)
**When** I scrub or play
**Then** it always reflects the current position live, never lagging behind

### Story 8.4: Replay playback speed control

As a fan replaying a past race,
I want to change the playback speed,
So that I can watch a stretch more quickly or more carefully.

**Acceptance Criteria:**

**Given** the Replay bar
**When** I select a speed (1x, 2x, or 4x)
**Then** playback immediately advances at that rate without restarting

**Given** the speed control
**When** rendered
**Then** it's a button group built from the existing tab-toggle pattern, with the active speed indicated

**Given** a mobile viewport (`< md`)
**When** the Replay bar renders
**Then** the speed group is tucked behind a compact overflow (⋯) button, keeping play/pause and scrub within one-handed reach (NFR2)

### Story 8.5: Replay pause and resume

As a fan replaying a past race,
I want to pause at any point and resume from exactly that point,
So that I don't lose my place if I need to stop and look at something.

**Acceptance Criteria:**

**Given** playback is running
**When** I click Pause
**Then** playback halts at the current lap and stops advancing

**Given** playback is paused
**When** I click Play again
**Then** it resumes from exactly the paused lap

**Given** I background the tab mid-replay
**When** I return within the same page session
**Then** playback is still paused at the same lap (no cross-reload persistence required)

## Epic 9: Fan Card Discovery, Redesign & Multi-Card

Fans get nudged toward the Fan Card feature from Standings, and once there, end up with a genuinely good-looking card — and can hold more than one.

**FRs covered:** FR-10, FR-11, FR-12 | **UX-DRs:** UX-DR5, UX-DR6 | **Architecture:** AD-9, AD-10, AD-13

### Story 9.1: Shared Modal primitive

As a fan interacting with any overlay-triggered flow in the app (starting with the Fan Card prompt),
I want dialogs to behave predictably and accessibly,
So that I can operate them with a mouse, touch, or keyboard alike.

**Acceptance Criteria:**

**Given** a Modal is opened
**When** it renders
**Then** it is portal-rendered above page content, with `role="dialog"` and `aria-modal="true"`

**Given** a Modal is open
**When** I press Escape or click the backdrop
**Then** the Modal closes

**Given** a Modal is open
**When** I press Tab repeatedly
**Then** focus is trapped within the Modal's interactive elements — it does not escape to page content behind it

**Given** a Modal closes
**When** focus returns
**Then** it returns to the element that triggered the Modal's opening

**Given** no modal/dialog primitive exists anywhere in the codebase today (reality-check per Architecture AD-13)
**When** this story is complete
**Then** this is the sole sanctioned overlay mechanism going forward — no ad hoc fixed-position divs for modal-like UI elsewhere in the app

### Story 9.2: Fan Card creation prompt

As a fan who hasn't created a Fan Card yet,
I want to be invited to create one while I'm on the Standings page,
So that I discover the feature instead of never finding it.

**Acceptance Criteria:**

**Given** I have zero Fan Cards
**When** I visit the Standings page
**Then** a prompt, built on the shared Modal (Story 9.1), invites me to create a Fan Card

**Given** I already hold at least one Fan Card
**When** I visit the Standings page
**Then** the prompt does not appear

**Given** the prompt is shown
**When** I choose to proceed
**Then** it launches directly into the existing 3-step Fan Card wizard, not a separate lightweight picker

**Given** the prompt is shown
**When** I dismiss it ("Not now")
**Then** Standings continues to work normally, and the prompt does not reappear on my next few visits

### Story 9.3: Fan Card visual redesign

As a fan who has created a Fan Card,
I want it to show my chosen driver's photo, autograph, team logo, and team principal,
So that the card feels like a real, personal keepsake rather than a placeholder.

**Acceptance Criteria:**

**Given** a Fan Card for a supported driver/constructor pairing
**When** it renders
**Then** it shows the driver's photo, a stylized-signature-font autograph, the constructor's team logo/color rule, and the team principal's name — alongside everything the MVP card already showed

**Given** driver photos and team-principal names are hand-curated static assets, not a new external API (AD-10)
**When** the roster is set up
**Then** all ~20 current drivers / 10 constructors have assets present

**Given** a driver whose photo asset is missing (e.g. a mid-season roster change not yet curated)
**When** their card renders
**Then** it falls back to an initials placeholder — never a broken-image state, and card creation is never blocked

**Given** the card's visual treatment
**When** it renders
**Then** it uses the portrait 5:7 trading-card aspect ratio, with constructor color reduced to a 4px top rule only — never a full-bleed team-color background (UX-DR5)

**Given** the card
**When** exported
**Then** it remains exportable as a client-side-generated image, unchanged from the MVP

### Story 9.4: Multiple Fan Cards per user

As a fan who follows more than one driver,
I want to create and keep several Fan Cards in the same browser,
So that I don't have to pick just one favorite.

**Acceptance Criteria:**

**Given** I already have one or more Fan Cards
**When** I create a new one
**Then** it is added alongside my existing cards — none are overwritten or deleted

**Given** I have N Fan Cards
**When** I view the Fan Card page
**Then** all N are visible in a grid (auto-fill, not a carousel/pagination), plus an "Add new card" tile

**Given** the existing MVP single-card storage (`f1app__fanCard__v1`, Zustand `persist`)
**When** this story migrates it
**Then** it uses `persist`'s own `version`/`migrate` mechanism to wrap the old single-card state into a one-item collection — no key rename, and an existing user's pre-upgrade card is preserved as their first card (AD-9)

**Given** zero Fan Cards
**When** I view the Fan Card page
**Then** the "Add new card" tile is the primary/only tile shown, framed as an empty state rather than a locked feature

## Epic 10: Race Weekend Detail — Context & Prediction

Checking a weekend before it starts gives fans a real track visual, lap records, circuit history, and a plain-language win prediction.

**FRs covered:** FR-13, FR-14, FR-15, FR-16 | **UX-DRs:** UX-DR7, UX-DR8, UX-DR9, UX-DR13, UX-DR15 | **Architecture:** AD-5, AD-6, AD-7 (reuses Story 7.3's asset pipeline)

### Story 10.1: Track layout visualization

As a fan checking a Race Weekend before it starts,
I want to see the circuit's real track shape in detail,
So that I understand the track's character, not just a name.

**Acceptance Criteria:**

**Given** a circuit with available outline data (from Story 7.3's circuit-configs coverage)
**When** I open its Race Weekend Detail page
**Then** it renders a recognizable track outline, at a larger/more detailed treatment than the FR-4 card-level version — same underlying `circuit-configs/{circuitId}.json` asset, no separate file (AD-5)

**Given** a circuit without a sourced outline asset
**When** I open its Race Weekend Detail page
**Then** the outline is omitted gracefully (or a generic placeholder), not a broken-image state

**Given** the track outline
**When** rendered
**Then** it fetches via the same relative same-origin path fixed in Story 7.3 (AD-6)

**Given** the track outline visual
**When** a screen reader encounters it
**Then** it has an accessible name identifying the circuit (UX-DR13)

### Story 10.2: Track lap record context

As a fan checking a Race Weekend before it starts,
I want to see the all-time and this-year's fastest lap at this circuit,
So that I have real performance context, not just the track shape.

**Acceptance Criteria:**

**Given** a circuit with lap record data
**When** I open its Race Weekend Detail page
**Then** a Track Records section (separate from the track-layout panel, per UX-DR8) shows the all-time fastest lap and the current/most-recently-completed-year's fastest lap, each with driver name

**Given** a driver name in the Track Records section
**When** I click it
**Then** it links through to that driver's profile page

**Given** a circuit lacking full record data
**When** the page renders
**Then** it shows whatever data exists rather than an error state

### Story 10.3: Track historical data and records

As a fan checking a Race Weekend before it starts,
I want to see the circuit's history — past winners and core stats,
So that I get a fuller picture of what this track means.

**Acceptance Criteria:**

**Given** a circuit with historical data (reusing the dataset already scoped for MVP's Circuit Profile page)
**When** I open its Race Weekend Detail page
**Then** a Circuit History section shows four stat tiles (length, corners, DRS zones, first F1 race year) and a past-winners list (year, driver, team)

**Given** a driver name in the past-winners list
**When** I click it
**Then** it links through to that driver's profile page

**Given** a circuit with only partial historical data
**When** the page renders
**Then** it shows whatever data exists (e.g. stat tiles without a full winners list) rather than an error state

### Story 10.4: Simplified win prediction

As a fan checking a Race Weekend the evening before qualifying or after it,
I want a plain-language read on who's likely to win and why,
So that I understand the stakes without parsing a probability table.

**Acceptance Criteria:**

**Given** qualifying results are available for this Race Weekend (existing MVP win-probability calculation)
**When** I open its Race Weekend Detail page
**Then** a Win Prediction callout names a likely winner and gives at least one concrete plain-language reason — no percentages shown by default

**Given** the Win Prediction callout
**When** rendered
**Then** it uses the `accent-editorial` bordered-card treatment (UX-DR7, UX-DR15) — the first non-chrome use of that token, never to be reused for any team-scoped or broadcast-coded value

**Given** the callout
**When** I activate its toggle (`aria-expanded`/`aria-controls`)
**Then** it expands to reveal the original MVP raw per-grid-slot win-probability table beneath a dashed divider — collapsed by default

**Given** no qualifying session has run yet for this Race Weekend
**When** I open its Race Weekend Detail page
**Then** the Win Prediction callout is simply absent, not a placeholder or error

## Epic 11: Profile Clarity

Driver, Constructor, and Circuit profile pages present career stats in clearly grouped, scannable sections instead of one flat list.

**FRs covered:** FR-17 | **UX-DRs:** UX-DR11

### Story 11.1: Structured career stats presentation

As a fan viewing a Driver, Constructor, or Circuit profile,
I want career stats grouped into clear sections instead of one long list,
So that I can scan for what I care about quickly.

**Acceptance Criteria:**

**Given** a Driver or Constructor profile page
**When** it renders
**Then** career statistics and history render in at least two visually distinct stacked sections (e.g. season-by-season results, career totals, head-to-head where applicable) rather than one flat list

**Given** these sections
**When** viewed at a 360px-wide mobile viewport
**Then** no section requires horizontal scrolling

**Given** the underlying data
**When** this story is implemented
**Then** it is unchanged from MVP FR-20–FR-22 — this story governs presentation/structure only, not new data

**Given** the sections
**When** rendered
**Then** they appear as stacked sections on one scrollable page, not tabs

## Epic 12: News Feed Preview

Fans can judge whether a headline is worth leaving the app for, from a thumbnail and one-line snippet, before clicking through.

**FRs covered:** FR-18 | **UX-DRs:** UX-DR10 | **Additional:** NFR1 (Accessibility CI gate, AD-12)

### Story 12.1: News item preview

As a fan skimming the News Feed,
I want to see a photo and short snippet for each headline,
So that I can judge relevance before clicking through to the source site.

**Acceptance Criteria:**

**Given** a news item whose source feed provides an enclosure image and description
**When** it renders in the News Feed
**Then** it shows a thumbnail (left, list-row style) and a one-line snippet, alongside the existing title, source, and publish time

**Given** the backend feed-parsing pipeline (`NewsFeedService`, `CodeHollow.FeedReader`)
**When** it processes each feed item
**Then** it extracts `imageUrl` (from the item's enclosure, if present) and `snippet` (from the item's Description, truncated) at parse time onto the existing `Models/NewsItem.cs` — no new external data source or second network hop (AD-11)

**Given** a news item whose source lacks an image or snippet
**When** it renders
**Then** it degrades gracefully to title-only, not a broken-image placeholder

**Given** a news item
**When** I click it
**Then** it still redirects to the original source article, unchanged from the MVP

### Story 12.2: Automated accessibility gate

As the developer maintaining F1_poc,
I want automated accessibility checks running in CI against the redesigned pages,
So that WCAG AA regressions are caught before they ship, not discovered manually.

**Acceptance Criteria:**

**Given** the existing Playwright E2E suite
**When** `@axe-core/playwright` assertions are added
**Then** they run against Calendar, Live Race, Standings, Fan Card, and Race Weekend Detail — the five pages named in the PRD's SM-2 success metric

**Given** these assertions
**When** run in CI
**Then** a score below the ≥95 threshold, or any critical/serious axe violation, fails the build

**Given** this gate
**When** wired in
**Then** it replaces phase-1's posture of no automated accessibility check with a real enforced floor (AD-12)
