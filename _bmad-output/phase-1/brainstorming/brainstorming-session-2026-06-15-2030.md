---
stepsCompleted: [1, 2, 3, 4]
workflow_completed: true
session_active: false
inputDocuments: []
session_topic: 'Formula 1 React + C# web application features'
session_goals: 'Discover all possible and cool features that can be built using the free F1 API, beyond the three core pages already planned'
selected_approach: 'ai-recommended'
techniques_used: ['SCAMPER Method', 'Cross-Pollination', 'Dream Fusion Laboratory']
ideas_generated: []
context_file: ''
---

# Brainstorming Session Results

**Facilitator:** Bohdan
**Date:** 2026-06-15

## Session Overview

**Topic:** Formula 1 React + C# web application — feature discovery and expansion
**Goals:** Analyze feasibility of the three planned pages (Race Calendar, Live Race Tracker, Standings) and brainstorm additional cool features using the free F1 API (Ergast / OpenF1)

### Session Setup

_Fresh session initiated for F1 application feature brainstorming. User has a solid React + C# stack in mind and a clear vision for the core three pages._

## Technique Execution Results

### Phase 1: SCAMPER Method

| # | Feature | Priority |
|---|---------|----------|
| S#1 | Race countdown push notifications | Core |
| S#2 | Full race list view | Core |
| S#3 | Race weekend card (main page) | Core |
| S#4 | Race weekend detail view (all sessions) | Core |
| S#5 | Living detail view with contextual data | Core |
| C#6 | Championship snapshot top 3 drivers+teams on race card | Core |
| C#7 | Live championship impact tracker | Core |
| C#8 | Timezone toggle (my time / track time) | Core |
| A#9 | Race day weather forecast | Low / post-POC |
| A#10 | Mini-sector colour coding on live map | Core |
| M#11 | Championship trajectory chart | Core |
| M#12 | Live gap visualiser | Core |
| P#13 | Driver head-to-head comparison page | Core |
| R#14 | Race prediction game | Low / post-POC |

### Phase 2: Cross-Pollination

| # | Feature | Source Domain | Priority |
|---|---------|--------------|----------|
| #15 | Live tyre tracker (compound + stint age) | F1 Video Game | Core |
| #16 | Race event timeline (SC, pits, DNFs, flags) | Sofascore | Core |
| #17 | Fastest sector board (S1/S2/S3 per session) | F1 Broadcast | Core |
| #18 | Live lap time chart per driver | Stock Market Apps | Core |
| #19 | F1 Season Wrapped (shareable recap) | Spotify Wrapped | Core |
| #20 | Circuit profile page | Wikipedia/RaceFans | Core |
| #21 | Driver career profile page | ESPN/NBA Stats | Core |
| #22 | F1 news feed page (RSS aggregation) | Reddit/Feedly | Core |
| #23 | Post-race team radio feed | Podcast Apps | Low / post-POC |
| #24 | Race weekend streak | Duolingo | Core |

### Phase 3: Dream Fusion Laboratory

| # | Feature | Dream Origin | Priority |
|---|---------|-------------|----------|
| #25 | Pre-race win probability widget | AI Race Oracle | Core |
| #26 | Smooth-animated live track map (interpolated) | 3D Circuit Render | Core |
| #27 | Pit window estimator | Full Pit Wall Data | Core |
| #28 | My F1 fan card (shareable identity) | Deep Personalisation AI | Core |

---

## Idea Organization and Prioritization

### Thematic Clusters → App Pages

**Page 1: Race Calendar & Schedule**
Race weekend cards, full race list, session detail view, living contextual data, timezone toggle, push notifications, weather forecast *(low)*, win probability widget

**Page 2: Live Race Experience**
Smooth-animated track map, mini-sector colour coding, live gap visualiser, tyre tracker, pit window estimator, lap time chart, fastest sector board, race event timeline, championship impact tracker

**Page 3: Championship & Standings**
Driver/team standings toggle, championship snapshot on race cards, trajectory chart, F1 Season Wrapped

**Page 4: Deep Dive Profiles** *(new)*
Circuit profile page, driver career profile page, head-to-head comparison page, post-race team radio *(low)*

**Page 5: Fan Engagement** *(new)*
F1 news feed, race weekend streak, My F1 fan card, race prediction game *(low)*

---

### Prioritization Results

**Bohdan's Top 4 Differentiating Features (beyond the original 3 pages):**

1. **Live Championship Impact Tracker** — real-time "what does this position change mean for the title?" annotations on the live race page. Broadcast-quality insight, uniquely valuable.
2. **F1 Season Wrapped** — end-of-season shareable recap card. Self-marketing feature; fans share it and the app spreads organically.
3. **Smooth-Animated Live Track Map** — interpolated car movement between OpenF1 coordinate pings. The detail that separates a premium app from a prototype.
4. **My F1 Fan Card** — one-time setup, shareable identity card. Creates emotional ownership and social sharing.

**POC Core Feature Set (24 features):**
All features marked "Core" above — 24 features across 5 page themes.

**Post-POC / Low Priority (4 features):**
Race day weather forecast, Race prediction game, Post-race team radio feed, (DRS indicator skipped by user)

---

## Session Summary and Insights

**Key Achievements:**
- 28 ideas generated across 3 techniques (SCAMPER, Cross-Pollination, Dream Fusion Laboratory)
- 5 natural page themes identified, expanding original 3-page concept to a 5-page app
- 24 core features scoped for POC; 4 deferred to post-POC
- Every feature grounded in free API data (Ergast + OpenF1) — no proprietary data required

**Breakthrough Moments:**
- *Live Championship Impact Tracker* — emerged from combining live timing data with standings math; nobody has built this for a fan web app
- *F1 Season Wrapped* — recognised as self-marketing: the feature that makes fans share the app without being asked
- *Smooth Animation* — identified as the single biggest quality gap between "prototype" and "product feel"

**API Feasibility Summary:**
- **Ergast API:** Race schedules, historical results, standings, driver/circuit data, lap-by-lap positions, career stats
- **OpenF1 API:** Live car positions (x/y), tyre compounds + age, gap intervals, sector times, race control messages, pit stop events, team radio audio URLs

**Creative Facilitation Notes:**
Bohdan thinks in concrete product decisions fast — strong instinct for what belongs in the POC vs. post-POC. Prefers data-rich features with clear fan value over gamification or social mechanics. The live race page is clearly the heart of the product vision.
