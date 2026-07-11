# PRD Addendum — F1_poc Phase 2 — UI/UX Improvement Pass

_Technical decisions and implementation context that belong downstream (architecture, solution design) rather than in the PRD itself._

---

## FR-4 / FR-13 — Track outline rendering

Both the Calendar race card (FR-4) and the Race Weekend Detail page (FR-13) need a real circuit track outline, at two different sizes and levels of detail. Not yet decided:

- **Source of the outline geometry — resolved during the Phase 2 UX session (2026-07-11).** [`f1db/f1db`](https://github.com/f1db/f1db) (CC-BY-4.0, attribution required) provides accurate, ready-made SVG outlines for every circuit and every historical layout, in four styles (black/black-outline/white/white-outline), at `src/assets/circuits/{style}/{circuitId}-{layoutNumber}.svg`. `src/data/circuits/{circuitId}.yml` maps layout numbers to year ranges — always use the entry with "present" in its range for the current F1 configuration (e.g. `monza-7`, not `monza-1`). Verified by rendering to PNG and visual review: recognizably accurate for Monza, Spa-Francorchamps, Zandvoort, and Silverstone. Alternatives noted but not used: [`julesr0y/f1-circuits-svg`](https://github.com/julesr0y/f1-circuits-svg) (same CC-BY-4.0 terms) and OpenStreetMap's `highway=raceway` tag (ODbL, raw GPS-traced coordinates via Overpass API, for a from-scratch/coordinate-based approach instead of pre-made SVGs). OpenF1's live x/y stream remains a live-position *data* source, not a shape *asset* source — the two are complementary, not alternatives.
- **Shared asset vs. two renderings** — the Phase 2 mocks reuse the same f1db path at two sizes/crops (a tight bounding-box crop scaled down for the card, a larger detailed crop for the Race Weekend Detail page); no separate asset needed per size. Whether the detail page additionally overlays sector markers or DRS zones on top of the f1db outline is still open.
- **Degradation behavior** — the PRD confirms missing outlines degrade gracefully; the specific fallback treatment (omit entirely vs. generic placeholder shape) is still open. Moot for any circuit f1db covers (all current F1 calendar circuits do); only relevant if a future calendar entry lacks an f1db layout.

Architect to confirm licensing/attribution handling (CC-BY-4.0 requires crediting f1db) before FR-4/FR-13 implementation; otherwise this is substantially de-risked.

## FR-11 — Fan Card asset sourcing

FR-11 requires driver photos, autographs, team logos, and team-principal names. Team logos can likely be sourced the same way team colors/branding are handled elsewhere in the app `[unverified]`. Photos, autographs, and team-principal data are not currently in the scope of either Ergast or OpenF1 (see PRD §9 Open Question 4 — flagged `[NOTE FOR PM]` as a possible blocker). Options to evaluate:

- Licensed/stock photo APIs vs. manually curated asset set (small, fixed roster of ~20 current drivers/10 constructors makes manual curation plausible for a solo project).
- Autograph representation — a real scanned signature asset vs. a stylized signature-style font rendering (the latter avoids licensing/sourcing risk entirely).
- Team-principal names/data — low-risk, publicly known facts, likely fine to hand-maintain in a static config rather than pulling from an API.

Architect/UX to resolve before FR-11 implementation; recommend spiking this early given it gates a whole feature area (§4.4).
