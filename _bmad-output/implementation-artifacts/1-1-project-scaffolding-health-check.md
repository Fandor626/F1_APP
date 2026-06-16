---
baseline_commit: NO_VCS
---

# Story 1.1: Project Scaffolding & Health Check

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want the frontend and backend projects scaffolded and able to talk to each other,
so that all later feature work has a working foundation to build on.

## Acceptance Criteria

1. **Frontend scaffolded.** A fresh checkout, after `npm create vite@latest frontend -- --template react-ts` and `npm install`, starts a dev server at `localhost:5173` rendering the default Vite/React page.
2. **Backend scaffolded.** `dotnet new webapi -n F1App.Api --framework net10.0` (plus `F1App.Api.Tests`) produces a project matching architecture's tree (`Controllers/`, `Services/`, `Clients/`, `Models/`, `Hubs/`, `Dtos/`), runs at `localhost:5000`.
3. **Round trip proven.** With both servers running, the frontend calls a `/api/health` endpoint via TanStack Query and renders a successful response — proving CORS (`AllowedOrigins: http://localhost:5173`) works end to end, with no console CORS errors.
4. **JSON casing configured globally.** `JsonNamingPolicy.CamelCase` is set once in `Program.cs`, not per-controller — verified by the health response being camelCase.

## Tasks / Subtasks

- [x] Task 1: Scaffold frontend (AC: 1)
  - [x] Verify Node.js ≥20.19 or ≥22.12 (Vite 's current minimum) before running create-vite
  - [x] `npm create vite@latest frontend -- --template react-ts` at repo root, then `npm install`
  - [x] Confirm `npm run dev` serves at `localhost:5173`
  - [x] Install the frontend library stack from architecture (exact set, no substitutions): `react-router-dom@7`, `@tanstack/react-query@5`, `zustand`, `@microsoft/signalr`, `recharts`, `tailwindcss@4` + `@tailwindcss/vite` plugin, `html-to-image`, `zod`, `vitest`, `@testing-library/react`, `msw`, `playwright`
  - [x] Register the Tailwind Vite plugin in `vite.config.ts` and add `@import "tailwindcss";` to the main CSS entry file — both are required for v4's plugin-based setup, the package alone does nothing
  - [x] Create the feature-based folder skeleton: `src/features/{calendar,live-race,standings,profiles,fan-engagement}/`, `src/shared/{components,hooks,utils,api}/` (empty dirs are fine — later stories populate them)
  - [x] Create `.env.local` (gitignored) and `.env.example` with `VITE_API_BASE_URL=http://localhost:5000` and `VITE_SIGNALR_HUB_URL=http://localhost:5000/hubs/race`
- [x] Task 2: Scaffold backend (AC: 2)
  - [x] `dotnet new sln -n F1_poc` at repo root
  - [x] `dotnet new webapi -n F1App.Api --framework net10.0` under `backend/`
  - [x] `dotnet new xunit -n F1App.Api.Tests --framework net10.0` under `backend/`
  - [x] `dotnet sln add backend/F1App.Api/F1App.Api.csproj backend/F1App.Api.Tests/F1App.Api.Tests.csproj`
  - [x] Create folders: `Controllers/`, `Services/`, `Clients/`, `Models/`, `Hubs/`, `Dtos/Ergast/`, `Dtos/OpenF1/`
  - [x] Leave the template's built-in OpenAPI support as-is (`Microsoft.AspNetCore.OpenApi`, doc at `/openapi/v1.json`) — do **not** add Swashbuckle/Swagger, .NET 10's webapi template replaced it
  - [x] Create `appsettings.Development.json` (gitignored) with `ErgastBaseUrl`, `OpenF1BaseUrl`, `AllowedOrigins: ["http://localhost:5173"]`, `JoinToleranceMs: 500` (values unused until later stories, but the config shape is decided now per architecture)
- [x] Task 3: Configure `Program.cs` (AC: 3, 4)
  - [x] Set `JsonNamingPolicy.CamelCase` globally on the JSON options (not per-controller)
  - [x] Set `JsonIgnoreCondition.WhenWritingNull` globally
  - [x] Add a CORS policy restricted to `http://localhost:5173`, applied via `app.UseCors(...)` before endpoint mapping
  - [x] Do **not** add SignalR hub mapping here — `RaceHub` belongs to Epic 2, out of scope for this story
- [x] Task 4: Health check round trip (AC: 3, 4)
  - [x] Add a thin `HealthController` (or minimal API route) returning `{ status: "ok" }` at `/api/health` — no business logic, per architecture's "Controllers are routing only" rule
  - [x] On the frontend, add a TanStack Query call to `GET /api/health` and render the result somewhere visible (e.g. `App.tsx`) to prove the round trip — this is throwaway scaffolding the moment Story 1.2 starts the real Calendar page, not a permanent component
  - [x] Manually verify in browser: response renders, no CORS error in console, JSON keys are camelCase
- [x] Task 5: Repo hygiene (AC: 1, 2)
  - [x] `git init` at repo root — repo was already initialized (with a commit and a GitHub remote) outside this workflow partway through the session; `git init` safely no-op'd (reinit preserves history, confirmed via `git fsck`)
  - [x] Root `.gitignore` covering `node_modules/`, `bin/`, `obj/`, `.env.local`, `appsettings.Development.json`
  - [x] Confirm final tree matches architecture's Complete Project Tree (see Project Structure Notes for one resolved discrepancy)

### Review Findings

- [x] [Review][Patch] Backend doesn't actually run on `localhost:5000` as AC2 requires [backend/F1App.Api/Properties/launchSettings.json, backend/F1App.Api/F1App.Api.http] — fixed: both launch profiles and the `.http` scratch file now point at port 5000; re-verified with a plain `dotnet run` (no manual override)
- [x] [Review][Patch] `app.UseHttpsRedirection()` runs before `app.UseCors(...)` — redirect responses lack CORS headers [backend/F1App.Api/Program.cs] — fixed: `UseCors` now runs before `UseHttpsRedirection`
- [x] [Review][Patch] Undefined `VITE_API_BASE_URL` silently builds a bogus relative-URL fetch instead of failing clearly [frontend/src/shared/api/health.ts] — fixed: throws a clear error instead
- [x] [Review][Patch] Brittle substring-based JSON assertion in `HealthControllerTests` [backend/F1App.Api.Tests/Controllers/HealthControllerTests.cs] — fixed: replaced with a case-sensitive `JsonDocument` property check (kept the casing verification, removed the brittleness — deserializing into a record alone wouldn't catch a casing regression, since property matching is case-insensitive by default)
- [x] [Review][Patch] Duplicate shadow DTO (`HealthResponseDto`) instead of referencing the real `HealthResponse` [backend/F1App.Api.Tests/Controllers/HealthControllerTests.cs] — fixed: test now references `F1App.Api.Controllers.HealthResponse` directly
- [x] [Review][Patch] `HealthController.Get()` returns `IActionResult` instead of `ActionResult<HealthResponse>`, losing OpenAPI schema fidelity [backend/F1App.Api/Controllers/HealthController.cs] — fixed
- [x] [Review][Patch] Missing test coverage for the non-2xx HTTP failure path in `fetchHealth` (only network-level failure is tested) [frontend/src/App.test.tsx] — fixed: added a 500-response test case
- [x] [Review][Patch] No startup warning when `AllowedOrigins` resolves to an empty array [backend/F1App.Api/Program.cs] — fixed: logs a warning at startup
- [x] [Review][Defer] Frontend health-check fetch has no timeout/`AbortController` [frontend/src/shared/api/health.ts] — deferred, code is explicitly throwaway (replaced in Story 1.2); apply the pattern to real TanStack Query calls going forward instead
- [x] [Review][Defer] Production-environment `AllowedOrigins` not yet decided in `appsettings.json` [backend/F1App.Api/appsettings.json] — deferred, pre-existing architecture gap, no non-POC CORS origin has been decided yet

## Dev Notes

- **No SignalR, no Ergast/OpenF1 clients, no real endpoints yet.** This story is pure scaffold + one throwaway health check. Resist the urge to start Story 1.2's calendar work here — keep this story's diff small and focused on AC 1-4.
- **`DateTimeOffset` is banned... eventually.** The backend rule ("`DateTime` is banned, use `DateTimeOffset` throughout") doesn't bite yet since there's no date-handling code in a health check — but set the expectation now for every story after this one.
- **ProblemDetails (RFC 7807) middleware** is an architecture-wide "from day one" pattern, but the health check has no error path to exercise it on. Recommended (not required by these ACs): wire the global exception-handling middleware in this story's `Program.cs` anyway, so Story 1.2's first real endpoint inherits it for free instead of retrofitting. Use judgment — skipping it here is not a defect.
- **Tailwind v4 setup differs from v3.** Use the `@tailwindcss/vite` plugin, not a `tailwind.config.js` + PostCS init flow. Don't seed `DESIGN.md`'s color/spacing tokens into the Tailwind theme in *this* story — no UI is rendered yet — but leave the config easy to extend, since Story 1.2 will need it.
- **.NET 10 web API template ships OpenAPI by default** (`Microsoft.AspNetCore.OpenApi`, served at `/openapi/v1.json`). This is new relative to older .NET versions that needed Swashbuckle — don't add it, the template already has equivalent (better) coverage.
- **`react-router-dom@7` naming note.** Architecture pins `react-router-dom v7` and that package still installs and works — React Router kept it as a compatibility re-export. But the canonical v7 package is the unified `react-router` (no `-dom` suffix); new v7 projects are generally pointed there. This story follows architecture exactly as documented (installs `react-router-dom@7`) rather than silently substituting — if you want the canonical package instead, that's an architecture-level call to make explicitly, not a scaffolding-story improvisation.

### Project Structure Notes

- Matches architecture's Complete Project Tree: `frontend/`, `backend/F1App.Api/`, `backend/F1App.Api.Tests/`.
- **Resolved discrepancy:** architecture.md's earlier "Solution Structure" section (Starter Template Evaluation) lists a root `docker-compose.yml`, but the later "Infrastructure" section explicitly states "Local dev: Independent processes (no docker-compose for POC)" and the final "Complete Project Tree" doesn't include it either. Treat the later, more specific sections as authoritative — **do not create `docker-compose.yml`** for this story or this POC.
- No database, no migrations — `IMemoryCache` and `localStorage` only, per NFR-3. Nothing to scaffold there.

### References

- [Source: architecture.md#Starter Template Evaluation] — exact scaffold commands, frontend/backend library stacks
- [Source: architecture.md#Solution Structure] — initial tree (superseded in part, see discrepancy note above)
- [Source: architecture.md#Infrastructure] — local dev ports, CORS scope, no-docker-compose decision
- [Source: architecture.md#Project Structure & Boundaries] — authoritative Complete Project Tree
- [Source: architecture.md#Implementation Handoff] — "First Implementation Priority" scaffold command block
- [Source: architecture.md#Gap Analysis Results] — env var names (`VITE_API_BASE_URL`, `ErgastBaseUrl`, etc.)
- [Source: epics.md#Epic 1: Project Foundation & Race Calendar > Story 1.1] — story origin, AC source
- [Web: .NET 10 SDK web API template ships `Microsoft.AspNetCore.OpenApi` by default, OpenAPI doc at `/openapi/v1.json` — Microsoft Learn, "What's new in ASP.NET Core in .NET 10"]
- [Web: Vite 's `npm create vite@latest -- --template react-ts` requires Node.js 20.19+ or 22.12+ — Vite official guide]

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6

### Debug Log References

- `.NET SDK missing entirely from environment` — resolved via user-approved Microsoft `dotnet-install.sh` script, user-local install to `~/.dotnet` (no sudo), `.NET 10.0.301` installed and added to `~/.bashrc`.
- `CORS headers absent on first verification pass` — root cause: started backend with `--no-launch-profile`, which skipped `ASPNETCORE_ENVIRONMENT=Development`, so `appsettings.Development.json` (holding `AllowedOrigins`) never loaded. Fixed by setting `ASPNETCORE_ENVIRONMENT=Development` explicitly. Re-verified: CORS headers present and correct.
- `git init` reported "reinitialized existing repository" — a git repo with one commit and a GitHub remote (`origin/main`) appeared mid-session, set up by the user outside this workflow. Confirmed via `git fsck` (clean) and `git reflog` that no history was lost; `git init` was a safe no-op.

### Completion Notes List

- Frontend (Vite + React 19 + TS) and backend (ASP.NET Core 10) scaffolded per architecture, full library stacks installed exactly as specified, no substitutions.
- `/api/health` round trip verified for real: backend curl-tested (camelCase JSON, correct CORS headers with and without preflight), then verified end-to-end through an actual headless-browser load of the frontend (Playwright) showing "Backend status: ok" rendered with zero console errors.
- Tests: 2 backend xunit tests (`HealthControllerTests`, via `WebApplicationFactory`) + 2 frontend vitest tests (`App.test.tsx`, success + failure path via MSW) — all passing. `dotnet build`: 0 warnings/0 errors. `eslint`: clean.
- Resolved the `react-router-dom@7` vs `react-router` package-naming question by following architecture exactly as documented (see Dev Notes) rather than substituting unilaterally.
- Did not create `docker-compose.yml` — resolved architecture's internal inconsistency in favor of its later, more specific sections (see Project Structure Notes).
- Per user instruction, committed and pushed this story's work to `origin/main`.

### File List

**Added:**
- `.gitignore`
- `F1_poc.slnx`
- `backend/F1App.Api/Program.cs`
- `backend/F1App.Api/F1App.Api.csproj`
- `backend/F1App.Api/F1App.Api.http`
- `backend/F1App.Api/appsettings.json`
- `backend/F1App.Api/appsettings.Development.json` *(gitignored, not pushed)*
- `backend/F1App.Api/Properties/launchSettings.json`
- `backend/F1App.Api/Controllers/HealthController.cs`
- `backend/F1App.Api.Tests/F1App.Api.Tests.csproj`
- `backend/F1App.Api.Tests/Controllers/HealthControllerTests.cs`
- `frontend/.env.example`
- `frontend/.env.local` *(gitignored, not pushed)*
- `frontend/.gitignore`
- `frontend/README.md`, `frontend/eslint.config.js`, `frontend/index.html`, `frontend/package.json`, `frontend/package-lock.json`
- `frontend/tsconfig.json`, `frontend/tsconfig.app.json`, `frontend/tsconfig.node.json`
- `frontend/vite.config.ts`
- `frontend/public/favicon.svg`, `frontend/public/icons.svg`
- `frontend/src/main.tsx`, `frontend/src/App.tsx`, `frontend/src/App.css`, `frontend/src/index.css`
- `frontend/src/App.test.tsx`
- `frontend/src/assets/hero.png`, `frontend/src/assets/react.svg`, `frontend/src/assets/vite.svg`
- `frontend/src/shared/api/health.ts`, `frontend/src/shared/api/queryKeys.ts`
- `frontend/src/shared/mocks/handlers/healthHandlers.ts`
- `frontend/src/shared/test/setup.ts`, `frontend/src/shared/test/server.ts`
- `frontend/src/features/{calendar,live-race,standings,profiles,fan-engagement}/` (empty, scaffolded for later stories)
- `frontend/src/shared/{components,hooks,utils}/` (empty, scaffolded for later stories)

**Modified (review patches):**
- `backend/F1App.Api/Properties/launchSettings.json` — port 5032 → 5000
- `backend/F1App.Api/F1App.Api.http` — port + stale `/weatherforecast` → `/api/health`
- `backend/F1App.Api/Program.cs` — CORS/HTTPS-redirect ordering, empty-`AllowedOrigins` warning log
- `backend/F1App.Api/Controllers/HealthController.cs` — `ActionResult<HealthResponse>` return type
- `backend/F1App.Api.Tests/Controllers/HealthControllerTests.cs` — case-sensitive JSON assertion, removed duplicate DTO
- `frontend/src/shared/api/health.ts` — guard for missing `VITE_API_BASE_URL`
- `frontend/src/App.test.tsx` — added non-2xx failure-path test
- `_bmad-output/implementation-artifacts/deferred-work.md` (new)

**Modified:**
- `_bmad-output/implementation-artifacts/1-1-project-scaffolding-health-check.md` (this file)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (status tracking)

## Change Log

- 2026-06-16: Frontend and backend scaffolded, `/api/health` round trip implemented and verified (browser + curl + automated tests), repo hygiene completed (`.gitignore`, confirmed pre-existing git history intact). All 5 tasks complete, all ACs satisfied. Status → review.
- 2026-06-16: Code review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) found 0 decision-needed, 8 patch, 2 defer, 4 dismissed. All 8 patches applied and re-verified (including a real AC2 violation: backend was running on port 5032, not 5000). Status → done.
