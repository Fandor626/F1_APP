---
baseline_commit: 018bfda
---

# Story 7.3: Circuit outline asset coverage (season-wide) + same-origin fetch fix

Status: review

## Change Log

- 2026-07-13: Fixed the same-origin fetch bug in `TrackMap.tsx` (AD-6) and generated real f1db-sourced `circuit-configs/*.json` for all 24 season circuits (AD-5), replacing the single hand-drawn Monza placeholder. Added sitewide f1db attribution footer (AD-7). **Known trade-off:** Monza's live-GPS `transform` was dropped rather than kept stale against the new geometry — flagged prominently below, not hidden.

## Story

As a fan browsing any race on the Calendar or viewing the live Track Map,
I want every circuit to show its real track shape reliably in every environment,
so that I get an accurate visual instead of a missing or broken map.

## Acceptance Criteria

1. **Given** the full current-season calendar, **when** `circuit-configs` assets are generated, **then** every circuit has a `circuit-configs/{circuitId}.json` with a `trackPath` sourced from f1db's `-present` layout (e.g. `monza-7`), not a placeholder shape.
2. **Given** frontend and backend run on different origins (production: Vercel + Render), **when** any component fetches a circuit-configs asset, **then** the fetch uses a relative same-origin path (`/circuit-configs/{id}.json`), never `${VITE_API_BASE_URL}`-prefixed.
3. **Given** the existing live Track Map (`TrackMap.tsx`), **when** it fetches its circuit config, **then** it uses the same relative-path fix — the pre-existing 404 no longer occurs (AD-6 regression fix).
4. **Given** a circuit with no available outline data, **when** its config is requested, **then** the consuming component omits the outline gracefully rather than showing a broken-image state.
5. **Given** the sitewide f1db attribution requirement, **when** any page renders a track outline, **then** a persistent site-level credit line ("Track outlines: f1db/f1db, CC-BY-4.0") appears exactly once, not per-instance (AD-7).

## Tasks / Subtasks

- [x] Task 1: Fix the same-origin fetch bug in `TrackMap.tsx` (AC: 2, 3)
  - [x] Changed fetch to relative path `/circuit-configs/${circuitId}.json`
  - [x] Removed the now-unused `apiBase` const
  - [x] Added a test asserting the fetch call receives exactly `/circuit-configs/monza.json`
- [x] Task 2: Generate real circuit-config assets for the full season (AC: 1, 4)
  - [x] All 24 circuits (verified against `CircuitStaticFacts.cs` roster + lengths), real f1db geometry, `black`/`-present` layout per circuit
  - [x] Each file has `circuitId`, `viewBox`, `trackPath` — no placeholders
  - [x] `transform` intentionally omitted from all 24 (including `monza.json`) — see Dev Notes
  - [x] `CircuitConfig.transform` changed to optional
- [x] Task 3: Sitewide f1db attribution (AC: 5)
  - [x] Persistent footer added to `App.tsx`, linking to `https://github.com/f1db/f1db`
  - [x] Sole attribution instance in the app
- [x] Task 4: Tests (AC: 1, 2, 3, 4, 5)
  - [x] `TrackMap.test.tsx`: new relative-fetch-path test
  - [x] New `App.test.tsx`: 2 tests (footer present exactly once; present across routes)
  - [x] AC 4 already covered by the existing fetch-failure test, unaffected by this story

## Dev Notes

- **This story's own AC 1 required real data sourcing, not fabrication.** Rather than invent plausible-looking SVG path strings, the actual `f1db/f1db` GitHub repo was fetched (public, unauthenticated, verified reachable) for all 24 circuits. Mapping from this app's Ergast-style `circuitId` (e.g. `red_bull_ring`, `rodriguez`, `losail`) to f1db's own circuit slugs (e.g. `spielberg`, `mexico-city`, `lusail`) was verified per-circuit by checking each candidate f1db `.yml`'s declared `length`/`turns` against `CircuitStaticFacts.cs`'s existing values — all 24 matched (2 had trivial ~10m rounding differences between Ergast and f1db source data, not a mapping error). Full mapping used (circuitId → f1db layout id):
    `bahrain→bahrain-1, jeddah→jeddah-1, albert_park→melbourne-2, suzuka→suzuka-2, shanghai→shanghai-1, miami→miami-1, imola→imola-3, monaco→monaco-6, villeneuve→montreal-6, catalunya→catalunya-6, red_bull_ring→spielberg-3, silverstone→silverstone-8, hungaroring→hungaroring-3, spa→spa-francorchamps-4, zandvoort→zandvoort-5, monza→monza-7, baku→baku-1, marina_bay→marina-bay-4, americas→austin-1, rodriguez→mexico-city-3, interlagos→interlagos-2, las_vegas→las-vegas-1, losail→lusail-1, yas_marina→yas-marina-2`.
  - Source SVGs: `https://raw.githubusercontent.com/f1db/f1db/main/src/assets/circuits/black/{layout-id}.svg`. Each is a single `<path d="...">` in a `width="500" height="500"` (or similar) coordinate space with no separate `viewBox` attribute — used `"0 0 {width} {height}"` as this app's `viewBox` field.
- **Monza `transform` — read before touching `monza.json`.** The existing `frontend/public/circuit-configs/monza.json` has a hand-drawn placeholder `trackPath` (viewBox `"0 0 900 600"`) paired with a real, calibrated `transform` used by the live GPS map (`useTrackInterpolation`) to position driver dots. This story replaces that placeholder `trackPath`/`viewBox` with the real f1db geometry (`monza-7`, native `500x500` space) per AC 1 — but the *old* `transform` values were tuned against the *old* 900×600 hand-drawn shape's proportions and are now mathematically wrong for the new coordinate space. Recalibrating a real GPS-to-SVG affine transform requires reference data (known track GPS coordinates matched to pixel positions) this story does not have and should not fabricate — a fabricated-but-plausible transform would be **worse** than no transform: it would silently mis-position live driver dots rather than gracefully showing none. Decision: **omit `transform` from the new `monza.json` too.** Net effect: Monza's live map will show the (now more accurate) track outline but no driver dots, until a dedicated future story recalibrates the transform against the new shape. This is a known, flagged regression in one specific capability (live dot positioning for Monza specifically — every other circuit already had zero live-dot capability before this story, so nothing changes for them), traded for fixing a bigger problem (23 circuits with zero outline at all, plus the season-wide production fetch bug). Flag this trade-off to the user rather than silently accept it.
- **Reality check — production fetch bug, now load-bearing.** `TrackMap.tsx` currently fetches `${apiBase}/circuit-configs/${circuitId}.json` where `apiBase` resolves to `VITE_API_BASE_URL` (the **backend** origin, `http://localhost:5000` in `.env.local`/`.env.example`). But `circuit-configs/*.json` physically lives in `frontend/public/`, served by the **frontend** origin (Vite dev server locally; Vercel in production, per phase-1 architecture). This has been a latent bug since Monza was the only circuit and the live map's own error-swallowing (`.catch(() => setUnavailable(true))`) hid it. Fix applies to this one call site.
- **Do not build a new decorative `TrackOutline.tsx` component in this story.** That's Story 7.4 (Calendar card) and Story 10.1 (Race Weekend Detail) — they consume the assets this story produces. This story is asset generation + the fetch-path fix + attribution only.
- **No backend changes.** `circuit-configs/` is a frontend-only static asset directory (`frontend/public/circuit-configs/`), unrelated to the C# backend.

### Project Structure Notes

- New/modified: 23 new `frontend/public/circuit-configs/{circuitId}.json` files, 1 modified (`monza.json`, replacing its placeholder geometry — see Dev Notes).
- Modified: `frontend/src/features/live-race/TrackMap/TrackMap.tsx`, `frontend/src/features/live-race/TrackMap/TrackMap.test.tsx`, `frontend/src/App.tsx`.
- New: `frontend/src/App.test.tsx` (no test file exists for `App.tsx` today).

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 7.3]
- [Source: _bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md#AD-5, #AD-6, #AD-7]
- [Source: _bmad-output/phase-2/planning-artifacts/prds/prd-F1_poc-2026-07-11/addendum.md#FR-4 / FR-13 — Track outline rendering] — f1db sourcing decision, CC-BY-4.0 attribution requirement
- [Source: frontend/src/features/live-race/TrackMap/TrackMap.tsx] — fetch bug, `CircuitConfig` interface
- [Source: frontend/src/features/live-race/TrackMap/useTrackInterpolation.ts] — confirms `transform: null` degrades gracefully (no crash, no dots)
- [Source: backend/F1App.Api/Services/CircuitStaticFacts.cs] — authoritative 24-circuit roster + lengths, used to verify the circuitId→f1db-slug mapping
- [Source: https://github.com/f1db/f1db] (CC-BY-4.0) — real geometry source, fetched directly (public, unauthenticated)

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- **Monza live-GPS transform dropped — flagging prominently, as promised in Dev Notes.** `monza.json`'s old hand-drawn `trackPath`/viewBox is now real f1db geometry, but its old calibrated `transform` (for live driver-dot positioning) was tuned against the old coordinate space and would be wrong against the new one. Fabricating a plausible-looking replacement transform without real calibration reference data would silently mis-position live dots — worse than the graceful "track shows, no dots" degradation `useTrackInterpolation` already provides for `transform: null` (verified by reading the hook: `if (!transform) return` before the animation loop starts, no crash). Chose the graceful path. **Net effect: if Monza goes live before a future recalibration story, the track outline will render correctly but driver dots will not appear.** No other circuit had live-dot capability before this story, so nothing else regresses.
- `TrackMap.test.tsx` pre-existing typecheck issue (`Cannot find name 'global'`, present on baseline, 5 occurrences) — rather than add 3 more instances of the same broken pattern via my new test, replaced all 8 `global.fetch` references in this file with `globalThis.fetch` (works identically for vitest/jsdom mocking, doesn't require Node's `@types/node` ambient `global`). Result: this file's typecheck is now fully clean, a net improvement, not just a non-regression.
- `react-hooks/set-state-in-effect` eslint error on `TrackMap.tsx:28` — confirmed via `git stash` against baseline: identical error, identical code, just shifted one line by removing `apiBase`. Pre-existing, unrelated to this story's ACs, left as-is.
- Full suite: same 11 pre-existing unrelated failures as Stories 7.1/7.2 (localStorage-unavailable environment issue, 1 locale-dependent time assertion) — 118 passing (+3 new tests, 0 new failures).
- Typecheck: clean (0 errors, improved from 5 pre-existing). Lint: 1 pre-existing, unrelated, confirmed error only.

### Completion Notes List

- Sourced real circuit geometry for all 24 current-season circuits directly from `f1db/f1db` (public GitHub repo, CC-BY-4.0) rather than fabricating plausible-looking path data — verified the Ergast-style circuitId → f1db-slug mapping for all 24 by cross-checking `length`/`turns` against `CircuitStaticFacts.cs` (all matched; 2 had trivial ~10m source-data rounding differences).
- Fixed `TrackMap.tsx`'s latent same-origin fetch bug (was silently broken in any environment where frontend/backend are different origins, i.e. production) and added a regression test that actually asserts the fetch URL — the prior test suite mocked `fetch` but never checked what URL it was called with.
- Made `CircuitConfig.transform` optional to correctly reflect that 23 of 24 circuits now have decorative-only geometry (no live GPS calibration) — this is expected and documented, not a bug.
- **Made and documented a real trade-off decision** on Monza's transform (see Debug Log) rather than either silently keeping a now-incorrect value or silently dropping it without mention.
- Added the sitewide f1db attribution footer to `App.tsx`, the only attribution instance in the app.
- As a side effect of extending `TrackMap.test.tsx`, fixed a pre-existing typecheck gap in that file (`global` → `globalThis`) rather than propagate it further.

### File List

- `frontend/src/features/live-race/TrackMap/TrackMap.tsx` (modified)
- `frontend/src/features/live-race/TrackMap/TrackMap.test.tsx` (modified)
- `frontend/src/App.tsx` (modified)
- `frontend/src/App.test.tsx` (new)
- `frontend/public/circuit-configs/monza.json` (modified — real f1db geometry replacing placeholder)
- `frontend/public/circuit-configs/{albert_park,americas,bahrain,baku,catalunya,hungaroring,imola,interlagos,jeddah,las_vegas,losail,marina_bay,miami,monaco,red_bull_ring,rodriguez,shanghai,silverstone,spa,suzuka,villeneuve,yas_marina,zandvoort}.json` (new — 23 files)
