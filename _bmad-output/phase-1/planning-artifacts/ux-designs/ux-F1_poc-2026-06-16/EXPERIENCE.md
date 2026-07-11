---
name: F1_poc
status: final
sources:
  - '_bmad-output/planning-artifacts/prds/prd-F1_poc-2026-06-15/prd.md'
  - '_bmad-output/planning-artifacts/prds/prd-F1_poc-2026-06-15/addendum.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/planning-artifacts/epics.md'
updated: 2026-06-16
---

# F1_poc — Experience Spine

## Foundation

Single-surface responsive web, no native app. Desktop is the primary target; mobile gets a deliberately adapted (not just shrunk) Live Race page. No component library — every interactive element (nav, tabs, dropdowns, the Fan Card wizard, the driver search/compare picker) is hand-built, which means this spine's Accessibility Floor is a hard build requirement, not an inherited default. `DESIGN.md` is the visual identity reference; this spine is the experience. No auth for the POC — there is no per-user account boundary, no private surface, no login flow anywhere in this IA.

## Information Architecture

| Surface | Reached from | Purpose |
|---|---|---|
| Race Calendar | Top nav (default landing page) | Season schedule; next Race Weekend pinned; FR-1/FR-2 |
| Race Weekend Detail | Calendar card click | All sessions, timezone toggle, last year's winner, championship delta, win probability; FR-3–FR-6 |
| Live Race | Top nav | Real-time cockpit during a session; falls back to the most recent race when nothing is live; FR-7–FR-16 |
| Standings | Top nav | Drivers/Constructors toggle, trajectory chart, Season Wrapped; FR-17–FR-19 |
| Circuit Profile | Any circuit name, anywhere | Track layout, lap record, past winners, stats; FR-20 |
| Driver Profile | Any driver name, anywhere | Career totals, constructor history, points chart; FR-21 |
| Head-to-Head | Top nav | Two-driver comparison with additive season/circuit filters; FR-22 |
| News Feed | Top nav | Aggregated F1 RSS headlines; FR-23 |
| Fan Card | Top nav, also prompted on first visit | Setup wizard + shareable card; FR-25 |

Persistent top nav bar across all pages, collapsing to a hamburger menu on mobile (`< md`). The race-weekend streak counter (FR-24) is a small always-visible badge inside the top nav — it does not get its own page. Modal/overlay surfaces (Fan Card wizard) stack one level deep, never two.

→ Composition reference: `mockups/calendar.html`, `mockups/live-race.html`, `mockups/standings.html`, `mockups/fancard-wizard.html`. Race Weekend Detail, Circuit Profile, Driver Profile, Head-to-Head, and News Feed are spine-only by design (no mock) — the Component/State Patterns tables below are the contract for those surfaces. Spine wins on conflict with any mock.

## Voice and Tone

Microcopy. Brand voice and aesthetic posture live in `DESIGN.md`.

| Do | Don't |
|---|---|
| "Next race: Italian GP, 3 days." | "🏁 Race week is here!!" |
| "Couldn't reach the server — try refreshing." | "Error 500: Internal Server Error" |
| "First race at this circuit." | "No historical data available." |
| "Provisional — recalculated if the race ended now." | "LIVE CHAMPIONSHIP IMPACT!!!" |
| Plain, fan-to-fan, no marketing fluff, no emoji in UI copy. | Hype language, exclamation marks, gamified streak-loss messaging. |

## Component Patterns

Behavioral. Visual specs live in `DESIGN.md.Components`.

| Component | Use | Behavioral rules |
|---|---|---|
| Gap list row (dense) | Live Race | Single line per driver: position, team chip + driver code, gap (battle-highlighted under 1s), tyre dot + stint laps, provisional championship delta. Never wraps to a second line. Re-sorts automatically as race order changes between snapshots. |
| Race Weekend card | Calendar | Whole card is clickable → detail view. The next upcoming weekend is visually pinned above the rest; past weekends remain scrollable below. |
| Standings toggle | Standings | Tab switch between Drivers/Constructors is instant — no reload, no route change. |
| Driver/circuit link | Anywhere a name appears (cards, gap list, standings, profiles) | Opens the relevant profile page. Same visual and behavioral treatment everywhere a name is clickable — no surface gets a different link style. |
| Timezone toggle | Race Weekend Detail | Two-state switch (Track / Local). All session times update immediately on flip; Local is the default on first load. |
| Fan Card wizard | Fan Card | 3-step picker (Driver → Constructor → Circuit). Re-enterable at any time to change picks, not just on first visit. |
| Head-to-head picker | Head-to-Head | Two searchable dropdowns + optional season/circuit filters (additive, both can apply at once). Comparison recalculates on every filter change. |
| Stale value | Live Race (any field) | Dimmed text + literal `~` prefix the instant a value misses its join-tolerance window. Never silently shows a confidently wrong number. |
| Live badge | Live Race header | Outlined pill, pulses subtly to signal "this page is updating," not just static text. |

## State Patterns

| State | Surface | Treatment |
|---|---|---|
| Cold load | Calendar, Standings, Profiles | Skeleton cards matching the final layout; resolves in place once data arrives. |
| Ergast unavailable | Calendar, Standings, Profiles, Head-to-Head | Clear loading/error message in place of an empty page — never a blank screen. |
| No live session | Live Race | Falls back to the most recently completed race, clearly labelled "Past race — {name}, {date}." Never an empty page. |
| Stream degraded | Live Race | live → stale → fallback-to-last-race per the architecture's state machine. A small connection-status indicator (connected / reconnecting / disconnected) is always visible, never an abrupt jump from live to offline. |
| No qualifying yet | Race Weekend Detail | Win probability widget is simply absent — no placeholder, no "coming soon." |
| No prior result at this circuit | Race Weekend Detail | "First race at this circuit." replaces the last-year-winner field. |
| Season in progress | Standings | Season Wrapped section is absent entirely, not a disabled/teaser placeholder. |
| Track map not yet calibrated for this circuit | Live Race | "Track map unavailable for this circuit." in place of the map panel; the rest of the page (gap list, chart, etc.) still functions normally. |
| All news feeds down | News Feed | "No news available right now." Partial feed failure still shows headlines from whichever sources succeeded — one broken feed never blocks the others. |
| Fan Card not yet set up | Fan Card (first visit) | Empty state prompts the setup wizard directly — no "feature locked" framing. |
| Streak reset | Top nav badge | The counter just shows the lower number on the next visit — no "you lost your streak" messaging (matches Voice and Tone: no gamified loss language). |
| No filters applied | Head-to-Head | Comparison defaults to the full all-time dataset across every circuit, explicitly labelled as such so scope is never ambiguous. |

## Interaction Primitives

- Mouse/keyboard is primary (desktop-first); touch is a first-class fallback given "mobile acceptable," not an afterthought — every hover-revealed affordance has a tap-reachable equivalent.
- All interactive elements (nav links, tabs, toggles, wizard steps, dropdowns) are reachable via `Tab` and operable via `Enter`/`Space`, with a visible focus ring.
- No keyboard-shortcut surface (no command palette, no vim-style navigation) — this is a casual fan app, not a power-user tool; standard tab/enter/escape is the full keyboard contract.
- `Esc` closes the topmost open overlay (Fan Card wizard) — modal stacks never exceed one level, so this is unambiguous.

## Accessibility Floor

Behavioral; visual contrast lives in `DESIGN.md` (Dark Editorial's text-on-background pairs are high-contrast by inspection; this is a proportionate floor for a hobby project, not a formal WCAG AA certification target).

- Every interactive element is keyboard-operable with a visible focus ring — an explicit build requirement here since the component-base decision was fully custom components, not a headless library that provides this for free.
- Race-control events (Safety Car, VSC, red flag, fastest lap) are announced via an `aria-live="polite"` region the moment they occur, not just as a visual marker on the Event Timeline — so the events that matter reach a non-sighted user without parsing a continuous 1-2s snapshot stream.
- `prefers-reduced-motion` is respected: the track-map interpolation glow and the live-badge pulse drop to static/instant rendering when set.
- Standings render as a real `<table>` element (not styled divs); one `<h1>` per page; heading hierarchy follows document order.
- **Known, explicit gap (logged, not accidental):** mini-sector and tyre-compound color coding is color-only, matching real F1 broadcast graphics, with no colorblind-safe secondary cue for this POC.

## Responsive & Platform

| Breakpoint | Behavior |
|---|---|
| Desktop (primary, `≥ md`) | Full layout: Live Race shows gap list, track map, lap chart, sector board, and event timeline simultaneously in a two-column grid. |
| Mobile (`< md`) | Top nav collapses to a hamburger menu. Live Race becomes tabbed: Gap List (with inline tyre tracker and championship delta) is the default/first tab; Track Map, Lap Time Chart, Sector Board, and Event Timeline become swipeable secondary tabs. |

F1_poc is responsive web only — no native app, no platform-specific gesture conventions beyond standard tap/swipe/scroll.

## Inspiration & Anti-patterns

- **Lifted from F1 broadcast graphics:** mini-sector purple/green/yellow/white, tyre compound colors, the "gap to car ahead" convention — fans already have this vocabulary memorized; the app speaks their existing visual language rather than inventing a new one.
- **Lifted from data-journalism/editorial dashboards:** the calm dark shell, restrained team-color use (dots/badges, not full-row paint) everywhere except the one dense table, generous whitespace on Calendar/Standings/Profiles.
- **Rejected — Cockpit HUD as the default register:** monospace numerics, sharp corners, glowing amber accents were explicitly tried and rejected for the whole app — too intense to sit with for a two-hour race; survives only as the Gap List's density pattern, not its visual language.
- **Rejected — headless component library:** chose fully custom components over Radix/shadcn-style primitives, trading faster accessible-by-default behavior for full control — the Accessibility Floor above is the explicit compensating responsibility this creates.
- **Rejected — colorblind-safe secondary cues:** considered and explicitly declined for the POC in favor of broadcast-standard color-only coding (see Accessibility Floor).
- **Rejected — gamified streak-loss messaging:** the streak counter (FR-24) just resets quietly; no "you lost your streak!" guilt copy, consistent with Voice and Tone.

## Key Flows

### Flow 1 — Race-day cockpit (a race-day fan, Sunday afternoon, 12 minutes into the race)

1. The fan opens f1poc.app and lands on Live Race.
2. The page connects to the SignalR hub; within 10 seconds the gap list populates with all 20 drivers in current order — the dense single-line rows let them see the whole field at a glance, no scrolling.
3. They notice two drivers battling — the gap cell is highlighted, sub-one-second — and check the tyre dot on the car ahead: Medium, lap 26, getting old.
4. A Safety Car triggers. The Event Timeline gets a new marker and an `aria-live` announcement fires. The fan glances at their favourite driver's row: "Provisional — if race ended now: +6 pts to P2."
5. **Climax:** Racing resumes. The fan watches the gap list re-sort live through the restart, the lap chart ticks forward, and the sector board flashes purple as a fastest sector falls — all without leaving the page or refreshing.

Failure: the OpenF1 stream drops mid-race (an 8-minute outage, the kind that hit Monaco 2024). The page goes live → stale (rows dim, `~` prefix appears) → fallback-to-last-race if the outage persists, never an empty or broken page. When the stream recovers, 3-5 good snapshots bring it back to live, debounced so it doesn't flicker.

### Flow 2 — Planning the weekend (a casual fan, Tuesday evening, checking the next race)

1. The fan opens the app out of habit and lands on Calendar — the default page when nothing is live.
2. The next Race Weekend card is visually pinned at the top: circuit, flag, date range, and the top-3 championship standings at a glance.
3. They tap the card to open the detail view, see every session listed in Track Time by default, then flip the toggle to Local Time — every session time updates immediately.
4. They scroll to see last year's winner at this circuit and the current championship gap between the top two drivers.
5. **Climax:** Qualifying happened earlier that day — the win probability widget is now visible, showing each driver's grid slot alongside a calculated win chance. The fan picks who to root for based on the numbers and closes the tab without checking three other sites.

Failure: it's a brand-new circuit with no prior race. "First race at this circuit." replaces the last-year-winner field — no broken layout, no missing-data error.

### Flow 3 — Settling a debate (a history enthusiast, post-race, comparing two drivers)

1. After watching a street-circuit race, the fan wonders whether one driver is statistically better than another at street circuits specifically.
2. They open Head-to-Head and use the searchable dropdown to pick the first driver, then the second.
3. They add a "circuit type: street" filter, additive with anything else set.
4. **Climax:** The side-by-side stat card renders — qualifying average, race finish average, DNFs, points, fastest laps, wins — scoped exactly to street circuits. The debate gets settled with real numbers from decades of Ergast data.

Failure: no filter applied — the comparison defaults to the full all-time dataset across every circuit, explicitly labelled as such so the fan always knows what scope they're looking at.
