---
name: F1_poc
description: Personal F1 fan web app. Dark by default, broadcast-standard color coding (mini-sectors, tyre compounds), F1 constructor colors as the accent system rather than one fixed brand color.
status: final
updated: 2026-06-16
colors:
  bg-app: '#14171c'
  bg-card: '#1b1f26'
  bg-card-hover: '#20242c'
  bg-inset: '#11141a'
  border-soft: '#2a2f38'
  text-primary: '#eef0f3'
  text-secondary: '#9aa1ad'
  text-tertiary: '#6b7280'
  text-dim: '#4b5160'
  accent-editorial: '#d8b65c'
  sector-purple: '#b066ff'
  sector-green: '#2ee686'
  sector-yellow: '#f2cf4a'
  sector-white: '#f4f6f8'
  tyre-soft: '#ef4444'
  tyre-medium: '#f2cf4a'
  tyre-hard: '#f4f6f8'
  tyre-intermediate: '#2ee686'
  tyre-wet: '#4d8dff'
  team-redbull: '#2741a6'
  team-ferrari: '#ef1a2d'
  team-mercedes: '#1fd6c1'
  team-mclaren: '#ff8a1e'
typography:
  display:
    fontFamily: 'Avenir Next'
    fontSize: 26px
    fontWeight: '700'
    lineHeight: '1.2'
    letterSpacing: -0.01em
  heading:
    fontFamily: 'Avenir Next'
    fontSize: 15px
    fontWeight: '700'
    lineHeight: '1.3'
    letterSpacing: -0.01em
  body:
    fontFamily: 'Avenir Next'
    fontSize: 13px
    fontWeight: '400'
    lineHeight: '1.5'
  label:
    fontFamily: 'Avenir Next'
    fontSize: 11.5px
    fontWeight: '600'
    lineHeight: '1.4'
    letterSpacing: 0.04em
  numeric-dense:
    fontFamily: 'Avenir Next'
    fontSize: 12px
    fontWeight: '400'
    lineHeight: '1.3'
rounded:
  sm: 6px
  md: 12px
  lg: 14px
  full: 9999px
spacing:
  '1': 4px
  '2': 8px
  '3': 12px
  '4': 16px
  '5': 22px
  '6': 28px
  '7': 32px
  dense-row: '6px 10px'
  card-padding: '18px 22px'
components:
  nav-topbar:
    background: '{colors.bg-card}'
    border: '{colors.border-soft}'
    height: 56px
  card:
    background: '{colors.bg-card}'
    border: '{colors.border-soft}'
    radius: '{rounded.lg}'
    padding: '{spacing.card-padding}'
    hover: 'none'
  weekend-card:
    extends: '{components.card}'
    pinnedBorder: '{colors.accent-editorial}'
  gap-list-row:
    padding: '{spacing.dense-row}'
    fontSize: '{typography.numeric-dense.fontSize}'
    hoverBackground: '{colors.bg-card-hover}'
    teamChip:
      shape: dot
      size: 7px
      radius: '{rounded.full}'
    tyreDot:
      shape: dot
      size: 10px
      radius: '{rounded.full}'
      border: 'rgba(0,0,0,0.4)'
    battleHighlight:
      background: '{colors.sector-yellow}'
      opacity: 0.16
      text: '{colors.text-primary}'
  tab-toggle:
    background: '{colors.bg-card}'
    activeBackground: '{colors.bg-card-hover}'
    activeText: '{colors.text-primary}'
    radius: '{rounded.md}'
  inline-link:
    text: '{colors.text-primary}'
    hoverUnderline: '{colors.accent-editorial}'
  toggle-switch:
    track: '{colors.bg-inset}'
    thumb: '{colors.text-primary}'
    activeTrack: '{colors.accent-editorial}'
    radius: '{rounded.full}'
  wizard-step:
    background: '{colors.card}'
    indicatorActive: '{colors.accent-editorial}'
    indicatorInactive: '{colors.border-soft}'
  search-dropdown:
    background: '{colors.bg-card}'
    border: '{colors.border-soft}'
    radius: '{rounded.md}'
    highlightedResult: '{colors.bg-card-hover}'
  stale-value:
    text: '{colors.text-dim}'
    prefix: '~'
  live-badge:
    background: 'transparent'
    border: '{colors.accent-editorial}'
    text: '{colors.accent-editorial}'
    radius: '{rounded.full}'
    pulse: true
---

## Brand & Style

F1_poc reads as a premium motorsport publication's live blog, not a pitwall telemetry HUD. The product spans two registers — a real-time cockpit on race weekends, a calm stats-and-history reference the rest of the week — and the visual identity stays the *same calm dark editorial voice* across both rather than switching personality. The one deliberate exception is the Live Gap List, which borrows a denser, more telemetry-like table layout out of necessity (20 drivers must be scannable without scrolling) while keeping every color and type token from the editorial system. Density is a local exception, not the house style.

Team identity is expressed through F1 constructor colors used as a restrained accent system — small dots, thin borders, left-rule strips — never as full-bleed backgrounds or a competing brand palette. The one fixed non-team accent (`accent-editorial`, a warm gold) marks the product's own voice: live status, focus rings, links — never used to represent a team or a broadcast color-coded value.

## Colors

- **Surfaces** (`bg-app` → `bg-card` → `bg-card-hover` → `bg-inset`) form a tonal scale from page background to recessed wells (chart/map containers), each one step lighter than the last except `bg-inset`, which sits deliberately darker than `bg-app` to read as "recessed" behind cards.
- **Text** (`text-primary` → `text-dim`) is a four-step ladder: primary for headings/numerals, secondary for captions, tertiary for muted meta text, dim reserved *exclusively* for stale/disabled values — never used for ordinary de-emphasis, so "dim" always means "this number is not current."
- **`accent-editorial`** (warm gold, `#d8b65c`) is the product's own voice: live badges, focus rings, link hovers, the masthead. It is not a team color and never appears on a driver/constructor-scoped element.
- **Broadcast sector and tyre colors** are fixed and not reusable for anything else — they encode a single, specific meaning each (mini-sector pace tier, tyre compound) exactly as real F1 graphics do, so returning fans recognize them instantly. Do not repurpose `sector-purple` etc. for unrelated "best/highlighted" states elsewhere in the product.
- **Constructor accents** are used sparingly (dot, thin left-rule, or badge) everywhere *except* the Gap List row, where the team chip is still just a small dot — full-row team-color paint was explicitly tried (Cockpit HUD direction) and rejected as too intense for a two-hour sit.
- Only 4 constructor colors are decided (Red Bull, Ferrari, Mercedes, McLaren — the set used in mockups). **Open item:** the full current-season constructor roster needs a runtime-loaded color mapping (mirroring the architecture's `circuit-configs/*.json` pattern), not a hardcoded 10-entry table here, since the grid changes season to season.

## Typography

Single humanist sans family (`Avenir Next`, falling back to system UI stacks) across every role — no serif or display-face moment; the product doesn't need editorial punctuation, it needs to stay legible at speed during a live session.

- `display` (26px/700) — page-level titles only, one per page.
- `heading` (15px/700) — card and panel titles.
- `body` (13px/400) — default reading text, captions, table cells outside the dense gap list.
- `label` (11.5px/600, tracked +0.04em) — uppercase-style section labels ("LIVE GAP LIST", "SECTOR 1").
- `numeric-dense` (12px/400) — the Gap List's compact row text only. Slightly smaller than `body` and tightly leaded; this is the one place density wins over comfort, by deliberate exception.

## Layout & Spacing

Spacing scale is a near-linear ramp (4 / 8 / 12 / 16 / 22 / 28 / 32px). `card-padding` (18px 22px) is the standard interior padding for any card/panel. `dense-row` (6px 10px) is the explicit exception for Gap List rows — roughly a third of a card's padding, which is exactly what buys back the vertical space needed to show ~20 drivers without scrolling.

Outer page padding is generous (28-32px) — the calm-editorial register extends to the page frame, not just the cards. Multi-panel pages (Live Race) use a grid, not a single column: gap list + lap chart + timeline in one column, track map + sector board + tyre overview in a second, so the page reads as a dashboard without feeling cramped, except inside the gap list itself.

## Elevation & Depth

Flat by design — no drop shadows, no floating-card elevation. Depth comes entirely from the tonal surface ladder (`bg-app` → `bg-card` → `bg-card-hover`): a card reads as "lifted" because it's a step lighter than the page behind it, not because of a shadow. `bg-inset` wells (chart/map containers) read as recessed the same way, one step darker than the page. This keeps the dark theme from getting muddy with overlapping shadow layers.

## Shapes

Soft but not playful: `rounded.lg` (14px) for cards/panels, `rounded.md` (12px) for smaller interactive elements (tabs, dropdowns, toggle tracks), `rounded.sm` (6px) for tight inline elements, `rounded.full` for every dot/chip/pill (team chips, tyre dots, the live badge). No sharp corners anywhere in the editorial system — sharp corners were Cockpit HUD's signature and were explicitly rejected as the default register.

## Components

- **`nav-topbar`** — persistent across all pages, `bg-card` background, collapses to a hamburger menu on mobile. Houses the streak-counter badge.
- **`card`** — the base panel: `bg-card`, `border-soft` hairline, `rounded.lg`, `card-padding`. Every page surface (calendar cards, standings tables, profile sections, sector board, chart wells) is built from this.
- **`weekend-card`** — extends `card`; the next/pinned Race Weekend gets an `accent-editorial` highlight border, otherwise identical.
- **`gap-list-row`** — the one deliberate density exception (see Brand & Style). `numeric-dense` text, `dense-row` padding, single line per driver. Nested tokens: `teamChip` (7px dot), `tyreDot` (10px dot, dark border), `battleHighlight` (sub-1s gap, low-opacity `sector-yellow` wash — restrained, not Cockpit HUD's solid amber box).
- **`tab-toggle`** — Standings' Drivers/Constructors switch; instant, no route change, `bg-card-hover` marks the active tab.
- **`inline-link`** — any driver/circuit name anywhere in the app; primary text color, gold underline only on hover (no permanent underline — keeps body text calm).
- **`toggle-switch`** — the Track/Local timezone toggle; gold when active.
- **`wizard-step`** — Fan Card setup; gold step indicator for current step, `border-soft` for upcoming steps.
- **`search-dropdown`** — the Head-to-Head driver picker; `bg-card-hover` marks the keyboard-highlighted result.
- **`stale-value`** — any live-race field that missed its join-tolerance window; `text-dim` + a literal `~` prefix character, never just a faded number with no marker.
- **`live-badge`** — outlined pill (not filled), gold border/text, subtle pulse animation (respects `prefers-reduced-motion`).

## Do's and Don'ts

| Do | Don't |
|---|---|
| Keep the calm dark-editorial register on every page, including Live Race's surrounding chrome | Apply Cockpit HUD's monospace/sharp-corner/glow treatment anywhere outside the one row exception |
| Use constructor colors as small dots/badges/thin rules | Paint full row or card backgrounds in team color |
| Reserve `sector-*` and `tyre-*` tokens for their one broadcast meaning each | Reuse a sector/tyre color for an unrelated "highlighted" or "success" state |
| Mark every stale value with `text-dim` **and** a `~` prefix | Fade a stale number without the `~` marker, or leave it looking current |
| Use `accent-editorial` for the product's own voice (live, links, focus) | Use `accent-editorial` to represent a team or a broadcast-coded value |
| Let the Gap List be the one dense table in the product | Carry the dense-row pattern into any other table (standings, news feed, tyre overview) |
