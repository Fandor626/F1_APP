# Spine Pair Review — F1_poc Phase 2

## Overall verdict

This is a well-disciplined extension spine pair: every token resolves (locally or into phase-1), every UJ has a full flow with climax and failure path, and the mocks are unusually well self-documented (each `.working/*.html` carries a header comment tracing every visual choice back to memlog/PRD/inherited-DESIGN/inherited-EXPERIENCE lines). Inheritance discipline is strong — `sources:` frontmatter is identical and resolves in both files, component names map 1:1 across DESIGN/EXPERIENCE with only cosmetic casing differences, and 5 of 5 known open items (sidebar third slot, Fan Card asset sourcing, prompt suppression window, news-preview-row sizing, Profile grouped-stats visual treatment) are transparently flagged with `[NOTE FOR UX]` rather than silently glossed over. The pair is source-extractable as a contract with two real defects worth fixing before handoff: a genuine self-contradiction (EXPERIENCE.md's own State Patterns table specifies the "zero completed races" state, but the Key Flows failure note for the same scenario claims it's "unspecified" and "not flagged anywhere upstream" — both false), and a rendered mock element (the replay bar's lap readout) that's named in Responsive & Platform but has no DESIGN.md visual-spec row or EXPERIENCE.md behavioral row anywhere.

## 1. Flow coverage — strong
Checked: UJ-1 through UJ-5 against EXPERIENCE.md § Key Flows for named protagonist, numbered steps, bolded climax, and failure path.

All 5 PRD UJs have a corresponding flow: Flow 1 (Priya) = UJ-1, Flow 2 (Tom) = UJ-2, Flow 3 (Aisha) = UJ-3, Flow 4 (Marcus) = UJ-4, Flow 5 (Elena) = UJ-5. Each has a named protagonist + context, numbered steps, an explicit `**Climax:**` beat, and a `Failure:` line. Content maps cleanly onto each UJ's Path/Climax/Resolution/Edge-case prose in the PRD (e.g., Flow 4 additionally dramatizes the FR-17 profile click-through that UJ-4's "Realizes" list appends).

### Findings
- **high** Flow 2's failure note directly contradicts EXPERIENCE.md's own State Patterns table two sections earlier — see also Mechanical notes (EXPERIENCE.md:72 vs EXPERIENCE.md:130). *Fix:* delete or rewrite the Flow 2 failure line; it's stale text left over from before the "Zero completed races this season" state and its memlog decision existed.

## 2. Token completeness — strong
Checked: every frontmatter `components.*` entry in DESIGN.md and every `{path.to.token}` reference in its prose against phase-1 DESIGN.md's frontmatter (colors, typography, rounded, spacing, components) plus this file's own frontmatter.

All references resolve. Traced individually: `{colors.bg-inset}`, `{colors.bg-card}`, `{colors.border-soft}`, `{colors.text-primary/secondary/tertiary}`, `{colors.accent-editorial}` all exist in phase-1 `colors`; `{rounded.lg}`/`{rounded.md}` exist in phase-1 `rounded`; `{spacing.6}` (28px) and `{spacing.7}` (32px) exist in phase-1 `spacing`; `{components.weekend-card}`, `{components.tab-toggle}`, `{components.inline-link}` all exist in phase-1 `components`. The one literal (non-token) value — `replay-bar.scrub.track: '#262b33'` — is explicitly annotated in-line as "unnamed inherited neutral, matches existing timeline track color," which the DESIGN.md spec permits (component tokens may be literal values or `{path}` references). No misses.

### Findings
None.

## 3. Component coverage — thin
Checked: every component named in DESIGN.md § Components (and its frontmatter) against a same-named row in EXPERIENCE.md § Component Patterns, and vice versa; cross-checked names appearing in EXPERIENCE.md's Responsive & Platform table against both Components sections; cross-checked against rendered mock elements.

7 of 9 frontmatter components map cleanly 1:1 (sidebar-championship↔Championship Sidebar, weekend-card-v2↔Race Weekend card v2, replay-bar↔Race Replay bar, trading-card↔Trading card + grid, fancard-prompt-modal↔Fan Card creation prompt, prediction-callout↔Win Prediction callout, track-records-section↔Track Records section, news-preview-row↔News preview row — 8 actually, casing/wording deltas only, no gaps).

### Findings
- **high** EXPERIENCE.md's Responsive & Platform row names a "lap readout" as one of the Replay bar's five inline desktop controls (EXPERIENCE.md:106), and the rendered mock implements it (`.replay-lap-readout` / "Lap 44 / 52", `key-live-race.html:143-144,355`) — but DESIGN.md's `replay-bar` component (DESIGN.md:33-51) defines only `playPause`, `scrub`, and `speedGroup`; there is no `lapReadout` token (position, type role, color) anywhere in DESIGN.md, and EXPERIENCE.md's "Race Replay bar" Component Patterns row (EXPERIENCE.md:54) never mentions it behaviorally either (e.g., whether it updates live during scrub-drag, its relationship to the scrub thumb). *Fix:* add a `lapReadout` entry to `replay-bar` in DESIGN.md (it's already using `numeric-dense`-style tabular figures in the mock) and one clause to the Component Patterns row.
- **medium** `circuit-stat-tile` (DESIGN.md:82-85) only specs the four stat tiles; the past-winners list that sits directly below it (mentioned only in prose: "above the past-winners table," DESIGN.md:112) has no row/border/typography treatment anywhere in DESIGN.md, and EXPERIENCE.md folds both under one row, "Circuit History section" (EXPERIENCE.md:59), without separately specifying the list's visual or behavioral pattern beyond "driver names link to their profile." Unlike the other 5 known open items in this pair, this one is not flagged with a `[NOTE FOR UX]` — it reads as resolved when it isn't. *Fix:* either add an explicit `[NOTE FOR UX]` acknowledging the gap (consistent with how the pair handles its other 4 open items) or give the past-winners list its own token (even "reuses inherited list-row defaults" as an explicit statement, matching the news-preview-row treatment).
- **low** `Profile grouped-stats presentation` has a full EXPERIENCE.md Component Patterns row (EXPERIENCE.md:60) but no frontmatter component entry and only a prose bullet in DESIGN.md that self-declares "a real gap: the grouping is decided, its visual rendering is not" (DESIGN.md:114). This is transparently flagged, unlike the finding above, so it's lower-severity — noting it here only because it is a genuinely introduced (FR-17) surface with zero visual-spec, and a downstream consumer building from DESIGN.md alone would have nothing to build the section-header/divider treatment from.

## 4. State coverage — adequate
Checked: EXPERIENCE.md § State Patterns "Addition" table against the 5 Phase-2-touched surfaces (Calendar, Live Race, Standings, Fan Card, Race Weekend Detail) for empty/loading/error/new-replay states.

Calendar: filter states (Future default, Past/All) covered. Live Race: fallback/replay, zero-completed-races, and playing/paused/scrubbing states all covered — this is the surface with the most genuinely new state complexity and it's handled thoroughly, including the cross-tab-background/resume case. Standings: prompt shown/dismissed covered (suppression window explicitly left open, consistent with PRD §9 OQ). Fan Card: zero-cards vs N-cards covered. Race Weekend Detail: prediction table collapsed/expanded, no-qualifying-yet, and no-prior-result/incomplete-history states all covered, matching phase-1's "absent, not placeholder" convention.

### Findings
- **medium** See Flow coverage finding #1 above (EXPERIENCE.md:72 vs :130) — the "Zero completed races" state row exists and is well-specified, but a downstream consumer reading only Key Flows would be told it's unspecified, which could cause it to be re-litigated or skipped during implementation.
- **low** No explicit state is defined for a genuine data-fetch failure on the Live Race replay data itself (distinct from "zero completed races" — e.g., the historical-race API call errors out). Phase-1's general "Ergast unavailable" state may not cleanly cover this since Live Race's live path uses OpenF1, not Ergast. Minor; likely inherits phase-1's generic error-state posture by default, but isn't explicitly stated.

## 5. Visual reference coverage — adequate
Checked: whether each of the 4 `.working/*.html` mocks is linked inline at its relevant spine section and captioned.

All 4 mocks are individually excellent as self-contained artifacts — each carries a substantial header comment (`key-calendar.html:8-24`, `key-live-race.html:8-22`, `key-fancard.html:8-27`, `key-race-weekend-detail.html:8-32`) that traces every rendered element back to a specific memlog decision, PRD FR, and named inherited DESIGN/EXPERIENCE component, including self-flagging two gaps directly in the mock comments ("FLAGGED AS A GAP in return summary" for the Calendar filter's visual treatment and Fan Card asset placeholders). However, at the spine-file level, all 4 mocks are cited only once, bundled into a single "Composition reference" line under § Information Architecture (EXPERIENCE.md:34) rather than linked near each surface's own Component Patterns/State Patterns rows. DESIGN.md never links any mock at all.

### Findings
- **low** Mock links are bundled in one IA-section line rather than distributed per-surface with individual captions describing what each illustrates (the task's literal bar). This matches the established convention in both phase-1 EXPERIENCE.md and the reference examples (Quill, Drift) — none of which caption mocks per-section either — so it is not a deviation from house style, but it does mean a consumer implementing, say, just the Replay bar has to go to the IA table to discover `key-live-race.html` exists rather than finding the pointer next to the Replay bar's own Component Patterns row. *Fix (optional, style improvement only):* add a one-line "→ see `.working/key-live-race.html`" pointer directly under the Replay bar's Component Patterns row and similarly for the other 3 mocked surfaces.
- DESIGN.md's silence on mocks is consistent with the design-example reference files (none of them cite `mockups/*` either) — not a gap.

## 6. Bloat & overspecification — strong
No padding observed. DESIGN.md's frontmatter is scoped to exactly the 9 components Phase 2 introduces or changes; no restatement of inherited colors/typography/spacing/shapes. EXPERIENCE.md's tables are consistently marked "Addition to phase-1's table" and contain only genuinely new rows. Flow prose is rich but every sentence carries information (specific driver names, specific lap numbers) rather than generic filler — consistent with the illustrative examples' density. The one literal hex value (`#262b33`) is justified rather than dropped in silently.

### Findings
None.

## 7. Inheritance discipline — strong
Checked: `sources:` frontmatter resolution, UJ traceability, component-name identity across files, EXPERIENCE.md-to-DESIGN.md token resolution.

Both files' `sources:` frontmatter list the identical 4 paths in the identical order; all 4 resolve (phase-1 DESIGN.md, phase-1 EXPERIENCE.md, phase-2 prd.md, phase-2 addendum.md — all read and confirmed to exist). The IA table's "Realizes" column (FR-1–FR-4, FR-5–FR-9, FR-10, FR-11/FR-12, FR-13–FR-16, FR-17, FR-18) matches the PRD's own UJ "Realizes" lists exactly, surface for surface. Component names are identical across DESIGN.md and EXPERIENCE.md modulo kebab-case↔Title Case formatting (the same convention phase-1 and the reference examples use). The addendum.md cross-references (track-outline sourcing resolved via f1db, Fan Card asset sourcing left open) are represented accurately in both DESIGN.md's inline notes and the memlog.

### Findings
- **low** EXPERIENCE.md's Key Flows invent specific persona names (Priya, Tom, Aisha, Marcus, Elena) that don't appear in the PRD (which uses generic descriptions like "a returning fan"). This is consistent with the spirit of the PRD's UJs (context/entry-state/path all map cleanly) and not a factual mismatch, but no flow explicitly cites "UJ-1"/"UJ-2" etc. by ID, so traceability from flow to UJ is by content-matching only, not an explicit label. Neither phase-1 nor the reference examples use explicit UJ-ID labels either, so this is a house-style observation, not a defect.

## 8. Shape fit — strong
Checked: DESIGN.md section order against the spec's canonical order; EXPERIENCE.md against its required-defaults list.

DESIGN.md present sections (Brand & Style → Layout & Spacing → Components → Do's and Don'ts) are a subsequence of the canonical 8-section order with no ordering violations — omitted sections (Colors, Typography, Elevation & Depth, Shapes) are exactly the ones this extension inherits unchanged, correctly per the IMPORTANT CONTEXT note. EXPERIENCE.md contains all 8 required-default sections (Foundation, Information Architecture, Voice and Tone, Component Patterns, State Patterns, Interaction Primitives, Accessibility Floor, Key Flows) in the correct relative order, plus Responsive & Platform in the same slot phase-1 and the Drift example use it (between Accessibility Floor and Key Flows). Inspiration & Anti-patterns is omitted, which is permitted (not in the required-defaults list) and consistent with an extension-only pass that isn't introducing new aesthetic lineage to justify.

### Findings
None.

## Mechanical notes

- **Contradiction, EXPERIENCE.md:72 vs EXPERIENCE.md:130.** State Patterns row "Zero completed races this season" fully specifies the behavior (no Replay bar, on-brand message with next-race name/date) and matches memlog line 19's explicit decision. Flow 2's failure note nonetheless states this same scenario "is not covered by the memlog or PRD" and is "unspecified — not flagged as an open question anywhere upstream either." Both claims in the failure note are false as of this document; it reads as an unresolved artifact from before the state row/memlog decision were added and was never reconciled.
- **Frontmatter completeness:** both files' frontmatter blocks are complete and internally consistent (`name`, `status: draft`, `updated: 2026-07-11` match across both; `sources:` identical). No missing required frontmatter keys per the DESIGN.md spec (`name` present; `description` present in DESIGN.md only, which is fine — EXPERIENCE.md's spec doesn't require one).
- **No broken cross-refs found** in `{path.to.token}` resolution (see §2) or in the `.working/` file paths cited (all 4 exist at the cited relative path).
- **Undisclosed gap** (see §3 finding): the past-winners list under `circuit-stat-tile`/"Circuit History section" is the one component-level gap in this pair that isn't self-flagged with `[NOTE FOR UX]`, unlike the other 5 known open items, which are all transparently marked.
