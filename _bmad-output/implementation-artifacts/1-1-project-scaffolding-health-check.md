---
baseline_commit: NO_VCS
---

# Story 1.1: Project Scaffolding & Health Check

Status: in-progress

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

- [ ] Task 1: Scaffold frontend (AC: 1)
  - [ ] Verify Node.js ≥20.19 or ≥22.12 (Vite 's current minimum) before running create-vite
  - [ ] `npm create vite@latest frontend -- --template react-ts` at repo root, then `npm install`
  - [ ] Confirm `npm run dev` serves at `localhost:5173`
  - [ ] Install the frontend library stack from architecture (exact set, no substitutions): `react-router-dom@7`, `@tanstack/react-query@5`, `zustand`, `@microsoft/signalr`, `recharts`, `tailwindcss@4` + `@tailwindcss/vite` plugin, `html-to-image`, `zod`, `vitest`, `@testing-library/react`, `msw`, `playwright`
  - [ ] Register the Tailwind Vite plugin in `vite.config.ts` and add `@import "tailwindcss";` to the main CSS entry file — both are required for v4's plugin-based setup, the package alone does nothing
  - [ ] Create the feature-based folder skeleton: `src/features/{calendar,live-race,standings,profiles,fan-engagement}/`, `src/shared/{components,hooks,utils,api}/` (empty dirs are fine — later stories populate them)
  - [ ] Create `.env.local` (gitignored) and `.env.example` with `VITE_API_BASE_URL=http://localhost:5000` and `VITE_SIGNALR_HUB_URL=http://localhost:5000/hubs/race`
- [ ] Task 2: Scaffold backend (AC: 2)
  - [ ] `dotnet new sln -n F1_poc` at repo root
  - [ ] `dotnet new webapi -n F1App.Api --framework net10.0` under `backend/`
  - [ ] `dotnet new xunit -n F1App.Api.Tests --framework net10.0` under `backend/`
  - [ ] `dotnet sln add backend/F1App.Api/F1App.Api.csproj backend/F1App.Api.Tests/F1App.Api.Tests.csproj`
  - [ ] Create folders: `Controllers/`, `Services/`, `Clients/`, `Models/`, `Hubs/`, `Dtos/Ergast/`, `Dtos/OpenF1/`
  - [ ] Leave the template's built-in OpenAPI support as-is (`Microsoft.AspNetCore.OpenApi`, doc at `/openapi/v1.json`) — do **not** add Swashbuckle/Swagger, .NET 10's webapi template replaced it
  - [ ] Create `appsettings.Development.json` (gitignored) with `ErgastBaseUrl`, `OpenF1BaseUrl`, `AllowedOrigins: ["http://localhost:5173"]`, `JoinToleranceMs: 500` (values unused until later stories, but the config shape is decided now per architecture)
- [ ] Task 3: Configure `Program.cs` (AC: 3, 4)
  - [ ] Set `JsonNamingPolicy.CamelCase` globally on the JSON options (not per-controller)
  - [ ] Set `JsonIgnoreCondition.WhenWritingNull` globally
  - [ ] Add a CORS policy restricted to `http://localhost:5173`, applied via `app.UseCors(...)` before endpoint mapping
  - [ ] Do **not** add SignalR hub mapping here — `RaceHub` belongs to Epic 2, out of scope for this story
- [ ] Task 4: Health check round trip (AC: 3, 4)
  - [ ] Add a thin `HealthController` (or minimal API route) returning `{ status: "ok" }` at `/api/health` — no business logic, per architecture's "Controllers are routing only" rule
  - [ ] On the frontend, add a TanStack Query call to `GET /api/health` and render the result somewhere visible (e.g. `App.tsx`) to prove the round trip — this is throwaway scaffolding the moment Story 1.2 starts the real Calendar page, not a permanent component
  - [ ] Manually verify in browser: response renders, no CORS error in console, JSON keys are camelCase
- [ ] Task 5: Repo hygiene (AC: 1, 2)
  - [ ] `git init` at repo root — no git repository exists yet, this is a fresh checkout
  - [ ] Root `.gitignore` covering `node_modules/`, `bin/`, `obj/`, `.env.local`, `appsettings.Development.json`
  - [ ] Confirm final tree matches architecture's Complete Project Tree (see Project Structure Notes for one resolved discrepancy)

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

### Debug Log References

### Completion Notes List

### File List
