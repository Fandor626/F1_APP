# PRD Addendum — F1_poc Phase 2 — UI/UX Improvement Pass

_Technical decisions and implementation context that belong downstream (architecture, solution design) rather than in the PRD itself._

---

## FR-4 / FR-13 — Track outline rendering

Both the Calendar race card (FR-4) and the Race Weekend Detail page (FR-13) need a real circuit track outline, at two different sizes and levels of detail. Not yet decided:

- **Source of the outline geometry** — hand-authored SVG per circuit, a public dataset of circuit layouts, or generated from Ergast/OpenF1 coordinate data. OpenF1's live x/y car-position stream, already used for MVP FR-7's animated track map, implies the geometry exists somewhere in the data pipeline and may be reusable rather than needing a new source.
- **Shared asset vs. two renderings** — whether the card-level (FR-4) and detail-page-level (FR-13) outlines are the same asset at different sizes, or warrant separate treatments (for example, the detail page could overlay sector markers or DRS zones).
- **Degradation behavior** — the PRD confirms missing outlines degrade gracefully; the specific fallback treatment (omit entirely vs. generic placeholder shape) is still open.

Architect/UX to resolve before FR-4/FR-13 implementation.

## FR-11 — Fan Card asset sourcing

FR-11 requires driver photos, autographs, team logos, and team-principal names. Team logos can likely be sourced the same way team colors/branding are handled elsewhere in the app `[unverified]`. Photos, autographs, and team-principal data are not currently in the scope of either Ergast or OpenF1 (see PRD §9 Open Question 4 — flagged `[NOTE FOR PM]` as a possible blocker). Options to evaluate:

- Licensed/stock photo APIs vs. manually curated asset set (small, fixed roster of ~20 current drivers/10 constructors makes manual curation plausible for a solo project).
- Autograph representation — a real scanned signature asset vs. a stylized signature-style font rendering (the latter avoids licensing/sourcing risk entirely).
- Team-principal names/data — low-risk, publicly known facts, likely fine to hand-maintain in a static config rather than pulling from an API.

Architect/UX to resolve before FR-11 implementation; recommend spiking this early given it gates a whole feature area (§4.4).
