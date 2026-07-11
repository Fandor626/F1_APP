# PRD Quality Review — F1_poc Phase 2 — UI/UX Improvement Pass

## Overall verdict

This is a well-constructed incremental PRD: it has a real thesis ("less clutter, always show something useful"), traces every feature back to it, and is honest about what it doesn't know (6 Open Questions, 12 indexed assumptions, 2 `[NOTE FOR PM]` callouts at genuine risk points — data sourcing for photos/autographs, the win-probability-widget/Win-Prediction relationship). Cross-references into the MVP PRD (FR-1, FR-4, FR-6, FR-16, FR-20–23) all check out against the actual MVP document, and the reused Glossary is copied verbatim with no drift. The main soft spots are mechanical rather than structural: two FRs (FR-13, FR-15) skip the testable-consequences pattern every sibling FR follows, two "lives in `addendum.md`" pointers resolve to a file that was never created, and one FR (FR-17) claims to realize a UJ it isn't actually in.

## Decision-readiness — strong

Trade-offs are named, not smoothed. §1 states plainly that the sidebar consolidation means race cards stop repeating full standings — a real thing given up in exchange for "specific, textured content." Open Question 2 is a genuinely unresolved tension (does the new plain-language Win Prediction replace, sit above, or hide the MVP's existing raw win-probability display?) rather than a rhetorical question answered in the next line. The Non-Goals section states the gamification exclusion as a deliberate, reasoned call ("explicit scope boundary set during brainstorming") rather than an omission. `[NOTE FOR PM]` callouts land on real tensions — FR-16's field-removal content audit, and OQ4's data-sourcing risk that "could affect FR-11/FR-18 feasibility."

### Findings
- **low** Parked Could-items lack rationale (§6.2) — "Sidebar live countdown" and "Standings-page custom loader" are parked with only "candidate for a future phase," no stated reason (effort, uncertain value, etc.) for why they didn't make Must-have. *Fix:* one clause each on why they were deprioritized, for continuity with future-phase planning.

## Substance over theater — strong

No persona proliferation — one recurring "returning/casual fan" persona across all 5 UJs, which fits a solo-operator consumer app; not padded to look thorough. The Vision (§1) is specific to this product's actual complaints (standings repeated on every card, blank Live Race page) rather than swappable boilerplate. Cross-cutting NFRs (§7) mostly avoid boilerplate: Accessibility names concrete controls (Calendar filter, Replay controls, Fan Card flow) and is backed by a measurable SM-2 (axe/Lighthouse ≥ 95); Performance is backed by a named counter-metric (SM-C1) rather than a bare "must be fast." No inflated differentiation or innovation-theater language.

## Strategic coherence — strong

The thesis is stated once (§1: "stop repeating the same generic data... in favor of showing specific, textured content" / "never leave the user looking at an empty or dead page") and every one of the 7 feature groups in §4 traces back to one or both moves: Calendar declutters standings and adds real track/lap content; Live Race eliminates the empty state and makes it explorable; Standings/Fan Card address discoverability and richness; Race Weekend Detail replaces a data dump with plain-language context; News Feed adds preview content before an external redirect. Success Metrics are honest about their own limits (§8: "No real users exist yet — metrics are proxy/completion-based") rather than reaching for vanity usage metrics, and SM-C1/SM-C2 are genuine counter-metrics that push back against SM-1's feature-richness incentive (no TTI regression, no added required steps to core journeys).

### Findings
- **medium** FR-17 is only loosely tethered to the thesis and to the UJ it claims (§4.6, §2.3 UJ-4) — see Downstream usability below; this is the one feature area that reads closer to "capability someone wanted" than a thesis-derived requirement.

## Done-ness clarity — adequate

Most FRs (14 of 18) carry a "Consequences (testable)" block with verifiable, specific conditions (e.g. FR-1: "zero past Race Weekends are rendered"; FR-9: "Resuming after pause continues from the paused lap/position, not from the start or the live edge"). This is the dimension the rubric asks to be unforgiving on, and there are real gaps:

### Findings
- **medium** FR-13 (Track layout visualization, §4.5) has no "Consequences (testable)" block at all — just a one-line requirement statement. Every sibling FR in §4.5 (FR-14, FR-16) has one; FR-13 doesn't say what "displays the circuit's real track outline/shape" means as a pass/fail check (recognizable per circuit? degrades how if missing, per FR-4's pattern?). *Fix:* add a consequences block mirroring FR-4's graceful-degradation language.
- **medium** FR-15 (Track historical data and records, §4.5) also has no "Consequences (testable)" block — only an inline `[ASSUMPTION]` about data source. "Additional circuit historical data and records beyond lap times" has no stated minimum content (how many records? which categories?) and no pass/fail condition. *Fix:* name at least the record categories in scope (e.g. "past winners list, corner count, DRS zones — per MVP FR-20") and a testable minimum.
- **medium** FR-17's consequence ("clearly structured, scannable layout") and §7's Mobile NFR ("fully usable on common mobile viewport widths," "one-handed operation feasible for the Replay controls") are adjective-graded, not bounded — exactly the "user-friendly" / "reasonable performance" pattern the rubric flags. SM-3 gives Mobile a numeric backstop (Lighthouse ≥ 85) but only for 3 of the redesigned pages (Calendar, Live Race, Standings) — Fan Card and Race Weekend Detail, both explicitly redesigned, have no performance or usability bound of their own. *Fix:* either extend SM-3's page list or give FR-17/§7 their own concrete criteria (e.g. named breakpoints, a specific interaction pattern for one-handed use).

## Scope honesty — strong

§5 Non-Goals does real work — each item is reasoned, not just listed (no accounts "consistent with MVP's no-auth design," no gamification as "explicit scope boundary set during brainstorming"). Every inline `[ASSUMPTION]` tag round-trips cleanly into §10's Assumptions Index (verified all ~13 inline tags against the 12 index entries — SM-2/SM-3's tooling-threshold assumptions are combined into one index line, everything else is 1:1). De-scoped items in §6.2 are named, not silently dropped. Open-items density (6 Open Questions + ~13 assumptions + 2 NOTE FOR PM) is proportionate for a document that itself says "everything the user described is Must-have" — nothing here reads as a real decision hidden by omission.

### Findings
- **medium** Two FRs point to `addendum.md` for load-bearing implementation content that has no other home — FR-4 (§4.1): "Track outline rendering technique (SVG generation/source)... lives in `addendum.md`, not here," and FR-11 (§4.4): "Sourcing/licensing of driver photos and autograph assets — implementation detail for `addendum.md`." No `addendum.md` exists for this PRD (confirmed: only `prd.md` and `.memlog.md` are in the directory, unlike the MVP PRD's folder which does have one). FR-11's underlying question is at least recoverable — Open Question 4 separately covers photo/autograph sourcing — but FR-4's track-outline rendering decision is not captured anywhere else in the document. *Fix:* either create a minimal `addendum.md` capturing these two items, or convert the FR-4 pointer into an Open Question the way FR-11's content already effectively is.

## Downstream usability — strong

The reused MVP Glossary (§3: 14 terms) is copied verbatim from `prd-F1_poc-2026-06-15/prd.md` §3 with no case, plural, or synonym drift. The four new terms (Championship Sidebar, Race Replay, Fan Card, Win Prediction) are defined once and used consistently in that exact form throughout §4 and §9. FR IDs (FR-1–FR-18) and UJ IDs (UJ-1–UJ-5) are contiguous and unique, and MVP cross-references resolve correctly against the source PRD: MVP FR-1, FR-4, FR-6, FR-16, FR-20–23 are cited accurately by number and content.

### Findings
- **low** FR-17 (§4.6) claims "Realizes UJ-4 (extended)," but UJ-4's own "Realizes:" list (§2.3) enumerates only FR-13–FR-16, and UJ-4's narrative (checking a Race Weekend before it starts) never visits the Driver/Constructor Profile pages FR-17 actually governs. FR-17 is effectively a floating FR without a UJ that describes its own journey. *Fix:* either add FR-17 to UJ-4's Realizes list and note the extension explicitly in the UJ path, or give profile-page browsing its own (even brief) UJ.
- **low** UJ protagonists are unnamed ("A casual fan," "same fan," "A new visitor") rather than literally named — consistent with the MVP PRD's own UJ style (also unnamed), so this isn't new drift, but it falls short of the rubric's "named protagonist" criterion. Low impact since each UJ does carry persona+context inline.

## Shape fit — strong

This is a brownfield, consumer-facing UX pass, and the PRD is shaped accordingly: UJs are load-bearing (5 UJs, each driving a named FR cluster), not overhead. The brownfield distinction the rubric asks for is made explicitly — §2 states Phase 2's UJs are new but MVP's UJ-1–UJ-3 "still hold structurally; Phase 2 changes how they look and feel, not what they accomplish." Every MVP-referencing claim checked against the source PRD (FR-1, FR-4, FR-6, FR-16, FR-20–22, FR-23) is accurate. Not over-formalized (one persona, 5 UJs for 18 FRs) or under-formalized (a real UX-facing product with UJs, not just a feature list).

## Mechanical notes

- **Glossary**: clean. All 14 reused terms match the MVP PRD's §3 exactly; new terms (Championship Sidebar, Race Replay, Fan Card, Win Prediction) used consistently.
- **ID continuity**: FR-1–FR-18 contiguous, no gaps/dupes. UJ-1–UJ-5 and SM-1–SM-4/SM-C1–C2 likewise clean.
- **Assumptions Index roundtrip**: clean. All inline `[ASSUMPTION]` tags are indexed in §10; no orphaned index entries.
- **UJ protagonist naming**: present but unnamed (see Downstream usability finding above) — matches MVP PRD convention.
- **Broken/dangling reference**: `addendum.md` is cited twice (§4.1 FR-4, §4.4 FR-11) but does not exist in this PRD's folder (see Scope honesty finding above).
- **FR/UJ cross-reference gap**: FR-17 claims to realize UJ-4 but is absent from UJ-4's own Realizes list (see Downstream usability finding above).
