# Code Review Defect Backlog

**Project:** F1_poc  
**Created:** 2026-06-17  
**Source:** Adversarial code review — backend (`F1App.Api`) and frontend (`frontend/src`)  
**Total tickets:** 46 (0 Critical · 8 High · 22 Medium · 16 Low)

Use ticket IDs (`F1-CR-###`) when creating Jira issues or sprint tasks.

---

## Summary by Component

| Component | High | Medium | Low |
|-----------|------|--------|-----|
| Backend   | 5    | 13     | 8   |
| Frontend  | 3    | 9      | 8   |

---

## High Priority

---

### F1-CR-001 — Production startup crash: missing `OpenF1BaseUrl` in committed config

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | High |
| **Component** | Backend |
| **Labels** | configuration, reliability, deployment |

**Description**  
`Program.cs` reads `OpenF1BaseUrl` with a null-forgiving operator and immediately calls `.TrimEnd('/')`. Committed `appsettings.json` defines only `ErgastBaseUrl`; `OpenF1BaseUrl` exists only in gitignored `appsettings.Development.json`. In non-Development environments the API throws `NullReferenceException` at startup.

**Affected files**
- `backend/F1App.Api/Program.cs` (line 46)
- `backend/F1App.Api/appsettings.json`

**Steps to reproduce**
1. Deploy or run the API with `ASPNETCORE_ENVIRONMENT=Production` (or any env without `OpenF1BaseUrl`).
2. Start the application.
3. Observe startup failure before the app listens on a port.

**Expected behavior**  
Application starts with a clear configuration error, or all required settings are present in committed config.

**Actual behavior**  
`NullReferenceException` at startup.

**Acceptance criteria**
- [ ] `OpenF1BaseUrl` (and `JoinToleranceMs` if required) are present in committed `appsettings.json` or validated via `GetRequiredSection` with a descriptive error message.
- [ ] `appsettings.Development.example.json` is committed so new clones know required dev settings.
- [ ] Application starts successfully in Production with documented env/config values.

**Technical notes**  
Prefer `builder.Configuration.GetRequiredSection("OpenF1BaseUrl")` or explicit null-check with `throw new InvalidOperationException("OpenF1BaseUrl is required")`.

---

### F1-CR-002 — No authentication on REST API or SignalR hub

| Field | Value |
|-------|-------|
| **Type** | Security |
| **Severity** | High |
| **Component** | Backend |
| **Labels** | security, auth, signalr, api |

**Description**  
No `AddAuthentication`, no `[Authorize]` attributes, and no hub authorization policy. Any client can call all `/api/*` endpoints and subscribe to `/hubs/race` live race snapshots without credentials.

**Affected files**
- `backend/F1App.Api/Program.cs`
- `backend/F1App.Api/Controllers/*`
- `backend/F1App.Api/Hubs/RaceHub.cs`

**Steps to reproduce**
1. Start the API without any auth configuration.
2. `curl http://localhost:<port>/api/races` — returns data.
3. Connect a SignalR client to `/hubs/race` — receives `RaceSnapshot` events.

**Expected behavior**  
Public POC may be acceptable locally; production deployment requires access control.

**Actual behavior**  
Fully open API and hub.

**Acceptance criteria**
- [ ] Document current open-access posture as intentional for POC, or
- [ ] Implement authentication (API key, JWT, or reverse-proxy auth) on REST endpoints.
- [ ] Apply authorization policy to SignalR hub connections.
- [ ] Update deployment docs with auth requirements.

**Technical notes**  
Acceptable for local POC. Block production deploy until addressed.

---

### F1-CR-003 — No rate limiting on public API endpoints

| Field | Value |
|-------|-------|
| **Type** | Security |
| **Severity** | High |
| **Component** | Backend |
| **Labels** | security, dos, rate-limiting |

**Description**  
No ASP.NET Core rate limiter is configured. Unauthenticated endpoints can be hammered. Each miss on `/api/races/{round}/win-probability` triggers upstream Ergast calls and populates `IMemoryCache`, amplifying DoS cost to the server and third-party APIs.

**Affected files**
- `backend/F1App.Api/Program.cs`
- `backend/F1App.Api/Services/WinProbabilityService.cs`
- `backend/F1App.Api/Controllers/WinProbabilityController.cs`

**Steps to reproduce**
1. Send rapid repeated requests to `/api/races/{round}/win-probability` with varying `round` values.
2. Observe no throttling; memory cache and upstream API calls grow.

**Expected behavior**  
Requests are throttled per IP or per route to prevent abuse.

**Actual behavior**  
Unlimited requests accepted.

**Acceptance criteria**
- [ ] ASP.NET Core rate limiting middleware added (per-IP and/or per-route).
- [ ] Win-probability and other expensive endpoints have stricter limits.
- [ ] Rate-limited responses return HTTP 429 with appropriate headers.
- [ ] Load test confirms throttling under abuse pattern.

**Technical notes**  
Combine with F1-CR-010 (round validation) and cache size limits.

---

### F1-CR-004 — Singleton hosted service holds captive transient `IOpenF1Client`

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | High |
| **Component** | Backend |
| **Labels** | di, reliability, httpclient |

**Description**  
`RaceDataOrchestrator` is registered as a singleton `BackgroundService` but constructor-injects `IOpenF1Client` from `AddHttpClient`, which is transient. DI resolves the client once at startup and retains it for the process lifetime, undermining `IHttpClientFactory` handler rotation and DNS refresh.

**Affected files**
- `backend/F1App.Api/Program.cs` (lines 47–51, 56)
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` (lines 12–18)

**Steps to reproduce**
1. Run the API for an extended period with DNS changes or certificate rotation on OpenF1 upstream.
2. Observe potential stale connections compared to scoped client usage.

**Expected behavior**  
HttpClient instances are created per scope or via factory per operation.

**Actual behavior**  
Single `IOpenF1Client` instance held for app lifetime.

**Acceptance criteria**
- [ ] `IOpenF1Client` is resolved inside each poll via `IServiceScopeFactory` (same pattern as `IErgastClient` at lines 78–80), or a dedicated factory is used.
- [ ] No captive dependency on transient `HttpClient` in singleton service.
- [ ] Existing orchestrator tests pass; add test verifying scoped resolution if feasible.

**Technical notes**  
Mirror the existing `scopeFactory.CreateScope()` pattern used for `IErgastClient`.

---

### F1-CR-005 — Production CORS not configured in committed settings

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | High |
| **Component** | Backend |
| **Labels** | configuration, cors, deployment |

**Description**  
`AllowedOrigins` defaults to `[]` when absent. Committed `appsettings.json` has no `AllowedOrigins`. Cross-origin browser requests (including SignalR with `AllowCredentials()`) are blocked in production unless env-specific config is manually added.

**Affected files**
- `backend/F1App.Api/Program.cs` (lines 21–28, 63–68)
- `backend/F1App.Api/appsettings.json`

**Steps to reproduce**
1. Deploy API with only committed `appsettings.json`.
2. Open frontend from a different origin.
3. Observe CORS failures on REST and SignalR.

**Expected behavior**  
Production origins are defined in environment-specific configuration.

**Actual behavior**  
Empty origins array; all cross-origin requests blocked.

**Acceptance criteria**
- [ ] Production `AllowedOrigins` documented and configured via `appsettings.Production.json` or environment variables.
- [ ] No use of `*` with `AllowCredentials()`.
- [ ] Frontend can connect to API and SignalR hub from deployed origin.

**Technical notes**  
Warning is already logged at startup when origins are empty (Program.cs lines 63–68).

---

### F1-CR-006 — SignalR payloads not validated at runtime

| Field | Value |
|-------|-------|
| **Type** | Security |
| **Severity** | High |
| **Component** | Frontend |
| **Labels** | security, signalr, validation |

**Description**  
`RaceSnapshot` messages are typed only at compile time. Incoming WebSocket data is passed directly into `normalizeSnapshot(snapshot.drivers)` with no Zod (or similar) validation, unlike REST calls in `ergast.ts`. A compromised hub, MITM on `ws://`, or malicious proxy could send malformed or oversized payloads into application state.

**Affected files**
- `frontend/src/features/live-race/hooks/useSignalRConnection.ts` (lines 19–21)
- `frontend/src/shared/types/signalR.ts`

**Steps to reproduce**
1. Connect to SignalR hub.
2. Send a `RaceSnapshot` message with invalid types, missing fields, or oversized strings.
3. Observe data accepted into Zustand store without validation.

**Expected behavior**  
All inbound SignalR payloads are validated before updating state; invalid messages are rejected.

**Actual behavior**  
TypeScript types only; no runtime validation.

**Acceptance criteria**
- [ ] Zod schema defined for `RaceSnapshotMessage` and `DriverState`.
- [ ] `safeParse` runs in `handleSnapshot` before `setDrivers`.
- [ ] Invalid payloads are logged (dev) and ignored without crashing UI.
- [ ] Unit test covers rejection of malformed snapshot.

**Technical notes**  
Mirror the `fetchJson` + Zod pattern in `ergast.ts`.

---

### F1-CR-007 — Unvalidated `teamColour` applied to inline styles

| Field | Value |
|-------|-------|
| **Type** | Security |
| **Severity** | High |
| **Component** | Frontend |
| **Labels** | security, xss, signalr |

**Description**  
`backgroundColor: \`#${driver.teamColour}\`` uses server/WebSocket data without format validation. React mitigates most XSS via text escaping, but style props are a separate trust boundary. Invalid or crafted values could cause unexpected rendering.

**Affected files**
- `frontend/src/features/live-race/GapList/DriverRow.tsx` (lines 44–48)

**Steps to reproduce**
1. Receive a snapshot where `teamColour` is not a 6-digit hex string (e.g. `url(...)`, invalid chars).
2. Observe value applied directly to `style.backgroundColor`.

**Expected behavior**  
Only valid hex colour codes are applied; invalid values use a safe fallback.

**Actual behavior**  
Any string from snapshot is interpolated into CSS.

**Acceptance criteria**
- [ ] `teamColour` validated against `/^[0-9A-Fa-f]{6}$/` before use.
- [ ] Fallback neutral colour used when validation fails.
- [ ] Test covers invalid `teamColour` input.

**Technical notes**  
Can be combined with F1-CR-006 schema validation.

---

### F1-CR-008 — No Content-Security-Policy configured

| Field | Value |
|-------|-------|
| **Type** | Security |
| **Severity** | High |
| **Component** | Frontend |
| **Labels** | security, csp, deployment |

**Description**  
No CSP meta tag or build-time headers. A future XSS bug or compromised dependency would have fewer browser-level guardrails.

**Affected files**
- `frontend/index.html`
- `frontend/vite.config.ts`

**Steps to reproduce**
1. Inspect response headers or `index.html` for CSP directives.
2. Confirm none are present.

**Expected behavior**  
Production build serves restrictive CSP headers.

**Actual behavior**  
No CSP configured.

**Acceptance criteria**
- [ ] CSP added via hosting (preferred) or Vite plugin for production builds.
- [ ] `default-src 'self'`; `connect-src` includes API and SignalR origins.
- [ ] `script-src 'self'` (adjust for Vite dev if needed).
- [ ] CSP documented in deployment guide.

**Technical notes**  
Dev and prod policies may differ; avoid breaking HMR in development.

---

## Medium Priority — Backend

---

### F1-CR-009 — `AllowedHosts: "*"` disables host header filtering

| Field | Value |
|-------|-------|
| **Type** | Security |
| **Severity** | Medium |
| **Component** | Backend |
| **Labels** | security, configuration |

**Description**  
Wildcard `AllowedHosts` in `appsettings.json` accepts any Host header, weakening host-header attack protections.

**Affected files**
- `backend/F1App.Api/appsettings.json` (line 8)

**Acceptance criteria**
- [ ] Production config sets explicit hostnames (e.g. API domain).
- [ ] Wildcard removed from production settings.

---

### F1-CR-010 — Unbounded memory cache growth via unvalidated `round` parameter

| Field | Value |
|-------|-------|
| **Type** | Security |
| **Severity** | Medium |
| **Component** | Backend |
| **Labels** | security, cache, dos |

**Description**  
`round` route parameter is only constrained to `int` (negative and very large values allowed). Each distinct round creates a cache entry (`winProbability:{round}`). `AddMemoryCache()` has no `SizeLimit`. An attacker can enumerate rounds to grow memory and trigger Ergast calls.

**Affected files**
- `backend/F1App.Api/Services/WinProbabilityService.cs` (lines 15–27)
- `backend/F1App.Api/Services/CacheKeys.cs`
- `backend/F1App.Api/Controllers/WinProbabilityController.cs`

**Acceptance criteria**
- [ ] `round` validated against current season schedule (e.g. 1–N).
- [ ] `IMemoryCache` configured with size limit and eviction policy.
- [ ] Invalid `round` returns 400 or 404 without upstream call.

---

### F1-CR-011 — `InvalidOperationException` incorrectly mapped to HTTP 502

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Backend |
| **Labels** | api, error-handling |

**Description**  
Global exception handler treats `InvalidOperationException` as upstream failure (502). `ErgastClient` throws `InvalidOperationException` for empty/malformed application-level responses. Clients see 502 instead of 500, masking internal/data-contract bugs.

**Affected files**
- `backend/F1App.Api/Program.cs` (lines 77–82)
- `backend/F1App.Api/Clients/ErgastClient.cs`

**Acceptance criteria**
- [ ] 502 reserved for `HttpRequestException`, timeouts, and true upstream unavailability.
- [ ] Data contract violations map to 500 (or 503) with distinct problem type.
- [ ] Test covers exception classification.

---

### F1-CR-012 — Race weekend check fails open — continuous OpenF1 polling

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Backend |
| **Labels** | reliability, polling, openf1 |

**Description**  
If Ergast schedule check fails, orchestrator assumes race weekend is active and starts five polling loops (~800 ms–10 s intervals) against OpenF1 for up to 24 hours.

**Affected files**
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` (lines 97–103)

**Acceptance criteria**
- [ ] Fail closed (or heavy backoff) when schedule cannot be determined.
- [ ] Warning logged with metric/alert hook for sustained fail-open state.
- [ ] Test covers Ergast failure path.

---

### F1-CR-013 — Cross-session stale live state not cleared in orchestrator

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Backend |
| **Labels** | live-race, data-integrity |

**Description**  
`_latestPositions`, `_latestIntervals`, `_latestStints`, `_driverCurrentLap` are never cleared on session transition. `PollStintsAsync` updates existing keys but does not remove stale drivers. After FP1 → qualifying → race transitions, ghost drivers can appear in snapshots.

**Affected files**
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` (lines 23–33, 152–224, 239–288)

**Acceptance criteria**
- [ ] State dictionaries rebuilt or cleared when `session_key` changes.
- [ ] Stint poll removes drivers absent from latest batch.
- [ ] Test covers session transition without ghost drivers.

---

### F1-CR-014 — Unguarded `Parse` on upstream string fields

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Backend |
| **Labels** | reliability, parsing, ergast |

**Description**  
`int.Parse`, `decimal.Parse`, `DateTimeOffset.Parse` on Ergast strings are uncaught. Unexpected upstream format causes unhandled `FormatException` → HTTP 500. `WinProbabilityService` uses `TryParse` for grid position but sibling services do not.

**Affected files**
- `backend/F1App.Api/Services/StandingsService.cs` (lines 45–56)
- `backend/F1App.Api/Services/RaceScheduleService.cs` (lines 65, 85, 138–151, 196–199)

**Acceptance criteria**
- [ ] All upstream string parsing uses `TryParse` with graceful degradation.
- [ ] Malformed upstream data logged with context.
- [ ] Tests cover unexpected format handling.

---

### F1-CR-015 — `ToDictionary` on driver list fails on duplicate keys

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Backend |
| **Labels** | live-race, parsing |

**Description**  
`drivers.ToDictionary(d => d.DriverNumber)` throws on duplicate `driver_number`. Exception is caught by broad catch, leaving stale `_driverInfo`.

**Affected files**
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` (lines 112–118)

**Acceptance criteria**
- [ ] Duplicate driver numbers handled via `GroupBy`/`DistinctBy` or first-wins policy.
- [ ] Warning logged when duplicates detected.

---

### F1-CR-016 — OpenF1 null response body silently becomes empty array

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Backend |
| **Labels** | reliability, openf1 |

**Description**  
`GetFromJsonAsync(...) ?? []` treats null body as success with no data. HTTP errors still throw, but empty/null successful responses are indistinguishable from "no updates".

**Affected files**
- `backend/F1App.Api/Clients/OpenF1Client.cs` (lines 14, 23, 28, 33, 42)

**Acceptance criteria**
- [ ] Null deserialize distinguished from valid empty array.
- [ ] Anomalies logged or metered.

---

### F1-CR-017 — Integration tests boot real background poller

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Backend |
| **Labels** | testing, ci, flaky |

**Description**  
`WebApplicationFactory<Program>` boots full app including `RaceDataOrchestrator`. Tests mock `IErgastClient` but not `IOpenF1Client`. When race weekend is active (or fail-open triggers), tests make real OpenF1 network calls — flaky CI and non-determinism.

**Affected files**
- `backend/F1App.Api/Program.cs` (line 56)
- `backend/F1App.Api.Tests/Controllers/*Tests.cs`

**Acceptance criteria**
- [ ] Hosted service replaced with no-op in test host, or `IOpenF1Client` mocked globally.
- [ ] CI tests run without external network dependency.

---

### F1-CR-018 — `Polling:ForceActive` can force polling in any environment

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Backend |
| **Labels** | configuration, openf1 |

**Description**  
`configuration.GetValue("Polling:ForceActive", false)` forces 24h OpenF1 polling regardless of calendar. Mis-set production env var hammers third-party APIs.

**Affected files**
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` (lines 69–70)
- `backend/F1App.Api/Properties/launchSettings.json`

**Acceptance criteria**
- [ ] `Polling:ForceActive` restricted to Development environment, or requires explicit ops flag with audit logging.

---

### F1-CR-019 — No security headers (HSTS, X-Content-Type-Options) in pipeline

| Field | Value |
|-------|-------|
| **Type** | Security |
| **Severity** | Medium |
| **Component** | Backend |
| **Labels** | security, headers |

**Description**  
Pipeline has CORS and HTTPS redirection only. No HSTS, `X-Content-Type-Options`, or related headers.

**Affected files**
- `backend/F1App.Api/Program.cs` (lines 96–110)

**Acceptance criteria**
- [ ] `UseHsts()` enabled in non-Development environments.
- [ ] Standard security headers middleware added for production.

---

### F1-CR-020 — Upstream base URLs not validated at startup

| Field | Value |
|-------|-------|
| **Type** | Security |
| **Severity** | Medium |
| **Component** | Backend |
| **Labels** | security, configuration, ssrf |

**Description**  
`ErgastBaseUrl` and `OpenF1BaseUrl` taken from config without scheme/host allowlisting. Compromised deployment config could point HttpClient at internal addresses (config-level SSRF).

**Affected files**
- `backend/F1App.Api/Program.cs` (lines 39–50)

**Acceptance criteria**
- [ ] Startup validation enforces HTTPS and allowlisted hostnames.
- [ ] Invalid config fails fast with clear error.

---

### F1-CR-021 — Poll cursor advances on `UtcNow` instead of max event timestamp

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Backend |
| **Labels** | live-race, data-integrity |

**Description**  
When OpenF1 returns rows, `_lastPositionPoll` / `_lastIntervalPoll` / `_lastLapPoll` jump to `timeProvider.GetUtcNow()` instead of max `date` in the batch. Clock skew or delayed records can be skipped on the next `date>` filter.

**Affected files**
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` (lines 157–159, 176–178, 212–214)

**Acceptance criteria**
- [ ] Cursor advanced to `max(record.Date)` with small overlap buffer.
- [ ] Test covers delayed-record scenario.

---

## Medium Priority — Frontend

---

### F1-CR-022 — Missing `VITE_SIGNALR_HUB_URL` crashes entire app at import time

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Frontend |
| **Labels** | reliability, routing, signalr |

**Description**  
`throw new Error(...)` in `signalRClient.ts` runs at module load. Because `router.tsx` statically imports `LiveRacePage`, calendar and all routes fail to boot if env var is unset — even when user never visits `/live`.

**Affected files**
- `frontend/src/features/live-race/signalRClient.ts` (lines 3–6)
- `frontend/src/router.tsx`

**Acceptance criteria**
- [ ] Live-race route lazy-loaded (`React.lazy`) or hub creation deferred to hook mount.
- [ ] Calendar and other routes work without SignalR env configured.
- [ ] `/live` shows route-local error when hub URL missing.

---

### F1-CR-023 — SignalR connection never stopped on component unmount

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Frontend |
| **Labels** | reliability, signalr |

**Description**  
Cleanup only calls `off('RaceSnapshot')`. `raceHubConnection.stop()` is never invoked. After visiting `/live`, WebSocket stays open for the session (module-level singleton).

**Affected files**
- `frontend/src/features/live-race/hooks/useSignalRConnection.ts` (lines 33–42)
- `frontend/src/features/live-race/signalRClient.ts`

**Acceptance criteria**
- [ ] `raceHubConnection.stop()` called when leaving `/live` or when last subscriber unmounts.
- [ ] Re-entering `/live` reconnects cleanly.

---

### F1-CR-024 — Invalid route param triggers fetch to `/api/races/NaN`

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Frontend |
| **Labels** | routing, api |

**Description**  
`useRaceDetail(Number(round))` has no `enabled` guard. `round` undefined or non-numeric → `NaN` → fetch to `/api/races/NaN`. `useWinProbability` correctly guards with `enabled: !isNaN(round) && round > 0`; `useRaceDetail` does not.

**Affected files**
- `frontend/src/features/calendar/RaceWeekendDetailView.tsx` (lines 24–26)
- `frontend/src/shared/api/ergast.ts` (lines 144–150, 158)

**Acceptance criteria**
- [ ] `round` parsed and validated once.
- [ ] `useRaceDetail` uses `enabled: Number.isFinite(round) && round > 0`.
- [ ] Invalid URL shows user-friendly error without API call.

---

### F1-CR-025 — Server timestamp `capturedAt` ignored for snapshot freshness

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Frontend |
| **Labels** | live-race, ux |

**Description**  
`setLastSnapshotTime(new Date())` uses client clock instead of `snapshot.capturedAt`, hiding latency and clock skew from stale-data detection.

**Affected files**
- `frontend/src/features/live-race/hooks/useSignalRConnection.ts` (line 21)
- `frontend/src/shared/types/f1.ts` (line 16)

**Acceptance criteria**
- [ ] `setLastSnapshotTime` uses validated `snapshot.capturedAt`.
- [ ] Falls back to client time only when server timestamp invalid.

---

### F1-CR-026 — `parseFloat` battle-gap logic mishandles F1 gap strings

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Frontend |
| **Labels** | live-race, ui |

**Description**  
`parseFloat(driver.gapToCarAhead) < 1.0` mis-handles strings like `"+1 LAP"`, `""`, or `"OUT"` (`parseFloat("+1 LAP")` → `1`, `parseFloat("LAP")` → `NaN`). Battle highlighting can be incorrect.

**Affected files**
- `frontend/src/features/live-race/GapList/DriverRow.tsx` (lines 13–16)

**Acceptance criteria**
- [ ] Battle gap logic parses only strict numeric formats (e.g. `/^\d+\.\d+$/`) or uses numeric field from backend.
- [ ] Tests cover `"+1 LAP"`, empty, and `OUT` cases.

---

### F1-CR-027 — Unused production dependencies increase supply-chain surface

| Field | Value |
|-------|-------|
| **Type** | Technical Debt |
| **Severity** | Medium |
| **Component** | Frontend |
| **Labels** | dependencies, security |

**Description**  
`html-to-image` and `recharts` are in `dependencies` but have no imports under `src/`. They still ship in `node_modules` and security audits.

**Affected files**
- `frontend/package.json` (lines 17, 21)

**Acceptance criteria**
- [ ] Unused packages removed, or lazy-imported when features are implemented.

---

### F1-CR-028 — TypeScript strict mode not enabled

| Field | Value |
|-------|-------|
| **Type** | Technical Debt |
| **Severity** | Medium |
| **Component** | Frontend |
| **Labels** | typescript, maintainability |

**Description**  
`tsconfig.app.json` has no `"strict": true`. Runtime bugs from unchecked `undefined` are easier to miss.

**Affected files**
- `frontend/tsconfig.app.json`

**Acceptance criteria**
- [ ] `strictNullChecks` enabled (incremental path documented).
- [ ] Build passes with no new suppressions without justification.

---

### F1-CR-029 — SignalR client lacks transport and origin hardening

| Field | Value |
|-------|-------|
| **Type** | Security |
| **Severity** | Medium |
| **Component** | Frontend |
| **Labels** | security, signalr, deployment |

**Description**  
`.withUrl(hubUrl)` with no explicit `wss://` enforcement in production. Backend hub is unauthenticated. Any client can open connections — connection exhaustion risk is server-side.

**Affected files**
- `frontend/src/features/live-race/signalRClient.ts` (lines 8–16)

**Acceptance criteria**
- [ ] Production builds require `wss://` hub URL (build-time or runtime check).
- [ ] Deployment docs state TLS requirement.
- [ ] Paired with backend rate limiting (F1-CR-003).

---

### F1-CR-030 — Global QueryClient uses defaults; all queries disable retry

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Medium |
| **Component** | Frontend |
| **Labels** | reliability, react-query |

**Description**  
`new QueryClient()` with no defaults; every hook sets `retry: false`. Transient network blips immediately surface as errors.

**Affected files**
- `frontend/src/main.tsx` (line 8)
- `frontend/src/shared/api/ergast.ts` (lines 122, 131, 140, 149, 159)

**Acceptance criteria**
- [ ] Sensible global defaults (`retry: 1`, appropriate `staleTime`).
- [ ] `retry: false` retained only where intentional.

---

## Low Priority — Backend

---

### F1-CR-031 — No input range validation on `round` route parameter

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Low |
| **Component** | Backend |

**Description**  
`{round:int}` accepts negatives and values far outside calendar.

**Affected files**
- `backend/F1App.Api/Controllers/RacesController.cs`
- `backend/F1App.Api/Controllers/WinProbabilityController.cs`

**Acceptance criteria**
- [ ] `[Range(1, 30)]` or validation against cached schedule.

---

### F1-CR-032 — Client disconnect `TaskCanceledException` mapped to 502

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Low |
| **Component** | Backend |

**Description**  
Client-aborted requests can surface as 502 "upstream unavailable".

**Affected files**
- `backend/F1App.Api/Program.cs` (line 77)

**Acceptance criteria**
- [ ] Cancellation when `HttpContext.RequestAborted` is signaled excluded from upstream failure mapping.

---

### F1-CR-033 — `PeriodicTimer` not disposed in poll loops

| Field | Value |
|-------|-------|
| **Type** | Technical Debt |
| **Severity** | Low |
| **Component** | Backend |

**Description**  
`PeriodicTimer` created per loop without `using`/`Dispose`. Minor resource leak on loop restart.

**Affected files**
- `backend/F1App.Api/Services/RaceDataOrchestrator.cs` (lines 154, 172, 192, 209, 228)

**Acceptance criteria**
- [ ] Timers wrapped in `using var timer = new PeriodicTimer(...)`.

---

### F1-CR-034 — Unused `FallbackTrigger` enum

| Field | Value |
|-------|-------|
| **Type** | Technical Debt |
| **Severity** | Low |
| **Component** | Backend |

**Description**  
`FallbackTrigger` is defined but never referenced. Suggests incomplete fallback logic.

**Affected files**
- `backend/F1App.Api/Models/FallbackTrigger.cs`

**Acceptance criteria**
- [ ] Enum implemented or removed.

---

### F1-CR-035 — Gitignored dev config without committed template

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Low |
| **Component** | Backend |

**Description**  
`appsettings.Development.json` is gitignored; no committed example. Fresh clone fails or runs with empty CORS until file is created manually.

**Affected files**
- `.gitignore`
- `backend/F1App.Api/Program.cs` (lines 66–67)

**Acceptance criteria**
- [ ] `appsettings.Development.example.json` committed with required keys documented.

---

### F1-CR-036 — No `HttpClient` max response size limit

| Field | Value |
|-------|-------|
| **Type** | Security |
| **Severity** | Low |
| **Component** | Backend |

**Description**  
No response size cap. Compromised upstream could stream very large JSON.

**Affected files**
- `backend/F1App.Api/Program.cs`
- `backend/F1App.Api/Clients/ErgastClient.cs`
- `backend/F1App.Api/Clients/OpenF1Client.cs`

**Acceptance criteria**
- [ ] `MaxResponseContentBufferSize` or streaming with limits configured.

---

### F1-CR-037 — Bleeding-edge target framework `net10.0`

| Field | Value |
|-------|-------|
| **Type** | Technical Debt |
| **Severity** | Low |
| **Component** | Backend |

**Description**  
Single NuGet dependency; small attack surface but framework maturity/support is a concern.

**Affected files**
- `backend/F1App.Api/F1App.Api.csproj`

**Acceptance criteria**
- [ ] SDK pinned in `global.json`; security advisories monitored.

---

### F1-CR-038 — Test coverage gaps on backend critical paths

| Field | Value |
|-------|-------|
| **Type** | Technical Debt |
| **Severity** | Low |
| **Component** | Backend |

**Description**  
No tests for `OpenF1Client`, `WinProbabilityController` HTTP layer, orchestrator session transitions, or global exception classification.

**Affected files**
- `backend/F1App.Api.Tests/`

**Acceptance criteria**
- [ ] Tests added for listed gaps (prioritize orchestrator state and exception mapping).

---

## Low Priority — Frontend

---

### F1-CR-039 — SignalR connection errors swallowed without logging

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Low |
| **Component** | Frontend |

**Description**  
`.catch(() => setConnectionStatus('disconnected'))` discards error — no logging or user-visible reason.

**Affected files**
- `frontend/src/features/live-race/hooks/useSignalRConnection.ts` (lines 34–37)

**Acceptance criteria**
- [ ] Error logged in development; optional "connection failed" UI with retry.

---

### F1-CR-040 — `ErrorBoundary` discards route error details

| Field | Value |
|-------|-------|
| **Type** | Technical Debt |
| **Severity** | Low |
| **Component** | Frontend |

**Description**  
`useRouteError()` return value ignored. No logging or differentiated UI.

**Affected files**
- `frontend/src/shared/components/ErrorBoundary.tsx` (lines 3–9)

**Acceptance criteria**
- [ ] Error logged in dev; safe generic message in prod.

---

### F1-CR-041 — `AbortSignal.any` / `AbortSignal.timeout` browser compatibility

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Low |
| **Component** | Frontend |

**Description**  
Relatively new APIs; older browsers may throw at fetch time.

**Affected files**
- `frontend/src/shared/api/ergast.ts` (line 107)

**Acceptance criteria**
- [ ] Polyfill or `AbortController` + `setTimeout` fallback if older browsers are in scope.

---

### F1-CR-042 — Zustand selector uses in-place `.sort()`

| Field | Value |
|-------|-------|
| **Type** | Technical Debt |
| **Severity** | Low |
| **Component** | Frontend |

**Description**  
`.sort()` mutates array from `Object.values`. Safe now but fragile if refactored.

**Affected files**
- `frontend/src/features/live-race/GapList/GapList.tsx` (lines 10–12)

**Acceptance criteria**
- [ ] Use `[...Object.values(s.drivers)].sort(...)`.

---

### F1-CR-043 — Playwright in devDependencies with no e2e script

| Field | Value |
|-------|-------|
| **Type** | Technical Debt |
| **Severity** | Low |
| **Component** | Frontend |

**Description**  
Playwright installed but no `test:e2e` script or tests under `frontend/`.

**Affected files**
- `frontend/package.json` (line 41)

**Acceptance criteria**
- [ ] Remove Playwright or add e2e tests and npm script.

---

### F1-CR-044 — `TimezoneToggle` a11y: `aria-disabled` without native `disabled`

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Low |
| **Component** | Frontend |
| **Labels** | accessibility |

**Description**  
Active segment uses `aria-disabled` and `tabIndex={-1}` but not native `disabled` attribute.

**Affected files**
- `frontend/src/features/calendar/TimezoneToggle.tsx` (lines 11–16, 26–31)

**Acceptance criteria**
- [ ] Proper toggle/radio group pattern or native `disabled` on active segment.

---

### F1-CR-045 — Duplicate `driverNumber` in snapshot silently overwrites

| Field | Value |
|-------|-------|
| **Type** | Defect |
| **Severity** | Low |
| **Component** | Frontend |

**Description**  
`normalizeSnapshot` last-wins on duplicate keys; bad snapshot could hide drivers without warning.

**Affected files**
- `frontend/src/shared/utils/normalizeSnapshot.ts` (lines 3–5)

**Acceptance criteria**
- [ ] Dev-mode warning or assert on duplicates; server validates uniqueness.

---

### F1-CR-046 — `.env.example` documents only `http://` URLs

| Field | Value |
|-------|-------|
| **Type** | Technical Debt |
| **Severity** | Low |
| **Component** | Frontend |

**Description**  
Fine for local dev; production should use `https://` / `wss://`. `.env.local` is gitignored (good).

**Affected files**
- `frontend/.env.example`

**Acceptance criteria**
- [ ] Deployment docs state production TLS requirements for API and SignalR URLs.

---

## Suggested Sprint Ordering

### Sprint 1 — Blockers & security baseline
F1-CR-001, F1-CR-005, F1-CR-035, F1-CR-003, F1-CR-010, F1-CR-006, F1-CR-008

### Sprint 2 — Live race reliability
F1-CR-004, F1-CR-013, F1-CR-021, F1-CR-012, F1-CR-022, F1-CR-023, F1-CR-025, F1-CR-026

### Sprint 3 — API correctness & tests
F1-CR-011, F1-CR-014, F1-CR-015, F1-CR-024, F1-CR-017, F1-CR-038

### Sprint 4 — Production hardening & hygiene
F1-CR-002, F1-CR-009, F1-CR-019, F1-CR-020, F1-CR-029, F1-CR-027, F1-CR-028, remaining Low items

---

## Positive Findings (no ticket required)

| Area | Status |
|------|--------|
| SQL / command injection | N/A — no database |
| SSRF via request params | Not found — fixed upstream URLs |
| XSS sinks | None — no `dangerouslySetInnerHTML` / `innerHTML` / `eval` |
| Secrets in source | None in committed code |
| REST validation | Zod on all `fetchJson` responses |
| Error leakage | ProblemDetails without stack traces |
| SignalR client invoke surface | Hub has no client-callable methods |
| Env secrets | `.env.local` gitignored |
