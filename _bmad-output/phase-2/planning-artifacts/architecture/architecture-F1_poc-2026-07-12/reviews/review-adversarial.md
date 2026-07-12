# Adversarial Review — ARCHITECTURE-SPINE.md (F1_poc Phase 2)

**Reviewed:** `_bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md`
**Method:** For each AD, constructed two hypothetical compliant implementers and asked whether they could produce clashing data shapes, dual ownership, or conflicting mutation paths — while spot-checking every "reuses existing X" claim against the actual as-built codebase (`frontend/src/features/live-race`, `frontend/src/features/fan-engagement`, `backend/F1App.Api/Services`, `backend/F1App.Api/Controllers`, `backend/F1App.Api/Models`, `backend/F1App.Api/Dtos`).

**Verdict:** Not clean. The spine's mermaid diagram and per-AD reasoning are structurally sound, but three of its "reuse the existing X" claims are factually wrong about what exists in the codebase today, and one of those (AD-9) is the exact data-loss failure mode the AD itself claims to prevent. There are also two real unresolved concurrency/ownership ambiguities (AD-1, AD-3/AD-4) and one FR that is marked "no new AD needed" on the basis of a pattern that does not exist in the codebase (FR-10).

---

## Finding 1 (Critical) — AD-9's migration source key does not exist; the AD will silently discard every existing user's Fan Card

**AD-9 rule (as written):** "New key `f1app__fanCard__v3` stores `Record<cardId, FanCardData>`. The existing `useLocalStorage` migration path seeds `v3` from the legacy `f1app__fanCard__v2` single-card value (if present) as its first entry on first load post-upgrade."

**What's actually in the codebase** (`frontend/src/features/fan-engagement/useFanCardStore.ts`):
```ts
export const useFanCardStore = create<FanCardState>()(
  persist(
    (set) => ({ ...EMPTY_PICKS, ... }),
    { name: 'f1app__fanCard__v1' },   // <-- v1, not v2
  ),
)
```
Two problems, both confirmed by reading the source, not inferred:

1. **The key is `v1`, not `v2`.** No user's browser has ever had a `f1app__fanCard__v2` key — the app has only ever written `v1`. AD-9's migration reads "seed v3 from v2 if present." Since v2 is never present, the "if present" branch is always false, and the migration silently no-ops. This is not a hypothetical edge case two devs might disagree on — it is a guaranteed outcome for 100% of existing users, and it is *exactly* the failure the AD's own "Prevents" line names: "silently discarding a user's existing MVP card on upgrade." The AD's rule, followed to the letter, produces the bug it exists to prevent.
   - Root cause is traceable: the phase-1 parent architecture doc (`_bmad-output/phase-1/planning-artifacts/architecture.md:372`) documents the *pattern* using `f1app__fanCard__v2` as an illustrative example key name. Phase-2's AD-9 appears to have copied that illustrative example as if it were the real, current key, without checking the as-built store.

2. **The current store is not built on the `useLocalStorage` hook at all.** It uses Zustand's `persist` middleware (`zustand/middleware`), which wraps state as `{state: {...}, version: N}` under the hood — a different serialization envelope than `useLocalStorage`'s plain `JSON.stringify(value)` (see `frontend/src/shared/hooks/useLocalStorage.ts`). AD-9 says "the existing `useLocalStorage` migration path" as though `useFanCardStore` already has one; it doesn't — `useLocalStorage.ts`'s own header comment states the *opposite* convention is what's actually implemented project-wide: "bumping the version is how a future breaking schema change is 'migrated' — old data is simply never read again under the new key, rather than needing in-place upgrade logic." I.e., the codebase's real, working precedent for "migration on key change" is *abandon old data*, not *seed-forward*. AD-9 asserts a new migration behavior (carry data forward into a new shape) while citing a mechanism (`useLocalStorage`'s migration path) that (a) isn't used by the store being modified and (b) doesn't actually do seed-forward migration anywhere in the codebase today.

**Why this creates incompatible builds, not just a bug:** two compliant devs reading AD-9 will resolve the ambiguity differently:
- **Dev A** takes "the existing `useLocalStorage` migration path" literally, assumes `useFanCardStore` already uses that hook, and writes a `useLocalStorage<Record<cardId,FanCardData>>('f1app__fanCard__v3', {})` — meaning the raw localStorage value at that key is a flat `Record<cardId,FanCardData>` JSON object, and the seed check (against a v2 key that's never present) is a dead branch.
- **Dev B** notices the store is actually Zustand `persist`-based, keeps using `persist` for idiomatic consistency with `v1`, and implements `migrate`/`version` inside `persist`'s config — meaning the raw localStorage value is `{"state":{"cards":{...}},"version":0}`, a different envelope entirely, and additionally has to decide independently (since AD-9 doesn't say) whether to key the seed-read off the real `v1` key or the AD's stated (wrong) `v2` key.

Either way, the two implementations produce **different literal localStorage payload shapes** at `f1app__fanCard__v3`, and at least one of them (arguably both, per point 1) fails to carry forward existing users' data. This is the single highest-severity finding in the spine.

**Recommendation:** Fix AD-9 to (a) name the real source key `f1app__fanCard__v1`, (b) state explicitly whether the v3 store continues to use Zustand `persist` (and if so, specify the `persist` `migrate`/`version` mechanics, not "the existing `useLocalStorage` migration path") or switches to a bare `useLocalStorage` call, and (c) require a test asserting an existing `v1` value survives the upgrade — not just a "seed if present" description.

---

## Finding 2 (High) — AD-11 / Structural Seed name a DTO file (`Dtos/NewsFeedItemDto.cs`) that doesn't exist, and creating it as specified would violate the inherited Ergast/OpenF1 isolation invariant

**Structural Seed says:**
```
backend/F1App.Api/
  Dtos/
    NewsFeedItemDto.cs          # MODIFIED — AD-11: + imageUrl, + snippet
```

**Actual codebase:** the type returned by `NewsFeedService.GetNewsAsync()` is `NewsItem`, a record defined at `backend/F1App.Api/Models/NewsItem.cs`:
```csharp
public record NewsItem(string Title, string Link, string Source, DateTimeOffset PublishedAt);
```
There is no `Dtos/NewsFeedItemDto.cs` anywhere in the backend. Every file that actually lives under `backend/F1App.Api/Dtos/` is an `Ergast*`/`OpenF1*` external-shape adapter (`Dtos/Ergast/ErgastScheduleResponseDto.cs`, `Dtos/OpenF1/OpenF1LapDto.cs`, etc.) — which matches the spine's own **Inherited Invariants** row: *"Ergast/OpenF1 external-shape isolation: only `Clients/`+`Dtos/` (backend) ... touch raw external shapes."* `NewsItem` is an internal domain/API-response model, correctly placed in `Models/` under the existing convention, not `Dtos/`.

This means AD-11's own "MODIFIED" instruction, if followed literally, tells a dev to create a *new* file (`Dtos/NewsFeedItemDto.cs`) inside a folder whose entire purpose (per the spine's own inherited-invariants table) is external-API-shape isolation — for a type that (a) isn't external-shape-isolating and (b) already exists elsewhere under a different name. This is finding-category 3 from the brief: a new AD (AD-11) quietly contradicts an inherited invariant (Dtos/ = external-shape isolation only) rather than extending it correctly.

**Divergence risk:** Dev A reads "MODIFIED" and correctly infers it means "modify `Models/NewsItem.cs`" (reading the actual code first). Dev B reads the Structural Seed literally and creates a *new* `Dtos/NewsFeedItemDto.cs`, wires the controller to return that instead, and now the codebase has two competing news-item shapes (`NewsItem` still used by `NewsFeedService` internally, `NewsFeedItemDto` bolted on for the controller boundary) — exactly the "two owners of one entity" failure mode named in the review brief. Contrast with AD-6, which explicitly did this reality-check for `TrackMap.tsx` (flagged `[ADOPTED — reality check]`) — AD-11 and the Structural Seed did not get the same scrutiny.

**Recommendation:** Correct the Structural Seed to `Models/NewsItem.cs — MODIFIED — AD-11: + ImageUrl, + Snippet` and drop the `Dtos/` reference, or explicitly justify why a new `Dtos/` file is warranted despite the isolation-scope precedent (it isn't, per the existing convention).

---

## Finding 3 (Medium-High) — FR-10's "inherited overlay pattern" doesn't exist in the codebase; this should likely be a fixed AD, not a Capability-Map presentation-only entry

**Capability → Architecture Map:** "FR-10 (Fan Card prompt) | `StandingsPage.tsx` modal | Inherited overlay pattern only — deferred suppression window (see Deferred)."

A repo-wide search for any existing modal/dialog/overlay primitive (`grep -rl "Modal\|Overlay\|Dialog"`, `role="dialog"`, `createPortal`, `fixed inset-0`) across `frontend/src/**/*.tsx` returns **zero matches**. There is no existing overlay component to inherit. The closest analog, `SeasonWrapped` (`frontend/src/features/standings/SeasonWrapped/SeasonWrapped.tsx`), is not an overlay at all — it's an inline conditional render (`if (!data) return null; return <SeasonWrappedCard .../>`), not a portal/dialog.

The spine's **Deferred** section only defers the dismissal-duration *constant* ("a dismissal-duration constant, not a structural decision"). It does not defer, and doesn't even surface, the actual structural question: is FR-10 a true modal (portal, focus trap, `role="dialog"`, backdrop, Escape-to-close) or an inline dismissible banner within `StandingsPage.tsx`? The PRD itself leaves this open ("popup or inline," FR-10). This is a real fork:
- **Dev A** builds a portal-rendered modal dialog with focus trapping (needed for AD-12's axe-core AA gate to pass — modals have specific a11y requirements: `aria-modal`, focus return, etc.).
- **Dev B** builds an inline collapsible banner at the top of `StandingsPage.tsx` — no portal, no focus trap, different a11y surface, different component location, different dismissal mechanics (banner collapse vs. dialog close).

Both are "compliant" with the spine as written, since nothing in it fixes this choice, and the two produce genuinely different code structures, different a11y test assertions under AD-12, and different UX. This is a case (per review lens #5) where something was wrongly left ungoverned — the choice of overlay-vs-inline is exactly the kind of decision the spine's other ADs (e.g., AD-1, AD-5) exist to nail down, and it's not free of consequence the way a suppression-window constant genuinely is.

**Recommendation:** Add a one-line AD (or extend the Capability Map row) fixing FR-10's mechanism as either a shared, newly-introduced overlay/dialog primitive (if so, name where it lives, e.g. `shared/components/Modal.tsx`, since none exists yet) or an inline banner. Don't defer only the dismissal window while leaving the structural shape unstated.

---

## Finding 4 (Medium) — AD-3/AD-4 leave scrub-during-active-playback unspecified: a real race condition between the interval timer and a direct index set

**AD-3 rule:** "Scrub = direct index set, no re-fetch."
**AD-4 rule:** "Playback advances `currentLapIndex` via a client-side interval timer at `baseIntervalMs / speed`. Pause clears the timer. Scrub sets the index directly with no animation catch-up (snap-to-lap, per `EXPERIENCE.md`)."

Neither AD states what happens to `isPlaying` / the running interval timer when the user scrubs **while playback is active** (not paused). `EXPERIENCE.md` doesn't resolve it either — the closest passage (line 127, Flow 2) has the user scrub, then separately "bumps speed to 2x," which is ambiguous about whether playback was continuous through the scrub. FR-7/FR-9 in the PRD (`prd.md:165-184`) each describe scrub and pause/resume as independent capabilities and never address their interaction.

Concretely, two compliant implementations diverge:
- **Dev A** treats scrub as implicitly pausing (`isPlaying: false` as a side effect of any scrub action) — a common video-player UX convention, but not written anywhere in this spine.
- **Dev B** treats `isPlaying` and `currentLapIndex` as fully independent per AD-3/AD-4's literal text (scrub "sets the index directly," full stop) — leaving the interval timer running. If the timer's closure captured a stale `currentLapIndex` (a classic Zustand-in-`setInterval` bug) or simply fires again shortly after the scrub, it will advance from the *pre-scrub* lap, immediately overwriting the user's scrub target with a visible jump backward/forward — a live, user-visible bug that only one of the two implementations would exhibit, and neither implementer would consider themselves out of spec.

This is a genuine state-mutation race the spine's own stated intent (AD-3's "Prevents: ... playback position being re-fetched per lap"; AD-4's "Prevents: a second real-time channel duplicating [the] job") doesn't cover, because the risk here isn't a second data source — it's two competing writers (interval tick vs. scrub handler) to the same `currentLapIndex` field with no arbitration rule.

**Recommendation:** Add one sentence to AD-4 (or a new consequence under AD-3): "Scrubbing while `isPlaying` is true clears the interval and sets `isPlaying: false`" (or the opposite — pin *some* answer), so the timer-vs-scrub write order is not left to be discovered independently by whoever implements `ReplayBar.tsx`.

---

## Finding 5 (Low-Medium) — AD-1's "pass through `normalizeSnapshot()`" understates what actually has to be synced into `liveRaceStore` per replay frame

AD-1 says replay frames "pass through the existing `normalizeSnapshot()` before reaching Zustand." Checked against the real code (`frontend/src/shared/utils/normalizeSnapshot.ts`, `frontend/src/features/live-race/hooks/useSignalRConnection.ts`), `normalizeSnapshot()` only transforms `RaceStateSnapshot.drivers` (an array) into a `Record<driverNumber, DriverState>` — it says nothing about `lapChart`, `fastestSectors`, `timeline`, `sessionMode`, `fallbackRaceName`, or `circuitId`, all of which are separate `liveRaceStore` fields that the live path (`useSignalRConnection.ts`) populates via **six additional setter calls** (`setLapChart`, `setFastestSectors`, `setTimeline`, `setSessionMode`, `setFallbackRaceName`, `setCircuitId`) alongside `setDrivers(normalizeSnapshot(...))`.

Since AD-1 explicitly binds `SectorBoard`, `LapTimeChart`, and `EventTimeline` — components that read `fastestSectors`, `lapChart`, and `timeline` respectively, not just `drivers` — a dev implementing the replay-frame-to-store sync "per AD-1's letter" (call `normalizeSnapshot()`, done) would under-populate the store and leave those three components stale/frozen during replay, while a more careful dev would independently replicate all six setter calls from `useSignalRConnection.ts`. This isn't as severe as Findings 1-3 (the fix is discoverable by reading `RaceStateSnapshot`'s actual shape), but it's exactly the kind of one-AD/two-readings gap the review brief asks to hunt for, and it sits in the same feature AD-1 was written to protect ("Prevents: replay and live rendering diverging into two component trees or two data shapes").

**Recommendation:** AD-1's rule should enumerate the full sync (or point at `useSignalRConnection.ts`'s setter list as the contract to replicate), not name `normalizeSnapshot()` alone as if it were sufficient.

---

## Finding 6 (Low) — AD-2's `{season}` route segment has no precedent anywhere in the existing API surface

AD-2 introduces `GET /api/races/{season}/{round}/replay`. Checked every existing controller (`RacesController` — `[Route("api/races")]`, `{round:int}` only, no season; `CircuitsController` — `{circuitId}` only; `StandingsController` — no season param; `DriversController` — no season param): **no endpoint in the current backend takes a `season` route parameter.** The whole API is implicitly current-season-scoped today. AD-2 is the first place a `season` dimension appears in routing, and the spine doesn't say (a) how the frontend determines which season value to pass (hardcode current year via `new Date().getFullYear()`? derive it from the fallback race's own season field returned by `/api/races/last-result`?), or (b) whether `season` is validated against "current season only" (given AD-2 says replay data comes from "the same OpenF1 historical REST + Ergast sources as the existing fallback-to-last-race path," which itself has no multi-season provision). This is lower severity than Findings 1-4 since it's unlikely to cause two implementations to diverge in an observably incompatible way (there's only one race to replay right now), but it's an unstated assumption that will matter the moment someone asks "can I replay last season's races" and finds the spine silent on it.

---

## Non-findings / things that held up under scrutiny

- **AD-6** (circuit-configs same-origin fetch bug): independently verified against `TrackMap.tsx` — `fetch(`${apiBase}/circuit-configs/${circuitId}.json`)` with `apiBase` resolving to the backend origin in both `.env.local` and `.env.example`. The AD's "pre-existing latent bug" claim is accurate, and this AD is a model example of reality-checking a "reuse existing X" claim before asserting it — the standard the rest of the spine should have been held to.
- **Capability → Architecture Map coverage of FR-1–FR-18:** every FR from FR-1 through FR-18 appears in the map with either a governing AD or an explicit no-new-AD justification. No silently ungoverned FR (setting aside Finding 3's objection to *how well-founded* FR-10's justification is).
- **Deferred section items** other than FR-10 (suppression window constant, Championship Sidebar third slot, rollout sequencing, field-removal audit) are genuinely non-structural — two devs choosing different values there don't produce incompatible builds.
- **AD-8** (Championship Sidebar reuses standings query): `frontend/src/shared/api/queryKeys.ts` exists as claimed; no second data path risk found.
