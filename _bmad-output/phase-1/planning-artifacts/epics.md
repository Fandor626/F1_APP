---
stepsCompleted: [1, 2, 3, 4]
status: complete
completedAt: '2026-06-16'
inputDocuments:
  - '_bmad-output/planning-artifacts/prds/prd-F1_poc-2026-06-15/prd.md'
  - '_bmad-output/planning-artifacts/prds/prd-F1_poc-2026-06-15/addendum.md'
  - '_bmad-output/planning-artifacts/architecture.md'
---

# F1_poc - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for F1_poc, decomposing the requirements from the PRD and Architecture into implementable stories. No dedicated UX Design document exists for this project.

## Requirements Inventory

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

### NonFunctional Requirements

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

### Additional Requirements

- **Starter templates** (impacts Epic 1 Story 1): Frontend — Vite + React 19 + TypeScript via `npm create vite@latest frontend -- --template react-ts`. Backend — ASP.NET Core 10 Web API via `dotnet new webapi -n F1App.Api --framework net10.0`, plus `dotnet new xunit -n F1App.Api.Tests`.
- **Solution structure**: independent `frontend/` (Vite React SPA) and `backend/F1App.Api/` + `backend/F1App.Api.Tests/` projects under one solution; no docker-compose for the POC.
- **Frontend library stack**: `react-router-dom` v7, `@tanstack/react-query` v5, `zustand`, `@microsoft/signalr`, `recharts`, `tailwindcss` v4, `html-to-image`, `zod`, `vitest` + `@testing-library/react`, `msw`, `playwright`.
- **Backend library stack**: `Microsoft.AspNetCore.SignalR`, `Microsoft.Extensions.Hosting` (`IHostedService`), `Microsoft.Extensions.Caching.Memory`, `Microsoft.Extensions.Http` (`IHttpClientFactory`), `CodeHollow.FeedReader`, `xunit` + `Moq`, `WireMock.Net`.
- **Real-time pipeline**: a single `RaceDataOrchestrator` hosted service owns 5 OpenF1 polling loops (`/position`, `/intervals`, `/car_data`, `/race_control`, `/pit`), assembles one `RaceStateSnapshot`, and pushes it via SignalR every 1-2 seconds as a full-state broadcast (no delta/patch protocol).
- **Ergast caching**: `IMemoryCache` with TTL tiers — historical results 7 days, season schedule 24 hours, standings 1 hour, circuit/driver metadata 24 hours, qualifying results 6 hours — plus proactive cache warm-up on startup (season schedule, standings, circuit metadata).
- **Per-circuit track mapping**: requires a runtime-loaded affine transform config asset at `public/circuit-configs/{circuitId}.json` (scale/rotate/translate + viewBox); start with 1-2 calibrated circuits for the POC, expand incrementally post-POC with no code changes.
- **API conventions**: kebab-case plural REST routes (`/api/races`, `/api/drivers`, ...), camelCase JSON globally via `JsonNamingPolicy.CamelCase`, ProblemDetails (RFC 7807) for all error responses, no `/api/v1/` prefix for the POC.
- **`TimeProvider` injection**: mandatory for all hosted services with time-dependent logic (enables deterministic testing of timestamp-age checks).
- **Required environment/config values**: `VITE_API_BASE_URL`, `VITE_SIGNALR_HUB_URL` (frontend `.env.local`); `ErgastBaseUrl`, `OpenF1BaseUrl`, `AllowedOrigins` (`http://localhost:5173`), `JoinToleranceMs` (500) (backend `appsettings.Development.json`).
- **CORS / local dev**: CORS restricted to `localhost:5173` for the POC; frontend and backend run as independent local processes (ports 5173 / 5000), no docker-compose.
- **RSS sources to integrate**: `formula1.com/en/latest/all.news.rss`, `autosport.com/rss/f1/news`, `racefans.net/feed` — proxied through the C# backend to avoid CORS, with per-feed (not per-cycle) error isolation.
- **Win probability calculation**: `WinProbabilityService` runs once after qualifying results are published (historical grid-slot win rate, championship standing weight, last-3-race form weight) and is cached until the next qualifying session — no live data dependency.
- **Post-POC deployment target**: Vercel (frontend) + Render (backend Docker container, free tier) — already informs the SignalR reconnect policy (handles Render free-tier sleep cycles) designed now.

### UX Design Requirements

None — no dedicated UX Design document exists for this project. Interaction and layout details are captured directly in the PRD's feature descriptions (§4) and the Architecture's component/file mapping.

### FR Coverage Map

FR-1: Epic 1 - Season race list
FR-2: Epic 1 - Race Weekend card with top-3 standings
FR-3: Epic 1 - Race Weekend detail view (sessions)
FR-4: Epic 1 - Contextual detail data (last year winner, championship delta)
FR-5: Epic 1 - Timezone toggle
FR-6: Epic 1 - Pre-race win probability widget
FR-7: Epic 3 - Animated live track map
FR-8: Epic 3 - Mini-sector colour coding
FR-9: Epic 2 - Live gap list
FR-10: Epic 2 - Live tyre tracker
FR-11: Epic 3 - Pit window estimator
FR-12: Epic 2 - Live lap time chart
FR-13: Epic 3 - Fastest Sector board
FR-14: Epic 3 - Race event timeline
FR-15: Epic 2 - Live championship impact tracker
FR-16: Epic 2 - Fallback to last race
FR-17: Epic 4 - Standings page with toggle
FR-18: Epic 4 - Championship trajectory chart
FR-19: Epic 4 - F1 Season Wrapped
FR-20: Epic 5 - Circuit profile page
FR-21: Epic 5 - Driver career profile page
FR-22: Epic 5 - Driver head-to-head comparison
FR-23: Epic 6 - F1 news feed
FR-24: Epic 6 - Race weekend streak
FR-25: Epic 6 - My F1 Fan Card

## Epic List

### Epic 1: Project Foundation & Race Calendar
Users can open the app and see the season's race calendar — every Race Weekend, the next one highlighted, full session schedules in local time, last year's result at that circuit, the championship gap, and a post-qualifying win probability widget. Also scaffolds the frontend and backend projects (Story 1.1) and establishes the Ergast client, caching, and query-key conventions used by every later epic.
**FRs covered:** FR-1, FR-2, FR-3, FR-4, FR-5, FR-6

### Epic 2: Live Race Core Experience
Users can follow a live Grand Prix in real time — current race order, gaps, tyre stints, lap-by-lap timing, and a live championship-impact readout — without needing F1 TV. When no session is live, they see the most recently completed race in the same view. Establishes the real-time pipeline: RaceDataOrchestrator, OpenF1 polling/joining, SignalR hub + client, RaceStateSnapshot, and the live→stale→fallback state machine.
**FRs covered:** FR-9, FR-10, FR-12, FR-15, FR-16

### Epic 3: Live Race Visual Enrichment
Adds the broadcast-quality layer on top of the core feed: an animated track map with mini-sector colour coding, a pit-window estimator, a fastest-sector board, and a lap-by-lap event timeline. Extends Epic 2's snapshot/pipeline; depends on Epic 2 being in place.
**FRs covered:** FR-7, FR-8, FR-11, FR-13, FR-14

### Epic 4: Championship & Standings
Users can see full Drivers'/Constructors' standings, how the championship developed across the season, and — once the season ends — a shareable Season Wrapped recap. Pure Ergast-driven; introduces the html-to-image export pattern reused by Epic 6's Fan Card.
**FRs covered:** FR-17, FR-18, FR-19

### Epic 5: Deep Dive Profiles
Users can explore any circuit's or driver's history in depth, and compare two drivers head-to-head across seasons/circuits. Pure Ergast historical data; reuses the trajectory-chart visual style introduced in Epic 4.
**FRs covered:** FR-20, FR-21, FR-22

### Epic 6: Fan Engagement
Users build a personal F1 identity in the app — a streak for following live sessions, a shareable Fan Card with their picks and stats, and a news feed to stay current between races. Streak depends on Epic 2's live-session detection; Fan Card depends on Epic 4's standings for current stats — hence last.
**FRs covered:** FR-23, FR-24, FR-25

## Epic 1: Project Foundation & Race Calendar

Users can open the app and see the season's race calendar — every Race Weekend, the next one highlighted, full session schedules in local time, last year's result at that circuit, the championship gap, and a post-qualifying win probability widget. Also scaffolds the frontend and backend projects and establishes the Ergast client, caching, and query-key conventions used by every later epic.

### Story 1.1: Project Scaffolding & Health Check

As a developer,
I want the frontend and backend projects scaffolded and able to talk to each other,
So that all later feature work has a working foundation to build on.

**Acceptance Criteria:**

**Given** a fresh checkout
**When** the frontend is scaffolded via `npm create vite@latest frontend -- --template react-ts` and dependencies installed
**Then** the dev server starts at `localhost:5173` and renders a default page
**And** the backend is scaffolded via `dotnet new webapi -n F1App.Api --framework net10.0` matching the architecture's project tree (`Controllers/`, `Services/`, `Clients/`, `Models/`, `Hubs/`, `Dtos/`), plus `F1App.Api.Tests`

**Given** both servers running
**When** the frontend calls a `/api/health` endpoint via TanStack Query
**Then** it receives and displays a successful response, confirming CORS (`AllowedOrigins: http://localhost:5173`) works end to end
**And** `JsonNamingPolicy.CamelCase` is configured globally in `Program.cs` from the start

### Story 1.2: Season Race List

As a casual fan,
I want to see all Race Weekends in the current season as a scrollable list with the next one highlighted,
So that I always know what's coming up.

**Acceptance Criteria:**

**Given** the calendar page loads
**When** the season schedule is fetched from Ergast (via `ErgastClient`, 24h cache TTL)
**Then** all Race Weekends for the current season render in chronological order
**And** the next upcoming Race Weekend's card is visually distinguished (pinned/highlighted) from the rest
**And** past Race Weekends remain visible and scrollable below it

**Given** the Ergast API is unavailable
**When** the calendar page loads
**Then** a clear loading/error state is shown instead of an empty page

### Story 1.3: Race Weekend Card Championship Snippet

As a casual fan,
I want each Race Weekend card to show the circuit, date, and current top-3 standings,
So that I get championship context without leaving the calendar.

**Acceptance Criteria:**

**Given** a Race Weekend card
**When** rendered
**Then** it shows circuit name, country flag, weekend date range, and main race date/time
**And** it shows the top 3 Drivers' Championship standings (name + points) and top 3 Constructors (name + points), sourced from current standings data (1h cache TTL)

### Story 1.4: Race Weekend Detail View — Sessions

As a casual fan,
I want to click a Race Weekend card and see every session with its time,
So that I know exactly when to tune in.

**Acceptance Criteria:**

**Given** a Race Weekend card is clicked
**When** the detail view opens
**Then** every Session is listed in order with date and time

**Given** a Sprint weekend
**When** the detail view renders
**Then** Sprint Shootout and Sprint are shown in place of FP2/FP3, correctly labelled

**Given** a standard weekend
**When** the detail view renders
**Then** FP1/FP2/FP3/Qualifying/Race show in the correct order

### Story 1.5: Contextual Detail Data — Last Year's Winner & Championship Delta

As a history-curious fan,
I want the detail view to show last year's winner at this circuit and the current championship gap,
So that I have context heading into the weekend.

**Acceptance Criteria:**

**Given** a circuit with a prior-year result
**When** the detail view loads
**Then** last year's winner (driver, team, time/gap) is displayed

**Given** a circuit with no prior F1 race
**When** the detail view loads
**Then** the field is omitted or labelled "First race at this circuit"

**Given** current standings
**When** the detail view loads
**Then** the Championship Delta between the top two Drivers is displayed

### Story 1.6: Timezone Toggle

As a fan checking session times,
I want to toggle between Track Time and Local Time,
So that I know exactly when to watch in my own timezone.

**Acceptance Criteria:**

**Given** the detail view
**When** the timezone toggle is switched
**Then** all Session times update immediately between Track Time and browser-detected Local Time

**Given** the detail view loads
**When** no toggle interaction has occurred
**Then** Local Time is the default displayed state

### Story 1.7: Pre-Race Win Probability Widget

As a fan deciding who to watch,
I want to see each driver's win probability after qualifying,
So that I know who's likely to win before the race starts.

**Acceptance Criteria:**

**Given** qualifying results are not yet published for a Race Weekend
**When** viewing the detail view
**Then** the win probability widget is not shown

**Given** qualifying results are published
**When** the detail view loads
**Then** each driver's grid position shows alongside a win probability (%) computed by `WinProbabilityService` from grid-slot win rate at this circuit, championship standing, and recent form
**And** probabilities sum to approximately 100% across all drivers
**And** the calculation runs once after qualifying and is cached until the next qualifying session

## Epic 2: Live Race Core Experience

Users can follow a live Grand Prix in real time — current race order, gaps, tyre stints, lap-by-lap timing, and a live championship-impact readout — without needing F1 TV. When no session is live, they see the most recently completed race in the same view. Establishes the real-time pipeline: RaceDataOrchestrator, OpenF1 polling/joining, SignalR hub + client, RaceStateSnapshot, and the live→stale→fallback state machine.

### Story 2.1: Live Gap List

As a race-day fan,
I want to see all drivers in current race order with real-time gaps to the car ahead,
So that I can follow the race battle without F1 TV.

**Acceptance Criteria:**

**Given** a live session is in progress
**When** the live race page loads
**Then** it connects to the SignalR hub and receives `RaceStateSnapshot` broadcasts every 1-2 seconds

**Given** a snapshot is received
**When** rendered
**Then** all drivers render in current race order with their Gap to the car ahead
**And** gaps under 1 second are visually highlighted as active battles
**And** the list re-sorts automatically as race order changes between snapshots
**And** OpenF1 endpoint joins use the configurable 500ms timestamp tolerance window; fields that miss the window keep their previous value with an `isStale` flag rather than showing a wrong number

### Story 2.2: Live Tyre Tracker

As a race-day fan,
I want to see each driver's current tyre compound and stint length in the gap list,
So that I understand their strategy in real time.

**Acceptance Criteria:**

**Given** the gap list
**When** a driver entry renders
**Then** it shows the current Tyre Compound as a colour-coded circle and the number of laps on the current Stint

**Given** a tyre data refresh from OpenF1
**When** received
**Then** compound and stint count update accordingly

### Story 2.3: Live Lap Time Chart

As a race-day fan,
I want a chart of each driver's lap times as the race progresses,
So that I can spot who's pushing and who's struggling.

**Acceptance Criteria:**

**Given** the live race page
**When** a lap completes for any driver
**Then** the chart updates with that driver's lap time at the corresponding lap number

**Given** a pit-out lap
**When** plotted
**Then** it renders as a visible upward spike

**Given** a hover on a data point
**When** triggered
**Then** it shows the exact lap time and gap to the race's fastest lap so far
**And** the chart initializes at lap 1 and grows lap by lap, with all drivers as separate coloured lines

### Story 2.4: Live Championship Impact Tracker

As a fan following championship stakes,
I want to see how each driver's current race position would affect their points gap to their nearest rival,
So that I understand what's on the line right now.

**Acceptance Criteria:**

**Given** the gap list
**When** a driver entry renders
**Then** it shows a Championship Delta annotation: the points gap to their nearest rival if the race ended at the current position
**And** the calculation merges current race provisional points with the most recent official Ergast standings
**And** it is clearly labelled "if race ended now" to distinguish it from official standings
**And** it updates each lap as positions change

### Story 2.5: Fallback to Last Race

As a fan checking the app outside a live session,
I want the live race page to show the most recently completed race instead of an empty page,
So that the page is always useful.

**Acceptance Criteria:**

**Given** no live Session is in progress
**When** the live race page loads
**Then** it shows the most recently completed race in static/replay view using the same gap list, tyre tracker, lap chart, and championship-impact components, populated from historical data, clearly labelled as a past race

**Given** OpenF1 becomes unavailable mid-session (HTTP timeout >5s, empty driver array, or stale timestamp >10-20s)
**When** detected
**Then** the page transitions through the live→stale→fallback-to-last-race state machine rather than showing an empty/error state

**Given** the stream recovers
**When** 3-5 consecutive valid responses arrive
**Then** the page transitions back to live, debounced to prevent flapping

## Epic 3: Live Race Visual Enrichment

Adds the broadcast-quality layer on top of the core feed: an animated track map with mini-sector colour coding, a pit-window estimator, a fastest-sector board, and a lap-by-lap event timeline. Extends Epic 2's snapshot/pipeline; depends on Epic 2 being in place.

### Story 3.1: Animated Live Track Map

As a race-day fan,
I want to see an animated track map with driver positions,
So that I can visualize the race like a broadcast.

**Acceptance Criteria:**

**Given** a live session
**When** the track map renders
**Then** an SVG circuit layout shows driver dots positioned from OpenF1 real-time x/y coordinates, transformed via the per-circuit affine config (`circuit-configs/{circuitId}.json`)

**Given** coordinate updates arrive at ~3.7Hz
**When** rendered
**Then** movement is smoothly interpolated client-side so dots glide rather than jump
**And** each dot shows racing number and is coloured by team colour
**And** the map is on-screen within 10 seconds of session start

**Given** a circuit with no calibrated config asset yet
**When** loading
**Then** a clear "track map unavailable for this circuit" state is shown instead of broken positions

### Story 3.2: Mini-Sector Colour Coding

As a race-day fan,
I want driver dots colour-coded by their current mini-sector pace,
So that I can spot who's pushing without watching every car.

**Acceptance Criteria:**

**Given** the track map
**When** a driver completes a mini-sector
**Then** their dot updates colour: purple (fastest overall), green (personal best), yellow (normal pace), white (in/out-lap)
**And** colour updates on each mini-sector completion

### Story 3.3: Pit Window Estimator

As a race-day fan,
I want to see when a driver is likely to pit,
So that I can anticipate strategy calls before they happen.

**Acceptance Criteria:**

**Given** a driver's current Stint lap count enters the historically typical pit window for their compound at this circuit (from Ergast historical pit data)
**When** the gap list renders
**Then** a pit window indicator activates on that driver's entry

**Given** the driver pits
**When** detected
**Then** the indicator deactivates
**And** the indicator is compound- and circuit-specific, not a fixed lap number

### Story 3.4: Fastest Sector Board

As a race-day fan,
I want a panel showing the current fastest sector times and who holds them,
So that I can track who's setting the pace.

**Acceptance Criteria:**

**Given** a live or replayed session
**When** the sector board renders
**Then** it shows current fastest S1, S2, S3 times with the holder highlighted purple

**Given** a sector record is broken
**When** detected
**Then** the board updates immediately to the new holder/time

### Story 3.5: Race Event Timeline

As a race-day fan,
I want a timeline of key race events,
So that I can see the full shape of the race at a glance.

**Acceptance Criteria:**

**Given** a live race
**When** the timeline renders
**Then** a horizontal bar shows lap number on the X-axis with markers for Safety Car, VSC, pit stops (per driver), DNFs, fastest lap, and red flags, sourced from OpenF1 race control and pit data
**And** the timeline grows lap by lap during the live race

**Given** a completed race (live or fallback per Epic 2's Story 2.5)
**When** viewed
**Then** the timeline is a static, browsable archive

## Epic 4: Championship & Standings

Users can see full Drivers'/Constructors' standings, how the championship developed across the season, and — once the season ends — a shareable Season Wrapped recap. Pure Ergast-driven; introduces the html-to-image export pattern reused by Epic 6's Fan Card.

### Story 4.1: Standings Page with Toggle

As a fan tracking the championship,
I want to see Drivers' and Constructors' standings with an instant toggle,
So that I can check either championship without a reload.

**Acceptance Criteria:**

**Given** the standings page loads
**Then** the Drivers tab shows position, name, nationality flag, Constructor, points, and wins for every driver

**Given** the Constructors tab is selected
**When** switched
**Then** it shows position, name, nationality flag, points, and wins for every constructor, with no page reload
**And** switching between tabs is instant

### Story 4.2: Championship Trajectory Chart

As a fan tracking the championship,
I want to see how points accumulated across the season,
So that I can understand the championship's shape over time.

**Acceptance Criteria:**

**Given** the standings page
**Then** the trajectory chart plots cumulative points per Driver across all completed rounds (X = round number, Y = total points)

**Given** a hover on a data point
**When** triggered
**Then** it shows race name, race result position, and points scored that round
**And** only completed rounds are plotted; the chart updates after each new Ergast result is available

### Story 4.3: F1 Season Wrapped

As a fan at the end of the season,
I want a shareable recap of the season's highlights,
So that I can celebrate and share it with friends.

**Acceptance Criteria:**

**Given** the final race of the season has been completed (per Ergast round count)
**Then** a Season Wrapped section appears with: most dramatic race (largest position swings), driver with most DNFs, biggest points comeback, most positions gained in a single race, and the most-improved Constructor

**Given** the season is still in progress
**Then** Season Wrapped is not shown
**And** the wrap is calculated on demand from full-season Ergast data and presented as a shareable card, exported client-side via `html-to-image` with no server-side rendering

## Epic 5: Deep Dive Profiles

Users can explore any circuit's or driver's history in depth, and compare two drivers head-to-head across seasons/circuits. Pure Ergast historical data; reuses the trajectory-chart visual style introduced in Epic 4.

### Story 5.1: Circuit Profile Page

As a history enthusiast,
I want to see a circuit's full history and stats,
So that I can understand its character and legacy.

**Acceptance Criteria:**

**Given** a circuit name is clicked anywhere in the app
**When** the profile page opens
**Then** it shows the SVG track layout, the all-time lap record (driver, team, year), all past race winners at this circuit (year, team), and circuit stats (length, corners, DRS zones, year of first F1 race), all sourced from Ergast
**And** historical name variants of the same physical track are grouped under one entry

### Story 5.2: Driver Career Profile Page

As a history enthusiast,
I want to see a driver's full career stats,
So that I can understand their place in F1 history.

**Acceptance Criteria:**

**Given** a driver name is clicked anywhere in the app
**When** the profile page opens
**Then** it shows career totals (races, wins, podiums, poles, fastest laps, titles), Constructor history year by year, and a career cumulative points progression chart, sourced from Ergast
**And** the career chart uses the same visual style as the championship trajectory chart (Epic 4, Story 4.2)

### Story 5.3: Driver Head-to-Head Comparison

As a history enthusiast,
I want to compare two drivers' stats side by side,
So that I can settle debates about who's better.

**Acceptance Criteria:**

**Given** the head-to-head page
**When** the user selects two Drivers from a searchable dropdown
**Then** a side-by-side stat card shows qualifying average position, race finish average, DNF count, points scored, fastest laps, and wins

**Given** optional season and/or Circuit filters are applied
**Then** they are additive and the stat card recalculates accordingly

**Given** no filters are applied
**Then** the comparison covers the full all-time Ergast dataset for both drivers

## Epic 6: Fan Engagement

Users build a personal F1 identity in the app — a streak for following live sessions, a shareable Fan Card with their picks and stats, and a news feed to stay current between races. Streak depends on Epic 2's live-session detection; Fan Card depends on Epic 4's standings for current stats — hence last.

### Story 6.1: F1 News Feed

As a fan staying current between races,
I want to see aggregated F1 news headlines,
So that I don't have to check multiple sites.

**Acceptance Criteria:**

**Given** the news feed page loads
**When** RSS feeds (Formula1.com, Autosport, RaceFans) are fetched via the backend proxy
**Then** headlines render as a card list showing title, source, and timestamp

**Given** a card is clicked
**Then** the article opens in a new browser tab

**Given** the feed is cached
**Then** it refreshes on a configurable interval (default 15 minutes)

**Given** all feeds are unavailable
**Then** a clear "no news available" state is shown
**And** one broken feed does not block headlines from the others (per-feed error isolation)

### Story 6.2: Race Weekend Streak

As a dedicated fan,
I want to track how many consecutive race weekends I've followed live,
So that I can see my own engagement streak.

**Acceptance Criteria:**

**Given** the user visits the app during a live Session (per Epic 2's live state)
**Then** the streak counter on the calendar page increments for that Race Weekend, once per weekend

**Given** a Race Weekend passes without a live-session visit
**Then** the streak counter resets at the next weekend
**And** the streak is stored in browser localStorage with a versioned key — no account/backend required, does not persist across devices

### Story 6.3: My F1 Fan Card

As a fan expressing my F1 identity,
I want to set up a personal fan card with my favorite driver, constructor, and circuit,
So that I can showcase my fandom and share it.

**Acceptance Criteria:**

**Given** the user has not set up a Fan Card
**When** they open the setup wizard
**Then** they can pick a favourite Driver, Constructor, and Circuit

**Given** picks are made
**Then** the card shows them alongside current season stats (driver/constructor points and position, from Epic 4's standings data)

**Given** an existing Fan Card
**Then** the wizard is re-accessible to change picks at any time

**Given** the user exports the card
**Then** it generates a client-side image (`html-to-image`) with only the chosen picks and current stats — no personal data
**And** the card is stored in localStorage with a versioned key
