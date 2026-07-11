---
title: F1 Fan Web Application
status: final
created: 2026-06-15
updated: 2026-06-16
---

# PRD: F1 Fan Web Application

## 0. Document Purpose

This PRD is written for the System Architect (Winston) and any future developers working on this project. It specifies what the product must do and for whom — tech stack choices (React, C#, Ergast, OpenF1) live in `addendum.md`. Features are grouped by page/surface with globally numbered FRs for stable downstream reference. Assumptions are tagged inline and indexed in §9.

---

## 1. Vision

An F1 fan web application that serves as the single destination for everything Formula 1 — race schedules, live race tracking, championship standings, driver and circuit history, and personal fan identity. The product replaces the scattered experience of checking multiple sites during a race weekend with one coherent, data-rich interface that works whether it is race day or a quiet Tuesday.

The live race page is the heart of the product: a real-time cockpit showing driver positions on a circuit map, time gaps between cars, tyre strategies, and championship implications — broadcast-quality context in a personal app, built entirely on free public APIs. Beyond race day, fans can explore driver careers, circuit histories, and championship trajectories using decades of public F1 data.

The POC ships without authentication — all features are immediately accessible. A future login layer will unlock user-specific features (persistent fan identity across devices, push notifications) without removing any currently public functionality.

---

## 2. Target User

### 2.1 Jobs To Be Done

- Know when the next race is and which sessions to watch, in my local timezone.
- Follow a live race in real time without paying for F1 TV.
- Understand what the championship stakes are during a race as positions change.
- Explore F1 statistics and history when curiosity strikes after a race.
- Express my F1 fan identity and share it with friends.

### 2.2 Non-Users (POC)

- Professional F1 analysts requiring certified telemetry.
- Users expecting official broadcast video or audio.
- Users requiring an account or personalisation beyond browser-local storage.

### 2.3 Key User Journeys

- **UJ-1.** Race-day fan on desktop browser, wants to follow a live Grand Prix without F1 TV — opens the live race page and monitors driver positions, gaps, tyres, and championship impact in real time.
- **UJ-2.** Casual fan on any device, mid-week, wants to know when the next race weekend is and which sessions to set alarms for — opens the calendar, clicks the next Race Weekend card, checks session times in their timezone.
- **UJ-3.** History enthusiast after watching a race, curious whether Norris is statistically better than Russell at street circuits — opens the head-to-head comparison tool, selects both drivers, filters by circuit type.

---

## 3. Glossary

- **Race Weekend** — The full event unit: one or more practice Sessions, Qualifying, and a Race (optionally a Sprint). The atomic scheduling unit in the app.
- **Session** — A discrete on-track event within a Race Weekend: FP1, FP2, FP3, Sprint Shootout, Sprint, Qualifying, or Race.
- **Circuit** — A physical race venue with a stable identity across seasons (e.g. "Circuit de Monaco").
- **Driver** — An F1 competitor identified by racing number and three-letter code (e.g. VER, HAM).
- **Constructor** — A team competing in the F1 Constructors' Championship (e.g. "Red Bull Racing").
- **Gap** — Time difference in seconds between two consecutive cars in current race order.
- **Stint** — A continuous run on a single set of tyres between pit stops or from race start.
- **Tyre Compound** — The specification of a tyre set: Soft (red), Medium (yellow), Hard (white), Intermediate (green), Wet (blue).
- **Sector** — One of three designated segments of a circuit lap (S1, S2, S3).
- **Championship Delta** — The points difference between two Drivers or Constructors in current standings.
- **Track Time** — The timezone of the circuit's host country.
- **Local Time** — The user's detected browser timezone.
- **Ergast API** — Free public REST API providing historical F1 data: schedules, results, standings, lap times, driver and circuit records.
- **OpenF1 API** — Free public REST/streaming API providing live and near-live F1 session data: car positions, tyres, gaps, sector times, race control messages.

---

## 4. Features

### 4.1 Race Calendar & Schedule

The primary navigation surface. Displays all Race Weekends in the current season as a scrollable card list. The next upcoming Race Weekend is visually prominent. Each card links to a Race Weekend Detail view showing all sessions with times and contextual data.

**Functional Requirements:**

#### FR-1: Season race list
User can view all Race Weekends in the current season as a chronological scrollable list. The next upcoming Race Weekend card is visually distinguished (e.g. pinned or highlighted).

**Consequences:**
- All rounds from the current season schedule are shown.
- Past Race Weekends remain visible and accessible below the current/next card.

#### FR-2: Race Weekend card
Each Race Weekend card displays: circuit name, country flag, weekend date range, main race date and time, top 3 Drivers in the Drivers' Championship with points, top 3 Constructors with points.

**Consequences:**
- Championship data on each card reflects the current standings at time of load.

#### FR-3: Race Weekend detail view
Clicking a Race Weekend card opens a detail view listing every Session in order (FP1, FP2, FP3 or Sprint Shootout/Sprint, Qualifying, Race) with date and time. Sprint weekends are correctly differentiated from standard weekends.

**Consequences:**
- All sessions for the selected Race Weekend are shown with correct session type labels.
- Sprint weekends show Sprint Shootout and Sprint instead of FP2/FP3.

#### FR-4: Contextual detail data
The Race Weekend detail view displays: last year's race winner at this Circuit (driver name, team, time/gap), and the current Championship Delta between the top two Drivers.

**Consequences:**
- If no prior-year result exists at this circuit (new venue), the field is omitted or labelled "First race at this circuit."

#### FR-5: Timezone toggle
A toggle on the detail view switches all Session times between Track Time and Local Time (detected via browser API). [ASSUMPTION: Browser timezone detection is sufficient; no manual timezone selector needed for POC.]

**Consequences:**
- All session times update immediately on toggle.
- Default state is Local Time.

#### FR-6: Pre-race win probability widget
After Qualifying, the Race Weekend detail view shows each Driver's grid position alongside a calculated win probability (%) derived from historical Ergast data: win rate from that grid slot at this Circuit, current championship standing, and weather conditions where available.

**Consequences:**
- Widget is absent before qualifying results are published.
- Probabilities sum to ~100% across all drivers.

---

### 4.2 Live Race Experience

The centrepiece of the product. Active during live F1 Sessions. When no live session is in progress, displays the most recently completed race in a static view. [ASSUMPTION: OpenF1 provides sufficient update frequency (≥1/sec) for smooth animation during live sessions.]

**Functional Requirements:**

#### FR-7: Animated live track map
An SVG circuit layout renders driver dots positioned using OpenF1 real-time x/y coordinates. Movement between coordinate updates is smoothly interpolated so drivers appear to glide continuously rather than jump. [ASSUMPTION: OpenF1 car coordinates are normalisable to SVG coordinate space for each circuit.]

**Consequences:**
- Each driver dot shows racing number and is coloured by Constructor team colour.
- On-screen within 10 seconds of session start.

#### FR-8: Mini-sector colour coding
Driver dots on the track map are colour-coded by current mini-sector status: purple (fastest overall), green (personal best), yellow (normal pace), white (in-lap or out-lap).

**Consequences:**
- Colour updates each time a driver completes a mini-sector.

#### FR-9: Live gap list
All drivers are shown in current race order with real-time Gaps to the car ahead, updated each lap. Gaps under 1 second are highlighted to indicate active battles.

**Consequences:**
- Gap list is always sorted by current race position.
- Leader shows gap to leader (0.000 or interval to next car behind, whichever is idiomatic).

#### FR-10: Live tyre tracker
Each entry in the gap list shows the Driver's current Tyre Compound as a colour-coded circle and the number of laps on that Stint.

**Consequences:**
- Compound and stint count update on each OpenF1 tyre data refresh.

#### FR-11: Pit window estimator
A pit window indicator activates on a driver's gap list entry when their current Stint lap count enters the historically typical pit window for their Tyre Compound at this Circuit, derived from Ergast historical pit stop data.

**Consequences:**
- Indicator is compound- and circuit-specific, not a fixed lap number.
- Indicator deactivates after a driver pits.

#### FR-12: Live lap time chart
A line chart plots each Driver's lap time per completed lap, updated each lap. Pit-out laps appear as visible spikes upward. Hovering a data point shows exact lap time and gap to the fastest lap of the race.

**Consequences:**
- Chart initialises at lap 1 and grows lap by lap.
- All drivers rendered as separate coloured lines.

#### FR-13: Fastest Sector board
A panel displays current fastest S1, S2, S3 times with the Driver who holds each (coloured purple), updated live.

**Consequences:**
- Sector board updates each time a sector record is broken.

#### FR-14: Race event timeline
A horizontal timeline bar shows lap number on the X-axis with event markers: Safety Car, Virtual Safety Car, pit stops (per driver), DNFs, fastest lap, red flags. Grows lap by lap during a live race; becomes a static browsable archive after session ends.

**Consequences:**
- Markers are sourced from OpenF1 race control messages and pit stop data.
- Post-race timeline is accessible when viewing any completed race.

#### FR-15: Live championship impact tracker
Each Driver entry in the gap list shows a live Championship Delta annotation: how their current race position would change their points gap to their nearest championship rival if the race ended now. Updates each lap as positions change.

**Consequences:**
- Annotation is calculated from current standings + current race provisional points.
- Clearly labelled to distinguish "if race ended now" from official standings.

#### FR-16: Fallback to last race
When no live Session is in progress, the live race page displays the most recently completed race in static/replay view with all the same UI components populated from historical data.

**Consequences:**
- Page never shows an empty or error state outside of API failures.
- Clearly labelled as a past race (race name, date).

---

### 4.3 Championship & Standings

**Functional Requirements:**

#### FR-17: Standings page with toggle
A standings page presents Drivers' Championship and Constructors' Championship as two tabs. Switching tabs is instant (no reload).

**Consequences:**
- Driver standings show: position, name, nationality flag, Constructor, points, wins.
- Constructor standings show: position, name, nationality flag, points, wins.

#### FR-18: Championship trajectory chart
A multi-line chart shows cumulative points per Driver across all completed race rounds. X-axis is round number, Y-axis is total points. Hovering a data point shows race name, race result position, and points scored that round.

**Consequences:**
- Only completed rounds are plotted.
- Chart updates after each race result is available in Ergast.

#### FR-19: F1 Season Wrapped
After the final race of the season, the standings page surfaces a Season Wrapped section: most dramatic race (largest position swings), driver with most DNFs, biggest points comeback, most positions gained in a single race, Constructor that improved most across the season. Presented as a shareable card exportable as an image.

**Consequences:**
- Wrap is calculated on demand from Ergast full-season data.
- Shareable card image generated client-side (no server-side image rendering required).
- Only visible after the final race of the current season. [ASSUMPTION: Final race detection uses Ergast round count for the season.]

---

### 4.4 Deep Dive Profiles

All profile pages are accessible by clicking any circuit name or driver name anywhere in the app.

**Functional Requirements:**

#### FR-20: Circuit profile page
Displays for a selected Circuit: SVG track layout, all-time lap record (driver, team, year), list of all past race winners at this circuit with year and team, circuit stats (length in km, number of corners, DRS zones, year of first F1 race).

**Consequences:**
- All data sourced from Ergast historical records.
- Circuit name is the entry key; all historical variants of the same physical track are grouped.

#### FR-21: Driver career profile page
Displays for a selected Driver: career totals (races entered, wins, podiums, pole positions, fastest laps, championship titles), Constructor history year by year, career cumulative points progression chart.

**Consequences:**
- Data sourced from Ergast.
- Career chart uses the same visual style as the championship trajectory chart (FR-18).

#### FR-22: Driver head-to-head comparison
User selects two Drivers from a searchable dropdown. Optional filters: season, Circuit. Returns a side-by-side stat card: qualifying average position, race finish average, DNF count, points scored, fastest laps, wins within the filtered scope.

**Consequences:**
- No filter = all-time head-to-head across full Ergast dataset.
- Filters are additive (season AND circuit can be combined).

---

### 4.5 Fan Engagement

**Functional Requirements:**

#### FR-23: F1 news feed
A news feed page aggregates headlines from public F1 RSS feeds (Formula1.com, Autosport, RaceFans). Displayed as a card list: title, source name, timestamp. Clicking a card opens the article in a new browser tab. Feed is fetched and cached by the C# backend to avoid browser CORS issues.

**Consequences:**
- Feed refreshes at a configurable interval (default: every 15 minutes).
- If all feeds are unavailable, a clear "no news available" state is shown.

#### FR-24: Race weekend streak
A streak counter on the calendar page shows how many consecutive Race Weekends the user has visited the app during a live Session. Stored in browser local storage. Counter resets if a Race Weekend passes without a visit during a live Session.

**Consequences:**
- No account or backend required.
- Counter is local to the browser; does not persist across devices.

#### FR-25: My F1 Fan Card
A one-time setup wizard lets the user pick their favourite Driver, Constructor, and Circuit. The app generates a styled card showing those picks alongside their current season stats (driver points/position, constructor points/position). Card is stored in local storage and exportable as a shareable image (client-side generation).

**Consequences:**
- Wizard is re-accessible to change picks at any time.
- Exported image contains no personal data — only the chosen picks and current stats.

---

## 5. Non-Goals (POC)

- User authentication or accounts of any kind.
- Push notifications (requires backend identity — deferred post-POC). `[NOTE FOR PM: highest priority post-POC unlock]`
- Live video or audio streaming.
- Official F1 licensed assets (3D car models, official branding).
- Native mobile application (web only).
- Betting, fantasy league, or wagering features.
- Social features: comments, follows, likes.
- Race prediction game. `[NOTE FOR PM: deferred post-POC]`
- Race day weather forecast. `[NOTE FOR PM: deferred post-POC]`
- Post-race team radio audio feed. `[NOTE FOR PM: deferred post-POC]`
- DRS activation indicator on track map.

---

## 6. MVP Scope

### 6.1 In Scope (POC)

- All 5 pages: Race Calendar, Live Race, Standings, Profiles, Fan Engagement.
- FR-1 through FR-25.
- Browser local storage for fan card and streak (no backend persistence required).
- Free API data only: Ergast (historical/schedule) + OpenF1 (live).
- Browser timezone detection (no manual timezone selector).
- Responsive web layout: desktop primary, mobile acceptable.
- C# backend acts as API proxy/aggregator and RSS fetcher.

### 6.2 Out of Scope for POC

- Authentication / login.
- Push notifications.
- Weather API integration.
- Race prediction game.
- Post-race team radio feed.
- Server-side image rendering for fan card / Season Wrapped.

### 6.3 Deployment Plan

- **POC:** Local development only.
- **Future (free tier):** React frontend → Vercel or Netlify; C# backend → Render or Railway.

---

## 7. Success Metrics

- **Primary:** Bohdan and friends use the app during at least 3 consecutive race weekends without switching to another source for live timing.
- **Secondary:** The live race page loads and displays correct driver positions within 10 seconds of a session starting.
- **Counter-metric:** Do not track page views, DAU, or engagement scores — this is a hobby project.

---

## 8. Open Questions

1. Does OpenF1 provide car coordinate data directly mappable to standard SVG circuit layouts, or does per-circuit normalisation need to be built into the backend?
2. Which F1 RSS feeds are reliably available without CORS issues from a C# backend proxy — and do any require API keys?
3. Should F1 Season Wrapped (FR-19) be generated on-demand each time the user views it, or pre-generated once after the final race and cached?
4. **Post-POC:** Which features remain fully public vs. require login after auth is introduced? (Streak and fan card are local-storage today — do they migrate to account-backed storage?)

---

## 9. Assumptions Index

- **A-1** (FR-5): Browser timezone detection is sufficient; no manual timezone selector needed for POC.
- **A-2** (FR-7): OpenF1 provides update frequency ≥1/sec for smooth animation during live sessions.
- **A-3** (FR-7): OpenF1 car coordinates are normalisable to SVG coordinate space per circuit.
- **A-4** (FR-19): Final race detection uses Ergast round count for the season.
- **A-5** (§4.2): OpenF1 API provides live tyre, sector, race control, and gap data in a single session stream or via parallel low-latency endpoints.
