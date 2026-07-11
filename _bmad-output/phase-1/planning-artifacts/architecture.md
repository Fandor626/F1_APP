---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
workflowType: 'architecture'
lastStep: 8
status: 'complete'
completedAt: '2026-06-16'
inputDocuments:
  - '_bmad-output/planning-artifacts/prds/prd-F1_poc-2026-06-15/prd.md'
  - '_bmad-output/planning-artifacts/prds/prd-F1_poc-2026-06-15/addendum.md'
workflowType: 'architecture'
project_name: 'F1_poc'
user_name: 'Bohdan'
date: '2026-06-16'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

---

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**
25 FRs across 5 feature groups:
- **4.1 Race Calendar & Schedule** (FR-1–6): Season list, race weekend cards, detail view with all sessions, contextual data, timezone toggle, post-qualifying win probability widget
- **4.2 Live Race Experience** (FR-7–16): Animated SVG track map, mini-sector colour coding, live gap list, tyre tracker, pit window estimator, lap time chart, fastest sector board, race event timeline, championship impact tracker, fallback-to-last-race
- **4.3 Championship & Standings** (FR-17–19): Standings table with driver/constructor toggle, trajectory chart, Season Wrapped
- **4.4 Deep Dive Profiles** (FR-20–22): Circuit profile page, driver career profile page, head-to-head comparison
- **4.5 Fan Engagement** (FR-23–25): RSS news feed, race weekend streak, My F1 Fan Card

**Non-Functional Requirements:**
- Live race page loads and shows correct positions within 10 seconds of session start
- Smooth animated car movement (client-side interpolation between OpenF1 coordinate updates)
- No backend database for POC — browser localStorage only
- All data from free public APIs (no auth required on data sources)
- Responsive web: desktop primary, mobile acceptable
- Client-side image generation for Fan Card and Season Wrapped (no server-side rendering)

**Scale & Complexity:**
- Complexity level: **Medium-High** (live race page is genuinely non-trivial for a hobby project)
- Primary domain: Full-stack web with real-time data pipeline
- Estimated architectural components: ~8 React page/feature clusters, ~5 C# service layers

### Technical Constraints & Dependencies

- **Ergast API**: Deprecated (no new features, still serving). Free, no auth, REST. Historical data only — schedule, results, standings, driver/circuit records back to 1950. **Risk**: may stop serving without notice.
- **OpenF1 API**: Active, free, no auth. REST + streaming. Live car positions (WGS84 GPS at ~3.7Hz), tyres, gaps, sector times, race control messages. **Risk**: no SLA — confirmed outage during 2024 Monaco GP (~8 min).
- **RSS feeds**: Formula1.com, Autosport, RaceFans — free, no auth. Must be proxied via C# backend to avoid CORS.
- **No backend database for POC**: localStorage only. Schema versioning required from day one (version key + migration logic) to prevent silent corruption on deploys.
- **Deployment target (post-POC)**: Vercel (React) + Render (C# Docker). Ergast base URL must be configurable from day one to enable zero-refactor migration to Jolpica API (Ergast-compatible fork/successor).

### Cross-Cutting Concerns Identified

**1. Real-time data pipeline (highest complexity)**
OpenF1 exposes data across multiple independent endpoints (`/position`, `/car_data`, `/intervals`, `/lap`, `/race_control`, etc.) each requiring its own polling loop. This is a multi-loop ETL, not a simple proxy. Key constraints:
- `car_data` (~3.7Hz) and `/position` (GPS, coarser) have different timestamp granularities — joining requires ~300ms tolerance windowing
- Use `DateTimeOffset` throughout C# backend — `DateTime` vs `DateTimeOffset` mismatch silently corrupts event ordering
- Backpressure: ~370 messages/sec peak (3.7Hz × 5 channels × 20 drivers) — throttle at SignalR hub level, not client
- Cold-start hydration required: user loading mid-race needs last-N-laps backfill

**2. SVG track map as a data asset (not a one-time task)**
OpenF1 GPS coordinates are raw WGS84 lat/lon — not normalised, not circuit-relative. Mapping to SVG requires a per-circuit affine transform (scale, rotation, translation). Architecture decision: transform parameters are a **runtime-loaded configuration asset** (not hardcoded), allowing circuits to be added incrementally. Start with 1-2 calibrated circuits for POC; expand post-POC. Car position interpolation (3.7Hz → 60fps) requires client-side linear interpolation with ~270ms lookahead buffer.

**3. Ergast deprecation and cache strategy**
Cache is load-bearing. Correct strategy: proactive cache warm on startup (crawl all seasons, circuits, driver data) — not request-time-only caching. TTL differentiation required: historical results (long TTL), current season standings (short TTL). Configurable base URL enables zero-code migration to Jolpica.

**4. Dual-source data merge and staleness**
Championship impact tracker (FR-15) merges OpenF1 live positions with Ergast standings. Ergast standings have days-to-weeks lag post-race. Merge must be clearly labelled in UI as projection based on last official standings (with timestamp). No silent staleness.

**5. Degraded-mode state machine (must be explicitly designed)**
Fallback behavior when OpenF1 is unavailable must be a defined state machine — not an implicit `DateTime` check. Trigger conditions to define: HTTP timeout, partial JSON response, stale timestamp threshold, empty driver array. Recovery path: return to live mode when stream resumes. OpenF1 silent failures (valid JSON, semantically incomplete) are the highest-risk failure mode.

**6. Win probability calculation (FR-6)**
Defined as a simple post-qualifying heuristic computed once per race weekend:
- Historical win rate from qualifying grid slot at this circuit (Ergast)
- Current championship standing weight (title contenders weighted up)
- Recent form weight (last 3 race results)
C# backend computes after qualifying results are published. No live data dependency.

---

## Starter Template Evaluation

### Primary Technology Domain
Full-stack web — React SPA (frontend) + ASP.NET Core Web API (backend). Two separate projects scaffolded independently.

### Selected Starter: Vite + React 19 + TypeScript (Frontend)

**Rationale:** Current standard for React SPAs in 2026. Create React App is deprecated; Next.js adds SSR complexity not needed for a SPA.

**Initialization Command:**
```bash
npm create vite@latest f1-app-frontend -- --template react-ts
```

**Frontend library stack (refined after review):**

| Library | Purpose | Notes |
|---|---|---|
| `react-router-dom v7` | Client-side routing (5 pages) | Standard Vite/React pairing |
| `@tanstack/react-query v5` | Ergast API data fetching + cache | `staleTime`/`gcTime` config; replaces manual fetch boilerplate |
| `zustand` | Live race UI-only client state | **Hard rule:** only UI state (overlay visibility, pinned driver) — never server data |
| `@microsoft/signalr` | SignalR client | Official Microsoft package; connection instantiated as module-level singleton, NOT inside `useEffect` |
| `recharts` | All standard charts (trajectory, lap times, career stats) | Validate spatial/track-position use case before committing; D3 is escape hatch |
| `tailwindcss v4` | Styling | Utility-first, Vite-native |
| `html-to-image` | Fan Card + Season Wrapped image export | **Replaces html2canvas** — actively maintained, better cross-origin handling, foreignObject SVG approach |
| `zod` | Runtime API schema validation at TanStack Query boundaries | Catches Ergast/OpenF1 shape changes before they corrupt UI |
| `vitest` + `@testing-library/react` | Unit/component tests | ESM-native, Jest-compatible API |
| `msw` (Mock Service Worker) | Frontend test network layer | Intercepts at network layer; reusable across Vitest + Playwright |
| `playwright` | E2E smoke tests | Race-day canary test |

### Selected Starter: ASP.NET Core 10 Web API (Backend)

**Rationale:** .NET 10 is the current LTS release (Nov 2025, supported until Nov 2028). Right long-term foundation for a project that will evolve.

**Initialization Command:**
```bash
dotnet new webapi -n F1App.Api --framework net10.0
```

**Backend library stack (refined after review):**

| Package | Purpose | Notes |
|---|---|---|
| `Microsoft.AspNetCore.SignalR` | Real-time hub — pushes live race data to React clients | Hub throttles at server level; client receives aggregated snapshots |
| `Microsoft.Extensions.Hosting` | `IHostedService` for OpenF1 background polling loops | Isolated from request pipeline to prevent memory leak cross-contamination |
| `Microsoft.Extensions.Caching.Memory` | In-memory Ergast response cache | POC only; configurable base URL for Ergast → Jolpica migration |
| `Microsoft.Extensions.Http` | `IHttpClientFactory` typed clients for Ergast + OpenF1 | Use `DateTimeOffset` throughout — never `DateTime` |
| `CodeHollow.FeedReader` | RSS feed parsing | **Replaces System.ServiceModel.Syndication** — async-native, malformed-RSS tolerant; catch exceptions per-feed not per-cycle |
| `xunit` + `Moq` | Unit tests | Use `MockHttpMessageHandler` pattern with Moq (not mocking factory directly) |
| `WireMock.Net` | Contract-style integration tests for Ergast + OpenF1 | Murat's recommendation for external API contract validation |

### Solution Structure

```
F1_poc/
├── frontend/          ← Vite React 19 TS SPA
├── backend/
│   └── F1App.Api/     ← ASP.NET Core 10 Web API
└── docker-compose.yml ← local dev orchestration
```

**Note:** Project initialization is the first implementation story.

---

### Scope Decisions (from architecture review)

| Decision | Resolution |
|---|---|
| Track map POC scope | One circuit initially; reusable data-driven component — circuit configs added incrementally as data assets |
| Win probability | In POC — simple heuristic, post-qualifying, C# calculation |
| Live race page structure | Split into Epic 1 (core loop: positions, gaps, timing, fallback) and Epic 2 (enrichment: track map, sector board, win probability) |
| Ergast base URL | Configurable from day one — Jolpica migration = one config change |
| localStorage | Version key + migration logic from day one |
| SignalR throttling | At hub level, not client |
| DateTimeOffset | Mandatory throughout C# backend |

---

## Core Architectural Decisions

_Step 4 decisions. Stress-tested via Party Mode (Winston + Amelia) before saving. Five amendments folded in from review._

### Data Architecture

**No backend database (POC).** All persistence is browser `localStorage`. Schema versioning with a version key and migration logic is required from day one to prevent silent corruption on deploys.

**Ergast caching — `IMemoryCache` with TTL tiers:**

| Data type | TTL | Rationale |
|---|---|---|
| Historical race results | 7 days | Immutable once published |
| Season schedule | 24 hours | Rarely changes mid-season |
| Standings | 1 hour | Updates after each race |
| Circuit + driver metadata | 24 hours | Changes rarely |
| Qualifying results | 6 hours | Published once per weekend |

Proactive cache warm on startup for: season schedule, current standings, circuit metadata. Avoids cold-start thundering herd on first page load.

**Ergast base URL is configurable** in `appsettings.json` from day one. Migrating to Jolpica (Ergast-compatible fork) = one config change, zero code refactor.

---

### Real-Time Architecture

**Single `RaceDataOrchestrator` hosted service** (amended from original multiple-services design after Party Mode review). Owns all five OpenF1 polling loops as internal `Task` instances (`/position`, `/intervals`, `/car_data`, `/race_control`, `/pit`). Maintains one mutable `RaceStateSnapshot` and publishes on cadence (1-2s push). Failure isolation at the `Task` level; assembly logic in one coherent place with no concurrent coordination primitives spanning separate services.

*Rationale for amendment:* multiple `IHostedService` instances would require shared concurrent state or an informal message bus for snapshot assembly — effectively an orchestrator arrived at sideways.

**Snapshot push pattern.** Hub broadcasts full `RaceStateSnapshot` every 1-2 seconds. React replaces full client state on each message. No delta/patch protocol — eliminates the class of bugs where a client misses a diff and drifts into permanently inconsistent state.

**`RaceStateSnapshot` payload per driver:** position, gap to car ahead, tyre compound, stint laps, S1/S2/S3 sector status, championship delta. ~2-4KB per snapshot at compact JSON encoding. At 10 concurrent viewers: ~40KB/s total outbound — well within Render free tier.

**Timestamp join tolerance: 500ms, configurable** (amended from 300ms after Party Mode review). OpenF1 endpoints are produced by independent server processes; observed skew during live sessions runs 200-800ms. Value exposed in `appsettings.json` for tuning after two race weekends of production data.

**Partial join behaviour (explicit).** When a timestamp join fails (no matching record within the tolerance window), the snapshot is emitted with the *previous known value* for the missing field plus a per-field `isStale: true` flag. This is not a fallback trigger. The frontend renders `~` prefix on stale gap/sector values rather than showing a confidently wrong number.

**Fallback state machine:**

```
live ──(trigger)──► stale ──(trigger)──► fallback-to-last-race
 ▲                                              │
 └───────(recovery: 3-5 consecutive good)───────┘
          first valid response → stale
          N consecutive valid → live
```

Entry triggers:
- HTTP timeout > 5s
- Empty driver array in response (catches valid-JSON-but-semantically-empty silent failures)
- Timestamp age > 10s (green flag racing: consider 15-20s to absorb known OpenF1 latency spikes)

Recovery path (amended — previously underspecified):
- First valid response after `fallback-to-last-race` → transition to `stale`
- 3-5 consecutive valid responses → transition to `live`
- Debounce prevents flapping during marginal connectivity

`connectionStatus` Zustand slice reflects all three states (`'connected' | 'reconnecting' | 'disconnected'`) so the frontend never jumps directly from "offline" to "live".

**SignalR reconnect policy.** `HubConnectionBuilder` configured with indefinite retry + exponential backoff (ceiling: ~60s). Default policy (retries at 0, 2, 10, 30s then stops) is unsuitable for an app that goes idle between races on Render's free tier sleep cycle.

**`TimeProvider` injection (mandatory).** All hosted services with time-dependent logic receive `TimeProvider` via DI (BCL, .NET 8+). Time-dependent evaluation (e.g. timestamp age check) exposed as `internal` callable method via `InternalsVisibleTo` — not only evaluated on the next incoming message, which would create a frozen-stream detection blind spot.

---

### API & Communication

**Typed HTTP clients via `IHttpClientFactory`:** `ErgastClient`, `OpenF1Client`. Named registrations in `Program.cs`.

**`DateTimeOffset` throughout.** `DateTime` is banned in C# backend — `DateTime` vs `DateTimeOffset` mismatch silently corrupts event ordering across timezone boundaries.

**Error responses:** ProblemDetails (RFC 7807) on all endpoints. Consistent shape keeps frontend error handling thin.

**RSS proxy:** `CodeHollow.FeedReader` (async-native, malformed-RSS tolerant). Catch exceptions per-feed not per-cycle — one broken feed does not block the others.

**Win probability:** `WinProbabilityService` computes once after qualifying results are published. Inputs: historical win rate from this grid slot at this circuit (Ergast), current championship standing weight, last-3-race form weight. Cached until next qualifying. No live data dependency.

---

### Frontend Architecture

**Feature-based folder structure:**
```
src/
  features/
    calendar/           ← FR-1 to FR-6
    live-race/          ← FR-7 to FR-16
    standings/          ← FR-17 to FR-19
    profiles/           ← FR-20 to FR-22
    fan-engagement/     ← FR-23 to FR-25
  shared/
    components/
    hooks/
    utils/
    api/                ← TanStack Query definitions + zod schemas
```

**State split:**
- `TanStack Query v5` — all server-fetched data (Ergast, OpenF1 REST, news feed). `staleTime`/`gcTime` configured per query.
- `Zustand` — UI-only client state: `connectionStatus`, overlay visibility, pinned driver, timezone toggle selection. **Hard rule:** Zustand never holds server data.

**Live race state normalization (amended).** `RaceStateSnapshot` arrives from SignalR as a driver array. A pure `normalizeSnapshot` function converts it to `Record<driverId, DriverState>` *before* the Zustand `set` call. This allows per-driver `useRaceStore(s => s.drivers[id])` selectors with `shallow` equality — only that driver's component re-renders when that driver's data changes. Without normalization, full-snapshot replacement triggers re-renders on every 1-2s tick across all 20 driver components.

All `useRaceStore` calls must use slice selectors with `shallow` equality. `state => state.drivers` in a parent component is prohibited — it re-renders on every tick regardless of data change.

**SignalR singleton.** `HubConnection` instantiated as a module-level singleton, not inside `useEffect`. Prevents duplicate connection lifecycle on re-renders.

**React Router v7** with route-level `ErrorBoundary` on each feature route.

**Testing:** `vitest` + `@testing-library/react` for unit/component. `msw` (Mock Service Worker) for network layer in tests — reusable across Vitest and Playwright. `playwright` for E2E smoke tests (race-day canary). `WireMock.Net` on the C# side for external API contract tests.

---

### Infrastructure

**Local dev:** Independent processes (no docker-compose for POC). Frontend on `localhost:5173` (Vite), backend on `localhost:5000`. CORS configured for `localhost:5173` only.

**Config:** `.env.local` (frontend), `appsettings.Development.json` (backend). Both gitignored.

**Logging:** `ILogger<T>` throughout. No third-party logging infrastructure for POC.

**Post-POC deployment:** Vercel (frontend SPA) + Render (backend Docker container, free tier). Render free tier sleep behaviour is accounted for in SignalR reconnect policy.

---

## Implementation Patterns & Consistency Rules

_Step 5 patterns. Defined to prevent AI agent implementation conflicts across the React/TypeScript frontend and C# backend._

### Naming Patterns

**API endpoint naming (C# controllers)**
- Style: kebab-case, plural nouns — `/api/races`, `/api/drivers`, `/api/circuits/{circuitId}/profile`
- Route params: `{camelCase}` — `{circuitId}`, `{driverId}`, `{season}`
- No `/api/v1/` prefix for POC — add versioning post-POC when breaking changes are real

**JSON field naming**
- C# `System.Text.Json` policy: `JsonNamingPolicy.CamelCase` set globally in `Program.cs`
- OpenF1 and Ergast both use snake_case internally — DTOs map to camelCase at the C# boundary, raw external field names are never forwarded to the frontend
- Frontend always sees `driverNumber`, never `driver_number`

**TypeScript/React naming**
- Components: `PascalCase.tsx` (`DriverCard.tsx`)
- Hooks: `useCamelCase.ts` (`useRaceSnapshot.ts`)
- Utilities: `camelCase.ts` (`normalizeSnapshot.ts`)
- zod schemas: `PascalCaseSchema` (`RaceStateSnapshotSchema`)
- TanStack Query key factories: camelCase string tuples (`['standings', 'drivers', season]`)

**C# naming**
- Services: `*Service` suffix (`WinProbabilityService`, `RaceDataOrchestrator`)
- Typed HTTP clients: `*Client` suffix (`ErgastClient`, `OpenF1Client`)
- DTOs for external API shapes: `*Dto` suffix (`ErgastRaceResultDto`)
- Internal domain models: plain names (`RaceStateSnapshot`, `DriverState`)
- Controller method names: `PascalCase` (C# default); route template follows kebab-case rule above

---

### Structure Patterns

**Frontend test co-location**
- Tests live alongside the file they test: `DriverCard.tsx` → `DriverCard.test.tsx`
- MSW handlers (shared network mocks): `src/shared/mocks/handlers/`
- E2E tests: `playwright/` at project root

**Shared utilities location**
- Pure functions, no React dependency: `src/shared/utils/`
- React hooks with no feature affinity: `src/shared/hooks/`
- zod schemas + TanStack Query definitions: `src/shared/api/` — one file per external resource (`ergast.ts`, `openf1.ts`)
- Query key registry: `src/shared/api/queryKeys.ts` — agents must not inline string keys
- If a utility is only used by one feature, it lives inside that feature folder, not in `shared/`

**C# project structure**
```
F1App.Api/
  Controllers/        ← thin HTTP layer only — no business logic
  Services/           ← business logic, orchestrator, win probability
  Clients/            ← typed HTTP clients (ErgastClient, OpenF1Client)
  Models/             ← internal domain models
  Hubs/               ← SignalR hub
  Dtos/               ← external API response shapes (Ergast/OpenF1 JSON shapes)
```

---

### Format Patterns

**API responses (success)**
- Direct object or array — no wrapper envelope
- `GET /api/standings/drivers` returns `[{...}, ...]` directly, not `{ data: [...], status: "ok" }`

**API responses (error)**
- ProblemDetails RFC 7807 always:
```json
{ "type": "...", "title": "Not Found", "status": 404, "detail": "Circuit 'foo' not found" }
```

**Date/time in JSON**
- ISO 8601 with offset: `"2026-07-05T13:00:00+01:00"` — `DateTimeOffset` serializes this way by default
- Never Unix timestamps in API responses
- Frontend: always `new Date(isoString)` — never manual string parsing

**SignalR message conventions**
- Hub → client method name: `"RaceSnapshot"` (PascalCase noun)
- Client → hub method name: `"SubscribeToRace"` (PascalCase verb)
- Payload: always a typed object, never a raw string

**`localStorage` key format**
- Pattern: `f1app__{featureName}__{key}` — e.g. `f1app__fanCard__v2`, `f1app__streak__lastVisit`
- Version embedded in the key name (not as a field inside the value) — key change triggers automatic migration

---

### Communication Patterns

**TanStack Query key structure**
- Tuple format: `[resource, scope?, id?]`
- Examples: `['drivers', 'standings', season]`, `['circuit', circuitId, 'profile']`, `['news', 'feed']`
- All keys defined in `src/shared/api/queryKeys.ts` — never inlined as strings

**Zustand stores**
- One store file per feature slice: `src/features/live-race/store/liveRaceStore.ts`
- Export naming: `use{Feature}Store` — `useLiveRaceStore`, `useFanCardStore`
- No cross-slice dependencies at store level

**Zustand action naming**
- Mutating actions use verb prefix: `setConnectionStatus`, `pinDriver`, `resetFanCard`
- Never prefix with `on` (reserved for event handler props)

**Loading state (TanStack Query v5)**
- Use `isPending` for initial load (not `isLoading` — deprecated in v5)
- Use `isFetching` for background refetch
- Never derive loading state from `data === undefined`

---

### Process Patterns

**Error handling (frontend)**
- Route-level `ErrorBoundary` on each feature route — catches render errors
- TanStack Query error state: read from `query.error` in component, not via `onError` callback (deprecated pattern)
- User-facing messages: generic ("Something went wrong — try refreshing") — never expose raw API error detail
- Log full errors to console in dev via `queryCache.config.onError`

**Validation (frontend)**
- zod schemas validate at the TanStack Query `select` / `transform` step — never inside a component
- zod parse failure → query enters error state — identical path as HTTP error

**Error handling (backend)**
- Controllers catch nothing — services throw, global middleware converts to ProblemDetails
- `ILogger.LogError` for unexpected exceptions
- `ILogger.LogWarning` for expected degraded states (Ergast cache miss, OpenF1 join tolerance exceeded)

**Null vs undefined (TypeScript)**
- Prefer `undefined` over `null` for optional values in frontend code
- C# API responses omit fields with no value rather than returning JSON `null` — configure `JsonIgnoreCondition.WhenWritingNull` globally

---

### Enforcement Summary

All AI agents working on this project MUST:

1. Apply `JsonNamingPolicy.CamelCase` globally — never set casing per-controller or per-endpoint
2. Define TanStack Query keys only in `src/shared/api/queryKeys.ts` — never inline
3. Never put business logic in controllers — controllers are routing only
4. Never hold server data in Zustand — Zustand is UI state only
5. Use `DateTimeOffset` throughout C# — `DateTime` is banned in backend code
6. Use `isPending` not `isLoading` for TanStack Query v5 initial load state
7. Co-locate test files with the files they test (frontend); use `*Dto` suffix for external API shapes (backend)

---

## Project Structure & Boundaries

### FR → Directory Mapping

| FR range | Feature | Location |
|---|---|---|
| FR-1 to FR-6 | Race Calendar & Schedule | `frontend/src/features/calendar/` |
| FR-7 to FR-16 | Live Race Experience | `frontend/src/features/live-race/` |
| FR-17 to FR-19 | Championship & Standings | `frontend/src/features/standings/` |
| FR-20 to FR-22 | Deep Dive Profiles | `frontend/src/features/profiles/` |
| FR-23 to FR-25 | Fan Engagement | `frontend/src/features/fan-engagement/` |

### Complete Project Tree

**Root**
```
F1_poc/
├── frontend/
├── backend/
│   ├── F1App.Api/
│   └── F1App.Api.Tests/
├── .gitignore
└── _bmad-output/
```

**Frontend**
```
frontend/
├── index.html
├── package.json
├── vite.config.ts
├── tsconfig.json
├── tsconfig.app.json
├── tailwind.config.ts
├── playwright.config.ts
├── .env.local                         ← gitignored
├── .env.example
├── .gitignore
├── public/
│   └── circuit-configs/               ← per-circuit affine transform JSON (runtime asset)
│       └── monza.json
├── playwright/
│   └── smoke.test.ts                  ← race-day canary E2E
└── src/
    ├── main.tsx
    ├── App.tsx
    ├── router.tsx                     ← React Router v7, route-level ErrorBoundary per feature
    ├── features/
    │   ├── calendar/
    │   │   ├── CalendarPage.tsx       ← FR-1: season list
    │   │   ├── RaceWeekendCard.tsx    ← FR-2: card with top 3 drivers/constructors
    │   │   ├── RaceWeekendCard.test.tsx
    │   │   ├── RaceWeekendDetailView.tsx  ← FR-3: all sessions
    │   │   ├── ContextualData.tsx     ← FR-4: last year winner, championship delta
    │   │   ├── TimezoneToggle.tsx     ← FR-5
    │   │   ├── WinProbabilityWidget.tsx   ← FR-6
    │   │   └── index.ts
    │   ├── live-race/
    │   │   ├── LiveRacePage.tsx       ← FR-7 to FR-16 host
    │   │   ├── TrackMap/
    │   │   │   ├── TrackMap.tsx       ← FR-7: animated SVG circuit
    │   │   │   ├── TrackMap.test.tsx
    │   │   │   ├── DriverDot.tsx      ← FR-8: mini-sector colour coding
    │   │   │   └── useTrackInterpolation.ts  ← 3.7Hz → 60fps client-side interpolation
    │   │   ├── GapList/
    │   │   │   ├── GapList.tsx        ← FR-9: live gap list
    │   │   │   ├── GapList.test.tsx
    │   │   │   ├── DriverRow.tsx      ← FR-10: tyre + FR-15: championship delta per driver
    │   │   │   └── PitWindowIndicator.tsx  ← FR-11
    │   │   ├── LapTimeChart/
    │   │   │   ├── LapTimeChart.tsx   ← FR-12
    │   │   │   └── LapTimeChart.test.tsx
    │   │   ├── SectorBoard/
    │   │   │   └── SectorBoard.tsx    ← FR-13
    │   │   ├── EventTimeline/
    │   │   │   └── EventTimeline.tsx  ← FR-14
    │   │   ├── store/
    │   │   │   └── liveRaceStore.ts   ← Zustand: connectionStatus, pinned driver, overlay state
    │   │   ├── hooks/
    │   │   │   ├── useSignalRConnection.ts   ← manages hub lifecycle
    │   │   │   └── useFallbackState.ts       ← live→stale→fallback state machine
    │   │   ├── signalRClient.ts       ← module-level HubConnection singleton
    │   │   └── index.ts
    │   ├── standings/
    │   │   ├── StandingsPage.tsx      ← FR-17: driver/constructor toggle
    │   │   ├── DriversStandingsTable.tsx
    │   │   ├── ConstructorsStandingsTable.tsx
    │   │   ├── TrajectoryChart.tsx    ← FR-18
    │   │   ├── SeasonWrapped/
    │   │   │   ├── SeasonWrapped.tsx  ← FR-19
    │   │   │   └── SeasonWrappedCard.tsx   ← html-to-image export target
    │   │   └── index.ts
    │   ├── profiles/
    │   │   ├── CircuitProfilePage.tsx ← FR-20
    │   │   ├── DriverProfilePage.tsx  ← FR-21
    │   │   ├── HeadToHeadPage.tsx     ← FR-22
    │   │   └── index.ts
    │   └── fan-engagement/
    │       ├── NewsFeedPage.tsx       ← FR-23
    │       ├── FanCard/
    │       │   ├── FanCard.tsx        ← FR-25: display + html-to-image export
    │       │   ├── FanCardWizard.tsx  ← FR-25: setup flow
    │       │   └── useFanCardStore.ts ← Zustand + localStorage persistence
    │       ├── StreakCounter.tsx      ← FR-24
    │       └── index.ts
    └── shared/
        ├── api/
        │   ├── queryKeys.ts           ← ALL TanStack Query key factories (canonical)
        │   ├── ergast.ts              ← Ergast queries + zod schemas
        │   └── openf1.ts             ← OpenF1 REST queries + zod schemas
        ├── components/
        │   ├── ErrorBoundary.tsx      ← used on every feature route
        │   ├── LoadingSpinner.tsx
        │   └── CountryFlag.tsx
        ├── hooks/
        │   └── useLocalStorage.ts     ← versioned, with migration support
        ├── utils/
        │   ├── normalizeSnapshot.ts   ← driver[] → Record<driverId, DriverState>
        │   ├── normalizeSnapshot.test.ts
        │   ├── dateUtils.ts           ← ISO 8601 formatting, timezone helpers
        │   └── tyreUtils.ts           ← compound colours, pit window thresholds
        ├── mocks/
        │   ├── browser.ts             ← MSW browser worker setup
        │   └── handlers/
        │       ├── ergastHandlers.ts
        │       └── openf1Handlers.ts
        └── types/
            ├── f1.ts                  ← shared domain types (RaceStateSnapshot, DriverState, etc.)
            └── signalR.ts             ← SignalR message type contracts
```

**Backend**
```
backend/
├── F1App.Api/
│   ├── F1App.Api.csproj
│   ├── appsettings.json
│   ├── appsettings.Development.json   ← gitignored
│   ├── Program.cs                     ← DI, middleware, JSON policy, CORS, SignalR
│   ├── Controllers/
│   │   ├── RacesController.cs         ← FR-1,3,4: schedule, detail, context data
│   │   ├── StandingsController.cs     ← FR-17,18: standings, trajectory data
│   │   ├── DriversController.cs       ← FR-21,22: driver profile, head-to-head
│   │   ├── CircuitsController.cs      ← FR-20: circuit profile
│   │   ├── NewsController.cs          ← FR-23: RSS feed proxy
│   │   └── WinProbabilityController.cs ← FR-6: post-qualifying widget data
│   ├── Hubs/
│   │   └── RaceHub.cs                 ← SignalR hub, mapped to /hubs/race
│   ├── Services/
│   │   ├── RaceDataOrchestrator.cs    ← IHostedService: 5 OpenF1 polling Tasks,
│   │   │                                assembles RaceStateSnapshot, pushes via IHubContext
│   │   ├── WinProbabilityService.cs   ← post-qualifying heuristic calculation
│   │   ├── NewsFeedService.cs         ← CodeHollow.FeedReader, per-feed error isolation
│   │   └── CacheWarmupService.cs      ← IHostedService: proactive Ergast cache warm on startup
│   ├── Clients/
│   │   ├── ErgastClient.cs            ← typed IHttpClientFactory, configurable base URL
│   │   └── OpenF1Client.cs            ← typed IHttpClientFactory
│   ├── Models/
│   │   ├── RaceStateSnapshot.cs       ← hub broadcast payload
│   │   ├── DriverState.cs             ← per-driver state within snapshot
│   │   ├── FallbackTrigger.cs         ← enum: Timeout, EmptyArray, StaleTimestamp
│   │   └── WinProbabilityResult.cs
│   └── Dtos/
│       ├── Ergast/
│       │   ├── ErgastScheduleDto.cs
│       │   ├── ErgastRaceResultDto.cs
│       │   └── ErgastStandingsDto.cs
│       └── OpenF1/
│           ├── OpenF1PositionDto.cs
│           ├── OpenF1IntervalDto.cs
│           ├── OpenF1TyreDto.cs
│           ├── OpenF1CarDataDto.cs
│           └── OpenF1RaceControlDto.cs
└── F1App.Api.Tests/
    ├── F1App.Api.Tests.csproj
    ├── Services/
    │   ├── RaceDataOrchestratorTests.cs   ← TimeProvider injection, fallback state machine
    │   └── WinProbabilityServiceTests.cs
    └── Clients/
        └── ErgastClientContractTests.cs   ← WireMock.Net Ergast contract tests
```

### Integration Boundaries

**Frontend ↔ Backend**

| Channel | Direction | Contract |
|---|---|---|
| REST (`/api/*`) | React → C# | TanStack Query fetches, zod-validated responses |
| SignalR `/hubs/race` | C# → React | `RaceSnapshot` message, `RaceStateSnapshot` payload |
| CORS | — | `localhost:5173` only (POC) |

**Data flows**

```
OpenF1 API
  → RaceDataOrchestrator (IHostedService, 5 polling Tasks, 500ms join window)
  → RaceStateSnapshot (assembled every 1-2s)
  → RaceHub (IHubContext broadcast)
  → SignalR client (module-level singleton)
  → normalizeSnapshot() → Record<driverId, DriverState>
  → liveRaceStore (Zustand, slice selectors + shallow equality)
  → per-driver components (re-render only on their driver's data change)

Ergast API
  → ErgastClient (typed HttpClient)
  → IMemoryCache (TTL tiers)
  → Controller action
  → JSON response (camelCase)
  → TanStack Query (staleTime configured)
  → zod parse at query boundary
  → React component

RSS feeds (Formula1.com, Autosport, RaceFans)
  → NewsFeedService (CodeHollow.FeedReader, per-feed error isolation)
  → IMemoryCache (15-min TTL)
  → NewsController
  → NewsFeedPage (TanStack Query)
```

**External boundary points** — the only places external API shapes are handled:
- `Clients/ErgastClient.cs` → deserializes into `Dtos/Ergast/`
- `Clients/OpenF1Client.cs` → deserializes into `Dtos/OpenF1/`
- `src/shared/api/ergast.ts` → zod schemas for Ergast REST responses
- `src/shared/api/openf1.ts` → zod schemas for direct OpenF1 REST queries

Internal models (`RaceStateSnapshot`, `DriverState`) use camelCase JSON and are never coupled to external API field shapes.

---

## Architecture Validation Results

### Coherence Validation ✅

**Decision Compatibility:** All technology choices are compatible. React 19 + Vite + TanStack Query v5 + Zustand + React Router v7 have no version conflicts. ASP.NET Core 10 SignalR is built-in. `IMemoryCache` + `IHostedService` are BCL primitives. `CodeHollow.FeedReader` is .NET Standard compatible with .NET 10. `html-to-image` has no React 19 issues. `msw` confirmed compatible with both Vitest and Playwright.

**Pattern Consistency:** Feature-based folder structure maps 1:1 to the FR-to-directory table with no orphaned FRs. `JsonNamingPolicy.CamelCase` applied globally — no per-controller overrides. `use{Feature}Store` naming consistent across all Zustand stores. `*Dto` suffix consistent across all external API shape classes. Query keys centralized in `queryKeys.ts`.

**Structure Alignment:** Every architectural decision (orchestrator, fallback state machine, snapshot normalization) has a named file in the project tree. External API shape handling is fully contained to `Clients/` + `Dtos/` (backend) and `shared/api/` (frontend). `signalRClient.ts` as module-level singleton is a named file — pattern is enforceable by code review.

---

### Requirements Coverage Validation ✅

**All 25 FRs covered:**

| FR | Component | Status |
|---|---|---|
| FR-1 season list | `CalendarPage.tsx` | ✅ |
| FR-2 race card with top-3 | `RaceWeekendCard.tsx` + standings query | ✅ |
| FR-3 session detail | `RaceWeekendDetailView.tsx` | ✅ |
| FR-4 contextual data | `ContextualData.tsx` + `RacesController` | ✅ |
| FR-5 timezone toggle | `TimezoneToggle.tsx` | ✅ |
| FR-6 win probability | `WinProbabilityWidget` + `WinProbabilityController` + `WinProbabilityService` | ✅ |
| FR-7 animated track map | `TrackMap.tsx` + `useTrackInterpolation.ts` + `circuit-configs/` | ✅ |
| FR-8 mini-sector colours | `DriverDot.tsx` | ✅ |
| FR-9 live gap list | `GapList.tsx` | ✅ |
| FR-10 tyre tracker | `DriverRow.tsx` | ✅ |
| FR-11 pit window estimator | `PitWindowIndicator.tsx` + `tyreUtils.ts` | ✅ |
| FR-12 lap time chart | `LapTimeChart.tsx` | ✅ |
| FR-13 fastest sector board | `SectorBoard.tsx` | ✅ |
| FR-14 race event timeline | `EventTimeline.tsx` | ✅ |
| FR-15 championship impact | `DriverRow.tsx` (delta annotation) | ✅ |
| FR-16 fallback to last race | `useFallbackState.ts` + `liveRaceStore.ts` | ✅ |
| FR-17 standings toggle | `StandingsPage.tsx` + two table components | ✅ |
| FR-18 trajectory chart | `TrajectoryChart.tsx` | ✅ |
| FR-19 Season Wrapped | `SeasonWrapped.tsx` + `SeasonWrappedCard.tsx` + `html-to-image` | ✅ |
| FR-20 circuit profile | `CircuitProfilePage.tsx` + `CircuitsController` | ✅ |
| FR-21 driver career | `DriverProfilePage.tsx` + `DriversController` | ✅ |
| FR-22 head-to-head | `HeadToHeadPage.tsx` + `DriversController` | ✅ |
| FR-23 news feed | `NewsFeedPage.tsx` + `NewsController` + `NewsFeedService` | ✅ |
| FR-24 streak counter | `StreakCounter.tsx` + `useLocalStorage.ts` | ✅ |
| FR-25 fan card | `FanCard.tsx` + `FanCardWizard.tsx` + `useFanCardStore.ts` | ✅ |

**NFR coverage:**

| NFR | Architectural support | Status |
|---|---|---|
| Live page loads within 10s | `CacheWarmupService` + SignalR direct connect | ✅ |
| Smooth car animation | `useTrackInterpolation.ts` (3.7Hz → 60fps) | ✅ |
| No backend DB (POC) | `IMemoryCache` + `localStorage` only | ✅ |
| Free public APIs only | Ergast + OpenF1 + RSS, all free/no-auth | ✅ |
| Responsive web | Tailwind CSS v4 | ✅ |
| Client-side image generation | `html-to-image` in browser | ✅ |

---

### Gap Analysis Results

**Critical gaps:** None.

**Important gaps (resolved — included below):**

1. **Environment variable names** — standardised here to prevent agent divergence:
   - Frontend (`.env.local`): `VITE_API_BASE_URL=http://localhost:5000`, `VITE_SIGNALR_HUB_URL=http://localhost:5000/hubs/race`
   - Backend (`appsettings.Development.json`): `"ErgastBaseUrl": "https://ergast.com/api/f1"`, `"OpenF1BaseUrl": "https://api.openf1.org/v1"`, `"AllowedOrigins": ["http://localhost:5173"]`, `"JoinToleranceMs": 500`

2. **Circuit config JSON schema** — canonical shape for `public/circuit-configs/{circuitId}.json`:
   ```json
   {
     "circuitId": "monza",
     "transform": {
       "scaleX": 0.0023,
       "scaleY": -0.0023,
       "translateX": 450,
       "translateY": 380,
       "rotationDeg": 15
     },
     "viewBox": "0 0 900 600"
   }
   ```
   `TrackMap.tsx` loads this at runtime via `fetch('/circuit-configs/{circuitId}.json')`. New circuits are added by dropping a calibrated JSON file — no code change required.

**Nice-to-have gaps:**
- `IMemoryCache` key naming: recommend a `CacheKeys` static class with string constants to prevent key collision across services
- SignalR reconnect policy: use `withAutomaticReconnect([0, 2000, 5000, 10000, 30000, 60000, 60000])` in `signalRClient.ts` with a final indefinite retry loop

---

### Architecture Completeness Checklist

**Requirements Analysis**
- [x] Project context thoroughly analyzed
- [x] Scale and complexity assessed
- [x] Technical constraints identified
- [x] Cross-cutting concerns mapped

**Architectural Decisions**
- [x] Critical decisions documented with versions
- [x] Technology stack fully specified
- [x] Integration patterns defined
- [x] Performance considerations addressed

**Implementation Patterns**
- [x] Naming conventions established
- [x] Structure patterns defined
- [x] Communication patterns specified
- [x] Process patterns documented

**Project Structure**
- [x] Complete directory structure defined
- [x] Component boundaries established
- [x] Integration points mapped
- [x] Requirements to structure mapping complete

---

### Architecture Readiness Assessment

**Overall Status: READY FOR IMPLEMENTATION**

**Confidence Level: High** — all 16 checklist items confirmed, no critical gaps, two important gaps resolved inline above.

**Key strengths:**
- All 25 FRs have a named file — no FR is "handled somewhere in the backend"
- Real-time pipeline's trickiest decisions (orchestrator shape, recovery debounce, render normalization) are all explicit
- External API shape isolation clean — two boundary points each side, no leakage

**Areas for future enhancement (post-POC):**
- Add `/api/v1/` versioning prefix when breaking changes become real
- Migrate `IMemoryCache` to Redis if Render deployment needs multi-instance support
- Replace `localStorage` streak/fan card with account-backed storage after auth introduction

### Implementation Handoff

**AI Agent Guidelines:**
- Follow all architectural decisions exactly as documented — no local improvisation on naming, state placement, or error handling
- Use `queryKeys.ts` for all TanStack Query keys — never inline strings
- The `normalizeSnapshot` utility must run before every Zustand `set` call on live race data
- `signalRClient.ts` is the single SignalR connection — never instantiate `HubConnection` anywhere else
- `TimeProvider` must be injected into all hosted services — never call `DateTimeOffset.UtcNow` directly in a service under test

**First Implementation Priority:**
```bash
# Frontend
npm create vite@latest frontend -- --template react-ts

# Backend
dotnet new webapi -n F1App.Api --framework net10.0
dotnet new xunit -n F1App.Api.Tests --framework net10.0
dotnet new sln -n F1_poc
dotnet sln add backend/F1App.Api/F1App.Api.csproj
dotnet sln add backend/F1App.Api.Tests/F1App.Api.Tests.csproj
```
