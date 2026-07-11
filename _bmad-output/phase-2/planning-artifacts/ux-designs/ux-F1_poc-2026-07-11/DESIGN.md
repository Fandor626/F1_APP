---
name: F1_poc Phase 2
description: UI/UX improvement pass on the shipped F1_poc dark editorial system. Extension only — inherits every color, typeface, radius, and spacing value from phase-1 DESIGN.md unchanged; this file specifies only new or changed component tokens.
status: final
updated: 2026-07-11
sources:
  - _bmad-output/phase-1/planning-artifacts/ux-designs/ux-F1_poc-2026-06-16/DESIGN.md
  - _bmad-output/phase-1/planning-artifacts/ux-designs/ux-F1_poc-2026-06-16/EXPERIENCE.md
  - _bmad-output/phase-2/planning-artifacts/prds/prd-F1_poc-2026-07-11/prd.md
  - _bmad-output/phase-2/planning-artifacts/prds/prd-F1_poc-2026-07-11/addendum.md
components:
  sidebar-championship:
    background: '{colors.bg-inset}'
    radius: '{rounded.lg}'
    padding: '20px 18px'
    width: '250px'
    position: 'sticky, left rail'
    hairline: 'none (well treatment, not a card — deliberately omits border-soft and bg-card)'
    mobile: 'collapses to a tappable drawer summary above the race list'
  weekend-card-v2:
    extends: '{components.weekend-card}'
    growth: 'taller only — same width as phase-1 card, more content stacked'
    trackOutlinePanel:
      background: '{colors.bg-inset}'
      radius: '{rounded.md}'
      width: '168px'
      position: 'distinct left-side panel, text/content on the right'
    fastestLapRow:
      label: '{colors.text-tertiary}'
      value: '{colors.text-primary}'
      driver: '{colors.text-secondary}'
      structure: 'two explicit labeled lines — all-time and current-year, each with driver name'
  replay-bar:
    position: 'fixed bottom (video-player convention)'
    background: '{colors.bg-card}'
    border: '{colors.border-soft}'
    height: '76px'
    playPause:
      shape: circle
      border: '{colors.accent-editorial}'
      icon: '{colors.accent-editorial}'
    lapReadout:
      text: '{colors.text-primary}'
      fontFamily: '{typography.numeric-dense}'
      format: 'Lap {n} / {total}'
      position: 'inline, left of scrub bar'
    scrub:
      style: 'snap-to-lap discrete ticks, not continuous'
      track: '#262b33 (unnamed inherited neutral, matches existing timeline track color)'
      fill: '{colors.accent-editorial}'
      thumb: '{colors.accent-editorial}'
    speedGroup:
      extends: '{components.tab-toggle}'
      options: ['1x', '2x', '4x']
      activeText: '{colors.accent-editorial}'
    mobile: 'only play/pause + scrub stay inline; Restart and speedGroup collapse behind an overflow (⋯) button, same bordered-circular treatment as play/pause'
  trading-card:
    aspectRatio: '5:7 (portrait, trading-card convention)'
    background: '{colors.bg-card}'
    border: '{colors.border-soft}'
    radius: '{rounded.lg}'
    teamRule:
      height: '4px'
      treatment: 'thin top strip in constructor color only — never a full-bleed team-color card background'
    photoPanel:
      background: '{colors.bg-inset}'
      treatment: 'recessed well, same pattern as track-outline-panel and track map containers'
    autograph:
      styleIntent: 'genuine handwritten signature (visual intent, independent of technical realization)'
    layout: 'grid, not carousel — multiple cards shown simultaneously'
  fancard-prompt-modal:
    extends: 'existing one-level-deep overlay/modal pattern (phase-1)'
    entry: 'launches straight into the existing 3-step wizard-step component, not a separate lightweight picker'
  prediction-callout:
    background: '{colors.bg-card}'
    border: '{colors.accent-editorial}'
    radius: '{rounded.lg}'
    label: '{colors.accent-editorial}'
    usage: 'new non-team use of accent-editorial — the product-voice color applied to a content callout, not just live/link/focus chrome'
    toggle:
      extends: '{components.inline-link} treatment for affordance, but rendered as a bordered button'
      revealsRawTable: true
  track-records-section:
    separateFrom: 'track-layout panel — its own card, per memlog decision'
    rowBackground: '{colors.bg-inset}'
    rowRadius: '{rounded.md}'
  circuit-stat-tile:
    background: '{colors.bg-inset}'
    radius: '{rounded.md}'
    textAlign: center
  news-preview-row:
    layout: 'thumbnail left (list-row style), one-line snippet'
---

## Brand & Style

This is an extension, not a restatement. Every phase-1 posture holds: the calm dark-editorial register, restrained team-color use, `accent-editorial` reserved for the product's own voice, flat/no-shadow depth, the Avenir Next type ramp. Phase 2 does not introduce a second visual register — it adds new surfaces and components that are built from the same tonal-surface and token vocabulary as phase-1.

One deliberate expansion: `accent-editorial` picks up a new job. It was previously "live status, focus rings, links" only — never a content color. The Win Prediction callout (§Components) uses it as a bordered callout background treatment, a genuine first for the token. This was an explicit memlog decision, not scope creep: the callout still isn't a team color and still isn't reused for broadcast-coded values, so it doesn't violate the phase-1 Do's and Don'ts — it extends the definition of "the product's own voice" to include "the product's own opinion" (a prediction).

## Layout & Spacing

- **Championship Sidebar** is a left rail on desktop: `250px` fixed column beside the main content in a two-column grid (`250px 1fr`, `{spacing.6}` gap), sticky-positioned so it stays reachable while the race list scrolls. It is explicitly *not* a card — no `border-soft` hairline, no `bg-card` fill — but uses the `bg-inset` well treatment, the same recessed pattern phase-1 established for chart/map containers. On mobile it collapses to a drawer.
- **Race Weekend card v2** keeps the phase-1 card's width; it only grows taller as more content (track outline panel, two fastest-lap lines) stacks in. The track outline sits in a distinct left-side sub-panel (its own `bg-inset` well within the card), with all text content to its right — not overlaid or interleaved.
- **Race Replay bar** is fixed to the viewport bottom (`height: 76px`), overlaying page content; `{spacing.7}`-scale bottom padding is reserved on the page so nothing sits underneath the bar — the same "controls anchor to the bottom, content scrolls above" convention as a video player.
- **Fan Card grid** uses `auto-fill, minmax(226px, 1fr)` with `{spacing.6}` gaps — cards reflow by count rather than a fixed column count, so 1 card and 8 cards both lay out cleanly.

## Components

- **`sidebar-championship`** — Calendar page only. Two standings groups (Drivers, Constructors), same live data source as the Standings page. `[NOTE FOR UX]` the memlog records a third sidebar slot left open ("maybe something else" — PRD Open Question 5) with no decision made; this spine ships a two-group sidebar and treats the third slot as unresolved, not silently decided against.
- **`weekend-card-v2`** — extends phase-1's `weekend-card` (still gets the `accent-editorial` pinned border for the next race). Adds the track-outline sub-panel and the two-line fastest-lap block. No longer renders championship standings (moved to the sidebar, per FR-4).
- **`replay-bar`** — visible only when the Live Race page is in fallback/replay mode (no live session). Play/pause is a bordered circular button; the scrub bar uses discrete snap-to-lap ticks (not a continuous drag), with major/minor tick styling to mark readable lap intervals; speed control is a 1x/2x/4x button group built from the existing `tab-toggle` visual pattern. On mobile (`< md`), only play/pause and the scrub bar stay inline; Restart and the speed group collapse behind a compact overflow (`⋯`) button using the same bordered-circular-button treatment as play/pause, opening a small menu anchored above it.
- **`trading-card`** — the Fan Card's new visual unit: portrait 5:7 aspect, constructor color reduced to a 4px top rule (never a full-bleed background, consistent with the inherited Do's and Don'ts), a recessed `bg-inset` photo panel, and a script-styled autograph line. Multiple cards render as a grid, plus an "Add new card" dashed-border tile using the same aspect ratio. `[NOTE FOR UX]` per `addendum.md` (PRD Open Question 4), driver photo, autograph, and team-logo asset sourcing is unresolved — the rendered mock uses explicit placeholders (silhouette icon + car number on the photo well, a color monogram instead of a licensed team logo, a CSS script font instead of a scanned signature) rather than implying real assets exist. This spine specifies the container and treatment; it does not resolve the asset-sourcing question, which sits with the architect per the addendum.
- **`fancard-prompt-modal`** — Standings-page discovery nudge (FR-10). Reuses the existing one-level-deep modal/overlay pattern and drops the user straight into the existing 3-step wizard rather than a new lightweight picker component.
- **`prediction-callout`** — Race Weekend Detail. Plain-language Win Prediction in an `accent-editorial`-bordered card, sitting above a toggle-revealed raw grid-slot win-probability table (the MVP's original component, kept — not deleted, per memlog).
- **`track-records-section`** — Race Weekend Detail. A separate card from the track-layout panel (explicit memlog decision, not merged into one "track" mega-card): all-time and current-year fastest lap, each as a `bg-inset` row.
- **`circuit-stat-tile`** — Race Weekend Detail's Circuit History card. Four `bg-inset` tiles (length, corners, DRS zones, first F1 race year) above the past-winners list. This tile grid appears in the rendered mock but has no separate decision recorded in the memlog; it's promoted from the approved mock, not invented fresh. `[NOTE FOR UX]` the past-winners list itself (year, driver, team — below the stat tiles) has no visual-spec token of its own here; treat it as reusing inherited list-row defaults (phase-1's plain-row pattern) until a real decision is made, rather than assuming a bespoke treatment.
- **`news-preview-row`** — thumbnail on the left, list-row style, one-line snippet, per memlog. `[NOTE FOR UX]` no key-screen mock was rendered for the News Feed page and the memlog gives no pixel-level sizing (thumbnail dimensions, row height, snippet truncation width) — those visual details are undecided; implement conservatively against the existing list-row/`gap-list-row` spacing vocabulary until a mock or explicit token decision exists.
- **Profile grouped-stats presentation** (Driver/Constructor/Circuit Profile, FR-17) — memlog records the structural decision ("grouped into season-by-season vs. career totals... stacked sections on one scrollable page, not tabs") but no visual mock was rendered and no component tokens were elicited (section header treatment, divider style, spacing between groups). `[NOTE FOR UX]` this is a real gap: the grouping is decided, its visual rendering is not.

## Do's and Don'ts

| Do | Don't |
|---|---|
| Use `accent-editorial` for the Win Prediction callout — it's the product's own voice/opinion, consistent with its existing role | Use `accent-editorial` (or any callout treatment resembling it) for a team-scoped or broadcast-coded value |
| Keep the Championship Sidebar a `bg-inset` well, visually distinct from card content | Give the sidebar a `bg-card` + `border-soft` treatment — that would read as "just another card," losing the recessed/persistent distinction |
| Keep constructor color on the Fan Card to a thin top rule + small badge | Fill the Fan Card background or photo panel in team color |
| Let the Race Weekend card grow taller to fit new content | Widen the card or break its established grid column width |
| Use snap-to-lap discrete ticks on the Replay scrub bar | Render the scrub bar as a continuous/analog seek control — laps are the unit that matters, not seconds |
