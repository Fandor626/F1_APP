---
baseline_commit: 6d2cf8c
---

# Story 9.2: Fan Card visual redesign

Status: review

## Story

As a fan who has created a Fan Card,
I want it to show my chosen driver's photo, autograph, team logo, and team principal,
so that the card feels like a real, personal keepsake rather than a placeholder.

## Acceptance Criteria

1. **Given** a Fan Card for a supported driver/constructor pairing, **when** it renders, **then** it shows the driver's photo, a stylized-signature-font autograph, the constructor's team logo/color rule, and the team principal's name — alongside everything the MVP card already showed (driver/constructor standings position + points, circuit).
2. **Given** driver photos and team-principal names are hand-curated static assets, not a new external API (AD-10), **when** the roster is set up, **then** infrastructure exists for all ~20 current drivers / 10 constructors (asset path convention + team-principal map covering all 10 constructors).
3. **Given** a driver whose photo asset is missing (e.g. a mid-season roster change not yet curated, or simply because no licensed photo file could be sourced), **when** their card renders, **then** it falls back to an initials placeholder — never a broken-image state, and card creation is never blocked.
4. **Given** the card's visual treatment, **when** it renders, **then** it uses the portrait 5:7 trading-card aspect ratio, with constructor color reduced to a 4px top rule only — never a full-bleed team-color background (UX-DR5).
5. **Given** the card, **when** exported, **then** it remains exportable as a client-side-generated image, unchanged from the MVP.

## Tasks / Subtasks

- [x] Task 1: Team principal + expanded constructor color data (AC 1, 2)
  - [x] New `frontend/src/shared/data/teamPrincipals.ts`: `Record<string, string>` keyed by constructor name, covering all 10 current constructors, plus `teamPrincipal(name: string): string | null` lookup (returns `null` for unmatched names — never throws, matching the existing `constructorColor` fallback pattern).
  - [x] Expand `frontend/src/features/standings/constructorColors.ts` from 4 to all 10 constructors, and add a `constructorBadgeLabel(name): string` helper (short monogram for the team badge, e.g. "M" for McLaren).
  - [x] Add the 6 missing `--color-team-*` CSS custom properties to `frontend/src/index.css` alongside the existing 4.
- [x] Task 2: Driver photo asset convention + fallback (AC 2, 3)
  - [x] New `frontend/src/shared/data/driverPhotos.ts`: `driverPhotoUrl(driverId: string): string` returning `/fan-card-assets/drivers/{driverId}.jpg` (AD-10's specified path).
  - [x] New directory `frontend/public/fan-card-assets/drivers/` with a `README.md` documenting the naming convention (`{driverId}.jpg`, driverId matches the Ergast-style id already used app-wide) — no actual photo files are included in this change (no licensed source available in this environment); the initials-placeholder fallback (Task 3) is what actually renders today for every driver, satisfying AC 3 unconditionally.
- [x] Task 3: Redesign `FanCard.tsx` to the trading-card treatment (AC 1, 3, 4, 5)
  - [x] Portrait 5:7 aspect ratio card (`aspect-[5/7]`), replacing the previous plain bordered box.
  - [x] 4px top rule in `constructorColor(picks.constructorName)` — `<div className="h-1 shrink-0" style={{ background: constructorColor(...) }} />`. No other use of the constructor color as a background anywhere in the card (AC 4).
  - [x] Recessed `bg-bg-inset` photo panel: `<img>` pointed at `driverPhotoUrl(picks.driverId)`, `onError` swaps to an initials placeholder (`useState` flag, computed initials from `picks.driverName`) — never a broken `<img>` icon.
  - [x] Info panel below the photo: driver name, a small colored team badge (`constructorBadgeLabel`) + constructor name, "Team Principal: {name}" line (rendered only when `teamPrincipal()` returns non-null), then the existing driver-position/points, constructor-position/points, and circuit-name content preserved as plain text (kept for the existing test assertions and "everything the MVP card already showed").
  - [x] Autograph line: driver name rendered in a script/cursive font stack (`font-family: "Segoe Script","Brush Script MT",cursive`, matching the UX mockup's system-font approach — no new font dependency/network fetch needed), slight rotation, dashed top border, per `DESIGN.md`'s `trading-card.autograph` spec.
  - [x] `cardRef`/export (`toPng`) still wraps the same rasterized subtree — export behavior unchanged (AC 5).
- [x] Task 4: Tests
  - [x] New `FanCard.test.tsx`: renders driver photo `<img>` with correct `src`; falls back to initials placeholder on image error; shows team principal when known, omits the line when unknown; shows the 4px team-rule element styled with the constructor's color; still contains driver/constructor/circuit text content (position, points, names); export button still triggers `toPng`.
  - [x] `FanCardPage.test.tsx`: existing assertions (driver/constructor/circuit text content, PNG export) continue to pass unmodified against the redesigned component — verifies "everything the MVP card already showed" (AC 1).

## Dev Notes

- `frontend/src/features/fan-engagement/FanCard.tsx` is the only file that renders the actual card visuals; it receives `CompleteFanCardPicks` and is mounted by `FanCardPage.tsx` — no prop-shape change needed for this story (multi-card support is Story 9.3).
- `frontend/src/features/standings/constructorColors.ts` currently only has 4 of 10 constructors mapped (`Red Bull Racing`, `Ferrari`, `Mercedes`, `McLaren`) with a neutral-dim fallback for the rest — this story expands it to all 10 using the app's existing naming convention for the 4 known ones and standard Ergast constructor names for the remaining 6 (`Aston Martin`, `Alpine F1 Team`, `Williams`, `RB F1 Team`, `Sauber`, `Haas F1 Team`). Since live names are only knowable at runtime from the Ergast-backed API, any exact-string mismatch degrades gracefully to the existing neutral fallback — consistent with how `constructorColor()` already behaves for unknown names, so nothing breaks if the real API string differs slightly.
- `html-to-image`'s `toPng` is already a dependency — no new library needed for export; `cardRef` must stay attached to the actual rasterized card subtree (not the outer buttons wrapper), same as before.
- No dialog/portal library, no font package, no image-hosting service exists or is needed for this story — the autograph uses a system cursive font stack (matches the UX mockup exactly) and the photo panel uses a plain `<img onError>` fallback, both zero-dependency.
- Real photo assets are out of scope for this environment (no legitimate sourcing path for licensed driver photography) — the initials-placeholder fallback is intentionally the universal rendering path today; the asset directory/path convention is real and ready for future manual curation.

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 9.2]
- [Source: _bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md#AD-10]
- [Source: _bmad-output/phase-2/planning-artifacts/ux-designs/ux-F1_poc-2026-07-11/DESIGN.md] — `trading-card` spec (5:7 aspect, 4px team rule, recessed photo panel, script autograph)
- [Source: frontend/src/features/fan-engagement/FanCard.tsx], [Source: frontend/src/features/standings/constructorColors.ts]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Completion Notes List

- All 4 tasks complete.
- Real driver photo image files are not included (no legitimate sourcing path in this environment); the app-wide initials-placeholder fallback is the actual rendering path for every driver today, which fully satisfies AC 3 and keeps AC 1's "driver's photo" slot visually present and functional (never a broken image) once real assets are curated later — asset path convention and directory are in place per AD-10.
- Constructor color/team-principal maps use best-effort standard names for the 6 constructors not already present in the codebase; both lookups fail closed (neutral color / omitted principal line) rather than showing wrong data if the live API's exact string differs.

### File List

- New: `frontend/src/shared/data/teamPrincipals.ts`
- New: `frontend/src/shared/data/driverPhotos.ts`
- New: `frontend/public/fan-card-assets/drivers/README.md`
- New: `frontend/src/features/fan-engagement/FanCard.test.tsx`
- Modified: `frontend/src/features/fan-engagement/FanCard.tsx`
- Modified: `frontend/src/features/standings/constructorColors.ts`
- Modified: `frontend/src/index.css` (6 new `--color-team-*` tokens)
