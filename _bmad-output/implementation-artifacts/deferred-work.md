## Deferred from: code review of 2-1-live-gap-list (2026-06-17)

- **[D1] Session transition: stale driver data persists in ConcurrentDictionaries** [`RaceDataOrchestrator.cs:20-21`] — `_latestPositions`/`_latestIntervals` never cleared when `session_key=latest` flips to a new session; retired drivers and stale positions accumulate. Story 2.5 scope.
- **[D2] PeriodicTimer recreated without dispose on exception restart, no backoff** [`RaceDataOrchestrator.cs:73,90`] — transient exceptions cause tight restart loop and OS timer handle leak. Acceptable for POC; apply disposal + backoff before production.
- **[D3] `_lastPositionPoll` DateTimeOffset not volatile; technically torn-read possible on 32-bit runtimes** [`RaceDataOrchestrator.cs:24`] — .NET 64-bit makes this safe in practice. Add `Interlocked` or `volatile` if ever targeting 32-bit.
- **[D4] `InvalidOperationException` conflated with upstream HTTP errors in global exception handler** [`Program.cs:77`] — pre-existing pattern; own-code `InvalidOperationException` logs at Warning and returns 502, hiding real bugs.
- **[D5] `InitialiseDriverInfoAsync` no retry; transient failure leaves driver names as numbers for process lifetime** [`RaceDataOrchestrator.cs:38`] — acceptable for POC; add exponential-backoff retry or periodic re-check before production.
- **[D6] `BuildSnapshot` observes partial ConcurrentDictionary state during publish tick (TOCTOU)** [`RaceDataOrchestrator.cs:124`] — position updated mid-snapshot can pair with stale interval timestamp, causing false staleness. Acceptable for 1s snapshot POC.
- **[D7] `parseFloat` on non-numeric gap strings (`"LAP"`, `"+1 LAP"`) — renders verbatim** [`DriverRow.tsx:15`] — `NaN < 1.0` keeps battle highlight safe, string falls through to plain span. Need display handling once non-numeric OpenF1 values are confirmed in real race data.
- **[D8] Race leader may show `~–` if OpenF1 omits them from intervals feed entirely** [`RaceDataOrchestrator.cs:129`] — leader should show `—` (no gap). Depends on real API behaviour; verify once live.
- **[D9] `setDrivers` replaces entire drivers map; any partial snapshot causes driver to vanish for one render cycle** [`liveRaceStore.ts:20`] — backend only publishes when `Drivers.Count > 0`, mitigating risk. Consider merge-by-key in future.
- **[D10] `OpenF1BaseUrl`/`ErgastBaseUrl` null-bang crashes process with no useful error on missing config** [`Program.cs:39,46`] — pre-existing pattern shared with `ErgastBaseUrl`. Replace with `GetRequiredSection`/explicit null-check before production.
- **[D11] `appsettings.json` has no fallback values for `OpenF1BaseUrl`/`JoinToleranceMs`; new contributor clone fails immediately** — setup/docs issue. Add commented-out example entries or a setup README section.
- **[D12] Test coverage gaps: `"LAP"` gap strings, session transitions, `PublishSnapshotLoopAsync` gate, `ExecuteAsync` integration** [`RaceDataOrchestratorTests.cs`] — all 8 tests exercise `BuildSnapshot` directly via pre-seeded state. Add integration-level tests when Story 2.5 adds the fallback state machine.

## Deferred from: code review of 1-7-pre-race-win-probability-widget (2026-06-17)

- **Cache stampede: `TryGetValue`/`Set` not atomic under concurrent requests** [`WinProbabilityService.cs:14-57`] — pre-existing pattern across all services; under concurrent load multiple callers can all miss the cache and fire duplicate Ergast requests. Fix with `GetOrCreateAsync` or a `SemaphoreSlim` when moving beyond single-instance POC.
- **Controller and service inject concrete types, no `IWinProbabilityService` / `IStandingsService` interfaces** [`WinProbabilityController.cs:10`, `WinProbabilityService.cs:9`] — pre-existing POC pattern consistent with `StandingsService` and `RaceScheduleService`; prevents controller-level unit testing but fine for this project's test strategy.
- **`WinProbability(int round)` cache key has no season component — collides across years** [`CacheKeys.cs:11`] — pre-existing pattern (existing `CurrentDriverStandings` key also lacks season scoping); in-memory cache is cleared on process restart so only relevant for long-lived servers spanning a season boundary.
- **Standings 1h TTL vs win probability 6h TTL creates stale champion-multiplier for up to 6h post-race** [`WinProbabilityService.cs:10`] — by design for POC; the combination (qualifying grid × champion weights) is snapshotted at compute time and is correct for the race weekend it covers. Full fix requires cache invalidation on standings refresh.

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
