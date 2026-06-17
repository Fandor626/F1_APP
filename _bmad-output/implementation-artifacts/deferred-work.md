## Deferred from: code review of 1-6-timezone-toggle (2026-06-17)

- **ARIA semantics: `aria-pressed` siblings vs. `role="radiogroup"`** [TimezoneToggle.tsx] — two `aria-pressed` buttons don't communicate mutual exclusivity to assistive technology; `role="radiogroup"` + `role="radio"` children would be more correct. Best-practice improvement, not a correctness bug.
- **DST edge case for track offset** [dateUtils.ts:34-46] — static UTC offset in the ISO string may be wrong if the backend uses the standard (non-DST) offset for a session that falls in a DST period. Upstream data quality concern; no fix possible without IANA timezone support.
- **Null/invalid-date guard in `formatSessionTimeForMode`** [dateUtils.ts:41-46] — malformed ISO string produces `Invalid Date` silently. Pre-existing concern; Zod schema at the API boundary guards against this in practice.

## Deferred from: code review of story-1-5 (2026-06-17)

- **`PointsGap` can be zero when top-two standings are tied on points** [RaceScheduleService.cs, GetChampionshipDeltaAsync] — Ergast data quality edge case; zero gap is technically correct but odd-looking. Consider `.ToString("F0")` formatting or a guard if it becomes a UX issue.
- **`ErgastResultDto` lacks a position field** [ErgastRaceResultResponseDto.cs] — winner assumed as `results[0]` without verifying `position == "1"`. The `/results/1.json` filter is verified live but a future Ergast API change could silently return the wrong driver.
- **`GetChampionshipDeltaAsync` has no inline cache guard** [RaceScheduleService.cs] — standings re-queried on every detail call; harmless because `StandingsService` caches internally, but the inconsistency with `GetPriorYearWinnerAsync`'s explicit guard is worth aligning eventually.
- **`int.Parse(race.Season)` throws on non-numeric Ergast season string** [RaceScheduleService.cs] — pre-existing pattern; if Ergast ever returns `"current"` as a season token it produces 500 instead of a clean error.
- **Cache stampede on concurrent cold-cache detail requests** [RaceScheduleService.cs, GetPriorYearWinnerAsync] — check-then-act window allows two concurrent requests to both miss and both call Ergast. Not an issue for a single-instance hobby POC but worth a `SemaphoreSlim` or `Lazy<Task>` if this ever goes multi-instance.
- **Missing test: `priorYearWinner` absent with `championshipDelta` present** [ContextualData.test.tsx] — the individual branches are covered; this combination is exercised via MSW mock for round 2 but has no dedicated `ContextualData` unit test case.

## Deferred from: code review of story-1-1 (2026-06-16)

- **Frontend health-check fetch has no timeout/`AbortController`** [frontend/src/shared/api/health.ts] — a hung connection leaves the UI on "Checking backend…" indefinitely. Not fixed now because this exact code is explicitly throwaway scaffolding (replaced by the real Calendar page in Story 1.2). Apply a timeout/abort pattern to the real TanStack Query calls introduced from Story 1.2 onward.
- **Production-environment `AllowedOrigins` not yet decided** [backend/F1App.Api/appsettings.json] — the committed (non-Development) config has no `AllowedOrigins`, so any non-Development environment currently resolves to an empty CORS allow-list. Pre-existing architecture gap: no non-POC CORS origin has been decided yet. Revisit when deployment (Vercel/Render) origins are finalized.
